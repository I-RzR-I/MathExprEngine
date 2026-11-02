// ***********************************************************************
//  Assembly          : RzR.Shared.Entity.MathExprEngine
//  Author            : RzR
//  Created On        : 2026-10-22 20:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-10-31 15:40
//  ***********************************************************************
//  <copyright file="ParameterBinder.cs" company="RzR SOFT & TECH">
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
using DomainCommonExtensions.ArraysExtensions;
using DomainCommonExtensions.Utilities.Ensure;
using MathExprEngine.Exceptions;
using MathExprEngine.Helpers;

#endregion

namespace MathExprEngine.Compilation
{
    /// <summary>
    ///     An immutable, thread-safe binder from caller parameters to compiled-delegate slots.
    /// </summary>
    internal sealed class ParameterBinder
    {
        /// <summary>
        ///     (Immutable) the number of slots.
        /// </summary>
        private readonly int _slotCount;

        /// <summary>
        ///     (Immutable) the slot index of each parameter name, case-insensitive.
        /// </summary>
        private readonly Dictionary<string, int> _slotIndex;

        /// <summary>
        ///     (Immutable) the parameter name of each slot, in slot order.
        /// </summary>
        private readonly string[] _slotNames;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ParameterBinder" /> class.
        /// </summary>
        /// <param name="slotNames">
        ///     The ordered slot names, distinct ignoring case (copied).
        /// </param>
        internal ParameterBinder(string[] slotNames)
        {
            DomainEnsure.IsNotNull(slotNames, nameof(slotNames));

            _slotCount = slotNames.Length;
            _slotNames = (string[])slotNames.Clone();
            _slotIndex = new Dictionary<string, int>(slotNames.Length, StringComparer.OrdinalIgnoreCase);
            
            for (var i = 0; i < slotNames.Length; i++)
                _slotIndex.Add(slotNames[i], i);
        }

        /// <summary>
        ///     Copies the caller's parameters into slots, matching names case-insensitively.
        /// </summary>
        /// <exception cref="MathRuleEngineException">
        ///     Thrown when reading <paramref name="parameters" /> fails, such as on concurrent mutation.
        /// </exception>
        /// <param name="parameters">The caller-supplied parameters.</param>
        /// <returns>
        ///     The slots, <see cref="CompiledRuntime.Missing" /> where unsupplied; shared and empty if no slots.
        /// </returns>
        internal object[] Bind(IDictionary<string, object> parameters)
        {
            var slots = CreateSlots();

            try
            {
                if (parameters.IsNotNullOrEmptyEnumerable())
                    return slots;

                if (_slotCount > 0 && IsCaseInsensitiveDictionary(parameters))
                {
                    var dictionary = (Dictionary<string, object>)parameters;
                    for (var i = 0; i < _slotNames.Length; i++)
                        if (dictionary.TryGetValue(_slotNames[i], out var value))
                            slots[i] = value;

                    return slots;
                }

                foreach (var pair in parameters)
                    if (_slotIndex.TryGetValue(pair.Key, out var index))
                        slots[index] = pair.Value;
            }
            catch (Exception ex)
            {
                throw NodeSemantics.ParametersModifiedDuringCopy(ex);
            }

            return slots;
        }

        /// <summary>
        ///     Checks whether <paramref name="parameters" /> is exactly a <see cref="Dictionary{TKey,TValue}" />
        ///     with <see cref="StringComparer.OrdinalIgnoreCase" />.
        /// </summary>
        /// <param name="parameters">The caller-supplied parameters.</param>
        /// <returns>
        ///     True when direct lookup matches the interpreter's case-insensitive binding.
        /// </returns>
        private static bool IsCaseInsensitiveDictionary(IDictionary<string, object> parameters)
        {
            return parameters.GetType() == typeof(Dictionary<string, object>)
                   && ReferenceEquals(((Dictionary<string, object>)parameters).Comparer,
                       StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        ///     Creates a slot array with every slot set to <see cref="CompiledRuntime.Missing" />.
        /// </summary>
        /// <returns>
        ///     A new slot array, or the shared empty array for zero slots.
        /// </returns>
        private object[] CreateSlots()
        {
            if (_slotCount == 0)
                return Array.Empty<object>();

            var slots = new object[_slotCount];
            for (var i = 0; i < slots.Length; i++)
                slots[i] = CompiledRuntime.Missing;

            return slots;
        }
    }
}