// ***********************************************************************
//  Assembly         : RzR.Shared.Entity.MathExprEngineTests
//  Author           : RzR
//  Created On       : 2026-01-18 19:01
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-01-18 19:22
// ***********************************************************************
//  <copyright file="ExpressionEngineTests.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

using MathExprEngine;
using System;

namespace MathExprEngineTests.Tests
{
    [TestClass]
    public class ExpressionEngineTests
    {
        private MathRuleEngine _calc;

        [TestInitialize]
        public void Init()
        {
            _calc = new MathRuleEngine();
        }

        [TestMethod]
        public void TestMethod1()
        {
            var result = _calc.Evaluate("2 + 3 * (5 - 2)");

            Assert.IsNotNull(result);
            Assert.AreEqual(11, result);
        }

        [TestMethod]
        public void TestMethod2()
        {
            var result = _calc.Evaluate("2 ^ 3 ^ 2");

            Assert.IsNotNull(result);
            Assert.AreEqual(512, result);
        }

        [TestMethod]
        public void TestMethod_Sin()
        {
            var result = _calc.Evaluate("sin(3.141592653589793/2)");

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result);
        }

        [TestMethod]
        public void TestMethod_Sqrt_Pow()
        {
            var result = _calc.Evaluate("sqrt(16) + pow(2,5)");

            Assert.IsNotNull(result);
            Assert.AreEqual(36, result);
        }

