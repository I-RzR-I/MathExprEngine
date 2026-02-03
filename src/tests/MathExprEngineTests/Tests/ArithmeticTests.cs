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
    }
}