using System;
using System.Collections.Generic;
using System.Linq;
using MathExprEngine;
using MathExprEngine.Compilation;
using MathExprEngine.Enums;
using MathExprEngine.Exceptions;
using MathExprEngine.Models;
using MathExprEngineTests.Helpers;
using MathExprEngineTests.Helpers.Assertions;
using MathExprEngineTests.Helpers.Concurrency;
using MathExprEngineTests.Helpers.Engines;
using MathExprEngineTests.Helpers.Inputs;
using MathExprEngineTests.Helpers.TestBases;

namespace MathExprEngineTests.Tests
{
    [TestClass]
    public class CompilationSafetyTests : DepthGuardedTestBase
    {
        private const int ReentrantLinks = 250;

        public TestContext TestContext { get; set; }

        private static string ResultOrCatchable(Func<double> action, double expected, string label)
        {
            string outcome;

            try
            {
                var result = action();
                Assert.AreEqual(expected, result, label);
                outcome = label + ": result " + result;
            }
            catch (MathRuleEngineException ex)
            {
                outcome = label + ": catchable MathRuleEngineException: " + ex.Message;
            }

            Assert.AreEqual(0, EvaluationContext.CurrentDepth, label + ": depth must be restored.");

            return outcome;
        }

        private static string NestedTernary(int levels)
        {
            return string.Concat(Enumerable.Repeat("1?", levels)) + "1" + string.Concat(Enumerable.Repeat(":0", levels));
        }

        private static string ReentrantLevelText(int level)
        {
            return ExpressionText.ZeroPadded("reenter(" + level + ")", ReentrantLinks);
        }

        private static MathRuleEngine CreateReentrantEngine(MathRuleEngineOptions options)
        {
            var engine = new MathRuleEngine(options);
            engine.RegisterFunction(
                "reenter",
                args =>
                {
                    var level = (int)args[0];

                    return level <= 0 ? 1.0 : engine.Evaluate(ReentrantLevelText(level - 1));
                });

            return engine;
        }

        private void LogReentrantPrograms(MathRuleEngine engine, int topLevel, string label)
        {
            for (var level = topLevel; level >= 0; level--)
            {
                ExpressionProgram program;
                var cached = engine.TryPeekCachedProgram(ReentrantLevelText(level), out program);
                TestContext.WriteLine(
                    label + " level " + level + " (base depth " + (topLevel - level) * (ReentrantLinks + 1) + "): " +
                    (cached ? "cached, state " + program.State + ", outcome " + program.LastOutcome : "not cached"));

                if (cached)
                    Assert.AreNotEqual(CompileState.Failed, program.State, "A transient deep-stack condition must never mark a program Failed (A-1).");
            }
        }

        [TestMethod]
        [Timeout(10000)]
        public void FlatChainHeight256_Compile_CompilesOnOneMegabyteStack_Test()
        {
            var engine = PinnedEngine.Create();

            WorkerThreads.RunOnOneMegabyteStack(() =>
            {
                var handle = engine.Compile(ExpressionText.FlatChain(256));

                Assert.IsTrue(handle.IsCompiled);
                Assert.AreEqual(CompileOutcome.Compiled, handle.Program.LastOutcome);
                Assert.AreEqual(1, engine.CompileCount);
                Assert.AreEqual(256.0, handle.Evaluate());
            });
        }

        [TestMethod]
        [Timeout(10000)]
        public void FlatChainHeight257_Compile_FallsBackToInterpreterOnOneMegabyteStack_Test()
        {
            var engine = PinnedEngine.Create();

            WorkerThreads.RunOnOneMegabyteStack(() =>
            {
                var handle = engine.Compile(ExpressionText.FlatChain(257));

                Assert.IsFalse(handle.IsCompiled);
                Assert.AreEqual(CompileOutcome.HeightCapExceeded, handle.Program.LastOutcome);
                Assert.AreEqual(0, engine.CompileCount);
                Assert.AreEqual(257.0, handle.Evaluate());
            });
        }

