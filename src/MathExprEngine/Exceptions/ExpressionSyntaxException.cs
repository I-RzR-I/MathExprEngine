// ***********************************************************************
//  Assembly         : RzR.Shared.Entity.MathExprEngine
//  Author           : RzR
//  Created On       : 2026-02-02 14:02
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-02-02 14:57
// ***********************************************************************
//  <copyright file="ExpressionSyntaxException.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using MathExprEngine.Helpers;
using System;
using DomainCommonExtensions.DataTypeExtensions;

#endregion

namespace MathExprEngine.Exceptions
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     Exception for signalling expression syntax errors.
    /// </summary>
    /// <seealso cref="T:Exception"/>
    /// =================================================================================================
    public class ExpressionSyntaxException : Exception
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
        ///     Initializes a new instance of the <see cref="ExpressionSyntaxException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="column">(Optional) The column.</param>
        /// =================================================================================================
        public ExpressionSyntaxException(string message, int column = 0)
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
                ? DefaultMessages.SyntaxExAtColumnWithMessage.FormatWith(column + 1, message)
                : DefaultMessages.SyntaxEx.FormatWith(message);
        }
    }
}