using System;

namespace MathExprEngineTests.Helpers.Assertions
{
    internal static class OptionsValidationAssert
    {
        public static ArgumentOutOfRangeException ThrowsOutOfRange(Action construct, string messagePrefix)
        {
            var ex = Assert.ThrowsException<ArgumentOutOfRangeException>(construct);
            Assert.AreEqual("options", ex.ParamName);
            StringAssert.StartsWith(ex.Message, messagePrefix);

            return ex;
        }
    }
}
