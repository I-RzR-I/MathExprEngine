using System;
using System.Linq;
using System.Reflection;

namespace MathExprEngineTests.Helpers.Formatting
{
    internal static class ReflectionFormat
    {
        public static string FormatType(Type type)
        {
            if (type.IsArray)
                return FormatType(type.GetElementType()) + "[]";

            if (!type.IsGenericType)
                return type.Name;

            var name = type.Name;
            var tick = name.IndexOf('`');
            if (tick >= 0)
                name = name.Substring(0, tick);

            return name + "<" + string.Join(",", type.GetGenericArguments().Select(FormatType)) + ">";
        }

        public static string MethodKey(MethodInfo method)
        {
            return method.DeclaringType.FullName + "." + method.Name + "(" +
                   string.Join(",", method.GetParameters().Select(p => FormatType(p.ParameterType))) + ")";
        }
    }
}
