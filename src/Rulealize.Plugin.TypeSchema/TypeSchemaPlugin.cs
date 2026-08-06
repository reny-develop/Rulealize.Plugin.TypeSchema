// Copyright (c) 2026 Reny
// Licensed under the Apache License, Version 2.0.

using Rulealize.Abstraction.Plugins;

namespace Rulealize.Plugin.TypeSchema
{
    /// <summary>
    /// Scalar schema vocabulary over the <c>type</c> namespace.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Everything here is a schema node, not an expression. Schema nodes appear inside
    /// <c>state.schema</c> and nowhere else, and are never evaluated.
    /// </para>
    /// <para>
    /// Composite types are somebody else's. A grid plugin supplies the board and takes any
    /// schema node for its cells; that this plugin's enumeration is what usually lands
    /// there is a fact about rule sets, not a dependency between the two.
    /// </para>
    /// </remarks>
    public sealed class TypeSchemaPlugin : IRulealizePlugin
    {
        /// <inheritdoc />
        public PluginManifest Manifest { get; } =
            new("Rulealize.Plugin.TypeSchema", new Version(1, 0, 0), "type");

        /// <inheritdoc />
        public void Register(IPluginRegistry registry)
        {
            ArgumentNullException.ThrowIfNull(registry);

            registry.AddSchema("enum", EnumSchemaNode.Build);
            registry.AddSchema("int", IntegerSchemaNode.Build);
            registry.AddSchema("bool", BooleanSchemaNode.Build);
            registry.AddSchema("string", StringSchemaNode.Build);
        }
    }
}
