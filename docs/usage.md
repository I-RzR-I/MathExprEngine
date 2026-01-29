# USING GUIDE

## Installation

The math expression is a single self-contained class. Add it directly to your project or package it as a library.

No external dependencies.

---

## Basic Usage

```csharp
var calc = new MathRuleEngine();

double result = calc.Evaluate("2 + 3 * (5 - 2)");
// result = 11
```

---

## Arithmetic and Precedence

```csharp
calc.Evaluate("2 + 3 * 4");     // 14
calc.Evaluate("(2 + 3) * 4");   // 20
calc.Evaluate("2 ^ 3 ^ 2");     // 512 (right-associative)
```

---

## Functions

### Built-in Functions

```csharp
calc.Evaluate("sqrt(16)");          // 4
calc.Evaluate("pow(2, 5)");         // 32
calc.Evaluate("sin(3.1415926535/2)");
```

### Custom Functions

```csharp
calc.RegisterFunction("max", args =>
{
    if (args.Length != 2)
        throw new ArgumentException("max expects 2 arguments");

    return Math.Max(args[0], args[1]);
});

calc.Evaluate("max(10, 7)"); // 10
```

---

## Comparisons

```csharp
calc.Evaluate("5 > 3");   // 1
calc.Evaluate("5 <= 3");  // 0
calc.Evaluate("5 == 5");  // 1
calc.Evaluate("5 != 5");  // 0
```

---

## Logical Operators (Short-Circuit)

```csharp
calc.Evaluate("1 && 1");        // 1
calc.Evaluate("0 && (1/0)");    // 0 (no exception)
calc.Evaluate("1 || (1/0)");    // 1 (no exception)
```

Only the required branch is evaluated.

---

## Conditional (Ternary)

```csharp
calc.Evaluate("1 ? 10 : 20");
calc.Evaluate("0 ? 10 : 20");
```

Short-circuiting example:

```csharp
calc.Evaluate("1 ? 10 : (1/0)"); // 10
calc.Evaluate("0 ? (1/0) : 10"); // 10
```

---

## Error Handling

All errors include column numbers:

```csharp
try
{
    calc.Evaluate("2 + * 3");
}
catch (Calculator.CalculatorException ex)
{
    Console.WriteLine(ex.Message);
}
```

Example output:

```text
Error: Unexpected token '*' at column 5
```

---

## Real-Life Use Cases

### Configuration-Based Rules

```text
(score > 90) ? 1.5 : 1.0
```

### Pricing Formula

```text
basePrice * (isVip && quantity > 10 ? 0.8 : 1.0)
```

### Validation Logic

```text
(age >= 18 && country == 1) ? 1 : 0
```

### Feature Toggles

```text
(userGroup > 3 && betaEnabled) ? 1 : 0
```

---

## Why This Math expression parses Is Helpful

* ✅ Deterministic and safe
* ✅ No runtime code execution
* ✅ Clear error diagnostics
* ✅ Extensible function system
* ✅ Short-circuit semantics
* ✅ Ideal base for a DSL or rule engine