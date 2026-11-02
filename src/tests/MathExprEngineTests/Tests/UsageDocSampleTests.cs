using System.Collections.Generic;
using MathExprEngine;
using MathExprEngine.Enums;
using MathExprEngine.Exceptions;
using MathExprEngineTests.Helpers.Assertions;
using MathExprEngineTests.Helpers.Engines;
using MathExprEngineTests.Helpers.Inputs;
using MathExprEngineTests.Helpers.TestBases;

namespace MathExprEngineTests.Tests
{
    [TestClass]
    public class UsageDocSampleTests : DepthGuardedTestBase
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void S1_UsageNaNAndInfinity_AllowNonFinite_RightOperandNaN_ThrowsAtOperatorColumn_Test()
        {
            var calc = new MathRuleEngine(new MathRuleEngineOptions { AllowNonFiniteResults = true });

            var ex = Assert.ThrowsException<MathRuleEngineException>(
                () => calc.Evaluate("1 && x", new Dictionary<string, object> { ["x"] = double.NaN }));

            Assert.AreEqual(2, ex.Column, "The '&&' operator column.");
        }

        [TestMethod]
        public void S1_UsageNaNAndInfinity_AllowNonFinite_ShortCircuit_ReturnsZero_Test()
        {
            var calc = new MathRuleEngine(new MathRuleEngineOptions { AllowNonFiniteResults = true });

            Assert.AreEqual(0.0, calc.Evaluate("0 && x", new Dictionary<string, object> { ["x"] = double.NaN }));
        }

        [TestMethod]
        public void S1_UsageNaNAndInfinity_DefaultOptions_RightOperandNaN_ThrowsAtOperandColumn_Test()
        {
            var calc = new MathRuleEngine();

            var ex = Assert.ThrowsException<MathRuleEngineException>(
                () => calc.Evaluate("1 && x", new Dictionary<string, object> { ["x"] = double.NaN }));

            Assert.AreEqual(5, ex.Column, "The parameter's own column (EnsureFinite precedes the condition check).");
        }

        [TestMethod]
        public void S2_UsageCompiledExpressions_CompileOnceEvaluateMany_Test()
        {
            var engine = new MathRuleEngine();

            CompiledExpression price = engine.Compile("basePrice * quantity * (1 - discount)");

            Assert.AreEqual(45.0, price.Evaluate(new Dictionary<string, object>
            {
                ["basePrice"] = 20,
                ["quantity"] = 3,
                ["discount"] = 0.25
            }));

            Assert.AreEqual(50.0, price.Evaluate(new Dictionary<string, object>
            {
                ["basePrice"] = 10,
                ["quantity"] = 5,
                ["discount"] = 0
            }));
        }

        [TestMethod]
        public void S2_UsageCompiledExpressions_ExpressionAndToStringReturnText_Test()
        {
            var engine = new MathRuleEngine();

            CompiledExpression price = engine.Compile("basePrice * quantity * (1 - discount)");

            Assert.AreEqual("basePrice * quantity * (1 - discount)", price.Expression);
            Assert.AreEqual("basePrice * quantity * (1 - discount)", price.ToString());
        }

        [TestMethod]
        public void S7_ReadmeKeyFeatures_CompiledExpressions_Total45_Test()
        {
            var engine = new MathRuleEngine();
            CompiledExpression rule = engine.Compile("basePrice * quantity * (1 - discount)");

            double total = rule.Evaluate(new Dictionary<string, object>
            {
                ["basePrice"] = 20, ["quantity"] = 3, ["discount"] = 0.25
            });

            Assert.AreEqual(45.0, total);
        }

        [TestMethod]
        public void S3_UsageCompiledExpressions_LateBinding_ReplacementUsedWithoutRecompile_Test()
        {
            var engine = new MathRuleEngine();
            engine.RegisterFunction("bonus", args => args[0] / 10);

            CompiledExpression pay = engine.Compile("salary + bonus(salary)");
            var input = new Dictionary<string, object> { ["salary"] = 1000 };

            Assert.AreEqual(1100.0, pay.Evaluate(input));

            engine.RegisterFunction("bonus", args => args[0] / 5);
            Assert.AreEqual(1200.0, pay.Evaluate(input));
        }

