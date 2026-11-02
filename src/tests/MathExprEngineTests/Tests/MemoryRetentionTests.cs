using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using MathExprEngine;
using MathExprEngine.Exceptions;
using MathExprEngineTests.Helpers.Engines;
using MathExprEngineTests.Helpers.Inputs;
using MathExprEngineTests.Helpers.TestBases;

namespace MathExprEngineTests.Tests
{
    [TestClass]
    public class MemoryRetentionTests : DepthGuardedTestBase
    {
        private const string Text = "x * 2 + 1";

        public TestContext TestContext { get; set; }

        private static void CollectThenAssertDead(string[] names, WeakReference[] references)
        {
            for (var attempt = 0; attempt < 3 && references.Any(r => r.IsAlive); attempt++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            for (var i = 0; i < references.Length; i++)
                Assert.IsFalse(references[i].IsAlive, names[i] + " is still reachable after 3 full collections.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static MathRuleEngine CreateEngine(int capacity)
        {
            return PinnedEngine.Create(new MathRuleEngineOptions { ExpressionCacheCapacity = capacity });
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static CompiledExpression CreateCompiledHandle()
        {
            var handle = CreateEngine(0).Compile(Text);
            Assert.IsTrue(handle.IsCompiled);

            return handle;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference[] EvaluateHandleAndDropParameters(CompiledExpression handle)
        {
            object boxed = 3.0;
            var parameters = new Dictionary<string, object> { { "x", boxed } };

            Assert.AreEqual(7.0, handle.Evaluate(parameters));

            return new[] { new WeakReference(parameters), new WeakReference(boxed) };
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference[] EvaluateHandleThrowingAndDropParameters(CompiledExpression handle)
        {
            object boxed = 4.0;
            var parameters = new Dictionary<string, object> { { "x", boxed } };
            MathExprEngineException kept = null;

            try
            {
                handle.Evaluate(parameters);
            }
            catch (MathExprEngineException ex)
            {
                kept = ex;
            }

            Assert.IsNotNull(kept, "The evaluation was expected to throw.");
            GC.KeepAlive(kept);

            return new[] { new WeakReference(parameters), new WeakReference(boxed) };
        }

        [TestMethod]
        public void CompiledHandle_AfterSuccessfulEvaluation_HoldsNoParameterDictionary_Test()
        {
            var handle = CreateCompiledHandle();

            var references = EvaluateHandleAndDropParameters(handle);
            CollectThenAssertDead(new[] { "parameters dictionary", "parameter value" }, references);

            Assert.AreEqual(9.0, handle.Evaluate(ParameterSets.X(4.0)));
            GC.KeepAlive(handle);
        }

        [TestMethod]
        public void CompiledHandle_AfterThrowingEvaluation_HoldsNoParameterDictionary_Test()
        {
            MathRuleEngine engine = CreateEngine(0);
            var handle = engine.Compile("10 / (x - x)");
            Assert.IsTrue(handle.IsCompiled);

            var references = EvaluateHandleThrowingAndDropParameters(handle);
            CollectThenAssertDead(new[] { "parameters dictionary", "parameter value" }, references);

            GC.KeepAlive(handle);
            GC.KeepAlive(engine);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference[] EvaluateTwiceThroughCacheAndDropParameters(MathRuleEngine engine)
        {
            var first = new Dictionary<string, object> { { "x", 1.0 } };
            var second = new Dictionary<string, object> { { "x", 2.0 } };

            Assert.AreEqual(3.0, engine.Evaluate(Text, first));
            Assert.AreEqual(5.0, engine.Evaluate(Text, second));

            var program = CachedPrograms.Peek(engine, Text);
            Assert.IsTrue(program.IsCompiled);

            return new[] { new WeakReference(first), new WeakReference(second) };
        }

        [TestMethod]
        public void CachedProgram_AfterInterpretedAndCompiledCalls_HoldsNoParameterDictionary_Test()
        {
            var engine = CreateEngine(16);

            var references = EvaluateTwiceThroughCacheAndDropParameters(engine);
            CollectThenAssertDead(new[] { "1st-call dictionary", "2nd-call (tier-up) dictionary" }, references);

            GC.KeepAlive(engine);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference[] CreateCompiledHandleOnCapacityZeroEngineAndDrop()
        {
            var engine = CreateEngine(0);
            var handle = engine.Compile(Text);
            Assert.IsTrue(handle.IsCompiled);
            Assert.AreEqual(7.0, handle.Evaluate(ParameterSets.X(3.0)));
            object compiledDelegate = handle.Program.Compiled.Delegate;
            Assert.IsNotNull(compiledDelegate);

            return new[] { new WeakReference(compiledDelegate), new WeakReference(handle), new WeakReference(engine) };
        }

        [TestMethod]
        public void DroppedHandle_FromCapacityZeroEngine_DelegateHandleAndEngineAreCollected_Test()
        {
            var references = CreateCompiledHandleOnCapacityZeroEngineAndDrop();

            CollectThenAssertDead(new[] { "compiled delegate", "handle", "engine" }, references);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference[] TierUpAndCaptureDelegate(MathRuleEngine engine)
        {
            engine.Evaluate(Text, ParameterSets.X(1.0));
            engine.Evaluate(Text, ParameterSets.X(2.0));

            var program = CachedPrograms.Peek(engine, Text);
            Assert.IsTrue(program.IsCompiled);
            object compiledDelegate = program.Compiled.Delegate;

            return new[] { new WeakReference(compiledDelegate), new WeakReference(program) };
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool EvictAndConfirmWithPeek(MathRuleEngine engine, int capacity)
        {
            for (var i = 0; i < 2 * capacity; i++)
                engine.Evaluate("3000 + " + i);

            return !CachedPrograms.IsCached(engine, Text);
        }

        [TestMethod]
        public void EvictedEntry_DelegateAndProgramCollected_WhileEngineStaysAlive_Test()
        {
            const int capacity = 4;
            var engine = CreateEngine(capacity);

            var references = TierUpAndCaptureDelegate(engine);
            Assert.IsTrue(EvictAndConfirmWithPeek(engine, capacity), "Test setup: the entry must be evicted from both generations.");

            CollectThenAssertDead(new[] { "evicted compiled delegate", "evicted program" }, references);

            Assert.AreEqual(7.0, engine.Evaluate(Text, ParameterSets.X(3.0)), "The engine must still work.");
            GC.KeepAlive(engine);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference[] CreateUseAndDropTieredEngine()
        {
            var engine = CreateEngine(1024);
            engine.RegisterFunction("twice", a => a[0] * 2);
            engine.Evaluate("twice(x) + 1", ParameterSets.X(1.0));
            engine.Evaluate("twice(x) + 1", ParameterSets.X(2.0));

            var program = CachedPrograms.Peek(engine, "twice(x) + 1");
            Assert.IsTrue(program.IsCompiled);

            return new[] { new WeakReference(engine), new WeakReference(program), new WeakReference(program.Compiled.Delegate) };
        }

        [TestMethod]
        public void UnreferencedEngine_WithCompiledCache_IsCollected_NoStaticRoot_Test()
        {
            var references = CreateUseAndDropTieredEngine();

            CollectThenAssertDead(new[] { "engine", "cached program", "compiled delegate" }, references);
        }
    }
}
