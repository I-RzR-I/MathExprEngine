// ***********************************************************************
//  Assembly         : RzR.Shared.Entity.MathExprEngineTests
//  Author           : RzR
//  Created On       : 2026-02-02 21:02
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-02-02 21:21
// ***********************************************************************
//  <copyright file="FunctionTests.cs" company="RzR SOFT & TECH">
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
    public class FunctionTests
    {
        private MathRuleEngine _engine;

        [TestInitialize]
        public void Init()
        {
            _engine = new MathRuleEngine();
        }

        [TestMethod]
        public void BuiltInFunctions_Work_Test()
        {
            Assert.AreEqual(3, _engine.Evaluate("sqrt(9)"));
            Assert.AreEqual(8, _engine.Evaluate("sum(3,5)"));
            Assert.AreEqual(5, _engine.Evaluate("max(2,5)"));
        }

        [TestMethod]
        public void NestedFunctions_Work_Test()
        {
            var result = _engine.Evaluate("sqrt(sum(4,5))");

            Assert.AreEqual(3, result);
        }

        [TestMethod]
        public void UnknownFunction_ShouldThrow_Test()
        {
            Assert.ThrowsException<ExpressionFunctionException>(() =>
                _engine.Evaluate("unknown(1)")
            );
        }
    }
}