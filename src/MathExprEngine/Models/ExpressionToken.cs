// ***********************************************************************
//  Assembly         : RzR.Shared.Entity.MathExprEngine
//  Author           : RzR
//  Created On       : 2026-01-18 18:01
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-01-18 18:53
// ***********************************************************************
//  <copyright file="ExpressionToken.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using MathExprEngine.Enums;

#endregion


namespace MathExprEngine.Models
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     A token.
    ///     Token structure: carries source column index for good error messages.
    /// </summary>
    /// =================================================================================================
    public class ExpressionToken
    {
        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     0-based column where token starts.
        /// </summary>
        /// =================================================================================================
        public int Column;

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     The token kind.
        /// </summary>
        /// =================================================================================================
        public TokenKind Kind;

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     If Kind == Number.
        /// </summary>
        /// =================================================================================================
        public double Number;

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Textual content (for numbers, identifiers, operators)
        /// </summary>
        /// =================================================================================================
        public string Text;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Kind}('{Text}')@{Column + 1}";
        }
    }
}