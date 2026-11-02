using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MathExprEngine;
using MathExprEngine.Exceptions;
using MathExprEngineTests.Helpers.Inputs;

namespace MathExprEngineTests.Tests
{
    [TestClass]
    public class LimitsAndConcurrencyTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        [Timeout(30000)]
        public void SharedEngine_ConcurrentEvaluateWithDistinctParameterSets_ProducesZeroWrongAnswersAndZeroExceptions_Test()
        {
            const int threadCount = 8;
            const int iterationsPerThread = 3000;

            var engine = new MathRuleEngine();

            var wrongAnswers = 0;
            var exceptionCount = 0;
            var sampleExceptions = new ConcurrentBag<string>();

            var threads = new Thread[threadCount];

            for (var t = 0; t < threadCount; t++)
            {
                var threadIndex = t;

                threads[t] = new Thread(() =>
                {
                    for (var i = 0; i < iterationsPerThread; i++)
                    {
                        var a = (double)threadIndex;
                        var b = (double)i;
                        var expected = a * 100000.0 + b;

                        var parameters = new Dictionary<string, object>
                        {
                            { "a", a },
                            { "b", b }
                        };

                        try
                        {
                            var result = engine.Evaluate("a * 100000 + b", parameters);

                            if (result != expected)
                                Interlocked.Increment(ref wrongAnswers);
                        }
                        catch (Exception ex)
                        {
                            Interlocked.Increment(ref exceptionCount);
                            sampleExceptions.Add(ex.GetType().Name + ": " + ex.Message);
                        }
                    }
                });
            }

            foreach (var thread in threads) thread.Start();
            foreach (var thread in threads) thread.Join();

            var totalEvaluations = threadCount * iterationsPerThread;
            TestContext.WriteLine(
                $"Concurrency result: {totalEvaluations} evaluations, {exceptionCount} exceptions, {wrongAnswers} wrong answers.");

            Assert.AreEqual(
                0,
                exceptionCount,
                $"Expected 0 exceptions from concurrent Evaluate calls on a shared engine, got {exceptionCount} " +
                $"out of {totalEvaluations}. Sample: {string.Join(" | ", sampleExceptions.Take(5))}");

            Assert.AreEqual(
                0,
                wrongAnswers,
                $"Expected 0 silently wrong answers from concurrent Evaluate calls on a shared engine, got " +
                $"{wrongAnswers} out of {totalEvaluations}. A non-zero count here means one thread's " +
                $"parameters leaked into another thread's evaluation.");
        }

        [TestMethod]
        public void CustomFunction_ReentrantEvaluateCall_DoesNotCorruptCallersParameters_Test()
        {
            var engine = new MathRuleEngine();

            engine.RegisterFunction(
                "reenter",
                args => engine.Evaluate("y + 1", new Dictionary<string, object> { { "y", 5.0 } }));

            var parameters = new Dictionary<string, object> { { "x", 10.0 } };

            var result = engine.Evaluate("x + reenter() + x", parameters);

            Assert.AreEqual(26.0, result);
        }

        [TestMethod]
        public void CustomFunction_ReentrantEvaluateCall_SameParameterNameIsIsolatedFromCaller_Test()
        {
            var engine = new MathRuleEngine();

            engine.RegisterFunction(
                "innerX",
                args => engine.Evaluate("x", new Dictionary<string, object> { { "x", 999.0 } }));

            var parameters = new Dictionary<string, object> { { "x", 10.0 } };

            var result = engine.Evaluate("x + innerX() + x", parameters);

            Assert.AreEqual(1019.0, result);
        }

        [TestMethod]
        [Timeout(30000)]
        public void RegisterFunction_ConcurrentWithInFlightEvaluate_DoesNotThrowOrCorrupt_Test()
        {
            var engine = new MathRuleEngine();
            const int iterations = 5000;

            var evaluateExceptions = 0;
            var evaluateWrongAnswers = 0;
            var registerExceptions = 0;

            var evalTask = Task.Run(() =>
            {
                for (var i = 0; i < iterations; i++)
                {
                    try
                    {
                        var result = engine.Evaluate("sqrt(sum(1,2,3,4))");

                        if (Math.Abs(result - Math.Sqrt(10)) > 1e-9)
                            Interlocked.Increment(ref evaluateWrongAnswers);
                    }
                    catch (Exception)
                    {
                        Interlocked.Increment(ref evaluateExceptions);
                    }
                }
            });

            var registerTask = Task.Run(() =>
            {
                for (var i = 0; i < iterations; i++)
                {
                    try
                    {
                        var capturedValue = (double)i;
                        engine.RegisterFunction("liveRegistered", args => capturedValue);
                    }
                    catch (Exception)
                    {
                        Interlocked.Increment(ref registerExceptions);
                    }
                }
            });

            Task.WaitAll(evalTask, registerTask);

            TestContext.WriteLine(
                $"Register-during-evaluate result: {evaluateExceptions} evaluate exceptions, " +
                $"{evaluateWrongAnswers} evaluate wrong answers, {registerExceptions} register exceptions.");

            Assert.AreEqual(0, evaluateExceptions, "Evaluate must not throw while a function is being registered concurrently.");
            Assert.AreEqual(0, evaluateWrongAnswers, "Evaluate must keep returning the correct result while registration races it.");
            Assert.AreEqual(0, registerExceptions, "RegisterFunction must not throw while Evaluate calls are in flight.");

            var finalResult = engine.Evaluate("liveRegistered()");
            Assert.AreEqual((double)(iterations - 1), finalResult);
        }

