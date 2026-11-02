using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathExprEngine;
using MathExprEngine.Exceptions;
using MathExprEngineTests.Helpers.Assertions;
using MathExprEngineTests.Helpers.Engines;
using MathExprEngineTests.Helpers.Inputs;
using MathExprEngineTests.Helpers.TestBases;

namespace MathExprEngineTests.Tests
{
    [TestClass]
    public class ExpressionCacheTests : DepthGuardedTestBase
    {
        public TestContext TestContext { get; set; }

        private static void Evict(MathRuleEngine engine, int capacity, string expression)
        {
            for (var i = 0; i < 2 * capacity; i++)
                engine.Evaluate("1000 + " + i);

            Assert.IsFalse(CachedPrograms.IsCached(engine, expression), "Test setup: '" + expression + "' must be evicted from both generations.");
        }

        private static string BalancedTree(int depth)
        {
            if (depth == 0)
                return "1";

            var child = BalancedTree(depth - 1);

            return new StringBuilder(child.Length * 2 + 3).Append('(').Append(child).Append('+').Append(child).Append(')').ToString();
        }

        [TestMethod]
        public void Capacity16_ThousandDistinctExpressions_EntryCountNeverExceedsCapacity_Test()
        {
            const int capacity = 16;
            var engine = PinnedEngine.Create(new MathRuleEngineOptions { ExpressionCacheCapacity = capacity });

            for (var i = 0; i < 1000; i++)
            {
                Assert.AreEqual(i + 2.0, engine.Evaluate("x + " + i, ParameterSets.X(2)));
                Assert.IsTrue(engine.CachedExpressionCount <= capacity, "Entry count " + engine.CachedExpressionCount + " exceeded capacity after insert " + i);
            }

            Assert.IsTrue(engine.CachedExpressionCount >= 1);
            Assert.AreEqual(1000, engine.PrepareCount);
        }

        [TestMethod]
        public void CapacityZero_NothingCached_V2PathDoesNotPrepareOrCompile_Test()
        {
            var engine = PinnedEngine.Create(new MathRuleEngineOptions { ExpressionCacheCapacity = 0 });

            for (var i = 0; i < 5; i++)
                Assert.AreEqual(7.0, engine.Evaluate("x * 2 + 1", ParameterSets.X(3)));

            Assert.AreEqual(0, engine.CachedExpressionCount);
            Assert.AreEqual(0, engine.RetainedNodeCount);
            Assert.AreEqual(0, engine.PrepareCount);
            Assert.AreEqual(0, engine.CompileCount);
            Assert.IsFalse(CachedPrograms.IsCached(engine, "x * 2 + 1"));
        }

        [TestMethod]
        [Timeout(120000)]
        public void AdversarialFill_Interpreted_Capacity1024_RetainedNodesWithinBudget_Test()
        {
            var options = new MathRuleEngineOptions
            {
                EnableCompilation = false,
                ExpressionCacheCapacity = 1024,
                MaxNodes = 8192,
                MaxTokens = 16384,
                MaxExpressionLength = 16384
            };
            var engine = PinnedEngine.Create(options);
            var tree = BalancedTree(11);
            var largestEntryNodes = 0;

            for (var i = 0; i < 200; i++)
            {
                var text = i + "+" + tree;
                Assert.AreEqual(i + 2048.0, engine.Evaluate(text));

                var nodes = CachedPrograms.Peek(engine, text).NodeCount;
                if (nodes > largestEntryNodes)
                    largestEntryNodes = nodes;

                Assert.IsTrue(
                    engine.RetainedNodeCount <= MathExprEngine.Helpers.ExpressionCache.NodeBudget + largestEntryNodes,
                    "RetainedNodeCount " + engine.RetainedNodeCount + " exceeded NodeBudget + largest entry after insert " + i);
                Assert.IsTrue(engine.CachedExpressionCount <= 1024);
            }

            Assert.AreEqual(4097, largestEntryNodes);
            Assert.IsTrue(engine.RetainedNodeCount > 0);
            TestContext.WriteLine("Interpreted fill: retained nodes " + engine.RetainedNodeCount + ", entries " + engine.CachedExpressionCount);
        }

