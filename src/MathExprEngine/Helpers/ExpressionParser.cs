// ***********************************************************************
//  Assembly          : RzR.Shared.Entity.MathExprEngine
//  Author            : RzR
//  Created On        : 2026-10-22 20:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-10-31 15:37
//  ***********************************************************************
//  <copyright file="ExpressionParser.cs" company="RzR SOFT & TECH">
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
using System.Runtime.CompilerServices;
using DomainCommonExtensions.Collections;
using DomainCommonExtensions.DataTypeExtensions;
using MathExprEngine.Enums;
using MathExprEngine.Exceptions;
using MathExprEngine.Models;
using MathExprEngine.Nodes;

#endregion

namespace MathExprEngine.Helpers
{
    /// <summary>
    ///     An expression parser.
    /// </summary>
    internal sealed class ExpressionParser
    {
        /// <summary>
        ///     (Immutable) the resource limits applied while parsing.
        /// </summary>
        private readonly MathRuleEngineOptions _options;

        /// <summary>
        ///     (Immutable) the tokens.
        /// </summary>
        private readonly IIndexableEnumerable<ExpressionToken> _tokens;

        /// <summary>
        ///     The current grammar recursion depth, capped by <see cref="MathRuleEngineOptions.MaxDepth" />.
        /// </summary>
        private int _depth;

        /// <summary>
        ///     The number of AST nodes built, capped by <see cref="MathRuleEngineOptions.MaxNodes" />.
        /// </summary>
        private int _nodeCount;

        /// <summary>
        ///     The position.
        /// </summary>
        internal int Pos;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ExpressionParser" /> class.
        /// </summary>
        /// <param name="tokens">The tokens to parse.</param>
        /// <param name="options">
        ///     The parse limits, or null for defaults.
        /// </param>
        public ExpressionParser(IIndexableEnumerable<ExpressionToken> tokens, MathRuleEngineOptions options)
        {
            _tokens = tokens;
            _options = options ?? new MathRuleEngineOptions();
            Pos = 0;
        }

        /// <summary>
        ///     Gets the number of AST nodes built so far.
        /// </summary>
        /// <value>
        ///     The node count; the AST size after a successful parse.
        /// </value>
        internal int NodeCount => _nodeCount;

        /// <summary>
        ///     Parse expression.
        /// </summary>
        /// <returns>
        ///     A Node.
        /// </returns>
        public NodeBase ParseExpression()
        {
            return ParseConditional();
        }

        /// <summary>
        ///     Returns the object at the position 'Pos'.
        /// </summary>
        /// <returns>
        ///     The current object at the position 'Pos'.
        /// </returns>
        private ExpressionToken Peek()
        {
            return _tokens[Pos];
        }

        /// <summary>
        ///     Returns the object at a position offset from 'Pos'.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <returns>
        ///     The token at the requested offset.
        /// </returns>
        private ExpressionToken Peek(int offset)
        {
            return _tokens[Pos + offset];
        }

        /// <summary>
        ///     Move to the next item in the collection.
        /// </summary>
        /// <returns>
        ///     An ExpressionToken.
        /// </returns>
        private ExpressionToken Next()
        {
            var t = _tokens[Pos];
            if (t.Kind != TokenKind.EndOfText) Pos++;

            return t;
        }

        /// <summary>
        ///     Matches the given kind.
        /// </summary>
        /// <param name="kind">The kind.</param>
        /// <returns>
        ///     True if it succeeds, false if it fails.
        /// </returns>
        private bool Match(TokenKind kind)
        {
            if (Peek().Kind == kind)
            {
                Next();

                return true;
            }

            return false;
        }

