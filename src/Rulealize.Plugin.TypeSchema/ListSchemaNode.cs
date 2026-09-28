// Copyright (c) 2026 Reny
// Licensed under the Apache License, Version 2.0.

using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using Rulealize.Abstraction.Building;
using Rulealize.Abstraction.Node;
using Rulealize.Abstraction.Value;

namespace Rulealize.Plugin.TypeSchema
{
    /// <summary>The schema of a field holding an ordered, homogeneous collection.</summary>
    /// <remarks>
    /// <para>
    /// The value is a <see cref="SequenceValue"/>, which is the whole design. A sequence is
    /// already a kind in the value model with a plugin's worth of vocabulary pointed at it,
    /// so a rule set counts a history with <c>seq.count</c> and searches one with
    /// <c>seq.any</c> the day this exists. A list of its own kind would have meant a second
    /// set of operations meaning the same things.
    /// </para>
    /// <para>
    /// That is also why this stays here among the scalars rather than becoming a plugin of
    /// its own, where records had to go: a list needs no vocabulary that does not exist.
    /// Writing one is <c>state.set</c> with an expression that produces a sequence.
    /// </para>
    /// <para>
    /// Bounds are worth declaring. A state is serialized on every transition, so an unbounded
    /// history grows a game's documents with the square of its length. The bound is often the
    /// rule itself rather than a concession — chess needs a position history only as far back
    /// as the last irreversible move, which the fifty-move rule caps at a hundred plies.
    /// </para>
    /// </remarks>
    internal sealed class ListSchemaNode(SchemaNode item, int minLength, int? maxLength) : SchemaNode
    {
        /// <inheritdoc />
        /// <remarks>
        /// A list field always holds a sequence. An empty list is written <c>[]</c>, which
        /// leaves null with nothing to mean here.
        /// </remarks>
        public override bool IsNullable => false;

        public override RecordValue Describe() => Bounds.Of(
            ("minLength", Bounds.Number(minLength)),
            ("maxLength", Bounds.Number(maxLength)));

        public static SchemaNode Build(INodeBuildContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            int minLength = context.OptionalInt32("minLength", 0);
            if (minLength < 0)
            {
                throw context.Error("minLength", $"must not be negative, but is {minLength}.");
            }

            int? maxLength = null;
            if (context.TryGetProperty("maxLength", out _))
            {
                int declared = context.RequireInt32("maxLength");
                if (declared < minLength)
                {
                    throw context.Error(
                        "maxLength",
                        $"must be at least minLength, which is {minLength}, but is {declared}.");
                }

                maxLength = declared;
            }

            return new ListSchemaNode(context.RequireSchema("element"), minLength, maxLength);
        }

        /// <inheritdoc />
        public override void Validate(RuleValue value, ISchemaValidationSink sink)
        {
            ArgumentNullException.ThrowIfNull(value);
            ArgumentNullException.ThrowIfNull(sink);

            if (value is not SequenceValue sequence)
            {
                sink.Violation($"Expected a list but got {RuleValue.Describe(value)}.");
                return;
            }

            int index = 0;
            foreach (RuleValue value_ in sequence)
            {
                item.Validate(value_, new ElementValidationSink(sink, index));
                index++;
            }

            ValidateLength(index, sink);
        }

        /// <inheritdoc />
        public override RuleValue ReadJson(JsonElement element, ISchemaValidationSink sink)
        {
            ArgumentNullException.ThrowIfNull(sink);

            if (element.ValueKind != JsonValueKind.Array)
            {
                sink.Violation("Expected an array.");
                return RuleValue.EmptySequence;
            }

            ImmutableArray<RuleValue>.Builder items = ImmutableArray.CreateBuilder<RuleValue>();
            int index = 0;
            foreach (JsonElement entry in element.EnumerateArray())
            {
                items.Add(item.ReadJson(entry, new ElementValidationSink(sink, index)));
                index++;
            }

            ValidateLength(index, sink);
            return RuleValue.Sequence(items.ToImmutable());
        }

        /// <inheritdoc />
        public override void WriteJson(Utf8JsonWriter writer, RuleValue value)
        {
            ArgumentNullException.ThrowIfNull(writer);
            ArgumentNullException.ThrowIfNull(value);

            writer.WriteStartArray();
            foreach (RuleValue entry in value.AsSequence("type.list"))
            {
                item.WriteJson(writer, entry);
            }

            writer.WriteEndArray();
        }

        /// <inheritdoc />
        /// <remarks>
        /// The reason this schema node needed settling at all. An expression that appends to
        /// a list produces a sequence that may be lazy over the state it read, and storing
        /// that would leave each state holding a way to recompute itself from the one before.
        /// Enumerating it here ends the chain at one.
        /// </remarks>
        public override RuleValue Normalize(RuleValue value)
        {
            ArgumentNullException.ThrowIfNull(value);

            if (value is not SequenceValue sequence)
            {
                return value;
            }

            ImmutableArray<RuleValue> settled = [.. sequence];
            return RuleValue.Sequence(settled);
        }

        private void ValidateLength(int length, ISchemaValidationSink sink)
        {
            if (length < minLength)
            {
                sink.Violation(
                    string.Create(CultureInfo.InvariantCulture, $"Expected at least {minLength} items but got {length}."));
            }

            if (maxLength is int limit && length > limit)
            {
                sink.Violation(
                    string.Create(CultureInfo.InvariantCulture, $"Expected at most {limit} items but got {length}."));
            }
        }
    }

    /// <summary>Reports an element's violations under the position they were found at.</summary>
    internal sealed class ElementValidationSink(ISchemaValidationSink inner, int index) : ISchemaValidationSink
    {
        public bool HasViolations => inner.HasViolations;

        public void Violation(string message) => inner.Violation(Position, message);

        public void Violation(string relativePath, string message) =>
            inner.Violation($"{Position}/{relativePath}", message);

        private string Position => string.Create(CultureInfo.InvariantCulture, $"[{index}]");
    }
}
