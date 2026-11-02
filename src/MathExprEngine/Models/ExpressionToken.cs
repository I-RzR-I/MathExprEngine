// ***********************************************************************
//  Assembly          : RzR.Shared.Entity.MathExprEngine
//  Author            : RzR
//  Created On        : 2026-09-03 22:09
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-10-31 15:37
//  ***********************************************************************
//  <copyright file="ExpressionToken.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using MathExprEngine.Enums;

#endregion


namespace MathExprEngine.Models
{
    /// <summary>
    ///     A token. Token structure: carries source column index for good error messages.
    /// </summary>
    internal class ExpressionToken
    {
        /// <summary>
        ///     0-based column where token starts.
        /// </summary>
        public int Column;

        /// <summary>
        ///     The token kind.
        /// </summary>
        public TokenKind Kind;

        /// <summary>
        ///     If Kind == Number.
        /// </summary>
        public double Number;

        /// <summary>
        ///     Textual content (for numbers, identifiers, operators)
        /// </summary>
        public string Text;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Kind}('{Text}')@{Column + 1}";
        }
    }
}