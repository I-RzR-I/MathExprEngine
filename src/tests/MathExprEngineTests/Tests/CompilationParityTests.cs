using System;
using System.Collections.Generic;
using System.Linq;
using MathExprEngine;
using MathExprEngine.Exceptions;
using MathExprEngine.Models;
using MathExprEngineTests.Helpers;
using MathExprEngineTests.Helpers.Assertions;
using MathExprEngineTests.Helpers.Engines;
using MathExprEngineTests.Helpers.Fakes;
using MathExprEngineTests.Helpers.Inputs;
using MathExprEngineTests.Helpers.Parity;
using MathExprEngineTests.Helpers.TestBases;
using MathExprEngineTests.Models;

namespace MathExprEngineTests.Tests
{
    [TestClass]
    public class CompilationParityTests : ParityDepthGuardedTestBase
    {
        private static readonly Dictionary<string, ParityCase> Corpus = BuildCorpus();

        private static Dictionary<string, ParityCase> BuildCorpus()
        {
            var c = new Dictionary<string, ParityCase>();

            void Add(string id, string expr, Func<IDictionary<string, object>> p = null, Action<MathRuleEngineOptions> cfg = null,
                Action<MathRuleEngine> setup = null, double? value = null, Type throws = null)
            {
                c.Add(id, new ParityCase { Expression = expr, Parameters = p, Configure = cfg, Setup = setup, ExpectedValue = value, ExpectedException = throws });
            }

            Add("U_Arith1", "2 + 3 * (5 - 2)", value: 11);
            Add("U_Prec1", "2 + 3 * 4", value: 14);
            Add("U_Prec2", "(2 + 3) * 4", value: 20);
            Add("U_PowRightAssoc", "2 ^ 3 ^ 2", value: 512);
            Add("U_NegPow", "-2^2", value: -4);
            Add("U_NegPowHalf", "-4^0.5", value: -2);
            Add("U_PowNeg", "2^-3", value: 0.125);
            Add("U_NegPowNeg", "-2^-2", value: -0.25);
            Add("U_Mod", "10 % 3", value: 1);
            Add("U_ModNeg", "10 % -3", value: 1);
            Add("U_Pct", "50%", value: 0.5);
            Add("U_PctTimes", "50% * 2", value: 1);
            Add("U_ModThenPct", "100 % 30 %");
            Add("U_PctPlus", "100% + 2", value: 0);
            Add("U_ParenPctPlus", "(100%) + 2", value: 3);
            Add("U_SqrtNeg", "sqrt(-1)", throws: typeof(ExpressionFunctionException));
            Add("U_ZeroNegPow", "0^-1", throws: typeof(MathRuleEngineException));
            Add("U_HugePow", "10^400", throws: typeof(MathRuleEngineException));
            Add("U_HugePow_Allow", "10^400", cfg: OptionPresets.AllowNonFinite, value: double.PositiveInfinity);
            Add("U_SqrtNeg_Allow", "sqrt(-1)", cfg: OptionPresets.AllowNonFinite, throws: typeof(ExpressionFunctionException));
            Add("U_Sqrt", "sqrt(16)", value: 4);
            Add("U_Pow", "pow(2, 5)", value: 32);
            Add("U_Sin", "sin(3.1415926535/2)");
            Add("U_Sum", "sum(1, 2, 3, 4)", value: 10);
            Add("U_Max", "max(3, 11, 5)", value: 11);
            Add("U_ModFn", "mod(10, 4)", value: 2);
            Add("U_Neg", "neg(7)", value: -7);
            Add("U_Gt", "5 > 3", value: 1);
            Add("U_Le", "5 <= 3", value: 0);
            Add("U_Eq", "5 == 5", value: 1);
            Add("U_Ne", "5 != 5", value: 0);
            Add("U_And", "1 && 1", value: 1);
            Add("U_AndShort", "0 && (1/0)", value: 0);
            Add("U_OrShort", "1 || (1/0)", value: 1);
            Add("U_Tern1", "1 ? 10 : 20", value: 10);
            Add("U_Tern0", "0 ? 10 : 20", value: 20);
            Add("U_TernLazy1", "1 ? 10 : (1/0)", value: 10);
            Add("U_TernLazy0", "0 ? (1/0) : 10", value: 10);
            Add("U_SyntaxErr", "2 + * 3", throws: typeof(ExpressionSyntaxException));
            Add("U_PriceNull", "price", () => new Dictionary<string, object> { ["price"] = null }, throws: typeof(MathRuleEngineException));
            Add("U_PriceAbc", "price", () => new Dictionary<string, object> { ["price"] = "abc" }, throws: typeof(MathRuleEngineException));
            Add("U_PriceDate", "price", () => new Dictionary<string, object> { ["price"] = new DateTime(2026, 1, 2) }, throws: typeof(MathRuleEngineException));
            Add("U_PriceMissing", "price", throws: typeof(ExpressionSyntaxException));
            Add("U_PriceRule", "price * qty > 100 && discount < 0.5 ? price * qty * (1 - discount) : price * qty",
                () => new Dictionary<string, object> { ["price"] = 25.5m, ["qty"] = 5, ["discount"] = "0.1" });

            Add("L_AndUnknownFn", "0 && unknownFn()", value: 0);
            Add("L_OrUndefined", "1 || undefinedParam", value: 1);
            Add("L_TernBadType", "0 ? bad : 1", () => new Dictionary<string, object> { ["bad"] = new object() }, value: 1);
            Add("L_TernDivZero", "1 ? 2 : 1/0", value: 2);
            Add("L_AndUnknownFnReached", "1 && unknownFn()", throws: typeof(ExpressionFunctionException));
            Add("L_OrUndefinedReached", "0 || undefinedParam", throws: typeof(ExpressionSyntaxException));
            Add("L_TernBadTypeReached", "1 ? bad : 1", () => new Dictionary<string, object> { ["bad"] = new object() }, throws: typeof(MathRuleEngineException));
            Add("L_Unknown", "unknown(1)", throws: typeof(ExpressionFunctionException));
            Add("L_UndefinedVar", "undefinedVar + 1", throws: typeof(ExpressionSyntaxException));
            Add("L_CaseInsensitiveParam", "X + x", ParameterSets.XFactory(2.0), value: 4);
            Add("L_CaseInsensitiveFn", "SQRT(9) + Sqrt(16)", value: 7);

            Add("F_DivZero", "1/0", throws: typeof(MathRuleEngineException));
            Add("F_ModZero", "5%0", throws: typeof(MathRuleEngineException));
            Add("F_ModFnZero", "mod(1,0)", throws: typeof(MathRuleEngineException));
            Add("F_SumEmpty", "sum()", throws: typeof(ExpressionFunctionException));
            Add("F_XDivZero", "x/0", ParameterSets.XFactory(3.0), throws: typeof(MathRuleEngineException));
            Add("F_XModZero", "x % 0", ParameterSets.XFactory(3.0), throws: typeof(MathRuleEngineException));
            Add("F_DivSelfMinus", "1/(x-x)", ParameterSets.XFactory(3.0), throws: typeof(MathRuleEngineException));
            Add("F_DivZero_Allow", "1/0", cfg: OptionPresets.AllowNonFinite, throws: typeof(MathRuleEngineException));
            Add("F_ModZero_Allow", "1%0", cfg: OptionPresets.AllowNonFinite, throws: typeof(MathRuleEngineException));
            Add("F_DivSelfMinus_Allow", "1/(x-x)", ParameterSets.XFactory(3.0), OptionPresets.AllowNonFinite, throws: typeof(MathRuleEngineException));
            Add("F_Overflow", "1e308", throws: typeof(ExpressionSyntaxException));
            Add("F_MulOverflow", "x * 10", ParameterSets.XFactory(1e308), throws: typeof(MathRuleEngineException));
            Add("F_DeepConst", "((((1/0))))", throws: typeof(MathRuleEngineException));
            Add("F_ColumnSpace", " 1/0", throws: typeof(MathRuleEngineException));

            Add("T_sbyte", "x + x * 2", ParameterSets.XFactory((sbyte)-3), value: -9);
            Add("T_byte", "x + x * 2", ParameterSets.XFactory((byte)200), value: 600);
            Add("T_short", "x + x * 2", ParameterSets.XFactory((short)-300), value: -900);
            Add("T_ushort", "x + x * 2", ParameterSets.XFactory((ushort)60000), value: 180000);
            Add("T_int", "x + x * 2", ParameterSets.XFactory(-7), value: -21);
            Add("T_uint", "x + x * 2", ParameterSets.XFactory(4000000000u), value: 12000000000);
            Add("T_long", "x + x * 2", ParameterSets.XFactory(-9000000000L), value: -27000000000);
            Add("T_ulong", "x + x * 2", ParameterSets.XFactory(ulong.MaxValue));
            Add("T_float", "x + x * 2", ParameterSets.XFactory(0.1f));
            Add("T_double", "x + x * 2", ParameterSets.XFactory(0.1));
            Add("T_decimal", "x + x * 2", ParameterSets.XFactory(0.1m));
            Add("T_decimalMax", "x", ParameterSets.XFactory(decimal.MaxValue));
            Add("T_boolTrue", "x + x * 2", ParameterSets.XFactory(true), value: 3);
            Add("T_boolFalse", "x + x * 2", ParameterSets.XFactory(false), value: 0);
            Add("T_stringThousands", "x + x * 2", ParameterSets.XFactory("1,234.5"));
            Add("T_stringSpaces", "x", ParameterSets.XFactory("  42 "));
            Add("T_stringNegZero", "x", ParameterSets.XFactory("-0"));
            Add("T_stringInvariant", "x", ParameterSets.XFactory("3.25"), value: 3.25);
            Add("T_null", "x", ParameterSets.XFactory(null), throws: typeof(MathRuleEngineException));
            Add("T_char", "x", ParameterSets.XFactory('5'), throws: typeof(MathRuleEngineException));
            Add("T_dateTime", "x", ParameterSets.XFactory(new DateTime(2026, 10, 21)), throws: typeof(MathRuleEngineException));
            Add("T_object", "x", ParameterSets.XFactory(new object()), throws: typeof(MathRuleEngineException));
            Add("T_unparsable", "x", ParameterSets.XFactory("abc"), throws: typeof(MathRuleEngineException));
            Add("T_enum", "x", ParameterSets.XFactory(DayOfWeek.Monday), throws: typeof(MathRuleEngineException));
            Add("T_timeSpan", "x", ParameterSets.XFactory(TimeSpan.FromSeconds(1)), throws: typeof(MathRuleEngineException));
            Add("T_nullSecondRead", "1 ? 2 : x", ParameterSets.XFactory(null), value: 2);
            Add("T_nanParam", "x", ParameterSets.XFactory(double.NaN), throws: typeof(MathRuleEngineException));
            Add("T_nanParam_Allow", "x + 1", ParameterSets.XFactory(double.NaN), OptionPresets.AllowNonFinite, value: double.NaN);
            Add("T_infString_Allow", "x", ParameterSets.XFactory("Infinity"), OptionPresets.AllowNonFinite);

            foreach (var op in new[] { "<", ">", "<=", ">=", "==", "!=" })
            {
                Add("N_NaN_" + op + "_1", "x " + op + " 1", ParameterSets.XFactory(double.NaN), OptionPresets.AllowNonFinite);
                Add("N_1_" + op + "_NaN", "1 " + op + " x", ParameterSets.XFactory(double.NaN), OptionPresets.AllowNonFinite);
                Add("N_NaN_" + op + "_NaN", "x " + op + " x", ParameterSets.XFactory(double.NaN), OptionPresets.AllowNonFinite);
                Add("N_Inf_" + op + "_Inf", "x " + op + " x", ParameterSets.XFactory(double.PositiveInfinity), OptionPresets.AllowNonFinite);
                Add("N_NaN_" + op + "_Default", "x " + op + " 1", ParameterSets.XFactory(double.NaN), throws: typeof(MathRuleEngineException));
            }

            Add("Z_NegZeroLiteral", "-0", value: -0.0);
            Add("Z_ZeroTimesNeg", "0 * -1", value: -0.0);
            Add("Z_ParamNegZero", "x", ParameterSets.XFactory(-0.0), value: -0.0);
            Add("Z_NegParamZero", "-x", ParameterSets.XFactory(0.0), value: -0.0);
            Add("Z_ParamTimesZero", "x * 0", ParameterSets.XFactory(-1.0), value: -0.0);
            Add("Z_DivByNegZero", "1/x", ParameterSets.XFactory(-0.0), throws: typeof(MathRuleEngineException));
            Add("Z_ModByNegZero", "1 % x", ParameterSets.XFactory(-0.0), throws: typeof(MathRuleEngineException));
            Add("Z_NegZeroEqZero", "x == 0", ParameterSets.XFactory(-0.0), value: 1);
            Add("Z_NegFnZero", "neg(0)", value: -0.0);
            Add("Z_NegModOne", "-1 % 1", value: -0.0);
            Add("Z_NegZeroPct", "-0%", value: -0.0);
            Add("Z_NegZeroCond", "x ? 1 : 2", ParameterSets.XFactory(-0.0), value: 2);
            Add("Z_NegZeroAnd", "1 && x", ParameterSets.XFactory(-0.0), value: 0);
            Add("Z_NegZeroNe", "x != 0", ParameterSets.XFactory(-0.0), value: 0);

            Action<MathRuleEngineOptions> d10 = o => o.MaxEvaluationDepth = 10;
            for (var n = 9; n <= 11; n++)
            {
                var overLimit = n > 10 ? typeof(MathRuleEngineException) : null;
                Add("D_Chain_" + n, ExpressionText.FlatChain(n), cfg: d10, value: n <= 10 ? n : (double?)null,
                    throws: overLimit);
                Add("D_Unary_" + n, new string('-', n - 1) + "1", cfg: d10, throws: overLimit);
                Add("D_NegFn_" + n, ExpressionText.NestedNeg(n - 1), cfg: d10,
                    throws: overLimit);
                Add("D_Pct_" + n, "50" + new string('%', n - 1), cfg: d10, throws: overLimit);
                Add("D_ParamChain_" + n, string.Join("+", Enumerable.Repeat("x", n)), ParameterSets.XFactory(1), d10,
                    throws: overLimit);
                Add("D_Ternary_" + n, string.Concat(Enumerable.Repeat("1 ? ", n - 1)) + "1" + string.Concat(Enumerable.Repeat(" : 0", n - 1)), cfg: d10,
                    throws: overLimit);
            }

            Add("D_Max1_Leaf", "1", cfg: o => o.MaxEvaluationDepth = 1, value: 1);
            Add("D_Max1_Unary", "-1", cfg: o => o.MaxEvaluationDepth = 1, throws: typeof(MathRuleEngineException));
            Add("D_Default_512", ExpressionText.FlatChain(512), value: 512);
            Add("D_Default_513", ExpressionText.FlatChain(513), throws: typeof(MathRuleEngineException));
            Add("D_Default_256", ExpressionText.FlatChain(256), value: 256);
            Add("D_Default_257", ExpressionText.FlatChain(257), value: 257);

            for (var n = 5; n <= 9; n++)
            {
                var inner = ExpressionText.FlatChain(n);
                Add("R_Reenter_" + n, "1 + re()", cfg: d10,
                    setup: e => e.RegisterFunction("re", a => e.Evaluate(inner)));
            }

            Action<MathRuleEngine> depthFn = e => e.RegisterFunction("depth", a => EvaluationContext.CurrentDepth);
            Add("K13_Leaf", "depth()", setup: depthFn);
            Add("K13_Plus", "1 + depth()", setup: depthFn);
            Add("K13_Unary", "-(-(depth()))", setup: depthFn);
            Add("K13_InArgs", "sum(1, depth(), neg(depth()))", setup: depthFn);
            Add("K13_Ternary", "1 ? depth() : 0", setup: depthFn);
            Add("K13_Or", "0 || depth()", setup: depthFn);
            Add("K13_Nested", "depth() + 2 * (3 - depth()) ^ 1", setup: depthFn);

            Add("H_InvalidOp", "1 + boom()", setup: e => e.RegisterFunction("boom", a => throw new InvalidOperationException("db-host-01")),
                throws: typeof(ExpressionFunctionException));
            Add("H_ArgEx", "2 * boom()", setup: e => e.RegisterFunction("boom", a => throw new ArgumentException("bad arg", "a")),
                throws: typeof(ExpressionFunctionException));
            Add("H_EngineNoColumn", "1 + boom()", setup: e => e.RegisterFunction("boom", a => throw new MathRuleEngineException("host says no")),
                throws: typeof(MathRuleEngineException));
            Add("H_EngineWithColumn", "1 + boom()", setup: e => e.RegisterFunction("boom", a => throw new MathRuleEngineException("host col", 0)),
                throws: typeof(MathRuleEngineException));
            Add("H_FnArgEx", "1 + mod(1)", throws: typeof(ExpressionFunctionException));
            Add("H_ModZeroColumn", "1 + mod(10, 0)", throws: typeof(MathRuleEngineException));
            Add("H_NaNFn", "nan() ? 1 : 2", setup: HostFunctions.RegisterNaN, throws: typeof(MathRuleEngineException));
            Add("H_NaNFn_Allow", "nan() + 1", cfg: OptionPresets.AllowNonFinite, setup: HostFunctions.RegisterNaN, value: double.NaN);
            Add("H_ArgsOrder", "f(1, 2, 3)", setup: e => e.RegisterFunction("f", a => a[0] * 100 + a[1] * 10 + a[2]), value: 123);
            Add("H_NestedThrowInArg", "max(1, boom())", setup: e => e.RegisterFunction("boom", a => throw new InvalidOperationException("inner")),
                throws: typeof(ExpressionFunctionException));

            Add("K10_NoParamsUsed", "1 + 1", () => new ThrowingEnumeratorDictionary(), throws: typeof(MathRuleEngineException));
            Add("K10_ParamUsed", "x", () => new ThrowingEnumeratorDictionary(), throws: typeof(MathRuleEngineException));

            Add("K11_UpperKeyLowerRead", "x", () => new Dictionary<string, object>(StringComparer.Ordinal) { { "X", 1.0 } }, value: 1);
            Add("K11_BothCases", "x + X", () => new Dictionary<string, object>(StringComparer.Ordinal) { { "X", 1.0 }, { "x", 2.0 } });
            Add("K11_EmptyDict", "1", () => new Dictionary<string, object>(), value: 1);

            Add("G_TrailingToken", "1 2", throws: typeof(ExpressionSyntaxException));
            Add("G_BangIsNotAUnaryOperator", "!0 + 1");
            Add("G_UnaryPlus", "+-+3", value: -3);
            Add("G_LongLimitBeatsTokens", "1+1+1+1", cfg: o => { o.MaxExpressionLength = 3; o.MaxTokens = 2; }, throws: typeof(MathRuleEngineException));
            Add("G_TokensBeatNodes", "1+1+1+1", cfg: o => { o.MaxTokens = 3; o.MaxNodes = 2; }, throws: typeof(MathRuleEngineException));
            Add("G_NodesLimit", "1+1+1+1", cfg: o => o.MaxNodes = 2, throws: typeof(MathRuleEngineException));
            Add("G_ArgsLimit", "sum(1,2,3)", cfg: o => o.MaxFunctionArgs = 2, throws: typeof(MathRuleEngineException));

            return c;
        }

