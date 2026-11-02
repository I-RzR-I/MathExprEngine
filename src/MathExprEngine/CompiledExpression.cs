// ***********************************************************************
//  Assembly          : RzR.Shared.Entity.MathExprEngine
//  Author            : RzR
//  Created On        : 2026-10-21 20:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-10-31 15:50
//  ***********************************************************************
//  <copyright file="CompiledExpression.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using System.Collections.Generic;
using RzR.Extensions.Domain.Validation;
using MathExprEngine.Compilation;
using MathExprEngine.Exceptions;

#endregion

namespace MathExprEngine
{
    /// <summary>
    ///     A thread-safe, re-entrant <see cref="MathRuleEngine.Compile(string)" /> handle using its engine's live functions.
    /// </summary>
    public sealed class CompiledExpression
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="CompiledExpression" /> class.
        /// </summary>
        /// <param name="expression">The raw expression text.</param>
        /// <param name="program">The prepared program for <paramref name="expression" />.</param>
        internal CompiledExpression(string expression, ExpressionProgram program)
        {
            DomainEnsure.IsNotNull(expression, nameof(expression));
            DomainEnsure.IsNotNull(program, nameof(program));

            Expression = expression;
            Program = program;
        }

        /// <summary>
        ///     Gets the expression text this handle was compiled from.
        /// </summary>
        /// <value>
        ///     The expression text.
        /// </value>
        public string Expression { get; }

        /// <summary>
        ///     Gets the prepared program behind this handle (test hook).
        /// </summary>
        /// <value>
        ///     The program.
        /// </value>
        internal ExpressionProgram Program { get; }

        /// <summary>
        ///     Gets a value indicating whether the program runs as a compiled delegate (test hook).
        /// </summary>
        /// <value>
        ///     True when the program is compiled; false when it is interpreted.
        /// </value>
        internal bool IsCompiled => Program.IsCompiled;

        /// <summary>
        ///     Evaluates exactly as <see cref="MathRuleEngine.Evaluate" /> does, retrying a transiently failed compile.
        /// </summary>
        /// <exception cref="ExpressionSyntaxException">
        ///     Thrown when a referenced parameter is not supplied.
        /// </exception>
        /// <exception cref="ExpressionFunctionException">
        ///     Thrown when an evaluated function is unknown or fails.
        /// </exception>
        /// <exception cref="MathRuleEngineException">
        ///     Thrown when a rule fails: zero divisor, non-finite, too deep, bad parameter.
        /// </exception>
        /// <param name="parameters">(Optional) the parameters (variables).</param>
        /// <returns>
        ///     The expression evaluation result.
        /// </returns>
        public double Evaluate(IDictionary<string, object> parameters = null)
        {
            Program.TryTierUp();

            return Program.Evaluate(parameters);
        }

        /// <summary>
        ///     Returns the expression text.
        /// </summary>
        /// <returns>
        ///     The expression text.
        /// </returns>
        public override string ToString()
        {
            return Expression;
        }
    }
}