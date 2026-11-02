// ***********************************************************************
//  Assembly          : RzR.Shared.Entity.MathExprEngine
//  Author            : RzR
//  Created On        : 2026-10-22 20:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-10-31 15:37
//  ***********************************************************************
//  <copyright file="NodeSemantics.cs" company="RzR SOFT & TECH">
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
using System.Globalization;
using DomainCommonExtensions.DataTypeExtensions;
using MathExprEngine.Exceptions;

#endregion

namespace MathExprEngine.Helpers
{
    /// <summary>
    ///     The shared throwing operations for interpreted and compiled evaluation.
    /// </summary>
    internal static class NodeSemantics
    {
        /// <summary>
        ///     Ensures the value is finite, regardless of <see cref="MathRuleEngineOptions.AllowNonFiniteResults" />.
        /// </summary>
        /// <exception cref="MathRuleEngineException">
        ///     Thrown when the value is <c>NaN</c> or <c>Infinity</c>.
        /// </exception>
        /// <param name="value">The value to validate.</param>
        /// <param name="column">
        ///     The producing node's column, for error reporting.
        /// </param>
        /// <returns>
        ///     The same <paramref name="value" />.
        /// </returns>
        internal static double EnsureFinite(double value, int column)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new MathRuleEngineException(DefaultMessages.NonFiniteResult, column);

            return value;
        }

        /// <summary>
        ///     Checks whether a control-flow operand is non-zero, rejecting non-finite values.
        /// </summary>
        /// <exception cref="MathRuleEngineException">
        ///     Thrown when the value is <c>NaN</c> or <c>Infinity</c>, regardless of
        ///     <see cref="MathRuleEngineOptions.AllowNonFiniteResults" />.
        /// </exception>
        /// <param name="value">The condition value.</param>
        /// <param name="column">The column reported when the value is not finite.</param>
        /// <returns>
        ///     True when the value is non-zero.
        /// </returns>
        internal static bool IsTrue(double value, int column)
        {
            EnsureFinite(value, column);

            return value != 0.0;
        }

        /// <summary>
        ///     Divides <paramref name="left" /> by <paramref name="right" />.
        /// </summary>
        /// <exception cref="MathRuleEngineException">
        ///     Thrown when <paramref name="right" /> is zero.
        /// </exception>
        /// <param name="left">The dividend.</param>
        /// <param name="right">The divisor.</param>
        /// <param name="column">The column of the operator, for error reporting.</param>
        /// <returns>
        ///     The quotient.
        /// </returns>
        internal static double Divide(double left, double right, int column)
        {
            if (right == 0.0)
                throw new MathRuleEngineException(DefaultMessages.DivisionByZero, column);

            return left / right;
        }

        /// <summary>
        ///     Computes the remainder of <paramref name="left" /> divided by <paramref name="right" />.
        /// </summary>
        /// <exception cref="MathRuleEngineException">
        ///     Thrown when <paramref name="right" /> is zero.
        /// </exception>
        /// <param name="left">The dividend.</param>
        /// <param name="right">The divisor.</param>
        /// <param name="column">The column of the operator, for error reporting.</param>
        /// <returns>
        ///     The remainder.
        /// </returns>
        internal static double Modulo(double left, double right, int column)
        {
            if (right == 0.0)
                throw new MathRuleEngineException(DefaultMessages.ModuloByZero, column);

            return left % right;
        }

        /// <summary>
        ///     Creates the exception for an undefined parameter.
        /// </summary>
        /// <param name="name">The parameter name as written in the expression.</param>
        /// <param name="column">The column of the parameter, for error reporting.</param>
        /// <returns>
        ///     The exception to throw.
        /// </returns>
        internal static ExpressionSyntaxException UndefinedVariable(string name, int column)
        {
            return new ExpressionSyntaxException(DefaultMessages.UndefinedVariable.FormatWith(name), column);
        }

        /// <summary>
        ///     Creates the exception for a failed parameter copy.
        /// </summary>
        /// <param name="inner">The exception raised by the caller's dictionary.</param>
        /// <returns>
        ///     The exception to throw.
        /// </returns>
        internal static MathRuleEngineException ParametersModifiedDuringCopy(Exception inner)
        {
            return new MathRuleEngineException(DefaultMessages.ParametersModifiedDuringCopy, inner);
        }

