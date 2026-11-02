// ***********************************************************************
//  Assembly          : RzR.Shared.Entity.MathExprEngine
//  Author            : RzR
//  Created On        : 2026-10-31 09:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-10-31 15:36
//  ***********************************************************************
//  <copyright file="CompilationAbortedException.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using System;

#endregion

namespace MathExprEngine.Exceptions
{
    /// <summary>
    ///     Thrown internally when the AST exceeds the compile-height cap.
    /// </summary>
    /// <seealso cref="T:Exception" />
    /// <seealso cref="T:System.Exception" />
    internal sealed class CompilationAbortedException : Exception
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="CompilationAbortedException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        internal CompilationAbortedException(string message)
            : base(message)
        {
        }
    }
}