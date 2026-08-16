// Copyright (c) 2026 Reny
// Licensed under the Apache License, Version 2.0.

using Rulealize.Abstraction.Building;
using Rulealize.Abstraction.Evaluation;
using Rulealize.Abstraction.Node;
using Rulealize.Abstraction.Value;

namespace Rulealize.Plugin.Example
{
    // THIS IS THE EXAMPLE. Copy its shape, write your own, and delete this file.
    //
    // What it does -- measuring a text -- has nothing to do with your vocabulary. It is here
    // because it is the shortest operation in which all four steps of an Evaluate are
    // visible at once.
    //
    // The JSON it answers to:
    //
    //   { "op": "yourns.example", "of": "tally" }   ->  5
    //
    internal sealed class ExampleNode(ExpressionNode operand) : ExpressionNode
    {
        // Build runs once, when the rule set is compiled. It is the only place JSON is
        // looked at. Ask for whatever was written under "of" and it comes back assembled;
        // what it turned out to be -- a literal, a $field, another operation -- is not this
        // node's business.
        public static ExpressionNode Build(INodeBuildContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            return new ExampleNode(context.RequireExpression("of"));
        }

        // Evaluate runs every time, and for GetValidInputs that is once per candidate. No
        // JSON here. Always the same four steps: evaluate the children, take them apart into
        // ordinary C#, compute, and put the answer back into a RuleValue.
        public override RuleValue Evaluate(IEvaluationContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            RuleValue value = operand.Evaluate(context);

            // No value in, no value out. AsText faults on null, so leaving this out is what
            // makes a vocabulary that works until the day something upstream returns nothing.
            if (value.IsNull)
            {
                return RuleValue.Null;
            }

            // The argument to AsText is where an error message says it happened. Write it as
            // operation.property; it is the only thing standing between a fault and a hunt
            // through the document.
            return RuleValue.Number(value.AsText("yourns.example.of").Length);
        }
    }
}
