// ***********************************************************************
//  Assembly         : RzR.Shared.Entity.MathExprEngineTests
//  Author           : RzR
//  Created On       : 2026-02-02 21:02
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-02-02 21:21
// ***********************************************************************
//  <copyright file="ErrorHandlingTests.cs" company="RzR SOFT & TECH">
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
    public class ErrorHandlingTests
    {
        [DataTestMethod]
        [DataRow("2 +", 3)]
        [DataRow("(2 + 3", 6)]
        [DataRow("2 ** 3", 3)]
        public void SyntaxErrors_ReportCorrectColumn_Test(string expression, int expectedColumn)
        {
            var expEval = new MathRuleEngine();

            var ex = Assert.ThrowsException<ExpressionSyntaxException>(() =>
                expEval.Evaluate(expression)
            );

            Assert.AreEqual(expectedColumn, ex.Column);
        }
    }
}