        [TestMethod]
        public void EffectiveCompileCap_IsMinOfCapAndMaxEvaluationDepth_Test()
        {
            var engine = PinnedEngine.Create(new MathRuleEngineOptions { MaxEvaluationDepth = 100 });
            var interpreter = PinnedEngine.Interpreter(new MathRuleEngineOptions { MaxEvaluationDepth = 100 });

            var atLimit = engine.Compile(ExpressionText.FlatChain(100));
            Assert.IsTrue(atLimit.IsCompiled);
            Assert.AreEqual(100.0, atLimit.Evaluate());

            var overLimit = engine.Compile(ExpressionText.FlatChain(101));
            Assert.IsFalse(overLimit.IsCompiled);
            Assert.AreEqual(CompileOutcome.HeightCapExceeded, overLimit.Program.LastOutcome);

            var compiledError = ExceptionCapture.EngineExceptionOf(() => overLimit.Evaluate());
            var interpretedError = ExceptionCapture.EngineExceptionOf(() => interpreter.Evaluate(ExpressionText.FlatChain(101)));

            EngineExceptionAssert.SameTypeMessageAndColumn(interpretedError, compiledError);
        }

        [TestMethod]
        [Timeout(10000)]
        public void Sum4096Args_Compile_CompilesOnOneMegabyteStack_Test()
        {
            var engine = PinnedEngine.Create(new MathRuleEngineOptions
            {
                MaxFunctionArgs = 4096,
                MaxNodes = 65536,
                MaxTokens = 65536,
                MaxExpressionLength = 65536
            });
            var expression = ExpressionText.SumOfOnes(4096);

            WorkerThreads.RunOnOneMegabyteStack(() =>
            {
                var handle = engine.Compile(expression);

                Assert.IsTrue(handle.IsCompiled);
                Assert.AreEqual(1, engine.CompileCount);
                Assert.AreEqual(4096.0, handle.Evaluate());
            });
        }

        [TestMethod]
        [Timeout(10000)]
        public void NestedTernaryHeight256_Compile_CompilesOnOneMegabyteStack_Test()
        {
            var engine = PinnedEngine.Create(new MathRuleEngineOptions { MaxDepth = 1024 });
            var expression = NestedTernary(255);

            WorkerThreads.RunOnOneMegabyteStack(() =>
            {
                var handle = engine.Compile(expression);

                Assert.IsTrue(handle.IsCompiled, "Outcome: " + handle.Program.LastOutcome);
                Assert.AreEqual(1.0, handle.Evaluate());
            });
        }

        [TestMethod]
        [Timeout(10000)]
        public void NestedFunctionHeight256_Compile_CompilesOnOneMegabyteStack_Test()
        {
            var engine = PinnedEngine.Create(new MathRuleEngineOptions { MaxDepth = 1024 });
            var expression = ExpressionText.NestedNeg(255);

            var outcome = string.Empty;

            WorkerThreads.RunOnOneMegabyteStack(() =>
            {
#if NETFRAMEWORK
                try
                {
                    var handle = engine.Compile(expression);

                    Assert.IsTrue(handle.IsCompiled, "Outcome: " + handle.Program.LastOutcome);
                    Assert.AreEqual(-1.0, handle.Evaluate());
                    outcome = "compiled";
                }
                catch (MathRuleEngineException ex)
                {
                    outcome = "catchable MathRuleEngineException: " + ex.Message;
                }

                Assert.AreEqual(0, EvaluationContext.CurrentDepth, "Depth must be restored.");
#else
                var handle = engine.Compile(expression);

                Assert.IsTrue(handle.IsCompiled, "Outcome: " + handle.Program.LastOutcome);
                Assert.AreEqual(-1.0, handle.Evaluate());
                outcome = "compiled";
#endif
            });

            TestContext.WriteLine("NestedFunctionHeight256 on 1 MB: " + outcome);
        }

        [TestMethod]
        [Timeout(10000)]
        public void NestedFunctionHeight256_Compile_CompilesOnSixteenMegabyteStack_Test()
        {
            var engine = PinnedEngine.Create(new MathRuleEngineOptions { MaxDepth = 1024 });
            var expression = ExpressionText.NestedNeg(255);

            WorkerThreads.RunWithStack(WorkerThreads.SixteenMegabytes, () =>
            {
                var handle = engine.Compile(expression);

                Assert.IsTrue(handle.IsCompiled, "Outcome: " + handle.Program.LastOutcome);
                Assert.AreEqual(1, engine.CompileCount);
                Assert.AreEqual(-1.0, handle.Evaluate());
                Assert.AreEqual(0, EvaluationContext.CurrentDepth);
            });
        }

