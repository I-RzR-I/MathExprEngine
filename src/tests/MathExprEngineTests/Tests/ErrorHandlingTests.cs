using System;
using MathExprEngine;
using MathExprEngine.Exceptions;

namespace MathExprEngineTests.Tests
{
    [TestClass]
    public class ErrorHandlingTests
    {
        [DataTestMethod]
        [DataRow("2 +", 3)]
        [DataRow("(2 + 3", 6)]
        [DataRow("2 ** 3", 3)]
        public void SyntaxErrors_ReportCorrectColumn_Test(string expression, int expectedColumn)
        {
            var expEval = new MathRuleEngine();

            var ex = Assert.ThrowsException<ExpressionSyntaxException>(() =>
                expEval.Evaluate(expression)
            );

            Assert.AreEqual(expectedColumn, ex.Column);
        }

        [DataTestMethod]
        [DataRow(typeof(MathRuleEngineException))]
        [DataRow(typeof(ExpressionSyntaxException))]
        [DataRow(typeof(ExpressionFunctionException))]
        [DataRow(typeof(FunctionArgumentException))]
        public void AllExceptionTypes_DeriveFromCommonPublicBase_Test(Type exceptionType)
        {
            Assert.IsTrue(
                typeof(MathExprEngineException).IsAssignableFrom(exceptionType),
                $"{exceptionType.Name} must derive from MathExprEngineException.");
        }

        [DataTestMethod]
        [DataRow("2 +")]
        [DataRow("unknown(1)")]
        [DataRow("sin(1,2)")]
        [DataRow("10 % 0")]
        [DataRow("0^-1")]
        public void AllEngineFailureModes_AreCatchableByCommonBase_Test(string expression)
        {
            var expEval = new MathRuleEngine();

            try
            {
                expEval.Evaluate(expression);
                Assert.Fail($"Expected '{expression}' to throw.");
            }
            catch (MathExprEngineException)
            {
            }
        }

        [TestMethod]
        public void PositionlessError_ReportsColumnMinusOne_NotZero_Test()
        {
            var syntax = new ExpressionSyntaxException("no position");
            var engineEx = new MathRuleEngineException("no position");
            var function = new ExpressionFunctionException("no position");

            Assert.AreEqual(-1, syntax.Column);
            Assert.AreEqual(-1, engineEx.Column);
            Assert.AreEqual(-1, function.Column);

            Assert.AreEqual("Error: Modulo by zero",
                new MathRuleEngineException("Modulo by zero").Message);
        }

        [TestMethod]
        public void FunctionError_WithoutOwnColumn_GetsTheCallSiteColumn_Test()
        {
            var expEval = new MathRuleEngine();

            var ex = Assert.ThrowsException<MathRuleEngineException>(() =>
                expEval.Evaluate("1 + mod(10, 0)")
            );

            Assert.AreEqual(4, ex.Column);
        }

        [TestMethod]
        public void FunctionArgumentException_IsSiblingOf_ExpressionFunctionException_NotASubclass_Test()
        {
            Assert.IsFalse(typeof(FunctionArgumentException).IsSubclassOf(typeof(ExpressionFunctionException)));
            Assert.IsTrue(typeof(FunctionArgumentException).IsSubclassOf(typeof(MathExprEngineException)));
            Assert.IsTrue(typeof(ExpressionFunctionException).IsSubclassOf(typeof(MathExprEngineException)));
        }
    }
}