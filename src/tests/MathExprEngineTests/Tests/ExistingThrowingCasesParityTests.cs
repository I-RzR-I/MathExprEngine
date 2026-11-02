using System;
using System.Collections.Generic;
using System.Linq;
using MathExprEngine;
using MathExprEngine.Exceptions;
using MathExprEngineTests.Helpers.Assertions;
using MathExprEngineTests.Helpers.Engines;
using MathExprEngineTests.Helpers.Inputs;
using MathExprEngineTests.Helpers.Parity;
using MathExprEngineTests.Helpers.TestBases;
using MathExprEngineTests.Models;

namespace MathExprEngineTests.Tests
{
    [TestClass]
    public class ExistingThrowingCasesParityTests : ParityDepthGuardedTestBase
    {
        private static readonly Dictionary<string, ParityCase> Cases = Build();

        private static Dictionary<string, ParityCase> Build()
        {
            var t = new Dictionary<string, ParityCase>();
            var mre = typeof(MathRuleEngineException);
            var ese = typeof(ExpressionSyntaxException);
            var efe = typeof(ExpressionFunctionException);
            var arg = typeof(ArgumentException);

            void Add(string source, string expression, Type type, Func<IDictionary<string, object>> p = null,
                Action<MathRuleEngineOptions> cfg = null, Action<MathRuleEngine> setup = null, string suffix = null)
            {
                var id = source.Replace(".cs", string.Empty).Replace(":", "_L") + (suffix == null ? string.Empty : "_" + suffix);
                t.Add(id, new ParityCase { Source = source, Expression = expression, ExpectedException = type, Parameters = p, Configure = cfg, Setup = setup });
            }

            Add("ArithmeticTests.cs:44", "10 / 0", mre);
            Add("ArithmeticTests.cs:89", "10%0", mre);

            Add("EdgeCaseTests.cs:128", "undefinedVar + 1", ese);
            Add("EdgeCaseTests.cs:222", "x", mre, ParameterSets.XFactory(null));
            Add("EdgeCaseTests.cs:238", "x", mre, ParameterSets.XFactory(double.NaN));
            Add("EdgeCaseTests.cs:256", "x", mre, ParameterSets.XFactory("abc"));
            Add("EdgeCaseTests.cs:272", "x", mre, ParameterSets.XFactory(new DateTime(2026, 10, 21, 12, 0, 0)));
            Add("EdgeCaseTests.cs:286", "x", mre, ParameterSets.XFactory('5'));
            Add("EdgeCaseTests.cs:299", "x", mre, ParameterSets.XFactory(TimeSpan.FromMinutes(5)));
            Add("EdgeCaseTests.cs:310", "x", mre, ParameterSets.XFactory(new Guid("6f9619ff-8b86-d011-b42d-00c04fc964ff")));
            Add("EdgeCaseTests.cs:325", "x", mre, ParameterSets.XFactory(DayOfWeek.Monday));
            Add("EdgeCaseTests.cs:336", "x", mre, ParameterSets.XFactory(new object()));
            foreach (var e in new[] { "sin()", "sin(1,2)", "cos()", "cos(1,2)", "sqrt()", "sqrt(1,2)", "pow(1)", "pow(1,2,3)", "neg()", "neg(1,2)" })
                Add("EdgeCaseTests.cs:396", e, efe, suffix: e.Replace("(", "_").Replace(")", string.Empty).Replace(",", "_"));
            Add("EdgeCaseTests.cs:412", "max()", efe);
            Add("EdgeCaseTests.cs:428", "sum()", efe);
            Add("EdgeCaseTests.cs:473", "max(1, 2, 3)", efe, setup: e => e.RegisterFunction("max", HostFunctions.TwoArgumentMax));
            Add("EdgeCaseTests.cs:491", "1.2.3", mre);
            Add("EdgeCaseTests.cs:508", "@", mre, suffix: "at");
            Add("EdgeCaseTests.cs:508", "$", mre, suffix: "dollar");
            Add("EdgeCaseTests.cs:525", "", arg);
            Add("EdgeCaseTests.cs:543", "   ", arg);
            Add("EdgeCaseTests.cs:561", "1e5", ese);
            Add("EdgeCaseTests.cs:584", "sqrt(-1)", efe);
            Add("EdgeCaseTests.cs:599", "0^-1", mre);
            Add("EdgeCaseTests.cs:612", "10^400", mre);
            Add("EdgeCaseTests.cs:633", "nan() ? 1 : 2", mre, setup: HostFunctions.RegisterNaN);
            Add("EdgeCaseTests.cs:668", "nan() ? 111 : 222", mre, cfg: OptionPresets.AllowNonFinite, setup: HostFunctions.RegisterNaN);
            Add("EdgeCaseTests.cs:681", "nan() && 1", mre, cfg: OptionPresets.AllowNonFinite, setup: HostFunctions.RegisterNaN);
            Add("EdgeCaseTests.cs:682", "nan() || 0", mre, cfg: OptionPresets.AllowNonFinite, setup: HostFunctions.RegisterNaN);

            Add("UnaryOperatorTests.cs:65", "0^-1", mre);

            Add("FunctionTests.cs:56", "unknown(1)", efe);
            Add("FunctionTests.cs:81", "mod(1)", efe, suffix: "mod1");
            Add("FunctionTests.cs:81", "mod(1,2,3)", efe, suffix: "mod3");
            Add("FunctionTests.cs:94", "mod(10, 0)", mre);
            Add("FunctionTests.cs:97", "10 % 0", mre);

            Add("ErrorHandlingTests.cs:38", "2 +", ese, suffix: "a");
            Add("ErrorHandlingTests.cs:38", "(2 + 3", ese, suffix: "b");
            Add("ErrorHandlingTests.cs:38", "2 ** 3", ese, suffix: "c");
            Add("ErrorHandlingTests.cs:127", "1 + mod(10, 0)", mre);

            Add("LimitsAndConcurrencyTests.cs:276", new string('+', 10) + "5", mre, cfg: o => o.MaxExpressionLength = 10);
            Add("LimitsAndConcurrencyTests.cs:304", "50%%%%%%", mre, cfg: o => o.MaxTokens = 6);
            Add("LimitsAndConcurrencyTests.cs:330", "50%%%%%", mre, cfg: o => o.MaxNodes = 5);
            Add("LimitsAndConcurrencyTests.cs:358", new string('-', 9) + "5", mre, cfg: o => o.MaxDepth = 10);
            Add("LimitsAndConcurrencyTests.cs:391", ExpressionText.FlatChain(11), mre, cfg: o => o.MaxEvaluationDepth = 10);
            Add("LimitsAndConcurrencyTests.cs:415", "sum(1,2,3,4)", mre, cfg: o => o.MaxFunctionArgs = 3);
            Add("LimitsAndConcurrencyTests.cs:438", ExpressionText.NestedParentheses(5000), mre);
            Add("LimitsAndConcurrencyTests.cs:458", ExpressionText.FlatChain(20000), mre);
            Add("LimitsAndConcurrencyTests.cs:484", ExpressionText.FlatChain(new MathRuleEngineOptions().MaxEvaluationDepth + 1), mre);
            Add("LimitsAndConcurrencyTests.cs:502", new string('-', 20000) + "1", mre);
            Add("LimitsAndConcurrencyTests.cs:521", new string('1', 51), mre, cfg: o => o.MaxExpressionLength = 50,
                setup: e => e.RegisterFunction("triple", a => a[0] * 3));
            Add("LimitsAndConcurrencyTests.cs:578", "10^400", mre);

            return t;
        }

