// ***********************************************************************
//  Assembly         : RzR.Shared.Entity.MathExprEngineTests
//  Author           : RzR
//  Created On       : 2026-01-18 19:01
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-01-18 19:29
// ***********************************************************************
//  <copyright file="ExpressionParseTests.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using System.Collections.Generic;
using MathExprEngine;
using MathExprEngineTests.Helpers;

#endregion

namespace MathExprEngineTests.Tests
{
    [TestClass]
    public class ExpressionParseTests
    {
        private Dictionary<string, Dictionary<string, object>> _store;

        [TestInitialize]
        public void Init()
        {
            _store = new Dictionary<string, Dictionary<string, object>>
            {
                {
                    "1001", new Dictionary<string, object>
                    {
                        { "02", 1 },
                        { "03", 8 }
                    }
                }
            };
        }

        [TestMethod]
        public void ParseExp_Test()
        {
            var expression = "{[1001];[02]}+{[1001];[03]}";
            var parseResult = ParseXData.Parse(expression);
            Assert.IsNotNull(parseResult);

            var newExpressing = expression;
            foreach (var p in parseResult)
            {
                var val = _store[p.Line][p.Col];
                newExpressing = newExpressing.Replace(p.RawVariable, $"{val}");
            }

            var expressionResult = new MathRuleEngine().Evaluate(newExpressing);

            Assert.IsNotNull(expressionResult);
            Assert.AreEqual(9, expressionResult);
        }
    }
}