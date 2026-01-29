// ***********************************************************************
//  Assembly         : RzR.Shared.Entity.MathExprEngine
//  Author           : RzR
//  Created On       : 2026-01-18 18:01
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-01-18 18:53
// ***********************************************************************
//  <copyright file="CalculatorException.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using System;
using DomainCommonExtensions.DataTypeExtensions;
using MathExprEngine.Helpers;

#endregion

namespace MathExprEngine.Exceptions
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     Exception for signalling mathematics rule engine errors.
    /// </summary>
    /// <seealso cref="T:Exception"/>
    /// =================================================================================================
    public class MathRuleEngineException : Exception
    {
        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets the column.
        /// </summary>
        /// <value>
        ///     The column.
        /// </value>
        /// =================================================================================================
        public int Column { get; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Initializes a new instance of the <see cref="MathRuleEngineException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="column">(Optional) The column.</param>
        /// =================================================================================================
        public MathRuleEngineException(string message, int column = 0)
            : base(FormatMessage(message, column))
        {
            Column = column;
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Format message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="column">The column.</param>
        /// <returns>
        ///     The formatted message.
        /// </returns>
        /// =================================================================================================
        private static string FormatMessage(string message, int column)
        {
            return column >= 0
                ? DefaultMessages.MathRuleEngineExAtColumn.FormatWith(message, column + 1)
                : DefaultMessages.MathRuleEngineEx.FormatWith(message);
        }
    }
}