        public TestContext TestContext { get; set; }

        public static IEnumerable<object[]> CorpusByPath => DynamicDataRows.ByCompiledPath(Corpus.Keys);

        public static IEnumerable<object[]> CorpusIds => DynamicDataRows.Ids(Corpus.Keys);

        private static bool IsCompilePath(ParityPath path) => path == ParityPath.Compile || path == ParityPath.CompileInterpretedTrees;

        [DataTestMethod]
        [DynamicData(nameof(CorpusByPath), DynamicDataDisplayName = nameof(TestDisplayNames.Format), DynamicDataDisplayNameDeclaringType = typeof(TestDisplayNames))]
        public void Corpus_CompiledPathMatchesInterpreter(string caseId, string pathName)
        {
            var c = Corpus[caseId];
            var path = ParityAssert.ParsePath(pathName);

            var oracle = ParityAssert.Matches(path, c);

            TestContext.WriteLine(caseId + " / " + path + ": " + oracle.Describe());
        }

        [DataTestMethod]
        [DynamicData(nameof(CorpusIds), DynamicDataDisplayName = nameof(TestDisplayNames.Format), DynamicDataDisplayNameDeclaringType = typeof(TestDisplayNames))]
        public void Corpus_InterpreterOracleMatchesHandDerivedExpectation(string caseId)
        {
            var c = Corpus[caseId];
            var oracle = ParityAssert.Run(ParityPath.Interpreter, c);

            if (c.ExpectedException != null)
            {
                Assert.IsTrue(oracle.Threw, caseId + ": " + oracle.Describe());
                Assert.AreEqual(c.ExpectedException, oracle.Error.GetType(), caseId + ": " + oracle.Describe());
            }
            else if (c.ExpectedValue != null)
            {
                Assert.IsFalse(oracle.Threw, caseId + ": " + oracle.Describe());
                BitAssert.AreEqual(c.ExpectedValue.Value, oracle.Value, caseId + ": " + oracle.Describe());
            }
        }

