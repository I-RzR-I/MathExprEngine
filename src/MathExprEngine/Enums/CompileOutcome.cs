// ***********************************************************************
//  Assembly          : RzR.Shared.Entity.MathExprEngine
//  Author            : RzR
//  Created On        : 2026-10-31 15:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-10-31 15:44
//  ***********************************************************************
//  <copyright file="CompileOutcome.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using MathExprEngine.Compilation;

#endregion

namespace MathExprEngine.Enums
{
    /// <summary>
    ///     The result of one compile or tier-up attempt.
    /// </summary>
    internal enum CompileOutcome
    {
        /// <summary>
        ///     No compile was attempted (guard declined or threshold not reached).
        /// </summary>
        None = 0,

        /// <summary>
        ///     The program was compiled and its delegate published.
        /// </summary>
        Compiled,

        /// <summary>
        ///     The AST is taller than the compile-height cap (permanent).
        /// </summary>
        HeightCapExceeded,

        /// <summary>
        ///     Too little stack or evaluation-depth headroom to compile (transient).
        /// </summary>
        InsufficientStack,

        /// <summary>
        ///     Dynamic code is unavailable or simulated as unavailable (permanent).
        /// </summary>
        NoDynamicCode,

        /// <summary>
        ///     An unexpected exception occurred while building or compiling (permanent).
        /// </summary>
        CompileFault,

        /// <summary>
        ///     Another thread holds the compile lock (never stored).
        /// </summary>
        Busy
    }
}