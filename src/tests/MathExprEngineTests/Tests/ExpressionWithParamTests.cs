using System.Collections.Generic;
using MathExprEngine;

namespace MathExprEngineTests.Tests
{
    [TestClass]
    public class ExpressionWithParamTests
    {
        private MathRuleEngine _calc;

        [TestInitialize]
        public void Init()
        {
            _calc = new MathRuleEngine();
        }

        [TestMethod]
        public void VarUsage1_Test()
        {
            var values = new Dictionary<string, object>
            {
                { "FirstVar", (double)1.75 },
                { "SecondVar", (double)2 },
                { "Result", (double)1000 },
                { "Result1", (double)1001 }
            };

            var result = _calc.Evaluate("FirstVar < SecondVar ? Result : Result1", values);

            Assert.AreEqual(values["Result"], result);
        }

        [TestMethod]
        public void VarUsage2_Test()
        {
            var values = new Dictionary<string, object>
            {
                { "FirstVar", (double)2.75 },
                { "SecondVar", (double)2 },
                { "Result", (double)1000 },
                { "Result_1", (double)1001 }
            };

            var result = _calc.Evaluate("FirstVar <= SecondVar ? Result : Result_1", values);

            Assert.AreEqual(values["Result_1"], result);
        }

        [TestMethod]
        public void VarUsage3_Test()
        {
            var values = new Dictionary<string, object>
            {
                { "FirstVar", (double)2.75 },
                { "SecondVar", (double)2 },
                { "Result", (double)1000 },
                { "Result_1", (double)1001 },
                { "TestVar", (double)0 }
            };

            var result = _calc.Evaluate("FirstVar > SecondVar ? sum(FirstVar,SecondVar) : Result_1", values);

            Assert.AreEqual(4.75, result);
        }
    }
}
