// ***********************************************************************
//  Assembly          : RzR.Shared.Entity.MathExprEngine
//  Author            : RzR
//  Created On        : 2026-10-22 20:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-10-31 15:53
//  ***********************************************************************
//  <copyright file="MathRuleEngineOptions.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using System;
using System.Linq.Expressions;

#endregion

namespace MathExprEngine
{
    /// <summary>
    ///     The <see cref="MathRuleEngine" /> limits and switches, copied at construction.
    /// </summary>
    public sealed class MathRuleEngineOptions
    {
        /// <summary>
        ///     (Immutable) the largest <see cref="MaxExpressionLength" /> the engine constructor accepts.
        /// </summary>
        internal const int MaxExpressionLengthCeiling = 65536;

        /// <summary>
        ///     (Immutable) the largest <see cref="MaxTokens" /> the engine constructor accepts.
        /// </summary>
        internal const int MaxTokensCeiling = 65536;

        /// <summary>
        ///     (Immutable) the largest <see cref="MaxNodes" /> the engine constructor accepts.
        /// </summary>
        internal const int MaxNodesCeiling = 65536;

        /// <summary>
        ///     (Immutable) the largest <see cref="MaxDepth" /> the engine constructor accepts.
        /// </summary>
        internal const int MaxDepthCeiling = 1024;

        /// <summary>
        ///     (Immutable) the largest <see cref="MaxEvaluationDepth" /> the engine constructor accepts.
        /// </summary>
        internal const int MaxEvaluationDepthCeiling = 2048;

        /// <summary>
        ///     (Immutable) the largest <see cref="MaxFunctionArgs" /> the engine constructor accepts.
        /// </summary>
        internal const int MaxFunctionArgsCeiling = 4096;

        /// <summary>
        ///     (Immutable) the largest <see cref="ExpressionCacheCapacity" /> the engine constructor accepts.
        /// </summary>
        internal const int ExpressionCacheCapacityCeiling = 65536;

        /// <summary>
        ///     (Immutable) the default <see cref="CompilationThreshold" />, compiling an expression on its second use.
        /// </summary>
        internal const int DefaultCompilationThreshold = 2;

        /// <summary>
        ///     (Immutable) the largest <see cref="CompilationThreshold" /> the engine constructor accepts.
        /// </summary>
        internal const int CompilationThresholdCeiling = 1024;

        /// <summary>
        ///     Gets or sets the maximum expression length in characters (default <c>4096</c>, range <c>1..65536</c>).
        /// </summary>
        /// <value>
        ///     Default <c>4096</c>, range <c>1..65536</c>.
        /// </value>
        public int MaxExpressionLength { get; set; } = 4096;

        /// <summary>
        ///     Gets or sets the maximum token count per expression (default <c>2048</c>, range <c>1..65536</c>).
        /// </summary>
        /// <value>
        ///     Default <c>2048</c>, range <c>1..65536</c>.
        /// </value>
        public int MaxTokens { get; set; } = 2048;

        /// <summary>
        ///     Gets or sets the maximum AST node count per expression (default <c>4096</c>, range <c>1..65536</c>).
        /// </summary>
        /// <value>
        ///     Default <c>4096</c>, range <c>1..65536</c>.
        /// </value>
        public int MaxNodes { get; set; } = 4096;

        /// <summary>
        ///     Gets or sets the parser step limit, ~2 per nesting level (default <c>256</c>, range <c>1..1024</c>).
        /// </summary>
        /// <value>
        ///     Default <c>256</c>, range <c>1..1024</c>.
        /// </value>
        public int MaxDepth { get; set; } = 256;

        /// <summary>
        ///     Gets or sets the per-thread evaluation depth limit, re-entrant calls included (default <c>512</c>,
        ///     range <c>1..2048</c>).
        /// </summary>
        /// <value>
        ///     Default <c>512</c>, range <c>1..2048</c>.
        /// </value>
        public int MaxEvaluationDepth { get; set; } = 512;

