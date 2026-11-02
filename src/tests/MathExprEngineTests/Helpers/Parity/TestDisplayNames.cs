using System.Linq;
using System.Reflection;

namespace MathExprEngineTests.Helpers.Parity
{
    public static class TestDisplayNames
    {
        public static string Format(MethodInfo method, object[] data) =>
            method.Name + "(" + string.Join(", ", data.Select(d => d?.ToString())) + ")";
    }
}
