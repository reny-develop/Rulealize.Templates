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
    //   { "op": "yourns.push", "target": "$pile", "token": "red" }
    //
    internal sealed class PushNode(StatePath pile, ExpressionNode token) : EffectNode
    {
        public static EffectNode Build(INodeBuildContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            return new PushNode(TargetPile.Resolve(context), context.RequireExpression("token"));
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
            ImmutableArray<RuleValue> tokens = [.. draft.Get(pile).AsSequence("yourns.push.target"), pushed];
            draft.Set(pile, RuleValue.Sequence(tokens));
        }
    }

    // How an effect reaches a field it is allowed to write to.
    //
    // The target is written "$pile", which builds into a node belonging to the state
    // vocabulary -- an assembly this one does not reference and cannot inspect. What it can
    // ask for is IStateLocation, the contract both reach through the abstraction, and take
    // the resolved path from it.
    //
    // The second check is the one worth copying. A StatePath carries the schema of the field
    // it resolved to, so pointing this effect at a field that is not a pile is refused here,
    // at build time, with the path in the document where it happened -- instead of faulting
    // on the first transition that reaches it.
    internal static class TargetPile
    {
        public static StatePath Resolve(INodeBuildContext context)
        {
            ExpressionNode target = context.RequireExpression("target");
            if (target is not IStateLocation location)
            {
                throw context.Error("target", "must denote a state field, such as \"$pile\".");
            }

            if (location.Path.Schema is not PileSchemaNode)
            {
                throw context.Error("target", $"'{location.Path}' is not a pile.");
            }

            return location.Path;
        }
    }
}
