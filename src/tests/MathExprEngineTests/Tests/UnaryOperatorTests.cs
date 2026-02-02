// ***********************************************************************
//  Assembly         : RzR.Shared.Entity.MathExprEngineTests
//  Author           : RzR
//  Created On       : 2026-02-02 21:02
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-02-02 21:21
// ***********************************************************************
//  <copyright file="UnaryOperatorTests.cs" company="RzR SOFT & TECH">
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
    public class UnaryOperatorTests
    {
        [DataTestMethod]
        [DataRow("-5", -5)]
        [DataRow("--5", 5)]
        [DataRow("-(2 + 3)", -5)]
        [DataRow("+5", 5)]
        public void UnaryOperators_WorkCorrectly_Test(string expression, double expected)
        {
            var expEval = new MathRuleEngine();

            Assert.AreEqual(expected, expEval.Evaluate(expression));
        }
    }
}