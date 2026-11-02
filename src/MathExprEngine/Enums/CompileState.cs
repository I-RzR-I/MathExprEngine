// ***********************************************************************
//  Assembly          : RzR.Shared.Entity.MathExprEngine
//  Author            : RzR
//  Created On        : 2026-10-31 15:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-10-31 15:44
//  ***********************************************************************
//  <copyright file="CompileState.cs" company="RzR SOFT & TECH">
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
    ///     The <see cref="ExpressionProgram" /> compile state, changed only under its lock.
    /// </summary>
    internal enum CompileState
    {
        /// <summary>
        ///     The program is not compiled yet and may tier up.
        /// </summary>
        NotCompiled = 0,

        /// <summary>
        ///     A published delegate runs when depth headroom allows (permanent).
        /// </summary>
        Compiled = 1,

        /// <summary>
        ///     Compilation failed, so the program always interprets its AST (permanent).
        /// </summary>
        Failed = 2
    }
}