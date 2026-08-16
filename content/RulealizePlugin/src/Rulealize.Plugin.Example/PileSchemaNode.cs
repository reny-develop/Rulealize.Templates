using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using Rulealize.Abstraction.Building;
using Rulealize.Abstraction.Node;
using Rulealize.Abstraction.Value;

namespace Rulealize.Plugin.Example
{
    // ONE OF THE THREE WORKED EXAMPLES. Copy the shape, write your own, delete this file.
    //
    // A schema node: it says what one state field holds, and it may appear in "state.schema"
    // and nowhere else. It is never evaluated -- there is no Evaluate here, and no
    // IEvaluationContext -- because a schema describes values rather than producing them.
    //
    // The JSON it answers to:
    //
    //   "schema":  { "pile": { "op": "yourns.pile", "max": 3 } }
    //   "initial": { "pile": ["red", "green"] }
    //
    // What makes it worth writing rather than reaching for `type.list` is the pair of
    // ReadJson and WriteJson below: a schema node owns the JSON its values are written as.
    // That is the seam that keeps the state vocabulary from knowing what a pile is, and it
    // is what a compact spelling of your own -- a board as "d3: black", a duration as "PT5M"
    // -- has to go through.
    internal sealed class PileSchemaNode(int? maximum) : SchemaNode
    {
        public static SchemaNode Build(INodeBuildContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            // Decidable from the document alone, so decided once, here, and reported with the
            // path in the document where it happened.
            int? maximum = context.TryGetProperty("max", out _) ? context.RequireInt32("max") : null;
            if (maximum is int limit && limit < 0)
            {
                throw context.Error("max", $"must not be negative, but is {limit}.");
            }

            return new PileSchemaNode(maximum);
        }

        // Whether null is a legal value for a field of this schema. An empty pile is written
        // [], which leaves null with nothing left to mean.
        public override bool IsNullable => false;

        // Validate reports rather than throws. One pass names every violation, so a malformed
        // state document takes one run to diagnose instead of as many runs as it has
        // mistakes.
        public override void Validate(RuleValue value, ISchemaValidationSink sink)
        {
            ArgumentNullException.ThrowIfNull(sink);

            if (value is not SequenceValue pile)
            {
                sink.Violation($"Expected a pile but got {RuleValue.Describe(value)}.");
                return;
            }

            int count = 0;
            foreach (RuleValue token in pile)
            {
                if (token is not TextValue)
                {
                    // The overload taking a path says where inside the value; the sink
                    // composes it with the path of the field it was created for.
                    sink.Violation(
                        string.Create(CultureInfo.InvariantCulture, $"[{count}]"),
                        $"Expected a token but got {RuleValue.Describe(token)}.");
                }

                count++;
            }

            if (maximum is int limit && count > limit)
            {
                sink.Violation(string.Create(
                    CultureInfo.InvariantCulture,
                    $"Expected at most {limit} tokens but got {count}."));
            }
        }

        // Reading a state document. Malformed JSON is reported to the sink and a best-effort
        // value returned, rather than thrown: the runtime checks the sink, and one bad field
        // should not hide the next one.
        public override RuleValue ReadJson(JsonElement element, ISchemaValidationSink sink)
        {
            ArgumentNullException.ThrowIfNull(sink);

            if (element.ValueKind != JsonValueKind.Array)
            {
                sink.Violation("Expected an array of tokens.");
                return RuleValue.EmptySequence;
            }

            ImmutableArray<RuleValue>.Builder tokens = ImmutableArray.CreateBuilder<RuleValue>();
            foreach (JsonElement entry in element.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.String)
                {
                    sink.Violation(
                        string.Create(CultureInfo.InvariantCulture, $"[{tokens.Count}]"),
                        "Expected a token.");
                    continue;
                }

                tokens.Add(RuleValue.Text(entry.GetString()!));
            }

            return RuleValue.Sequence(tokens.ToImmutable());
        }

        // Writing one. The value has already satisfied this schema, so there is nothing to
        // check here -- and the shape written is the shape ReadJson above will be handed
        // back, which is the whole of the contract.
        public override void WriteJson(Utf8JsonWriter writer, RuleValue value)
        {
            ArgumentNullException.ThrowIfNull(writer);
            ArgumentNullException.ThrowIfNull(value);

            writer.WriteStartArray();
            foreach (RuleValue token in value.AsSequence("yourns.pile"))
            {
                writer.WriteStringValue(token.AsText("yourns.pile"));
            }

            writer.WriteEndArray();
        }

        // Called once per field when a transition commits, before the new state is anything
        // anybody can see. Most schemas have nothing to do here; a sequence does, because the
        // value model lets one be lazy over the state it was built from. Stored as it is, a
        // state would hold a way to recompute itself from the state before it, and the chain
        // would grow with every move. Enumerating it here ends the chain at one.
        //
        // Not a place to reject anything -- that is Validate's business.
        public override RuleValue Normalize(RuleValue value)
        {
            ArgumentNullException.ThrowIfNull(value);

            if (value is not SequenceValue pile)
            {
                return value;
            }

            ImmutableArray<RuleValue> settled = [.. pile];
            return RuleValue.Sequence(settled);
        }
    }
}
