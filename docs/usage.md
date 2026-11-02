# Usage Guide

## Installation

Install from NuGet:

```powershell
Install-Package MathExprEngine
```

---

## Basic Usage

```csharp
var calc = new MathRuleEngine();

double result = calc.Evaluate("2 + 3 * (5 - 2)");
// result = 11
```

One engine is thread-safe, so create it once and reuse it.

---

## Resource Limits

```csharp
var calc = new MathRuleEngine(new MathRuleEngineOptions
{
    MaxExpressionLength = 4096, // default (range 1-65536)
    MaxTokens = 2048, // default (range 1-65536)
    MaxNodes = 4096, // default (range 1-65536)
    MaxDepth = 256, // default (range 1-1024)
    MaxEvaluationDepth = 512, // default (range 1-2048)
    MaxFunctionArgs = 64, // default (range 1-4096)
    AllowNonFiniteResults = false, // default
    ExpressionCacheCapacity = 1024, // default (range 0-65536)
    EnableCompilation = true, // default
    CompilationThreshold = 2 // default (range 1-1024)
});
```

```csharp
var bad = new MathRuleEngineOptions { MaxDepth = 0 };
new MathRuleEngine(bad); // throws ArgumentOutOfRangeException
```

Pass `MathRuleEngineOptions` to the constructor to change the limits.

Keep the default limits and `CompilationThreshold` for untrusted text.

---

## Parameters (Variables)

```csharp
var calc = new MathRuleEngine();

double result = calc.Evaluate(
    "basePrice * quantity",
    new Dictionary<string, object>
    {
        ["basePrice"] = 19.99,
        ["quantity"] = 3
    });

// result = 59.97
```

```csharp
var p = new Dictionary<string, object> { ["x"] = "2.5" };
calc.Evaluate("x", p); // 2.5
p["x"] = true;
calc.Evaluate("x", p); // 1
p["x"] = null;
calc.Evaluate("x", p); // throws MathRuleEngineException
```

Parameter names are case-insensitive, so a request key `AGE` can override a trusted `age` in one dictionary.

---

## Arithmetic and Precedence

```csharp
calc.Evaluate("2 + 3 * 4"); // 14
calc.Evaluate("(2 + 3) * 4"); // 20
calc.Evaluate("2 ^ 3 ^ 2"); // 512 (right-associative)
```

`^` binds tighter than unary minus:

```csharp
calc.Evaluate("-2^2"); // -4
calc.Evaluate("2^-3"); // 0.125
```

---

## Modulo and Percentage

```csharp
calc.Evaluate("10 % 3"); // 1 (modulo)
calc.Evaluate("10 % -3"); // 1 (modulo)
calc.Evaluate("50%"); // 0.5 (percentage)
calc.Evaluate("50% * 2"); // 1 (percentage)
calc.Evaluate("100% + 2"); // 0 (modulo)
calc.Evaluate("(100%) + 2"); // 3 (percentage)
```

`%` is modulo when an operand follows it and a percentage otherwise. Write `(100%) + 2` for a percentage.

---

## NaN and Infinity

```csharp
calc.Evaluate("10^400"); // throws MathRuleEngineException

var ieee = new MathRuleEngine(new MathRuleEngineOptions { AllowNonFiniteResults = true });
ieee.Evaluate("10^400"); // Infinity
ieee.Evaluate("sqrt(-1)"); // throws ExpressionFunctionException
ieee.Evaluate("(10^400 - 10^400) ? 1 : 2"); // throws MathRuleEngineException
```

By default a NaN or Infinity result throws. A NaN condition and a negative sqrt still throw with the option set.

---

## Functions

### Built-in Functions

```csharp
calc.Evaluate("sqrt(16)"); // 4
calc.Evaluate("pow(2, 5)"); // 32
calc.Evaluate("sin(3.1415926535/2)");
calc.Evaluate("sum(1, 2, 3, 4)"); // 10
calc.Evaluate("max(3, 11, 5)"); / 11
calc.Evaluate("mod(10, 4)"); // 2
calc.Evaluate("neg(7)"); // -7
```

With no arguments, `sum()` and `max()` throw `ExpressionFunctionException`.

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

Built-in names can be overridden by registering a function with the same name.

The engine has no timeout, so keep custom functions fast.

---

## Comparisons

```csharp
calc.Evaluate("5 > 3"); // 1
calc.Evaluate("5 <= 3"); // 0
calc.Evaluate("5 == 5"); // 1
calc.Evaluate("5 != 5"); // 0
```

---

## Logical Operators (Short-Circuit)

```csharp
calc.Evaluate("1 && 1"); // 1
calc.Evaluate("0 && (1/0)"); // 0 (no exception)
calc.Evaluate("1 || (1/0)"); // 1 (no exception)
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

Parse and evaluation errors derive from `MathExprEngineException`, which has a `Column` property:

```csharp
try
{
    calc.Evaluate("2 + * 3");
}
catch (ExpressionSyntaxException ex)
{
    Console.WriteLine(ex.Message);
}
```

Example output:

```text
Error: Syntax error at column 5. Message: Unexpected token '*'
```

You can catch `ExpressionFunctionException` for function-related runtime errors.

Do not return `ex.Message` to untrusted callers.

---

## Compiled Expressions

```csharp
CompiledExpression price = calc.Compile("basePrice * quantity * (1 - discount)");
var p = new Dictionary<string, object>
    { ["basePrice"] = 20, ["quantity"] = 3, ["discount"] = 0.25 };
price.Evaluate(p); // 45
```

`Evaluate` caches each expression and tries to compile it on its `CompilationThreshold`-th use. `Compile` reports syntax errors at once.

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
