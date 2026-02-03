// ***********************************************************************
//  Assembly         : RzR.Shared.Entity.MathExprEngine
//  Author           : RzR
//  Created On       : 2026-02-02 21:02
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-02-02 21:04
// ***********************************************************************
//  <copyright file="ExpressionFunctionException.cs" company="RzR SOFT & TECH">
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
    ///     Exception for signalling expression function errors.
    /// </summary>
    /// <seealso cref="T:Exception"/>
    /// =================================================================================================
    public class ExpressionFunctionException : Exception
    {
        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Initializes a new instance of the <see cref="ExpressionFunctionException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="column">(Optional) The column.</param>
        /// =================================================================================================
        public ExpressionFunctionException(string message, int column = 0)
            : base(FormatMessage(message, column))
        {
            Column = column;
        }

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
                ? DefaultMessages.SyntaxExAtColumnWithMessage.FormatWith(message, column + 1)
                : DefaultMessages.SyntaxEx.FormatWith(message);
        }
    }
}