        [TestMethod]
        [Timeout(30000)]
        public void AdversarialFill_Compiled_64Entries_RetainedNodesWithinBudget_AndEntriesCompiled_Test()
        {
            var options = new MathRuleEngineOptions
            {
                CompilationThreshold = 1,
                ExpressionCacheCapacity = 64,
                MaxNodes = 8192,
                MaxTokens = 16384,
                MaxExpressionLength = 16384
            };
            var engine = PinnedEngine.Create(options);
            var tree = BalancedTree(10);
            var largestEntryNodes = 0;

            for (var i = 0; i < 64; i++)
            {
                var text = i + "+" + tree;
                Assert.AreEqual(i + 1024.0, engine.Evaluate(text));

                var program = CachedPrograms.Peek(engine, text);
                Assert.IsTrue(program.IsCompiled, "Entry " + i + " must compile in the compiled variant.");

                if (program.NodeCount > largestEntryNodes)
                    largestEntryNodes = program.NodeCount;

                Assert.IsTrue(
                    engine.RetainedNodeCount <= MathExprEngine.Helpers.ExpressionCache.NodeBudget + largestEntryNodes,
                    "RetainedNodeCount " + engine.RetainedNodeCount + " exceeded NodeBudget + largest entry after insert " + i);
                Assert.IsTrue(engine.CachedExpressionCount <= 64);
            }

            Assert.AreEqual(64, engine.CompileCount);
            TestContext.WriteLine("Compiled fill: retained nodes " + engine.RetainedNodeCount + ", entries " + engine.CachedExpressionCount);
        }

        [TestMethod]
        public void EvictedExpression_ReevaluatesToIdenticalResultAndException_Test()
        {
            const int capacity = 4;
            const string text = "10 / (x - 1)";
            var engine = PinnedEngine.Create(new MathRuleEngineOptions { ExpressionCacheCapacity = capacity });

            engine.Evaluate(text, ParameterSets.X(3));
            var before = engine.Evaluate(text, ParameterSets.X(3));
            Assert.IsTrue(CachedPrograms.Peek(engine, text).IsCompiled);
            var errorBefore = Assert.ThrowsException<MathRuleEngineException>(() => engine.Evaluate(text, ParameterSets.X(1)));

            Evict(engine, capacity, text);

            var after = engine.Evaluate(text, ParameterSets.X(3));
            var errorAfter = Assert.ThrowsException<MathRuleEngineException>(() => engine.Evaluate(text, ParameterSets.X(1)));

            BitAssert.AreEqual(before, after);
            EngineExceptionAssert.SameMessageAndColumn(errorBefore, errorAfter);
            Assert.AreNotSame(errorBefore, errorAfter);
        }

        [TestMethod]
        public void HandleSurvivesEviction_StaysCompiled_AndEvaluateAfterEvictionRePrepares_Test()
        {
            const int capacity = 4;
            const string text = "x * x + 1";
            var engine = PinnedEngine.Create(new MathRuleEngineOptions { ExpressionCacheCapacity = capacity });

            engine.Evaluate(text, ParameterSets.X(1));
            engine.Evaluate(text, ParameterSets.X(2));
            var cached = CachedPrograms.Peek(engine, text);
            var handle = engine.Compile(text);
            Assert.AreSame(cached, handle.Program);
            Assert.IsTrue(handle.IsCompiled);

            Evict(engine, capacity, text);

            Assert.AreEqual(10.0, handle.Evaluate(ParameterSets.X(3)), "A handle must keep working after its shared entry is evicted.");
            Assert.IsTrue(handle.IsCompiled);

            var prepares = engine.PrepareCount;
            Assert.AreEqual(17.0, engine.Evaluate(text, ParameterSets.X(4)));

            Assert.AreEqual(prepares + 1, engine.PrepareCount, "Evaluate after eviction must re-Prepare.");
            var fresh = CachedPrograms.Peek(engine, text);
            Assert.AreNotSame(handle.Program, fresh);
            Assert.AreEqual(1, fresh.UseCount, "The re-Prepared entry restarts its use count (AC-B16).");
        }

