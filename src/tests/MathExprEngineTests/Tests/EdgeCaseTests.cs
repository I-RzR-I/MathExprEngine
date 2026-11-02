using System;
using MathExprEngine;
using MathExprEngine.Exceptions;
using MathExprEngineTests.Helpers.Engines;
using MathExprEngineTests.Helpers.Inputs;

namespace MathExprEngineTests.Tests
{
    [TestClass]
    public class EdgeCaseTests
    {
        [TestMethod]
        public void PercentOperator_FollowedByUnaryMinus_IsDisambiguatedAsInfixModulo_Test()
        {
            var engine = new MathRuleEngine();

            var result = engine.Evaluate("10 % -3");

            Assert.AreEqual(1, result, 0.000000001d);
        }

        [TestMethod]
        public void PercentOperator_ParenthesizedNegativeDivisor_IsTrueModulo_Test()
        {
            var engine = new MathRuleEngine();

            var result = engine.Evaluate("10 % (-3)");

            Assert.AreEqual(1, result, 0.000000001d);
        }

        [TestMethod]
        public void Power_BindsTighterThanUnaryMinus_NegativeTwoSquared_Test()
        {
            var engine = new MathRuleEngine();

            var result = engine.Evaluate("-2^2");

            Assert.AreEqual(-4, result);
        }

        [TestMethod]
        public void Ternary_RightAssociative_ChainInFalseBranch_Test()
        {
            var engine = new MathRuleEngine();

            var result = engine.Evaluate("0 ? 1 : 1 ? 2 : 3");

            Assert.AreEqual(2, result);
        }

        [TestMethod]
        public void UndefinedVariable_ShouldThrow_Test()
        {
            var engine = new MathRuleEngine();

            Assert.ThrowsException<ExpressionSyntaxException>(() =>
                engine.Evaluate("undefinedVar + 1")
            );
        }

        [DataTestMethod]
        [DataRow("5", 5d)]
        [DataRow(true, 1d)]
        [DataRow(false, 0d)]
        [DataRow(double.MaxValue, double.MaxValue)]
        public void ParameterCoercion_SupportedRawTypes_ConvertCorrectly_Test(object rawValue, double expected)
        {
            var engine = new MathRuleEngine();
            var values = ParameterSets.Of("x", rawValue);

            var result = engine.Evaluate("x", values);

            Assert.AreEqual(expected, result);
        }

        [DataTestMethod]
        [DataRow((byte)5, 5d)]
        [DataRow((sbyte)5, 5d)]
        [DataRow((sbyte)-5, -5d)]
        [DataRow((short)5, 5d)]
        [DataRow((ushort)5, 5d)]
        [DataRow(5, 5d)]
        [DataRow((uint)5, 5d)]
        [DataRow(5L, 5d)]
        [DataRow((ulong)5, 5d)]
        [DataRow(5f, 5d)]
        public void ParameterCoercion_AcceptedNumericPrimitiveTypes_ConvertCorrectly_Test(object rawValue, double expected)
        {
            var engine = new MathRuleEngine();
            var values = ParameterSets.Of("x", rawValue);

            var result = engine.Evaluate("x", values);

            Assert.AreEqual(expected, result, 0.000000001d);
        }

        [TestMethod]
        public void ParameterCoercion_DecimalValue_ConvertsCorrectly_Test()
        {
            var engine = new MathRuleEngine();
            var values = ParameterSets.Of("x", 123.45m);

            var result = engine.Evaluate("x", values);

            Assert.AreEqual(123.45, result, 0.000000001d);
        }

        [TestMethod]
        public void ParameterCoercion_StringWithThousandsSeparator_ParsesWithInvariantCulture_Test()
        {
            var engine = new MathRuleEngine();
            var values = ParameterSets.Of("x", "1,234.5");

            var result = engine.Evaluate("x", values);

            Assert.AreEqual(1234.5, result, 0.000000001d);
        }

        [TestMethod]
        public void ParameterCoercion_NullValue_ShouldThrow_Test()
        {
            var engine = new MathRuleEngine();
            var values = ParameterSets.Of("x", null);

            var ex = Assert.ThrowsException<MathRuleEngineException>(() =>
                engine.Evaluate("x", values)
            );

            StringAssert.Contains(ex.Message, "cannot be converted to a number");
        }

        [TestMethod]
        public void ParameterCoercion_NaNValue_ShouldThrow_Test()
        {
            var engine = new MathRuleEngine();
            var values = ParameterSets.Of("x", double.NaN);

            Assert.ThrowsException<MathRuleEngineException>(() =>
                engine.Evaluate("x", values)
            );
        }

        [TestMethod]
        public void ParameterCoercion_NonNumericString_ShouldThrowMathRuleEngineException_Test()
        {
            var engine = new MathRuleEngine();
            var values = ParameterSets.Of("x", "abc");

            Assert.ThrowsException<MathRuleEngineException>(() =>
                engine.Evaluate("x", values)
            );
        }

