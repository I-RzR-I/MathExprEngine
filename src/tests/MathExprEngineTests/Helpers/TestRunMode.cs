using System;
using MathExprEngine;
using MathExprEngineTests.Helpers.Assertions;

namespace MathExprEngineTests.Helpers
{
    [TestClass]
    public class TestRunMode
    {
        public TestContext TestContext { get; set; }

        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            var mode = RunMode.Parse(Environment.GetEnvironmentVariable(RunMode.EnvironmentVariableName));

            RunMode.Initialize(mode);
            MathRuleEngine.TestOptionsOverride = RunMode.Override;

            var line = "MathExprEngine test run mode: " + mode +
                       " (" + RunMode.EnvironmentVariableName + "='" +
                       (Environment.GetEnvironmentVariable(RunMode.EnvironmentVariableName) ?? "<unset>") + "')";
            Console.WriteLine(line);
            context?.WriteLine(line);
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            MathRuleEngine.TestOptionsOverride = null;
        }

        [TestMethod]
        public void RunMode_IsApplied()
        {
            TestContext.WriteLine("Active run mode: " + RunMode.Current);
            Assert.IsTrue(RunMode.IsInitialized, "AssemblyInitialize did not run.");
            Assert.IsNotNull(MathRuleEngine.TestOptionsOverride, "TestOptionsOverride is not installed.");
            Assert.IsFalse(RunMode.IsSuppressed, "Suppress scope leaked into this flow.");

            var defaults = new MathRuleEngineOptions();
            var o = RunMode.ReadEngineOptions(new MathRuleEngine());

            switch (RunMode.Current)
            {
                case TestMode.Interpreter:
                    Assert.IsFalse(o.EnableCompilation);
                    Assert.AreEqual(0, o.ExpressionCacheCapacity);
                    Assert.IsFalse(o.ThrowOnCompileFault);
                    Assert.IsFalse(o.PreferInterpretedExpressionTrees);
                    break;

                case TestMode.Tiered:
                    Assert.IsTrue(o.EnableCompilation);
                    Assert.AreEqual(defaults.CompilationThreshold, o.CompilationThreshold);
                    Assert.AreEqual(2, o.CompilationThreshold);
                    Assert.AreEqual(defaults.ExpressionCacheCapacity, o.ExpressionCacheCapacity);
                    Assert.IsTrue(o.ThrowOnCompileFault);
                    Assert.IsFalse(o.PreferInterpretedExpressionTrees);
                    break;

                case TestMode.Eager:
                    Assert.IsTrue(o.EnableCompilation);
                    Assert.AreEqual(1, o.CompilationThreshold);
                    Assert.AreEqual(defaults.ExpressionCacheCapacity, o.ExpressionCacheCapacity);
                    Assert.IsTrue(o.ThrowOnCompileFault);
                    Assert.IsFalse(o.PreferInterpretedExpressionTrees);
                    break;

                case TestMode.EagerInterpretedTrees:
                    Assert.IsTrue(o.EnableCompilation);
                    Assert.AreEqual(1, o.CompilationThreshold);
                    Assert.AreEqual(defaults.ExpressionCacheCapacity, o.ExpressionCacheCapacity);
                    Assert.IsTrue(o.ThrowOnCompileFault);
                    Assert.IsTrue(o.PreferInterpretedExpressionTrees);
                    break;

                default:
                    Assert.Fail("Unknown mode " + RunMode.Current);
                    break;
            }
        }

        [TestMethod]
        public void RunMode_Suppress_LeavesOptionsUnchanged()
        {
            var requested = new MathRuleEngineOptions
            {
                EnableCompilation = true,
                CompilationThreshold = 7,
                ExpressionCacheCapacity = 33
            };

            MathRuleEngineOptions o;
            using (RunMode.Suppress())
            {
                Assert.IsTrue(RunMode.IsSuppressed);
                o = RunMode.ReadEngineOptions(new MathRuleEngine(requested));
            }

            Assert.IsFalse(RunMode.IsSuppressed);
            Assert.IsTrue(o.EnableCompilation);
            Assert.AreEqual(7, o.CompilationThreshold);
            Assert.AreEqual(33, o.ExpressionCacheCapacity);
            Assert.IsFalse(o.ThrowOnCompileFault);
            Assert.IsFalse(o.PreferInterpretedExpressionTrees);
        }

        [TestMethod]
        public void TestOptionsOverride_CannotChangeResourceLimitsOrNonFiniteSwitch()
        {
            MathRuleEngineOptions o;
            using (RunMode.UseCustomOverride(x =>
                   {
                       x.MaxDepth = 1;
                       x.MaxEvaluationDepth = 1;
                       x.MaxNodes = 1;
                       x.AllowNonFiniteResults = true;
                       x.CompilationThreshold = 5;
                       return x;
                   }))
            {
                o = RunMode.ReadEngineOptions(new MathRuleEngine(new MathRuleEngineOptions()));
            }

            var defaults = new MathRuleEngineOptions();
            Assert.AreEqual(defaults.MaxDepth, o.MaxDepth);
            Assert.AreEqual(defaults.MaxEvaluationDepth, o.MaxEvaluationDepth);
            Assert.AreEqual(defaults.MaxNodes, o.MaxNodes);
            Assert.IsFalse(o.AllowNonFiniteResults);
            Assert.AreEqual(5, o.CompilationThreshold, "One of the five members must be copied.");
        }

        [TestMethod]
        public void TestOptionsOverride_ResultIsValidated()
        {
            using (RunMode.UseCustomOverride(x =>
                   {
                       x.CompilationThreshold = 0;
                       return x;
                   }))
            {
                OptionsValidationAssert.ThrowsOutOfRange(
                    () => new MathRuleEngine(new MathRuleEngineOptions()),
                    "CompilationThreshold must be between 1 and 1024");
            }
        }

        [TestMethod]
        public void RunMode_Parse_UnknownValueThrows_UnsetIsTiered()
        {
            Assert.AreEqual(TestMode.Tiered, RunMode.Parse(null));
            Assert.AreEqual(TestMode.Tiered, RunMode.Parse(string.Empty));
            Assert.AreEqual(TestMode.Eager, RunMode.Parse("Eager"));
            Assert.ThrowsException<InvalidOperationException>(() => RunMode.Parse("eager"));
            Assert.ThrowsException<InvalidOperationException>(() => RunMode.Parse("Interpretor"));
            Assert.ThrowsException<InvalidOperationException>(() => RunMode.Parse(" Tiered"));
        }
    }
}
