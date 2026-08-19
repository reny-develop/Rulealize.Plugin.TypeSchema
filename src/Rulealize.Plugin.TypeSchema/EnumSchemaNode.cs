// Copyright (c) 2026 Reny
// Licensed under the Apache License, Version 2.0.

using System.Collections.Immutable;
using System.Text.Json;
using Rulealize.Abstraction.Building;
using Rulealize.Abstraction.Node;
using Rulealize.Abstraction.Value;

namespace Rulealize.Plugin.TypeSchema
{
    /// <summary>A finite set of text values.</summary>
    /// <remarks>
    /// <para>
    /// The order of <c>values</c> carries no meaning. A field whose values are ordered
    /// wants <c>type.int</c>.
    /// </para>
    /// <para>
    /// A Reversi cell is written as this enumeration with <c>nullable</c> set, so that an
    /// empty square is null. Adding a third member for "empty" was considered and rejected:
    /// a coordinate off the board reads as null whatever the cell type says, and with a
    /// distinct empty member the two would stop agreeing. Collapsing "no stone here" and
    /// "there is no here" into one answer is what keeps the flip rule to a single
    /// comparison.
    /// </para>
    /// </remarks>
    internal sealed class EnumSchemaNode(ImmutableArray<string> values, bool nullable) : ScalarSchemaNode(nullable)
    {
        public static SchemaNode Build(INodeBuildContext context)
        {
            ImmutableArray<string> values = context.RequireStringArray("values");
            if (values.Length == 0)
            {
                throw context.Error("values", "must not be empty.");
            }

            HashSet<string> seen = new(StringComparer.Ordinal);
            foreach (string value in values)
            {
                if (!seen.Add(value))
                {
                    throw context.Error("values", $"'{value}' is listed more than once.");
                }
            }

            return new EnumSchemaNode(values, context.OptionalBoolean("nullable", false));
        }

        protected override void ValidateValue(RuleValue value, ISchemaValidationSink sink)
        {
            if (value is not TextValue text)
            {
                sink.Violation($"Expected one of {Members()} but got {RuleValue.Describe(value)}.");
                return;
            }

            if (!values.Contains(text.Value, StringComparer.Ordinal))
            {
                sink.Violation($"Expected one of {Members()} but got \"{text.Value}\".");
            }
        }

        protected override RuleValue ReadValue(JsonElement element, ISchemaValidationSink sink)
        {
            if (element.ValueKind != JsonValueKind.String)
            {
                sink.Violation($"Expected one of {Members()}.");
                return RuleValue.Null;
            }

            string text = element.GetString()!;
            if (!values.Contains(text, StringComparer.Ordinal))
            {
                sink.Violation($"Expected one of {Members()} but got \"{text}\".");
            }

            return RuleValue.Text(text);
        }

        protected override void WriteValue(Utf8JsonWriter writer, RuleValue value) =>
            writer.WriteStringValue(value.AsText("type.enum"));

        private string Members() => string.Join(", ", values);
    }
}
