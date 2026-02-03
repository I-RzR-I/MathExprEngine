// ***********************************************************************
//  Assembly         : RzR.Shared.Entity.MathExprEngine
//  Author           : RzR
//  Created On       : 2026-01-18 18:01
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-01-18 18:53
// ***********************************************************************
//  <copyright file="Calculator.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using DomainCommonExtensions.ArraysExtensions;
using DomainCommonExtensions.DataTypeExtensions;
using DomainCommonExtensions.Utilities.Ensure;
using MathExprEngine.Enums;
using MathExprEngine.Exceptions;
using MathExprEngine.Helpers;
using System;
using System.Collections.Generic;

#endregion

namespace MathExprEngine
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     The mathematics rule engine.
    /// </summary>
    /// =================================================================================================
    public sealed class MathRuleEngine
    {
        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     (Immutable) the functions.
        /// </summary>
        /// =================================================================================================
        internal readonly Dictionary<string, Func<double[], double>> Functions =
            new Dictionary<string, Func<double[], double>>(StringComparer.OrdinalIgnoreCase);

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     The instance.
        /// </summary>
        /// =================================================================================================
        public static MathRuleEngine Instance = new MathRuleEngine();

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Initializes a new instance of the <see cref="MathRuleEngine"/> class.
        /// </summary>
        /// <param name="functions">(Optional) The functions.</param>
        /// =================================================================================================
        public MathRuleEngine(IDictionary<string, Func<double[], double>> functions = null)
        {
            RegisterFunctions(BuiltInFunctions.GetAll());

            if (functions.IsNotNullOrEmptyEnumerable())
                RegisterFunctions(functions);

        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Register a custom function available to expressions.
        /// </summary>
        /// <exception cref="ArgumentException" />
        /// <exception cref="ArgumentNullException" />
        /// <param name="name">The name.</param>
        /// <param name="impl">The function implementation.</param>
        /// =================================================================================================
        public void RegisterFunction(string name, Func<double[], double> impl)
        {
            DomainEnsure.IsNotNullOrEmpty(name, nameof(name), DefaultMessages.FuncNameIsMissing);
            DomainEnsure.IsNotNull(impl, nameof(impl));

            Functions.AddOrUpdate(name, impl);
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Register a custom functions available to expressions.
        /// </summary>
        /// <param name="functions">The functions.</param>
        /// =================================================================================================
        public void RegisterFunctions(IDictionary<string, Func<double[], double>> functions)
        {
            functions
                .NotNull()
                .ForEach(
                    func =>
                    {
                        RegisterFunction(func.Key, func.Value);
                    });
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Evaluate the math expression.
        /// </summary>
        /// <exception cref="ArgumentException" />
        /// <exception cref="MathRuleEngineException" />
        /// <param name="expression">The expression.</param>
        /// <returns>
        ///     A expression evaluation result (double value).
        /// </returns>
        /// =================================================================================================
        public double Evaluate(string expression)
        {
            DomainEnsure.IsNotNullOrEmpty(expression, nameof(expression));

            // Tokenize
            var tokens = ExpressionTokenizer.Tokenize(expression);

            // Parse
            var parser = new ExpressionParser(tokens);
            var ast = parser.ParseExpression();

            // Ensure we consumed all tokens (except EndOfText)
            var next = tokens[parser.Pos];

            if (next.Kind != TokenKind.EndOfText)
                throw new ExpressionSyntaxException(DefaultMessages.UnexceptedTokenAtTheEnd.FormatWith(next.Text), next.Column);

            return ast.Evaluate(this);
        }
    }
}

