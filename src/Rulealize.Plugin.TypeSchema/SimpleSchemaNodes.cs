// Copyright (c) 2026 Reny
// Licensed under the Apache License, Version 2.0.

using System.Globalization;
using System.Text.Json;
using Rulealize.Abstraction.Building;
using Rulealize.Abstraction.Node;
using Rulealize.Abstraction.Value;

namespace Rulealize.Plugin.TypeSchema
{
    /// <summary>A boolean.</summary>
    internal sealed class BooleanSchemaNode(bool nullable) : ScalarSchemaNode(nullable)
    {
        public static SchemaNode Build(INodeBuildContext context) =>
            new BooleanSchemaNode(context.OptionalBoolean("nullable", false));

        protected override void ValidateValue(RuleValue value, ISchemaValidationSink sink)
        {
            if (value is not BooleanValue)
            {
                sink.Violation($"Expected true or false but got {RuleValue.Describe(value)}.");
            }
        }

        protected override RuleValue ReadValue(JsonElement element, ISchemaValidationSink sink)
        {
            if (element.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            {
                sink.Violation("Expected true or false.");
                return RuleValue.Null;
            }

            return RuleValue.Boolean(element.GetBoolean());
        }

        protected override void WriteValue(Utf8JsonWriter writer, RuleValue value) =>
            writer.WriteBooleanValue(value.AsBoolean("type.bool"));
    }

    /// <summary>Text, optionally bounded in length.</summary>
    /// <remarks>
    /// There is no pattern constraint, and deliberately so: a rule set whose validity
    /// depended on a regular expression dialect would not validate the same way everywhere.
    /// Most of the cases that reach for a pattern are really enumerations.
    /// </remarks>
    internal sealed class StringSchemaNode(int? minimumLength, int? maximumLength, bool nullable)
        : ScalarSchemaNode(nullable)
    {
        public static SchemaNode Build(INodeBuildContext context)
        {
            int? minimumLength = context.TryGetProperty("minLength", out _) ? context.RequireInt32("minLength") : null;
            int? maximumLength = context.TryGetProperty("maxLength", out _) ? context.RequireInt32("maxLength") : null;

            if (minimumLength is int low and < 0)
            {
                throw context.Error("minLength", $"must not be negative, but is {low.ToString(CultureInfo.InvariantCulture)}.");
            }

            if (minimumLength is int lower && maximumLength is int upper && lower > upper)
            {
                throw context.Error("minLength is greater than maxLength, so no value satisfies this field.");
            }

            return new StringSchemaNode(minimumLength, maximumLength, context.OptionalBoolean("nullable", false));
        }

        protected override void ValidateValue(RuleValue value, ISchemaValidationSink sink)
        {
            if (value is not TextValue text)
            {
                sink.Violation($"Expected text but got {RuleValue.Describe(value)}.");
                return;
            }

            Check(text.Value, sink);
        }

        protected override RuleValue ReadValue(JsonElement element, ISchemaValidationSink sink)
        {
            if (element.ValueKind != JsonValueKind.String)
            {
                sink.Violation("Expected text.");
                return RuleValue.Null;
            }

            string text = element.GetString()!;
            Check(text, sink);
            return RuleValue.Text(text);
        }

        protected override void WriteValue(Utf8JsonWriter writer, RuleValue value) =>
            writer.WriteStringValue(value.AsText("type.string"));

        private void Check(string text, ISchemaValidationSink sink)
        {
            if (minimumLength is int low && text.Length < low)
            {
                sink.Violation($"Expected at least {low.ToString(CultureInfo.InvariantCulture)} characters "
                    + $"but got {text.Length.ToString(CultureInfo.InvariantCulture)}.");
            }

            if (maximumLength is int high && text.Length > high)
            {
                sink.Violation($"Expected at most {high.ToString(CultureInfo.InvariantCulture)} characters "
                    + $"but got {text.Length.ToString(CultureInfo.InvariantCulture)}.");
            }
        }
    }
}
