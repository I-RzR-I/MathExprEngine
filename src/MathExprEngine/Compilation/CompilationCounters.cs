// ***********************************************************************
//  Assembly          : RzR.Shared.Entity.MathExprEngine
//  Author            : RzR
//  Created On        : 2026-10-22 20:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-10-31 15:39
//  ***********************************************************************
//  <copyright file="CompilationCounters.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using System.Threading;

#endregion

namespace MathExprEngine.Compilation
{
    /// <summary>
    ///     The thread-safe prepare and compile counters of one engine.
    /// </summary>
    internal sealed class CompilationCounters
    {
        /// <summary>
        ///     The number of compile attempts whose lambda was built.
        /// </summary>
        private int _compileCount;

        /// <summary>
        ///     The number of prepare calls, including failing ones.
        /// </summary>
        private int _prepareCount;

        /// <summary>
        ///     Gets the number of prepare calls made so far.
        /// </summary>
        /// <value>
        ///     The prepare count.
        /// </value>
        internal int PrepareCount => Volatile.Read(ref _prepareCount);

        /// <summary>
        ///     Gets the number of compile attempts made so far.
        /// </summary>
        /// <value>
        ///     The compile count.
        /// </value>
        internal int CompileCount => Volatile.Read(ref _compileCount);

        /// <summary>
        ///     Records the start of one prepare call.
        /// </summary>
        internal void OnPrepare()
        {
            Interlocked.Increment(ref _prepareCount);
        }

        /// <summary>
        ///     Records one compile attempt, before fault injection and compiling.
        /// </summary>
        internal void OnCompile()
        {
            Interlocked.Increment(ref _compileCount);
        }
    }
}