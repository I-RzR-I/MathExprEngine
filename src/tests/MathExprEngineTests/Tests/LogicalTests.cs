// ***********************************************************************
//  Assembly         : RzR.Shared.Entity.MathExprEngineTests
//  Author           : RzR
//  Created On       : 2026-02-02 20:02
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-02-02 21:21
// ***********************************************************************
//  <copyright file="LogicalTests.cs" company="RzR SOFT & TECH">
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
    public class LogicalTests
    {
        [DataTestMethod]
        [DataRow("1 > 0 && 2 > 1", 1)]
        [DataRow("1 > 0 || 0 > 1", 1)]
        [DataRow("1 < 0 || 0 > 1", 0)]
        [DataRow("(1 > 0) && (0 > 1)", 0)]
        public void LogicalExpressions_EvaluateCorrectly_Test(string expression, double expected)
        {
            var expEval = new MathRuleEngine();

            Assert.AreEqual(expected, expEval.Evaluate(expression));
        }
    }
}