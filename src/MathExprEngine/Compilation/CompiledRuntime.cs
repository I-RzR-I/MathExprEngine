// ***********************************************************************
//  Assembly          : RzR.Shared.Entity.MathExprEngine
//  Author            : RzR
//  Created On        : 2026-10-22 20:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-10-31 15:39
//  ***********************************************************************
//  <copyright file="CompiledRuntime.cs" company="RzR SOFT & TECH">
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
using System.Reflection;
using DomainCommonExtensions.DataTypeExtensions;
using MathExprEngine.Exceptions;
using MathExprEngine.Helpers;
using MathExprEngine.Models;

#endregion

namespace MathExprEngine.Compilation
{
    /// <summary>
    ///     The helpers and cached <see cref="MethodInfo" /> targets compiled delegates call.
    /// </summary>
    internal static class CompiledRuntime
    {
        /// <summary>
        ///     (Immutable) the binding flags for resolving the library's helpers.
        /// </summary>
        private const BindingFlags InternalStatic = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

        /// <summary>
        ///     (Immutable) the slot sentinel for an unsupplied parameter.
        /// </summary>
        internal static readonly object Missing = new();

        /// <summary>
        ///     (Immutable) the <see cref="NodeSemantics.EnsureFinite" /> method.
        /// </summary>
        internal static readonly MethodInfo EnsureFiniteMethod = Require(
            typeof(NodeSemantics).GetMethod(nameof(NodeSemantics.EnsureFinite), InternalStatic, null,
                new[] { typeof(double), typeof(int) }, null),
            nameof(NodeSemantics),
            nameof(NodeSemantics.EnsureFinite));

        /// <summary>
        ///     (Immutable) the <see cref="NodeSemantics.IsTrue" /> method.
        /// </summary>
        internal static readonly MethodInfo IsTrueMethod = Require(
            typeof(NodeSemantics).GetMethod(nameof(NodeSemantics.IsTrue), InternalStatic, null,
                new[] { typeof(double), typeof(int) }, null),
            nameof(NodeSemantics),
            nameof(NodeSemantics.IsTrue));

        /// <summary>
        ///     (Immutable) the <see cref="NodeSemantics.Divide" /> method.
        /// </summary>
        internal static readonly MethodInfo DivideMethod = Require(
            typeof(NodeSemantics).GetMethod(nameof(NodeSemantics.Divide), InternalStatic, null,
                new[] { typeof(double), typeof(double), typeof(int) }, null),
            nameof(NodeSemantics),
            nameof(NodeSemantics.Divide));

        /// <summary>
        ///     (Immutable) the <see cref="NodeSemantics.Modulo" /> method.
        /// </summary>
        internal static readonly MethodInfo ModuloMethod = Require(
            typeof(NodeSemantics).GetMethod(nameof(NodeSemantics.Modulo), InternalStatic, null,
                new[] { typeof(double), typeof(double), typeof(int) }, null),
            nameof(NodeSemantics),
            nameof(NodeSemantics.Modulo));

        /// <summary>
        ///     (Immutable) the <see cref="NodeSemantics.ResolveFunction" /> method.
        /// </summary>
        internal static readonly MethodInfo ResolveFunctionMethod = Require(
            typeof(NodeSemantics).GetMethod(nameof(NodeSemantics.ResolveFunction),
                InternalStatic,
                null,
                new[] { typeof(IDictionary<string, Func<double[], double>>), typeof(string), typeof(int) },
                null),
            nameof(NodeSemantics),
            nameof(NodeSemantics.ResolveFunction));

        /// <summary>
        ///     (Immutable) the <see cref="ReadParameter" /> method.
        /// </summary>
        internal static readonly MethodInfo ReadParameterMethod = Require(
            typeof(CompiledRuntime).GetMethod(nameof(ReadParameter), InternalStatic, null,
                new[] { typeof(object), typeof(string), typeof(int) }, null),
            nameof(CompiledRuntime),
            nameof(ReadParameter));

        /// <summary>
        ///     (Immutable) the <see cref="InvokeFunction" /> method.
        /// </summary>
        internal static readonly MethodInfo InvokeFunctionMethod = Require(
            typeof(CompiledRuntime).GetMethod(nameof(InvokeFunction),
                InternalStatic,
                null,
                new[]
                {
                    typeof(Func<double[], double>), typeof(string), typeof(int), typeof(double[]), typeof(int),
                    typeof(int)
                },
                null),
            nameof(CompiledRuntime),
            nameof(InvokeFunction));

