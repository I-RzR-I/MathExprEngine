// ***********************************************************************
//  Assembly          : RzR.Shared.Entity.MathExprEngine
//  Author            : RzR
//  Created On        : 2026-09-03 22:09
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-10-31 15:36
//  ***********************************************************************
//  <copyright file="MathExprEngineException.cs" company="RzR SOFT & TECH">
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

#endregion

namespace MathExprEngine.Exceptions
{
    /// <summary>
    ///     The base class of the engine's public expression exceptions.
    /// </summary>
    /// <seealso cref="T:Exception" />
    [Serializable]
    public abstract class MathExprEngineException : Exception
    {
        /// <summary>
        ///     (Immutable) the serialization key used for <see cref="Column" />.
        /// </summary>
        private const string ColumnKey = "MathExprEngine.Column";

        /// <summary>
        ///     Initializes a new instance of the <see cref="MathExprEngineException" /> class.
        /// </summary>
        /// <param name="rawMessage">
        ///     The message before column formatting, reused by <see cref="WithColumn" />.
        /// </param>
        /// <param name="formattedMessage">The already-formatted message.</param>
        /// <param name="column">0-based source column, or -1 when the position is unknown.</param>
        /// <param name="innerException">The inner exception.</param>
        protected MathExprEngineException(string rawMessage, string formattedMessage, int column,
            Exception innerException)
            : base(formattedMessage, innerException)
        {
            Column = column;
            RawMessage = rawMessage;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MathExprEngineException" /> class for deserialization.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The streaming context.</param>
        protected MathExprEngineException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Column = info.GetInt32(ColumnKey);
        }

        /// <summary>
        ///     Gets the 0-based source column, or <c>-1</c> if unknown.
        /// </summary>
        /// <value>
        ///     The column.
        /// </value>
        public int Column { get; }

        /// <summary>
        ///     Gets the message before column formatting, or null when deserialized.
        /// </summary>
        /// <value>
        ///     The raw, pre-formatting message.
        /// </value>
        protected string RawMessage { get; }

        /// <summary>
        ///     Returns a copy reporting <paramref name="column" />, keeping raw message and inner exception.
        /// </summary>
        /// <param name="column">The 0-based source column to attach.</param>
        /// <returns>
        ///     A copy with <paramref name="column" />, or this instance if not overridden.
        /// </returns>
        internal virtual MathExprEngineException WithColumn(int column)
        {
            return this;
        }

        /// <inheritdoc />
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(ColumnKey, Column);
        }
    }
}