        [TestMethod]
        public void ParameterCoercion_DateTimeValue_ShouldThrowMathRuleEngineException_Test()
        {
            var engine = new MathRuleEngine();
            var values = ParameterSets.Of("x", DateTime.Now);

            Assert.ThrowsException<MathRuleEngineException>(() =>
                engine.Evaluate("x", values)
            );
        }

        [TestMethod]
        public void ParameterCoercion_CharValue_ShouldThrow_Test()
        {
            var engine = new MathRuleEngine();
            var values = ParameterSets.Of("x", '5');

            Assert.ThrowsException<MathRuleEngineException>(() =>
                engine.Evaluate("x", values)
            );
        }

        [TestMethod]
        public void ParameterCoercion_TimeSpanValue_ShouldThrow_Test()
        {
            var engine = new MathRuleEngine();
            var values = ParameterSets.Of("x", TimeSpan.FromMinutes(5));

            Assert.ThrowsException<MathRuleEngineException>(() =>
                engine.Evaluate("x", values)
            );
        }

        [TestMethod]
        public void ParameterCoercion_GuidValue_ShouldThrow_Test()
        {
            var engine = new MathRuleEngine();
            var values = ParameterSets.Of("x", Guid.NewGuid());

            Assert.ThrowsException<MathRuleEngineException>(() =>
                engine.Evaluate("x", values)
            );
        }

        [TestMethod]
        public void ParameterCoercion_EnumValue_ShouldThrow_Test()
        {
            var engine = new MathRuleEngine();
            var values = ParameterSets.Of("x", DayOfWeek.Monday);

            Assert.ThrowsException<MathRuleEngineException>(() =>
                engine.Evaluate("x", values)
            );
        }

        [TestMethod]
        public void ParameterCoercion_ArbitraryObjectValue_ShouldThrow_Test()
        {
            var engine = new MathRuleEngine();
            var values = ParameterSets.Of("x", new object());

            Assert.ThrowsException<MathRuleEngineException>(() =>
                engine.Evaluate("x", values)
            );
        }

        [TestMethod]
        public void ParameterCoercion_InvalidValues_NeverLeakBclExceptionType_Test()
        {
            var engine = new MathRuleEngine();
            var invalidValues = new object[] { "abc", DateTime.Now, '5', Guid.NewGuid() };

            foreach (var invalidValue in invalidValues)
            {
                var values = ParameterSets.Of("x", invalidValue);

                try
                {
                    engine.Evaluate("x", values);
                    Assert.Fail($"Expected a value of type {invalidValue.GetType()} to be rejected.");
                }
                catch (Exception ex)
                {
                    Assert.IsInstanceOfType(
                        ex,
                        typeof(MathExprEngineException),
                        $"Expected a MathExprEngineException-derived type for {invalidValue.GetType()}, " +
                        $"but got {ex.GetType()} ({ex.Message}).");
                }
            }
        }

        [DataTestMethod]
        [DataRow("sin()")]
        [DataRow("sin(1,2)")]
        [DataRow("cos()")]
        [DataRow("cos(1,2)")]
        [DataRow("sqrt()")]
        [DataRow("sqrt(1,2)")]
        [DataRow("pow(1)")]
        [DataRow("pow(1,2,3)")]
        [DataRow("neg()")]
        [DataRow("neg(1,2)")]
        public void BuiltInFunctions_WrongArgumentCount_ShouldThrow_Test(string expression)
        {
            var engine = new MathRuleEngine();

            Assert.ThrowsException<ExpressionFunctionException>(() =>
                engine.Evaluate(expression)
            );
        }

        [TestMethod]
        public void MaxFunction_ZeroArguments_ShouldThrow_Test()
        {
            var engine = new MathRuleEngine();

            Assert.ThrowsException<ExpressionFunctionException>(() =>
                engine.Evaluate("max()")
            );
        }

        [TestMethod]
        public void SumFunction_ZeroArguments_ShouldThrow_Test()
        {
            var engine = new MathRuleEngine();

            Assert.ThrowsException<ExpressionFunctionException>(() =>
                engine.Evaluate("sum()")
            );
        }

        [TestMethod]
        public void CustomFunction_CanOverrideBuiltIn_ValueTest()
        {
            var engine = new MathRuleEngine();
            engine.RegisterFunction("max", args =>
            {
                if (args.Length != 2)
                    throw new ArgumentException("max expects 2 arguments");

                return Math.Max(args[0], args[1]) + 100;
            });

            var result = engine.Evaluate("max(3, 11)");

            Assert.AreEqual(111, result);
        }

        [TestMethod]
        public void CustomFunction_OverrideAppliesArgValidation_Test()
        {
            var engine = new MathRuleEngine();
            engine.RegisterFunction("max", HostFunctions.TwoArgumentMax);

            Assert.ThrowsException<ExpressionFunctionException>(() =>
                engine.Evaluate("max(1, 2, 3)")
            );
        }

