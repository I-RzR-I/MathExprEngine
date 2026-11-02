namespace MathExprEngineTests.Helpers.ExpressionTrees
{
    internal static class CompiledTreeAllowList
    {
        public static readonly string[] AllowedMethods =
        {
            "MathExprEngine.Helpers.NodeSemantics.EnsureFinite(Double,Int32)",
            "MathExprEngine.Helpers.NodeSemantics.IsTrue(Double,Int32)",
            "MathExprEngine.Helpers.NodeSemantics.Divide(Double,Double,Int32)",
            "MathExprEngine.Helpers.NodeSemantics.Modulo(Double,Double,Int32)",
            "MathExprEngine.Helpers.NodeSemantics.ResolveFunction(IDictionary<String,Func<Double[],Double>>,String,Int32)",
            "MathExprEngine.Compilation.CompiledRuntime.ReadParameter(Object,String,Int32)",
            "MathExprEngine.Compilation.CompiledRuntime.InvokeFunction(Func<Double[],Double>,String,Int32,Double[],Int32,Int32)",
            "MathExprEngine.Compilation.CompiledRuntime.ThrowUnknownOperator(String,Int32)",
            "System.Math.Pow(Double,Double)"
        };

        public const string ResolveFunctionKey =
            "MathExprEngine.Helpers.NodeSemantics.ResolveFunction(IDictionary<String,Func<Double[],Double>>,String,Int32)";
    }
}
