// ***********************************************************************
//  Assembly          : RzR.Shared.Entity.MathExprEngine
//  Author            : RzR
//  Created On        : 2026-10-22 20:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-10-31 15:37
//  ***********************************************************************
//  <copyright file="ConditionalNode.cs" company="RzR SOFT & TECH">
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
using MathExprEngine.Helpers;
using MathExprEngine.Models;

#endregion

namespace MathExprEngine.Nodes
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     A conditional node.
    /// </summary>
    /// <seealso cref="T:MathExprEngine.Nodes.NodeBase" />
    /// =================================================================================================
    internal sealed class ConditionalNode : NodeBase
    {
        // condition ? ifTrue : ifFalse
        private readonly NodeBase _condition, _ifTrue, _ifFalse;

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Initializes a new instance of the <see cref="ConditionalNode" /> class.
        /// </summary>
        /// <param name="cond">The condition.</param>
        /// <param name="t">A NodeBase to process.</param>
        /// <param name="f">A NodeBase to process.</param>
        /// <param name="col">The col.</param>
        /// =================================================================================================
        public ConditionalNode(NodeBase cond, NodeBase t, NodeBase f, int col)
        {
            _condition = cond;
            _ifTrue = t;
            _ifFalse = f;

            Column = col;
        }

        /// <inheritdoc />
        protected override bool IsFiniteWhenOperandsFinite => true;

        /// <inheritdoc />
        protected override double EvaluateCore(EvaluationContext ctx)
        {
            var conditionEval = _condition.Evaluate(ctx);

            var condTrue = NodeSemantics.IsTrue(conditionEval, Column);

            if (condTrue)
                return _ifTrue.Evaluate(ctx); // short-circuit: only evaluate chosen branch
            return _ifFalse.Evaluate(ctx);
        }

        /// <inheritdoc />
        protected override Expression CompileCore(CompilationScope scope)
        {
            var condition = _condition.Compile(scope);
            var ifTrue = _ifTrue.Compile(scope);
            var ifFalse = _ifFalse.Compile(scope);

            return Expression.Condition(
                Expression.Call(CompiledRuntime.IsTrueMethod, condition, Expression.Constant(Column)),
                ifTrue,
                ifFalse);
        }
    }
}