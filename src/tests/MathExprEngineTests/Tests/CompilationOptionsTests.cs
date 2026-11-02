using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MathExprEngine;
using MathExprEngine.Compilation;
using MathExprEngineTests.Helpers;
using MathExprEngineTests.Helpers.Assertions;
using MathExprEngineTests.Helpers.Engines;
using MathExprEngineTests.Helpers.Inputs;
using MathExprEngineTests.Helpers.TestBases;

namespace MathExprEngineTests.Tests
{
    [TestClass]
    public class CompilationOptionsTests : DepthGuardedTestBase
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void NewOptions_PublicDefaults_MatchDocumentedValues_Test()
        {
            var options = new MathRuleEngineOptions();

            Assert.AreEqual(1024, options.ExpressionCacheCapacity);
            Assert.IsTrue(options.EnableCompilation);
            Assert.AreEqual(2, options.CompilationThreshold);

            Assert.AreEqual(4096, options.MaxExpressionLength);
            Assert.AreEqual(2048, options.MaxTokens);
            Assert.AreEqual(4096, options.MaxNodes);
            Assert.AreEqual(256, options.MaxDepth);
            Assert.AreEqual(512, options.MaxEvaluationDepth);
            Assert.AreEqual(64, options.MaxFunctionArgs);
            Assert.IsFalse(options.AllowNonFiniteResults);
        }

        [TestMethod]
        public void InternalCeilingsAndSeams_HaveSafeDefaults_Test()
        {
            Assert.AreEqual(65536, MathRuleEngineOptions.ExpressionCacheCapacityCeiling);
            Assert.AreEqual(1024, MathRuleEngineOptions.CompilationThresholdCeiling);
            Assert.AreEqual(2, MathRuleEngineOptions.DefaultCompilationThreshold);
            Assert.AreEqual(256, ExpressionCompiler.MaxCompileHeight);
            Assert.AreEqual(4096, ExpressionCompiler.MaxTierUpNodes);
            Assert.AreEqual(262144, MathExprEngine.Helpers.ExpressionCache.NodeBudget);

            var options = new MathRuleEngineOptions();
            Assert.IsFalse(options.PreferInterpretedExpressionTrees);
            Assert.IsNull(options.CompileFaultInjector);
            Assert.IsFalse(options.SimulateNoDynamicCode);
            Assert.IsFalse(options.ThrowOnCompileFault);
        }

        [DataTestMethod]
        [DataRow(-1, false)]
        [DataRow(0, true)]
        [DataRow(65536, true)]
        [DataRow(65537, false)]
        public void ExpressionCacheCapacity_Boundaries_Test(int value, bool accepted)
        {
            var options = new MathRuleEngineOptions { ExpressionCacheCapacity = value };

            if (accepted)
            {
                var engine = PinnedEngine.Create(options);
                Assert.AreEqual(2.0, engine.Evaluate("1 + 1"));
                Assert.AreEqual(2.0, engine.Evaluate("1 + 1"));

                return;
            }

            var ex = OptionsValidationAssert.ThrowsOutOfRange(() => PinnedEngine.Create(options), "ExpressionCacheCapacity must be between 0 and 65536");

            Assert.AreEqual(value, (int)ex.ActualValue);
        }

        [DataTestMethod]
        [DataRow(0, false)]
        [DataRow(1, true)]
        [DataRow(1024, true)]
        [DataRow(1025, false)]
        public void CompilationThreshold_Boundaries_Test(int value, bool accepted)
        {
            var options = new MathRuleEngineOptions { CompilationThreshold = value };

            if (accepted)
            {
                var engine = PinnedEngine.Create(options);
                Assert.AreEqual(2.0, engine.Evaluate("1 + 1"));

                return;
            }

            var ex = OptionsValidationAssert.ThrowsOutOfRange(() => PinnedEngine.Create(options), "CompilationThreshold must be between 1 and 1024");

            Assert.AreEqual(value, (int)ex.ActualValue);
        }

        [TestMethod]
        public void InvalidThreshold_IsRejected_EvenWhenCompilationDisabled_Test()
        {
            var options = new MathRuleEngineOptions { EnableCompilation = false, CompilationThreshold = 0 };

            OptionsValidationAssert.ThrowsOutOfRange(() => PinnedEngine.Create(options), "CompilationThreshold must be between 1 and 1024");
        }

