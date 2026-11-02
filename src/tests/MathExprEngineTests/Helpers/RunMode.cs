using System;
using System.Reflection;
using System.Threading;
using MathExprEngine;

namespace MathExprEngineTests.Helpers
{
    public static class RunMode
    {
        public const string EnvironmentVariableName = "MATHEXPRENGINE_TEST_MODE";

        private static readonly AsyncLocal<int> SuppressDepth = new AsyncLocal<int>();

        private static volatile bool _initialized;

        private static TestMode _current = TestMode.Tiered;

        public static TestMode Current => _current;

        public static bool IsInitialized => _initialized;

        public static bool IsSuppressed => SuppressDepth.Value > 0;

        public static bool IsEager => _current == TestMode.Eager || _current == TestMode.EagerInterpretedTrees;

        public static IDisposable Suppress()
        {
            SuppressDepth.Value = SuppressDepth.Value + 1;

            return new DisposeOnceScope(() => SuppressDepth.Value = SuppressDepth.Value - 1);
        }

        public static TestMode Parse(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return TestMode.Tiered;

            switch (raw)
            {
                case "Interpreter": return TestMode.Interpreter;
                case "Tiered": return TestMode.Tiered;
                case "Eager": return TestMode.Eager;
                case "EagerInterpretedTrees": return TestMode.EagerInterpretedTrees;
                default:
                    throw new InvalidOperationException(
                        EnvironmentVariableName + " has an unknown value '" + raw +
                        "'. Allowed: Interpreter, Tiered, Eager, EagerInterpretedTrees (unset = Tiered).");
            }
        }

        internal static MathRuleEngineOptions ApplyMode(TestMode mode, MathRuleEngineOptions options)
        {
            switch (mode)
            {
                case TestMode.Interpreter:
                    options.EnableCompilation = false;
                    options.ExpressionCacheCapacity = 0;
                    break;

                case TestMode.Tiered:
                    options.ThrowOnCompileFault = true;
                    break;

                case TestMode.Eager:
                    options.CompilationThreshold = 1;
                    options.ThrowOnCompileFault = true;
                    break;

                case TestMode.EagerInterpretedTrees:
                    options.CompilationThreshold = 1;
                    options.ThrowOnCompileFault = true;
                    options.PreferInterpretedExpressionTrees = true;
                    break;
            }

            return options;
        }

        internal static MathRuleEngineOptions Override(MathRuleEngineOptions options)
        {
            var custom = CustomOverride.Value;
            if (custom != null)
                return custom(options);

            if (IsSuppressed)
                return options;

            return ApplyMode(_current, options);
        }

        private static readonly AsyncLocal<Func<MathRuleEngineOptions, MathRuleEngineOptions>> CustomOverride =
            new AsyncLocal<Func<MathRuleEngineOptions, MathRuleEngineOptions>>();

        public static IDisposable UseCustomOverride(Func<MathRuleEngineOptions, MathRuleEngineOptions> custom)
        {
            var previous = CustomOverride.Value;
            CustomOverride.Value = custom;

            return new DisposeOnceScope(() => CustomOverride.Value = previous);
        }

        internal static void Initialize(TestMode mode)
        {
            _current = mode;
            _initialized = true;
        }

        public static MathRuleEngineOptions ReadEngineOptions(MathRuleEngine engine)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            var type = typeof(MathRuleEngine);

            var field = type.GetField("_options", flags);
            if (field != null && field.FieldType == typeof(MathRuleEngineOptions))
                return (MathRuleEngineOptions)field.GetValue(engine);

            foreach (var f in type.GetFields(flags))
                if (f.FieldType == typeof(MathRuleEngineOptions))
                    return (MathRuleEngineOptions)f.GetValue(engine);

            foreach (var p in type.GetProperties(flags))
                if (p.PropertyType == typeof(MathRuleEngineOptions) && p.GetIndexParameters().Length == 0)
                    return (MathRuleEngineOptions)p.GetValue(engine);

            throw new InvalidOperationException("MathRuleEngine options snapshot not found by reflection.");
        }
    }
}