        [TestMethod]
        public void PreviousGenerationHits_ArePromoted_NeverRePreparedOrRecompiled_Test()
        {
            const int capacity = 4;
            const string text = "x * 3";
            var engine = PinnedEngine.Create(new MathRuleEngineOptions { ExpressionCacheCapacity = capacity });

            engine.Evaluate(text, ParameterSets.X(1));
            engine.Evaluate(text, ParameterSets.X(2));
            var program = CachedPrograms.Peek(engine, text);
            Assert.IsTrue(program.IsCompiled);
            Assert.AreEqual(1, engine.CompileCount);

            for (var i = 0; i < 20; i++)
            {
                engine.Evaluate("2000 + " + i);
                Assert.AreEqual(3.0 * i, engine.Evaluate(text, ParameterSets.X(i)));
            }

            Assert.AreEqual(1 + 20, engine.PrepareCount, "Only the fillers may Prepare; the promoted text must never re-Prepare.");
            Assert.AreEqual(1, engine.CompileCount, "Promotion must keep the compiled state.");
            Assert.AreSame(program, CachedPrograms.Peek(engine, text));
            Assert.IsTrue(program.IsCompiled);
        }

        [TestMethod]
        public void CapacityOne_AlternatingTwoTexts_CorrectResultsAndCountAtMostOne_Test()
        {
            var engine = PinnedEngine.Create(new MathRuleEngineOptions { ExpressionCacheCapacity = 1 });

            for (var i = 0; i < 20; i++)
            {
                Assert.AreEqual(i + 1.0, engine.Evaluate("x + 1", ParameterSets.X(i)));
                Assert.IsTrue(engine.CachedExpressionCount <= 1);
                Assert.AreEqual(i + 2.0, engine.Evaluate("x + 2", ParameterSets.X(i)));
                Assert.IsTrue(engine.CachedExpressionCount <= 1);
            }
        }

        [TestMethod]
        [Timeout(30000)]
        public void HotSet600_Capacity1024_AfterWarmUp_RepeatedHitsNeverReprepareOrRecompile_Test()
        {
            const int capacity = 1024;
            const int hotSet = 600;
            var engine = PinnedEngine.Create(new MathRuleEngineOptions { ExpressionCacheCapacity = capacity });
            var texts = Enumerable.Range(0, hotSet).Select(i => "x * " + i + " + 1").ToArray();

            for (var round = 0; round < 5; round++)
                for (var i = 0; i < hotSet; i++)
                    Assert.AreEqual(2.0 * i + 1, engine.Evaluate(texts[i], ParameterSets.X(2)));

            var prepares = engine.PrepareCount;
            var compiles = engine.CompileCount;
            var entries = engine.CachedExpressionCount;
            TestContext.WriteLine("R2 warm-up: prepares " + prepares + ", compiles " + compiles + ", entries " + entries);
            Assert.IsTrue(prepares >= hotSet);

            for (var round = 0; round < 5; round++)
            {
                for (var i = 0; i < hotSet; i++)
                    Assert.AreEqual(3.0 * i + 1, engine.Evaluate(texts[i], ParameterSets.X(3)));

                Assert.AreEqual(prepares, engine.PrepareCount, "Round " + round + ": a hot hit must never re-Prepare (no rotation eviction).");
                Assert.AreEqual(compiles, engine.CompileCount, "Round " + round + ": a hot hit must never recompile.");
                Assert.AreEqual(entries, engine.CachedExpressionCount, "Round " + round + ": the entry count must stay stable.");
                Assert.IsTrue(engine.CachedExpressionCount <= capacity);
            }

            for (var i = 0; i < hotSet; i++)
                Assert.IsTrue(CachedPrograms.Peek(engine, texts[i]).IsCompiled, "Hot text " + i + " must still be cached and compiled.");
        }

