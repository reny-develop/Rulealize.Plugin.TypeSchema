// Copyright (c) 2026 Reny
// Licensed under the Apache License, Version 2.0.

using System.Text.Json;
using Rulealize.Abstraction;
using Rulealize.Abstraction.Value;
using Rulealize.Plugin.Binding;
using Rulealize.Plugin.State;

namespace Rulealize.Plugin.TypeSchema.Tests;

/// <summary>An amount: a number with a fixed most number of decimal places, as <c>type.decimal</c> holds it.</summary>
/// <remarks>
/// The value model's number was decimal all along, so what is pinned here is the constraint and
/// nothing else: too many decimal places is refused rather than rounded, and the bounds a host
/// builds its field from are the ones the state is checked against. Each is a rule set the
/// Rulealize runtime builds and moves, as a host meets the schema node.
/// </remarks>
public sealed class DecimalSchemaTests
{
    private const string Amount = """{ "op": "type.decimal", "scale": 2, "min": 0, "max": 1000 }""";

    /// <summary>One amount, paid in by a value somebody types.</summary>
    private const string Wallet = $$"""
        {
          "id": "wallet", "version": "1.0.0",
          "requires": [
            { "plugin": "Rulealize.Plugin.TypeSchema" }, { "plugin": "Rulealize.Plugin.State" }, { "plugin": "Rulealize.Plugin.Binding" }
          ],
          "state": { "schema": { "amount": {{Amount}} }, "initial": { "amount": 0 } },
          "inputs": {
            "pay": {
              "params": { "to": { "open": { "field": "amount" } } },
              "effects": [ { "op": "state.set", "path": "amount", "value": "@to" } ]
            }
          }
        }
        """;

    [Theory]
    [InlineData("12.5")]
    [InlineData("12.50")]
    [InlineData("0")]
    [InlineData("1000")]
    public void AnAmountWithinItsScaleAndBoundsIsTaken(string amount) =>
        Assert.Equal(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), Set(amount));

    [Theory]
    [InlineData("12.505", "Expected at most 2 decimal places but got 12.505.")]
    [InlineData("1000.01", "Expected at most 1000 but got 1000.01.")]
    [InlineData("-0.01", "Expected at least 0 but got -0.01.")]
    [InlineData("\"12\"", "Expected a number but got")]
    public void WhatTheEffectsBuiltIsHeldToTheScaleAndTheBounds(string amount, string said) =>
        Assert.Contains(said, Assert.ThrowsAny<Exception>(() => Set(amount)).Message, StringComparison.Ordinal);

    [Fact]
    public void TheInitialStateIsHeldToItTooWhenTheRuleSetIsBuilt() =>
        Assert.Contains(
            "Expected at most 2 decimal places but got 0.001.",
            Assert.Throws<RuleSetBuildException>(() => Runtime().CreateContext(Holding(Amount, "0.001"))).Message,
            StringComparison.Ordinal);

    [Fact]
    public void AValueWithTooManyDecimalPlacesIsRefusedRatherThanRounded()
    {
        // Rounding is a rule — which way is the domain's to say — so the schema does not do it.
        IllegalInputException refused = Assert.Throws<IllegalInputException>(() => Paid("12.505"));

        Assert.Equal("to: Expected at most 2 decimal places but got 12.505.", refused.Message);
    }

    [Fact]
    public void TheScaleAndTheBoundsComeOutAsDataForWhoeverHasToAskForTheValue()
    {
        RuleContext context = Runtime().CreateContext(Wallet);
        OpenParameter to = Assert.Single(Assert.Single(context.GetValidInputs(context.InitialState, 10)).Open);

        Assert.Equal(2m, Number(to.Description["scale"]));
        Assert.Equal(0m, Number(to.Description["min"]));
        Assert.Equal(1000m, Number(to.Description["max"]));
    }

    [Fact]
    public void ABoundNobodyDeclaredIsAbsentRatherThanNull()
    {
        RuleContext context = Runtime().CreateContext(Wallet.Replace(Amount, """{ "op": "type.decimal" }""", StringComparison.Ordinal));

        Assert.Empty(Assert.Single(Assert.Single(context.GetValidInputs(context.InitialState, 10)).Open).Description.Fields);
    }

    [Theory]
    [InlineData("\"scale\": 29", "must be from 0 to 28")]
    [InlineData("\"scale\": -1", "must be from 0 to 28")]
    [InlineData("\"scale\": 2, \"min\": 0.005", "has more than 2 decimal places")]
    [InlineData("\"min\": 10.5, \"max\": 2", "min 10.5 is greater than max 2")]
    [InlineData("\"max\": \"ten\"", "must be a number")]
    public void ADeclarationNoValueCouldSatisfyIsRefusedWhenTheRuleSetIsBuilt(string bounds, string said) =>
        Assert.Contains(
            said,
            Assert.Throws<RuleSetBuildException>(() => Runtime().CreateContext(Holding($$"""{ "op": "type.decimal", "nullable": true, {{bounds}} }""", "null"))).Message,
            StringComparison.Ordinal);

    [Fact]
    public void ANullableAmountMayBeNull() =>
        Assert.Equal(JsonValueKind.Null, Field(Set("""{ "op": "type.decimal", "scale": 2, "nullable": true }""", "1.5", "null")).ValueKind);

    private static decimal Set(string amount) => Field(Set(Amount, "0", amount)).GetDecimal();

    /// <summary>The state after an input whose one effect writes a value into a field of a schema.</summary>
    private static string Set(string schema, string initial, string value)
    {
        RuleContext context = Runtime().CreateContext(Holding(schema, initial, value));
        return context.ApplyToState("""{ "input": "set", "args": {} }""", context.InitialState).State;
    }

    private static JsonElement Field(string state)
    {
        using JsonDocument document = JsonDocument.Parse(state);
        return document.RootElement.GetProperty("data").GetProperty("amount").Clone();
    }

    /// <summary>A rule set with one field of a schema, and an input that writes a value into it.</summary>
    private static string Holding(string schema, string initial, string value = "null") => $$"""
        {
          "id": "holding", "version": "1.0.0",
          "requires": [ { "plugin": "Rulealize.Plugin.TypeSchema" }, { "plugin": "Rulealize.Plugin.State" } ],
          "state": { "schema": { "amount": {{schema}} }, "initial": { "amount": {{initial}} } },
          "inputs": { "set": { "effects": [ { "op": "state.set", "path": "amount", "value": {{value}} } ] } }
        }
        """;

    private static decimal Paid(string amount)
    {
        RuleContext context = Runtime().CreateContext(Wallet);
        string state = context.ApplyToState($$"""{ "input": "pay", "args": { "to": {{amount}} } }""", context.InitialState).State;

        using JsonDocument document = JsonDocument.Parse(state);
        return document.RootElement.GetProperty("data").GetProperty("amount").GetDecimal();
    }

    private static RuleRuntime Runtime() =>
        new RuleRuntime().AddPlugin(new TypeSchemaPlugin()).AddPlugin(new StatePlugin()).AddPlugin(new BindingPlugin());

    private static decimal? Number(RuleValue value) => value is NumberValue number ? number.Value : null;
}
