using System;
using System.Collections.Generic;
using MathExprEngineTests.Helpers.Fakes;
using MathExprEngineTests.Helpers.Inputs;
using MathExprEngineTests.Helpers.Parity;
using MathExprEngineTests.Helpers.TestBases;
using MathExprEngineTests.Models;

namespace MathExprEngineTests.Tests
{
    [TestClass]
    public class ParameterBinderParityTests : ParityDepthGuardedTestBase
    {
        private static readonly Dictionary<string, ParityCase> Cases = Build();

        private static Dictionary<string, ParityCase> Build()
        {
            var c = new Dictionary<string, ParityCase>();

            void Add(string id, string expr, Func<IDictionary<string, object>> p) =>
                c.Add(id, new ParityCase { Expression = expr, Parameters = p });

            Add("A_Matching", "x * 2 + y", () => ParameterSets.OfIgnoreCase("x", 3.0, "y", 4));
            Add("A_MatchingOtherCase", "X * 2 + Y", () => ParameterSets.OfIgnoreCase("x", 3.0, "y", 4));
            Add("A_SameNameReadTwiceMixedCase", "x + X + x", () => ParameterSets.OfIgnoreCase("X", 1.5));
            Add("A_Missing", "x + missing", () => ParameterSets.OfIgnoreCase("x", 1.0));
            Add("A_MissingOnlyParam", "missing", () => ParameterSets.OfIgnoreCase("x", 1.0));
            Add("A_MissingNotReached", "0 && missing", () => ParameterSets.OfIgnoreCase("x", 1.0));
            Add("A_NullValue", "x + 1", () => ParameterSets.OfIgnoreCase("x", null));
            Add("A_NullValueNotReached", "1 ? 2 : x", () => ParameterSets.OfIgnoreCase("x", null));
            Add("A_StringValue", "x + 1", () => ParameterSets.OfIgnoreCase("x", "2.5"));
            Add("A_StringThousands", "x", () => ParameterSets.OfIgnoreCase("x", "1,234.5"));
            Add("A_StringUnparsable", "x + 1", () => ParameterSets.OfIgnoreCase("x", "abc"));
            Add("A_BadTypeUnusedKey", "x + 1", () => ParameterSets.OfIgnoreCase("x", 1.0, "unused", new DateTime(2026, 1, 1)));
            Add("A_BadTypeUsed", "1 + d", () => ParameterSets.OfIgnoreCase("d", new DateTime(2026, 1, 1)));
            Add("A_DecimalAndBool", "x + b", () => ParameterSets.OfIgnoreCase("x", 0.1m, "b", true));
            Add("A_NaN", "x", () => ParameterSets.OfIgnoreCase("x", double.NaN));
            Add("A_Empty", "x", () => ParameterSets.OfIgnoreCase());
            Add("A_EmptyNoParams", "1 + 2", () => ParameterSets.OfIgnoreCase());

            Add("A2_InvariantCultureIgnoreCase", "x + X", () => new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase) { { "X", 2.0 } });
            Add("A2_SubclassReimplementsEnumerator", "x * 10", () => new EnumeratorOverridingDictionary());

            Add("B_LowerThenUpper", "x", () => new Dictionary<string, object>(StringComparer.Ordinal) { { "x", 1.0 }, { "X", 2.0 } });
            Add("B_UpperThenLower", "x", () => new Dictionary<string, object>(StringComparer.Ordinal) { { "X", 2.0 }, { "x", 1.0 } });
            Add("B_UpperRead", "X + 0", () => new Dictionary<string, object>(StringComparer.Ordinal) { { "x", 1.0 }, { "X", 2.0 } });
            Add("B_CollapseLastIsNull", "x", () => new Dictionary<string, object>(StringComparer.Ordinal) { { "x", 1.0 }, { "X", null } });
            Add("B_CollapseLastIsBadType", "x", () => new Dictionary<string, object>(StringComparer.Ordinal) { { "X", 1.0 }, { "x", 'c' } });
            Add("B_DefaultComparerUpperOnly", "x", () => new Dictionary<string, object> { { "X", 7.0 } });

