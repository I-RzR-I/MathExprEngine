using System;
using System.Collections.Generic;
using System.Threading;
using MathExprEngine;
using MathExprEngine.Compilation;
using MathExprEngine.Enums;
using MathExprEngine.Models;
using MathExprEngineTests.Helpers.Assertions;
using MathExprEngineTests.Helpers.Engines;
using MathExprEngineTests.Helpers.Inputs;
using MathExprEngineTests.Helpers.TestBases;

namespace MathExprEngineTests.Tests
{
    [TestClass]
    public class CompilationTieringTests : DepthGuardedTestBase
    {
        private const string Expr = "x * 2 + y";

        public TestContext TestContext { get; set; }

        [TestMethod]
        public void DefaultCompilationThreshold_IsTwo_AndPublicOptionDefaultsToIt_Test()
        {
            Assert.AreEqual(2, MathRuleEngineOptions.DefaultCompilationThreshold);
            Assert.AreEqual(2, new MathRuleEngineOptions().CompilationThreshold);
        }

        [TestMethod]
        public void FirstEvaluate_InterpretsAndCaches_DoesNotCompile_Test()
        {
            var engine = PinnedEngine.Create();

            var result = engine.Evaluate(Expr, ParameterSets.XY(3, 1));

            Assert.AreEqual(7.0, result);
            Assert.AreEqual(1, engine.CachedExpressionCount);
            Assert.AreEqual(1, engine.PrepareCount);
            Assert.AreEqual(0, engine.CompileCount);

            var program = CachedPrograms.Peek(engine, Expr);
            Assert.AreEqual(1, program.UseCount);
            Assert.IsFalse(program.IsCompiled);
            Assert.AreEqual(CompileState.NotCompiled, program.State);
            Assert.AreEqual(CompileOutcome.None, program.LastOutcome);
            Assert.IsNull(program.Compiled);
        }

        [TestMethod]
        public void SecondEvaluate_CompilesWithoutReparse_AndIsServedByDelegate_Test()
        {
            var engine = PinnedEngine.Create();

            engine.Evaluate(Expr, ParameterSets.XY(3, 1));
            var second = engine.Evaluate(Expr, ParameterSets.XY(-2.5, 0.125));

            Assert.AreEqual(1, engine.PrepareCount, "The 2nd use must not tokenize/parse again.");
            Assert.AreEqual(1, engine.CompileCount);

            var program = CachedPrograms.Peek(engine, Expr);
            Assert.IsTrue(program.IsCompiled);
            Assert.AreEqual(CompileOutcome.Compiled, program.LastOutcome);
            Assert.IsNotNull(program.Compiled);
            Assert.IsTrue(program.WouldUseDelegate(0), "A top-level call must be served by the compiled delegate.");
            BitAssert.AreEqualDescribed(PinnedEngine.Interpreter().Evaluate(Expr, ParameterSets.XY(-2.5, 0.125)), second, "2nd-use result must be bit-equal to the interpreter.");
        }

        [TestMethod]
        public void Calls3To10_DoNotRecompileOrReparse_Test()
        {
            var engine = PinnedEngine.Create();
            ExpressionProgram first = null;

            for (var i = 1; i <= 10; i++)
            {
                var parameters = ParameterSets.XY(i, i / 2.0);
                var result = engine.Evaluate(Expr, parameters);

                BitAssert.AreEqualDescribed(PinnedEngine.Interpreter().Evaluate(Expr, parameters), result, "call " + i);

                if (i == 1)
                    first = CachedPrograms.Peek(engine, Expr);

                if (i >= 2)
                    Assert.AreEqual(1, engine.CompileCount, "CompileCount after call " + i);
            }

            Assert.AreEqual(1, engine.PrepareCount);
            Assert.AreEqual(1, engine.CompileCount);
            Assert.AreSame(first, CachedPrograms.Peek(engine, Expr), "The cached program must be the same instance for calls 1..10.");
            Assert.IsTrue(first.IsCompiled);
        }

