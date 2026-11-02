// ***********************************************************************
//  Assembly          : RzR.Shared.Entity.MathExprEngine
//  Author            : RzR
//  Created On        : 2026-09-03 22:09
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-10-31 15:36
//  ***********************************************************************
//  <copyright file="FunctionArgumentException.cs" company="RzR SOFT & TECH">
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
    ///     Exception for signalling function argument errors.
    /// </summary>
    /// <seealso cref="T:MathExprEngine.Exceptions.MathExprEngineException" />
    [Serializable]
    public class FunctionArgumentException : MathExprEngineException
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="FunctionArgumentException" /> class for a
        ///     function that requires an exact number of arguments.
        /// </summary>
        /// <param name="functionName">Name of the function.</param>
        /// <param name="argumentCount">The number of arguments.</param>
        public FunctionArgumentException(string functionName, int argumentCount)
            : this(functionName, argumentCount, false, null)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="FunctionArgumentException" /> class for an exact arity.
        /// </summary>
        /// <param name="functionName">Name of the function.</param>
        /// <param name="argumentCount">The number of arguments.</param>
        /// <param name="innerException">The inner exception.</param>
        public FunctionArgumentException(string functionName, int argumentCount, Exception innerException)
            : this(functionName, argumentCount, false, innerException)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="FunctionArgumentException" /> class.
        /// </summary>
        /// <param name="functionName">Name of the function.</param>
        /// <param name="argumentCount">
        ///     The number of arguments (exact, or minimum when <paramref name="minimumOnly" /> is true).
        /// </param>
        /// <param name="minimumOnly">
        ///     True when <paramref name="argumentCount" /> is a minimum rather than an exact arity.
        /// </param>
        /// <param name="innerException">The inner exception.</param>
        private FunctionArgumentException(
            string functionName, int argumentCount, bool minimumOnly, Exception innerException)
            : this(FormatMessage(functionName, argumentCount, minimumOnly), functionName, argumentCount, minimumOnly,
                innerException)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="FunctionArgumentException" /> class from a preformatted message.
        /// </summary>
        /// <param name="message">The already-formatted display message.</param>
        /// <param name="functionName">Name of the function.</param>
        /// <param name="argumentCount">
        ///     The number of arguments (exact, or minimum when <paramref name="minimumOnly" /> is true).
        /// </param>
        /// <param name="minimumOnly">
        ///     True when <paramref name="argumentCount" /> is a minimum rather than an exact arity.
        /// </param>
        /// <param name="innerException">The inner exception.</param>
        private FunctionArgumentException(
            string message, string functionName, int argumentCount, bool minimumOnly, Exception innerException)
            : base(message, message, -1, innerException)
        {
            Function = functionName;
            ArgumentCount = argumentCount;
            IsMinimum = minimumOnly;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="FunctionArgumentException" /> class for deserialization.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The streaming context.</param>
        protected FunctionArgumentException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        ///     Gets the function.
        /// </summary>
        /// <value>
        ///     The function.
        /// </value>
        public string Function { get; }

        /// <summary>
        ///     Gets the number of arguments.
        /// </summary>
        /// <value>
        ///     The number of arguments.
        /// </value>
        public int ArgumentCount { get; }

        /// <summary>
        ///     Gets a value indicating whether <see cref="ArgumentCount" /> is a minimum arity.
        /// </summary>
        /// <value>
        ///     True if <see cref="ArgumentCount" /> is a minimum, false if it is exact.
        /// </value>
        public bool IsMinimum { get; }

        /// <summary>
        ///     Creates an exception for at least <paramref name="minimumArgumentCount" /> arguments.
        /// </summary>
        /// <param name="functionName">Name of the function.</param>
        /// <param name="minimumArgumentCount">The minimum number of arguments.</param>
        /// <returns>
        ///     A new <see cref="FunctionArgumentException" />.
        /// </returns>
        public static FunctionArgumentException ForMinimumArity(string functionName, int minimumArgumentCount)
        {
            return new FunctionArgumentException(functionName, minimumArgumentCount, true, null);
        }

        /// <summary>
        ///     Formats the expected-arity message, exact or minimum.
        /// </summary>
        /// <param name="functionName">Name of the function.</param>
        /// <param name="argCount">Number of arguments.</param>
        /// <param name="minimumOnly">
        ///     True when <paramref name="argCount" /> is a minimum rather than an exact arity.
        /// </param>
        /// <returns>
        ///     The formatted message.
        /// </returns>
        private static string FormatMessage(string functionName, int argCount, bool minimumOnly)
        {
            if (minimumOnly)
                return DefaultMessages.FunctionExpectsAtLeastXArgs.FormatWith(functionName, argCount);

            return argCount > 1
                ? DefaultMessages.FunctionExpectsXArgs.FormatWith(functionName, argCount)
                : DefaultMessages.FunctionExpectsXArg.FormatWith(functionName, argCount);
        }
    }
}