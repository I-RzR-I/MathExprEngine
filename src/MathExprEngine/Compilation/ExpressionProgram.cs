// ***********************************************************************
//  Assembly          : RzR.Shared.Entity.MathExprEngine
//  Author            : RzR
//  Created On        : 2026-10-22 20:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-10-31 15:39
//  ***********************************************************************
//  <copyright file="ExpressionProgram.cs" company="RzR SOFT & TECH">
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
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using RzR.Extensions.Domain.Primitives;
using RzR.Extensions.Domain.Validation;
using MathExprEngine.Enums;
using MathExprEngine.Exceptions;
using MathExprEngine.Models;
using MathExprEngine.Nodes;

#endregion

namespace MathExprEngine.Compilation
{
    /// <summary>
    ///     A thread-safe, re-entrant parsed expression that runs interpreted or compiled.
    /// </summary>
    internal sealed class ExpressionProgram
    {
        /// <summary>
        ///     (Immutable) the divisor of <see cref="MathRuleEngineOptions.MaxEvaluationDepth" /> that caps the
        ///     depth for compiling.
        /// </summary>
        internal const int CompileHeadroomDivisor = 2;

        /// <summary>
        ///     (Immutable) the parsed AST root, kept for the interpreted path.
        /// </summary>
        private readonly NodeBase _ast;

        /// <summary>
        ///     (Immutable) the engine-owned counter sink.
        /// </summary>
        private readonly CompilationCounters _counters;

        /// <summary>
        ///     (Immutable) the engine's live function registry, resolved late on every call.
        /// </summary>
        private readonly IDictionary<string, Func<double[], double>> _functions;

        /// <summary>
        ///     (Immutable) the engine's frozen options snapshot.
        /// </summary>
        private readonly MathRuleEngineOptions _options;

        /// <summary>
        ///     (Immutable) the compile lock, never held while evaluating.
        /// </summary>
        private readonly object _sync = new();

        /// <summary>
        ///     The published compiled state, null until compiled.
        /// </summary>
        private CompiledState _compiled;

        /// <summary>
        ///     The last stored <see cref="CompileOutcome" /> as an int, accessed through <see cref="Volatile" />.
        /// </summary>
        private int _lastOutcome;

        /// <summary>
        ///     The <see cref="CompileState" /> as an int, accessed through <see cref="Volatile" />.
        /// </summary>
        private int _state;

        /// <summary>
        ///     The saturating use counter consulted by <see cref="TryTierUp" />.
        /// </summary>
        private int _uses;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ExpressionProgram" /> class.
        /// </summary>
        /// <param name="ast">The root of the parsed AST.</param>
        /// <param name="nodeCount">The number of AST nodes the parser produced.</param>
        /// <param name="functions">The engine's live function registry.</param>
        /// <param name="options">The engine's frozen options snapshot.</param>
        /// <param name="counters">The engine-owned counter sink.</param>
        internal ExpressionProgram(NodeBase ast, int nodeCount, IDictionary<string, Func<double[], double>> functions,
            MathRuleEngineOptions options, CompilationCounters counters)
        {
            DomainEnsure.IsNotNull(ast, nameof(ast));
            DomainEnsure.IsNotNull(functions, nameof(functions));
            DomainEnsure.IsNotNull(options, nameof(options));
            DomainEnsure.IsNotNull(counters, nameof(counters));

            _ast = ast;
            NodeCount = nodeCount;
            _functions = functions;
            _options = options;
            _counters = counters;
        }

        /// <summary>
        ///     Gets the AST node count for tier-up and cache budgets.
        /// </summary>
        /// <value>
        ///     The node count.
        /// </value>
        internal int NodeCount { get; }

        /// <summary>
        ///     Gets the current compile state (test hook).
        /// </summary>
        /// <value>
        ///     The state.
        /// </value>
        internal CompileState State => (CompileState)Volatile.Read(ref _state);

        /// <summary>
        ///     Gets a value indicating whether tier-up is still possible.
        /// </summary>
        /// <value>
        ///     True while the state is <see cref="CompileState.NotCompiled" />.
        /// </value>
        internal bool CanTierUp => State == CompileState.NotCompiled;

        /// <summary>
        ///     Gets a value indicating whether the compiled delegate is published.
        /// </summary>
        /// <value>
        ///     True when the state is <see cref="CompileState.Compiled" />.
        /// </value>
        internal bool IsCompiled => State == CompileState.Compiled;

