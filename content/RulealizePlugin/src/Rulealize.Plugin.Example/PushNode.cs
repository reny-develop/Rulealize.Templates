using System.Collections.Immutable;
using Rulealize.Abstraction;
using Rulealize.Abstraction.Building;
using Rulealize.Abstraction.Evaluation;
using Rulealize.Abstraction.Node;
using Rulealize.Abstraction.Value;

namespace Rulealize.Plugin.Example
{
    // ONE OF THE THREE WORKED EXAMPLES. Copy the shape, write your own, delete this file.
    //
    // An effect node: it writes to the state, and it may appear in an input's "effects"
    // array and nowhere else. Using one as an expression is a build error, so the two
    // meanings never have to be told apart by reading.
    //
    // The JSON it answers to:
    //
    //   { "op": "yourns.push", "path": "pile", "token": "red" }
    //
    internal sealed class PushNode(StatePath path, ExpressionNode token) : EffectNode
    {
        public static EffectNode Build(INodeBuildContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            // A static key: read here, at build time, from a literal in the document. Writing
            // an expression under "path" is refused, which is what lets the field be resolved
            // against the schema once instead of on every transition.
            string field = context.RequireString("path");
            if (!context.State.TryResolve(field, out StatePath? path))
            {
                throw context.Error("path", $"'{field}' is not a field of the state schema.");
            }

            return new PushNode(path, context.RequireExpression("token"));
        }

        // Read from context, write to draft. Everything this evaluates through the context
        // sees the state as it was when the input was applied, not as amended by an earlier
        // effect in the same array -- which is what lets an input be written in the order a
        // person would describe it.
        public override void Apply(IEvaluationContext context, IStateDraft draft)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(draft);

            RuleValue pushed = token.Evaluate(context);

            // Writing is strict where reading is lenient. Silently pushing nothing would give
            // a pile one token shorter than the rule set believes it wrote.
            if (pushed.IsNull)
            {
                throw new RuleEvaluationException("yourns.push.token", "There is nothing to push.");
            }

            // draft.Get rather than context, deliberately: this is the one read that must see
            // what an earlier effect wrote, so that two pushes in one input both land. The
            // schema node decides what a pile is; this only adds to the end of one.
            ImmutableArray<RuleValue> pile = [.. draft.Get(path).AsSequence("yourns.push.path"), pushed];
            draft.Set(path, RuleValue.Sequence(pile));
        }
    }
}
