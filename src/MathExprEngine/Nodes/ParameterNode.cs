// ***********************************************************************
//  Assembly          : RzR.Shared.Entity.MathExprEngine
//  Author            : RzR
//  Created On        : 2026-10-22 20:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-10-31 15:37
//  ***********************************************************************
//  <copyright file="ParameterNode.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using System.Linq.Expressions;
using DomainCommonExtensions.DataTypeExtensions;
using MathExprEngine.Compilation;
using MathExprEngine.Helpers;
using MathExprEngine.Models;

#endregion

namespace MathExprEngine.Nodes
{
    /// <summary>
    ///     A parameter (variable) node.
    /// </summary>
    /// <seealso cref="T:MathExprEngine.Nodes.NodeBase" />
    internal sealed class ParameterNode : NodeBase
    {
        /// <summary>
        ///     (Immutable) the name.
        /// </summary>
        private readonly string _name;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ParameterNode" /> class.
        /// </summary>
        /// <param name="name">(Immutable) the name.</param>
        /// <param name="column">The column.</param>
        public ParameterNode(string name, int column)
        {
            _name = name;
            Column = column;
        }

        /// <inheritdoc />
        protected override double EvaluateCore(EvaluationContext ctx)
        {
            if (ctx.Parameters.TryGetValue(_name, out var raw).IsFalse())
                throw NodeSemantics.UndefinedVariable(_name, Column);

            return NodeSemantics.CoerceParameter(_name, raw, Column);
        }

        /// <inheritdoc />
        protected override Expression CompileCore(CompilationScope scope)
        {
            var slot = Expression.ArrayIndex(scope.Slots, Expression.Constant(scope.GetSlot(_name)));

            return Expression.Call(
                CompiledRuntime.ReadParameterMethod, slot, Expression.Constant(_name), Expression.Constant(Column));
        }
    }
}