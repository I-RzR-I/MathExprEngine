using MathExprEngine.Exceptions;

namespace MathExprEngineTests.Helpers.Assertions
{
    internal static class EngineExceptionAssert
    {
        public static void SameMessageAndColumn(MathExprEngineException expected, MathExprEngineException actual)
        {
            Assert.AreEqual(expected.Message, actual.Message);
            Assert.AreEqual(expected.Column, actual.Column);
        }

        public static void SameTypeMessageAndColumn(MathExprEngineException expected, MathExprEngineException actual, string message = null)
        {
            var text = message ?? string.Empty;
            Assert.AreEqual(expected.GetType(), actual.GetType(), text);
            Assert.AreEqual(expected.Message, actual.Message, text);
            Assert.AreEqual(expected.Column, actual.Column, text);
        }
    }
}
