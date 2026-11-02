// ***********************************************************************
//  Assembly          : RzR.Shared.Entity.MathExprEngine
//  Author            : RzR
//  Created On        : 2026-10-22 20:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-11-01 21:06
//  ***********************************************************************
//  <copyright file="MathRuleEngine.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using DomainCommonExtensions.ArraysExtensions;
using DomainCommonExtensions.CommonExtensions;
using DomainCommonExtensions.DataTypeExtensions;
using DomainCommonExtensions.Utilities.Ensure;
using MathExprEngine.Compilation;
using MathExprEngine.Enums;
using MathExprEngine.Exceptions;
using MathExprEngine.Helpers;
using MathExprEngine.Models;
using MathExprEngine.Nodes;

#endregion

namespace MathExprEngine
{
    /// <summary>
    ///     The mathematics rule engine.
    /// </summary>
    public sealed class MathRuleEngine
    {
        /// <summary>
        ///     The compile/cache switch override, null in production (test-only seam).
        /// </summary>
        internal static Func<MathRuleEngineOptions, MathRuleEngineOptions> TestOptionsOverride;

        /// <summary>
        ///     (Immutable) the per-engine expression cache, or null when
        ///     <see cref="MathRuleEngineOptions.ExpressionCacheCapacity" /> is <c>0</c>.
        /// </summary>
        private readonly ExpressionCache _cache;

        /// <summary>
        ///     (Immutable) the engine-owned counters shared by every program this engine prepares.
        /// </summary>
        private readonly CompilationCounters _counters = new();

        /// <summary>
        ///     (Immutable) the case-insensitive function registry with lock-free reads and concurrent registration.
        /// </summary>
        private readonly ConcurrentDictionary<string, Func<double[], double>> _functions =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     (Immutable) the frozen options snapshot for this engine instance.
        /// </summary>
        private readonly MathRuleEngineOptions _options;

        /// <summary>
        ///     (Immutable) the prepare step delegate, created once to avoid per-miss allocation.
        /// </summary>
        private readonly Func<string, ExpressionProgram> _prepare;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MathRuleEngine" /> class.
        /// </summary>
        /// <param name="functions">(Optional) The functions.</param>
        public MathRuleEngine(IDictionary<string, Func<double[], double>> functions = null)
            : this(null, functions)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MathRuleEngine" /> class.
        /// </summary>
        /// <param name="options">The options, copied and range-checked at construction, or null for defaults.</param>
        /// <param name="functions">(Optional) The functions.</param>
        public MathRuleEngine(MathRuleEngineOptions options,
            IDictionary<string, Func<double[], double>> functions = null)
        {
            _options = (options ?? new MathRuleEngineOptions()).Clone();
            ApplyTestOptionsOverride(_options);
            ValidateOptions(_options);

            _cache = _options.ExpressionCacheCapacity == 0
                ? null
                : new ExpressionCache(_options.ExpressionCacheCapacity);
            _prepare = Prepare;

            RegisterFunctions(BuiltInFunctions.GetAll());

            if (functions.IsNotNullOrEmptyEnumerable())
                RegisterFunctions(functions);
        }

        /// <summary>
        ///     Gets the number of prepare calls, failures included (test hook).
        /// </summary>
        /// <value>
        ///     The prepare count.
        /// </value>
        internal int PrepareCount => _counters.PrepareCount;

        /// <summary>
        ///     Gets the count of compile attempts whose lambda was built (test hook).
        /// </summary>
        /// <value>
        ///     The compile count.
        /// </value>
        internal int CompileCount => _counters.CompileCount;

        /// <summary>
        ///     Gets the number of cached expressions (test hook).
        /// </summary>
        /// <value>
        ///     The cached expression count; <c>0</c> when no cache exists.
        /// </value>
        internal int CachedExpressionCount => _cache?.Count ?? 0;

        /// <summary>
        ///     Gets the number of AST nodes retained by the cache (test hook).
        /// </summary>
        /// <value>
        ///     The retained node count; <c>0</c> when no cache exists.
        /// </value>
        internal int RetainedNodeCount => _cache?.RetainedNodeCount ?? 0;

