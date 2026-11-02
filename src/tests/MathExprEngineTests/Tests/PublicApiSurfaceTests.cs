using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using MathExprEngine;
using MathExprEngineTests.Helpers.Engines;
using MathExprEngineTests.Helpers.Formatting;
using MathExprEngineTests.Helpers.TestBases;

namespace MathExprEngineTests.Tests
{
    [TestClass]
    public class PublicApiSurfaceTests : DepthGuardedTestBase
    {
        private const string Dict = "IDictionary<String,Func<Double[],Double>>";

        private static readonly string[] ExpectedPublicTypes =
        {
            "MathExprEngine.MathRuleEngine",
            "MathExprEngine.MathRuleEngineOptions",
            "MathExprEngine.CompiledExpression",
            "MathExprEngine.Exceptions.MathExprEngineException",
            "MathExprEngine.Exceptions.MathRuleEngineException",
            "MathExprEngine.Exceptions.ExpressionSyntaxException",
            "MathExprEngine.Exceptions.ExpressionFunctionException",
            "MathExprEngine.Exceptions.FunctionArgumentException"
        };

        private static readonly string[] ExpectedMembers =
        {
            "MathRuleEngine: public ctor(" + Dict + "=null)",
            "MathRuleEngine: public ctor(MathRuleEngineOptions," + Dict + "=null)",
            "MathRuleEngine: public method RegisterFunction(String,Func<Double[],Double>) : Void",
            "MathRuleEngine: public method RegisterFunctions(" + Dict + ") : Void",
            "MathRuleEngine: public method Evaluate(String,IDictionary<String,Object>=null) : Double",
            "MathRuleEngine: public method Compile(String) : CompiledExpression",

            "MathRuleEngineOptions: public ctor()",
            "MathRuleEngineOptions: public property MaxExpressionLength : Int32 { get; set; }",
            "MathRuleEngineOptions: public property MaxTokens : Int32 { get; set; }",
            "MathRuleEngineOptions: public property MaxNodes : Int32 { get; set; }",
            "MathRuleEngineOptions: public property MaxDepth : Int32 { get; set; }",
            "MathRuleEngineOptions: public property MaxEvaluationDepth : Int32 { get; set; }",
            "MathRuleEngineOptions: public property MaxFunctionArgs : Int32 { get; set; }",
            "MathRuleEngineOptions: public property AllowNonFiniteResults : Boolean { get; set; }",
            "MathRuleEngineOptions: public property ExpressionCacheCapacity : Int32 { get; set; }",
            "MathRuleEngineOptions: public property EnableCompilation : Boolean { get; set; }",
            "MathRuleEngineOptions: public property CompilationThreshold : Int32 { get; set; }",

            "CompiledExpression: public property Expression : String { get; }",
            "CompiledExpression: public method Evaluate(IDictionary<String,Object>=null) : Double",
            "CompiledExpression: public method ToString() : String",

            "MathExprEngineException: protected ctor(String,String,Int32,Exception)",
            "MathExprEngineException: protected ctor(SerializationInfo,StreamingContext)",
            "MathExprEngineException: public property Column : Int32 { get; }",
            "MathExprEngineException: protected property RawMessage : String { get; }",
            "MathExprEngineException: public method GetObjectData(SerializationInfo,StreamingContext) : Void",

            "MathRuleEngineException: public ctor(String)",
            "MathRuleEngineException: public ctor(String,Exception)",
            "MathRuleEngineException: public ctor(String,Int32)",
            "MathRuleEngineException: public ctor(String,Int32,Exception)",
            "MathRuleEngineException: protected ctor(SerializationInfo,StreamingContext)",

            "ExpressionSyntaxException: public ctor(String)",
            "ExpressionSyntaxException: public ctor(String,Exception)",
            "ExpressionSyntaxException: public ctor(String,Int32)",
            "ExpressionSyntaxException: public ctor(String,Int32,Exception)",
            "ExpressionSyntaxException: protected ctor(SerializationInfo,StreamingContext)",

            "ExpressionFunctionException: public ctor(String)",
            "ExpressionFunctionException: public ctor(String,Exception)",
            "ExpressionFunctionException: public ctor(String,Int32)",
            "ExpressionFunctionException: public ctor(String,Int32,Exception)",
            "ExpressionFunctionException: protected ctor(SerializationInfo,StreamingContext)",

            "FunctionArgumentException: public ctor(String,Int32)",
            "FunctionArgumentException: public ctor(String,Int32,Exception)",
            "FunctionArgumentException: protected ctor(SerializationInfo,StreamingContext)",
            "FunctionArgumentException: public static method ForMinimumArity(String,Int32) : FunctionArgumentException",
            "FunctionArgumentException: public property Function : String { get; }",
            "FunctionArgumentException: public property ArgumentCount : Int32 { get; }",
            "FunctionArgumentException: public property IsMinimum : Boolean { get; }"
        };

        public TestContext TestContext { get; set; }

        private static string FormatParameters(ParameterInfo[] parameters)
        {
            return string.Join(
                ",",
                parameters.Select(p =>
                    ReflectionFormat.FormatType(p.ParameterType) +
                    (p.HasDefaultValue ? "=" + (p.DefaultValue == null ? "null" : Convert.ToString(p.DefaultValue, CultureInfo.InvariantCulture)) : string.Empty)));
        }

        private static string Visibility(MethodBase method)
        {
            if (method == null)
                return null;

            if (method.IsPublic)
                return "public";

            if (method.IsFamily || method.IsFamilyOrAssembly)
                return "protected";

            return null;
        }

