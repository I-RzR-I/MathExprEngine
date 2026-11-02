using MathExprEngine;
using MathExprEngine.Exceptions;

namespace MathExprEngineTests.Tests
{
    [TestClass]
    public class UnaryOperatorTests
    {
        [DataTestMethod]
        [DataRow("-5", -5)]
        [DataRow("--5", 5)]
        [DataRow("-(2 + 3)", -5)]
        [DataRow("+5", 5)]
        [DataRow("50%", 0.5)]
        [DataRow("(20 + 30)%", 0.5)]
        [DataRow("200 * 10%", 20)]
        [DataRow("-50%", -0.5)]
        [DataRow("25%%", 0.0025)]
        [DataRow("100 + 10 + 3%", 110.03)]
        [DataRow("-2^2", -4)]
        [DataRow("-4^0.5", -2)]
        [DataRow("2^3^2", 512)]
        [DataRow("2^-3", 0.125)]
        [DataRow("-2^-2", -0.25)]
        [DataRow("2*-3", -6)]
        [DataRow("-2*3", -6)]
        public void UnaryOperators_WorkCorrectly_Test(string expression, double expected)
        {
            var expEval = new MathRuleEngine();

            Assert.AreEqual(expected, expEval.Evaluate(expression));
        }

        [TestMethod]
        public void ZeroToNegativePower_ThrowsInsteadOfReturningInfinity_Test()
        {
            var expEval = new MathRuleEngine();

            Assert.ThrowsException<MathRuleEngineException>(() =>
                expEval.Evaluate("0^-1")
            );
        }
    }
}