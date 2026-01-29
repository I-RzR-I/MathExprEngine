// ***********************************************************************
//  Assembly         : RzR.Shared.Entity.MathExprEngine
//  Author           : RzR
//  Created On       : 2026-01-18 18:01
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-01-18 18:53
// ***********************************************************************
//  <copyright file="ConditionalNode.cs" company="RzR SOFT & TECH">
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
    ///     A conditional node.
    /// </summary>
    /// <seealso cref="T:MathExprEngine.Nodes.NodeBase"/>
    /// =================================================================================================
    internal class ConditionalNode : NodeBase
    {
        // condition ? ifTrue : ifFalse
        private readonly NodeBase _condition, _ifTrue, _ifFalse;

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Initializes a new instance of the <see cref="ConditionalNode"/> class.
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
        public override double Evaluate(MathRuleEngine ctx)
        {
            var conditionEval = _condition.Evaluate(ctx);
            var condTrue = conditionEval != 0.0;

            if (condTrue)
                return _ifTrue.Evaluate(ctx); // short-circuit: only evaluate chosen branch
            return _ifFalse.Evaluate(ctx);
        }
    }
}