        [TestMethod]
        public void TestMethod_Compare_1()
        {
            var result = _calc.Evaluate("3 > 2");

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result);
        }

        [TestMethod]
        public void TestMethod_Compare_0()
        {
            var result = _calc.Evaluate("3 < 2");

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void TestMethod_Compare_Logical()
        {
            var result = _calc.Evaluate("(3 > 2) && (1 - 1)");

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void TestMethod_Max()
        {
            var result = _calc.Evaluate("max(7,3)");

            Assert.IsNotNull(result);
            Assert.AreEqual(7, result);
        }

        [TestMethod]
        public void TestMethod_Neg()
        {
            var result = _calc.Evaluate("neg(3)");

            Assert.IsNotNull(result);
            Assert.AreEqual(-3, result);
        }

        [TestMethod]
        public void TestMethod_Multiple()
        {
            var result = _calc.Evaluate("2 ^ 3");            // 8

            Assert.AreEqual(8, result);
            result = _calc.Evaluate("2 ^ 3 ^ 2");        // 512 (right-associative)

            Assert.AreEqual(512, result);
            result = _calc.Evaluate("sin(3.14159265/2)"); // 1
            Assert.AreEqual(1, result);
            result = _calc.Evaluate("sqrt(9)");           // 3
            Assert.AreEqual(3, result);
            result = _calc.Evaluate("pow(2,5)");          // 32
            Assert.AreEqual(32, result);
            result = _calc.Evaluate("pow(2,pow(2,3))");   // 256
            Assert.AreEqual(256, result);
            result = _calc.Evaluate("3 + sqrt(16) * 2");  // 11
            Assert.AreEqual(11, result);
        }

        [TestMethod]
        public void TestMethod_Multiple_2()
        {
            var result = _calc.Evaluate("2 + 3 * (5 - 2)");
            Assert.AreEqual(11, result);

            result = _calc.Evaluate("2 ^ 3 ^ 2");
            Assert.AreEqual(512, result);

            result = _calc.Evaluate("sqrt(16) + pow(2,5)");
            Assert.AreEqual(36, result);

            /*
            // short-circuit: left is false (0) => evaluate right -> throws division by zero
            result = _calc.Evaluate("0 || (1/0)");
            Assert.AreEqual(0, result);
            */
            // short-circuit: left is false (0) => evaluate right -> true
            result = _calc.Evaluate("0 || (0/1)");
            Assert.AreEqual(0, result);

            // short-circuit: left is true (1) => returns 1.0 and does NOT evaluate (1/0)
            result = _calc.Evaluate("1 || (1/0)");
            Assert.AreEqual(1, result);


            // ternary (short-circuiting)
            result = _calc.Evaluate("1 ? 10 : (1/0)");
            Assert.AreEqual(10, result);

            result = _calc.Evaluate("0 ? (1/0) : 5");
            Assert.AreEqual(5, result);

            result = _calc.Evaluate("10>1 ? ((2-1)*8) : 5");
            Assert.AreEqual(8, result);
        }

        [TestMethod]
        public void TestMethod_Floating_Data()
        {
            var result = _calc.Evaluate("2.2 + 3.3");
            Assert.AreEqual(5.5, result);

            result = _calc.Evaluate("2.02 + max(12,50) - 0.02");
            Assert.AreEqual(52, result);
        }

        [TestMethod]
        public void TestMethod_Sum_Multiple()
        {
            var result = _calc.Evaluate("sum(1,2,3,4,5,6,7,8,9)");
            Assert.AreEqual(45, result);
        }

        [TestMethod]
        public void TestMethod_Execute_Custom_Func()
        {
            var result = _calc.Evaluate("3+2*(2+3)-4/2^2");
            Assert.AreEqual(12, result);
        }

        [TestMethod]
        public void TestMethod_Execute_Custom_1_Func()
        {
            Assert.AreEqual(14, _calc.Evaluate("2 + 3 * 4"));     // 14
            Assert.AreEqual(20, _calc.Evaluate("(2 + 3) * 4"));   // 20
            Assert.AreEqual(512, _calc.Evaluate("2 ^ 3 ^ 2"));     // 512 (right-associative)
            
            Assert.AreEqual(4, _calc.Evaluate("sqrt(16)"));          // 4
            Assert.AreEqual(32, _calc.Evaluate("pow(2, 5)"));         // 32
            Assert.AreEqual(1, _calc.Evaluate("sin(3.1415926535/2)"));
            Assert.AreEqual(10, _calc.Evaluate("max(10, 7)")); // 10
            
            Assert.AreEqual(1, _calc.Evaluate("5 > 3"));   // 1
            Assert.AreEqual(0, _calc.Evaluate("5 <= 3"));  // 0
            Assert.AreEqual(1, _calc.Evaluate("5 == 5"));  // 1
            Assert.AreEqual(0, _calc.Evaluate("5 != 5"));  // 0
            
            Assert.AreEqual(1, _calc.Evaluate("1 && 1"));        // 1
            Assert.AreEqual(0, _calc.Evaluate("0 && (1/0)"));    // 0 (no exception)
            Assert.AreEqual(1, _calc.Evaluate("1 || (1/0)"));    // 1 (no exception)

            Assert.AreEqual(10, _calc.Evaluate("1 ? 10 : 20"));
            Assert.AreEqual(20, _calc.Evaluate("0 ? 10 : 20"));

            Assert.AreEqual(10, _calc.Evaluate("1 ? 10 : (1/0)")); // 10
            Assert.AreEqual(10, _calc.Evaluate("0 ? (1/0) : 10")); // 10

            Assert.AreEqual(1.0, _calc.Evaluate("(80 > 90) ? 1.5 : 1.0")); // 1.0
            Assert.AreEqual(1, _calc.Evaluate("(18 >= 18 && 1 == 1) ? 1 : 0")); // 1

            Assert.AreEqual(7.12, _calc.Evaluate("1.01 + 1.11 + 5"));
        }

        [TestMethod]
        public void TestMethod_Execute_Exception_Func()
        {
            try
            {
                _calc.Evaluate("2 + * 3");
            }
            catch (Exception ex)
            {
                Assert.AreEqual("Error: Syntax error at column 5. Message: Unexpected token '*'", ex.Message);
            }
        }

        [TestMethod]
        public void TestMethod_Execute_TernSumCondition_Func()
        {
           var result = _calc.Evaluate("((1>0) && (12 > 4) && (8<9)) ? sum(1, 2, 3)/3 : -48");
           
           Assert.AreEqual(2, result);
        }
    }
}

