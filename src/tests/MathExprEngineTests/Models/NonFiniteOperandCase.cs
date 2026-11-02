namespace MathExprEngineTests.Models
{
    internal sealed class NonFiniteOperandCase
    {
        public string Expression { get; set; }

        public bool AllowNonFinite { get; set; }

        public double X { get; set; }

        public int? ExpectedColumn { get; set; }

        public double ExpectedValue { get; set; }
    }
}
