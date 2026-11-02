// ***********************************************************************
//  Assembly          : RzR.Shared.Entity.MathExprEngine
//  Author            : RzR
//  Created On        : 2026-09-03 22:09
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-10-31 15:36
//  ***********************************************************************
//  <copyright file="ExpressionFunctionException.cs" company="RzR SOFT & TECH">
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
using RzR.Extensions.Domain.Text;
using MathExprEngine.Helpers;

#endregion

namespace MathExprEngine.Exceptions
{
    /// <summary>
    ///     Exception for signalling expression function errors.
    /// </summary>
    /// <seealso cref="T:MathExprEngine.Exceptions.MathExprEngineException" />
    [Serializable]
    public class ExpressionFunctionException : MathExprEngineException
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ExpressionFunctionException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public ExpressionFunctionException(string message)
            : this(message, -1, null)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ExpressionFunctionException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="innerException">The inner exception.</param>
        public ExpressionFunctionException(string message, Exception innerException)
            : this(message, -1, innerException)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ExpressionFunctionException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="column">The column.</param>
        public ExpressionFunctionException(string message, int column)
            : this(message, column, null)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ExpressionFunctionException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="column">The column.</param>
        /// <param name="innerException">The inner exception.</param>
        public ExpressionFunctionException(string message, int column, Exception innerException)
            : base(message, FormatMessage(message, column), column, innerException)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ExpressionFunctionException" /> class for deserialization.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The streaming context.</param>
        protected ExpressionFunctionException(SerializationInfo info, StreamingContext context)
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
                ? DefaultMessages.FunctionExAtColumnWithMessage.FormatWith(column + 1, message)
                : DefaultMessages.FunctionEx.FormatWith(message);
        }

        /// <inheritdoc />
        internal override MathExprEngineException WithColumn(int column)
        {
            return new ExpressionFunctionException(RawMessage, column, InnerException);
        }
    }
}