// ***********************************************************************
//  Assembly          : RzR.Shared.Entity.MathExprEngine
//  Author            : RzR
//  Created On        : 2026-10-22 20:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-10-31 15:39
//  ***********************************************************************
//  <copyright file="CompilationScope.cs" company="RzR SOFT & TECH">
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
using RzR.Extensions.Domain.Text;
using RzR.Extensions.Domain.Validation;
using MathExprEngine.Exceptions;
using MathExprEngine.Helpers;

#endregion

namespace MathExprEngine.Compilation
{
    /// <summary>
    ///     The single-threaded state of one lambda build: depth, height, slots.
    /// </summary>
    internal sealed class CompilationScope
    {
        /// <summary>
        ///     (Immutable) the maximum static depth that may be compiled.
        /// </summary>
        private readonly int _heightCap;

        /// <summary>
        ///     (Immutable) the case-insensitive slot index of each parameter name.
        /// </summary>
        private readonly Dictionary<string, int> _slotIndex = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     (Immutable) the parameter name of each slot, in slot order.
        /// </summary>
        private readonly List<string> _slotNames = new();

        /// <summary>
        ///     Initializes a new instance of the <see cref="CompilationScope" /> class.
        /// </summary>
        /// <param name="options">The engine's frozen options snapshot.</param>
        internal CompilationScope(MathRuleEngineOptions options)
        {
            DomainEnsure.IsNotNull(options, nameof(options));

            AllowNonFiniteResults = options.AllowNonFiniteResults;
            _heightCap = Math.Min(ExpressionCompiler.MaxCompileHeight, options.MaxEvaluationDepth);

            Functions = Expression.Parameter(typeof(IDictionary<string, Func<double[], double>>), "functions");
            Slots = Expression.Parameter(typeof(object[]), "slots");
            BaseDepth = Expression.Parameter(typeof(int), "baseDepth");
        }

        /// <summary>
        ///     Gets the lambda parameter carrying the engine's live function registry.
        /// </summary>
        /// <value>
        ///     The functions parameter.
        /// </value>
        internal ParameterExpression Functions { get; }

        /// <summary>
        ///     Gets the lambda parameter carrying the per-call parameter slots.
        /// </summary>
        /// <value>
        ///     The slots parameter.
        /// </value>
        internal ParameterExpression Slots { get; }

        /// <summary>
        ///     Gets the lambda parameter carrying the call's starting evaluation depth.
        /// </summary>
        /// <value>
        ///     The base depth parameter.
        /// </value>
        internal ParameterExpression BaseDepth { get; }

        /// <summary>
        ///     Gets a value indicating whether per-node finite checks are omitted.
        /// </summary>
        /// <value>
        ///     The frozen <see cref="MathRuleEngineOptions.AllowNonFiniteResults" /> value.
        /// </value>
        internal bool AllowNonFiniteResults { get; }

        /// <summary>
        ///     Gets the current node's static depth (root is <c>1</c>).
        /// </summary>
        /// <value>
        ///     The current depth.
        /// </value>
        internal int CurrentDepth { get; private set; }

        /// <summary>
        ///     Gets the deepest static depth entered so far.
        /// </summary>
        /// <value>
        ///     The height.
        /// </value>
        internal int Height { get; private set; }

        /// <summary>
        ///     Enters a node, tracking height, paired with <see cref="ExitNode" /> in <c>finally</c>.
        /// </summary>
        /// <exception cref="CompilationAbortedException">
        ///     Thrown when the depth exceeds the effective compile-height cap.
        /// </exception>
        /// <exception cref="InsufficientExecutionStackException">
        ///     Thrown when the stack runs low.
        /// </exception>
        internal void EnterNode()
        {
            RuntimeHelpers.EnsureSufficientExecutionStack();

            if (++CurrentDepth > _heightCap)
            {
                CurrentDepth--;

                throw new CompilationAbortedException(DefaultMessages.CompileHeightExceeded.FormatWith(_heightCap));
            }

            if (CurrentDepth > Height)
                Height = CurrentDepth;
        }

        /// <summary>
        ///     Exits a node.
        /// </summary>
        internal void ExitNode()
        {
            CurrentDepth--;
        }

        /// <summary>
        ///     Gets a parameter's slot index, allocating one on first use.
        /// </summary>
        /// <param name="name">The parameter name as written in the expression.</param>
        /// <returns>
        ///     The slot index.
        /// </returns>
        internal int GetSlot(string name)
        {
            if (_slotIndex.TryGetValue(name, out var index))
                return index;

            index = _slotNames.Count;
            _slotIndex.Add(name, index);
            _slotNames.Add(name);

            return index;
        }

        /// <summary>
        ///     Creates the binder for the slots allocated during this build.
        /// </summary>
        /// <returns>
        ///     The parameter binder.
        /// </returns>
        internal ParameterBinder CreateBinder()
        {
            return new ParameterBinder(_slotNames.ToArray());
        }
    }
}