        /// <summary>
        ///     (Immutable) the <see cref="ThrowUnknownOperator" /> method.
        /// </summary>
        internal static readonly MethodInfo ThrowUnknownOperatorMethod = Require(
            typeof(CompiledRuntime).GetMethod(nameof(ThrowUnknownOperator), InternalStatic, null,
                new[] { typeof(string), typeof(int) }, null),
            nameof(CompiledRuntime),
            nameof(ThrowUnknownOperator));

        /// <summary>
        ///     (Immutable) the <see cref="Math.Pow" /> method, the interpreter's call for <c>^</c>.
        /// </summary>
        internal static readonly MethodInfo PowMethod = Require(
            typeof(Math).GetMethod(nameof(Math.Pow), BindingFlags.Static | BindingFlags.Public, null,
                new[] { typeof(double), typeof(double) }, null),
            nameof(Math),
            nameof(Math.Pow));

        /// <summary>
        ///     Reads a parameter slot and converts it to a number.
        /// </summary>
        /// <exception cref="ExpressionSyntaxException">
        ///     Thrown when the parameter was not supplied.
        /// </exception>
        /// <exception cref="MathRuleEngineException">
        ///     Thrown when the value is null or unconvertible to a number.
        /// </exception>
        /// <param name="raw">The slot value, or <see cref="Missing" />.</param>
        /// <param name="name">The parameter name.</param>
        /// <param name="column">The parameter column.</param>
        /// <returns>
        ///     The numeric value.
        /// </returns>
        internal static double ReadParameter(object raw, string name, int column)
        {
            if (ReferenceEquals(raw, Missing))
                throw NodeSemantics.UndefinedVariable(name, column);

            return NodeSemantics.CoerceParameter(name, raw, column);
        }

        /// <summary>
        ///     Invokes a host function at its node depth, so re-entrant calls share one budget.
        /// </summary>
        /// <param name="impl">The resolved function implementation.</param>
        /// <param name="name">The function name as written in the expression.</param>
        /// <param name="column">The column of the function call, for error reporting.</param>
        /// <param name="args">The evaluated arguments, freshly allocated for this call.</param>
        /// <param name="baseDepth">The starting evaluation depth, restored afterwards.</param>
        /// <param name="nodeDepth">The static depth of the function node (the root is <c>1</c>).</param>
        /// <returns>
        ///     The function result.
        /// </returns>
        internal static double InvokeFunction(Func<double[], double> impl, string name,
            int column, double[] args, int baseDepth, int nodeDepth)
        {
            EvaluationContext.RestoreDepth(baseDepth + nodeDepth);
            try
            {
                return NodeSemantics.InvokeFunction(impl, name, column, args);
            }
            finally
            {
                EvaluationContext.RestoreDepth(baseDepth);
            }
        }

        /// <summary>
        ///     Throws the interpreter's unknown-operator error, unreachable for parser-produced trees.
        /// </summary>
        /// <exception cref="MathRuleEngineException">Always thrown.</exception>
        /// <param name="message">The formatted unknown-operator message.</param>
        /// <param name="column">The column of the operator node.</param>
        /// <returns>
        ///     Never returns.
        /// </returns>
        internal static double ThrowUnknownOperator(string message, int column)
        {
            throw NodeSemantics.UnknownOperator(message, column);
        }

        /// <summary>
        ///     Ensures a helper was resolved, failing type initialisation otherwise.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when <paramref name="method" /> is null.
        /// </exception>
        /// <param name="method">The resolved method, or null.</param>
        /// <param name="typeName">The declaring type name, for the error message.</param>
        /// <param name="methodName">The method name, for the error message.</param>
        /// <returns>
        ///     The same <paramref name="method" />.
        /// </returns>
        private static MethodInfo Require(MethodInfo method, string typeName, string methodName)
        {
            if (method == null)
                throw new InvalidOperationException(
                    DefaultMessages.CompiledHelperMissing.FormatWith(typeName, methodName));

            return method;
        }
    }
}