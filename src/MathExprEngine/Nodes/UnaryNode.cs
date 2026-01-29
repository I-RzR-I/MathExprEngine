// ***********************************************************************
//  Assembly         : RzR.Shared.Entity.MathExprEngine
//  Author           : RzR
//  Created On       : 2026-01-18 18:01
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-01-18 18:53
// ***********************************************************************
//  <copyright file="UnaryNode.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using MathExprEngine.Exceptions;

#endregion

namespace MathExprEngine.Nodes
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     An unary node.
    /// </summary>
    /// <seealso cref="T:MathExprEngine.Nodes.NodeBase"/>
    /// =================================================================================================
    internal class UnaryNode : NodeBase
    {
        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     (Immutable) the operand.
        /// </summary>
        /// =================================================================================================
        private readonly NodeBase _operand;

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     (Immutable) operation "+" or "-".
        /// </summary>
        /// =================================================================================================
        private readonly string _operation;

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Initializes a new instance of the <see cref="UnaryNode"/> class.
        /// </summary>
        /// <param name="operation">(Immutable) operation "+" or "-".</param>
        /// <param name="operand">(Immutable) the operand.</param>
        /// <param name="col">The col.</param>
        /// =================================================================================================
        public UnaryNode(string operation, NodeBase operand, int col)
        {
            _operation = operation;
            _operand = operand;

            Column = col;
        }

        /// <inheritdoc />
        public override double Evaluate(MathRuleEngine ctx)
        {
            var v = _operand.Evaluate(ctx);

            return _operation switch
            {
                "+" => v,
                "-" => -v,
                _ => throw new MathRuleEngineException($"Unknown unary operator '{_operation}'", Column)
            };
        }
    }
}

