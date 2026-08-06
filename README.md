# Rulealize.Plugin.TypeSchema

The scalar vocabulary a [Rulealize](https://github.com/reny-develop/Rulealize) rule set's
`state.schema` is written in.

| | |
| --- | --- |
| Plugin id | `Rulealize.Plugin.TypeSchema` |
| Namespace | `type` |
| Reserved prefix | none |
| Depends on | `Rulealize.Abstraction` |

Everything here is a **schema node**, not an expression. Schema nodes appear inside
`state.schema` and nowhere else, and are never evaluated. Using one in expression position
is a build error.

Composite types belong to whoever owns them. A grid plugin supplies the board and accepts
any schema node for its cells; that this plugin's enumeration is what usually lands there
is a fact about rule sets, not a dependency between the two.

## Operations

```jsonc
{ "op": "type.enum",   "values": ["<value>", …], "nullable": <bool> }
{ "op": "type.int",    "min": <int>, "max": <int>, "nullable": <bool> }
{ "op": "type.bool",   "nullable": <bool> }
{ "op": "type.string", "minLength": <int>, "maxLength": <int>, "nullable": <bool> }
```

Everything but `values` is optional. `nullable` defaults to false and is shared by all four
— a modifier rather than a separate type per scalar, so that the pattern a board cell needs
is one line:

```jsonc
{ "op": "type.enum", "values": ["black", "white"], "nullable": true }
```

## What the schema is for

A rule set runs from `state.initial` alone. The schema earns its place by making three
other things possible:

1. **Validating state that came from outside.** `ApplyToStateAsync` accepts a document
   written by hand or by another system. Without a schema there is nothing to check it
   against.
2. **Validating the initial state**, when the rule set is built rather than when it runs.
3. **Type inference, eventually.** Exhaustiveness checking for `branch.match` and
   kind-mismatch detection for `cmp` both need it. Neither exists yet.

Failures are reported by path, value and constraint, all of them, rather than stopping at
the first.

## Null, and why an Othello cell is nullable

An empty square is null. Adding a third enumeration member for "empty" was considered and
rejected.

`grid.at` answers null for a coordinate off the board, whatever the cell type says. With a
distinct empty member, "no stone here" and "there is no here" would stop agreeing, and the
flip rule — which walks a ray until it stops finding opposing stones, and reads one square
past the end — would need an explicit bounds check to tell them apart. Collapsing both into
null is what keeps that rule to a single comparison.

## Redundancy with the transition rules

Othello's pass counter is declared `{ "op": "type.int", "min": 0, "max": 2 }`, and its
terminal condition also says two passes ends the game. The same number, twice.

They are not the same statement. The terminal condition is a rule about transitions: at
two, stop. The schema is a claim about the state space: a state with three does not exist.
The second is what catches a state document arriving from outside with `"passes": 5`.

Nothing currently checks that the two agree with each other.

## Building

`Rulealize.Abstraction` is not on nuget.org yet, so `NuGet.config` points at a folder
feed. Produce it from the abstraction repository first:

```
dotnet pack path\to\Rulealize.Abstraction\src\Rulealize.Abstraction -c Release -o path\to\LocalNuGet
```

with `LocalNuGet` a sibling of this repository. Then `dotnet build`.

## License

Apache-2.0.
