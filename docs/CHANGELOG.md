### **v2.0.0.0** [[RzR](mailto:108324929+I-RzR-I@users.noreply.github.com)] 03-11-2026
* (RzR) -> [#8] - Remove `MathRuleEngine.Instance`; create an engine with `new MathRuleEngine()`.
* (RzR) -> [#8] - Change `^` to bind tighter than unary minus: `-2^2` = -4.
* (RzR) -> [#8] - Change `%` followed by + or - to modulo: `100% + 2` = 0.
* (RzR) -> [#8] - Throw on NaN/Infinity results, null parameter values and sum() without arguments.
* (RzR) -> [#8] - Make ExpressionToken, TokenKind internal; drop FunctionArgumentException(string).
* (RzR) -> [#8] - Add `MathRuleEngineOptions` with resource limits and `AllowNonFiniteResults`.
* (RzR) -> [#8] - Add `Compile()`, `CompiledExpression` and a per-engine expression cache.
* (RzR) -> [#8] - Replace `DomainCommonExtensions` with `RzR.Extensions.Domain` 7.1.0.5494.

### **v1.3.0.8086** [[RzR](mailto:108324929+I-RzR-I@users.noreply.github.com)] 16-03-2026
* [7cec471] (RzR) -> Auto commit uncommited files
* [61ed47d] (RzR) -> Adjust the readme and using doc
* [8ba81a8] (RzR) -> [#6] - Add the `%`(modulo) evaluate support.
* [5e3264b] (RzR) -> [#5] - Add the `%`(percentage) evaluate support.

### **v1.2.0.6785** [[RzR](mailto:108324929+I-RzR-I@users.noreply.github.com)] 17-02-2026
* [009ab18] (RzR) -> [#3] - Add parameters (variables) in expression evaluation.

### **vv1.2.0.6785** [[RzR](mailto:108324929+I-RzR-I@users.noreply.github.com)] 17-02-2026
* [db8c8f1] (RzR) -> Auto commit uncommited files
* [d40691c] (RzR) -> Upgrade ref package version.
* [94d0eeb] (RzR) -> Auto commit uncommited files
* [009ab18] (RzR) -> [#3] - Add parameters (variables) in expression evaluation.

### **v1.1.1.4273** [[RzR](mailto:108324929+I-RzR-I@users.noreply.github.com)] 10-02-2026
* [3958643] (RzR) -> Auto commit uncommited files
* [2e56d9d] (RzR) -> Fix the message format in `ExpressionFunctionException`.

### **v1.1.0.4548** [[RzR](mailto:108324929+I-RzR-I@users.noreply.github.com)] 03-02-2026
* [044e479] (RzR) -> [DEV] - Add new explicit expcetions and tests.

### **v1.0.0.0** [[RzR](mailto:108324929+I-RzR-I@users.noreply.github.com)] 29-01-2026
* (RzR) -> [DEV] - Add init package version.
