// ***********************************************************************
//  Assembly         : RzR.Shared.Entity.MathExprEngine
//  Author           : RzR
//  Created On       : 2026-01-18 18:01
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-01-18 18:53
// ***********************************************************************
//  <copyright file="BinaryNode.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using System;
using DomainCommonExtensions.DataTypeExtensions;
using MathExprEngine.Exceptions;
using MathExprEngine.Helpers;

#endregion

namespace MathExprEngine.Nodes
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     A binary node.
    /// </summary>
    /// <seealso cref="T:MathExprEngine.Nodes.NodeBase"/>
    /// =================================================================================================
    internal class BinaryNode : NodeBase
    {
        private readonly NodeBase _left, _right;
        private readonly string _oper; // + - * / ^ < > <= >= == != && ||

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Initializes a new instance of the <see cref="BinaryNode"/> class.
        /// </summary>
        /// <param name="oper">The operator.</param>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <param name="col">The col.</param>
        /// =================================================================================================
        public BinaryNode(string oper, NodeBase left, NodeBase right, int col)
        {
            _oper = oper;
            _left = left;
            _right = right;

            Column = col;
        }

        /// <inheritdoc/>
        public override double Evaluate(MathRuleEngine ctx)
        {
            // Short-circuit behavior is implemented here for && and ||
            if (_oper == "&&")
            {
                var leftEval = _left.Evaluate(ctx);
                
                if (leftEval == 0.0) 
                    return 0.0; // short-circuit: false && ... => false (0)

                var rightEval = _right.Evaluate(ctx);

                return rightEval != 0.0 
                    ? 1.0 
                    : 0.0;
            }

            if (_oper == "||")
            {
                var leftEval = _left.Evaluate(ctx);
                
                if (leftEval != 0.0) 
                    return 1.0; // short-circuit: true || ... => true (1)

                var rightEval = _right.Evaluate(ctx);

                return rightEval != 0.0 
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
                case "/":
                    if (r == 0.0) 
                        throw new MathRuleEngineException("Division by zero", Column);

                    return l / r;
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
                    throw new MathRuleEngineException(DefaultMessages.UnknownBinaryOperator.FormatWith(_oper), Column);
            }
        }
    }
}