        [TestMethod]
        public void ModuloAndDivisionByZero_ExactMessages_AllPaths()
        {
            foreach (var path in ParityAssert.CompiledPaths.Concat(new[] { ParityPath.Interpreter }))
            {
                var mod = ParityAssert.Run(path, "x % 0", ParameterSets.XFactory(4.0));
                Assert.AreEqual("Error: Modulo by zero at column 3", mod.Error?.Message, path + ": " + mod.Describe());
                Assert.AreEqual(2, ((MathRuleEngineException)mod.Error).Column);

                var div = ParityAssert.Run(path, "x / 0", ParameterSets.XFactory(4.0));
                Assert.AreEqual("Error: Division by zero at column 3", div.Error?.Message, path + ": " + div.Describe());
            }
        }

        [DataTestMethod]
        [DataRow("0 && f()", 0, 0)]
        [DataRow("1 || f()", 0, 0)]
        [DataRow("1 && f()", 1, 0)]
        [DataRow("0 || f()", 1, 0)]
        [DataRow("1 ? f() : g()", 1, 0)]
        [DataRow("0 ? f() : g()", 0, 1)]
        [DataRow("f() + f() * g()", 2, 1)]
        [DataRow("max(f(), g(), f())", 2, 1)]
        public void ShortCircuit_SideEffectCounts_MatchInterpreter(string expression, int expectedF, int expectedG)
        {
            foreach (var path in ParityAssert.AllPaths)
            {
                var log = new List<string>();
                var engine = ParityAssert.CreateEngine(path, null, e =>
                {
                    e.RegisterFunction("f", a => { log.Add("f"); return 1; });
                    e.RegisterFunction("g", a => { log.Add("g"); return 2; });
                });

                if (path == ParityPath.TieredSecondUse)
                {
                    engine.Evaluate(expression);
                    log.Clear();
                }

                var outcome = ParityAssert.RunOn(engine, path == ParityPath.TieredSecondUse ? ParityPath.Eager : path, expression);

                Assert.IsFalse(outcome.Threw, path + ": " + outcome.Describe());
                Assert.AreEqual(expectedF, log.Count(s => s == "f"), path + " f count for " + expression);
                Assert.AreEqual(expectedG, log.Count(s => s == "g"), path + " g count for " + expression);
            }
        }