        [TestMethod]
        public void S4_UsageCompiledExpressions_ErrorsStayLazy_DivisionByZeroAtEvaluate_Test()
        {
            var engine = new MathRuleEngine();

            CompiledExpression ratio = engine.Compile("1 / x");
            Assert.AreEqual(0.25, ratio.Evaluate(new Dictionary<string, object> { ["x"] = 4 }));

            var ex = Assert.ThrowsException<MathRuleEngineException>(
                () => ratio.Evaluate(new Dictionary<string, object> { ["x"] = 0 }));
            StringAssert.Contains(ex.Message, "Division by zero");
        }

        [TestMethod]
        public void S4_UsageCompiledExpressions_ErrorsStayLazy_UnknownFunctionAtEvaluate_Test()
        {
            var engine = new MathRuleEngine();

            CompiledExpression unknown = engine.Compile("unknownFn(1)");

            Assert.ThrowsException<ExpressionFunctionException>(() => unknown.Evaluate());
        }

        [TestMethod]
        public void S4_UsageCompiledExpressions_SyntaxErrorThrowsAtCompileLikeEvaluate_Test()
        {
            var engine = new MathRuleEngine();

            var fromCompile = Assert.ThrowsException<ExpressionSyntaxException>(() => engine.Compile("2 + * 3"));
            var fromEvaluate = Assert.ThrowsException<ExpressionSyntaxException>(() => engine.Evaluate("2 + * 3"));

            EngineExceptionAssert.SameMessageAndColumn(fromEvaluate, fromCompile);
        }

        [TestMethod]
        public void S5_UsageTiering_FirstInterpretsSecondCompilesThirdHits_Test()
        {
            var engine = PinnedEngine.Create();
            var p = new Dictionary<string, object> { ["x"] = 3, ["y"] = 4 };

            Assert.AreEqual(11.0, engine.Evaluate("x + 2 * y", p));
            Assert.AreEqual(1, engine.PrepareCount);
            Assert.AreEqual(0, engine.CompileCount);
            var prog = CachedPrograms.Peek(engine, "x + 2 * y");
            Assert.IsFalse(prog.IsCompiled);

            Assert.AreEqual(11.0, engine.Evaluate("x + 2 * y", p));
            Assert.AreEqual(1, engine.PrepareCount);
            Assert.AreEqual(1, engine.CompileCount);
            Assert.IsTrue(prog.IsCompiled);

            Assert.AreEqual(11.0, engine.Evaluate("x + 2 * y", p));
            Assert.AreEqual(1, engine.PrepareCount);
            Assert.AreEqual(1, engine.CompileCount);
        }

        [TestMethod]
        public void S6_UsageTiering_OptionVariants_SameResults_Test()
        {
            var eager = new MathRuleEngine(new MathRuleEngineOptions { CompilationThreshold = 1 });
            var interpretOnly = new MathRuleEngine(new MathRuleEngineOptions { EnableCompilation = false });
            var noCache = new MathRuleEngine(new MathRuleEngineOptions { ExpressionCacheCapacity = 0 });

            Assert.AreEqual(11.0, eager.Evaluate("2 + 3 * (5 - 2)"));
            Assert.AreEqual(11.0, interpretOnly.Evaluate("2 + 3 * (5 - 2)"));
            Assert.AreEqual(11.0, noCache.Evaluate("2 + 3 * (5 - 2)"));
        }

        [TestMethod]
        public void S6_UsageTiering_Eager_CompilesOnFirstEvaluate_Test()
        {
            var eager = PinnedEngine.Create(new MathRuleEngineOptions { CompilationThreshold = 1 });

            Assert.AreEqual(11.0, eager.Evaluate("2 + 3 * (5 - 2)"));

            Assert.AreEqual(1, eager.CompileCount);
            Assert.IsTrue(CachedPrograms.Peek(eager, "2 + 3 * (5 - 2)").IsCompiled);
        }

