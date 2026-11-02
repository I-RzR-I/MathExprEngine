using System;
using System.Collections.Generic;
using MathExprEngine;

namespace MathExprEngineTests.Models
{
    internal sealed class ParityCase
    {
        public string Expression { get; set; }

        public Func<IDictionary<string, object>> Parameters { get; set; }

        public Action<MathRuleEngineOptions> Configure { get; set; }

        public Action<MathRuleEngine> Setup { get; set; }

        public double? ExpectedValue { get; set; }

        public Type ExpectedException { get; set; }

        public string Source { get; set; }
    }
}