        [TestMethod]
        public void FunctionArguments_EvaluatedLeftToRightOnce_AllPaths()
        {
            string oracleOrder = null;

            foreach (var path in ParityAssert.AllPaths)
            {
                var log = new List<string>();
                var engine = ParityAssert.CreateEngine(path, null, e =>
                {
                    e.RegisterFunction("a", x => { log.Add("a"); return 1; });
                    e.RegisterFunction("b", x => { log.Add("b"); return 2; });
                    e.RegisterFunction("c", x => { log.Add("c"); return 3; });
                    e.RegisterFunction("f", x => { log.Add("f"); return x.Sum(); });
                });

                if (path == ParityPath.TieredSecondUse)
                {
                    engine.Evaluate("f(a(), b() + c(), a())");
                    log.Clear();
                }

                var r = IsCompilePath(path)
                    ? engine.Compile("f(a(), b() + c(), a())").Evaluate()
                    : engine.Evaluate("f(a(), b() + c(), a())");

                Assert.AreEqual(7.0, r, path.ToString());
                var order = string.Join(",", log);
                Assert.AreEqual("a,b,c,a,f", order, path.ToString());
                oracleOrder = oracleOrder ?? order;
                Assert.AreEqual(oracleOrder, order, path.ToString());
            }
        }

