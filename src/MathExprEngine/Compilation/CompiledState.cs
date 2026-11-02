// ***********************************************************************
//  Assembly          : RzR.Shared.Entity.MathExprEngine
//  Author            : RzR
//  Created On        : 2026-10-22 20:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-10-31 15:39
//  ***********************************************************************
//  <copyright file="CompiledState.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using DomainCommonExtensions.Utilities.Ensure;

#endregion

namespace MathExprEngine.Compilation
{
    /// <summary>
    ///     The immutable delegate, AST height and binder of a compile.
    /// </summary>
    internal sealed class CompiledState
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="CompiledState" /> class.
        /// </summary>
        /// <param name="delegate">
        ///     The closure-free compiled delegate, taking (functions, slots, baseDepth).
        /// </param>
        /// <param name="height">The static AST height (deepest node depth) the delegate runs at.</param>
        /// <param name="binder">The binder that builds the slot array for each call.</param>
        internal CompiledState(CompiledEvaluator @delegate, int height,
            ParameterBinder binder)
        {
            DomainEnsure.IsNotNull(@delegate, nameof(@delegate));
            DomainEnsure.IsNotNull(binder, nameof(binder));

            Delegate = @delegate;
            Height = height;
            Binder = binder;
        }

        /// <summary>
        ///     Gets the compiled delegate.
        /// </summary>
        /// <value>
        ///     The delegate.
        /// </value>
        internal CompiledEvaluator Delegate { get; }

        /// <summary>
        ///     Gets the static AST height for the depth-headroom check.
        /// </summary>
        /// <value>
        ///     The height.
        /// </value>
        internal int Height { get; }

        /// <summary>
        ///     Gets the parameter binder.
        /// </summary>
        /// <value>
        ///     The binder.
        /// </value>
        internal ParameterBinder Binder { get; }
    }
}