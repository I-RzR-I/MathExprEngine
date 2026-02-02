// ***********************************************************************
//  Assembly         : RzR.Shared.Entity.MathExprEngineTests
//  Author           : RzR
//  Created On       : 2026-02-02 21:02
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-02-02 21:21
// ***********************************************************************
//  <copyright file="TernaryTests.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using System;
using MathExprEngine;

#endregion

namespace MathExprEngineTests.Tests
{
    [TestClass]
    public class TernaryTests
    {
        [DataTestMethod]
        [DataRow("1 > 0 ? 10 : 20", 10)]
        [DataRow("1 < 0 ? 10 : 20", 20)]
        [DataRow("1 > 0 ? 1 > 0 ? 5 : 6 : 7", 5)]
        public void TernaryExpressions_WorkCorrectly_Test(string expression, double expected)
        {
            var expEval = new MathRuleEngine();

            Assert.AreEqual(expected, expEval.Evaluate(expression));
        }

        [TestMethod]
        public void Ternary_ShouldEvaluateOnlyChosenBranch_Test()
        {
            var expEval = new MathRuleEngine();

            expEval.RegisterFunction("explode", _ => { throw new Exception("Should not execute"); });

            var result = expEval.Evaluate("1 > 0 ? 10 : explode()");

            Assert.AreEqual(10, result);
        }
    }
}