        /// <summary>
        ///     Validates each numeric option against its supported range.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown when an option is outside its supported range.
        /// </exception>
        /// <param name="options">The options to validate.</param>
        private static void ValidateOptions(MathRuleEngineOptions options)
        {
            CheckRange(nameof(options), options.MaxExpressionLength, MathRuleEngineOptions.MaxExpressionLengthCeiling,
                nameof(options.MaxExpressionLength));
            CheckRange(nameof(options), options.MaxTokens, MathRuleEngineOptions.MaxTokensCeiling,
                nameof(options.MaxTokens));
            CheckRange(nameof(options), options.MaxNodes, MathRuleEngineOptions.MaxNodesCeiling,
                nameof(options.MaxNodes));
            CheckRange(nameof(options), options.MaxDepth, MathRuleEngineOptions.MaxDepthCeiling,
                nameof(options.MaxDepth), "stack-safety ceiling for parser recursion");
            CheckRange(nameof(options), options.MaxEvaluationDepth, MathRuleEngineOptions.MaxEvaluationDepthCeiling,
                nameof(options.MaxEvaluationDepth), "stack-safety ceiling for evaluation recursion");
            CheckRange(nameof(options), options.MaxFunctionArgs, MathRuleEngineOptions.MaxFunctionArgsCeiling,
                nameof(options.MaxFunctionArgs));
            CheckRange(nameof(options), options.ExpressionCacheCapacity, 0,
                MathRuleEngineOptions.ExpressionCacheCapacityCeiling, nameof(options.ExpressionCacheCapacity),
                "memory bound for cached expressions");
            CheckRange(nameof(options), options.CompilationThreshold, 1,
                MathRuleEngineOptions.CompilationThresholdCeiling, nameof(options.CompilationThreshold));
        }

        /// <summary>
        ///     Copies only compile and cache switches from <see cref="TestOptionsOverride" />, if set.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the override returns null.</exception>
        /// <param name="options">The engine's own options clone, updated in place.</param>
        private static void ApplyTestOptionsOverride(MathRuleEngineOptions options)
        {
            var testOverride = Volatile.Read(ref TestOptionsOverride);
            if (testOverride.IsNull())
                return;

            var overridden = testOverride(options.Clone());
            DomainEnsure.IsNotNull(overridden, nameof(TestOptionsOverride),
                DefaultMessages.TestOptionsOverrideReturnedNull);

            options.EnableCompilation = overridden.EnableCompilation;
            options.CompilationThreshold = overridden.CompilationThreshold;
            options.ExpressionCacheCapacity = overridden.ExpressionCacheCapacity;
            options.PreferInterpretedExpressionTrees = overridden.PreferInterpretedExpressionTrees;
            options.ThrowOnCompileFault = overridden.ThrowOnCompileFault;
        }

        /// <summary>
        ///     Validates that an option is within <c>1..ceiling</c>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown when <paramref name="value" /> is less than <c>1</c> or greater than
        ///     <paramref name="ceiling" />.
        /// </exception>
        /// <param name="constructorParamName">
        ///     The constructor parameter name, used as the exception's <c>ParamName</c>.
        /// </param>
        /// <param name="value">The option's current value.</param>
        /// <param name="ceiling">The maximum accepted value for the option.</param>
        /// <param name="propertyName">
        ///     The name of the <see cref="MathRuleEngineOptions" /> property being validated.
        /// </param>
        /// <param name="ceilingRationale">
        ///     (Optional) a short reason for the ceiling, appended to the message.
        /// </param>
        private static void CheckRange(string constructorParamName, int value, int ceiling, string propertyName,
            string ceilingRationale = null)
        {
            CheckRange(constructorParamName, value, 1, ceiling, propertyName, ceilingRationale);
        }

