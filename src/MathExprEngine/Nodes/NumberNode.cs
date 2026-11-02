// ***********************************************************************
//  Assembly          : RzR.Shared.Entity.MathExprEngine
//  Author            : RzR
//  Created On        : 2026-10-22 20:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-10-31 15:37
//  ***********************************************************************
//  <copyright file="NumberNode.cs" company="RzR SOFT & TECH">
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
    ///     A number node.
    /// </summary>
    /// <seealso cref="T:MathExprEngine.Nodes.NodeBase" />
    internal sealed class NumberNode : NodeBase
    {
        /// <summary>
        ///     (Immutable) the value.
        /// </summary>
        private readonly double _value;

        /// <summary>
        ///     Initializes a new instance of the <see cref="NumberNode" /> class.
        /// </summary>
        /// <param name="v">The value.</param>
        /// <param name="col">The col.</param>
        public NumberNode(double v, int col)
        {
            _value = v;

            Column = col;
        }

        /// <inheritdoc />
        protected override bool IsFiniteWhenOperandsFinite => !double.IsNaN(_value) && !double.IsInfinity(_value);

        /// <inheritdoc />
        protected override double EvaluateCore(EvaluationContext ctx)
        {
            return _value;
        }

        /// <inheritdoc />
        protected override Expression CompileCore(CompilationScope scope)
        {
            return Expression.Constant(_value);
        }
    }
}