using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using MathExprEngine;
using MathExprEngine.Exceptions;
using MathExprEngine.Models;
using MathExprEngineTests.Helpers.Concurrency;
using MathExprEngineTests.Helpers.Engines;
using MathExprEngineTests.Helpers.Inputs;
using MathExprEngineTests.Helpers.TestBases;

namespace MathExprEngineTests.Tests
{
    [TestClass]
    public class CompiledConcurrencyTests : DepthGuardedTestBase
    {
        private const int ThreadCount = 16;

        public TestContext TestContext { get; set; }

        [TestMethod]
        [Timeout(60000)]
        public void SixteenThreads_SameKeyTierUp_CompileCountIsExactlyOne_Test()
        {
            const string expression = "x * 3 + y";
            const int iterations = 200;
            var oracle = PinnedEngine.Interpreter();
            var expected = new double[ThreadCount, 7];
            for (var t = 0; t < ThreadCount; t++)
                for (var y = 0; y < 7; y++)
                    expected[t, y] = oracle.Evaluate(expression, ParameterSets.XY(t, y));

            var engine = PinnedEngine.Create();
            var errors = new ConcurrentQueue<Exception>();
            var wrong = 0;

            WorkerThreads.RunTogether(
                ThreadCount,
                t =>
                {
                    for (var i = 0; i < iterations; i++)
                    {
                        var y = i % 7;
                        var result = engine.Evaluate(expression, ParameterSets.XY(t, y));
                        if (BitConverter.DoubleToInt64Bits(result) != BitConverter.DoubleToInt64Bits(expected[t, y]))
                            Interlocked.Increment(ref wrong);
                    }
                },
                errors);

            Assert.AreEqual(0, errors.Count, WorkerThreads.Describe(errors));
            Assert.AreEqual(0, wrong, "Wrong results under contention.");
            Assert.AreEqual(1, engine.CompileCount, "Exactly one compilation of the shared key under contention.");
            Assert.IsTrue(engine.PrepareCount >= 1 && engine.PrepareCount <= ThreadCount, "PrepareCount " + engine.PrepareCount + " must be within 1..16.");

            var program = CachedPrograms.Peek(engine, expression);
            Assert.IsTrue(program.IsCompiled);
        }

        [TestMethod]
        [Timeout(60000)]
        public void SixteenThreads_EvictionChurn_Capacity16_40Keys_AllResultsCorrect_BoundHolds_Test()
        {
            const int keyCount = 40;
            const int iterations = 2000;
            var keys = Enumerable.Range(0, keyCount).Select(k => "x * " + (k + 1) + " - " + k + " / 2").ToArray();
            var oracle = PinnedEngine.Interpreter();
            var expected = new double[ThreadCount, keyCount];
            for (var t = 0; t < ThreadCount; t++)
                for (var k = 0; k < keyCount; k++)
                    expected[t, k] = oracle.Evaluate(keys[k], ParameterSets.X(t + 0.5));

            var engine = PinnedEngine.Create(new MathRuleEngineOptions { ExpressionCacheCapacity = 16 });
            var errors = new ConcurrentQueue<Exception>();
            var wrong = 0;
            var boundViolations = 0;

            WorkerThreads.RunTogether(
                ThreadCount,
                t =>
                {
                    var parameters = ParameterSets.X(t + 0.5);

                    for (var i = 0; i < iterations; i++)
                    {
                        var k = (t * 7 + i * 3) % keyCount;
                        var result = engine.Evaluate(keys[k], parameters);
                        if (BitConverter.DoubleToInt64Bits(result) != BitConverter.DoubleToInt64Bits(expected[t, k]))
                            Interlocked.Increment(ref wrong);

                        if ((i & 63) == 0 && engine.CachedExpressionCount > 16)
                            Interlocked.Increment(ref boundViolations);
                    }
                },
                errors);

            TestContext.WriteLine("Churn: prepares " + engine.PrepareCount + ", compiles " + engine.CompileCount + ", entries " + engine.CachedExpressionCount);
            Assert.AreEqual(0, errors.Count, WorkerThreads.Describe(errors));
            Assert.AreEqual(0, wrong, "Wrong results under eviction churn.");
            Assert.AreEqual(0, boundViolations, "CachedExpressionCount exceeded the capacity during churn.");
            Assert.IsTrue(engine.CachedExpressionCount <= 16);
        }