        [TestMethod]
        [Timeout(10000)]
        public void NestedTernaryAtMaxDepth1024_FallsBackAndEvaluatesOnOneMegabyteStack_Test()
        {
            var engine = PinnedEngine.Create(new MathRuleEngineOptions
            {
                MaxDepth = 1024,
                MaxEvaluationDepth = 2048,
                MaxNodes = 65536,
                MaxTokens = 65536,
                MaxExpressionLength = 65536
            });
            var expression = NestedTernary(1000);

            var outcomes = new List<string>();

            WorkerThreads.RunOnOneMegabyteStack(() =>
            {
                var handle = engine.Compile(expression);

                Assert.IsFalse(handle.IsCompiled);
                Assert.AreEqual(CompileOutcome.HeightCapExceeded, handle.Program.LastOutcome);

                outcomes.Add(ResultOrCatchable(() => handle.Evaluate(), 1.0, "handle"));
                outcomes.Add(ResultOrCatchable(() => engine.Evaluate(expression), 1.0, "Evaluate #1"));
                outcomes.Add(ResultOrCatchable(() => engine.Evaluate(expression), 1.0, "Evaluate #2"));
            });

            foreach (var outcome in outcomes)
                TestContext.WriteLine(outcome);
        }

        [TestMethod]
        [Timeout(10000)]
        public void NestedTernaryAtMaxDepth1024_FallsBackAndEvaluatesOnSixteenMegabyteStack_Test()
        {
            var engine = PinnedEngine.Create(new MathRuleEngineOptions
            {
                MaxDepth = 1024,
                MaxEvaluationDepth = 2048,
                MaxNodes = 65536,
                MaxTokens = 65536,
                MaxExpressionLength = 65536
            });
            var expression = NestedTernary(1000);

            WorkerThreads.RunWithStack(WorkerThreads.SixteenMegabytes, () =>
            {
                var handle = engine.Compile(expression);

                Assert.AreEqual(CompileOutcome.HeightCapExceeded, handle.Program.LastOutcome);
                Assert.AreEqual(1.0, handle.Evaluate());
                Assert.AreEqual(1.0, engine.Evaluate(expression));
                Assert.AreEqual(1.0, engine.Evaluate(expression));
                Assert.AreEqual(0, EvaluationContext.CurrentDepth);
            });
        }

        [TestMethod]
        [Timeout(10000)]
        public void FlatChainAtAllCeilings_CompileAndEvaluateTwice_ThrowCatchable_NeverCrash_Test()
        {
            var engine = PinnedEngine.Create(new MathRuleEngineOptions
            {
                MaxNodes = 65536,
                MaxTokens = 65536,
                MaxExpressionLength = 65536,
                MaxEvaluationDepth = 2048
            });
            var expression = ExpressionText.FlatChain(32768);

            WorkerThreads.RunOnOneMegabyteStack(() =>
            {
                var handle = engine.Compile(expression);
                Assert.IsFalse(handle.IsCompiled);
                Assert.AreEqual(CompileOutcome.HeightCapExceeded, handle.Program.LastOutcome);

                Assert.ThrowsException<MathRuleEngineException>(() => handle.Evaluate());
                Assert.AreEqual(0, EvaluationContext.CurrentDepth, "after handle.Evaluate");
                Assert.ThrowsException<MathRuleEngineException>(() => engine.Evaluate(expression));
                Assert.AreEqual(0, EvaluationContext.CurrentDepth, "after Evaluate #1");
                Assert.ThrowsException<MathRuleEngineException>(() => engine.Evaluate(expression));
                Assert.AreEqual(0, EvaluationContext.CurrentDepth, "after Evaluate #2");
                Assert.AreEqual(0, engine.CompileCount);
            });
        }

        [TestMethod]
        public void LazyAnd_OverDeepRightOperand_ReturnsZero_OnAllPaths_Test()
        {
            var engine = PinnedEngine.Create(new MathRuleEngineOptions { CompilationThreshold = 1 });
            var lazy = "0 && (" + ExpressionText.FlatChain(600) + ")";
            var eager = "1 && (" + ExpressionText.FlatChain(600) + ")";

            Assert.AreEqual(0.0, engine.Evaluate(lazy));
            Assert.AreEqual(0.0, engine.Evaluate(lazy));
            Assert.AreEqual(0.0, engine.Compile(lazy).Evaluate());
            Assert.ThrowsException<MathRuleEngineException>(() => engine.Evaluate(eager));
        }