        /// <summary>
        ///     Validates that an option is within <c>minimum..ceiling</c>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown when <paramref name="value" /> is less than <paramref name="minimum" /> or greater
        ///     than <paramref name="ceiling" />.
        /// </exception>
        /// <param name="constructorParamName">
        ///     The constructor parameter name, used as the exception's <c>ParamName</c>.
        /// </param>
        /// <param name="value">The option's current value.</param>
        /// <param name="minimum">The minimum accepted value for the option.</param>
        /// <param name="ceiling">The maximum accepted value for the option.</param>
        /// <param name="propertyName">
        ///     The name of the <see cref="MathRuleEngineOptions" /> property being validated.
        /// </param>
        /// <param name="ceilingRationale">
        ///     (Optional) a short reason for the ceiling, appended to the message.
        /// </param>
        private static void CheckRange(string constructorParamName, int value, int minimum, int ceiling,
            string propertyName, string ceilingRationale = null)
        {
            if (value >= minimum && value <= ceiling)
                return;

            var rationale = ceilingRationale.IsNull() ? string.Empty : $" ({ceilingRationale})";

            throw new ArgumentOutOfRangeException(
                constructorParamName,
                value,
                $"{propertyName} must be between {minimum} and {ceiling}{rationale}.");
        }

        /// <summary>
        ///     Peeks at a cached program without side effects (test hook).
        /// </summary>
        /// <param name="expression">The raw expression text.</param>
        /// <param name="program">[out] The cached program, or null when not cached.</param>
        /// <returns>
        ///     True when a cached program was found.
        /// </returns>
        internal bool TryPeekCachedProgram(string expression, out ExpressionProgram program)
        {
            if (_cache.IsNotNull())
                return _cache.TryPeek(expression, out program);

            program = null;

            return false;
        }

        /// <summary>
        ///     Register a custom function available to expressions.
        /// </summary>
        /// <exception cref="ArgumentException">.</exception>
        /// <exception cref="ArgumentNullException">.</exception>
        /// <param name="name">The name.</param>
        /// <param name="impl">The function implementation.</param>
        public void RegisterFunction(string name, Func<double[], double> impl)
        {
            DomainEnsure.IsNotNullOrEmpty(name, nameof(name), DefaultMessages.FuncNameIsMissing);
            DomainEnsure.IsNotNull(impl, nameof(impl));

            _functions[name] = impl;
        }

        /// <summary>
        ///     Register a custom functions available to expressions.
        /// </summary>
        /// <param name="functions">The functions.</param>
        public void RegisterFunctions(IDictionary<string, Func<double[], double>> functions)
        {
            functions
                .NotNull()
                .ForEach(func => { RegisterFunction(func.Key, func.Value); });
        }

        /// <summary>
        ///     Parses and, when possible, compiles the expression, deferring runtime errors.
        /// </summary>
        /// <exception cref="ArgumentException">
        ///     Thrown when <paramref name="expression" /> is null, empty or white space.
        /// </exception>
        /// <exception cref="MathRuleEngineException">
        ///     Thrown when a resource limit is exceeded or a token is invalid.
        /// </exception>
        /// <exception cref="ExpressionSyntaxException">
        ///     Thrown when the expression is not syntactically valid.
        /// </exception>
        /// <param name="expression">The expression.</param>
        /// <returns>
        ///     A reusable handle; interpreted when compiling is disabled, impossible or briefly deferred.
        /// </returns>
        public CompiledExpression Compile(string expression)
        {
            ValidateExpression(expression);

            if (_cache.IsNull() || !_cache.TryPeek(expression, out var program))
                program = Prepare(expression);

            if (_options.EnableCompilation)
                program.TryCompile(true);

            return new CompiledExpression(expression, program);
        }

        /// <summary>
        ///     Evaluate the math expression. When the expression cache is enabled, the parsed expression
        ///     is cached on first use and, unless compilation is disabled, compiled once it reaches
        ///     <see cref="MathRuleEngineOptions.CompilationThreshold" /> uses; results are identical on
        ///     every path.
        /// </summary>
        /// <exception cref="ExpressionSyntaxException">.</exception>
        /// <exception cref="ArgumentException">.</exception>
        /// <param name="expression">The expression.</param>
        /// <param name="parameters">
        ///     (Optional)
        ///     (Immutable) the parameters (variables).
        /// </param>
        /// <returns>
        ///     A expression evaluation result (double value).
        /// </returns>
        public double Evaluate(string expression, IDictionary<string, object> parameters = null)
        {
            ValidateExpression(expression);

            if (_cache.IsNull())
                return EvaluateUncached(expression, parameters);

            var program = _cache.GetOrAdd(expression, _prepare);

            program.TryTierUp();

            return program.Evaluate(parameters);
        }

