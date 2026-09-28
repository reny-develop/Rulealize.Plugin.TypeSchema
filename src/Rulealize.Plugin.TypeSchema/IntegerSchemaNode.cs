// Copyright (c) 2026 Reny
// Licensed under the Apache License, Version 2.0.

using System.Globalization;
using System.Text.Json;
using Rulealize.Abstraction.Building;
using Rulealize.Abstraction.Node;
using Rulealize.Abstraction.Value;

namespace Rulealize.Plugin.TypeSchema
{
    /// <summary>A whole number, optionally bounded.</summary>
    /// <remarks>
    /// The value model has one numeric kind and does not distinguish integers from
    /// fractions, so "is a whole number" is expressed here, as a constraint, rather than by
    /// a separate kind of value.
    /// </remarks>
    internal sealed class IntegerSchemaNode(int? minimum, int? maximum, bool nullable) : ScalarSchemaNode(nullable)
    {
        public override RecordValue Describe() => Bounds.Of(
            ("min", Bounds.Number(minimum)),
            ("max", Bounds.Number(maximum)));

        public static SchemaNode Build(INodeBuildContext context)
        {
            int? minimum = context.TryGetProperty("min", out _) ? context.RequireInt32("min") : null;
            int? maximum = context.TryGetProperty("max", out _) ? context.RequireInt32("max") : null;

            if (minimum is int low && maximum is int high && low > high)
            {
                throw context.Error($"min {low} is greater than max {high}, so no value satisfies this field.");
            }

            return new IntegerSchemaNode(minimum, maximum, context.OptionalBoolean("nullable", false));
        }

        protected override void ValidateValue(RuleValue value, ISchemaValidationSink sink)
        {
            if (value is not NumberValue number)
            {
                sink.Violation($"Expected a whole number but got {RuleValue.Describe(value)}.");
                return;
            }

            Check(number.Value, sink);
        }

        protected override RuleValue ReadValue(JsonElement element, ISchemaValidationSink sink)
        {
            if (element.ValueKind != JsonValueKind.Number || !element.TryGetDecimal(out decimal number))
            {
                sink.Violation("Expected a whole number.");
                return RuleValue.Null;
            }

            Check(number, sink);
            return RuleValue.Number(number);
        }

        protected override void WriteValue(Utf8JsonWriter writer, RuleValue value) =>
            writer.WriteNumberValue(value.AsNumber("type.int"));

        private void Check(decimal number, ISchemaValidationSink sink)
        {
            if (decimal.Truncate(number) != number)
            {
                sink.Violation($"Expected a whole number but got {RuleValue.FormatNumber(number)}.");
                return;
            }

            if (minimum is int low && number < low)
            {
                sink.Violation($"Expected at least {low.ToString(CultureInfo.InvariantCulture)} "
                    + $"but got {RuleValue.FormatNumber(number)}.");
            }

            if (maximum is int high && number > high)
            {
                sink.Violation($"Expected at most {high.ToString(CultureInfo.InvariantCulture)} "
                    + $"but got {RuleValue.FormatNumber(number)}.");
            }
        }
    }
}
