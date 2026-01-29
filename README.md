> **Note** This repository is developed for .netstandard2.0+ 

[![NuGet Version](https://img.shields.io/nuget/v/MathExprEngine.svg?style=flat&logo=nuget)](https://www.nuget.org/packages/MathExprEngine/)
[![Nuget Downloads](https://img.shields.io/nuget/dt/MathExprEngine.svg?style=flat&logo=nuget)](https://www.nuget.org/packages/MathExprEngine)

## Overview

**Expression Calculator** is a lightweight, embeddable C# expression engine that parses and evaluates mathematical and logical expressions using a real **Abstract Syntax Tree (AST)**.

It is designed for scenarios where:

* expressions must be **user-defined or configurable**
* correctness of **operator precedence** matters
* **short-circuit logic** is required (logical operators, conditional expressions)
* **clear error diagnostics** (with column numbers) are important

The engine intentionally avoids scripting engines, or `eval`-style execution, giving you **full control, safety, and extensibility**.

---

## What Problems Does It Solve?

Typical real-world problems:

* 🔧 **Business rules** stored as expressions in configuration or database
* 📊 **Dynamic calculations** (pricing formulas, scoring systems, validation rules)
* 🧪 **Feature flags** or conditional behavior without recompilation
* 🧮 **Math domains** (finance, engineering, analytics)
* 🧩 **DSL foundations** for more complex rule engines

This math expression is intentionally **small, explicit, and deterministic**.

---

## Key Features

### Arithmetic

* `+  -  *  /  ^`
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
* `max(x)`
* `sum(x)`
* `sum(x, y, z, ...)`
* `neg(x)`

Custom functions:

* Register your own C# functions at runtime

### Error Reporting

* All syntax and runtime errors include **exact column numbers**
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
* future extensibility (variables, assignments, lambdas)

### Numeric Boolean Model

* `true` → `1.0`
* `false` → `0.0`

This keeps the engine simple and interoperable with math-heavy formulas.

---

## Architecture Overview

```text
Expression String
       ↓
   Tokenizer
       ↓
     Parser
       ↓
   AST Nodes
       ↓
   Evaluation
```

* **Tokenizer**: converts text into tokens with column tracking
* **Parser**: recursive-descent + precedence handling
* **AST Nodes**: executable expression tree
* **Evaluator**: walks the tree with short-circuit semantics

> To get acquainted with a more detailed description, please check the content table at [the first point](docs/usage.md).

No additional components or packs are required for use. So, it only needs to be added/installed in the project and can be used instantly.

**In case you wish to use it in your project, u can install the package from <a href="https://www.nuget.org/packages/MathExprEngine" target="_blank">nuget.org</a>** or specify what version you want:


> `Install-Package MathExprEngine -Version x.x.x.x`

## Content
1. [USING](docs/usage.md)
1. [CHANGELOG](docs/CHANGELOG.md)
1. [BRANCH-GUIDE](docs/branch-guide.md)