        /// <summary>
        ///     Creates the exception for an unknown operator.
        /// </summary>
        /// <param name="message">The formatted unknown-operator message.</param>
        /// <param name="column">The column of the operator node.</param>
        /// <returns>
        ///     The exception to throw.
        /// </returns>
        internal static MathRuleEngineException UnknownOperator(string message, int column)
        {
            return new MathRuleEngineException(message, column);
        }

        /// <summary>
        ///     Converts a parameter value to a number.
        /// </summary>
        /// <exception cref="MathRuleEngineException">
        ///     Thrown when the value is null or unconvertible to a number.
        /// </exception>
        /// <param name="name">The parameter name as written in the expression.</param>
        /// <param name="raw">The supplied value.</param>
        /// <param name="column">The column of the parameter, for error reporting.</param>
        /// <returns>
        ///     The numeric value.
        /// </returns>
        internal static double CoerceParameter(string name, object raw, int column)
        {
            if (raw == null)
                throw new MathRuleEngineException(DefaultMessages.ParameterNullValue.FormatWith(name), column);

            switch (raw)
            {
                case double d: return d;
                case float f: return f;
                case decimal m: return (double)m;
                case int i: return i;
                case long l: return l;
                case short s: return s;
                case sbyte sb: return sb;
                case byte b: return b;
                case ushort us: return us;
                case uint ui: return ui;
                case ulong ul: return ul;
                case bool bo: return bo ? 1.0 : 0.0;
                case string st when double.TryParse(
                    st,
                    NumberStyles.Float | NumberStyles.AllowThousands,
                    CultureInfo.InvariantCulture,
                    out var parsed):
                    return parsed;
                default:
                    throw new MathRuleEngineException(
                        DefaultMessages.ParameterNotConvertible.FormatWith(name, raw.GetType().Name), column);
            }
        }

        /// <summary>
        ///     Resolves a function by name in the engine's live registry.
        /// </summary>
        /// <exception cref="ExpressionFunctionException">
        ///     Thrown when no function with the given name is registered.
        /// </exception>
        /// <param name="functions">The engine's live function registry.</param>
        /// <param name="name">The function name as written in the expression.</param>
        /// <param name="column">The column of the function call, for error reporting.</param>
        /// <returns>
        ///     The function implementation.
        /// </returns>
        internal static Func<double[], double> ResolveFunction(
            IDictionary<string, Func<double[], double>> functions,
            string name,
            int column)
        {
            if (!functions.TryGetValue(name, out var impl))
                throw new ExpressionFunctionException(DefaultMessages.UnknownFunction.FormatWith(name), column);

            return impl;
        }

        /// <summary>
        ///     Invokes a resolved function, translating its failures into engine exceptions.
        /// </summary>
        /// <exception cref="ExpressionFunctionException">
        ///     Thrown when the function throws a <see cref="FunctionArgumentException" /> or non-engine exception.
        /// </exception>
        /// <param name="impl">The function implementation.</param>
        /// <param name="name">The function name as written in the expression.</param>
        /// <param name="column">The column of the function call, for error reporting.</param>
        /// <param name="args">The evaluated arguments.</param>
        /// <returns>
        ///     The function result.
        /// </returns>
        internal static double InvokeFunction(Func<double[], double> impl, string name, int column, double[] args)
        {
            try
            {
                return impl(args);
            }
            catch (FunctionArgumentException ex)
            {
                throw new ExpressionFunctionException(
                    DefaultMessages.FunctionError.FormatWith(name, ex.Message), column, ex);
            }
            catch (MathExprEngineException ex) when (ex.Column < 0)
            {
                throw ex.WithColumn(column);
            }
            catch (Exception ex) when (!(ex is MathExprEngineException))
            {
                throw new ExpressionFunctionException(
                    DefaultMessages.FunctionError.FormatWith(name, ex.Message), column, ex);
            }
        }
    }
}