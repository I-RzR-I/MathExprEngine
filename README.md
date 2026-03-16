> Note: This library targets `.NET Standard 2.0+`.

[![NuGet Version](https://img.shields.io/nuget/v/MathExprEngine.svg?style=flat&logo=nuget)](https://www.nuget.org/packages/MathExprEngine/)
[![Nuget Downloads](https://img.shields.io/nuget/dt/MathExprEngine.svg?style=flat&logo=nuget)](https://www.nuget.org/packages/MathExprEngine)

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
* Correct precedence and associativity
* Unary `+` and `-`

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

### Error Reporting

* Syntax and runtime errors include **exact column numbers**
* Examples:

  * unexpected tokens
  * missing parentheses
  * division by zero
  * invalid function usage

---

## Design Principles

The engine parses expressions into an **Abstract Syntax Tree**, not Reverse Polish Notation.

This enables:

* true short-circuit evaluation
* clean implementation of ternary operators
* better error localization
* future extensibility

### Numeric Boolean Model

* `true` → `1.0`
* `false` → `0.0`

This keeps the engine simple and interoperable with math-heavy formulas.

---

## Execution Overview

```text
Expression String
    ↳ Tokenizer
        ↳ Parser
            ↳ AST Nodes     
                ↳ Evaluation
```

* **Tokenizer**: converts text into tokens with column tracking
* **Parser**: recursive-descent + precedence handling
* **AST Nodes**: executable expression tree
* **Evaluator**: walks the tree with short-circuit semantics

> For a detailed walkthrough, see the [Usage Guide](docs/usage.md).

No additional components are required. Install the package and use it directly.

Install from [nuget.org](https://www.nuget.org/packages/MathExprEngine), optionally choosing a specific version:


> `Install-Package MathExprEngine -Version x.x.x.x`

## Content
1. [USING](docs/usage.md)
1. [CHANGELOG](docs/CHANGELOG.md)
1. [BRANCH-GUIDE](docs/branch-guide.md)