        [TestMethod]
        public void ArgumentReRegistersCalledFunction_LookupBeforeArguments_AllPaths()
        {
            foreach (var path in ParityAssert.AllPaths)
            {
                MathRuleEngine engine = null;
                Action resetF = () => engine.RegisterFunction("f", a => 1);
                engine = ParityAssert.CreateEngine(path, null, e =>
                {
                    e.RegisterFunction("f", a => 1);
                    e.RegisterFunction("g", a => { e.RegisterFunction("f", b => 2); return 0; });
                });

                double result;
                if (IsCompilePath(path))
                {
                    var h = engine.Compile("f(g())");
                    result = h.Evaluate();
                    Assert.AreEqual(1.0, result, path + " (1st)");
                    resetF();
                    result = h.Evaluate();
                }
                else
                {
                    result = engine.Evaluate("f(g())");
                    if (path == ParityPath.TieredSecondUse)
                    {
                        Assert.AreEqual(1.0, result, path + " (1st)");
                        resetF();
                        result = engine.Evaluate("f(g())");
                    }
                }

                Assert.AreEqual(1.0, result, path.ToString());
                Assert.AreEqual(2.0, engine.Evaluate("f(0)"), path + ": g must have replaced f");
            }
        }

        [TestMethod]
        public void ParameterSnapshot_MutationDuringEvaluation_NotObservedInSameCall_AllPaths()
        {
            foreach (var path in ParityAssert.AllPaths)
            {
                var dict = new Dictionary<string, object> { { "x", 1.0 } };
                var engine = ParityAssert.CreateEngine(path, null, e =>
                    e.RegisterFunction("mutate", a => { dict["x"] = 100.0; return 0; }));

                Func<double> eval = IsCompilePath(path)
                    ? (Func<double>)(() => engine.Compile("x + mutate() + x").Evaluate(dict))
                    : () => engine.Evaluate("x + mutate() + x", dict);

                if (path == ParityPath.TieredSecondUse)
                {
                    Assert.AreEqual(2.0, eval(), path + " (1st)");
                    dict["x"] = 1.0;
                }

                Assert.AreEqual(2.0, eval(), path.ToString());
                Assert.AreEqual(100.0, dict["x"], "the mutation itself must have happened");

                Assert.AreEqual(200.0, eval(), path + " (next call)");
            }
        }

