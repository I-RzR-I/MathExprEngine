// ***********************************************************************
//  Assembly          : RzR.Shared.Entity.MathExprEngine
//  Author            : RzR
//  Created On        : 2026-09-03 22:09
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-10-31 15:37
//  ***********************************************************************
//  <copyright file="BuiltInFunctions.cs" company="RzR SOFT & TECH">
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
using System.Linq;
using DomainCommonExtensions.DataTypeExtensions;
using MathExprEngine.Exceptions;

#endregion

namespace MathExprEngine.Helpers
{
    /// <summary>
    ///     A build-in functions.
    /// </summary>
    internal static class BuiltInFunctions
    {
        /// <summary>
        ///     (Immutable) the functions.
        /// </summary>
        private static readonly IDictionary<string, Func<double[], double>> Functions =
            new Dictionary<string, Func<double[], double>>(StringComparer.OrdinalIgnoreCase)
            {
                {
                    "sin", args =>
                    {
                        if (args.Length != 1)
                            throw new FunctionArgumentException("sin", 1);

                        return Math.Sin(args[0]);
                    }
                },
                {
                    "cos", args =>
                    {
                        if (args.Length != 1)
                            throw new FunctionArgumentException("cos", 1);

                        return Math.Cos(args[0]);
                    }
                },
                {
                    "sqrt", args =>
                    {
                        if (args.Length != 1)
                            throw new FunctionArgumentException("sqrt", 1);

                        if (args[0] < 0.0)
                            throw new ExpressionFunctionException(
                                DefaultMessages.FunctionDomainError.FormatWith("sqrt", args[0]));

                        return Math.Sqrt(args[0]);
                    }
                },
                {
                    "pow", args =>
                    {
                        if (args.Length != 2)
                            throw new FunctionArgumentException("pow", 2);

                        return Math.Pow(args[0], args[1]);
                    }
                },
                {
                    "max", args =>
                    {
                        if (args.Length == 0)
                            throw FunctionArgumentException.ForMinimumArity("max", 1);

                        return args.Max();
                    }
                },
                {
                    "sum", args =>
                    {
                        if (args.Length == 0)
                            throw FunctionArgumentException.ForMinimumArity("sum", 1);

                        return args.Sum();
                    }
                },
                {
                    "neg", args =>
                    {
                        if (args.Length != 1)
                            throw new FunctionArgumentException("neg", 1);

                        return -args[0];
                    }
                },
                {
                    "mod", args =>
                    {
                        if (args.Length != 2)
                            throw new FunctionArgumentException("mod", 2);

                        if (args[1] == 0.0)
                            throw new MathRuleEngineException(DefaultMessages.ModuloByZero);

                        return args[0] % args[1];
                    }
                }
            };

        /// <summary>
        ///     Gets a copy of all registered built-in functions. A copy is returned so callers cannot
        ///     mutate the engine's internal built-in table.
        /// </summary>
        /// <returns>
        ///     A new dictionary containing all built-in functions.
        /// </returns>
        internal static IDictionary<string, Func<double[], double>> GetAll()
        {
            return new Dictionary<string, Func<double[], double>>(Functions, StringComparer.OrdinalIgnoreCase);
        }
    }
}