        [TestMethod]
        [Timeout(20000)]
        public void FullCache_HotSet48_SurvivesPairsOfNewTexts_OnlyNewTextsPrepare_Test()
        {
            const int capacity = 64;
            const int hotSet = 48;
            var engine = PinnedEngine.Create(new MathRuleEngineOptions { ExpressionCacheCapacity = capacity });
            var hot = Enumerable.Range(0, hotSet).Select(i => "x * " + i + " - 7").ToArray();

            for (var round = 0; round < 3; round++)
                for (var i = 0; i < hotSet; i++)
                    Assert.AreEqual(2.0 * i - 7, engine.Evaluate(hot[i], ParameterSets.X(2)));

            for (var i = 0; i < hotSet; i++)
                Assert.IsTrue(CachedPrograms.Peek(engine, hot[i]).IsCompiled, "Warm-up: hot text " + i + " must be compiled.");

            var filler = 0;
            while (engine.CachedExpressionCount < capacity && filler < 1000)
                engine.Evaluate("5000 + " + filler++);

            Assert.AreEqual(capacity, engine.CachedExpressionCount, "Test setup: the cache must be full (" + filler + " fillers added).");

            for (var cycle = 0; cycle < 5; cycle++)
            {
                var prepares = engine.PrepareCount;
                var compiles = engine.CompileCount;

                engine.Evaluate("9000 + " + (2 * cycle));
                Assert.IsTrue(engine.CachedExpressionCount <= capacity, "cycle " + cycle + ": entry bound");
                engine.Evaluate("9000 + " + (2 * cycle + 1));
                Assert.IsTrue(engine.CachedExpressionCount <= capacity, "cycle " + cycle + ": entry bound");

                for (var i = 0; i < hotSet; i++)
                {
                    Assert.AreEqual(3.0 * i - 7, engine.Evaluate(hot[i], ParameterSets.X(3)));
                    Assert.IsTrue(engine.CachedExpressionCount <= capacity, "cycle " + cycle + ": entry bound");
                }

                var preparesGrowth = engine.PrepareCount - prepares;
                var compilesGrowth = engine.CompileCount - compiles;
                var hotCached = hot.Count(t => CachedPrograms.IsCached(engine, t));
                TestContext.WriteLine("cycle " + cycle + ": prepares +" + preparesGrowth + ", compiles +" + compilesGrowth + ", hot cached " + hotCached);

                Assert.IsTrue(preparesGrowth >= 2 && preparesGrowth <= 3, "cycle " + cycle + ": Prepares must be the 2 new texts plus at most 1 re-prepared hot text, was +" + preparesGrowth);
                Assert.IsTrue(compilesGrowth <= 1, "cycle " + cycle + ": at most one re-prepared hot text may recompile, was +" + compilesGrowth);
                Assert.IsTrue(hotCached >= hotSet - 1, "cycle " + cycle + ": at least 47 of 48 hot texts must survive, was " + hotCached);
            }
        }

        [TestMethod]
        [Timeout(20000)]
        public void CeilingCapacity65536_FullThenPromotionAndInsertBurst_CompletesBoundedAndWithinCapacity_Test()
        {
            const int capacity = 65536;
            var engine = PinnedEngine.Create(new MathRuleEngineOptions { ExpressionCacheCapacity = capacity, EnableCompilation = false });

            for (var i = 0; i < capacity; i++)
                engine.Evaluate("x + " + i, ParameterSets.X(1));

            Assert.IsTrue(engine.CachedExpressionCount <= capacity, "after fill");

            for (var i = 0; i < 20000; i++)
            {
                Assert.AreEqual(1.0 + i, engine.Evaluate("x + " + i, ParameterSets.X(1)));
                engine.Evaluate("y + " + i, new Dictionary<string, object> { { "y", 0.0 } });
            }

            Assert.IsTrue(engine.CachedExpressionCount <= capacity, "after burst: " + engine.CachedExpressionCount);
            TestContext.WriteLine("S8 burst: prepares " + engine.PrepareCount + ", entries " + engine.CachedExpressionCount);
        }

