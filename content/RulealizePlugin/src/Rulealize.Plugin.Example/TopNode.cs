using Rulealize.Abstraction.Building;
using Rulealize.Abstraction.Evaluation;
using Rulealize.Abstraction.Node;
using Rulealize.Abstraction.Value;

namespace Rulealize.Plugin.Example
{
    // ONE OF THE THREE WORKED EXAMPLES. Copy the shape, write your own, delete this file.
    //
    // An expression node: it produces a value and may appear anywhere a value is wanted --
    // a guard, a definition, an effect's argument, the terminal condition.
    //
    // The JSON it answers to:
    //
    //   { "op": "yourns.top", "of": "$pile" }   ->  the token on top, or null when empty
    //
    internal sealed class TopNode(ExpressionNode pile) : ExpressionNode
    {
        // Build runs once, when the rule set is compiled. It is the only place JSON is
        // looked at. Ask for whatever was written under "of" and it comes back assembled;
        // what it turned out to be -- a literal, a $field, another operation -- is not this
        // node's business.
        public static ExpressionNode Build(INodeBuildContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            return new TopNode(context.RequireExpression("of"));
        }

        // Evaluate runs every time, and for GetValidInputs that is once per candidate. No
        // JSON here. Always the same four steps: evaluate the children, take them apart into
        // ordinary C#, compute, and put the answer back into a RuleValue.
        public override RuleValue Evaluate(IEvaluationContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            RuleValue value = pile.Evaluate(context);

            // No value in, no value out. AsSequence faults on null, so leaving this out is
            // what makes a vocabulary that works until the day something upstream returns
            // nothing.
            if (value.IsNull)
            {
                return RuleValue.Null;
            }

            // The argument to AsSequence is where an error message says it happened. Write it
            // as operation.property; it is the only thing standing between a fault and a hunt
            // through the document.
            RuleValue top = RuleValue.Null;
            foreach (RuleValue token in value.AsSequence("yourns.top.of"))
            {
                // Anything that loops checks the token. GetValidInputs evaluates a guard once
                // per candidate, and that is the work a caller is most likely to abandon.
                context.CancellationToken.ThrowIfCancellationRequested();
                top = token;
            }

            // An empty pile has no top, and that is null rather than a fault. Reading is
            // lenient so that a rule set does not have to measure before it reads; writing is
            // strict, which is why the effect next door refuses instead.
            return top;
        }
    }
}