        /// <summary>
        ///     Gets the last stored compile outcome, never <see cref="CompileOutcome.Busy" /> (test hook).
        /// </summary>
        /// <value>
        ///     The last outcome.
        /// </value>
        internal CompileOutcome LastOutcome => (CompileOutcome)Volatile.Read(ref _lastOutcome);

        /// <summary>
        ///     Gets the use count, saturating at <see cref="MathRuleEngineOptions.CompilationThreshold" /> (test hook).
        /// </summary>
        /// <value>
        ///     The use count.
        /// </value>
        internal int UseCount => Volatile.Read(ref _uses);

        /// <summary>
        ///     Gets the published compiled state (test hook).
        /// </summary>
        /// <value>
        ///     The compiled state, or null unless the program is compiled.
        /// </value>
        internal CompiledState Compiled => Volatile.Read(ref _compiled);

        /// <summary>
        ///     Records one use, saturating at <see cref="MathRuleEngineOptions.CompilationThreshold" />.
        /// </summary>
        /// <returns>
        ///     The use count after this call.
        /// </returns>
        internal int RecordUse()
        {
            var threshold = _options.CompilationThreshold;
            int current;

            do
            {
                current = Volatile.Read(ref _uses);
                if (current >= threshold)
                    return current;
            } while (Interlocked.CompareExchange(ref _uses, current + 1, current) != current);

            return current + 1;
        }

        /// <summary>
        ///     Resets the use counter after <see cref="CompileOutcome.InsufficientStack" />.
        /// </summary>
        internal void ResetUses()
        {
            Volatile.Write(ref _uses, 0);
        }

        /// <summary>
        ///     Compiles without blocking once tier-up guards and use threshold pass.
        /// </summary>
        /// <returns>
        ///     The outcome of this call; <see cref="CompileOutcome.None" /> when a guard declined.
        /// </returns>
        internal CompileOutcome TryTierUp()
        {
            if (!_options.EnableCompilation || !CanTierUp)
                return CompileOutcome.None;

            if (NodeCount > ExpressionCompiler.MaxTierUpNodes)
                return CompileOutcome.None;

            if (!HasCompileHeadroom())
                return CompileOutcome.None;

            if (RecordUse() < _options.CompilationThreshold)
                return CompileOutcome.None;

            var outcome = TryCompile(false);
            if (outcome == CompileOutcome.InsufficientStack)
                ResetUses();

            return outcome;
        }

        /// <summary>
        ///     Compiles the program under its lock, at most once.
        /// </summary>
        /// <exception cref="Exception">
        ///     Rethrown on a compile fault when <see cref="MathRuleEngineOptions.ThrowOnCompileFault" /> is set.
        /// </exception>
        /// <param name="wait">
        ///     True to block on the lock, false to skip when busy.
        /// </param>
        /// <returns>
        ///     <see cref="CompileOutcome.Busy" /> if not waiting and busy; otherwise the stored or new outcome.
        /// </returns>
        internal CompileOutcome TryCompile(bool wait)
        {
            var taken = false;
            try
            {
                if (wait)
                    Monitor.Enter(_sync, ref taken);
                else
                    Monitor.TryEnter(_sync, ref taken);

                if (!taken)
                    return CompileOutcome.Busy;

                if (State != CompileState.NotCompiled)
                    return LastOutcome;

                return CompileUnderLock();
            }
            finally
            {
                if (taken)
                    Monitor.Exit(_sync);
            }
        }

        /// <summary>
        ///     Compiles from <see cref="CompileState.NotCompiled" /> under <see cref="_sync" />, rethrowing faults
        ///     only if <see cref="MathRuleEngineOptions.ThrowOnCompileFault" /> is set.
        /// </summary>
        /// <returns>
        ///     The outcome of this attempt.
        /// </returns>
        private CompileOutcome CompileUnderLock()
        {
            if (!ExpressionCompiler.IsDynamicCodeAvailable(_options))
                return Fail(CompileOutcome.NoDynamicCode);

            if (!HasCompileHeadroom())
                return Transient(CompileOutcome.InsufficientStack);

            try
            {
                var lambda = ExpressionCompiler.BuildLambda(_ast, _options, out var height, out var binder);

                _counters.OnCompile();
                _options.CompileFaultInjector?.Invoke(lambda);

                RuntimeHelpers.EnsureSufficientExecutionStack();
                var compiled = ExpressionCompiler.CompileLambda(lambda, _options.PreferInterpretedExpressionTrees);

                Volatile.Write(ref _compiled, new CompiledState(compiled, height, binder));
                Volatile.Write(ref _lastOutcome, (int)CompileOutcome.Compiled);
                Volatile.Write(ref _state, (int)CompileState.Compiled);

                return CompileOutcome.Compiled;
            }
            catch (CompilationAbortedException)
            {
                return Fail(CompileOutcome.HeightCapExceeded);
            }
            catch (InsufficientExecutionStackException)
            {
                return Transient(CompileOutcome.InsufficientStack);
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException) && !(ex is ThreadAbortException))
            {
                Fail(CompileOutcome.CompileFault);

                if (_options.ThrowOnCompileFault)
                    throw;

                return CompileOutcome.CompileFault;
            }
        }