        [TestMethod]
        public void S6_UsageTiering_InterpretOnly_CachesButNeverCompiles_Test()
        {
            var interpretOnly = PinnedEngine.Create(new MathRuleEngineOptions { EnableCompilation = false });

            interpretOnly.Evaluate("2 + 3 * (5 - 2)");
            interpretOnly.Evaluate("2 + 3 * (5 - 2)");

            Assert.AreEqual(0, interpretOnly.CompileCount);
            Assert.AreEqual(1, interpretOnly.PrepareCount);
            Assert.IsFalse(CachedPrograms.Peek(interpretOnly, "2 + 3 * (5 - 2)").IsCompiled);
        }

        [TestMethod]
        public void S6_UsageTiering_NoCache_NothingCached_Test()
        {
            var noCache = PinnedEngine.Create(new MathRuleEngineOptions { ExpressionCacheCapacity = 0 });

            noCache.Evaluate("2 + 3 * (5 - 2)");
            noCache.Evaluate("2 + 3 * (5 - 2)");

            Assert.AreEqual(0, noCache.CachedExpressionCount);
            Assert.IsFalse(CachedPrograms.IsCached(noCache, "2 + 3 * (5 - 2)"));
        }

        [DataTestMethod]
        [DataRow("ExpressionCacheCapacity", -1)]
        [DataRow("ExpressionCacheCapacity", 65537)]
        [DataRow("CompilationThreshold", 0)]
        [DataRow("CompilationThreshold", 1025)]
        public void P1_UsageResourceLimits_OutOfRangeNewOptions_ThrowNamingOptions_Test(string property, int value)
        {
            var options = new MathRuleEngineOptions();
            if (property == "ExpressionCacheCapacity")
                options.ExpressionCacheCapacity = value;
            else
                options.CompilationThreshold = value;

            OptionsValidationAssert.ThrowsOutOfRange(() => PinnedEngine.Create(options), property + " must be between");
        }

        [TestMethod]
        public void P2_UsageCompiledExpressions_CompileDoesNotPopulateCache_Test()
        {
            var engine = PinnedEngine.Create();

            engine.Compile("a * b");

            Assert.AreEqual(0, engine.CachedExpressionCount);
            Assert.IsFalse(CachedPrograms.IsCached(engine, "a * b"));
        }

        [TestMethod]
        public void P3_UsageWhenSecondEvaluateDoesNotCompile_Row6_DeepReentrantDeferred_ShallowCompiles_Test()
        {
            const string inner = "z + 1";
            var engine = PinnedEngine.Create(new MathRuleEngineOptions { MaxEvaluationDepth = 64 });
            engine.RegisterFunction("deep", args => engine.Evaluate(inner, new Dictionary<string, object> { ["z"] = 5 }));
            var outer = ExpressionText.ZeroPadded("deep()", 40);

            Assert.AreEqual(6.0, engine.Evaluate(outer));
            Assert.AreEqual(6.0, engine.Evaluate(outer));
            Assert.AreEqual(6.0, engine.Evaluate(outer));

            var program = CachedPrograms.Peek(engine, inner);
            Assert.IsFalse(program.IsCompiled, "Deep re-entrant uses must not compile.");
            Assert.AreEqual(CompileState.NotCompiled, program.State, "Deferred, not Failed.");

            engine.Evaluate(inner, new Dictionary<string, object> { ["z"] = 1 });
            engine.Evaluate(inner, new Dictionary<string, object> { ["z"] = 2 });

            Assert.IsTrue(program.IsCompiled, "A later shallow call path compiles it.");
        }

        [TestMethod]
        public void P4_UsageExpressionCache_WhitespaceVariantsAreSeparateEntries_Test()
        {
            var engine = PinnedEngine.Create();
            var p = new Dictionary<string, object> { ["x"] = 2 };

            Assert.AreEqual(3.0, engine.Evaluate("x+1", p));
            Assert.AreEqual(3.0, engine.Evaluate("x + 1", p));

            Assert.AreEqual(2, engine.PrepareCount);
            Assert.AreNotSame(CachedPrograms.Peek(engine, "x+1"), CachedPrograms.Peek(engine, "x + 1"));
        }
    }
}
