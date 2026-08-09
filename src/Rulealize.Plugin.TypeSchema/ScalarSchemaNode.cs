// Copyright (c) 2026 Reny
// Licensed under the Apache License, Version 2.0.

using System.Text.Json;
using Rulealize.Abstraction.Node;
using Rulealize.Abstraction.Value;

namespace Rulealize.Plugin.TypeSchema
{
    /// <summary>The part every scalar schema shares: whether null is allowed.</summary>
    /// <remarks>
    /// Nullability is a modifier rather than a separate type per scalar, so that the
    /// pattern a board cell needs — one of these values, or empty — is a single line:
    /// <c>{ "op": "type.enum", "values": ["black", "white"], "nullable": true }</c>.
    /// </remarks>
    internal abstract class ScalarSchemaNode(bool nullable) : SchemaNode
    {
        public sealed override bool IsNullable => nullable;

        public sealed override void Validate(RuleValue value, ISchemaValidationSink sink)
        {
            ArgumentNullException.ThrowIfNull(value);
            ArgumentNullException.ThrowIfNull(sink);

            if (value.IsNull)
            {
                if (!nullable)
                {
                    sink.Violation("Null is not allowed here.");
                }

                return;
            }

            ValidateValue(value, sink);
        }

        public sealed override RuleValue ReadJson(JsonElement element, ISchemaValidationSink sink)
        {
            ArgumentNullException.ThrowIfNull(sink);

            if (element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                if (!nullable)
                {
                    sink.Violation("Null is not allowed here.");
                }

                return RuleValue.Null;
            }

            return ReadValue(element, sink);
        }

        public sealed override void WriteJson(Utf8JsonWriter writer, RuleValue value)
        {
            ArgumentNullException.ThrowIfNull(writer);
            ArgumentNullException.ThrowIfNull(value);

            if (value.IsNull)
            {
                writer.WriteNullValue();
                return;
            }

            WriteValue(writer, value);
        }

        /// <summary>Checks a value that is known not to be null.</summary>
        /// <param name="value">The value.</param>
        /// <param name="sink">Receives every violation found, not just the first.</param>
        protected abstract void ValidateValue(RuleValue value, ISchemaValidationSink sink);

        /// <summary>Reads a JSON value that is known not to be null.</summary>
        /// <param name="element">The JSON.</param>
        /// <param name="sink">Receives a description of anything malformed.</param>
        /// <returns>The value, best-effort when the JSON is wrong.</returns>
        protected abstract RuleValue ReadValue(JsonElement element, ISchemaValidationSink sink);

        /// <summary>Writes a value that is known not to be null.</summary>
        /// <param name="writer">The writer.</param>
        /// <param name="value">The value.</param>
        protected abstract void WriteValue(Utf8JsonWriter writer, RuleValue value);
    }
}
