// ***********************************************************************
//  Assembly         : RzR.Shared.Entity.MathExprEngine
//  Author           : RzR
//  Created On       : 2026-01-18 18:01
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-01-18 18:53
// ***********************************************************************
//  <copyright file="FunctionArgumentException.cs" company="RzR SOFT & TECH">
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
    ///     Exception for signalling function argument errors.
    /// </summary>
    /// <seealso cref="T:Exception" />
    /// =================================================================================================
    public class FunctionArgumentException : Exception
    {
        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets the function.
        /// </summary>
        /// <value>
        ///     The function.
        /// </value>
        /// =================================================================================================
        public string Function { get; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets the number of arguments.
        /// </summary>
        /// <value>
        ///     The number of arguments.
        /// </value>
        /// =================================================================================================
        public int ArgumentCount { get; }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Initializes a new instance of the <see cref="FunctionArgumentException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// =================================================================================================
        public FunctionArgumentException(string message)
            : base(message)
        {
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Initializes a new instance of the <see cref="FunctionArgumentException" /> class.
        /// </summary>
        /// <param name="functionName">Name of the function.</param>
        /// <param name="argumentCount">The number of arguments.</param>
        /// =================================================================================================
        public FunctionArgumentException(string functionName, int argumentCount)
            : base(FormatMessage(functionName, argumentCount))
        {
            Function = functionName;
            ArgumentCount = argumentCount;
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Format message.
        /// </summary>
        /// <param name="functionName">Name of the function.</param>
        /// <param name="argCount">Number of arguments.</param>
        /// <returns>
        ///     The formatted message.
        /// </returns>
        /// =================================================================================================
        private static string FormatMessage(string functionName, int argCount)
        {
            return argCount > 1
                ? DefaultMessages.FunctionExpectsXArgs.FormatWith(functionName, argCount)
                : DefaultMessages.FunctionExpectsXArg.FormatWith(functionName, argCount);
        }
    }
}