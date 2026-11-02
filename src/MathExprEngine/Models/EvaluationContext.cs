// ***********************************************************************
//  Assembly          : RzR.Shared.Entity.MathExprEngine
//  Author            : RzR
//  Created On        : 2026-10-22 20:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-10-31 15:37
//  ***********************************************************************
//  <copyright file="EvaluationContext.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DomainCommonExtensions.ArraysExtensions;
using DomainCommonExtensions.DataTypeExtensions;
using MathExprEngine.Exceptions;
using MathExprEngine.Helpers;

#endregion

namespace MathExprEngine.Models
{
    /// <summary>
    ///     The per-call evaluation state, with a per-thread depth budget.
    /// </summary>
    internal sealed class EvaluationContext
    {
        /// <summary>
        ///     (Immutable) the depth mask that probes the stack every 8th level.
        /// </summary>
        private const int StackProbeMask = 7;

        /// <summary>
        ///     The thread's evaluation depth, accumulated across re-entrant calls.
        /// </summary>
        [ThreadStatic] private static int _depth;

        /// <summary>
        ///     (Immutable) the shared empty parameters, used when none are supplied.
        /// </summary>
        private static readonly IReadOnlyDictionary<string, object> EmptyParameters =
            new Dictionary<string, object>(0, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     Initializes a new instance of the <see cref="EvaluationContext" /> class.
        /// </summary>
        /// <exception cref="MathRuleEngineException">
        ///     Thrown when copying <paramref name="parameters" /> fails or the stack runs low.
        /// </exception>
        /// <param name="functions">The engine's live function table, stored by reference.</param>
        /// <param name="parameters">
        ///     (Optional) the caller's parameters, copied unsynchronized into a case-insensitive map.
        /// </param>
        /// <param name="options">The frozen options snapshot owned by the engine.</param>
        public EvaluationContext(IDictionary<string, Func<double[], double>> functions,
            IDictionary<string, object> parameters, MathRuleEngineOptions options)
        {
            Functions = functions;
            Options = options;

            try
            {
                if (parameters.IsNullOrEmptyEnumerable())
                {
                    Parameters = EmptyParameters;
                }
                else
                {
                    var copy = new Dictionary<string, object>(parameters.Count, StringComparer.OrdinalIgnoreCase);
                    foreach (var pair in parameters)
                        copy[pair.Key] = pair.Value;

                    Parameters = copy;
                }
            }
            catch (Exception ex)
            {
                throw NodeSemantics.ParametersModifiedDuringCopy(ex);
            }

            if (_depth < options.MaxEvaluationDepth)
                EnsureSufficientStack(_depth + 1);
        }

        /// <summary>
        ///     Gets the engine's live function table.
        /// </summary>
        /// <value>
        ///     The functions.
        /// </value>
        public IDictionary<string, Func<double[], double>> Functions { get; }

        /// <summary>
        ///     Gets the read-only, case-insensitive copy of the caller's parameters.
        /// </summary>
        /// <value>
        ///     The parameters.
        /// </value>
        public IReadOnlyDictionary<string, object> Parameters { get; }

        /// <summary>
        ///     Gets the frozen options snapshot for this call.
        /// </summary>
        /// <value>
        ///     The options.
        /// </value>
        public MathRuleEngineOptions Options { get; }

        /// <summary>
        ///     Gets the calling thread's current evaluation depth.
        /// </summary>
        /// <value>
        ///     The current depth for this thread.
        /// </value>
        internal static int CurrentDepth => _depth;

        /// <summary>
        ///     Enters a node, to be paired with <see cref="ExitNode" /> in <c>finally</c>.
        /// </summary>
        /// <exception cref="MathRuleEngineException">
        ///     Thrown when <see cref="MathRuleEngineOptions.MaxEvaluationDepth" /> is exceeded or the stack runs low.
        /// </exception>
        public void EnterNode()
        {
            if (++_depth > Options.MaxEvaluationDepth)
            {
                _depth--;

                throw new MathRuleEngineException(
                    DefaultMessages.EvaluationDepthExceeded.FormatWith(Options.MaxEvaluationDepth));
            }

            if ((_depth & StackProbeMask) == 0 && !HasSufficientStack())
            {
                var depth = _depth;
                _depth--;

                throw StackExhausted(depth);
            }
        }

        /// <summary>
        ///     Ensures enough stack remains to evaluate at <paramref name="depth" />.
        /// </summary>
        /// <exception cref="MathRuleEngineException">
        ///     Thrown when the stack runs low.
        /// </exception>
        /// <param name="depth">The evaluation depth being entered, reported in the message.</param>
        internal static void EnsureSufficientStack(int depth)
        {
            if (!HasSufficientStack())
                throw StackExhausted(depth);
        }

        /// <summary>
        ///     Checks whether enough stack remains for another evaluation step.
        /// </summary>
        /// <returns>
        ///     True when enough stack remains.
        /// </returns>
        private static bool HasSufficientStack()
        {
#if NETSTANDARD2_1
            return RuntimeHelpers.TryEnsureSufficientExecutionStack();
#else
            try
            {
                RuntimeHelpers.EnsureSufficientExecutionStack();

                return true;
            }
            catch (InsufficientExecutionStackException)
            {
                return false;
            }
#endif
        }

        /// <summary>
        ///     Creates the exception for an exhausted thread stack.
        /// </summary>
        /// <param name="depth">The evaluation depth being entered.</param>
        /// <returns>
        ///     The exception to throw.
        /// </returns>
        private static MathRuleEngineException StackExhausted(int depth)
        {
            return new MathRuleEngineException(DefaultMessages.EvaluationStackExhausted.FormatWith(depth));
        }

        /// <summary>
        ///     Exits a node evaluation, decrementing the thread-local depth counter.
        /// </summary>
        public void ExitNode()
        {
            _depth--;
        }

        /// <summary>
        ///     Restores the thread's depth to a captured, possibly non-zero, value.
        /// </summary>
        /// <param name="depth">The depth value previously read from <see cref="CurrentDepth" />.</param>
        internal static void RestoreDepth(int depth)
        {
            _depth = depth;
        }

        /// <summary>
        ///     Ensures the given value is finite, unless <see cref="MathRuleEngineOptions.AllowNonFiniteResults" />
        ///     is set.
        /// </summary>
        /// <exception cref="MathRuleEngineException">
        ///     Thrown when the value is <c>NaN</c> or <c>Infinity</c>.
        /// </exception>
        /// <param name="value">The value to validate.</param>
        /// <param name="column">
        ///     The producing node's column, for error reporting.
        /// </param>
        public void EnsureFinite(double value, int column)
        {
            if (Options.AllowNonFiniteResults)
                return;

            NodeSemantics.EnsureFinite(value, column);
        }
    }
}