        [DataTestMethod]
        [DataRow("MaxExpressionLength")]
        [DataRow("MaxTokens")]
        [DataRow("MaxNodes")]
        [DataRow("MaxDepth")]
        [DataRow("MaxFunctionArgs")]
        public void OverLimitInput_NeverReachesCompiler_OnCompileOrEvaluate_Test(string limit)
        {
            var options = new MathRuleEngineOptions { CompilationThreshold = 1 };
            string expression;

            switch (limit)
            {
                case "MaxExpressionLength": options.MaxExpressionLength = 10; expression = new string('+', 10) + "5"; break;
                case "MaxTokens": options.MaxTokens = 6; expression = "50%%%%%%"; break;
                case "MaxNodes": options.MaxNodes = 5; expression = "50%%%%%"; break;
                case "MaxDepth": options.MaxDepth = 10; expression = new string('-', 9) + "5"; break;
                default: options.MaxFunctionArgs = 3; expression = "sum(1,2,3,4)"; break;
            }

            var engine = PinnedEngine.Create(options);

            var fromCompile = Assert.ThrowsException<MathRuleEngineException>(() => engine.Compile(expression));
            var fromEvaluate = Assert.ThrowsException<MathRuleEngineException>(() => engine.Evaluate(expression));
            Assert.ThrowsException<MathRuleEngineException>(() => engine.Evaluate(expression));

            EngineExceptionAssert.SameMessageAndColumn(fromEvaluate, fromCompile);
            Assert.AreEqual(0, engine.CompileCount, "No delegate compilation may start for an over-limit input.");
            Assert.AreEqual(0, engine.CachedExpressionCount);
        }

        [TestMethod]
        [Timeout(30000)]
        public void ReentrantTierUp_DeepStack_CompilesOrDefersSafely()
        {
            const int topLevel = 7;
            var engine = CreateReentrantEngine(new MathRuleEngineOptions { MaxEvaluationDepth = 2048 });
            var results = new double[3];

            WorkerThreads.RunWithStack(WorkerThreads.SixteenMegabytes, () =>
            {
                for (var run = 0; run < results.Length; run++)
                {
                    results[run] = engine.Evaluate(ReentrantLevelText(topLevel));
                    Assert.AreEqual(0, EvaluationContext.CurrentDepth, "Depth not restored after run " + run);
                }
            });

            for (var run = 0; run < results.Length; run++)
                Assert.AreEqual(1.0, results[run], "run " + run);

            LogReentrantPrograms(engine, topLevel, "ReentrantTierUp_DeepStack_CompilesOrDefersSafely");
        }

        [TestMethod]
        [Timeout(30000)]
        public void ReentrantTierUp_DeepStack_OneMegabyte_ResultOrCatchable_NeverCrashes()
        {
            var engine = CreateReentrantEngine(new MathRuleEngineOptions { MaxEvaluationDepth = 2048 });
            var outcomes = new List<string>();

            WorkerThreads.RunOnOneMegabyteStack(() =>
            {
                for (var run = 0; run < 3; run++)
                    outcomes.Add(ResultOrCatchable(() => engine.Evaluate(ReentrantLevelText(7)), 1.0, "within-budget run " + run));

                for (var run = 0; run < 3; run++)
                {
                    Assert.ThrowsException<MathRuleEngineException>(() => engine.Evaluate(ReentrantLevelText(12)), "over-budget run " + run);
                    Assert.AreEqual(0, EvaluationContext.CurrentDepth, "Depth not restored after over-budget run " + run);
                }

                Assert.AreEqual(1.0, engine.Evaluate(ReentrantLevelText(0)));
            });

            foreach (var outcome in outcomes)
                TestContext.WriteLine("ReentrantTierUp_DeepStack_OneMegabyte: " + outcome);

            LogReentrantPrograms(engine, 12, "ReentrantTierUp_DeepStack_OneMegabyte");
        }

