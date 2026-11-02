// ***********************************************************************
//  Assembly          : RzR.Shared.Entity.MathExprEngine
//  Author            : RzR
//  Created On        : 2026-10-22 20:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-10-31 15:38
//  ***********************************************************************
//  <copyright file="UnaryNode.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using System.Linq.Expressions;
using RzR.Extensions.Domain.Text;
using MathExprEngine.Compilation;
using MathExprEngine.Helpers;
using MathExprEngine.Models;

#endregion

namespace MathExprEngine.Nodes
{
    /// <summary>
    ///     An unary node.
    /// </summary>
    /// <seealso cref="T:MathExprEngine.Nodes.NodeBase" />
    internal sealed class UnaryNode : NodeBase
    {
        /// <summary>
        ///     (Immutable) the operand.
        /// </summary>
        private readonly NodeBase _operand;

        /// <summary>
        ///     (Immutable) operation "+", "-" or "%".
        /// </summary>
        private readonly string _operation;

        /// <summary>
        ///     Initializes a new instance of the <see cref="UnaryNode" /> class.
        /// </summary>
        /// <param name="operation">(Immutable) operation "+", "-" or "%".</param>
        /// <param name="operand">(Immutable) the operand.</param>
        /// <param name="col">The col.</param>
        public UnaryNode(string operation, NodeBase operand, int col)
        {
            _operation = operation;
            _operand = operand;

            Column = col;
        }

        /// <inheritdoc />
        protected override bool IsFiniteWhenOperandsFinite => _operation == "+" || _operation == "-";

        /// <inheritdoc />
        protected override double EvaluateCore(EvaluationContext ctx)
        {
            var v = _operand.Evaluate(ctx);

            return _operation switch
            {
                "+" => v,
                "-" => -v,
                "%" => v / 100.0,
                _ => throw NodeSemantics.UnknownOperator(DefaultMessages.UnknownUnaryOperator.FormatWith(_operation),
                    Column)
            };
        }

        /// <inheritdoc />
        protected override Expression CompileCore(CompilationScope scope)
        {
            var operand = _operand.Compile(scope);

            switch (_operation)
            {
                case "+": return operand;
                case "-": return Expression.Negate(operand);
                case "%": return Expression.Divide(operand, Expression.Constant(100.0));
                default:
                    return Expression.Call(
                        CompiledRuntime.ThrowUnknownOperatorMethod,
                        Expression.Constant(DefaultMessages.UnknownUnaryOperator.FormatWith(_operation)),
                        Expression.Constant(Column));
            }
        }
    }
}