        /// <summary>
        ///     Marks the program permanently <see cref="CompileState.Failed" />, so it always interprets.
        /// </summary>
        /// <param name="outcome">The outcome to store as the last outcome.</param>
        /// <returns>
        ///     The same <paramref name="outcome" />.
        /// </returns>
        private CompileOutcome Fail(CompileOutcome outcome)
        {
            Volatile.Write(ref _lastOutcome, (int)outcome);
            Volatile.Write(ref _state, (int)CompileState.Failed);

            return outcome;
        }

        /// <summary>
        ///     Stores a transient outcome, leaving the program <see cref="CompileState.NotCompiled" /> to retry.
        /// </summary>
        /// <param name="outcome">The outcome to store as the last outcome.</param>
        /// <returns>
        ///     The same <paramref name="outcome" />.
        /// </returns>
        private CompileOutcome Transient(CompileOutcome outcome)
        {
            Volatile.Write(ref _lastOutcome, (int)outcome);

            return outcome;
        }

        /// <summary>
        ///     Checks whether the thread's re-entrant depth leaves room to compile.
        /// </summary>
        /// <returns>
        ///     True when the thread's current evaluation depth leaves enough headroom to compile.
        /// </returns>
        private bool HasCompileHeadroom()
        {
            return EvaluationContext.CurrentDepth <= _options.MaxEvaluationDepth / CompileHeadroomDivisor;
        }

        /// <summary>
        ///     Checks whether <see cref="Evaluate" /> would use the compiled delegate (test hook).
        /// </summary>
        /// <param name="baseDepth">The evaluation depth the call would start from.</param>
        /// <returns>
        ///     True when the compiled delegate would be used.
        /// </returns>
        internal bool WouldUseDelegate(int baseDepth)
        {
            return CanRunCompiled(Volatile.Read(ref _compiled), baseDepth, _options.MaxEvaluationDepth);
        }

        /// <summary>
        ///     Builds, without compiling, the lambda for this program's AST (test hook).
        /// </summary>
        /// <returns>
        ///     The lambda expression.
        /// </returns>
        internal LambdaExpression BuildLambda()
        {
            return ExpressionCompiler.BuildLambda(_ast, _options, out _, out _);
        }

        /// <summary>
        ///     Evaluates, compiled if depth allows, restoring the caller's re-entrant depth.
        /// </summary>
        /// <param name="parameters">The caller-supplied parameters.</param>
        /// <returns>
        ///     The evaluation result.
        /// </returns>
        internal double Evaluate(IDictionary<string, object> parameters)
        {
            var baseDepth = EvaluationContext.CurrentDepth;
            try
            {
                var compiled = Volatile.Read(ref _compiled);
                if (CanRunCompiled(compiled, baseDepth, _options.MaxEvaluationDepth))
                {
                    var slots = compiled.Binder.Bind(parameters);
                    EvaluationContext.EnsureSufficientStack(baseDepth + 1);

                    return compiled.Delegate(_functions, slots, baseDepth);
                }

                return _ast.Evaluate(new EvaluationContext(_functions, parameters, _options));
            }
            finally
            {
                EvaluationContext.RestoreDepth(baseDepth);
            }
        }

        /// <summary>
        ///     Checks whether a compiled delegate exists and fits the budget.
        /// </summary>
        /// <param name="compiled">The published compiled state, or null.</param>
        /// <param name="baseDepth">The evaluation depth the call starts from.</param>
        /// <param name="maxEvaluationDepth">The configured evaluation-depth budget.</param>
        /// <returns>
        ///     True when the compiled delegate may run.
        /// </returns>
        private static bool CanRunCompiled(CompiledState compiled, int baseDepth, int maxEvaluationDepth)
        {
            return compiled.IsNotNull() && baseDepth <= maxEvaluationDepth - compiled.Height;
        }
    }
}