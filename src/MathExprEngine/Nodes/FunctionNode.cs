// ***********************************************************************
//  Assembly         : RzR.Shared.Entity.MathExprEngine
//  Author           : RzR
//  Created On       : 2026-01-18 18:01
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-01-18 18:53
// ***********************************************************************
//  <copyright file="FunctionNode.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using System;
using System.Collections.Generic;
using DomainCommonExtensions.DataTypeExtensions;
using MathExprEngine.Exceptions;
using MathExprEngine.Helpers;

#endregion

namespace MathExprEngine.Nodes
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     A function node.
    /// </summary>
    /// <seealso cref="T:MathExprEngine.Nodes.NodeBase"/>
    /// =================================================================================================
    internal class FunctionNode : NodeBase
    {
        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     (Immutable) the arguments.
        /// </summary>
        /// =================================================================================================
        private readonly List<NodeBase> _args;

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     (Immutable) the name.
        /// </summary>
        /// =================================================================================================
        private readonly string _name;

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Initializes a new instance of the <see cref="FunctionNode"/> class.
        /// </summary>
        /// <param name="name">(Immutable) the name.</param>
        /// <param name="args">(Immutable) the arguments.</param>
        /// <param name="col">The col.</param>
        /// =================================================================================================
        public FunctionNode(string name, List<NodeBase> args, int col)
        {
            _name = name;
            _args = args;

            Column = col;
        }

        /// <inheritdoc/>
        public override double Evaluate(MathRuleEngine ctx)
        {
            if (!ctx.Functions.TryGetValue(_name, out var impl))
                throw new ExpressionFunctionException(DefaultMessages.UnknownFunction.FormatWith(_name), Column);

            var evaluated = new double[_args.Count];
            for (var i = 0; i < _args.Count; i++)
                evaluated[i] = _args[i].Evaluate(ctx);

            try
            {
                return impl(evaluated);
            }
            catch (Exception ex)
            {
                throw new ExpressionFunctionException(DefaultMessages.FunctionError.FormatWith(_name, ex.Message), Column);
            }
        }
    }
}