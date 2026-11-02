// ***********************************************************************
//  Assembly          : RzR.Shared.Entity.MathExprEngine
//  Author            : RzR
//  Created On        : 2026-09-03 22:09
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-10-31 15:37
//  ***********************************************************************
//  <copyright file="ExpressionTokenizer.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

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
    /// <summary>
    ///     An expression tokenizer.
    /// </summary>
    internal static class ExpressionTokenizer
    {
        /// <summary>
        ///     Splits the expression into tokens with their source columns.
        /// </summary>
        /// <exception cref="MathRuleEngineException">
        ///     Thrown when a number or character is invalid, or tokens exceed <see cref="MathRuleEngineOptions.MaxTokens" />.
        /// </exception>
        /// <param name="sourceExpression">Source expression.</param>
        /// <param name="options">
        ///     The token limit, or null for defaults.
        /// </param>
        /// <returns>
        ///     The tokens, ending with an end-of-text token.
        /// </returns>
        internal static IIndexableEnumerable<ExpressionToken> Tokenize(string sourceExpression,
            MathRuleEngineOptions options)
        {
            options = options ?? new MathRuleEngineOptions();

            var tokens = new List<ExpressionToken>();
            var i = 0;
            var n = sourceExpression.Length;

            static void AddToken(List<ExpressionToken> tokens, MathRuleEngineOptions options, ExpressionToken token)
            {
                tokens.Add(token);

                if (token.Kind != TokenKind.EndOfText && tokens.Count > options.MaxTokens)
                    throw new MathRuleEngineException(
                        DefaultMessages.TooManyTokens.FormatWith(tokens.Count, options.MaxTokens), token.Column);
            }

            while (true)
            {
                // Skip whitespace but count columns
                while (i < n && char.IsWhiteSpace(sourceExpression[i])) i++;

                if (i >= n)
                {
                    AddToken(tokens, options,
                        new ExpressionToken { Kind = TokenKind.EndOfText, Text = "", Column = i });
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

                    if (double.TryParse(txt, NumberStyles.Float, CultureInfo.InvariantCulture, out var val).IsFalse()
                        || double.IsInfinity(val) || double.IsNaN(val))
                        throw new MathRuleEngineException(DefaultMessages.TokenInvalidNumber.FormatWith(txt), start);

                    AddToken(tokens, options, new ExpressionToken
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
                    while (i < n && (char.IsLetterOrDigit(sourceExpression[i]) || sourceExpression[i] == '_'))
                        i++;

                    var name = sourceExpression.Substring(start, i - start);
                    var nextNonSpace = i;
                    while (nextNonSpace < n && char.IsWhiteSpace(sourceExpression[nextNonSpace]))
                        nextNonSpace++;

                    var kind = nextNonSpace < n && sourceExpression[nextNonSpace] == '('
                        ? TokenKind.Identifier // function call follows
                        : TokenKind.ParamVariable; // bare name → variable

                    AddToken(tokens, options, new ExpressionToken
                    {
                        Kind = kind,
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
                            AddToken(tokens, options, new ExpressionToken
                            {
                                Kind = TokenKind.LessEqual,
                                Text = two,
                                Column = col
                            });
                            i += 2;
                            continue;
                        case ">=":
                            AddToken(tokens, options, new ExpressionToken
                            {
                                Kind = TokenKind.GreaterEqual,
                                Text = two,
                                Column = col
                            });
                            i += 2;
                            continue;
                        case "==":
                            AddToken(tokens, options, new ExpressionToken
                            {
                                Kind = TokenKind.EqualEqual,
                                Text = two,
                                Column = col
                            });
                            i += 2;
                            continue;
                        case "!=":
                            AddToken(tokens, options, new ExpressionToken
                            {
                                Kind = TokenKind.NotEqual,
                                Text = two,
                                Column = col
                            });
                            i += 2;
                            continue;
                        case "&&":
                            AddToken(tokens, options, new ExpressionToken
                            {
                                Kind = TokenKind.AndAnd,
                                Text = two,
                                Column = col
                            });
                            i += 2;
                            continue;
                        case "||":
                            AddToken(tokens, options, new ExpressionToken
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
                        AddToken(tokens, options, new ExpressionToken
                        {
                            Kind = TokenKind.Plus,
                            Text = "+",
                            Column = col
                        });
                        i++;
                        break;
                    case '-':
                        AddToken(tokens, options, new ExpressionToken
                        {
                            Kind = TokenKind.Minus,
                            Text = "-",
                            Column = col
                        });
                        i++;
                        break;
                    case '*':
                        AddToken(tokens, options, new ExpressionToken
                        {
                            Kind = TokenKind.Star,
                            Text = "*",
                            Column = col
                        });
                        i++;
                        break;
                    case '/':
                        AddToken(tokens, options, new ExpressionToken
                        {
                            Kind = TokenKind.Slash,
                            Text = "/",
                            Column = col
                        });
                        i++;
                        break;
                    case '^':
                        AddToken(tokens, options, new ExpressionToken
                        {
                            Kind = TokenKind.Caret,
                            Text = "^",
                            Column = col
                        });
                        i++;
                        break;
                    case '(':
                        AddToken(tokens, options, new ExpressionToken
                        {
                            Kind = TokenKind.LParen,
                            Text = "(",
                            Column = col
                        });
                        i++;
                        break;
                    case ')':
                        AddToken(tokens, options, new ExpressionToken
                        {
                            Kind = TokenKind.RParen,
                            Text = ")",
                            Column = col
                        });
                        i++;
                        break;
                    case ',':
                        AddToken(tokens, options, new ExpressionToken
                        {
                            Kind = TokenKind.Comma,
                            Text = ",",
                            Column = col
                        });
                        i++;
                        break;
                    case '?':
                        AddToken(tokens, options, new ExpressionToken
                        {
                            Kind = TokenKind.Question,
                            Text = "?",
                            Column = col
                        });
                        i++;
                        break;
                    case ':':
                        AddToken(tokens, options, new ExpressionToken
                        {
                            Kind = TokenKind.Colon,
                            Text = ":",
                            Column = col
                        });
                        i++;
                        break;
                    case '<':
                        AddToken(tokens, options, new ExpressionToken
                        {
                            Kind = TokenKind.Less,
                            Text = "<",
                            Column = col
                        });
                        i++;
                        break;
                    case '>':
                        AddToken(tokens, options, new ExpressionToken
                        {
                            Kind = TokenKind.Greater,
                            Text = ">",
                            Column = col
                        });
                        i++;
                        break;
                    case '%':
                        AddToken(tokens, options, new ExpressionToken
                        {
                            Kind = TokenKind.Percentage,
                            Text = "%",
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