        /// <summary>
        ///     Gets or sets the maximum argument count per function call (default <c>64</c>, range <c>1..4096</c>).
        /// </summary>
        /// <value>
        ///     Default <c>64</c>, range <c>1..4096</c>.
        /// </value>
        public int MaxFunctionArgs { get; set; } = 64;

        /// <summary>
        ///     Gets or sets a value indicating whether <c>NaN</c> or <c>Infinity</c> results propagate (default false).
        /// </summary>
        /// <value>
        ///     True to propagate them, false (default) to throw at the originating node.
        /// </value>
        public bool AllowNonFiniteResults { get; set; }

        /// <summary>
        ///     Gets or sets the expression cache size limit (default <c>1024</c>, range <c>0..65536</c>).
        /// </summary>
        /// <value>
        ///     Default <c>1024</c>, range <c>0..65536</c>; <c>0</c> disables the cache.
        /// </value>
        public int ExpressionCacheCapacity { get; set; } = 1024;

        /// <summary>
        ///     Gets or sets a value indicating whether expressions are compiled to delegates (default true).
        /// </summary>
        /// <value>
        ///     True (default) to compile hot expressions; false to keep interpreting.
        /// </value>
        public bool EnableCompilation { get; set; } = true;

        /// <summary>
        ///     Gets or sets how many uses trigger compilation (default <c>2</c>, range <c>1..1024</c>).
        /// </summary>
        /// <value>
        ///     Default <c>2</c>, range <c>1..1024</c>; <c>1</c> compiles on first use.
        /// </value>
        public int CompilationThreshold { get; set; } = DefaultCompilationThreshold;

        /// <summary>
        ///     Gets or sets a value indicating whether lambdas use the LINQ interpreter (test-only seam).
        /// </summary>
        /// <value>
        ///     True to interpret (netstandard2.1 only); false (default) to compile to IL.
        /// </value>
        internal bool PreferInterpretedExpressionTrees { get; set; }

        /// <summary>
        ///     Gets or sets a pre-compile fault hook, not copied by <see cref="MathRuleEngine.TestOptionsOverride" /> (test-only seam).
        /// </summary>
        /// <value>
        ///     The fault injector, or null (default) for none.
        /// </value>
        internal Action<LambdaExpression> CompileFaultInjector { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether dynamic code is treated as unavailable (test-only seam).
        /// </summary>
        /// <value>
        ///     True to simulate a platform without dynamic code; false (default) otherwise.
        /// </value>
        internal bool SimulateNoDynamicCode { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether unexpected compile faults are rethrown (test-only seam).
        /// </summary>
        /// <value>
        ///     True to rethrow compile faults; false (default) to fall back.
        /// </value>
        internal bool ThrowOnCompileFault { get; set; }

        /// <summary>
        ///     Creates a copy of this instance for an engine snapshot.
        /// </summary>
        /// <returns>
        ///     A new <see cref="MathRuleEngineOptions" /> with every property and internal seam copied.
        /// </returns>
        internal MathRuleEngineOptions Clone()
        {
            return new MathRuleEngineOptions
            {
                MaxExpressionLength = MaxExpressionLength,
                MaxTokens = MaxTokens,
                MaxNodes = MaxNodes,
                MaxDepth = MaxDepth,
                MaxEvaluationDepth = MaxEvaluationDepth,
                MaxFunctionArgs = MaxFunctionArgs,
                AllowNonFiniteResults = AllowNonFiniteResults,
                ExpressionCacheCapacity = ExpressionCacheCapacity,
                EnableCompilation = EnableCompilation,
                CompilationThreshold = CompilationThreshold,
                PreferInterpretedExpressionTrees = PreferInterpretedExpressionTrees,
                CompileFaultInjector = CompileFaultInjector,
                SimulateNoDynamicCode = SimulateNoDynamicCode,
                ThrowOnCompileFault = ThrowOnCompileFault
            };
        }
    }
}