        [TestMethod]
        [Timeout(30000)]
        public void ReentrantTierUp_DeepStack_OverBudget_ThrowsCatchableAndRestoresDepth()
        {
            const int topLevel = 12;
            var engine = CreateReentrantEngine(new MathRuleEngineOptions { MaxEvaluationDepth = 2048 });
            var messages = new string[3];

            WorkerThreads.RunWithStack(WorkerThreads.SixteenMegabytes, () =>
            {
                for (var run = 0; run < messages.Length; run++)
                {
                    var ex = Assert.ThrowsException<MathRuleEngineException>(() => engine.Evaluate(ReentrantLevelText(topLevel)));
                    messages[run] = ex.Message;
                    Assert.AreEqual(0, EvaluationContext.CurrentDepth, "Depth not restored after run " + run);
                }

                Assert.AreEqual(1.0, engine.Evaluate(ReentrantLevelText(0)));
            });

            Assert.AreEqual(messages[0], messages[1]);
            Assert.AreEqual(messages[0], messages[2]);
            LogReentrantPrograms(engine, topLevel, "ReentrantTierUp_DeepStack_OverBudget");
        }

        [TestMethod]
        [Timeout(30000)]
        public void ReentrantTierUp_DeepStack_EagerThreshold1_CompilesOrDefersSafely()
        {
            const int topLevel = 7;
            MathRuleEngine engine;
            using (RunMode.Suppress())
            {
                engine = CreateReentrantEngine(new MathRuleEngineOptions { MaxEvaluationDepth = 2048, CompilationThreshold = 1 });
            }

            var result = double.NaN;

            WorkerThreads.RunWithStack(WorkerThreads.SixteenMegabytes, () =>
            {
                result = engine.Evaluate(ReentrantLevelText(topLevel));
                Assert.AreEqual(0, EvaluationContext.CurrentDepth);
            });

            Assert.AreEqual(1.0, result);

            var top = CachedPrograms.Peek(engine, ReentrantLevelText(topLevel));
            Assert.IsTrue(top.IsCompiled, "The top level (base depth 0) must compile eagerly. Outcome: " + top.LastOutcome);
            LogReentrantPrograms(engine, topLevel, "ReentrantTierUp_DeepStack_EagerThreshold1");
        }

        private static string Security_S1_Evaluate(CompiledExpression handle, double expected, string label)
        {
            string outcome;

            try
            {
                Assert.AreEqual(expected, handle.Evaluate(), label);
                outcome = label + ": result";
            }
            catch (MathRuleEngineException ex)
            {
                outcome = label + ": catchable MathRuleEngineException: " + ex.Message;
            }

            Assert.AreEqual(0, EvaluationContext.CurrentDepth, label + ": depth must be restored.");

            return outcome;
        }

        [TestMethod]
        [Timeout(10000)]
        public void Security_S1_WorstCaseTernaryHeight256_ExplicitCompileOn256KbThread_NeverCrashes_Test()
        {
            var engine = PinnedEngine.Create(new MathRuleEngineOptions { MaxDepth = 1024 });
            var expression = NestedTernary(255);
            var log = new List<string>();

            WorkerThreads.RunWithStack(256 * 1024, () =>
            {
                var handle = engine.Compile(expression);

                Assert.IsNotNull(handle);
                Assert.AreNotEqual(CompileState.Failed, handle.Program.State, "A stack shortage is transient, never Failed.");
                log.Add("outcome " + handle.Program.LastOutcome + ", compiled " + handle.IsCompiled);
                log.Add(Security_S1_Evaluate(handle, 1.0, "evaluate #1"));
                log.Add(Security_S1_Evaluate(handle, 1.0, "evaluate #2"));
            });

            foreach (var line in log)
                TestContext.WriteLine("Security_S1 ternary 256 KB: " + line);
        }

        [TestMethod]
        [Timeout(10000)]
        public void Security_S1_WorstCaseNestedFunctionHeight256_ExplicitCompileOn256KbThread_NeverCrashes_Test()
        {
            var engine = PinnedEngine.Create(new MathRuleEngineOptions { MaxDepth = 1024 });
            var expression = ExpressionText.NestedNeg(255);
            var log = new List<string>();

            WorkerThreads.RunWithStack(256 * 1024, () =>
            {
                CompiledExpression handle = null;

                try
                {
                    handle = engine.Compile(expression);
                }
                catch (MathRuleEngineException ex)
                {
                    log.Add("parser stack guard: " + ex.Message);
                }

                Assert.AreEqual(0, EvaluationContext.CurrentDepth);

                if (handle != null)
                {
                    Assert.AreNotEqual(CompileState.Failed, handle.Program.State, "A stack shortage is transient, never Failed.");
                    log.Add("outcome " + handle.Program.LastOutcome + ", compiled " + handle.IsCompiled);
                    log.Add(Security_S1_Evaluate(handle, -1.0, "evaluate"));
                }
            });

            foreach (var line in log)
                TestContext.WriteLine("Security_S1 neg 256 KB: " + line);
        }

