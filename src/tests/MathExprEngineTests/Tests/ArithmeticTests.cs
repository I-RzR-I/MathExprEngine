using MathExprEngine;
using MathExprEngine.Exceptions;
using MathExprEngineTests.Helpers.Inputs;

namespace MathExprEngineTests.Tests
{
    [TestClass]
    public class ArithmeticTests
    {
        [TestMethod]
        public void FloatingPointPrecision_IsWithinTolerance_Test()
        {
            var expEval = new MathRuleEngine();
            var result = expEval.Evaluate("0.1 + 0.2");

            Assert.IsTrue(result > 0.299999 && result < 0.300001);
        }

        [TestMethod]
        public void DivisionByZero_ShouldThrow_Test()
        {
            var expEval = new MathRuleEngine();

            Assert.ThrowsException<MathRuleEngineException>(() =>
                expEval.Evaluate("10 / 0")
            );
        }

        [TestMethod]
        [DataRow("5%3", 2)]
        [DataRow("20%6", 2)]
        [DataRow("6%3", 0)]
        [DataRow("10 + 5%3", 12)]
        [DataRow("11%4+1", 4)]
        [DataRow("17%5*2", 4)]
        [DataRow("17%(5*2)", 7)]
        [DataRow("(17%5)%2", 0)]
        [DataRow("100%3", 1)]
        [DataRow("100%%3", 1)]
        [DataRow("-5%3", -2)]
        [DataRow("5%(-3)", 2)]
        [DataRow("0.5%0.2", 0.1)]
        [DataRow("200 * 10%", 20)]
        [DataRow("50%", 0.5)]
        [DataRow("100% + 2", 0)]
        [DataRow("(80+20)%*300", 300)]
        [DataRow("(sum(1,2,3)/1)%3", 0)]
        [DataRow("(sum(1,2,3)*2.5)%4", 3)]
        [DataRow("sqrt(sum(1,2,3,3))%3", 0)]
        public void PercentOperator_ShouldSupportModuloAndPercentage_Test(string expression, double expected)
        {
            var expEval = new MathRuleEngine();

            Assert.AreEqual(expected, expEval.Evaluate(expression), 0.000000001d);
        }

        [TestMethod]
        public void ModuloByZero_ShouldThrow_Test()
        {
            var expEval = new MathRuleEngine();

            Assert.ThrowsException<MathRuleEngineException>(() =>
                expEval.Evaluate("10%0")
            );
        }

        [DataTestMethod]
        [DataRow("10 % 3", 1)]
        [DataRow("10 % -3", 1)]
        [DataRow("10 % +3", 1)]
        [DataRow("10 % (3)", 1)]
        [DataRow("50% * 2", 1)]
        [DataRow("50%", 0.5)]
        [DataRow("100 % 30 %", 0.1)]
        [DataRow("10 % sqrt(4)", 0)]
        public void PercentOperator_DecisionTable_PrimaryRows_Test(string expression, double expected)
        {
            var expEval = new MathRuleEngine();

            Assert.AreEqual(expected, expEval.Evaluate(expression), 0.000000001d);
        }

        [TestMethod]
        public void PercentOperator_DecisionTable_InfixModuloWithParamVariableRhs_Test()
        {
            var expEval = new MathRuleEngine();
            var values = ParameterSets.Of("a", 10d, "b", 3d);

            var result = expEval.Evaluate("a % b", values);

            Assert.AreEqual(1, result, 0.000000001d);
        }

        [TestMethod]
        public void PercentOperator_DecisionTable_InfixModuloWithNegatedParamVariableRhs_Test()
        {
            var expEval = new MathRuleEngine();
            var values = ParameterSets.Of("a", 10d, "b", 3d);

            var result = expEval.Evaluate("a % -b", values);

            Assert.AreEqual(1, result, 0.000000001d);
        }
    }
}