            Add("C_InconsistentLookup", "x + 1", () => new InconsistentDictionary(new KeyValuePair<string, object>("x", 1.0)));
            Add("C_InconsistentCollapse", "x", () => new InconsistentDictionary(
                new KeyValuePair<string, object>("x", 1.0), new KeyValuePair<string, object>("X", 5.0)));
            Add("C_InconsistentMissing", "y", () => new InconsistentDictionary(new KeyValuePair<string, object>("x", 1.0)));
            Add("C_SortedDictionary", "a + b", () => new SortedDictionary<string, object> { { "a", 1.0 }, { "b", "2" } });

            Add("D_ZeroParam_NonEmptyOic", "2 * 3", () => ParameterSets.OfIgnoreCase("x", 1.0, "bad", new object()));
            Add("D_ZeroParam_NonEmptyOrdinal", "2 * 3", () => new Dictionary<string, object>(StringComparer.Ordinal) { { "x", 1.0 }, { "X", 2.0 } });
            Add("D_ZeroParam_ThrowingEnumerator", "2 * 3", () => new ThrowingEnumeratorDictionary());
            Add("D_Param_ThrowingEnumerator", "x", () => new ThrowingEnumeratorDictionary());
            Add("D_ZeroParam_CustomDictionary", "2 * 3", () => new InconsistentDictionary(new KeyValuePair<string, object>("x", 1.0)));

            return c;
        }

        public static IEnumerable<object[]> CaseIdsByPath => DynamicDataRows.ByCompiledPath(Cases.Keys);

        [DataTestMethod]
        [DynamicData(nameof(CaseIdsByPath), DynamicDataDisplayName = nameof(TestDisplayNames.Format), DynamicDataDisplayNameDeclaringType = typeof(TestDisplayNames))]
        public void ParameterBinding_CompiledPathMatchesInterpreter(string caseId, string pathName)
        {
            var c = Cases[caseId];
            var path = ParityAssert.ParsePath(pathName);

            ParityAssert.Matches(path, c);
        }

        [DataTestMethod]
        [DataRow("B_LowerThenUpper", 2.0)]
        [DataRow("B_UpperThenLower", 1.0)]
        [DataRow("C_InconsistentLookup", 2.0)]
        [DataRow("C_InconsistentCollapse", 5.0)]
        [DataRow("A2_SubclassReimplementsEnumerator", 10.0)]
        [DataRow("A_SameNameReadTwiceMixedCase", 4.5)]
        public void ParameterBinding_InterpreterOracle_UsesEnumerationLastWins(string caseId, double expected)
        {
            var c = Cases[caseId];
            var oracle = ParityAssert.Run(ParityPath.Interpreter, c);

            Assert.IsFalse(oracle.Threw, oracle.Describe());
            Assert.AreEqual(expected, oracle.Value, caseId);
        }

        [TestMethod]
        public void ParameterBinding_SameHandle_AlternatingDictionaryKinds_MatchesInterpreter()
        {
            var compiled = ParityAssert.CreateEngine(ParityPath.Compile).Compile("x * 100 + y");
            var oracle = ParityAssert.CreateEngine(ParityPath.Interpreter);

            var dicts = new Func<IDictionary<string, object>>[]
            {
                () => ParameterSets.OfIgnoreCase("x", 1.0, "y", 2.0),
                () => new Dictionary<string, object>(StringComparer.Ordinal) { { "x", 3.0 }, { "X", 4.0 }, { "y", 5.0 } },
                () => new InconsistentDictionary(new KeyValuePair<string, object>("x", 6.0), new KeyValuePair<string, object>("Y", 7.0)),
                () => ParameterSets.OfIgnoreCase("X", 8.0),
                () => new ThrowingEnumeratorDictionary(),
                () => ParameterSets.OfIgnoreCase("x", 9.0, "y", 10.0)
            };

            for (var round = 0; round < 2; round++)
            {
                foreach (var make in dicts)
                {
                    var expected = Outcome.Capture(() => oracle.Evaluate("x * 100 + y", make()));
                    var actual = Outcome.Capture(() => compiled.Evaluate(make()));

                    ParityAssert.AreEquivalent(expected, actual, "round " + round + ", " + make().GetType().Name);
                }
            }
        }
    }
}