        /// <summary>
        ///     Expects.
        /// </summary>
        /// <exception cref="ExpressionSyntaxException">
        ///     Thrown when an Expression Syntax error condition occurs.
        /// </exception>
        /// <exception cref="MathRuleEngineException">
        ///     Thrown when the Mathematics Rule Engine error condition occurs.
        /// </exception>
        /// <param name="kind">The token kind.</param>
        /// <param name="messageIfFail">(Optional) The failure message.</param>
        private void Expect(TokenKind kind, string messageIfFail = null)
        {
            if (Match(kind).IsFalse())
            {
                var token = Peek();

                throw new ExpressionSyntaxException(
                    messageIfFail.IfNullOrWhiteSpace(
                        DefaultMessages.ExpectedTokenFoundToken.FormatWith(kind, token.Kind)), token.Column);
            }
        }

        /// <summary>
        ///     Counts a new AST node against the node budget.
        /// </summary>
        /// <exception cref="MathRuleEngineException">
        ///     Thrown when the number of constructed nodes exceeds
        ///     <see cref="MathRuleEngineOptions.MaxNodes" />.
        /// </exception>
        /// <typeparam name="T">The node type.</typeparam>
        /// <param name="node">The node just constructed.</param>
        /// <returns>
        ///     The same <paramref name="node" />, for use in a fluent return expression.
        /// </returns>
        private T Track<T>(T node) where T : NodeBase
        {
            if (++_nodeCount > _options.MaxNodes)
                throw new MathRuleEngineException(
                    DefaultMessages.NodeBudgetExceeded.FormatWith(_options.MaxNodes), node.Column);

            return node;
        }

        /// <summary>
        ///     Enters a recursive grammar rule, counting depth against the limit.
        /// </summary>
        /// <exception cref="MathRuleEngineException">
        ///     Thrown when depth exceeds <see cref="MathRuleEngineOptions.MaxDepth" /> or the stack runs low.
        /// </exception>
        private void EnterRule()
        {
            if (++_depth > _options.MaxDepth)
            {
                _depth--;

                throw new MathRuleEngineException(
                    DefaultMessages.ParseDepthExceeded.FormatWith(_options.MaxDepth), Peek().Column);
            }

            try
            {
                RuntimeHelpers.EnsureSufficientExecutionStack();
            }
            catch (InsufficientExecutionStackException ex)
            {
                _depth--;

                throw new MathRuleEngineException(
                    DefaultMessages.ParseStackExhausted.FormatWith(_options.MaxDepth), Peek().Column, ex);
            }
        }

        /// <summary>
        ///     Exits a self-recursive grammar rule previously entered with <see cref="EnterRule" />.
        /// </summary>
        private void ExitRule()
        {
            _depth--;
        }

        /// <summary>
        ///     Parse conditional.
        /// </summary>
        /// <remarks>
        ///     conditional := logicalOr [ '?' expression ':' conditional ] <br />
        ///     Note: right-associative for ternary chains: a ? b : c ? d : e  =&gt; a ? b : (c ? d : e)
        /// </remarks>
        /// <returns>
        ///     A Node.
        /// </returns>
        private NodeBase ParseConditional()
        {
            EnterRule();
            try
            {
                var cond = ParseLogicalOr();

                if (Match(TokenKind.Question).IsTrue())
                {
                    var trueExpr = ParseExpression(); // expression after '?'
                    Expect(TokenKind.Colon, DefaultMessages.ExpectedColonInConditional);
                    var falseExpr = ParseConditional(); // right-associative: parse another conditional on RHS

                    return Track(new ConditionalNode(cond, trueExpr, falseExpr, cond.Column));
                }

                return cond;
            }
            finally
            {
                ExitRule();
            }
        }

        /// <summary>
        ///     Parse logical OR.
        /// </summary>
        /// <remarks>
        ///     logicalOr := logicalAnd ( '||' logicalAnd )*.
        /// </remarks>
        /// <returns>
        ///     A Node.
        /// </returns>
        private NodeBase ParseLogicalOr()
        {
            var node = ParseLogicalAnd();

            while (Match(TokenKind.OrOr).IsTrue())
            {
                var opToken = _tokens[Pos - 1];
                var right = ParseLogicalAnd();

                node = Track(new BinaryNode("||", node, right, opToken.Column));
            }

            return node;
        }

