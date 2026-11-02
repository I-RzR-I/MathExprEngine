using MathExprEngine;

namespace MathExprEngineTests.Helpers.Engines
{
    internal static class PinnedEngine
    {
        public static MathRuleEngine Create(MathRuleEngineOptions options = null)
        {
            using (RunMode.Suppress())
            {
                return new MathRuleEngine(options ?? new MathRuleEngineOptions());
            }
        }

        public static MathRuleEngine Interpreter(MathRuleEngineOptions options = null)
        {
            var interpreterOptions = options == null ? new MathRuleEngineOptions() : options.Clone();
            OptionPresets.InterpreterOnly(interpreterOptions);

            return Create(interpreterOptions);
        }
    }
}
