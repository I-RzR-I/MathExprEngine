// ***********************************************************************
//  Assembly         : RzR.Shared.Entity.MathExprEngine
//  Author           : RzR
//  Created On       : 2026-01-18 18:01
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-01-18 18:53
// ***********************************************************************
//  <copyright file="NodeBase.cs" company="RzR SOFT & TECH">
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
    ///     A node base.
    /// </summary>
    /// =================================================================================================
    internal abstract class NodeBase
    {
        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     The column idx. For error reporting.
        /// </summary>
        /// =================================================================================================
        public int Column;

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Evaluates the given context.
        /// </summary>
        /// <param name="ctx">The context.</param>
        /// <returns>
        ///     A double.
        /// </returns>
        /// =================================================================================================
        public abstract double Evaluate(MathRuleEngine ctx);
    }
}