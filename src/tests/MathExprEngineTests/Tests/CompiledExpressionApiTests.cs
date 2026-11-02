using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using MathExprEngine;
using MathExprEngine.Enums;
using MathExprEngine.Exceptions;
using MathExprEngineTests.Helpers.Assertions;
using MathExprEngineTests.Helpers.Engines;
using MathExprEngineTests.Helpers.Inputs;
using MathExprEngineTests.Helpers.Parity;
using MathExprEngineTests.Helpers.TestBases;

namespace MathExprEngineTests.Tests
{
    [TestClass]
    public class CompiledExpressionApiTests : ParityDepthGuardedTestBase
    {
        public TestContext TestContext { get; set; }

        private static MathRuleEngine NewEngine(Action<MathRuleEngineOptions> configure = null)
        {
            var o = new MathRuleEngineOptions { ThrowOnCompileFault = true };
            configure?.Invoke(o);

            return PinnedEngine.Create(o);
        }

        [DataTestMethod]
        [DataRow("a + b * 2")]
        [DataRow("a * b + c > 10 && max(a, b, c) < 100 ? a % 7 : b^2")]
        [DataRow("sqrt(a) + -b^2 + 50% * c")]
        public void Compile_HandleEvaluate_EqualsEngineEvaluate(string expression)
        {
            var engine = NewEngine();
            var handle = engine.Compile(expression);

            Assert.IsNotNull(handle);
            Assert.IsTrue(handle.IsCompiled);

            var p = ParameterSets.Of("a", 9.0, "b", 4, "c", "3.5");
            var viaEvaluate = NewEngine(OptionPresets.InterpreterOnly).Evaluate(expression, p);

            BitAssert.AreEqual(viaEvaluate, handle.Evaluate(p));
        }

        [TestMethod]
        public void Compile_HandleReusedWithDifferentParameters_NoLeakBetweenCalls()
        {
            var handle = NewEngine().Compile("x + 1");
            var oracle = NewEngine(OptionPresets.InterpreterOnly);

            Assert.AreEqual(2.0, handle.Evaluate(ParameterSets.Of("x", 1.0)));
            Assert.AreEqual(3.0, handle.Evaluate(ParameterSets.Of("x", 2)));

            ParityAssert.AreSameException(
                ExceptionCapture.Of(() => oracle.Evaluate("x + 1")),
                ExceptionCapture.Of(() => handle.Evaluate()),
                "null parameters after a successful call");
            ParityAssert.AreSameException(
                ExceptionCapture.Of(() => oracle.Evaluate("x + 1", new Dictionary<string, object>())),
                ExceptionCapture.Of(() => handle.Evaluate(new Dictionary<string, object>())),
                "empty parameters after a successful call");

            Assert.AreEqual(6.0, handle.Evaluate(ParameterSets.Of("x", "5")));
            Assert.AreEqual(-9.0, handle.Evaluate(ParameterSets.Of("X", -10m)));
        }

        [DataTestMethod]
        [DataRow("1/0")]
        [DataRow("5%0")]
        [DataRow("sqrt(-1)")]
        [DataRow("10^400")]
        [DataRow("unknownFn()")]
        [DataRow("undefinedParam + 1")]
        [DataRow("mod(1,0)")]
        [DataRow("sum()")]
        public void Compile_RuntimeOnlyError_CompilesThenThrowsIdenticallyOnEvaluate(string expression)
        {
            var engine = NewEngine();

            var handle = engine.Compile(expression);
            Assert.IsTrue(handle.IsCompiled, "Runtime-only errors must not prevent compilation (no folding).");
            Assert.AreEqual(1, engine.CompileCount);

            var oracle = NewEngine(OptionPresets.InterpreterOnly);
            ParityAssert.AreSameException(ExceptionCapture.Of(() => oracle.Evaluate(expression)), ExceptionCapture.Of(() => handle.Evaluate()), expression);
        }

