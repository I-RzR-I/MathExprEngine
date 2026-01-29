// ***********************************************************************
//  Assembly         : RzR.Shared.Entity.MathExprEngine
//  Author           : RzR
//  Created On       : 2026-01-18 18:01
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-01-18 21:25
// ***********************************************************************
//  <copyright file="BuiltInFunctions.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using System;
using System.Collections.Generic;
using System.Linq;
using DomainCommonExtensions.ArraysExtensions;
using DomainCommonExtensions.DataTypeExtensions;
using MathExprEngine.Exceptions;

#endregion

namespace MathExprEngine.Helpers
{
    /// -------------------------------------------------------------------------------------------------
    /// <summary>
    ///     A build-in functions.
    /// </summary>
    /// =================================================================================================
    internal static class BuiltInFunctions
    {
        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     (Immutable) the functions.
        /// </summary>
        /// =================================================================================================
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
                        var max = args.Max();

                        return max;
                    }
                },
                {
                    "sum", args =>
                    {
                        var sum = args.Sum();

                        return sum;
                    }
                },
                {
                    "neg", args =>
                    {
                        if (args.Length != 1)
                            throw new FunctionArgumentException("neg", 1);

                        return -args[0];
                    }
                }
            };

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Sets a function.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="func">The function.</param>
        /// =================================================================================================
        internal static void SetFunc(string name, Func<double[], double> func)
        {
            if (name.IsMissing() || Functions.ContainsKey(name))
                return;

            Functions.AddIfNotExist(name, func);
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets a function.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>
        ///     A function delegate that yields a double.
        /// </returns>
        /// =================================================================================================
        internal static Func<double[], double> GetFunc(string name)
        {
            return Functions.GetValueOrDefault(name);
        }

        /// -------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Gets all.
        /// </summary>
        /// <returns>
        ///     all.
        /// </returns>
        /// =================================================================================================
        internal static IDictionary<string, Func<double[], double>> GetAll()
        {
            return Functions ?? new Dictionary<string, Func<double[], double>>();
        }
    }
}