        private static IEnumerable<string> DescribeVisibleMembers(Type type)
        {
            const BindingFlags flags = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var prefix = type.Name + ": ";

            foreach (var ctor in type.GetConstructors(flags))
            {
                var visibility = Visibility(ctor);
                if (visibility != null && !ctor.IsStatic)
                    yield return prefix + visibility + " ctor(" + FormatParameters(ctor.GetParameters()) + ")";
            }

            foreach (var method in type.GetMethods(flags))
            {
                var visibility = Visibility(method);
                if (visibility == null || method.IsSpecialName)
                    continue;

                yield return prefix + visibility + (method.IsStatic ? " static" : string.Empty) + " method " + method.Name +
                             "(" + FormatParameters(method.GetParameters()) + ") : " + ReflectionFormat.FormatType(method.ReturnType);
            }

            foreach (var property in type.GetProperties(flags))
            {
                var getter = Visibility(property.GetGetMethod(true));
                var setter = Visibility(property.GetSetMethod(true));
                if (getter == null && setter == null)
                    continue;

                var visibility = getter == "public" || setter == "public" ? "public" : "protected";
                yield return prefix + visibility + " property " + property.Name + " : " + ReflectionFormat.FormatType(property.PropertyType) +
                             " {" + (getter != null ? " get;" : string.Empty) + (setter != null ? " set;" : string.Empty) + " }";
            }

            foreach (var field in type.GetFields(flags))
            {
                if (field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly)
                    yield return prefix + (field.IsPublic ? "public" : "protected") + (field.IsStatic ? " static" : string.Empty) +
                                 " field " + field.Name + " : " + ReflectionFormat.FormatType(field.FieldType);
            }

            foreach (var evt in type.GetEvents(flags))
            {
                if (Visibility(evt.GetAddMethod(true)) != null)
                    yield return prefix + "event " + evt.Name;
            }

            foreach (var nested in type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (nested.IsNestedPublic || nested.IsNestedFamily || nested.IsNestedFamORAssem)
                    yield return prefix + "nested type " + nested.Name;
            }
        }

        [TestMethod]
        public void PublicTypes_EqualBaselinePlusCompiledExpression_Test()
        {
            var actual = typeof(MathRuleEngine).Assembly.GetExportedTypes().Select(t => t.FullName).OrderBy(n => n, StringComparer.Ordinal).ToList();
            var expected = ExpectedPublicTypes.OrderBy(n => n, StringComparer.Ordinal).ToList();

            CollectionAssert.AreEqual(expected, actual, "Public types: " + string.Join(", ", actual));
        }

        [TestMethod]
        public void PublicSurface_EqualsBaselinePlusDeclaredAdditions_Test()
        {
            var actual = new HashSet<string>(
                typeof(MathRuleEngine).Assembly.GetExportedTypes().SelectMany(DescribeVisibleMembers),
                StringComparer.Ordinal);
            var expected = new HashSet<string>(ExpectedMembers, StringComparer.Ordinal);

            var missing = expected.Where(m => !actual.Contains(m)).OrderBy(m => m, StringComparer.Ordinal).ToList();
            var extra = actual.Where(m => !expected.Contains(m)).OrderBy(m => m, StringComparer.Ordinal).ToList();

            Assert.IsTrue(
                missing.Count == 0 && extra.Count == 0,
                "Public surface drift.\nMissing (removed/changed vs BASELINE+declared):\n  " + string.Join("\n  ", missing) +
                "\nExtra (undeclared additions):\n  " + string.Join("\n  ", extra));
        }

        [TestMethod]
        public void CompiledExpression_IsSealed_WithNoPublicConstructor_Test()
        {
            var type = typeof(CompiledExpression);

            Assert.IsTrue(type.IsSealed);
            Assert.AreEqual(0, type.GetConstructors(BindingFlags.Instance | BindingFlags.Public).Length);
        }

        [TestMethod]
        public void TestSeams_AreNotPublic_Test()
        {
            const BindingFlags publicMembers = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;
            var seams = new[] { "ThrowOnCompileFault", "CompileFaultInjector", "SimulateNoDynamicCode", "PreferInterpretedExpressionTrees" };

            foreach (var seam in seams)
                Assert.AreEqual(0, typeof(MathRuleEngineOptions).GetMember(seam, publicMembers).Length, seam + " must not be public.");

            foreach (var hook in new[] { "TestOptionsOverride", "PrepareCount", "CompileCount", "CachedExpressionCount", "RetainedNodeCount", "TryPeekCachedProgram" })
                Assert.AreEqual(0, typeof(MathRuleEngine).GetMember(hook, publicMembers).Length, hook + " must not be public.");

            foreach (var hook in new[] { "Program", "IsCompiled" })
                Assert.AreEqual(0, typeof(CompiledExpression).GetMember(hook, publicMembers).Length, hook + " must not be public.");
        }

#if NETCOREAPP
        [TestMethod]
        public void CompiledHandle_ParameterlessFunctionFree_AllocatesZeroBytesPerEvaluation_Test()
        {
            var engine = PinnedEngine.Create();

            var handle = engine.Compile("(1 + 2 * 3 - 4 / 5) ^ 2 > 3 ? 7 % 4 : 50%");
            Assert.IsTrue(handle.IsCompiled);

            var sink = 0.0;
            for (var i = 0; i < 1000; i++)
                sink += handle.Evaluate();

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 1000; i++)
                sink += handle.Evaluate();
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.AreEqual(3000.0 * 2, sink, "Sanity: (1+6-0.8)^2 = 38.44 > 3, so every call returns 7 % 4 = 3.");
            Assert.AreEqual(0L, allocated, "A warmed parameterless, function-free compiled evaluation must allocate 0 bytes (" + allocated + " B over 1000 calls).");
        }
#endif
    }
}