        [DataTestMethod]
        [DataRow("0 && unknownFn()", 0.0)]
        [DataRow("1 || undefinedParam", 1.0)]
        [DataRow("1 ? 2 : 1/0", 2.0)]
        public void Compile_LazyErrors_UnreachedNodeDoesNotThrow(string expression, double expected)
        {
            var handle = NewEngine().Compile(expression);

            Assert.IsTrue(handle.IsCompiled);
            Assert.AreEqual(expected, handle.Evaluate());
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        [DataRow("\t")]
        public void Compile_NullOrBlank_ThrowsSameArgumentExceptionAsEvaluate(string expression)
        {
            var engine = NewEngine();

            var expected = ExceptionCapture.Of(() => engine.Evaluate(expression));
            var actual = ExceptionCapture.Of(() => engine.Compile(expression));

            Assert.IsInstanceOfType(expected, typeof(ArgumentException));
            ParityAssert.AreSameException(expected, actual, "Compile(" + (expression ?? "null") + ")");
        }

        [DataTestMethod]
        [DataRow("2 +", 0, 0, 0, 0)]
        [DataRow("(2 + 3", 0, 0, 0, 0)]
        [DataRow("2 ** 3", 0, 0, 0, 0)]
        [DataRow("1 2", 0, 0, 0, 0)]
        [DataRow("1.2.3", 0, 0, 0, 0)]
        [DataRow("@", 0, 0, 0, 0)]
        [DataRow("50%%%%%%", 6, 0, 0, 0)]
        [DataRow("50%%%%%", 0, 5, 0, 0)]
        [DataRow("---------5", 0, 0, 10, 0)]
        [DataRow("sum(1,2,3,4)", 0, 0, 0, 3)]
        [DataRow("1+1+1+1", 3, 2, 0, 0)]
        public void Compile_PrepareFailure_ThrowsIdentically_NoCompileNoCacheInsert(
            string expression, int maxTokens, int maxNodes, int maxDepth, int maxArgs)
        {
            void Cfg(MathRuleEngineOptions o)
            {
                if (maxTokens > 0) o.MaxTokens = maxTokens;
                if (maxNodes > 0) o.MaxNodes = maxNodes;
                if (maxDepth > 0) o.MaxDepth = maxDepth;
                if (maxArgs > 0) o.MaxFunctionArgs = maxArgs;
            }

            var oracle = NewEngine(o => { Cfg(o); OptionPresets.InterpreterOnly(o); });
            var engine = NewEngine(Cfg);

            var expected = ExceptionCapture.Of(() => oracle.Evaluate(expression));
            var cachedBefore = engine.CachedExpressionCount;
            var prepareBefore = engine.PrepareCount;

            var actual = ExceptionCapture.Of(() => engine.Compile(expression));

            ParityAssert.AreSameException(expected, actual, "Compile(\"" + expression + "\")");
            Assert.AreEqual(prepareBefore + 1, engine.PrepareCount, "PrepareCount");
            Assert.AreEqual(cachedBefore, engine.CachedExpressionCount, "CachedExpressionCount");
            Assert.AreEqual(0, engine.CompileCount, "CompileCount");

            ParityAssert.AreSameException(expected, ExceptionCapture.Of(() => engine.Evaluate(expression)), "Evaluate(\"" + expression + "\")");
            Assert.AreEqual(0, engine.CompileCount, "CompileCount after Evaluate");
        }

        [TestMethod]
        public void Compile_OverMaxExpressionLength_ThrowsIdentically_NoCompile()
        {
            var expression = new string('+', 10) + "5";
            var oracle = NewEngine(o => { o.MaxExpressionLength = 10; OptionPresets.InterpreterOnly(o); });
            var engine = NewEngine(o => o.MaxExpressionLength = 10);

            ParityAssert.AreSameException(ExceptionCapture.Of(() => oracle.Evaluate(expression)), ExceptionCapture.Of(() => engine.Compile(expression)), "length");
            Assert.AreEqual(0, engine.CompileCount);
            Assert.AreEqual(0, engine.CachedExpressionCount);
            Assert.IsTrue(engine.PrepareCount <= 1, "PrepareCount");
        }

        [TestMethod]
        public void Compile_HandleBoundToOwningEngineFunctionsAndOptions()
        {
            var a = NewEngine(o => o.AllowNonFiniteResults = true);
            var b = NewEngine();
            a.RegisterFunction("f", x => 1);
            b.RegisterFunction("f", x => 2);

            var ha = a.Compile("f() + 10^400");
            var hb = b.Compile("f()");

            Assert.IsTrue(double.IsPositiveInfinity(ha.Evaluate()), "engine A allows non-finite");
            Assert.AreEqual(2.0, hb.Evaluate());
            Assert.ThrowsException<MathRuleEngineException>(() => b.Compile("f() + 10^400").Evaluate());

            b.RegisterFunction("f", x => 3);
            Assert.AreEqual(3.0, hb.Evaluate());
            Assert.IsTrue(double.IsPositiveInfinity(ha.Evaluate()), "engine B's registry must not affect A");
        }

        [TestMethod]
        public void Compile_LeadingWhitespace_ChangesColumn()
        {
            var engine = NewEngine();

            var e1 = (MathRuleEngineException)ExceptionCapture.Of(() => engine.Compile("1/0").Evaluate());
            var e2 = (MathRuleEngineException)ExceptionCapture.Of(() => engine.Compile(" 1/0").Evaluate());

            Assert.AreEqual(1, e1.Column);
            Assert.AreEqual(2, e2.Column);
        }

        [DataTestMethod]
        [DataRow("  X + Sqrt(4) ")]
        [DataRow("1")]
        [DataRow("a\t*\tB")]
        public void Compile_ExpressionAndToString_ReturnExactText(string expression)
        {
            var handle = NewEngine().Compile(expression);

            Assert.AreEqual(expression, handle.Expression);
            Assert.AreEqual(expression, handle.ToString());
            Assert.IsTrue(ReferenceEquals(expression, handle.Expression) || string.Equals(expression, handle.Expression, StringComparison.Ordinal));
        }

        [TestMethod]
        public void Evaluate_WhitespaceAndCaseVariants_AreSeparateCacheKeys()
        {
            var engine = NewEngine();
            var p = ParameterSets.Of("x", 2.0);

            Assert.AreEqual(3.0, engine.Evaluate("x+1", p));
            Assert.AreEqual(3.0, engine.Evaluate("x + 1", p));
            Assert.AreEqual(3.0, engine.Evaluate("X+1", p));
            Assert.AreEqual(3.0, engine.Evaluate("x+1 ", p));

            Assert.AreEqual(4, engine.CachedExpressionCount);
            Assert.AreEqual(4, engine.PrepareCount);
        }

        [TestMethod]
        public void Compile_FreshEngine_CompiledImmediately()
        {
            var engine = NewEngine();

            var handle = engine.Compile("a * b + 1");

            Assert.IsTrue(handle.IsCompiled);
            Assert.AreEqual(CompileState.Compiled, handle.Program.State);
            Assert.AreEqual(CompileOutcome.Compiled, handle.Program.LastOutcome);
            Assert.IsNotNull(handle.Program.Compiled);
            Assert.AreEqual(1, engine.CompileCount);
            Assert.AreEqual(1, engine.PrepareCount);
            Assert.IsTrue(handle.Program.WouldUseDelegate(0));

            Assert.AreEqual(7.0, handle.Evaluate(ParameterSets.Of("a", 2, "b", 3)));
            Assert.AreEqual(1, engine.CompileCount, "evaluation must not recompile");
        }

        [TestMethod]
        public void Compile_CompilationDisabled_ReturnsInterpretedHandle()
        {
            var engine = NewEngine(o => o.EnableCompilation = false);

            var handle = engine.Compile("a * b + 1");

            Assert.IsFalse(handle.IsCompiled);
            Assert.AreEqual(CompileOutcome.None, handle.Program.LastOutcome);
            Assert.AreEqual(0, engine.CompileCount);
            Assert.AreEqual(7.0, handle.Evaluate(ParameterSets.Of("a", 2, "b", 3)));
        }

        [DataTestMethod]
        [DataRow(1024)]
        [DataRow(0)]
        public void Compile_NeverInsertsIntoCache(int capacity)
        {
            var engine = NewEngine(o => o.ExpressionCacheCapacity = capacity);

            var handle = engine.Compile("x * 2");

            Assert.IsTrue(handle.IsCompiled);
            Assert.AreEqual(0, engine.CachedExpressionCount);
            Assert.IsFalse(CachedPrograms.IsCached(engine, "x * 2"));
            Assert.AreEqual(1, engine.PrepareCount);

            Assert.AreEqual(6.0, engine.Evaluate("x * 2", ParameterSets.Of("x", 3)));
            Assert.AreEqual(capacity == 0 ? 0 : 1, engine.CachedExpressionCount);

            if (capacity > 0)
            {
                Assert.AreEqual(2, engine.PrepareCount);
                var cached = CachedPrograms.Peek(engine, "x * 2");
                Assert.AreNotSame(handle.Program, cached);
                Assert.IsFalse(cached.IsCompiled, "1st Evaluate must not compile (threshold 2)");
                Assert.AreEqual(1, engine.CompileCount);
            }
        }

        [TestMethod]
        public void Compile_AfterTierUp_SharesCompiledCachedProgram()
        {
            var engine = NewEngine();

            engine.Evaluate("x + 3", ParameterSets.Of("x", 1));
            engine.Evaluate("x + 3", ParameterSets.Of("x", 2));
            var cached = CachedPrograms.Peek(engine, "x + 3");
            Assert.IsTrue(cached.IsCompiled);

            var prepare = engine.PrepareCount;
            var compiles = engine.CompileCount;
            var useCount = cached.UseCount;

            var handle = engine.Compile("x + 3");

            Assert.AreSame(cached, handle.Program);
            Assert.IsTrue(handle.IsCompiled);
            Assert.AreEqual(prepare, engine.PrepareCount);
            Assert.AreEqual(compiles, engine.CompileCount);
            Assert.AreEqual(useCount, cached.UseCount);
            Assert.AreEqual(1, engine.CachedExpressionCount);
            Assert.AreEqual(13.0, handle.Evaluate(ParameterSets.Of("x", 10)));
        }

        [TestMethod]
        public void Compile_AfterFirstEvaluate_DoesNotTouchCachedEntryState()
        {
            var engine = NewEngine();

            engine.Evaluate("x + 4", ParameterSets.Of("x", 1));
            var cached = CachedPrograms.Peek(engine, "x + 4");
            Assert.AreEqual(1, cached.UseCount);
            Assert.IsFalse(cached.IsCompiled);

            var handle = engine.Compile("x + 4");

            Assert.IsTrue(handle.IsCompiled);
            Assert.AreEqual(1, cached.UseCount, "Compile must not record a use");
            Assert.AreEqual(1, engine.CachedExpressionCount);
            Assert.AreEqual(5.0, handle.Evaluate(ParameterSets.Of("x", 1)));
        }

        [TestMethod]
        public void Compile_ThenReplaceBuiltInDifferentCase_NextEvaluationUsesReplacement()
        {
            var engine = NewEngine();
            var handle = engine.Compile("sqrt(4)");
            Assert.AreEqual(2.0, handle.Evaluate());

            engine.RegisterFunction("SQRT", a => 42);

            Assert.AreEqual(42.0, handle.Evaluate());
        }

        [TestMethod]
        public void TierUp_ThenReplaceBuiltInDifferentCase_NextEvaluationUsesReplacement()
        {
            var engine = NewEngine();
            Assert.AreEqual(2.0, engine.Evaluate("sqrt(4)"));
            Assert.AreEqual(2.0, engine.Evaluate("sqrt(4)"));
            var program = CachedPrograms.Peek(engine, "sqrt(4)");
            Assert.IsTrue(program.IsCompiled);

            engine.RegisterFunction("Sqrt", a => 42);

            Assert.AreEqual(42.0, engine.Evaluate("sqrt(4)"));
            Assert.AreEqual(1, engine.CompileCount, "late binding must not need a recompile");
        }

        [TestMethod]
        public void Compile_FunctionRegisteredAfterCompile_ResolvedAtEvaluation()
        {
            var engine = NewEngine();
            var oracle = NewEngine(OptionPresets.InterpreterOnly);

            var handle = engine.Compile("1 && laterFn(2)");
            Assert.IsTrue(handle.IsCompiled);

            ParityAssert.AreSameException(ExceptionCapture.Of(() => oracle.Evaluate("1 && laterFn(2)")), ExceptionCapture.Of(() => handle.Evaluate()), "before registration");

            engine.RegisterFunction("LATERFN", a => a[0] * 5);

            Assert.AreEqual(1.0, handle.Evaluate());
            Assert.AreEqual(0.0, engine.Compile("0 && laterFn(2)").Evaluate());
        }

        private static bool LibraryIsNetStandard21()
        {
            var attr = (TargetFrameworkAttribute)Attribute.GetCustomAttribute(typeof(MathRuleEngine).Assembly, typeof(TargetFrameworkAttribute));
            return attr != null && attr.FrameworkName.IndexOf("Version=v2.1", StringComparison.Ordinal) >= 0;
        }

        private const string LinqInterpreterNamespace = "System.Linq.Expressions.Interpreter";

        private static bool IsLinqInterpreted(Delegate d, out string chain)
        {
            var names = new List<string>();
            var found = false;
            object target = d?.Target;

            for (var hop = 0; hop < 4; hop++)
            {
                if (target == null)
                {
                    names.Add("<null>");
                    break;
                }

                var type = target.GetType();
                names.Add(type.FullName);

                if (type.Namespace != null && type.Namespace.StartsWith(LinqInterpreterNamespace, StringComparison.Ordinal))
                {
                    found = true;
                    break;
                }

                if (!(target is Delegate next))
                    break;

                target = next.Target;
            }

            chain = string.Join(" -> ", names);
            return found;
        }

        [TestMethod]
        public void PreferInterpretedExpressionTrees_DelegateIsLinqInterpreter_EagerIsNot()
        {
            var interpretedEngine = NewEngine(o => { o.CompilationThreshold = 1; o.PreferInterpretedExpressionTrees = true; });
            var eagerEngine = NewEngine(o => o.CompilationThreshold = 1);

            var hi = interpretedEngine.Compile("x * 2 + sqrt(x)");
            var he = eagerEngine.Compile("x * 2 + sqrt(x)");
            Assert.IsTrue(hi.IsCompiled);
            Assert.IsTrue(he.IsCompiled);

            var interpreted = IsLinqInterpreted(hi.Program.Compiled.Delegate, out var interpretedChain);
            var eagerInterpreted = IsLinqInterpreted(he.Program.Compiled.Delegate, out var eagerChain);

            TestContext.WriteLine("Library netstandard2.1: " + LibraryIsNetStandard21() + "; interpreted target chain: " + interpretedChain + "; eager target chain: " + eagerChain);

            Assert.IsFalse(eagerInterpreted, eagerChain);

            if (LibraryIsNetStandard21())
                Assert.IsTrue(interpreted, interpretedChain);
            else
                Assert.IsFalse(interpreted, interpretedChain);

            var p = ParameterSets.Of("x", 9.0);
            BitAssert.AreEqual(he.Evaluate(p), hi.Evaluate(p));
        }

        [TestMethod]
        public void PreferInterpretedExpressionTrees_HostFunctionException_IdenticalToInterpreter()
        {
            Action<MathRuleEngine> setup = e => e.RegisterFunction("boom", a => throw new InvalidOperationException("db-host-01"));

            foreach (var path in new[] { ParityPath.CompileInterpretedTrees, ParityPath.EagerInterpretedTrees, ParityPath.Compile, ParityPath.Eager })
            {
                var oracle = ParityAssert.Matches(path, "1 + boom()", null, null, setup);
                Assert.AreEqual(typeof(ExpressionFunctionException), oracle.Error.GetType());
                StringAssert.Contains(oracle.Error.Message, "db-host-01");
            }
        }
    }
}