        [TestMethod]
        [Timeout(60000)]
        public void OneHandle_SixteenThreads_DistinctParameters_EveryResultCorrect_Test()
        {
            const string expression = "a * 1000 + b";
            const int iterations = 10000;
            var engine = PinnedEngine.Create();
            var handle = engine.Compile(expression);
            Assert.IsTrue(handle.IsCompiled);
            var errors = new ConcurrentQueue<Exception>();
            var wrong = 0;

            WorkerThreads.RunTogether(
                ThreadCount,
                t =>
                {
                    for (var i = 0; i < iterations; i++)
                    {
                        var b = (double)(i % 1000);
                        var result = handle.Evaluate(ParameterSets.Of("a", (double)t, "b", b));
                        if (result != t * 1000.0 + b)
                            Interlocked.Increment(ref wrong);
                    }
                },
                errors);

            Assert.AreEqual(0, errors.Count, WorkerThreads.Describe(errors));
            Assert.AreEqual(0, wrong, "A parameter value leaked between threads sharing one handle.");
            Assert.AreEqual(1, engine.CompileCount);
        }

        [TestMethod]
        [Timeout(60000)]
        public void OneHandle_SixteenThreads_WhileRegisterFunctionRaces_ResultsFromOldOrNewOnly_Test()
        {
            const int iterations = 2000;
            Func<double[], double> oldImpl = a => a[0] * 2;
            Func<double[], double> newImpl = a => a[0] * 3;
            var engine = PinnedEngine.Create();
            engine.RegisterFunction("f", oldImpl);
            var handle = engine.Compile("f(x) + 1");
            Assert.IsTrue(handle.IsCompiled);
            var errors = new ConcurrentQueue<Exception>();
            var unexpected = 0;
            var evaluatorsDone = 0;
            var registrations = 0;

            WorkerThreads.RunTogether(
                ThreadCount + 1,
                t =>
                {
                    if (t == ThreadCount)
                    {
                        var useNew = false;
                        while (Volatile.Read(ref evaluatorsDone) < ThreadCount)
                        {
                            useNew = !useNew;
                            engine.RegisterFunction(useNew ? "F" : "f", useNew ? newImpl : oldImpl);
                            registrations++;
                        }

                        return;
                    }

                    try
                    {
                        for (var i = 0; i < iterations; i++)
                        {
                            var x = (double)(t + 1);
                            var result = handle.Evaluate(ParameterSets.X(x));
                            if (result != x * 2 + 1 && result != x * 3 + 1)
                                Interlocked.Increment(ref unexpected);
                        }
                    }
                    finally
                    {
                        Interlocked.Increment(ref evaluatorsDone);
                    }
                },
                errors);

            TestContext.WriteLine("Registrations during the race: " + registrations);
            Assert.AreEqual(0, errors.Count, WorkerThreads.Describe(errors));
            Assert.AreEqual(0, unexpected, "Every result must come from the old or the new implementation.");
            Assert.IsTrue(registrations > 0);
            Assert.AreEqual(1, engine.CompileCount, "Late binding must not trigger recompilation.");
        }

        [TestMethod]
        [Timeout(60000)]
        public void DepthLimitFailureOnOneThread_DoesNotChangeBudgetOfOtherThreads_Test()
        {
            const int iterations = 500;
            var engine = PinnedEngine.Create(new MathRuleEngineOptions { MaxEvaluationDepth = 20 });
            var atLimit = ExpressionText.FlatChain(20);
            var overLimit = ExpressionText.FlatChain(21);
            var errors = new ConcurrentQueue<Exception>();
            var depthFailures = 0;
            var successes = 0;

            WorkerThreads.RunTogether(
                4,
                t =>
                {
                    for (var i = 0; i < iterations; i++)
                    {
                        if (t % 2 == 0)
                        {
                            try
                            {
                                engine.Evaluate(overLimit);
                            }
                            catch (MathRuleEngineException)
                            {
                                Interlocked.Increment(ref depthFailures);
                            }

                            if (EvaluationContext.CurrentDepth != 0)
                                throw new InvalidOperationException("Depth leaked on the failing thread: " + EvaluationContext.CurrentDepth);
                        }
                        else
                        {
                            if (engine.Evaluate(atLimit) == 20.0)
                                Interlocked.Increment(ref successes);
                        }
                    }
                },
                errors);

            Assert.AreEqual(0, errors.Count, WorkerThreads.Describe(errors));
            Assert.AreEqual(2 * iterations, depthFailures);
            Assert.AreEqual(2 * iterations, successes);
            Assert.AreEqual(20.0, engine.Evaluate(atLimit), "The test thread keeps its full budget.");
        }
    }
}
