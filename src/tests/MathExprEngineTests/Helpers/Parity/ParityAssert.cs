using System;
using System.Collections.Generic;
using MathExprEngine;
using MathExprEngine.Compilation;
using MathExprEngine.Exceptions;
using MathExprEngineTests.Helpers.Assertions;
using MathExprEngineTests.Helpers.Engines;
using MathExprEngineTests.Models;

namespace MathExprEngineTests.Helpers.Parity
{
    public static class ParityAssert
    {
        public static readonly ParityPath[] CompiledPaths =
        {
            ParityPath.Eager,
            ParityPath.EagerInterpretedTrees,
            ParityPath.TieredSecondUse,
            ParityPath.Compile,
            ParityPath.CompileInterpretedTrees
        };

        public static readonly ParityPath[] AllPaths =
        {
            ParityPath.Interpreter,
            ParityPath.Eager,
            ParityPath.EagerInterpretedTrees,
            ParityPath.TieredSecondUse,
            ParityPath.Compile,
            ParityPath.CompileInterpretedTrees
        };

        public static ParityPath ParsePath(string name) => (ParityPath)Enum.Parse(typeof(ParityPath), name);

        public static void ApplyPath(ParityPath path, MathRuleEngineOptions options)
        {
            switch (path)
            {
                case ParityPath.Interpreter:
                    options.EnableCompilation = false;
                    options.ExpressionCacheCapacity = 0;
                    options.ThrowOnCompileFault = false;
                    break;

                case ParityPath.Eager:
                    options.EnableCompilation = true;
                    options.CompilationThreshold = 1;
                    options.ThrowOnCompileFault = true;
                    break;

                case ParityPath.EagerInterpretedTrees:
                    options.EnableCompilation = true;
                    options.CompilationThreshold = 1;
                    options.PreferInterpretedExpressionTrees = true;
                    options.ThrowOnCompileFault = true;
                    break;

                case ParityPath.TieredSecondUse:
                    options.EnableCompilation = true;
                    options.CompilationThreshold = 2;
                    options.ThrowOnCompileFault = true;
                    break;

                case ParityPath.Compile:
                    options.EnableCompilation = true;
                    options.ThrowOnCompileFault = true;
                    break;

                case ParityPath.CompileInterpretedTrees:
                    options.EnableCompilation = true;
                    options.PreferInterpretedExpressionTrees = true;
                    options.ThrowOnCompileFault = true;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(path), path, null);
            }
        }

        public static MathRuleEngine CreateEngine(
            ParityPath path,
            Action<MathRuleEngineOptions> configure = null,
            Action<MathRuleEngine> setup = null)
        {
            var options = new MathRuleEngineOptions();
            configure?.Invoke(options);
            ApplyPath(path, options);

            var engine = PinnedEngine.Create(options);

            setup?.Invoke(engine);

            return engine;
        }

        public static Outcome Run(
            ParityPath path,
            string expression,
            Func<IDictionary<string, object>> parameters = null,
            Action<MathRuleEngineOptions> configure = null,
            Action<MathRuleEngine> setup = null)
        {
            var engine = CreateEngine(path, configure, setup);

            return RunOn(engine, path, expression, parameters);
        }

        internal static Outcome Run(ParityPath path, ParityCase testCase)
        {
            return Run(path, testCase.Expression, testCase.Parameters, testCase.Configure, testCase.Setup);
        }

        public static Outcome RunOn(
            MathRuleEngine engine,
            ParityPath path,
            string expression,
            Func<IDictionary<string, object>> parameters = null)
        {
            var outcome = new Outcome();
            var make = parameters ?? (() => null);

            try
            {
                switch (path)
                {
                    case ParityPath.Interpreter:
                    case ParityPath.Eager:
                    case ParityPath.EagerInterpretedTrees:
                        outcome.Value = engine.Evaluate(expression, make());
                        break;

                    case ParityPath.TieredSecondUse:
                        try
                        {
                            engine.Evaluate(expression, make());
                        }
                        catch (Exception)
                        {
                        }

                        outcome.Value = engine.Evaluate(expression, make());
                        break;

                    case ParityPath.Compile:
                    case ParityPath.CompileInterpretedTrees:
                        var handle = engine.Compile(expression);
                        outcome.IsCompiled = handle.IsCompiled;
                        outcome.Value = handle.Evaluate(make());
                        break;
                }
            }
            catch (Exception ex)
            {
                outcome.Error = ex;
            }

            if (outcome.IsCompiled == null && path != ParityPath.Interpreter && !string.IsNullOrWhiteSpace(expression))
            {
                ExpressionProgram program;
                if (engine.TryPeekCachedProgram(expression, out program) && program != null)
                    outcome.IsCompiled = program.IsCompiled;
            }

            return outcome;
        }

        public static void AreEquivalent(Outcome expected, Outcome actual, string context)
        {
            var where = context + Environment.NewLine +
                        "  expected: " + expected.Describe() + Environment.NewLine +
                        "  actual  : " + actual.Describe();

            Assert.AreEqual(expected.Threw, actual.Threw, "Throw/return mismatch. " + where);

            if (!expected.Threw)
            {
                BitAssert.AreEqual(expected.Value, actual.Value, "Result bits differ. " + where);
                return;
            }

            AreSameException(expected.Error, actual.Error, where);
        }

        public static void AreSameException(Exception expected, Exception actual, string where)
        {
            Assert.AreEqual(expected.GetType(), actual.GetType(), "Exception type differs. " + where);
            Assert.IsTrue(string.Equals(expected.Message, actual.Message, StringComparison.Ordinal), "Message differs. " + where);

            if (expected is MathExprEngineException em)
                Assert.AreEqual(em.Column, ((MathExprEngineException)actual).Column, "Column differs. " + where);

            if (expected is ArgumentException ea)
                Assert.AreEqual(ea.ParamName, ((ArgumentException)actual).ParamName, "ParamName differs. " + where);

            if (expected.InnerException == null)
            {
                Assert.IsNull(actual.InnerException, "Unexpected InnerException. " + where);
            }
            else
            {
                Assert.IsNotNull(actual.InnerException, "Missing InnerException. " + where);
                Assert.AreEqual(expected.InnerException.GetType(), actual.InnerException.GetType(), "InnerException type differs. " + where);
                Assert.IsTrue(
                    string.Equals(expected.InnerException.Message, actual.InnerException.Message, StringComparison.Ordinal),
                    "InnerException message differs. " + where);
            }
        }

        public static Outcome Matches(
            ParityPath path,
            string expression,
            Func<IDictionary<string, object>> parameters = null,
            Action<MathRuleEngineOptions> configure = null,
            Action<MathRuleEngine> setup = null)
        {
            var oracle = Run(ParityPath.Interpreter, expression, parameters, configure, setup);
            var actual = Run(path, expression, parameters, configure, setup);

            AreEquivalent(oracle, actual, "Expression \"" + expression + "\" on path " + path);

            return oracle;
        }

        internal static Outcome Matches(ParityPath path, ParityCase testCase)
        {
            return Matches(path, testCase.Expression, testCase.Parameters, testCase.Configure, testCase.Setup);
        }

        public static Outcome AllPathsMatch(
            string expression,
            Func<IDictionary<string, object>> parameters = null,
            Action<MathRuleEngineOptions> configure = null,
            Action<MathRuleEngine> setup = null)
        {
            Outcome oracle = null;

            foreach (var path in CompiledPaths)
                oracle = Matches(path, expression, parameters, configure, setup);

            return oracle;
        }
    }
}