        [TestMethod]
        public void FailingText_Evaluate_TakesNoCacheSlot_AndThrowsFreshIdenticalExceptions_Test()
        {
            var engine = PinnedEngine.Create(new MathRuleEngineOptions { ExpressionCacheCapacity = 4, CompilationThreshold = 1 });
            engine.Evaluate("x + 1", ParameterSets.X(1));
            var countBefore = engine.CachedExpressionCount;

            var first = Assert.ThrowsException<ExpressionSyntaxException>(() => engine.Evaluate("1+"));
            var second = Assert.ThrowsException<ExpressionSyntaxException>(() => engine.Evaluate("1+"));

            Assert.AreNotSame(first, second, "Exceptions must never be cached or reused (SR-4).");
            EngineExceptionAssert.SameMessageAndColumn(first, second);
            Assert.AreEqual(countBefore, engine.CachedExpressionCount, "A failing text must take no cache slot.");
            Assert.IsFalse(CachedPrograms.IsCached(engine, "1+"));
            Assert.IsTrue(CachedPrograms.IsCached(engine, "x + 1"), "A failing text must not evict a valid entry.");
            Assert.AreEqual(3, engine.PrepareCount, "Failing Prepares are counted.");
            Assert.AreEqual(1, engine.CompileCount, "Only the valid text compiled.");
        }

        [TestMethod]
        public void FailingText_Compile_ThrowsFreshIdenticalExceptions_Test()
        {
            var engine = PinnedEngine.Create();

            var first = Assert.ThrowsException<ExpressionSyntaxException>(() => engine.Compile("1+"));
            var second = Assert.ThrowsException<ExpressionSyntaxException>(() => engine.Compile("1+"));

            Assert.AreNotSame(first, second);
            EngineExceptionAssert.SameMessageAndColumn(first, second);
            Assert.AreEqual(0, engine.CachedExpressionCount);
            Assert.AreEqual(0, engine.CompileCount);
        }

        [TestMethod]
        public void RuntimeError_FromCompiledEntry_ThrowsFreshInstanceEachCall_Test()
        {
            var engine = PinnedEngine.Create(new MathRuleEngineOptions { CompilationThreshold = 1 });

            var first = Assert.ThrowsException<MathRuleEngineException>(() => engine.Evaluate("1 / x", ParameterSets.X(0)));
            var second = Assert.ThrowsException<MathRuleEngineException>(() => engine.Evaluate("1 / x", ParameterSets.X(0)));

            Assert.IsTrue(CachedPrograms.Peek(engine, "1 / x").IsCompiled);
            Assert.AreNotSame(first, second);
            EngineExceptionAssert.SameMessageAndColumn(first, second);
        }

        [TestMethod]
        public void TwoEngines_SameText_NeverShareEntriesOptionsOrFunctions_Test()
        {
            var permissive = PinnedEngine.Create(new MathRuleEngineOptions { AllowNonFiniteResults = true });
            var strict = PinnedEngine.Create();
            permissive.RegisterFunction("f", a => 1);
            strict.RegisterFunction("f", a => 2);

            for (var i = 0; i < 3; i++)
            {
                Assert.IsTrue(double.IsPositiveInfinity(permissive.Evaluate("10^400 + f()")));
                Assert.ThrowsException<MathRuleEngineException>(() => strict.Evaluate("10^400 + f()"));
                Assert.AreEqual(1.0, permissive.Evaluate("f()"));
                Assert.AreEqual(2.0, strict.Evaluate("f()"));
            }

            Assert.AreNotSame(CachedPrograms.Peek(permissive, "f()"), CachedPrograms.Peek(strict, "f()"));
            Assert.AreEqual(2, permissive.PrepareCount);
            Assert.AreEqual(2, strict.PrepareCount);
        }

        [TestMethod]
        public void CompiledCacheEntry_DoesNotFreezeFunctionImplementation_Test()
        {
            var engine = PinnedEngine.Create();
            engine.RegisterFunction("f", a => a[0] * 2);

            engine.Evaluate("f(x) + 1", ParameterSets.X(1));
            Assert.AreEqual(5.0, engine.Evaluate("f(x) + 1", ParameterSets.X(2)));
            Assert.IsTrue(CachedPrograms.Peek(engine, "f(x) + 1").IsCompiled);

            engine.RegisterFunction("F", a => a[0] * 10);

            Assert.AreEqual(21.0, engine.Evaluate("f(x) + 1", ParameterSets.X(2)), "The replaced implementation must be used on the next call.");
            Assert.AreEqual(1, engine.CompileCount, "Late binding must not require a recompile.");
        }
    }
}