        [TestMethod]
        [Timeout(20000)]
        public void Security_S1_ReentrantExplicitCompileNearDepthCeiling_IsTransient_HandleUsable_ThenCompiles_Test()
        {
            const string inner = "z * 2 + 1";
            var engine = PinnedEngine.Create(new MathRuleEngineOptions
            {
                MaxEvaluationDepth = 2048,
                MaxNodes = 65536,
                MaxTokens = 65536,
                MaxExpressionLength = 65536
            });
            CompiledExpression handle = null;
            var stateInside = CompileState.Compiled;
            var outcomeInside = CompileOutcome.Compiled;
            var depthInside = 0;

            engine.RegisterFunction(
                "mk",
                args =>
                {
                    depthInside = EvaluationContext.CurrentDepth;
                    handle = engine.Compile(inner);
                    stateInside = handle.Program.State;
                    outcomeInside = handle.Program.LastOutcome;

                    return handle.Evaluate(ParameterSets.Of("z", 5.0));
                });

            var outer = ExpressionText.ZeroPadded("mk()", 2000);
            var outerResult = double.NaN;

            WorkerThreads.RunWithStack(WorkerThreads.SixteenMegabytes, () =>
            {
                outerResult = engine.Evaluate(outer);
                Assert.AreEqual(0, EvaluationContext.CurrentDepth);
            });

            Assert.AreEqual(11.0, outerResult, "The handle created deep must be usable in the same call.");
            Assert.IsTrue(depthInside > 1024, "Test setup: Compile must run beyond the headroom limit (depth " + depthInside + ").");
            Assert.AreEqual(CompileState.NotCompiled, stateInside, "Deep explicit Compile must be TRANSIENT (NotCompiled), never Failed.");
            Assert.IsTrue(
                outcomeInside == CompileOutcome.None || outcomeInside == CompileOutcome.InsufficientStack,
                "Deep explicit Compile outcome must be transient, was " + outcomeInside);

            Assert.AreEqual(7.0, handle.Evaluate(ParameterSets.Of("z", 3.0)));
            Assert.AreEqual(9.0, handle.Evaluate(ParameterSets.Of("z", 4.0)));
            Assert.IsTrue(handle.IsCompiled, "The transiently deferred handle must compile once headroom is available. Outcome: " + handle.Program.LastOutcome);
        }

        private const string MatrixText = "rec(n) + 1";

        private static double RunMatrixCase(bool outerCompiled, bool innerCompiled, double n, List<int> observedDepths)
        {
            var compiledEngine = PinnedEngine.Create(new MathRuleEngineOptions { MaxEvaluationDepth = 64, CompilationThreshold = 1 });
            var interpretedEngine = PinnedEngine.Interpreter(new MathRuleEngineOptions { MaxEvaluationDepth = 64 });

            CompiledExpression inner = null;
            Func<double[], double> rec = args =>
            {
                observedDepths.Add(EvaluationContext.CurrentDepth);

                return args[0] <= 0 ? 0.0 : inner.Evaluate(ParameterSets.Of("n", args[0] - 1));
            };
            compiledEngine.RegisterFunction("rec", rec);
            interpretedEngine.RegisterFunction("rec", rec);

            inner = (innerCompiled ? compiledEngine : interpretedEngine).Compile(MatrixText);
            var outer = (outerCompiled ? compiledEngine : interpretedEngine).Compile(MatrixText);
            Assert.AreEqual(innerCompiled, inner.IsCompiled);
            Assert.AreEqual(outerCompiled, outer.IsCompiled);

            return outer.Evaluate(ParameterSets.Of("n", n));
        }