        /// <summary>
        ///     Parse logical and.
        /// </summary>
        /// <remarks>
        ///     logicalAnd := comparison ( '&amp;&amp;' comparison )*.
        /// </remarks>
        /// <returns>
        ///     A Node.
        /// </returns>
        private NodeBase ParseLogicalAnd()
        {
            var node = ParseComparison();

            while (Match(TokenKind.AndAnd).IsTrue())
            {
                var opToken = _tokens[Pos - 1];
                var right = ParseComparison();

                node = Track(new BinaryNode("&&", node, right, opToken.Column));
            }

            return node;
        }

        /// <summary>
        ///     Parse comparison.
        /// </summary>
        /// <remarks>
        ///     comparison := additive ( ( &lt; &gt; &lt;= &gt;= == != ) additive )*.
        /// </remarks>
        /// <returns>
        ///     A Node.
        /// </returns>
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
                node = Track(new BinaryNode(operationText, node, right, col));
            }

            return node;
        }

        /// <summary>
        ///     Parse additive.
        /// </summary>
        /// <remarks>
        ///     additive := multiplicative ( ('+' | '-') multiplicative )*.
        /// </remarks>
        /// <returns>
        ///     A Node.
        /// </returns>
        private NodeBase ParseAdditive()
        {
            var node = ParseMultiplicative();

            while (true)
            {
                if (Match(TokenKind.Plus).IsTrue())
                {
                    var opToken = _tokens[Pos - 1];
                    var right = ParseMultiplicative();

                    node = Track(new BinaryNode("+", node, right, opToken.Column));
                    continue;
                }

                if (Match(TokenKind.Minus).IsTrue())
                {
                    var opToken = _tokens[Pos - 1];
                    var right = ParseMultiplicative();

                    node = Track(new BinaryNode("-", node, right, opToken.Column));
                    continue;
                }

                break;
            }

            return node;
        }

        /// <summary>
        ///     Parse multiplicative.
        /// </summary>
        /// <remarks>
        ///     multiplicative := unary ( ('*' | '/' | '%') unary )*.
        /// </remarks>
        /// <returns>
        ///     A NodeBase.
        /// </returns>
        private NodeBase ParseMultiplicative()
        {
            var node = ParseUnary();
            while (true)
            {
                if (Match(TokenKind.Star).IsTrue())
                {
                    var opToken = _tokens[Pos - 1];
                    var right = ParseUnary();

                    node = Track(new BinaryNode("*", node, right, opToken.Column));
                    continue;
                }

                if (Match(TokenKind.Slash).IsTrue())
                {
                    var opToken = _tokens[Pos - 1];
                    var right = ParseUnary();

                    node = Track(new BinaryNode("/", node, right, opToken.Column));
                    continue;
                }

                if (Match(TokenKind.Percentage).IsTrue())
                {
                    var opToken = _tokens[Pos - 1];
                    var right = ParseUnary();

                    node = Track(new BinaryNode("%", node, right, opToken.Column));
                    continue;
                }

                break;
            }

            return node;
        }

        /// <summary>
        ///     Parse power.
        /// </summary>
        /// <remarks>
        ///     power := postfix ( '^' unary )?   // right-associative.
        /// </remarks>
        /// <returns>
        ///     A Node.
        /// </returns>
        private NodeBase ParsePower()
        {
            var left = ParsePostfix();
            if (Match(TokenKind.Caret).IsTrue())
            {
                var opToken = _tokens[Pos - 1];
                var right = ParseUnary(); // right-assoc, routes back through unary so '2^-3' works

                return Track(new BinaryNode("^", left, right, opToken.Column));
            }

            return left;
        }

        /// <summary>
        ///     Parse unary.
        /// </summary>
        /// <remarks>
        ///     unary := ('+' | '-') unary | power.
        /// </remarks>
        /// <returns>
        ///     A Node.
        /// </returns>
        private NodeBase ParseUnary()
        {
            EnterRule();
            try
            {
                if (Match(TokenKind.Plus).IsTrue())
                {
                    var opToken = _tokens[Pos - 1];
                    var operand = ParseUnary();

                    return Track(new UnaryNode("+", operand, opToken.Column));
                }

                if (Match(TokenKind.Minus).IsTrue())
                {
                    var opToken = _tokens[Pos - 1];
                    var operand = ParseUnary();

                    return Track(new UnaryNode("-", operand, opToken.Column));
                }

                return ParsePower();
            }
            finally
            {
                ExitRule();
            }
        }

        /// <summary>
        ///     Parse postfix.
        /// </summary>
        /// <remarks>
        ///     postfix := primary ('%')*.
        /// </remarks>
        /// <returns>
        ///     A Node.
        /// </returns>
        private NodeBase ParsePostfix()
        {
            var node = ParsePrimary();

            while (Peek().Kind == TokenKind.Percentage && IsPostfixPercentage().IsTrue())
            {
                var opToken = Next();
                node = Track(new UnaryNode("%", node, opToken.Column));
            }

            return node;
        }

        /// <summary>
        ///     Checks whether a token kind can begin an operand.
        /// </summary>
        /// <param name="kind">The token kind to test.</param>
        /// <returns>
        ///     True if the kind can begin an operand, false otherwise.
        /// </returns>
        private static bool CanStartOperand(TokenKind kind)
        {
            switch (kind)
            {
                case TokenKind.Number:
                case TokenKind.ParamVariable:
                case TokenKind.Identifier:
                case TokenKind.LParen:
                case TokenKind.Plus:
                case TokenKind.Minus:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        ///     Determine if the current percentage token should be treated as a postfix operator. A '%'
        ///     is postfix if and only if the token immediately following it cannot begin an operand;
        ///     otherwise it is left unconsumed so <see cref="ParseMultiplicative" /> reads it as infix
        ///     modulo.
        /// </summary>
        /// <returns>
        ///     True if it succeeds, false if it fails.
        /// </returns>
        private bool IsPostfixPercentage()
        {
            return !CanStartOperand(Peek(1).Kind);
        }

        /// <summary>
        ///     Parse primary.
        /// </summary>
        /// <remarks>
        ///     primary := number
        ///     | identifier '(' arglist? ')'
        ///     | paramVariable
        ///     | '(' expression ')'.
        /// </remarks>
        /// <exception cref="MathRuleEngineException">
        ///     Thrown when the Mathematics Rule Engine error condition occurs.
        /// </exception>
        /// <exception cref="ExpressionSyntaxException">
        ///     Thrown when an Expression Syntax error condition occurs.
        /// </exception>
        /// <returns>
        ///     A Node.
        /// </returns>
        private NodeBase ParsePrimary()
        {
            if (Match(TokenKind.Number).IsTrue())
            {
                var tk = _tokens[Pos - 1];

                return Track(new NumberNode(tk.Number, tk.Column));
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
                        {
                            if (args.Count >= _options.MaxFunctionArgs)
                                throw new MathRuleEngineException(
                                    DefaultMessages.FunctionTooManyArgs.FormatWith(id.Text, _options.MaxFunctionArgs),
                                    id.Column);

                            args.Add(ParseExpression());
                        }

                        Expect(TokenKind.RParen, DefaultMessages.MissingClosingParenAfterArgs);
                    }

                    return Track(new FunctionNode(id.Text, args, id.Column));
                }

                // We treat bare identifiers as error (we only support functions)
                throw new ExpressionSyntaxException(
                    DefaultMessages.UnexpectedIdentifier.FormatWith(id.Text), id.Column);
            }

            if (Match(TokenKind.ParamVariable).IsTrue())
            {
                var id = _tokens[Pos - 1];

                return Track(new ParameterNode(id.Text, id.Column));
            }

            if (Match(TokenKind.LParen).IsTrue())
            {
                var expr = ParseExpression();

                Expect(TokenKind.RParen, DefaultMessages.MissingClosingParen);

                return expr;
            }

            // Unexpected token
            var peekedToken = Peek();

            throw new ExpressionSyntaxException(DefaultMessages.UnexpectedToken.FormatWith(peekedToken.Text),
                peekedToken.Column);
        }
    }
}