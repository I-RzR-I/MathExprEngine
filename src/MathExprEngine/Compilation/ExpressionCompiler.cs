// ***********************************************************************
//  Assembly          : RzR.Shared.Entity.MathExprEngine
//  Author            : RzR
//  Created On        : 2026-10-22 20:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-10-31 15:39
//  ***********************************************************************
//  <copyright file="ExpressionCompiler.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using System.Linq.Expressions;
using RzR.Extensions.Domain.Validation;
using MathExprEngine.Exceptions;
using MathExprEngine.Nodes;

#if NETSTANDARD2_1

using System.Runtime.CompilerServices;

#endif

#endregion

namespace MathExprEngine.Compilation
{
    /// <summary>
    ///     The AST-to-lambda compiler that never evaluates or folds constants.
    /// </summary>
    internal static class ExpressionCompiler
    {
        /// <summary>
        ///     (Immutable) the maximum compiled AST height, also capped by
        ///     <see cref="MathRuleEngineOptions.MaxEvaluationDepth" />.
        /// </summary>
        internal const int MaxCompileHeight = 256;

        /// <summary>
        ///     (Immutable) the maximum node count for automatic tier-up, not explicit compiles.
        /// </summary>
        internal const int MaxTierUpNodes = 4096;

        /// <summary>
        ///     Builds, without compiling, the lambda for <paramref name="ast" />.
        /// </summary>
        /// <exception cref="CompilationAbortedException">
        ///     Thrown when the AST is taller than the effective compile-height cap.
        /// </exception>
        /// <exception cref="System.InsufficientExecutionStackException">
        ///     Thrown when the stack runs low while building.
        /// </exception>
        /// <param name="ast">The root of the parsed AST.</param>
        /// <param name="options">The engine's frozen options snapshot.</param>
        /// <param name="height">[out] The static AST height of the built lambda.</param>
        /// <param name="binder">[out] The binder for the lambda's parameter slots.</param>
        /// <returns>
        ///     The lambda, taking (functions, slots, baseDepth).
        /// </returns>
        internal static Expression<CompiledEvaluator> BuildLambda(NodeBase ast, MathRuleEngineOptions options,
            out int height, out ParameterBinder binder)
        {
            DomainEnsure.IsNotNull(ast, nameof(ast));
            DomainEnsure.IsNotNull(options, nameof(options));

            var scope = new CompilationScope(options);
            var body = ast.Compile(scope);

            height = scope.Height;
            binder = scope.CreateBinder();

            return Expression.Lambda<CompiledEvaluator>(
                body, scope.Functions, scope.Slots, scope.BaseDepth);
        }

        /// <summary>
        ///     Checks whether compiled delegates can run as generated code.
        /// </summary>
        /// <param name="options">The engine's frozen options snapshot.</param>
        /// <returns>
        ///     False when dynamic code is unavailable or simulated as unavailable.
        /// </returns>
        internal static bool IsDynamicCodeAvailable(MathRuleEngineOptions options)
        {
            if (options.SimulateNoDynamicCode)
                return false;

#if NETSTANDARD2_1
            return RuntimeFeature.IsDynamicCodeCompiled;
#else
            return true;
#endif
        }

        /// <summary>
        ///     Compiles a built lambda to a delegate.
        /// </summary>
        /// <param name="lambda">The lambda to compile.</param>
        /// <param name="preferInterpretation">
        ///     True to use the LINQ interpreter, on netstandard2.1 only.
        /// </param>
        /// <returns>
        ///     The compiled delegate.
        /// </returns>
        internal static CompiledEvaluator CompileLambda(Expression<CompiledEvaluator> lambda,
            bool preferInterpretation)
        {
#if NETSTANDARD2_1
            if (preferInterpretation)
                return CompileInterpreted(lambda);
#endif

            return lambda.Compile();
        }

#if NETSTANDARD2_1

        /// <summary>
        ///     Compiles a lambda with <c>preferInterpretation: true</c>.
        /// </summary>
        /// <param name="lambda">The lambda to compile.</param>
        /// <returns>
        ///     The interpreted delegate.
        /// </returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static CompiledEvaluator CompileInterpreted(
            Expression<CompiledEvaluator> lambda)
        {
            return lambda.Compile(preferInterpretation: true);
        }
#endif
    }
}