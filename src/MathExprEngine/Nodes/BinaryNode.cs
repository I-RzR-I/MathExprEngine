// ***********************************************************************
//  Assembly          : RzR.Shared.Entity.MathExprEngine
//  Author            : RzR
//  Created On        : 2026-10-22 20:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-10-31 15:37
//  ***********************************************************************
//  <copyright file="BinaryNode.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using System;
using System.Linq.Expressions;
using RzR.Extensions.Domain.Text;
using MathExprEngine.Compilation;
using MathExprEngine.Helpers;
using MathExprEngine.Models;

#endregion

namespace MathExprEngine.Nodes
{
    /// <summary>
    ///     A binary node.
    /// </summary>
    /// <seealso cref="T:MathExprEngine.Nodes.NodeBase" />
    internal sealed class BinaryNode : NodeBase
    {
        private readonly NodeBase _left, _right;
        private readonly string _oper; // + - * / % ^ < > <= >= == != && ||

        /// <summary>
        ///     Initializes a new instance of the <see cref="BinaryNode" /> class.
        /// </summary>
        /// <param name="oper">The operator.</param>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <param name="col">The col.</param>
        public BinaryNode(string oper, NodeBase left, NodeBase right, int col)
        {
            _oper = oper;
            _left = left;
            _right = right;

            Column = col;
        }

        /// <inheritdoc />
        protected override bool IsFiniteWhenOperandsFinite
        {
            get
            {
                switch (_oper)
                {
                    case "&&":
                    case "||":
                    case "<":
                    case ">":
                    case "<=":
                    case ">=":
                    case "==":
                    case "!=":
                        return true;
                    default:
                        return false;
                }
            }
        }

        /// <inheritdoc />
        protected override double EvaluateCore(EvaluationContext ctx)
        {
            // Short-circuit behavior is implemented here for && and ||
            if (_oper == "&&")
            {
                var leftEval = _left.Evaluate(ctx);

                if (!NodeSemantics.IsTrue(leftEval, Column))
                    return 0.0; // short-circuit: false && ... => false (0)

                var rightEval = _right.Evaluate(ctx);

                return NodeSemantics.IsTrue(rightEval, Column)
                    ? 1.0
                    : 0.0;
            }

            if (_oper == "||")
            {
                var leftEval = _left.Evaluate(ctx);

                if (NodeSemantics.IsTrue(leftEval, Column))
                    return 1.0; // short-circuit: true || ... => true (1)

                var rightEval = _right.Evaluate(ctx);

                return NodeSemantics.IsTrue(rightEval, Column)
                    ? 1.0
                    : 0.0;
            }

            // For other ops, evaluate both sides
            var l = _left.Evaluate(ctx);
            var r = _right.Evaluate(ctx);

            switch (_oper)
            {
                case "+": return l + r;
                case "-": return l - r;
                case "*": return l * r;
                case "/": return NodeSemantics.Divide(l, r, Column);
                case "%": return NodeSemantics.Modulo(l, r, Column);
                case "^": return Math.Pow(l, r);

                // Comparisons: return 1.0 for true, 0.0 for false
                case "<": return l < r ? 1.0 : 0.0;
                case ">": return l > r ? 1.0 : 0.0;
                case "<=": return l <= r ? 1.0 : 0.0;
                case ">=": return l >= r ? 1.0 : 0.0;
                // ReSharper disable CompareOfFloatsByEqualityOperator
                case "==": return l == r ? 1.0 : 0.0;
                case "!=": return l != r ? 1.0 : 0.0;
                // ReSharper restore CompareOfFloatsByEqualityOperator

                default:
                    throw NodeSemantics.UnknownOperator(DefaultMessages.UnknownBinaryOperator.FormatWith(_oper),
                        Column);
            }
        }

        /// <inheritdoc />
        protected override Expression CompileCore(CompilationScope scope)
        {
            var l = _left.Compile(scope);
            var r = _right.Compile(scope);
            var column = Expression.Constant(Column);

            switch (_oper)
            {
                case "&&":
                    return Expression.Condition(
                        Expression.Call(CompiledRuntime.IsTrueMethod, l, column),
                        AsNumber(Expression.Call(CompiledRuntime.IsTrueMethod, r, column)),
                        Expression.Constant(0.0));
                case "||":
                    return Expression.Condition(
                        Expression.Call(CompiledRuntime.IsTrueMethod, l, column),
                        Expression.Constant(1.0),
                        AsNumber(Expression.Call(CompiledRuntime.IsTrueMethod, r, column)));
                case "+": return Expression.Add(l, r);
                case "-": return Expression.Subtract(l, r);
                case "*": return Expression.Multiply(l, r);
                case "/": return Expression.Call(CompiledRuntime.DivideMethod, l, r, column);
                case "%": return Expression.Call(CompiledRuntime.ModuloMethod, l, r, column);
                case "^": return Expression.Call(CompiledRuntime.PowMethod, l, r);
                case "<": return AsNumber(Expression.LessThan(l, r));
                case ">": return AsNumber(Expression.GreaterThan(l, r));
                case "<=": return AsNumber(Expression.LessThanOrEqual(l, r));
                case ">=": return AsNumber(Expression.GreaterThanOrEqual(l, r));
                case "==": return AsNumber(Expression.Equal(l, r));
                case "!=": return AsNumber(Expression.NotEqual(l, r));
                default:
                    return Expression.Call(
                        CompiledRuntime.ThrowUnknownOperatorMethod,
                        Expression.Constant(DefaultMessages.UnknownBinaryOperator.FormatWith(_oper)),
                        column);
            }
        }

        /// <summary>
        ///     Maps a boolean expression to <c>1.0</c> or <c>0.0</c>, as the interpreter does.
        /// </summary>
        /// <param name="test">The boolean expression.</param>
        /// <returns>
        ///     An expression of type <see cref="double" />.
        /// </returns>
        private static ConditionalExpression AsNumber(Expression test)
        {
            return Expression.Condition(test, Expression.Constant(1.0), Expression.Constant(0.0));
        }
    }
}