        [TestMethod]
        public void Clone_CopiesEveryInstanceField_ReflectionOverAllMembers_Test()
        {
            var source = new MathRuleEngineOptions();
            var fields = typeof(MathRuleEngineOptions).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Action<LambdaExpression> injector = lambda => { };

            Assert.IsTrue(fields.Length >= 14, "Expected at least 14 instance fields (7 v2 options + 3 public + 4 internal seams), found " + fields.Length);

            for (var i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                var type = field.FieldType;
                object sentinel;

                if (type == typeof(int))
                    sentinel = 7001 + i;
                else if (type == typeof(bool))
                    sentinel = !(bool)field.GetValue(source);
                else if (type == typeof(double))
                    sentinel = 0.5 + i;
                else if (type == typeof(string))
                    sentinel = "sentinel-" + i;
                else if (type == typeof(Action<LambdaExpression>))
                    sentinel = injector;
                else if (type.IsEnum)
                    sentinel = Enum.GetValues(type).Cast<object>().Last();
                else
                {
                    Assert.Fail("Unsupported option field type " + type + " for " + field.Name + "; extend this test so Clone() coverage stays complete.");

                    return;
                }

                field.SetValue(source, sentinel);
            }

            var clone = source.Clone();

            Assert.AreNotSame(source, clone);

            foreach (var field in fields)
                Assert.AreEqual(field.GetValue(source), field.GetValue(clone), "Clone() did not copy " + field.Name);
        }

        [TestMethod]
        public void Options_ExposeAllExpectedPublicAndInternalMembers_Test()
        {
            var expected = new[]
            {
                "MaxExpressionLength", "MaxTokens", "MaxNodes", "MaxDepth", "MaxEvaluationDepth", "MaxFunctionArgs",
                "AllowNonFiniteResults", "ExpressionCacheCapacity", "EnableCompilation", "CompilationThreshold",
                "PreferInterpretedExpressionTrees", "CompileFaultInjector", "SimulateNoDynamicCode", "ThrowOnCompileFault"
            };
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            foreach (var name in expected)
            {
                var property = typeof(MathRuleEngineOptions).GetProperty(name, flags);
                Assert.IsNotNull(property, "Missing option property " + name);
                Assert.IsTrue(property.CanRead && property.CanWrite, name + " must be readable and writable.");
            }
        }

        [TestMethod]
        public void OptionsMutatedAfterConstruction_HaveNoEffectOnEngine_Test()
        {
            var injectorCalls = 0;
            var options = new MathRuleEngineOptions { ExpressionCacheCapacity = 8, CompilationThreshold = 2 };
            var engine = PinnedEngine.Create(options);

            options.ExpressionCacheCapacity = 0;
            options.EnableCompilation = false;
            options.CompilationThreshold = 1024;
            options.CompileFaultInjector = lambda => { injectorCalls++; throw new InvalidProgramException("must not be seen"); };

            engine.Evaluate("x + 1", ParameterSets.X(1.0));
            engine.Evaluate("x + 1", ParameterSets.X(2.0));

            ExpressionProgram program;
            Assert.IsTrue(engine.TryPeekCachedProgram("x + 1", out program), "Capacity mutation must not apply.");
            Assert.IsTrue(program.IsCompiled, "Threshold/EnableCompilation/injector mutations must not apply.");
            Assert.AreEqual(0, injectorCalls);
        }

        [TestMethod]
        public void UnsuppressedEngine_RunModeOverride_NeverChangesLimitsOrInjector_Test()
        {
            Action<LambdaExpression> injector = lambda => { };
            var options = new MathRuleEngineOptions
            {
                MaxExpressionLength = 777,
                MaxTokens = 555,
                MaxNodes = 333,
                MaxDepth = 7,
                MaxEvaluationDepth = 33,
                MaxFunctionArgs = 5,
                AllowNonFiniteResults = true
            };
            options.CompileFaultInjector = injector;
            options.SimulateNoDynamicCode = true;

            var effective = RunMode.ReadEngineOptions(new MathRuleEngine(options));

            Assert.AreNotSame(options, effective);
            Assert.AreEqual(777, effective.MaxExpressionLength);
            Assert.AreEqual(555, effective.MaxTokens);
            Assert.AreEqual(333, effective.MaxNodes);
            Assert.AreEqual(7, effective.MaxDepth);
            Assert.AreEqual(33, effective.MaxEvaluationDepth);
            Assert.AreEqual(5, effective.MaxFunctionArgs);
            Assert.IsTrue(effective.AllowNonFiniteResults);
            Assert.AreSame(injector, effective.CompileFaultInjector, "CompileFaultInjector is not override-settable (SR-26).");
            Assert.IsTrue(effective.SimulateNoDynamicCode, "SimulateNoDynamicCode is not one of the five overridable members.");
        }

        [TestMethod]
        public void SuppressedEngine_KeepsExplicitCompileAndCacheOptions_Test()
        {
            var options = new MathRuleEngineOptions { ExpressionCacheCapacity = 3, CompilationThreshold = 7, EnableCompilation = true };
            options.PreferInterpretedExpressionTrees = true;

            var effective = RunMode.ReadEngineOptions(PinnedEngine.Create(options));

            Assert.AreEqual(3, effective.ExpressionCacheCapacity);
            Assert.AreEqual(7, effective.CompilationThreshold);
            Assert.IsTrue(effective.EnableCompilation);
            Assert.IsTrue(effective.PreferInterpretedExpressionTrees);
            Assert.IsFalse(effective.ThrowOnCompileFault, "Suppressed engines see production fallback (C3).");
        }
    }
}
