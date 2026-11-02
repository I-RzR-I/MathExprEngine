using MathExprEngine;

namespace MathExprEngineTests.Tests
{
    [TestClass]
    public class LogicalTests
    {
        [DataTestMethod]
        [DataRow("1 > 0 && 2 > 1", 1)]
        [DataRow("1 > 0 || 0 > 1", 1)]
        [DataRow("1 < 0 || 0 > 1", 0)]
        [DataRow("(1 > 0) && (0 > 1)", 0)]
        public void LogicalExpressions_EvaluateCorrectly_Test(string expression, double expected)
        {
            var expEval = new MathRuleEngine();

            Assert.AreEqual(expected, expEval.Evaluate(expression));
        }
    }
}