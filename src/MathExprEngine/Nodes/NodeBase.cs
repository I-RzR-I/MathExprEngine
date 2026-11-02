// ***********************************************************************
//  Assembly          : RzR.Shared.Entity.MathExprEngine
//  Author            : RzR
//  Created On        : 2026-10-22 20:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-10-31 15:37
//  ***********************************************************************
//  <copyright file="NodeBase.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using System.Linq.Expressions;
using MathExprEngine.Compilation;
using MathExprEngine.Models;

#endregion

namespace MathExprEngine.Nodes
{
    /// <summary>
    ///     A node base.
    /// </summary>
    internal abstract class NodeBase
    {
        /// <summary>
        ///     The column idx. For error reporting.
        /// </summary>
        public int Column;

        /// <summary>
        ///     Gets a value indicating whether finite operands guarantee a finite result.
        /// </summary>
        /// <value>
        ///     True to skip the compiled finite check.
        /// </value>
        protected virtual bool IsFiniteWhenOperandsFinite => false;

        /// <summary>
        ///     Evaluates this node with depth and finiteness checks.
        /// </summary>
        /// <param name="ctx">The evaluation context.</param>
        /// <returns>
        ///     The node's value.
        /// </returns>
        public double Evaluate(EvaluationContext ctx)
        {
            ctx.EnterNode();
            try
            {
                var value = EvaluateCore(ctx);
                ctx.EnsureFinite(value, Column);

                return value;
            }
            finally
            {
                ctx.ExitNode();
            }
        }

        /// <summary>
        ///     Evaluates this node, leaving depth and finiteness checks to <see cref="Evaluate" />.
        /// </summary>
        /// <param name="ctx">The evaluation context.</param>
        /// <returns>
        ///     The node's value.
        /// </returns>
        protected abstract double EvaluateCore(EvaluationContext ctx);

        /// <summary>
        ///     Builds this node's expression tree with depth, height-cap and finiteness checks.
        /// </summary>
        /// <param name="scope">The build scope.</param>
        /// <returns>
        ///     An expression of type <see cref="double" />.
        /// </returns>
        internal Expression Compile(CompilationScope scope)
        {
            scope.EnterNode();
            try
            {
                var body = CompileCore(scope);
                if (scope.AllowNonFiniteResults || IsFiniteWhenOperandsFinite)
                    return body;

                return Expression.Call(CompiledRuntime.EnsureFiniteMethod, body, Expression.Constant(Column));
            }
            finally
            {
                scope.ExitNode();
            }
        }

        /// <summary>
        ///     Builds this node's expression tree without folding, pre-resolving, or checking depth and finiteness.
        /// </summary>
        /// <param name="scope">The build scope.</param>
        /// <returns>
        ///     An expression of type <see cref="double" />.
        /// </returns>
        protected abstract Expression CompileCore(CompilationScope scope);
    }
}