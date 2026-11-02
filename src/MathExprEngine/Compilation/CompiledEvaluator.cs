// ***********************************************************************
//  Assembly          : RzR.Shared.Entity.MathExprEngine
//  Author            : RzR
//  Created On        : 2026-10-22 20:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-10-31 15:39
//  ***********************************************************************
//  <copyright file="CompiledEvaluator.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using System;
using System.Collections.Generic;

#endregion

namespace MathExprEngine.Compilation
{
    /// <summary>
    ///     Represents a closure-free, thread-safe, re-entrant method that runs compiled code.
    /// </summary>
    /// <param name="functions">
    ///     The engine's live function registry, resolved late on every call.
    /// </param>
    /// <param name="slots">The parameter slots bound for this call.</param>
    /// <param name="baseDepth">The evaluation depth the call starts from.</param>
    /// <returns>
    ///     The evaluation result.
    /// </returns>
    internal delegate double CompiledEvaluator(IDictionary<string, Func<double[], double>> functions,
        object[] slots, int baseDepth);
}