        [TestMethod]
        public void MaxExpressionLength_AtLimit_Succeeds_Test()
        {
            var options = new MathRuleEngineOptions { MaxExpressionLength = 10 };
            var engine = new MathRuleEngine(options);

            var expression = new string('+', 9) + "5";
            Assert.AreEqual(10, expression.Length);

            var result = engine.Evaluate(expression);

            Assert.AreEqual(5.0, result);
        }

        [TestMethod]
        public void MaxExpressionLength_OverLimit_Throws_Test()
        {
            var options = new MathRuleEngineOptions { MaxExpressionLength = 10 };
            var engine = new MathRuleEngine(options);

            var expression = new string('+', 10) + "5";
            Assert.AreEqual(11, expression.Length);

            Assert.ThrowsException<MathRuleEngineException>(() => engine.Evaluate(expression));
        }

        [TestMethod]
        public void MaxTokens_AtLimit_Succeeds_Test()
        {
            var options = new MathRuleEngineOptions { MaxTokens = 6 };
            var engine = new MathRuleEngine(options);

            var result = engine.Evaluate("50%%%%");

            Assert.AreEqual(0.0000005, result, 1e-12);
        }

        [TestMethod]
        public void MaxTokens_OverLimit_Throws_Test()
        {
            var options = new MathRuleEngineOptions { MaxTokens = 6 };
            var engine = new MathRuleEngine(options);

            Assert.ThrowsException<MathRuleEngineException>(() => engine.Evaluate("50%%%%%%"));
        }

        [TestMethod]
        public void MaxNodes_AtLimit_Succeeds_Test()
        {
            var options = new MathRuleEngineOptions { MaxNodes = 5 };
            var engine = new MathRuleEngine(options);

            var result = engine.Evaluate("50%%%%");

            Assert.AreEqual(0.0000005, result, 1e-12);
        }

        [TestMethod]
        public void MaxNodes_OverLimit_Throws_Test()
        {
            var options = new MathRuleEngineOptions { MaxNodes = 5 };
            var engine = new MathRuleEngine(options);

            Assert.ThrowsException<MathRuleEngineException>(() => engine.Evaluate("50%%%%%"));
        }

        [TestMethod]
        public void MaxDepth_AtLimit_Succeeds_Test()
        {
            var options = new MathRuleEngineOptions { MaxDepth = 10 };
            var engine = new MathRuleEngine(options);

            var result = engine.Evaluate(new string('-', 8) + "5");

            Assert.AreEqual(5.0, result);
        }

        [TestMethod]
        public void MaxDepth_OverLimit_Throws_Test()
        {
            var options = new MathRuleEngineOptions { MaxDepth = 10 };
            var engine = new MathRuleEngine(options);

            Assert.ThrowsException<MathRuleEngineException>(() => engine.Evaluate(new string('-', 9) + "5"));
        }

        [TestMethod]
        public void MaxEvaluationDepth_AtLimit_Succeeds_Test()
        {
            var options = new MathRuleEngineOptions { MaxEvaluationDepth = 10 };
            var engine = new MathRuleEngine(options);

            var expression = ExpressionText.FlatChain(10);

            var result = engine.Evaluate(expression);

            Assert.AreEqual(10.0, result);
        }

        [TestMethod]
        public void MaxEvaluationDepth_OverLimit_Throws_Test()
        {
            var options = new MathRuleEngineOptions { MaxEvaluationDepth = 10 };
            var engine = new MathRuleEngine(options);

            var expression = ExpressionText.FlatChain(11);

            Assert.ThrowsException<MathRuleEngineException>(() => engine.Evaluate(expression));
        }

        [TestMethod]
        public void MaxFunctionArgs_AtLimit_Succeeds_Test()
        {
            var options = new MathRuleEngineOptions { MaxFunctionArgs = 3 };
            var engine = new MathRuleEngine(options);

            var result = engine.Evaluate("sum(1,2,3)");

            Assert.AreEqual(6.0, result);
        }