        [DataTestMethod]
        [DataRow("Interpreter", "Interpreter")]
        [DataRow("Interpreter", "Compile")]
        [DataRow("Compile", "Interpreter")]
        [DataRow("Compile", "Compile")]
        [DataRow("Eager", "Eager")]
        [DataRow("Eager", "Interpreter")]
        [DataRow("Interpreter", "Eager")]
        [DataRow("EagerInterpretedTrees", "CompileInterpretedTrees")]
        public void ReentrantMixedChain_SameBoundaryAsInterpreter(string outerName, string innerName)
        {
            var outerPath = ParityAssert.ParsePath(outerName);
            var innerPath = ParityAssert.ParsePath(innerName);

            int Boundary(ParityPath outerP, ParityPath innerP)
            {
                var last = -1;
                for (var n = 1; n <= 12; n++)
                {
                    var inner = ExpressionText.FlatChain(n);
                    var innerEngine = ParityAssert.CreateEngine(innerP, o => o.MaxEvaluationDepth = 10);
                    var handle = IsCompilePath(innerP) ? innerEngine.Compile(inner) : null;
                    var outerEngine = ParityAssert.CreateEngine(outerP, o => o.MaxEvaluationDepth = 10, e =>
                        e.RegisterFunction("re", a => handle != null ? handle.Evaluate() : innerEngine.Evaluate(inner)));

                    var outcome = ParityAssert.RunOn(outerEngine, outerP, "1 + re()");
                    Assert.AreEqual(0, EvaluationContext.CurrentDepth, "depth not restored (n=" + n + ")");

                    if (outcome.Threw)
                    {
                        Assert.AreEqual(typeof(MathRuleEngineException), outcome.Error.GetType(), outcome.Describe());
                        StringAssert.Contains(outcome.Error.Message, "evaluation depth exceeds the configured maximum (10)");
                        break;
                    }

                    Assert.AreEqual(1.0 + n, outcome.Value);
                    last = n;
                }

                return last;
            }

            var oracle = Boundary(ParityPath.Interpreter, ParityPath.Interpreter);
            var actual = Boundary(outerPath, innerPath);

            Assert.IsTrue(oracle > 0 && oracle < 12, "oracle boundary not found: " + oracle);
            Assert.AreEqual(oracle, actual, outerName + " -> " + innerName);

            var fresh = ParityAssert.CreateEngine(outerPath, o => o.MaxEvaluationDepth = 10);
            Assert.AreEqual(10.0, ParityAssert.RunOn(fresh, outerPath, ExpressionText.FlatChain(10)).Value);
        }

