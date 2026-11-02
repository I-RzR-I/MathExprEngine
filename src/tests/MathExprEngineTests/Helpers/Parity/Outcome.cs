using System;
using MathExprEngine.Exceptions;

namespace MathExprEngineTests.Helpers.Parity
{
    public sealed class Outcome
    {
        public bool Threw => Error != null;

        public double Value { get; internal set; }

        public Exception Error { get; internal set; }

        public bool? IsCompiled { get; internal set; }

        public static Outcome Capture(Func<double> evaluate)
        {
            var outcome = new Outcome();

            try
            {
                outcome.Value = evaluate();
            }
            catch (Exception ex)
            {
                outcome.Error = ex;
            }

            return outcome;
        }

        public string Describe()
        {
            if (!Threw)
                return "value " + Value.ToString("R", System.Globalization.CultureInfo.InvariantCulture) +
                       " (bits 0x" + BitConverter.DoubleToInt64Bits(Value).ToString("X16") + ")";

            var column = Error is MathExprEngineException me ? me.Column.ToString() : "n/a";
            var inner = Error.InnerException == null ? "none" : Error.InnerException.GetType().Name + ": " + Error.InnerException.Message;

            return Error.GetType().FullName + " | Column=" + column + " | Message=\"" + Error.Message + "\" | Inner=" + inner;
        }
    }
}