        [TestMethod]
        public void Compile_OnFreshEngine_CompilesImmediately_AndDoesNotInsert_Test()
        {
            var engine = PinnedEngine.Create();

            var handle = engine.Compile(Expr);

            Assert.IsTrue(handle.IsCompiled, "Compile must compile immediately (D1).");
            Assert.AreEqual(CompileOutcome.Compiled, handle.Program.LastOutcome);
            Assert.AreEqual(1, engine.CompileCount);
            Assert.AreEqual(1, engine.PrepareCount);
            Assert.AreEqual(0, engine.CachedExpressionCount, "Compile must never insert into the cache (A-9).");

            Assert.IsFalse(CachedPrograms.IsCached(engine, Expr));
            Assert.AreEqual(7.0, handle.Evaluate(ParameterSets.XY(3, 1)));
        }

        [TestMethod]
        public void CompileThenEvaluate_EvaluatePreparesOnceAndTiersNormally_Test()
        {
            var engine = PinnedEngine.Create();
            var handle = engine.Compile(Expr);

            Assert.AreEqual(7.0, engine.Evaluate(Expr, ParameterSets.XY(3, 1)));

            Assert.AreEqual(2, engine.PrepareCount, "The handle was not cached, so the 1st Evaluate must Prepare once.");
            var program = CachedPrograms.Peek(engine, Expr);
            Assert.AreNotSame(handle.Program, program);
            Assert.AreEqual(1, program.UseCount);
            Assert.IsFalse(program.IsCompiled);
            Assert.AreEqual(1, engine.CompileCount);

            Assert.AreEqual(9.0, engine.Evaluate(Expr, ParameterSets.XY(4, 1)));

            Assert.IsTrue(program.IsCompiled);
            Assert.AreEqual(2, engine.CompileCount);
            Assert.AreEqual(2, engine.PrepareCount);
        }

        [TestMethod]
        public void EvaluateTwiceThenCompile_SharesCompiledProgram_NoNewPrepareOrCompile_Test()
        {
            var engine = PinnedEngine.Create();
            engine.Evaluate(Expr, ParameterSets.XY(1, 1));
            engine.Evaluate(Expr, ParameterSets.XY(2, 1));
            var cached = CachedPrograms.Peek(engine, Expr);
            Assert.IsTrue(cached.IsCompiled);

            var handle = engine.Compile(Expr);

            Assert.AreSame(cached, handle.Program, "A compiled cache entry must be shared by Compile (A-9).");
            Assert.IsTrue(handle.IsCompiled);
            Assert.AreEqual(1, engine.PrepareCount);
            Assert.AreEqual(1, engine.CompileCount);
            Assert.AreEqual(1, engine.CachedExpressionCount);
            Assert.AreEqual(11.0, handle.Evaluate(ParameterSets.XY(5, 1)));
        }

        [TestMethod]
        public void EvaluateOnceThenCompile_CompilesCachedProgramInPlace_WithoutUseCountChange_Test()
        {
            var engine = PinnedEngine.Create();
            engine.Evaluate(Expr, ParameterSets.XY(1, 1));
            var cached = CachedPrograms.Peek(engine, Expr);
            Assert.IsFalse(cached.IsCompiled);

            var handle = engine.Compile(Expr);

            Assert.AreSame(cached, handle.Program, "architecture.md A-9 extension: Compile reuses a cached not-yet-compiled program.");
            Assert.IsTrue(handle.IsCompiled);
            Assert.AreEqual(1, cached.UseCount, "Compile peeks without recording a use.");
            Assert.AreEqual(1, engine.PrepareCount);
            Assert.AreEqual(1, engine.CompileCount);
            Assert.AreEqual(1, engine.CachedExpressionCount);
        }

        [TestMethod]
        public void ReentrantSecondUse_TiersUpInsideCustomFunction_WithoutTopLevelUse_Test()
        {
            const string subRule = "z * 3 + 1";
            var engine = PinnedEngine.Create();
            var observedDepth = -1;

            engine.RegisterFunction(
                "rule",
                args =>
                {
                    observedDepth = EvaluationContext.CurrentDepth;
                    return engine.Evaluate(subRule, ParameterSets.Of("z", args[0]));
                });

            Assert.AreEqual(7.0, engine.Evaluate("rule(2)"));
            Assert.IsTrue(observedDepth > 0, "The sub-rule must really be evaluated re-entrantly.");
            Assert.IsFalse(CachedPrograms.Peek(engine, subRule).IsCompiled, "1st (re-entrant) use must not compile.");

            Assert.AreEqual(10.0, engine.Evaluate("rule(3)"));

            var program = CachedPrograms.Peek(engine, subRule);
            Assert.IsTrue(program.IsCompiled, "2nd re-entrant use with sufficient headroom must compile (A-1).");
            Assert.AreEqual(CompileOutcome.Compiled, program.LastOutcome);
        }

