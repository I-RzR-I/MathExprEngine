> Note: This library targets `.NET Standard 2.0+`.

[![NuGet Version](https://img.shields.io/nuget/v/MathExprEngine.svg?style=flat&logo=nuget)](https://www.nuget.org/packages/MathExprEngine/)
[![Nuget Downloads](https://img.shields.io/nuget/dt/MathExprEngine.svg?style=flat&logo=nuget)](https://www.nuget.org/packages/MathExprEngine)

> **Upgrading from v1.x?** v2.0.0 has breaking changes, including two that change expression *results* silently (`^` vs unary minus, `%` disambiguation). Read [`docs/CHANGELOG.md`](docs/CHANGELOG.md) before upgrading a production rule set.

## Overview

`MathExprEngine` is a lightweight, embeddable C# expression engine that parses and evaluates mathematical and logical expressions using an **Abstract Syntax Tree (AST)**.

It is designed for scenarios where:

* expressions must be **user-defined or configurable**
* correctness of **operator precedence** matters
* **short-circuit logic** is required (logical operators, conditional expressions)
* **clear error diagnostics** (with column numbers) are important

The engine intentionally avoids scripting engines and `eval`-style execution, giving you **control, safety, and extensibility**.

---

## What Problems Does It Solve?

Typical use cases:

* **Business rules** stored as expressions in configuration or databases
* **Dynamic calculations** (pricing formulas, scoring systems, validation rules)
* **Feature flags** or conditional behavior without recompilation
* **Math-heavy domains** (finance, engineering, analytics)
* **DSL foundations** for more complex rule engines

The engine is intentionally **small, explicit, and deterministic**.

---

## Key Features

### Arithmetic

* `+  -  *  /  ^  %`
* Correct precedence and associativity-> `^` binds tighter than unary minus (`-2^2` is `-4`, not `4`) and is right-associative (`2^3^2` is `512`)
* Unary `+` and `-`

> **`%` has two meanings, decided per use.** As an infix operator it is modulo: `10 % 3` is `1`, and `10 % -3` is `1` (not `-2.9`). As a postfix operator, when nothing that could start a new operand follows it -> it is percentage: `50%` is `0.5`. This means `100% + 2` is `0` (modulo), not `3`; write `(100%) + 2` if you want the old percentage-then-add reading. See [Usage Guide -> The `%` Operator](docs/usage.md#the--operator-modulo-vs-percentage) for the full rule and worked examples.

### Comparisons

* `<  >  <=  >=  ==  !=`
* Return numeric booleans (`1.0` = true, `0.0` = false)

### Logical Operators (Short-Circuit)

* `&&` (AND)
* `||` (OR)

Only the necessary side of the expression is evaluated.

### Conditional (Ternary)

```text
condition ? exprIfTrue : exprIfFalse
```

* Fully short-circuited
* Only the selected branch is evaluated

### Functions

Built-in:

* `sin(x)`
* `cos(x)`
* `sqrt(x)`
* `pow(x, y)`
* `max(x1, x2, ...)`
* `sum(x1, x2, ...)`
* `neg(x)`
* `mod(x, y)`

Custom functions:

* Register your own C# functions at runtime

`sum()` and `max()` called with zero arguments throw rather than silently returning `0`.

### Parameters (Variables)

* Bare names in an expression (e.g. `basePrice`, `quantity`) are resolved from a `parameters` dictionary passed to `Evaluate`
* Lookup is case-insensitive
* Accepted value types: all numeric primitives, `decimal`, `bool`, and `string` (parsed with `InvariantCulture`)
* `null` and unconvertible types (e.g. `DateTime`) throw rather than silently becoming `0`

See [Usage Guide => Parameters](docs/usage.md#parameters-variables) for the full type table.

### Compiled Expressions

* `engine.Compile(expression)` parses and compiles a rule once and returns a reusable, thread-safe `CompiledExpression`; call `Evaluate(parameters)` on it as often as you like
* Plain `Evaluate` calls get faster on their own: each engine keeps a bounded cache of parsed expressions, and by default the second `Evaluate` of the same text compiles it to a delegate
* Results, exception types, messages and columns are identical whether an expression is interpreted or compiled
* Functions are looked up when the expression runs, so `RegisterFunction` replacing a function after `Compile` takes effect immediately
* Platforms without runtime code generation (Native AOT, iOS) keep interpreting, with the same results

```csharp
var engine = new MathRuleEngine();
CompiledExpression rule = engine.Compile("basePrice * quantity * (1 - discount)");

double total = rule.Evaluate(new Dictionary<string, object>
{
    ["basePrice"] = 20, ["quantity"] = 3, ["discount"] = 0.25
});
// total = 45
```

See [Usage Guide => Compiled Expressions and the Expression Cache](docs/usage.md#compiled-expressions-and-the-expression-cache) for the cache options, when compilation is skipped, and how to turn it off.

### Error Reporting

* Syntax and runtime errors include **exact column numbers**
* All exceptions derive from one common base, `MathExprEngineException`, so a single `catch` handles every engine failure
* Examples:

  * unexpected tokens
  * missing parentheses
  * division/modulo by zero
  * invalid function usage
  * a configured resource limit (expression length, tokens, nodes, nesting depth) exceeded
  * a non-finite result (`NaN` / `Infinity`), rejected instead of returned
  * the thread stack running low during parsing or evaluation, reported as a catchable exception instead of crashing the process

---

## Design Principles

The engine parses expressions into an **Abstract Syntax Tree**, not Reverse Polish Notation.

This enables:

* true short-circuit evaluation
* clean implementation of ternary operators
* better error localization
* future extensibility

### Numeric Boolean Model

* `true` -> `1.0`
* `false` -> `0.0`

This keeps the engine simple and interoperable with math-heavy formulas.

### Thread Safety and Resource Limits

A `MathRuleEngine` instance holds no per-call state -> construct one and share it, including across threads and while registering additional functions. Each `Evaluate` call runs against its own isolated copy of the parameters you pass in. The engine's expression cache is shared by all its callers; it stores only parsed and compiled expressions, never parameter values, results or exceptions. Use one engine per trust domain if tenants must not affect each other's cache.

Construction accepts an optional `MathRuleEngineOptions` with resource limits (expression length, token count, node count, nesting depth, evaluation depth, function-argument count) so that a hostile or accidental runaway expression throws instead of exhausting memory or the call stack. Defaults are generous for hand-authored rules; see [Usage Guide -> Resource Limits](docs/usage.md#resource-limits-mathruleengineoptions) for every default and what it protects against.

---

## Execution Overview

```text
Expression String
    ↳ Expression cache (per engine, keyed by exact text)
        ↳ on a miss: Tokenizer
            ↳ Parser
                ↳ AST Nodes
        ↳ Evaluation
            ↳ interpreted (walk the AST), or
            ↳ compiled delegate (once the text is hot, or after Compile)
```

* **Expression cache**: keeps the parsed tree (and, once compiled, the delegate) for recently used texts
* **Tokenizer**: converts text into tokens with column tracking
* **Parser**: recursive-descent + precedence handling
* **AST Nodes**: executable expression tree
* **Evaluator**: walks the tree with short-circuit semantics
* **Compiler**: turns the tree into a delegate with `System.Linq.Expressions`, preserving the same short-circuit rules, checks and error messages

> For a detailed walkthrough, see the [Usage Guide](docs/usage.md).

Install from [nuget.org](https://www.nuget.org/packages/MathExprEngine), optionally choosing a specific version:

> `Install-Package MathExprEngine -Version x.x.x.x`

---

## Content
1. [USING](docs/usage.md)
2. [CHANGELOG](docs/CHANGELOG.md)
3. [BRANCH-GUIDE](docs/branch-guide.md)