        [TestMethod]
        public void MaxFunctionArgs_OverLimit_Throws_Test()
        {
            var options = new MathRuleEngineOptions { MaxFunctionArgs = 3 };
            var engine = new MathRuleEngine(options);

            Assert.ThrowsException<MathRuleEngineException>(() => engine.Evaluate("sum(1,2,3,4)"));
        }

        [TestMethod]
        public void DeeplyNestedParentheses_5000Levels_ThrowsCatchableException_Test()
        {
            var engine = new MathRuleEngine();

            var expression = ExpressionText.NestedParentheses(5000);

            Assert.ThrowsException<MathRuleEngineException>(() => engine.Evaluate(expression));
        }

        [TestMethod]
        public void LongFlatOperatorChain_20000Terms_ThrowsCatchableException_Test()
        {
            var engine = new MathRuleEngine();

            var expression = ExpressionText.FlatChain(20000);

            Assert.ThrowsException<MathRuleEngineException>(() => engine.Evaluate(expression));
        }

        [TestMethod]
        public void FlatOperatorChain_JustOverDefaultEvaluationDepth_ThrowsDuringEvaluationNotParsing_Test()
        {
            var defaults = new MathRuleEngineOptions();
            var engine = new MathRuleEngine();

            var termCount = defaults.MaxEvaluationDepth + 1;
            var expression = ExpressionText.FlatChain(termCount);

            Assert.IsTrue(
                expression.Length < defaults.MaxExpressionLength,
                "Test assumption broken: the expression no longer fits under the default MaxExpressionLength, " +
                "so this test would no longer isolate MaxEvaluationDepth specifically.");

            Assert.ThrowsException<MathRuleEngineException>(() => engine.Evaluate(expression));
        }

        [TestMethod]
        public void LongUnaryChain_20000LeadingMinusSigns_ThrowsCatchableException_Test()
        {
            var engine = new MathRuleEngine();

            var expression = new string('-', 20000) + "1";

            Assert.ThrowsException<MathRuleEngineException>(() => engine.Evaluate(expression));
        }

        [TestMethod]
        public void Constructor_OptionsAndFunctionsOverload_RegistersCustomFunctionAndAppliesOptions_Test()
        {
            var options = new MathRuleEngineOptions { MaxExpressionLength = 50 };
            var functions = new Dictionary<string, Func<double[], double>>
            {
                { "triple", args => args[0] * 3 }
            };

            var engine = new MathRuleEngine(options, functions);

            Assert.AreEqual(9.0, engine.Evaluate("triple(3)"));
            Assert.ThrowsException<MathRuleEngineException>(() => engine.Evaluate(new string('1', 51)));
        }

        [TestMethod]
        public void Constructor_FunctionsOnlyOverload_StillCompilesAndBehaves_Test()
        {
            var functions = new Dictionary<string, Func<double[], double>>
            {
                { "triple", args => args[0] * 3 }
            };

            var engine = new MathRuleEngine(functions);

            Assert.AreEqual(9.0, engine.Evaluate("triple(3)"));
            Assert.AreEqual(3.0, engine.Evaluate("sqrt(9)"));
        }

        [DataTestMethod]
        [DataRow("MaxExpressionLength")]
        [DataRow("MaxTokens")]
        [DataRow("MaxNodes")]
        [DataRow("MaxDepth")]
        [DataRow("MaxEvaluationDepth")]
        [DataRow("MaxFunctionArgs")]
        public void Constructor_InvalidOptionValue_ThrowsArgumentOutOfRangeExceptionNamingTheProperty_Test(string propertyName)
        {
            var options = new MathRuleEngineOptions();

            switch (propertyName)
            {
                case "MaxExpressionLength": options.MaxExpressionLength = 0; break;
                case "MaxTokens": options.MaxTokens = 0; break;
                case "MaxNodes": options.MaxNodes = 0; break;
                case "MaxDepth": options.MaxDepth = 0; break;
                case "MaxEvaluationDepth": options.MaxEvaluationDepth = 0; break;
                case "MaxFunctionArgs": options.MaxFunctionArgs = 0; break;
            }

            var ex = Assert.ThrowsException<ArgumentOutOfRangeException>(() => new MathRuleEngine(options));

            Assert.AreEqual("options", ex.ParamName);
            StringAssert.Contains(ex.Message, propertyName);
        }

        [TestMethod]
        public void AllowNonFiniteResults_Default_RejectsNonFiniteResult_Test()
        {
            var engine = new MathRuleEngine();

            Assert.ThrowsException<MathRuleEngineException>(() => engine.Evaluate("10^400"));
        }

        [TestMethod]
        public void AllowNonFiniteResults_EnabledExplicitly_ReturnsInfinity_Test()
        {
            var options = new MathRuleEngineOptions { AllowNonFiniteResults = true };
            var engine = new MathRuleEngine(options);

            var result = engine.Evaluate("10^400");

            Assert.IsTrue(double.IsPositiveInfinity(result));
        }
    }
}