        public TestContext TestContext { get; set; }

        public static IEnumerable<object[]> CaseIdsByPath => DynamicDataRows.ByCompiledPath(Cases.Keys);

        [DataTestMethod]
        [DynamicData(nameof(CaseIdsByPath), DynamicDataDisplayName = nameof(TestDisplayNames.Format), DynamicDataDisplayNameDeclaringType = typeof(TestDisplayNames))]
        public void ExistingThrowingCase_CompiledPathThrowsIdentically(string caseId, string pathName)
        {
            var c = Cases[caseId];
            var path = ParityAssert.ParsePath(pathName);

            var oracle = ParityAssert.Run(ParityPath.Interpreter, c);

            Assert.IsTrue(oracle.Threw, c.Source + ": the interpreter oracle no longer throws. " + oracle.Describe());
            Assert.AreEqual(c.ExpectedException, oracle.Error.GetType(), c.Source + ": oracle type differs from the pinned v2.0.0 type.");

            var actual = ParityAssert.Run(path, c);

            ParityAssert.AreEquivalent(oracle, actual, c.Source + " on " + path);
            TestContext.WriteLine(c.Source + " / " + path + ": " + actual.Describe());
        }

        [TestMethod]
        public void Inventory_Covers48RealSites()
        {
            var distinctSites = Cases.Values.Select(c => c.Source).Distinct().Count();

            Assert.AreEqual(47, distinctSites);
        }

        [DataTestMethod]
        [DataRow("MaxExpressionLength")]
        [DataRow("MaxTokens")]
        [DataRow("MaxNodes")]
        [DataRow("MaxDepth")]
        [DataRow("MaxEvaluationDepth")]
        [DataRow("MaxFunctionArgs")]
        public void Constructor_InvalidOptionValue_IdenticalAcrossCompileConfigurations(string propertyName)
        {
            Exception Construct(ParityPath path)
            {
                var o = new MathRuleEngineOptions();
                typeof(MathRuleEngineOptions).GetProperty(propertyName).SetValue(o, 0);
                ParityAssert.ApplyPath(path, o);

                return ExceptionCapture.OrNull(() => PinnedEngine.Create(o));
            }

            var oracle = Construct(ParityPath.Interpreter);
            Assert.IsInstanceOfType(oracle, typeof(ArgumentOutOfRangeException));
            Assert.AreEqual("options", ((ArgumentOutOfRangeException)oracle).ParamName);

            foreach (var path in ParityAssert.CompiledPaths)
            {
                var actual = Construct(path);
                Assert.IsNotNull(actual, path.ToString());
                ParityAssert.AreSameException(oracle, actual, propertyName + " on " + path);
            }
        }
    }
}
