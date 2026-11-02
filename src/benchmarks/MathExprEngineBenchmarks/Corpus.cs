using MathExprEngineBenchmarks.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace MathExprEngineBenchmarks
{
    public static class Corpus
    {
        public const int UniqueTextPoolSize = 8192;

        private const string SmallExpression = "x + 2 * y";

        private const string MediumExpression =
            "(price * quantity - discount) / 2 + max(price, 10, quantity) * 1.5 >= 100 && rate < 0.5"
            + " ? price * rate : -discount";

        private const string LargeExpression =
            "(price * quantity - discount) / 2 + max(price, 10, quantity) * 1.5 >= 100 && rate < 0.5"
            + " ? sqrt(pow(price, 2) + pow(quantity, 2)) * (1 + rate) - sum(price, quantity, discount, 4, 5) % 7"
            + " : (discount > 0 || rate != 0 ? pow(1 + rate, 3) * price - neg(discount) : 50% * price)"
            + " + sin(rate) * cos(rate) - (price - quantity * 2) ^ 2 / (quantity + 1)";

        public static string GetExpression(ExpressionSize size)
        {
            switch (size)
            {
                case ExpressionSize.Small:
                    return SmallExpression;
                case ExpressionSize.Medium:
                    return MediumExpression;
                case ExpressionSize.Large:
                    return LargeExpression;
                default:
                    throw new ArgumentOutOfRangeException(nameof(size), size, null);
            }
        }

        public static IDictionary<string, object> CreateParameters(ExpressionSize size)
        {
            if (size == ExpressionSize.Small)
                return new Dictionary<string, object> { ["x"] = 3.0, ["y"] = 4.0 };

            return new Dictionary<string, object>
            {
                ["price"] = 120.0,
                ["quantity"] = 3.0,
                ["discount"] = 15.0,
                ["rate"] = 0.2
            };
        }

        public static string[] CreateUniqueVariants(string expression)
        {
            var variants = new string[UniqueTextPoolSize];
            for (var i = 0; i < variants.Length; i++)
                variants[i] = "(" + expression + ") + " + i.ToString(CultureInfo.InvariantCulture);

            return variants;
        }

        public static string CreateParameterExpression(int count, ParameterKind kind, out IDictionary<string, object> parameters)
        {
            var names = new string[count];
            parameters = new Dictionary<string, object>(count);
            for (var i = 0; i < count; i++)
            {
                names[i] = "p" + i.ToString(CultureInfo.InvariantCulture);
                var value = i + 0.5;
                parameters[names[i]] = kind == ParameterKind.String
                    ? value.ToString(CultureInfo.InvariantCulture)
                    : (object)value;
            }

            return "(" + string.Join(" + ", names) + ") * 0.5";
        }
    }
}
