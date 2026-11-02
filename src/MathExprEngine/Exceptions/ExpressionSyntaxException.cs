// ***********************************************************************
//  Assembly          : RzR.Shared.Entity.MathExprEngine
//  Author            : RzR
//  Created On        : 2026-09-03 22:09
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-10-31 15:36
//  ***********************************************************************
//  <copyright file="ExpressionSyntaxException.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using System;
using System.Runtime.Serialization;
using DomainCommonExtensions.DataTypeExtensions;
using MathExprEngine.Helpers;

#endregion

namespace MathExprEngine.Exceptions
{
    /// <summary>
    ///     Exception for signalling expression syntax errors.
    /// </summary>
    /// <seealso cref="T:MathExprEngine.Exceptions.MathExprEngineException" />
    [Serializable]
    public class ExpressionSyntaxException : MathExprEngineException
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ExpressionSyntaxException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public ExpressionSyntaxException(string message)
            : this(message, -1, null)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ExpressionSyntaxException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="innerException">The inner exception.</param>
        public ExpressionSyntaxException(string message, Exception innerException)
            : this(message, -1, innerException)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ExpressionSyntaxException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="column">The column.</param>
        public ExpressionSyntaxException(string message, int column)
            : this(message, column, null)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ExpressionSyntaxException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="column">The column.</param>
        /// <param name="innerException">The inner exception.</param>
        public ExpressionSyntaxException(string message, int column, Exception innerException)
            : base(message, FormatMessage(message, column), column, innerException)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ExpressionSyntaxException" /> class for deserialization.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The streaming context.</param>
        protected ExpressionSyntaxException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        ///     Format message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="column">The column.</param>
        /// <returns>
        ///     The formatted message.
        /// </returns>
        private static string FormatMessage(string message, int column)
        {
            return column >= 0
                ? DefaultMessages.SyntaxExAtColumnWithMessage.FormatWith(column + 1, message)
                : DefaultMessages.SyntaxEx.FormatWith(message);
        }

        /// <inheritdoc />
        internal override MathExprEngineException WithColumn(int column)
        {
            return new ExpressionSyntaxException(RawMessage, column, InnerException);
        }
    }
}