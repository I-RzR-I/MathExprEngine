using System;
using MathExprEngine.Exceptions;

namespace MathExprEngineTests.Helpers.Assertions
{
    internal static class ExceptionCapture
    {
        public static Exception OrNull(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                return ex;
            }

            return null;
        }

        public static Exception Of(Action action)
        {
            var captured = OrNull(action);
            if (captured == null)
                Assert.Fail("Expected an exception.");

            return captured;
        }

        public static MathExprEngineException EngineExceptionOf(Func<double> action)
        {
            try
            {
                action();
            }
            catch (MathExprEngineException ex)
            {
                return ex;
            }

            Assert.Fail("Expected a MathExprEngineException.");

            return null;
        }
    }
}