        /// <summary>
        ///     Validates the expression text before any cache lookup or parse.
        /// </summary>
        /// <exception cref="MathRuleEngineException">
        ///     Thrown when the expression is longer than <see cref="MathRuleEngineOptions.MaxExpressionLength" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when <paramref name="expression" /> is null, empty or white space.
        /// </exception>
        /// <param name="expression">The expression.</param>
        private void ValidateExpression(string expression)
        {
            DomainEnsure.IsNotNullOrEmpty(expression, nameof(expression), DefaultMessages.ExpressionIsRequired);

            if (expression.Length > _options.MaxExpressionLength)
                throw new MathRuleEngineException(
                    DefaultMessages.ExpressionTooLong.FormatWith(expression.Length, _options.MaxExpressionLength));
        }

        /// <summary>
        ///     Counts the attempt, then tokenizes and parses the expression.
        /// </summary>
        /// <exception cref="MathRuleEngineException">
        ///     Thrown when a resource limit is exceeded or a token is invalid.
        /// </exception>
        /// <exception cref="ExpressionSyntaxException">
        ///     Thrown when the expression is not syntactically valid.
        /// </exception>
        /// <param name="expression">The validated expression.</param>
        /// <returns>
        ///     A new, uncompiled program bound to this engine.
        /// </returns>
        private ExpressionProgram Prepare(string expression)
        {
            _counters.OnPrepare();

            var ast = Parse(expression, out var nodeCount);

            return new ExpressionProgram(ast, nodeCount, _functions, _options, _counters);
        }

        /// <summary>
        ///     Tokenizes and parses the expression, rejecting leftover tokens.
        /// </summary>
        /// <exception cref="ExpressionSyntaxException">
        ///     Thrown when the expression is not syntactically valid.
        /// </exception>
        /// <exception cref="MathRuleEngineException">
        ///     Thrown when a resource limit is exceeded or a token is invalid.
        /// </exception>
        /// <param name="expression">The validated expression.</param>
        /// <param name="nodeCount">[out] The number of AST nodes produced.</param>
        /// <returns>
        ///     The root of the parsed AST.
        /// </returns>
        private NodeBase Parse(string expression, out int nodeCount)
        {
            // Tokenize
            var tokens = ExpressionTokenizer.Tokenize(expression, _options);

            // Parse
            var parser = new ExpressionParser(tokens, _options);
            var ast = parser.ParseExpression();

            // Ensure we consumed all tokens (except EndOfText)
            var next = tokens[parser.Pos];

            if (next.Kind != TokenKind.EndOfText)
                throw new ExpressionSyntaxException(DefaultMessages.UnexceptedTokenAtTheEnd.FormatWith(next.Text),
                    next.Column);

            nodeCount = parser.NodeCount;

            return ast;
        }

        /// <summary>
        ///     Parses and interprets the expression when <see cref="MathRuleEngineOptions.ExpressionCacheCapacity" /> is <c>0</c>.
        /// </summary>
        /// <param name="expression">The validated expression.</param>
        /// <param name="parameters">The parameters (variables).</param>
        /// <returns>
        ///     The evaluation result.
        /// </returns>
        private double EvaluateUncached(string expression, IDictionary<string, object> parameters)
        {
            var ast = Parse(expression, out _);

            var ctx = new EvaluationContext(_functions, parameters, _options);

            var depthSnapshot = EvaluationContext.CurrentDepth;
            try
            {
                return ast.Evaluate(ctx);
            }
            finally
            {
                EvaluationContext.RestoreDepth(depthSnapshot);
            }
        }
    }
}