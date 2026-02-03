// ***********************************************************************
//  Assembly         : RzR.Shared.Entity.MathExprEngineTests
//  Author           : RzR
//  Created On       : 2026-02-02 20:02
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-02-02 21:21
// ***********************************************************************
//  <copyright file="ShortCircuitTests.cs" company="RzR SOFT & TECH">
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
    public class ShortCircuitTests
    {
        private MathRuleEngine _engine;

        [TestInitialize]
        public void Init()
        {
            _engine = new MathRuleEngine();
            _engine.RegisterFunction("fail", _ =>
            {
                throw new InvalidOperationException("Must not be evaluated");
            });
        }

        [TestMethod]
        public void AndOperator_ShouldShortCircuit_Test()
        {
            var result = _engine.Evaluate("0 > 1 && fail()");

            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void OrOperator_ShouldShortCircuit_Test()
        {
            var result = _engine.Evaluate("1 > 0 || fail()");

            Assert.AreEqual(1, result);
        }
    }
}