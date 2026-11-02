using System;
using System.Collections.Generic;

namespace MathExprEngineTests.Helpers.Inputs
{
    internal static class ParameterSets
    {
        public static Dictionary<string, object> Of(params object[] keyValues)
        {
            return Fill(new Dictionary<string, object>(), keyValues);
        }

        public static Dictionary<string, object> OfIgnoreCase(params object[] keyValues)
        {
            return Fill(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase), keyValues);
        }

        public static Dictionary<string, object> X(double x)
        {
            return new Dictionary<string, object> { { "x", x } };
        }

        public static Dictionary<string, object> XY(double x, double y)
        {
            return new Dictionary<string, object> { { "x", x }, { "y", y } };
        }

        public static Func<IDictionary<string, object>> XFactory(object value)
        {
            return () => new Dictionary<string, object> { { "x", value } };
        }

        private static Dictionary<string, object> Fill(Dictionary<string, object> dictionary, object[] keyValues)
        {
            if (keyValues.Length % 2 != 0)
                throw new ArgumentException("Key/value arguments must come in pairs.", nameof(keyValues));

            for (var i = 0; i < keyValues.Length; i += 2)
                dictionary[(string)keyValues[i]] = keyValues[i + 1];

            return dictionary;
        }
    }
}
