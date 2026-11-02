// ***********************************************************************
//  Assembly          : RzR.Shared.Entity.MathExprEngine
//  Author            : RzR
//  Created On        : 2026-09-03 22:09
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-10-31 15:38
//  ***********************************************************************
//  <copyright file="TokenKind.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

namespace MathExprEngine.Enums
{
    /// <summary>
    ///     Values that represent token kinds.
    /// </summary>
    internal enum TokenKind
    {
        /// <summary>
        ///     An enum constant representing the number option.
        /// </summary>
        Number,

        /// <summary>
        ///     function names, variables (we only use as function names)
        /// </summary>
        Identifier,

        /// <summary>
        ///     An enum constant representing the plus [+] option.
        /// </summary>
        Plus,

        /// <summary>
        ///     An enum constant representing the minus [-] option.
        /// </summary>
        Minus,

        /// <summary>
        ///     An enum constant representing the star [*] option.
        /// </summary>
        Star,

        /// <summary>
        ///     An enum constant representing the slash [/] option.
        /// </summary>
        Slash,

        /// <summary>
        ///     An enum constant representing the caret [^] option.
        /// </summary>
        Caret,

        /// <summary>
        ///     An enum constant representing the left parenthesis  [(] option.
        /// </summary>
        LParen,

        /// <summary>
        ///     An enum constant representing the right parenthesis [)] option.
        /// </summary>
        RParen,

        /// <summary>
        ///     An enum constant representing the 'comma' [,] option.
        /// </summary>
        Comma,

        /// <summary>
        ///     An enum constant representing the 'question' [?] option.
        /// </summary>
        Question,

        /// <summary>
        ///     An enum constant representing the 'colon' [:] option.
        /// </summary>
        Colon,

        /// <summary>
        ///     An enum constant representing the 'less' [&lt;] option.
        /// </summary>
        Less,

        /// <summary>
        ///     An enum constant representing the 'greater' [&gt;] option.
        /// </summary>
        Greater,

        /// <summary>
        ///     An enum constant representing the 'less equal' [&lt;=] option.
        /// </summary>
        LessEqual,

        /// <summary>
        ///     An enum constant representing the 'greater equal' [&gt;=] option.
        /// </summary>
        GreaterEqual,

        /// <summary>
        ///     An enum constant representing the 'equal' [==] option.
        /// </summary>
        EqualEqual,

        /// <summary>
        ///     An enum constant representing the 'not equal' [!=] option.
        /// </summary>
        NotEqual,

        /// <summary>
        ///     An enum constant representing the 'and' [&amp;&amp;] option.
        /// </summary>
        AndAnd,

        /// <summary>
        ///     An enum constant representing the 'or' [||] option.
        /// </summary>
        OrOr,

        /// <summary>
        ///     An enum constant representing the end of text option.
        /// </summary>
        EndOfText,

        /// <summary>
        ///     An enum constant representing the Parameter variable option.
        /// </summary>
        ParamVariable,

        /// <summary>
        ///     An enum constant representing the percentage [%] option.
        /// </summary>
        Percentage
    }
}