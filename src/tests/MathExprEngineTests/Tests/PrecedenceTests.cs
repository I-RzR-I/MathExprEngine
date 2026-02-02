// ***********************************************************************
//  Assembly         : RzR.Shared.Entity.MathExprEngineTests
//  Author           : RzR
//  Created On       : 2026-02-02 20:02
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-02-02 21:21
// ***********************************************************************
//  <copyright file="PrecedenceTests.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using MathExprEngine;

#endregion

namespace MathExprEngineTests.Tests
{
    [TestClass]
    public class PrecedenceTests
    {
        [DataTestMethod]
        [DataRow("2 + 3 * 4", 14)]
        [DataRow("2 * 3 + 4", 10)]
        [DataRow("2 * (3 + 4)", 14)]
        [DataRow("10 - 4 - 3", 3)]
        [DataRow("2 ^ 3 ^ 2", 512)]
        public void OperatorPrecedence_IsCorrect_Test(string expression, double expected)
        {
            var expEval = new MathRuleEngine();
            var result = expEval.Evaluate(expression);

            Assert.AreEqual(expected, result);
        }
    }
}