// ***********************************************************************
//  Assembly         : RzR.Shared.Entity.MathExprEngine
//  Author           : RzR
//  Created On       : 2026-02-17 14:02
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-02-17 15:27
// ***********************************************************************
//  <copyright file="ParameterNode.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using System;
using System.Globalization;
using DomainCommonExtensions.DataTypeExtensions;
using MathExprEngine.Exceptions;

#endregion

namespace MathExprEngine.Nodes
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     A parameter (variable) node.
    /// </summary>
    /// <seealso cref="T:MathExprEngine.Nodes.NodeBase"/>
    /// =================================================================================================
    internal class ParameterNode : NodeBase
    {
        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     (Immutable) the name.
        /// </summary>
        /// =================================================================================================
        private readonly string _name;

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Initializes a new instance of the <see cref="ParameterNode"/> class.
        /// </summary>
        /// <param name="name">(Immutable) the name.</param>
        /// <param name="column">The column.</param>
        /// =================================================================================================
        public ParameterNode(string name, int column)
        {
            _name = name;
            Column = column;
        }

        /// <inheritdoc />
        public override double Evaluate(MathRuleEngine ctx)
        {
            if (ctx.Parameters.TryGetValue(_name, out var raw).IsFalse())
                throw new ExpressionSyntaxException($"Undefined variable '{_name}'.", Column);

            return Convert.ToDouble(raw, CultureInfo.InvariantCulture);
        }
    }
}