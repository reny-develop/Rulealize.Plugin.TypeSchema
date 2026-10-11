// Copyright (c) 2026 Reny
// Licensed under the Apache License, Version 2.0.

using System.Globalization;
using System.Text.Json;
using Rulealize.Abstraction.Building;
using Rulealize.Abstraction.Node;
using Rulealize.Abstraction.Value;

namespace Rulealize.Plugin.TypeSchema
{
    /// <summary>A number with a fixed most number of decimal places, optionally bounded: an amount of money, a rate.</summary>
    /// <remarks>
    /// <para>
    /// The value model's one numeric kind is already decimal, so what this adds is the
    /// constraint, as <see cref="IntegerSchemaNode"/> adds "whole": a value with more decimal
    /// places than <c>scale</c> is refused rather than rounded. Rounding is a rule — half up,
    /// half to even, always down — and a rule belongs in an expression a reader can see, not in
    /// a schema that quietly changes the value it was handed.
    /// </para>
    /// <para>
    /// The bounds are decimals, not integers, since a limit on an amount is as often 0.01 as 1,
    /// and each is held to the same <c>scale</c> as the values: a bound no value can equal would
    /// be a bound that says one thing and checks another.
    /// </para>
    /// </remarks>
    internal sealed class DecimalSchemaNode(int? scale, decimal? minimum, decimal? maximum, bool nullable) : ScalarSchemaNode(nullable)
    {
        /// <summary>The most decimal places <see cref="decimal"/> holds.</summary>
        private const int MostScale = 28;

        public override RecordValue Describe() => Bounds.Of(
            ("scale", Bounds.Number(scale)),
            ("min", Bounds.Number(minimum)),
            ("max", Bounds.Number(maximum)));

        public static SchemaNode Build(INodeBuildContext context)
        {
            int? scale = context.TryGetProperty("scale", out _) ? context.RequireInt32("scale") : null;
            if (scale is int places and (< 0 or > MostScale))
            {
                throw context.Error("scale", $"must be from 0 to {MostScale}, but is {places.ToString(CultureInfo.InvariantCulture)}.");
            }

            decimal? minimum = Bound(context, "min", scale);
            decimal? maximum = Bound(context, "max", scale);

            if (minimum is decimal low && maximum is decimal high && low > high)
            {
                throw context.Error($"min {RuleValue.FormatNumber(low)} is greater than max {RuleValue.FormatNumber(high)}, so no value satisfies this field.");
            }

            return new DecimalSchemaNode(scale, minimum, maximum, context.OptionalBoolean("nullable", false));
        }

        protected override void ValidateValue(RuleValue value, ISchemaValidationSink sink)
        {
            if (value is not NumberValue number)
            {
                sink.Violation($"Expected a number but got {RuleValue.Describe(value)}.");
                return;
            }

            Check(number.Value, sink);
        }

        protected override RuleValue ReadValue(JsonElement element, ISchemaValidationSink sink)
        {
            if (element.ValueKind != JsonValueKind.Number || !element.TryGetDecimal(out decimal number))
            {
                sink.Violation("Expected a number.");
                return RuleValue.Null;
            }

            Check(number, sink);
            return RuleValue.Number(number);
        }

        protected override void WriteValue(Utf8JsonWriter writer, RuleValue value) =>
            writer.WriteNumberValue(value.AsNumber("type.decimal"));

        private static decimal? Bound(INodeBuildContext context, string name, int? scale)
        {
            if (!context.TryGetProperty(name, out JsonElement element))
            {
                return null;
            }

            if (element.ValueKind != JsonValueKind.Number || !element.TryGetDecimal(out decimal bound))
            {
                throw context.Error(name, "must be a number.");
            }

            if (scale is int places && !Fits(bound, places))
            {
                throw context.Error(name, $"has more than {places.ToString(CultureInfo.InvariantCulture)} decimal places, so no value can equal it.");
            }

            return bound;
        }

        private static bool Fits(decimal number, int places) => decimal.Round(number, places) == number;

        private void Check(decimal number, ISchemaValidationSink sink)
        {
            if (scale is int places && !Fits(number, places))
            {
                sink.Violation($"Expected at most {places.ToString(CultureInfo.InvariantCulture)} decimal places "
                    + $"but got {RuleValue.FormatNumber(number)}.");
                return;
            }

            if (minimum is decimal low && number < low)
            {
                sink.Violation($"Expected at least {RuleValue.FormatNumber(low)} but got {RuleValue.FormatNumber(number)}.");
            }

            if (maximum is decimal high && number > high)
            {
                sink.Violation($"Expected at most {RuleValue.FormatNumber(high)} but got {RuleValue.FormatNumber(number)}.");
            }
        }
    }
}