        [TestMethod]
        public void ReentrancyMatrix_WithinBudget_IdenticalResultAndObservedDepths_Test()
        {
            var oracleDepths = new List<int>();
            var oracle = RunMatrixCase(false, false, 10, oracleDepths);
            Assert.AreEqual(11.0, oracle);

            foreach (var outerCompiled in new[] { true, false })
            {
                foreach (var innerCompiled in new[] { true, false })
                {
                    var depths = new List<int>();
                    var result = RunMatrixCase(outerCompiled, innerCompiled, 10, depths);
                    var label = "outerCompiled=" + outerCompiled + ", innerCompiled=" + innerCompiled;

                    Assert.AreEqual(oracle, result, label);
                    CollectionAssert.AreEqual(oracleDepths, depths, label + ": host-observed CurrentDepth must match the interpreter.");
                    Assert.AreEqual(0, EvaluationContext.CurrentDepth, label);
                }
            }
        }

        [TestMethod]
        public void ReentrancyMatrix_OverBudget_IdenticalExceptionAcrossLayers_AndDepthRestored_Test()
        {
            var oracle = ExceptionCapture.EngineExceptionOf(() => RunMatrixCase(false, false, 100, new List<int>()));
            Assert.AreEqual(0, EvaluationContext.CurrentDepth);

            foreach (var outerCompiled in new[] { true, false })
            {
                foreach (var innerCompiled in new[] { true, false })
                {
                    var label = "outerCompiled=" + outerCompiled + ", innerCompiled=" + innerCompiled;
                    var ex = ExceptionCapture.EngineExceptionOf(() => RunMatrixCase(outerCompiled, innerCompiled, 100, new List<int>()));

                    EngineExceptionAssert.SameTypeMessageAndColumn(oracle, ex, label);
                    Assert.AreEqual(0, EvaluationContext.CurrentDepth, label + ": depth must be restored after the exception.");
                }
            }

            var engine = PinnedEngine.Create(new MathRuleEngineOptions { MaxEvaluationDepth = 64, CompilationThreshold = 1 });
            Assert.AreEqual(64.0, engine.Evaluate(ExpressionText.FlatChain(64)));
        }

        [TestMethod]
        public void HandleEvaluatedReentrantlyByOwnFunction_SharesOneDepthBudget_Test()
        {
            var engine = PinnedEngine.Create(new MathRuleEngineOptions { MaxEvaluationDepth = 64 });
            CompiledExpression handle = null;
            engine.RegisterFunction(
                "self",
                args => args[0] <= 0 ? 0.0 : handle.Evaluate(ParameterSets.Of("n", args[0] - 1)));
            handle = engine.Compile("self(n) + 1");
            Assert.IsTrue(handle.IsCompiled);

            Assert.AreEqual(6.0, handle.Evaluate(ParameterSets.Of("n", 5.0)));
            Assert.AreEqual(0, EvaluationContext.CurrentDepth);

            Assert.ThrowsException<MathRuleEngineException>(() => handle.Evaluate(ParameterSets.Of("n", 100.0)));
            Assert.AreEqual(0, EvaluationContext.CurrentDepth, "Depth must be restored after a re-entrant depth failure.");

            Assert.AreEqual(6.0, handle.Evaluate(ParameterSets.Of("n", 5.0)), "The handle must stay usable.");
            Assert.AreEqual(64.0, engine.Evaluate(ExpressionText.FlatChain(64)), "Full budget must be available afterwards.");
        }

        [TestMethod]
        public void CustomFunctionThrowsInsideCompiledEvaluation_DepthRestored_HandleReusable_Test()
        {
            var engine = PinnedEngine.Create();
            engine.RegisterFunction(
                "boom",
                args =>
                {
                    if (args[0] < 0)
                        throw new InvalidOperationException("boom-host");

                    return args[0];
                });
            var handle = engine.Compile("1 + boom(x)");
            Assert.IsTrue(handle.IsCompiled);

            Assert.ThrowsException<ExpressionFunctionException>(() => handle.Evaluate(ParameterSets.X(-1.0)));
            Assert.AreEqual(0, EvaluationContext.CurrentDepth);

            Assert.AreEqual(3.0, handle.Evaluate(ParameterSets.X(2.0)));
            Assert.IsTrue(handle.IsCompiled);
        }