        [TestMethod]
        public void Tokenizer_MultipleDotsInNumber_ShouldThrowWithColumn_Test()
        {
            var engine = new MathRuleEngine();

            var ex = Assert.ThrowsException<MathRuleEngineException>(() =>
                engine.Evaluate("1.2.3")
            );

            Assert.AreEqual(3, ex.Column);
        }

        [DataTestMethod]
        [DataRow("@", 0)]
        [DataRow("$", 0)]
        public void Tokenizer_UnexpectedCharacter_ShouldThrowWithColumn_Test(string expression, int expectedColumn)
        {
            var engine = new MathRuleEngine();

            var ex = Assert.ThrowsException<MathRuleEngineException>(() =>
                engine.Evaluate(expression)
            );

            Assert.AreEqual(expectedColumn, ex.Column);
        }

        [TestMethod]
        public void EmptyExpression_ShouldThrowArgumentException_Test()
        {
            var engine = new MathRuleEngine();

            Assert.ThrowsException<ArgumentException>(() =>
                engine.Evaluate("")
            );
        }

        [TestMethod]
        public void WhitespaceOnlyExpression_ShouldThrowArgumentException_Test()
        {
            var engine = new MathRuleEngine();

            Assert.ThrowsException<ArgumentException>(() =>
                engine.Evaluate("   ")
            );
        }

        [TestMethod]
        public void ExponentNotation_IsNotSupported_ShouldThrowSyntaxException_Test()
        {
            var engine = new MathRuleEngine();

            var ex = Assert.ThrowsException<ExpressionSyntaxException>(() =>
                engine.Evaluate("1e5")
            );

            Assert.AreEqual(1, ex.Column);
        }

        [TestMethod]
        public void Sqrt_NegativeInput_ShouldThrow_Test()
        {
            var engine = new MathRuleEngine();

            Assert.ThrowsException<ExpressionFunctionException>(() =>
                engine.Evaluate("sqrt(-1)")
            );
        }

        [TestMethod]
        public void ZeroToNegativePower_ShouldThrow_Test()
        {
            var engine = new MathRuleEngine();

            Assert.ThrowsException<MathRuleEngineException>(() =>
                engine.Evaluate("0^-1")
            );
        }

        [TestMethod]
        public void HugeExponent_ShouldThrow_Test()
        {
            var engine = new MathRuleEngine();

            Assert.ThrowsException<MathRuleEngineException>(() =>
                engine.Evaluate("10^400")
            );
        }

        [TestMethod]
        public void NaNValue_UsedAsTernaryCondition_ThrowsAtProducingNode_Test()
        {
            var engine = new MathRuleEngine();
            HostFunctions.RegisterNaN(engine);

            Assert.ThrowsException<MathRuleEngineException>(() =>
                engine.Evaluate("nan() ? 1 : 2")
            );
        }

        [TestMethod]
        public void AllowNonFiniteResults_True_LetsNonFiniteValuePropagate_Test()
        {
            var options = new MathRuleEngineOptions { AllowNonFiniteResults = true };
            var engine = new MathRuleEngine(options);
            HostFunctions.RegisterNaN(engine);

            var result = engine.Evaluate("nan() + 1");

            Assert.IsTrue(double.IsNaN(result));
        }

        [TestMethod]
        public void AllowNonFiniteResults_True_StillRejectsNaNAsACondition_Test()
        {
            var options = new MathRuleEngineOptions { AllowNonFiniteResults = true };
            var engine = new MathRuleEngine(options);
            HostFunctions.RegisterNaN(engine);

            Assert.ThrowsException<MathRuleEngineException>(() =>
                engine.Evaluate("nan() ? 111 : 222")
            );
        }

        [TestMethod]
        public void AllowNonFiniteResults_True_StillRejectsNaNInLogicalOperators_Test()
        {
            var options = new MathRuleEngineOptions { AllowNonFiniteResults = true };
            var engine = new MathRuleEngine(options);
            HostFunctions.RegisterNaN(engine);

            Assert.ThrowsException<MathRuleEngineException>(() => engine.Evaluate("nan() && 1"));
            Assert.ThrowsException<MathRuleEngineException>(() => engine.Evaluate("nan() || 0"));
        }

        [TestMethod]
        public void FloatEquality_PointOnePlusPointTwo_IsNotExactlyPointThree_Test()
        {
            var engine = new MathRuleEngine();

            var result = engine.Evaluate("0.1 + 0.2 == 0.3");

            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void DeeplyNestedParentheses_EvaluatesCorrectly_AtSafeRecursionDepth_Test()
        {
            var engine = new MathRuleEngine();
            const int depth = 100;
            var expression = ExpressionText.NestedParentheses(depth);

            var result = engine.Evaluate(expression);

            Assert.AreEqual(1, result);
        }
    }
}
