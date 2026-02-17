// ***********************************************************************
//  Assembly         : RzR.Shared.Entity.MathExprEngine
//  Author           : RzR
//  Created On       : 2026-01-18 18:01
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-01-18 18:57
// ***********************************************************************
//  <copyright file="Parser.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using System.Collections.Generic;
using DomainCommonExtensions.Collections;
using DomainCommonExtensions.DataTypeExtensions;
using MathExprEngine.Enums;
using MathExprEngine.Exceptions;
using MathExprEngine.Models;
using MathExprEngine.Nodes;

#endregion

namespace MathExprEngine.Helpers
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     An expression parser.
    /// </summary>
    /// =================================================================================================
    internal class ExpressionParser
    {
        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     (Immutable) the tokens.
        /// </summary>
        /// =================================================================================================
        private readonly IIndexableEnumerable<ExpressionToken> _tokens;

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     The position.
        /// </summary>
        /// =================================================================================================
        internal int Pos;

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Initializes a new instance of the <see cref="ExpressionParser"/> class.
        /// </summary>
        /// <param name="tokens">(Immutable) the tokens.</param>
        /// =================================================================================================
        public ExpressionParser(IIndexableEnumerable<ExpressionToken> tokens)
        {
            _tokens = tokens;
            Pos = 0;
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Parse expression.
        /// </summary>
        /// <returns>
        ///     A Node.
        /// </returns>
        /// =================================================================================================
        public NodeBase ParseExpression()
        {
            return ParseConditional();
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Returns the object at the position 'Pos'.
        /// </summary>
        /// <returns>
        ///     The current object at the position 'Pos'.
        /// </returns>
        /// =================================================================================================
        private ExpressionToken Peek()
        {
            return _tokens[Pos];
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Move to the next item in the collection.
        /// </summary>
        /// <returns>
        ///     An ExpressionToken.
        /// </returns>
        /// =================================================================================================
        private ExpressionToken Next()
        {
            var t = _tokens[Pos];
            if (t.Kind != TokenKind.EndOfText) Pos++;

            return t;
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Matches the given kind.
        /// </summary>
        /// <param name="kind">The kind.</param>
        /// <returns>
        ///     True if it succeeds, false if it fails.
        /// </returns>
        /// =================================================================================================
        private bool Match(TokenKind kind)
        {
            if (Peek().Kind == kind)
            {
                Next();

                return true;
            }

            return false;
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Expects.
        /// </summary>
        /// <exception cref="MathRuleEngineException">
        ///     Thrown when the Mathematics Rule Engine error condition occurs.
        /// </exception>
        /// <param name="kind">The token kind.</param>
        /// <param name="messageIfFail">(Optional) The failure message.</param>
        /// =================================================================================================
        private void Expect(TokenKind kind, string messageIfFail = null)
        {
            if (Match(kind).IsFalse())
            {
                var token = Peek();

                throw new ExpressionSyntaxException(messageIfFail.IfNullOrWhiteSpace($"Expected {kind}, found {token.Kind}"), token.Column);
            }
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Parse conditional.
        /// </summary>
        /// <list type="">
        /// </list>
        /// <remarks>
        ///     conditional := logicalOr [ '?' expression ':' conditional ] <br/>
        ///     Note: right-associative for ternary chains: a ? b : c ? d : e  => a ? b : (c ? d : e)
        /// </remarks>
        /// <returns>
        ///     A Node.
        /// </returns>
        /// =================================================================================================
        private NodeBase ParseConditional()
        {
            var cond = ParseLogicalOr();

            if (Match(TokenKind.Question).IsTrue())
            {
                var trueExpr = ParseExpression(); // expression after '?'
                Expect(TokenKind.Colon, "Expected ':' in conditional operator");
                var falseExpr = ParseConditional(); // right-associative: parse another conditional on RHS

                return new ConditionalNode(cond, trueExpr, falseExpr, cond.Column);
            }

            return cond;
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Parse logical OR.
        /// </summary>
        /// <remarks>
        ///     logicalOr := logicalAnd ( '||' logicalAnd )*
        /// </remarks>
        /// <returns>
        ///     A Node.
        /// </returns>
        /// =================================================================================================
        private NodeBase ParseLogicalOr()
        {
            var node = ParseLogicalAnd();

            while (Match(TokenKind.OrOr).IsTrue())
            {
                var opToken = _tokens[Pos - 1];
                var right = ParseLogicalAnd();

                node = new BinaryNode("||", node, right, opToken.Column);
            }

            return node;
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Parse logical and.
        /// </summary>
        /// <remarks>
        ///     logicalAnd := comparison ( '&&' comparison )*
        /// </remarks>
        /// <returns>
        ///     A Node.
        /// </returns>
        /// =================================================================================================
        private NodeBase ParseLogicalAnd()
        {
            var node = ParseComparison();

            while (Match(TokenKind.AndAnd).IsTrue())
            {
                var opToken = _tokens[Pos - 1];
                var right = ParseComparison();

                node = new BinaryNode("&&", node, right, opToken.Column);
            }

            return node;
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Parse comparison.
        /// </summary>
        /// <remarks>
        ///     comparison := additive ( ( &lt; &gt; &lt;= &gt;= == != ) additive )*
        /// </remarks>
        /// <returns>
        ///     A Node.
        /// </returns>
        /// =================================================================================================
        private NodeBase ParseComparison()
        {
            var node = ParseAdditive();

            while (true)
            {
                var tokenKind = Peek().Kind;
                string operationText;
                var col = Peek().Column;
                if (tokenKind == TokenKind.Less)
                {
                    Next();
                    operationText = "<";
                }
                else if (tokenKind == TokenKind.Greater)
                {
                    Next();
                    operationText = ">";
                }
                else if (tokenKind == TokenKind.LessEqual)
                {
                    Next();
                    operationText = "<=";
                }
                else if (tokenKind == TokenKind.GreaterEqual)
                {
                    Next();
                    operationText = ">=";
                }
                else if (tokenKind == TokenKind.EqualEqual)
                {
                    Next();
                    operationText = "==";
                }
                else if (tokenKind == TokenKind.NotEqual)
                {
                    Next();
                    operationText = "!=";
                }
                else
                {
                    break;
                }

                var right = ParseAdditive();
                node = new BinaryNode(operationText, node, right, col);
            }

            return node;
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Parse additive.
        /// </summary>
        /// <remarks>
        ///     additive := multiplicative ( ('+' | '-') multiplicative )*
        /// </remarks>
        /// <returns>
        ///     A Node.
        /// </returns>
        /// =================================================================================================
        private NodeBase ParseAdditive()
        {
            var node = ParseMultiplicative();

            while (true)
            {
                if (Match(TokenKind.Plus).IsTrue())
                {
                    var opToken = _tokens[Pos - 1];
                    var right = ParseMultiplicative();

                    node = new BinaryNode("+", node, right, opToken.Column);
                    continue;
                }

                if (Match(TokenKind.Minus).IsTrue())
                {
                    var opToken = _tokens[Pos - 1];
                    var right = ParseMultiplicative();

                    node = new BinaryNode("-", node, right, opToken.Column);
                    continue;
                }

                break;
            }

            return node;
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Parse multiplicative.
        /// </summary>
        /// <remarks>
        ///     multiplicative := power ( ('*' | '/') power )*
        /// </remarks>
        /// <returns>
        ///     A NodeBase.
        /// </returns>
        /// =================================================================================================
        private NodeBase ParseMultiplicative()
        {
            var node = ParsePower();
            while (true)
            {
                if (Match(TokenKind.Star).IsTrue())
                {
                    var opToken = _tokens[Pos - 1];
                    var right = ParsePower();

                    node = new BinaryNode("*", node, right, opToken.Column);
                    continue;
                }

                if (Match(TokenKind.Slash))
                {
                    var opToken = _tokens[Pos - 1];
                    var right = ParsePower();

                    node = new BinaryNode("/", node, right, opToken.Column);
                    continue;
                }

                break;
            }

            return node;
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Parse power.
        /// </summary>
        /// <remarks>
        ///     power := unary ( '^' power )?   // right-associative
        /// </remarks>
        /// <returns>
        ///     A Node.
        /// </returns>
        /// =================================================================================================
        private NodeBase ParsePower()
        {
            var left = ParseUnary();
            if (Match(TokenKind.Caret).IsTrue())
            {
                var opToken = _tokens[Pos - 1];
                var right = ParsePower(); // right-assoc

                return new BinaryNode("^", left, right, opToken.Column);
            }

            return left;
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Parse unary.
        /// </summary>
        /// <remarks>
        ///     unary := ('+' | '-') unary | primary
        /// </remarks>
        /// <returns>
        ///     A Node.
        /// </returns>
        /// =================================================================================================
        private NodeBase ParseUnary()
        {
            if (Match(TokenKind.Plus).IsTrue())
            {
                var opToken = _tokens[Pos - 1];
                var operand = ParseUnary();

                return new UnaryNode("+", operand, opToken.Column);
            }

            if (Match(TokenKind.Minus).IsTrue())
            {
                var opToken = _tokens[Pos - 1];
                var operand = ParseUnary();

                return new UnaryNode("-", operand, opToken.Column);
            }

            return ParsePrimary();
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Parse primary.
        /// </summary>
        /// <exception cref="MathRuleEngineException">
        ///     Thrown when the Mathematics Rule Engine error condition occurs.
        /// </exception>
        /// <remarks>
        ///      primary := number
        ///          | identifier '(' arglist? ')'
        ///          | '(' expression ')'
        /// </remarks>
        /// <returns>
        ///     A Node.
        /// </returns>
        /// =================================================================================================
        private NodeBase ParsePrimary()
        {
            var token = Peek();

            if (Match(TokenKind.Number).IsTrue())
            {
                var tk = _tokens[Pos - 1];

                return new NumberNode(tk.Number, tk.Column);
            }

            if (Match(TokenKind.Identifier).IsTrue())
            {
                var id = _tokens[Pos - 1];
                
                // If next is '(', it's a function call
                if (Match(TokenKind.LParen).IsTrue())
                {
                    var args = new List<NodeBase>();
                    if (Match(TokenKind.RParen).IsFalse())
                    {
                        // Parse first argument
                        args.Add(ParseExpression());
                        
                        // Parse remaining comma-separated args
                        while (Match(TokenKind.Comma).IsTrue()) 
                            args.Add(ParseExpression());
                        Expect(TokenKind.RParen, "Missing closing ')' after function arguments");
                    }

                    return new FunctionNode(id.Text, args, id.Column);
                }

                // We treat bare identifiers as error (we only support functions)
                throw new ExpressionSyntaxException(
                    $"Unexpected identifier '{id.Text}' (functions must be called like name(...))", id.Column);
            }

            if (Match(TokenKind.ParamVariable).IsTrue())
            {
                var id = _tokens[Pos - 1];

                return new ParameterNode(id.Text, id.Column);
            }

            if (Match(TokenKind.LParen).IsTrue())
            {
                var start = _tokens[Pos - 1];
                var expr = ParseExpression();

                Expect(TokenKind.RParen, "Missing closing ')'");

                return expr;
            }

            // Unexpected token
            var peekedToken = Peek();

            throw new ExpressionSyntaxException($"Unexpected token '{peekedToken.Text}'", peekedToken.Column);
        }
    }
}