        [DataTestMethod]
        [DataRow("depth()", 1.0)]
        [DataRow("1 + depth()", 3.0)]
        [DataRow("-(-(depth()))", 3.0)]
        [DataRow("0 || depth()", 1.0)]
        [DataRow("sum(depth(), depth())", 4.0)]
        public void ObservedDepthInCustomFunction_SameInEveryRunMode(string expression, double expected)
        {
            var engine = new MathRuleEngine();
            engine.RegisterFunction("depth", a => EvaluationContext.CurrentDepth);

            for (var i = 0; i < 3; i++)
                Assert.AreEqual(expected, engine.Evaluate(expression), "call " + (i + 1) + " in mode " + RunMode.Current);

            Assert.AreEqual(expected, engine.Compile(expression).Evaluate(), "handle in mode " + RunMode.Current);
        }

        [TestMethod]
        public void ThrowingCustomFunction_DepthRestored_HandleReusable()
        {
            var shouldThrow = true;
            var engine = ParityAssert.CreateEngine(ParityPath.Compile, null, e =>
                e.RegisterFunction("f", a => shouldThrow ? throw new InvalidOperationException("x") : a[0]));

            var h = engine.Compile("1 + -(f(2))");
            Assert.IsTrue(h.IsCompiled);

            Assert.ThrowsException<ExpressionFunctionException>(() => h.Evaluate());
            Assert.AreEqual(0, EvaluationContext.CurrentDepth);

            shouldThrow = false;
            Assert.AreEqual(-1.0, h.Evaluate());
            Assert.AreEqual(0, EvaluationContext.CurrentDepth);
        }
    }
}
