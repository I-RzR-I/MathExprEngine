// ***********************************************************************
//  Assembly         : RzR.Shared.Entity.MathExprEngine
//  Author           : RzR
//  Created On       : 2026-01-18 18:01
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-01-18 18:53
// ***********************************************************************
//  <copyright file="NumberNode.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

namespace MathExprEngine.Nodes
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     A number node.
    /// </summary>
    /// <seealso cref="T:MathExprEngine.Nodes.NodeBase"/>
    /// =================================================================================================
    internal class NumberNode : NodeBase
    {
        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     (Immutable) the value.
        /// </summary>
        /// =================================================================================================
        private readonly double _value;

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Initializes a new instance of the <see cref="NumberNode"/> class.
        /// </summary>
        /// <param name="v">The value.</param>
        /// <param name="col">The col.</param>
        /// =================================================================================================
        public NumberNode(double v, int col)
        {
            _value = v;

            Column = col;
        }

        /// <inheritdoc />
        public override double Evaluate(MathRuleEngine ctx)
        {
            return _value;
        }
    }
}

