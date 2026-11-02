using System;

namespace MathExprEngineTests.Helpers.Assertions
{
    internal static class BitAssert
    {
        public static void AreEqual(double expected, double actual, string message = null)
        {
            Assert.AreEqual(BitConverter.DoubleToInt64Bits(expected), BitConverter.DoubleToInt64Bits(actual), message ?? string.Empty);
        }

        public static void AreEqualDescribed(double expected, double actual, string because)
        {
            AreEqual(expected, actual, because + " expected=" + expected + " actual=" + actual);
        }
    }
}