        [TestMethod]
        public void ReentrantUse_InsufficientHeadroom_DefersNotFails_ThenShallowCallsCompile_Test()
        {
            const string inner = "z + 1";
            var engine = PinnedEngine.Create(new MathRuleEngineOptions { MaxEvaluationDepth = 64 });

            engine.RegisterFunction(
                "deep",
                args => engine.Evaluate(inner, ParameterSets.Of("z", 5.0)));

            var outer = ExpressionText.ZeroPadded("deep()", 40);

            Assert.AreEqual(6.0, engine.Evaluate(outer));
            Assert.AreEqual(6.0, engine.Evaluate(outer));
            Assert.AreEqual(6.0, engine.Evaluate(outer));

            var program = CachedPrograms.Peek(engine, inner);
            Assert.AreEqual(CompileState.NotCompiled, program.State, "Insufficient headroom is transient: never Failed.");
            Assert.IsFalse(program.IsCompiled);

            Assert.AreEqual(2.0, engine.Evaluate(inner, ParameterSets.Of("z", 1.0)));
            Assert.AreEqual(3.0, engine.Evaluate(inner, ParameterSets.Of("z", 2.0)));

            Assert.IsTrue(program.IsCompiled, "Later calls with headroom must compile the deferred program.");
        }

        [DataTestMethod]
        [DataRow(1)]
        [DataRow(3)]
        [DataRow(1024)]
        public void CompilationThreshold_IsRespectedExactly_Test(int threshold)
        {
            var engine = PinnedEngine.Create(new MathRuleEngineOptions { CompilationThreshold = threshold });

            for (var use = 1; use <= threshold; use++)
            {
                Assert.AreEqual(use * 2.0 + 1, engine.Evaluate(Expr, ParameterSets.XY(use, 1)));

                var program = CachedPrograms.Peek(engine, Expr);

                if (use < threshold)
                {
                    Assert.IsFalse(program.IsCompiled, "Compiled too early at use " + use + " of threshold " + threshold);
                    Assert.AreEqual(0, engine.CompileCount);
                }
                else
                {
                    Assert.IsTrue(program.IsCompiled, "Not compiled at use " + use + " == threshold");
                    Assert.AreEqual(1, engine.CompileCount);
                }
            }

            Assert.AreEqual(1, engine.PrepareCount);
        }

        [TestMethod]
        public void EnableCompilationFalse_NeverCompiles_CachesAst_CompileReturnsInterpretedHandle_Test()
        {
            var engine = PinnedEngine.Create(new MathRuleEngineOptions { EnableCompilation = false });

            for (var i = 1; i <= 10; i++)
                Assert.AreEqual(i * 2.0 + 1, engine.Evaluate(Expr, ParameterSets.XY(i, 1)));

            Assert.AreEqual(1, engine.PrepareCount, "The AST must be cached.");
            Assert.AreEqual(0, engine.CompileCount);
            Assert.IsFalse(CachedPrograms.Peek(engine, Expr).IsCompiled);

            var handle = engine.Compile(Expr);

            Assert.IsFalse(handle.IsCompiled);
            Assert.AreEqual(CompileOutcome.None, handle.Program.LastOutcome);
            Assert.AreEqual(21.0, handle.Evaluate(ParameterSets.XY(10, 1)));
            Assert.AreEqual(0, engine.CompileCount);
        }

        [TestMethod]
        public void CacheCapacityZero_CompileStillCompiles_EvaluateNeverCompiles_Test()
        {
            var engine = PinnedEngine.Create(new MathRuleEngineOptions { ExpressionCacheCapacity = 0 });

            var handle = engine.Compile(Expr);

            Assert.IsTrue(handle.IsCompiled);
            Assert.AreEqual(1, engine.CompileCount);
            Assert.AreEqual(1, engine.PrepareCount);

            for (var i = 1; i <= 5; i++)
                Assert.AreEqual(i * 2.0 + 1, engine.Evaluate(Expr, ParameterSets.XY(i, 1)));

            Assert.AreEqual(0, engine.CachedExpressionCount);
            Assert.AreEqual(1, engine.CompileCount, "Evaluate on a capacity-0 engine must never compile.");
            Assert.AreEqual(1, engine.PrepareCount, "The capacity-0 v2.0.0 path does not count as Prepare.");

            Assert.IsFalse(CachedPrograms.IsCached(engine, Expr));
        }

