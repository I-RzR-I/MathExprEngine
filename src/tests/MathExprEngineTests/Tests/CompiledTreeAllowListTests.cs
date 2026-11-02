using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MathExprEngine;
using MathExprEngineTests.Helpers.Engines;
using MathExprEngineTests.Helpers.ExpressionTrees;
using MathExprEngineTests.Helpers.Formatting;
using MathExprEngineTests.Helpers.TestBases;

namespace MathExprEngineTests.Tests
{
    [TestClass]
    public class CompiledTreeAllowListTests : DepthGuardedTestBase
    {
        private static readonly string[] Corpus =
        {
            "1 + 2", "x - y", "x * y", "x / y", "x % y", "x%", "50% * 2", "100 % 30 %", "-x", "+x", "--x",
            "x ^ y", "-2^2", "2^3^2", "x < y", "x > y", "x <= y", "x >= y", "x == y", "x != y",
            "x ? 1 : 2", "x ? y : x ? 3 : 4", "x && y", "x || y", "(x && y) || (x ? 0 : y)",
            "x + x + x", "((x))", "unknownFn()", "sqrt(x)", "max(x, y, 3)", "sum(sqrt(x), max(y, 1), neg(2))",
            "mod(x, y)", "10^400", "1/0", "5%0", "sqrt(-1)", "x * 1.5 - y / 4 + (x > 2 ? x % 3 : -y^2)"
        };

        public TestContext TestContext { get; set; }

        private static LambdaExpression BuildLambda(string expression, bool allowNonFinite)
        {
            var engine = PinnedEngine.Create(new MathRuleEngineOptions { AllowNonFiniteResults = allowNonFinite });

            var handle = engine.Compile(expression);
            Assert.IsTrue(handle.IsCompiled, "Corpus entry did not compile: '" + expression + "', outcome " + handle.Program.LastOutcome);

            var lambda = handle.Program.BuildLambda();
            Assert.IsNotNull(lambda);

            return lambda;
        }

        [TestMethod]
        public void BuildLambda_EveryGrammarConstruct_BothNonFiniteSettings_UsesOnlyAllowListedNodes_Test()
        {
            var violations = new List<string>();
            var seenTypes = new HashSet<ExpressionType>();
            var calledMethods = new HashSet<string>();

            foreach (var allowNonFinite in new[] { false, true })
            {
                foreach (var expression in Corpus)
                {
                    var lambda = BuildLambda(expression, allowNonFinite);
                    var visitor = new AllowListVisitor(lambda);
                    visitor.Visit(lambda);

                    foreach (var v in visitor.Violations)
                        violations.Add("[" + expression + ", AllowNonFiniteResults=" + allowNonFinite + "] " + v);

                    seenTypes.UnionWith(visitor.SeenNodeTypes);
                    calledMethods.UnionWith(visitor.CalledMethods);
                }
            }

            TestContext.WriteLine("Node types seen: " + string.Join(", ", seenTypes.OrderBy(t => t.ToString())));
            TestContext.WriteLine("Methods called: " + string.Join(" | ", calledMethods.OrderBy(m => m)));

            Assert.AreEqual(0, violations.Count, "Allow-list violations:\n" + string.Join("\n", violations.Distinct().Take(40)));
            Assert.IsTrue(calledMethods.Contains(CompiledTreeAllowList.ResolveFunctionKey), "The corpus must exercise late-bound function resolution.");
        }

        [TestMethod]
        public void BuildLambda_RootShape_IsClosureFreeThreeParameterLambdaReturningDouble_Test()
        {
            var lambda = BuildLambda("x + sqrt(y) + f(1, 2)", false);

            Assert.AreEqual(3, lambda.Parameters.Count);
            Assert.AreEqual(typeof(IDictionary<string, Func<double[], double>>), lambda.Parameters[0].Type);
            Assert.AreEqual(typeof(object[]), lambda.Parameters[1].Type);
            Assert.AreEqual(typeof(int), lambda.Parameters[2].Type);
            Assert.AreEqual(typeof(double), lambda.ReturnType);
            Assert.AreEqual(typeof(double), lambda.Body.Type);
        }

        [TestMethod]
        public void AllowListedMethods_AllResolve_TrimmingCheck_Test()
        {
            var assembly = typeof(MathRuleEngine).Assembly;

            foreach (var key in CompiledTreeAllowList.AllowedMethods)
            {
                var open = key.IndexOf('(');
                var qualified = key.Substring(0, open);
                var lastDot = qualified.LastIndexOf('.');
                var typeName = qualified.Substring(0, lastDot);
                var methodName = qualified.Substring(lastDot + 1);

                var type = typeName == "System.Math" ? typeof(Math) : assembly.GetType(typeName, false);
                Assert.IsNotNull(type, "Allow-listed declaring type not found: " + typeName);

                var matches = type
                    .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(m => m.Name == methodName && ReflectionFormat.MethodKey(m) == key)
                    .ToList();

                Assert.AreEqual(1, matches.Count, "Allow-listed method must resolve exactly once: " + key);
            }
        }

        [TestMethod]
        public void Visitor_SelfTest_RejectsInvokeNewMemberAccessDynamicAndForeignMethods_Test()
        {
            var root = BuildLambda("x + 1", false);
            var forbidden = new List<KeyValuePair<string, Expression>>
            {
                new KeyValuePair<string, Expression>("Invoke", Expression.Invoke(Expression.Constant(new Func<double>(() => 1.0)))),
                new KeyValuePair<string, Expression>("New", Expression.New(typeof(object))),
                new KeyValuePair<string, Expression>("MemberAccess", Expression.Property(Expression.Constant("abc"), "Length")),
                new KeyValuePair<string, Expression>("Dynamic", Expression.Dynamic(new NoOpBinder(), typeof(object), Expression.Constant(1.0))),
                new KeyValuePair<string, Expression>("Call", Expression.Call(typeof(Math).GetMethod("Abs", new[] { typeof(double) }), Expression.Constant(1.0))),
                new KeyValuePair<string, Expression>("Add", Expression.Add(Expression.Constant(1.0), Expression.Constant(2.0), typeof(Math).GetMethod("Max", new[] { typeof(double), typeof(double) }))),
                new KeyValuePair<string, Expression>("Convert", Expression.Convert(Expression.Constant(1), typeof(double))),
                new KeyValuePair<string, Expression>("Parameter", Expression.Parameter(typeof(double), "foreign")),
                new KeyValuePair<string, Expression>("Divide", Expression.Divide(Expression.Constant(1.0), Expression.Constant(3.0)))
            };

            foreach (var entry in forbidden)
            {
                var visitor = new AllowListVisitor(root);
                visitor.Visit(entry.Value);

                Assert.IsTrue(
                    visitor.Violations.Any(v => v.StartsWith(entry.Key, StringComparison.Ordinal)),
                    "The allow-list visitor failed to reject a " + entry.Key + " expression. Violations: " + string.Join(" | ", visitor.Violations));
            }
        }
    }
}
