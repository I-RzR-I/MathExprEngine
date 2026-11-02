using System.Collections.Generic;
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
    public class LogicalNonFiniteOperandTests : ParityDepthGuardedTestBase
    {
        private const string NonFinite = "The expression produced a non-finite value (NaN or Infinity)";

        private static readonly Dictionary<string, NonFiniteOperandCase> CaseTable = new Dictionary<string, NonFiniteOperandCase>
        {
            ["Allow_And_RightNaN"] = new NonFiniteOperandCase { Expression = "1 && x", AllowNonFinite = true, X = double.NaN, ExpectedColumn = 2 },
            ["Allow_Or_RightNaN"] = new NonFiniteOperandCase { Expression = "0 || x", AllowNonFinite = true, X = double.NaN, ExpectedColumn = 2 },
            ["Allow_And_LeftNaN"] = new NonFiniteOperandCase { Expression = "x && 1", AllowNonFinite = true, X = double.NaN, ExpectedColumn = 2 },
            ["Allow_Or_LeftNaN"] = new NonFiniteOperandCase { Expression = "x || 0", AllowNonFinite = true, X = double.NaN, ExpectedColumn = 2 },
            ["Allow_And_RightPosInf"] = new NonFiniteOperandCase { Expression = "1 && x", AllowNonFinite = true, X = double.PositiveInfinity, ExpectedColumn = 2 },
            ["Allow_Or_RightNegInf"] = new NonFiniteOperandCase { Expression = "0 || x", AllowNonFinite = true, X = double.NegativeInfinity, ExpectedColumn = 2 },
            ["Allow_And_RightNaN_Spaced"] = new NonFiniteOperandCase { Expression = "  1   &&   x", AllowNonFinite = true, X = double.NaN, ExpectedColumn = 6 },
            ["Allow_Or_RightNaN_Spaced"] = new NonFiniteOperandCase { Expression = "  0   ||   x", AllowNonFinite = true, X = double.NaN, ExpectedColumn = 6 },
            ["Allow_And_Nested_InnerOperator"] = new NonFiniteOperandCase { Expression = "1 && (1 && x)", AllowNonFinite = true, X = double.NaN, ExpectedColumn = 8 },
            ["Allow_Ternary_InfCondition"] = new NonFiniteOperandCase { Expression = "x ? 1 : 2", AllowNonFinite = true, X = double.PositiveInfinity, ExpectedColumn = 0 },

            ["Allow_And_ShortCircuit"] = new NonFiniteOperandCase { Expression = "0 && x", AllowNonFinite = true, X = double.NaN, ExpectedValue = 0.0 },
            ["Allow_Or_ShortCircuit"] = new NonFiniteOperandCase { Expression = "1 || x", AllowNonFinite = true, X = double.NaN, ExpectedValue = 1.0 },
            ["Allow_And_RightFinite"] = new NonFiniteOperandCase { Expression = "1 && x", AllowNonFinite = true, X = 2.5, ExpectedValue = 1.0 },
            ["Allow_Or_RightZero"] = new NonFiniteOperandCase { Expression = "0 || x", AllowNonFinite = true, X = 0.0, ExpectedValue = 0.0 },

            ["Default_And_RightNaN_ThrowsAtOperand"] = new NonFiniteOperandCase { Expression = "1 && x", AllowNonFinite = false, X = double.NaN, ExpectedColumn = 5 },
            ["Default_Or_RightNaN_ThrowsAtOperand"] = new NonFiniteOperandCase { Expression = "0 || x", AllowNonFinite = false, X = double.NaN, ExpectedColumn = 5 },
            ["Default_And_LeftNaN_ThrowsAtOperand"] = new NonFiniteOperandCase { Expression = "x && 1", AllowNonFinite = false, X = double.NaN, ExpectedColumn = 0 },
            ["Default_And_RightNaN_Spaced"] = new NonFiniteOperandCase { Expression = "  1   &&   x", AllowNonFinite = false, X = double.NaN, ExpectedColumn = 11 },
            ["Default_Ternary_InfCondition"] = new NonFiniteOperandCase { Expression = "x ? 1 : 2", AllowNonFinite = false, X = double.PositiveInfinity, ExpectedColumn = 0 },
            ["Default_And_ShortCircuit"] = new NonFiniteOperandCase { Expression = "0 && x", AllowNonFinite = false, X = double.NaN, ExpectedValue = 0.0 },
        };

        public TestContext TestContext { get; set; }

        public static IEnumerable<object[]> CaseIdsByPath => DynamicDataRows.ByEveryPath(CaseTable.Keys);

        [DataTestMethod]
        [DynamicData(nameof(CaseIdsByPath), DynamicDataDisplayName = nameof(TestDisplayNames.Format), DynamicDataDisplayNameDeclaringType = typeof(TestDisplayNames))]
        public void LogicalOperand_NonFinite_ThrowsAtExactColumn_OrReturnsValue(string caseId, string pathName)
        {
            var c = CaseTable[caseId];
            var path = ParityAssert.ParsePath(pathName);

            var outcome = ParityAssert.Run(
                path,
                c.Expression,
                ParameterSets.XFactory(c.X),
                o => o.AllowNonFiniteResults = c.AllowNonFinite);

            TestContext.WriteLine(caseId + " / " + path + ": " + outcome.Describe());

            if (c.ExpectedColumn == null)
            {
                Assert.IsFalse(outcome.Threw, "Expected a value. " + outcome.Describe());
                BitAssert.AreEqual(c.ExpectedValue, outcome.Value);
                return;
            }

            Assert.IsTrue(outcome.Threw, "Expected MathRuleEngineException, got " + outcome.Describe());
            Assert.AreEqual(typeof(MathRuleEngineException), outcome.Error.GetType(), outcome.Describe());

            var ex = (MathRuleEngineException)outcome.Error;
            Assert.AreEqual(c.ExpectedColumn.Value, ex.Column, outcome.Describe());
            Assert.AreEqual("Error: " + NonFinite + " at column " + (c.ExpectedColumn.Value + 1), ex.Message);
            Assert.IsNull(ex.InnerException);
        }

        [DataTestMethod]
        [DynamicData(nameof(CaseIdsByPathCompiledOnly), DynamicDataDisplayName = nameof(TestDisplayNames.Format), DynamicDataDisplayNameDeclaringType = typeof(TestDisplayNames))]
        public void LogicalOperand_NonFinite_CompiledMatchesInterpreter(string caseId, string pathName)
        {
            var c = CaseTable[caseId];
            var path = ParityAssert.ParsePath(pathName);

            ParityAssert.Matches(
                path,
                c.Expression,
                ParameterSets.XFactory(c.X),
                o => o.AllowNonFiniteResults = c.AllowNonFinite);
        }

        public static IEnumerable<object[]> CaseIdsByPathCompiledOnly => DynamicDataRows.ByCompiledPath(CaseTable.Keys);

        [DataTestMethod]
        [DataRow("Interpreter", true, 2)]
        [DataRow("Eager", true, 2)]
        [DataRow("EagerInterpretedTrees", true, 2)]
        [DataRow("TieredSecondUse", true, 2)]
        [DataRow("Compile", true, 2)]
        [DataRow("CompileInterpretedTrees", true, 2)]
        [DataRow("Interpreter", false, 5)]
        [DataRow("Eager", false, 5)]
        [DataRow("EagerInterpretedTrees", false, 5)]
        [DataRow("TieredSecondUse", false, 5)]
        [DataRow("Compile", false, 5)]
        [DataRow("CompileInterpretedTrees", false, 5)]
        public void LogicalAnd_RightOperandFunctionReturnsNaN_ThrowsAtExpectedColumn(string pathName, bool allowNonFinite, int expectedColumn)
        {
            var path = ParityAssert.ParsePath(pathName);

            var outcome = ParityAssert.Run(
                path,
                "1 && nan()",
                null,
                o => o.AllowNonFiniteResults = allowNonFinite,
                HostFunctions.RegisterNaN);

            Assert.IsTrue(outcome.Threw, outcome.Describe());
            Assert.AreEqual(typeof(MathRuleEngineException), outcome.Error.GetType(), outcome.Describe());
            Assert.AreEqual(expectedColumn, ((MathRuleEngineException)outcome.Error).Column, outcome.Describe());
            Assert.AreEqual("Error: " + NonFinite + " at column " + (expectedColumn + 1), outcome.Error.Message);
        }

        [TestMethod]
        public void LogicalAnd_RightOperandNaN_LegModeEngine_Throws()
        {
            var engine = new MathRuleEngine(new MathRuleEngineOptions { AllowNonFiniteResults = true });
            var p = new Dictionary<string, object> { { "x", double.NaN } };

            for (var i = 0; i < 3; i++)
            {
                var ex = Assert.ThrowsException<MathRuleEngineException>(() => engine.Evaluate("1 && x", p));
                Assert.AreEqual(2, ex.Column);

                var ex2 = Assert.ThrowsException<MathRuleEngineException>(() => engine.Evaluate("0 || x", p));
                Assert.AreEqual(2, ex2.Column);
            }
        }
    }
}
