// ***********************************************************************
//  Assembly         : RzR.Shared.Entity.MathExprEngineTests
//  Author           : RzR
//  Created On       : 2026-02-02 21:02
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-02-02 21:32
// ***********************************************************************
//  <copyright file="ArithmeticTests.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using MathExprEngine;
using MathExprEngine.Exceptions;

#endregion

namespace MathExprEngineTests.Tests
{
    [TestClass]
    public class ArithmeticTests
    {
        [TestMethod]
        public void FloatingPointPrecision_IsWithinTolerance_Test()
        {
            var expEval = new MathRuleEngine();
            var result = expEval.Evaluate("0.1 + 0.2");

            Assert.IsTrue(result > 0.299999 && result < 0.300001);
        }

        [TestMethod]
        public void DivisionByZero_ShouldThrow_Test()
        {
            var expEval = new MathRuleEngine();

            Assert.ThrowsException<MathRuleEngineException>(() =>
                expEval.Evaluate("10 / 0")
            );
        }

        [TestMethod]
        [DataRow("5%3", 2)]
        [DataRow("20%6", 2)]
        [DataRow("6%3", 0)]
        [DataRow("10 + 5%3", 12)]
        [DataRow("11%4+1", 4)]
        [DataRow("17%5*2", 4)]
        [DataRow("17%(5*2)", 7)]
        [DataRow("(17%5)%2", 0)]
        [DataRow("100%3", 1)]
        [DataRow("100%%3", 1)]
        [DataRow("-5%3", -2)]
        [DataRow("5%(-3)", 2)]
        [DataRow("0.5%0.2", 0.1)]
        [DataRow("200 * 10%", 20)]
        [DataRow("50%", 0.5)]
        [DataRow("100% + 2", 3)]
        [DataRow("(80+20)%*300", 300)]
        [DataRow("(sum(1,2,3)/1)%3", 0)]
        [DataRow("(sum(1,2,3)*2.5)%4", 3)]
        [DataRow("sqrt(sum(1,2,3,3))%3", 0)]
        public void PercentOperator_ShouldSupportModuloAndPercentage_Test(string expression, double expected)
        {
            var expEval = new MathRuleEngine();

            Assert.AreEqual(expected, expEval.Evaluate(expression), 0.000000001d);
        }

        [TestMethod]
        public void ModuloByZero_ShouldThrow_Test()
        {
            var expEval = new MathRuleEngine();

            Assert.ThrowsException<MathRuleEngineException>(() =>
                expEval.Evaluate("10%0")
            );
        }
    }
}