        [TestMethod]
        public void HeightCapExceeded_ProgramFailed_NotReattemptedOnLaterCalls_Test()
        {
            var engine = PinnedEngine.Create();
            var chain = ExpressionText.FlatChain(257);

            Assert.AreEqual(257.0, engine.Evaluate(chain));
            Assert.AreEqual(257.0, engine.Evaluate(chain));

            var program = CachedPrograms.Peek(engine, chain);
            Assert.AreEqual(CompileState.Failed, program.State);
            Assert.AreEqual(CompileOutcome.HeightCapExceeded, program.LastOutcome);
            Assert.AreEqual(0, engine.CompileCount, "A height-cap abort is not a compile attempt.");

            for (var i = 0; i < 10; i++)
                Assert.AreEqual(257.0, engine.Evaluate(chain));

            Assert.AreEqual(0, engine.CompileCount);
            Assert.AreEqual(CompileState.Failed, program.State);
            Assert.AreEqual(CompileOutcome.HeightCapExceeded, program.LastOutcome);
            Assert.AreEqual(1, engine.PrepareCount);
        }

        [TestMethod]
        public void Height257_EagerEvaluate_ReachesHeightCapPath_FallsBackWithInterpreterResult_Test()
        {
            var options = new MathRuleEngineOptions { CompilationThreshold = 1 };
            options.ThrowOnCompileFault = true;
            var engine = PinnedEngine.Create(options);
            var chain = ExpressionText.FlatChain(257);

            var result = engine.Evaluate(chain);

            BitAssert.AreEqualDescribed(PinnedEngine.Interpreter().Evaluate(chain, null), result, "Height-257 fallback result");
            var program = CachedPrograms.Peek(engine, chain);
            Assert.AreEqual(CompileOutcome.HeightCapExceeded, program.LastOutcome);
            Assert.AreEqual(CompileState.Failed, program.State);
            Assert.AreEqual(0, engine.CompileCount);
        }

        [TestMethod]
        public void Height256_EagerEvaluate_Compiles_Test()
        {
            var engine = PinnedEngine.Create(new MathRuleEngineOptions { CompilationThreshold = 1 });
            var chain = ExpressionText.FlatChain(256);

            Assert.AreEqual(256.0, engine.Evaluate(chain));

            var program = CachedPrograms.Peek(engine, chain);
            Assert.IsTrue(program.IsCompiled);
            Assert.AreEqual(CompileOutcome.Compiled, program.LastOutcome);
            Assert.AreEqual(1, engine.CompileCount);
        }

        [TestMethod]
        public void NodeCountAboveMaxTierUpNodes_EvaluateDoesNotAttempt_ExplicitCompileIsExempt_Test()
        {
            var argCount = ExpressionCompiler.MaxTierUpNodes;
            Assert.IsTrue(argCount <= 4096, "Test assumption: MaxTierUpNodes fits under the MaxFunctionArgs ceiling.");

            var options = new MathRuleEngineOptions
            {
                MaxFunctionArgs = argCount,
                MaxNodes = 65536,
                MaxTokens = 65536,
                MaxExpressionLength = 65536
            };
            var engine = PinnedEngine.Create(options);
            var expression = ExpressionText.SumOfOnes(argCount);

            Assert.AreEqual((double)argCount, engine.Evaluate(expression));
            Assert.AreEqual((double)argCount, engine.Evaluate(expression));

            var program = CachedPrograms.Peek(engine, expression);
            Assert.AreEqual(argCount + 1, program.NodeCount);
            Assert.AreEqual(CompileState.NotCompiled, program.State);
            Assert.AreEqual(CompileOutcome.None, program.LastOutcome);
            Assert.AreEqual(0, engine.CompileCount, "Evaluate must not attempt to compile a program above MaxTierUpNodes.");

            var handle = engine.Compile(expression);

            Assert.IsTrue(handle.IsCompiled, "Explicit Compile is exempt from the MaxTierUpNodes gate.");
            Assert.AreEqual((double)argCount, handle.Evaluate());
        }

