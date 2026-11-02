using System;
using MathExprEngine;

namespace MathExprEngineTests.Helpers.Engines
{
    internal static class HostFunctions
    {
        public static void RegisterNaN(MathRuleEngine engine)
        {
            engine.RegisterFunction("nan", _ => double.NaN);
        }

        public static double TwoArgumentMax(double[] args)
        {
            if (args.Length != 2)
                throw new ArgumentException("max expects 2 arguments");

            return Math.Max(args[0], args[1]);
        }
    }
}
