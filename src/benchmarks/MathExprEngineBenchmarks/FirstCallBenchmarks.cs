using BenchmarkDotNet.Attributes;
using MathExprEngine;
using MathExprEngineBenchmarks.Enums;
using System.Collections.Generic;

namespace MathExprEngineBenchmarks
{
    [MemoryDiagnoser]
    [ProcessCount(3)]
    public class FirstCallBenchmarks
    {
        private const int PoolMask = Corpus.UniqueTextPoolSize - 1;

        private string[] _texts;
        private int _next;
        private IDictionary<string, object> _parameters;
        private MathRuleEngine _interpreter;
#if !BASELINE_ENGINE
        private MathRuleEngine _tiered;
        private MathRuleEngine _eager;
#endif

        [Params(ExpressionSize.Small, ExpressionSize.Medium, ExpressionSize.Large)]
        public ExpressionSize Size { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _texts = Corpus.CreateUniqueVariants(Corpus.GetExpression(Size));
            _parameters = Corpus.CreateParameters(Size);
            _interpreter = EngineFactory.CreateInterpreter();
#if !BASELINE_ENGINE
            _tiered = EngineFactory.CreateDefault();
            _eager = EngineFactory.CreateEager();
#endif
        }

        [Benchmark(Baseline = true)]
        public double Interpreter_OneShot()
        {
            return _interpreter.Evaluate(NextText(), _parameters);
        }

#if !BASELINE_ENGINE
        [Benchmark]
        public double Evaluate_FirstUse_UniqueText()
        {
            return _tiered.Evaluate(NextText(), _parameters);
        }

        [Benchmark]
        public double Evaluate_TwoUses_UniqueText()
        {
            var text = NextText();
            _tiered.Evaluate(text, _parameters);

            return _tiered.Evaluate(text, _parameters);
        }

        [Benchmark]
        public double Evaluate_FirstUse_UniqueText_Eager()
        {
            return _eager.Evaluate(NextText(), _parameters);
        }

        [Benchmark]
        public CompiledExpression Compile_Cost()
        {
            return _tiered.Compile(NextText());
        }
#endif

        private string NextText()
        {
            _next = (_next + 1) & PoolMask;

            return _texts[_next];
        }
    }
}
