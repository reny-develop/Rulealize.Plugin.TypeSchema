# Rulealize.Plugin.TypeSchema

The scalar vocabulary a [Rulealize](https://github.com/reny-develop/Rulealize) rule set's
`state.schema` is written in.

| | |
| --- | --- |
| Plugin id | `Rulealize.Plugin.TypeSchema` |
| Namespace | `type` |
| Reserved prefix | none |
| Depends on | `Rulealize.Abstraction` |
| Specification | [doc/specification.md](doc/specification.md) |

`type.enum`, `type.int`, `type.bool`, `type.string`, `type.list`. Everything here is a
**schema node**, not an expression: schema nodes appear inside `state.schema` and nowhere
else, and are never evaluated. Using one in expression position is a build error.

Composite types belong to whoever owns them. A grid plugin supplies the board and accepts
any schema node for its cells; that this plugin's enumeration is what usually lands there is
a fact about rule sets, not a dependency between the two.

A rule set runs from `state.initial` alone, so the schema has to earn its place. It does
that by making three other things possible: checking a state document that arrived from
outside, checking the initial state while the rule set is built rather than when it runs,
and type inference eventually — exhaustiveness for `branch.match` and kind mismatch for
`cmp` both need it, and neither exists yet.

`nullable` is a shared modifier rather than a variant per scalar, because the shape a board
cell needs — `black`, `white`, or empty — then fits on one line. An empty square being null
rather than a third enumeration member is a deliberate decision the specification argues
for; it is what keeps Reversi's flip rule to a single comparison.

## Building

`Rulealize.Abstraction` is not on nuget.org yet, so `NuGet.config` points at a folder
feed. Produce it from the abstraction repository first:

```
dotnet pack path\to\Rulealize.Abstraction\src\Rulealize.Abstraction -c Release -o path\to\LocalNuGet
```

with `LocalNuGet` a sibling of this repository. Then `dotnet build`.

## License

Apache-2.0.