        [TestMethod]
        public void FailedByCompileFault_NoNewAttemptOnLaterCalls_Test()
        {
            var injectorCalls = 0;
            var options = new MathRuleEngineOptions();
            options.CompileFaultInjector = lambda =>
            {
                Interlocked.Increment(ref injectorCalls);
                throw new InvalidProgramException("injected");
            };
            var engine = PinnedEngine.Create(options);

            Assert.AreEqual(3.0, engine.Evaluate(Expr, ParameterSets.XY(1, 1)));
            Assert.AreEqual(5.0, engine.Evaluate(Expr, ParameterSets.XY(2, 1)), "A compile fault must never reach the caller in production configuration.");

            var program = CachedPrograms.Peek(engine, Expr);
            Assert.AreEqual(CompileState.Failed, program.State);
            Assert.AreEqual(CompileOutcome.CompileFault, program.LastOutcome);

            for (var i = 0; i < 10; i++)
                Assert.AreEqual(i * 2.0 + 1, engine.Evaluate(Expr, ParameterSets.XY(i, 1)));

            Assert.AreEqual(1, injectorCalls, "A Failed program must never be re-attempted.");
            Assert.AreEqual(CompileOutcome.CompileFault, program.LastOutcome);
            Assert.AreEqual(CompileState.Failed, program.State);
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void InjectedInsufficientStack_IsTransient_DefersThenCompiles_Test(bool throwOnCompileFault)
        {
            var injectorCalls = 0;
            var options = new MathRuleEngineOptions();
            options.ThrowOnCompileFault = throwOnCompileFault;
            options.CompileFaultInjector = lambda =>
            {
                if (Interlocked.Increment(ref injectorCalls) == 1)
                    throw new InsufficientExecutionStackException();
            };
            var engine = PinnedEngine.Create(options);

            Assert.AreEqual(3.0, engine.Evaluate(Expr, ParameterSets.XY(1, 1)));
            Assert.AreEqual(5.0, engine.Evaluate(Expr, ParameterSets.XY(2, 1)), "A transient stack shortage must be interpreted, never thrown.");

            var program = CachedPrograms.Peek(engine, Expr);
            Assert.AreEqual(CompileOutcome.InsufficientStack, program.LastOutcome);
            Assert.AreEqual(CompileState.NotCompiled, program.State, "InsufficientStack is transient, not Failed.");
            Assert.IsFalse(program.IsCompiled);

            Assert.AreEqual(7.0, engine.Evaluate(Expr, ParameterSets.XY(3, 1)));
            Assert.AreEqual(9.0, engine.Evaluate(Expr, ParameterSets.XY(4, 1)));

            Assert.IsTrue(program.IsCompiled, "A later call must compile once the stack shortage is gone.");
            Assert.AreEqual(CompileOutcome.Compiled, program.LastOutcome);
            Assert.AreEqual(2, injectorCalls);
        }

        [TestMethod]
        public void SimulateNoDynamicCode_SecondEvaluate_FailsToAstInterpreter_Test()
        {
            var options = new MathRuleEngineOptions();
            options.SimulateNoDynamicCode = true;
            var engine = PinnedEngine.Create(options);

            Assert.AreEqual(3.0, engine.Evaluate(Expr, ParameterSets.XY(1, 1)));
            Assert.AreEqual(5.0, engine.Evaluate(Expr, ParameterSets.XY(2, 1)));
            Assert.AreEqual(7.0, engine.Evaluate(Expr, ParameterSets.XY(3, 1)));

            var program = CachedPrograms.Peek(engine, Expr);
            Assert.AreEqual(CompileOutcome.NoDynamicCode, program.LastOutcome);
            Assert.AreEqual(CompileState.Failed, program.State);
            Assert.IsNull(program.Compiled, "The no-dynamic-code fallback is the AST interpreter, not interpreted trees (C5).");
        }

        [TestMethod]
        public void SimulateNoDynamicCode_Compile_ReturnsInterpretedHandle_NeverThrows_Test()
        {
            var options = new MathRuleEngineOptions();
            options.SimulateNoDynamicCode = true;
            var engine = PinnedEngine.Create(options);

            var handle = engine.Compile(Expr);

            Assert.IsFalse(handle.IsCompiled);
            Assert.AreEqual(CompileOutcome.NoDynamicCode, handle.Program.LastOutcome);
            Assert.AreEqual(CompileState.Failed, handle.Program.State);
            Assert.AreEqual(AssertNoThrowEvaluate(handle, ParameterSets.XY(4, 2)), PinnedEngine.Interpreter().Evaluate(Expr, ParameterSets.XY(4, 2)));
        }

        private static double AssertNoThrowEvaluate(CompiledExpression handle, IDictionary<string, object> parameters)
        {
            return handle.Evaluate(parameters);
        }

        [TestMethod]
        [Timeout(30000)]
        public void CompileRaceLoser_InterpretsThatCall_WinnerCompilesOnce_Test()
        {
            var entered = new ManualResetEventSlim(false);
            var release = new ManualResetEventSlim(false);
            var injectorCalls = 0;
            var options = new MathRuleEngineOptions();
            options.CompileFaultInjector = lambda =>
            {
                if (Interlocked.Increment(ref injectorCalls) == 1)
                {
                    entered.Set();
                    release.Wait(TimeSpan.FromSeconds(5));
                }
            };
            var engine = PinnedEngine.Create(options);

            Assert.AreEqual(3.0, engine.Evaluate(Expr, ParameterSets.XY(1, 1)));

            var winnerResult = double.NaN;
            Exception winnerError = null;
            var winner = new Thread(() =>
            {
                try
                {
                    winnerResult = engine.Evaluate(Expr, ParameterSets.XY(2, 1));
                }
                catch (Exception ex)
                {
                    winnerError = ex;
                }
            });
            winner.IsBackground = true;

            double loserResult;
            CompileState stateWhileWinnerCompiles;
            try
            {
                winner.Start();
                Assert.IsTrue(entered.Wait(TimeSpan.FromSeconds(5)), "The winner never reached the compile step.");

                loserResult = engine.Evaluate(Expr, ParameterSets.XY(5, 3));
                stateWhileWinnerCompiles = CachedPrograms.Peek(engine, Expr).State;
            }
            finally
            {
                release.Set();
            }

            Assert.IsTrue(winner.Join(TimeSpan.FromSeconds(10)), "The winner thread did not finish.");

            Assert.AreEqual(13.0, loserResult, "The race loser must be served by the interpreter.");
            Assert.AreEqual(CompileState.NotCompiled, stateWhileWinnerCompiles, "The loser must not wait for the winner's compile.");
            Assert.IsNull(winnerError, winnerError == null ? string.Empty : winnerError.ToString());
            Assert.AreEqual(5.0, winnerResult);

            var program = CachedPrograms.Peek(engine, Expr);
            Assert.IsTrue(program.IsCompiled);
            Assert.AreNotEqual(CompileOutcome.Busy, program.LastOutcome, "Busy is never stored.");
            Assert.AreEqual(1, engine.CompileCount);
            Assert.AreEqual(1, injectorCalls);
        }

        [TestMethod]
        public void EntryEvictedBetweenFirstAndSecondUse_RestartsUseCount_Test()
        {
            const int capacity = 4;
            var engine = PinnedEngine.Create(new MathRuleEngineOptions { ExpressionCacheCapacity = capacity });

            Assert.AreEqual(3.0, engine.Evaluate(Expr, ParameterSets.XY(1, 1)));

            for (var i = 0; i < 2 * capacity; i++)
                engine.Evaluate("100 + " + i);

            Assert.IsFalse(CachedPrograms.IsCached(engine, Expr), "Test setup: the entry must be evicted.");
            var preparesBefore = engine.PrepareCount;

            Assert.AreEqual(5.0, engine.Evaluate(Expr, ParameterSets.XY(2, 1)));

            Assert.AreEqual(preparesBefore + 1, engine.PrepareCount, "An evicted entry must be re-Prepared.");
            var program = CachedPrograms.Peek(engine, Expr);
            Assert.AreEqual(1, program.UseCount, "Eviction restarts the use count.");
            Assert.IsFalse(program.IsCompiled, "The 2nd overall use after eviction is a 1st use: no compile.");
            Assert.AreEqual(0, engine.CompileCount);

            Assert.AreEqual(7.0, engine.Evaluate(Expr, ParameterSets.XY(3, 1)));

            Assert.IsTrue(program.IsCompiled);
            Assert.AreEqual(1, engine.CompileCount);
        }
    }
}
