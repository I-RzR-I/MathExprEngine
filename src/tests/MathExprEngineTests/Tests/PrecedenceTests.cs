using MathExprEngine;

namespace MathExprEngineTests.Tests
{
    [TestClass]
    public class PrecedenceTests
    {
        [DataTestMethod]
        [DataRow("2 + 3 * 4", 14)]
        [DataRow("2 * 3 + 4", 10)]
        [DataRow("2 * (3 + 4)", 14)]
        [DataRow("10 - 4 - 3", 3)]
        [DataRow("2 ^ 3 ^ 2", 512)]
        public void OperatorPrecedence_IsCorrect_Test(string expression, double expected)
        {
            var expEval = new MathRuleEngine();
            var result = expEval.Evaluate(expression);

            Assert.AreEqual(expected, result);
        }
    }
}