        [TestMethod]
        public void HostFunction_ReceivesFreshArgumentArrayOnEveryCall_Test()
        {
            var engine = PinnedEngine.Create();
            var received = new List<double[]>();
            engine.RegisterFunction(
                "keep",
                args =>
                {
                    received.Add(args);

                    return args[0];
                });
            var handle = engine.Compile("keep(x) + keep(x + 1)");
            Assert.IsTrue(handle.IsCompiled);

            Assert.AreEqual(3.0, handle.Evaluate(ParameterSets.X(1.0)));
            Assert.AreEqual(21.0, handle.Evaluate(ParameterSets.X(10.0)));

            Assert.AreEqual(4, received.Count);
            for (var i = 0; i < received.Count; i++)
                for (var j = i + 1; j < received.Count; j++)
                    Assert.AreNotSame(received[i], received[j], "Argument arrays must never be reused (" + i + "," + j + ").");

            CollectionAssert.AreEqual(new[] { 1.0 }, received[0]);
            CollectionAssert.AreEqual(new[] { 2.0 }, received[1]);
            CollectionAssert.AreEqual(new[] { 10.0 }, received[2]);
            CollectionAssert.AreEqual(new[] { 11.0 }, received[3]);
        }

        private static Exception CreateFault(string kind)
        {
            switch (kind)
            {
                case "PlatformNotSupported": return new PlatformNotSupportedException("injected");
                case "InvalidProgram": return new InvalidProgramException("injected");
                case "InvalidOperation": return new InvalidOperationException("injected");
                default: return new ArgumentException("injected");
            }
        }

        [DataTestMethod]
        [DataRow("PlatformNotSupported")]
        [DataRow("InvalidProgram")]
        [DataRow("InvalidOperation")]
        [DataRow("Argument")]
        public void CompileFaultInjector_PermanentFault_EvaluateFallsBackWithCorrectResult_Test(string kind)
        {
            var options = new MathRuleEngineOptions();
            options.CompileFaultInjector = lambda => { throw CreateFault(kind); };
            var engine = PinnedEngine.Create(options);

            Assert.AreEqual(7.0, engine.Evaluate("x * 2 + 1", ParameterSets.X(3.0)));
            Assert.AreEqual(9.0, engine.Evaluate("x * 2 + 1", ParameterSets.X(4.0)));

            var program = CachedPrograms.Peek(engine, "x * 2 + 1");
            Assert.AreEqual(CompileOutcome.CompileFault, program.LastOutcome, kind);
            Assert.AreEqual(CompileState.Failed, program.State, kind);
        }

        [DataTestMethod]
        [DataRow("PlatformNotSupported")]
        [DataRow("InvalidProgram")]
        public void CompileFaultInjector_PermanentFault_CompileReturnsInterpretedHandle_Test(string kind)
        {
            var options = new MathRuleEngineOptions();
            options.CompileFaultInjector = lambda => { throw CreateFault(kind); };
            var engine = PinnedEngine.Create(options);

            var handle = engine.Compile("x * 2 + 1");

            Assert.IsFalse(handle.IsCompiled);
            Assert.AreEqual(CompileOutcome.CompileFault, handle.Program.LastOutcome);
            Assert.AreEqual(7.0, handle.Evaluate(ParameterSets.X(3.0)));
        }

        [TestMethod]
        public void ThrowOnCompileFaultTrue_UnexpectedFault_IsRethrownOnce_ThenProgramIsFailed_Test()
        {
            var options = new MathRuleEngineOptions();
            options.ThrowOnCompileFault = true;
            options.CompileFaultInjector = lambda => { throw new InvalidProgramException("injected"); };
            var engine = PinnedEngine.Create(options);
            var parameters = ParameterSets.X(3.0);

            Assert.AreEqual(7.0, engine.Evaluate("x * 2 + 1", parameters));
            Assert.ThrowsException<InvalidProgramException>(() => engine.Evaluate("x * 2 + 1", parameters));
            Assert.AreEqual(0, EvaluationContext.CurrentDepth);

            var program = CachedPrograms.Peek(engine, "x * 2 + 1");
            Assert.AreEqual(CompileState.Failed, program.State);
            Assert.AreEqual(CompileOutcome.CompileFault, program.LastOutcome);
            Assert.AreEqual(7.0, engine.Evaluate("x * 2 + 1", parameters), "A Failed program is interpreted; no new attempt, no rethrow.");
        }
    }
}
