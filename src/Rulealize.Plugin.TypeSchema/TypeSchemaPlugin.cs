// Copyright (c) 2026 Reny
// Licensed under the Apache License, Version 2.0.

using Rulealize.Abstraction.Plugin;

namespace Rulealize.Plugin.TypeSchema
{
    /// <summary>
    /// Schema vocabulary over the <c>type</c> namespace.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Everything here is a schema node, not an expression. Schema nodes appear inside
    /// <c>state.schema</c> and nowhere else, and are never evaluated. That is the line this
    /// plugin keeps, rather than a line between scalars and everything else: <c>type.list</c>
    /// describes a collection and still belongs here, because the value it describes is a
    /// sequence and the vocabulary for reading a sequence already exists elsewhere.
    /// </para>
    /// <para>
    /// A record is the other way round and lives in a plugin of its own, since nothing in
    /// the standard set can read one.
    /// </para>
    /// <para>
    /// The rest of the composite types are somebody else's. A grid plugin supplies the board
    /// and takes any schema node for its cells; that this plugin's enumeration is what
    /// usually lands there is a fact about rule sets, not a dependency between the two.
    /// </para>
    /// </remarks>
    public sealed class TypeSchemaPlugin : IRulealizePlugin
    {
        /// <inheritdoc />
        public PluginManifest Manifest { get; } =
            new("Rulealize.Plugin.TypeSchema", new Version(1, 1, 0), "type");

        /// <inheritdoc />
        public void Register(IPluginRegistry registry)
        {
            ArgumentNullException.ThrowIfNull(registry);

            registry.AddSchema("enum", EnumSchemaNode.Build);
            registry.AddSchema("int", IntegerSchemaNode.Build);
            registry.AddSchema("bool", BooleanSchemaNode.Build);
            registry.AddSchema("string", StringSchemaNode.Build);
            registry.AddSchema("list", ListSchemaNode.Build);
        }
    }
}
