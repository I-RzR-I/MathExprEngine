// ***********************************************************************
//  Assembly         : RzR.Shared.Entity.MathExprEngine
//  Author           : RzR
//  Created On       : 2026-01-18 20:01
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-01-18 20:10
// ***********************************************************************
//  <copyright file="ExpressionTokenizer.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using System.Collections.Generic;
using System.Globalization;
using DomainCommonExtensions.Collections;
using DomainCommonExtensions.DataTypeExtensions;
using MathExprEngine.Enums;
using MathExprEngine.Exceptions;
using MathExprEngine.Models;

#endregion

namespace MathExprEngine.Helpers
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     An expression tokenizer.
    /// </summary>
    /// =================================================================================================
    internal static class ExpressionTokenizer
    {
        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Tokenize the input string(expression), producing tokens with column info.
        /// </summary>
        /// <exception cref="MathRuleEngineException">
        ///     Thrown when the Mathematics Rule Engine error condition occurs.
        /// </exception>
        /// <param name="sourceExpression">Source expression.</param>
        /// <returns>
        ///     An IIndexableEnumerable&lt;ExpressionToken&gt;
        /// </returns>
        /// =================================================================================================
        internal static IIndexableEnumerable<ExpressionToken> Tokenize(string sourceExpression)
        {
            var tokens = new List<ExpressionToken>();
            var i = 0;
            var n = sourceExpression.Length;

            while (true)
            {
                // Skip whitespace but count columns
                while (i < n && char.IsWhiteSpace(sourceExpression[i])) i++;

                if (i >= n)
                {
                    tokens.Add(new ExpressionToken { Kind = TokenKind.EndOfText, Text = "", Column = i });
                    break;
                }

                var c = sourceExpression[i];
                var col = i;

                // Numbers: digits and optional single dot (no exponent notation for simplicity)
                if (char.IsDigit(c) || c == '.')
                {
                    var start = i;
                    var sawDot = false;
                    while (i < n && (char.IsDigit(sourceExpression[i]) || sourceExpression[i] == '.'))
                    {
                        if (sourceExpression[i] == '.')
                        {
                            if (sawDot)
                                throw new MathRuleEngineException(DefaultMessages.TokenInvalidNumericMultipleDot, i);
                            sawDot = true;
                        }

                        i++;
                    }

                    var txt = sourceExpression.Substring(start, i - start);
                    if (double.TryParse(txt, NumberStyles.Float, CultureInfo.InvariantCulture, out var val).IsFalse())
                        throw new MathRuleEngineException(DefaultMessages.TokenInvalidNumber.FormatWith(txt), start);

                    tokens.Add(new ExpressionToken
                    {
                        Kind = TokenKind.Number,
                        Text = txt,
                        Number = val, 
                        Column = col
                    });
                    continue;
                }

                // Identifiers: letters only (function names)
                if (char.IsLetter(c).IsTrue())
                {
                    var start = i;
                    while (i < n && char.IsLetter(sourceExpression[i])) 
                        i++;

                    var name = sourceExpression.Substring(start, i - start);

                    tokens.Add(new ExpressionToken
                    {
                        Kind = TokenKind.Identifier, 
                        Text = name,
                        Column = col
                    });
                    continue;
                }

                // Multi-character operators
                if (i + 1 < n)
                {
                    var two = sourceExpression.Substring(i, 2);
                    switch (two)
                    {
                        case "<=":
                            tokens.Add(new ExpressionToken
                            {
                                Kind = TokenKind.LessEqual, 
                                Text = two, 
                                Column = col
                            });
                            i += 2;
                            continue;
                        case ">=":
                            tokens.Add(new ExpressionToken
                            {
                                Kind = TokenKind.GreaterEqual, 
                                Text = two, 
                                Column = col
                            });
                            i += 2;
                            continue;
                        case "==":
                            tokens.Add(new ExpressionToken
                            {
                                Kind = TokenKind.EqualEqual, 
                                Text = two, 
                                Column = col
                            });
                            i += 2;
                            continue;
                        case "!=":
                            tokens.Add(new ExpressionToken
                            {
                                Kind = TokenKind.NotEqual, 
                                Text = two,
                                Column = col
                            });
                            i += 2;
                            continue;
                        case "&&":
                            tokens.Add(new ExpressionToken
                            {
                                Kind = TokenKind.AndAnd,
                                Text = two, 
                                Column = col
                            });
                            i += 2;
                            continue;
                        case "||":
                            tokens.Add(new ExpressionToken
                            {
                                Kind = TokenKind.OrOr,
                                Text = two, 
                                Column = col
                            });
                            i += 2;
                            continue;
                    }
                }

                // Single-character tokens
                switch (c)
                {
                    case '+':
                        tokens.Add(new ExpressionToken
                        {
                            Kind = TokenKind.Plus, 
                            Text = "+",
                            Column = col
                        });
                        i++;
                        break;
                    case '-':
                        tokens.Add(new ExpressionToken
                        {
                            Kind = TokenKind.Minus,
                            Text = "-", 
                            Column = col
                        });
                        i++;
                        break;
                    case '*':
                        tokens.Add(new ExpressionToken
                        {
                            Kind = TokenKind.Star,
                            Text = "*", 
                            Column = col
                        });
                        i++;
                        break;
                    case '/':
                        tokens.Add(new ExpressionToken
                        {
                            Kind = TokenKind.Slash,
                            Text = "/",
                            Column = col
                        });
                        i++;
                        break;
                    case '^':
                        tokens.Add(new ExpressionToken
                        {
                            Kind = TokenKind.Caret,
                            Text = "^", 
                            Column = col
                        });
                        i++;
                        break;
                    case '(':
                        tokens.Add(new ExpressionToken
                        {
                            Kind = TokenKind.LParen,
                            Text = "(", 
                            Column = col
                        });
                        i++;
                        break;
                    case ')':
                        tokens.Add(new ExpressionToken
                        {
                            Kind = TokenKind.RParen,
                            Text = ")", 
                            Column = col
                        });
                        i++;
                        break;
                    case ',':
                        tokens.Add(new ExpressionToken
                        {
                            Kind = TokenKind.Comma,
                            Text = ",",
                            Column = col
                        });
                        i++;
                        break;
                    case '?':
                        tokens.Add(new ExpressionToken
                        {
                            Kind = TokenKind.Question,
                            Text = "?", 
                            Column = col
                        });
                        i++;
                        break;
                    case ':':
                        tokens.Add(new ExpressionToken
                        {
                            Kind = TokenKind.Colon,
                            Text = ":",
                            Column = col
                        });
                        i++;
                        break;
                    case '<':
                        tokens.Add(new ExpressionToken
                        {
                            Kind = TokenKind.Less,
                            Text = "<",
                            Column = col
                        });
                        i++;
                        break;
                    case '>':
                        tokens.Add(new ExpressionToken
                        {
                            Kind = TokenKind.Greater, 
                            Text = ">", 
                            Column = col
                        });
                        i++;
                        break;
                    default:
                        throw new MathRuleEngineException(DefaultMessages.TokenUnexpectedCharacter.FormatWith(c), col);
                }
            }

            return new IndexableEnumerable<ExpressionToken>(tokens);
        }
    }
}

