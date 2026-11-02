// ***********************************************************************
//  Assembly          : RzR.Shared.Entity.MathExprEngine
//  Author            : RzR
//  Created On        : 2026-10-22 20:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-10-31 15:37
//  ***********************************************************************
//  <copyright file="ExpressionCache.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using RzR.Extensions.Domain.Primitives;
using MathExprEngine.Compilation;

#endregion

namespace MathExprEngine.Helpers
{
    /// <summary>
    ///     A bounded two-generation program cache with lock-free current-generation hits.
    /// </summary>
    internal sealed class ExpressionCache
    {
        /// <summary>
        ///     (Immutable) the maximum AST nodes retained across both generations.
        /// </summary>
        internal const int NodeBudget = 262144;

        /// <summary>
        ///     (Immutable) the largest initial size of a generation.
        /// </summary>
        private const int PresizeCeiling = 4096;

        /// <summary>
        ///     (Immutable) the maximum number of entries across both generations.
        /// </summary>
        private readonly int _capacity;

        /// <summary>
        ///     (Immutable) the new-insert count that triggers a rotation.
        /// </summary>
        private readonly int _generationSize;

        /// <summary>
        ///     (Immutable) the lock guarding inserts, moves, rotations and counters.
        /// </summary>
        private readonly object _insertLock = new();

        /// <summary>
        ///     (Immutable) a value indicating whether rotations keep a previous generation (capacity above 1).
        /// </summary>
        private readonly bool _keepPrevious;

        /// <summary>
        ///     (Immutable) the initial size of each generation.
        /// </summary>
        private readonly int _presize;

        /// <summary>
        ///     The current generation, replaced only under <see cref="_insertLock" />.
        /// </summary>
        private volatile ConcurrentDictionary<string, ExpressionProgram> _current;

        /// <summary>
        ///     The current generation's entry count, kept under <see cref="_insertLock" />.
        /// </summary>
        private int _currentEntries;

        /// <summary>
        ///     The new-insert count since the last rotation, kept under <see cref="_insertLock" />.
        /// </summary>
        private int _currentInserts;

        /// <summary>
        ///     The current generation's AST node count, kept under <see cref="_insertLock" />.
        /// </summary>
        private int _currentNodes;

        /// <summary>
        ///     The resumable eviction scan or null, under <see cref="_insertLock" />.
        /// </summary>
        private IEnumerator<KeyValuePair<string, ExpressionProgram>> _evictionCursor;

        /// <summary>
        ///     The eviction pass (<c>0</c> none, <c>1</c> cold, <c>2</c> any), under <see cref="_insertLock" />.
        /// </summary>
        private int _evictionPass;

        /// <summary>
        ///     The previous generation or null, replaced only under <see cref="_insertLock" />.
        /// </summary>
        private volatile ConcurrentDictionary<string, ExpressionProgram> _previous;

        /// <summary>
        ///     The previous generation's entry count, kept under <see cref="_insertLock" />.
        /// </summary>
        private int _previousEntries;

        /// <summary>
        ///     The previous generation's AST node count, kept under <see cref="_insertLock" />.
        /// </summary>
        private int _previousNodes;

        /// <summary>
        ///     The mutation version, release-written under <see cref="_insertLock" /> on each change.
        /// </summary>
        private int _version;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ExpressionCache" /> class.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown when <paramref name="capacity" /> is less than <c>1</c>.
        /// </exception>
        /// <param name="capacity">The maximum number of cached entries across both generations.</param>
        internal ExpressionCache(int capacity)
        {
            if (capacity < 1)
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, null);

            _capacity = capacity;
            _keepPrevious = capacity > 1;
            _generationSize = _keepPrevious ? capacity / 2 : 1;
            _presize = Math.Min(capacity, PresizeCeiling);
            _current = CreateGeneration();
        }

        /// <summary>
        ///     Gets the exact number of cached entries (test hook).
        /// </summary>
        /// <value>
        ///     The entry count.
        /// </value>
        internal int Count
        {
            get
            {
                lock (_insertLock)
                {
                    return _currentEntries + _previousEntries;
                }
            }
        }

        /// <summary>
        ///     Gets the number of AST nodes retained across both generations (test hook).
        /// </summary>
        /// <value>
        ///     The retained node count.
        /// </value>
        internal int RetainedNodeCount
        {
            get
            {
                lock (_insertLock)
                {
                    return _currentNodes + _previousNodes;
                }
            }
        }

        /// <summary>
        ///     Checks whether either generation caches the text, without side effects.
        /// </summary>
        /// <param name="expression">The raw expression text.</param>
        /// <param name="program">[out] The cached program, or null on a miss.</param>
        /// <returns>
        ///     True on a hit in either generation.
        /// </returns>
        internal bool TryPeek(string expression, out ExpressionProgram program)
        {
            if (_current.TryGetValue(expression, out program))
                return true;

            var previous = _previous;

            return previous != null && previous.TryGetValue(expression, out program);
        }

        /// <summary>
        ///     Returns the cached program, creating it on a miss.
        /// </summary>
        /// <param name="expression">The raw expression text.</param>
        /// <param name="factory">
        ///     The prepare step, run outside the lock, caching nothing if it throws.
        /// </param>
        /// <returns>
        ///     The program for <paramref name="expression" />, shared by racing callers.
        /// </returns>
        internal ExpressionProgram GetOrAdd(string expression, Func<string, ExpressionProgram> factory)
        {
            // Read before the lookups: if no mutation completes between here and the insert lock, both
            // misses still hold and the re-check under the lock (two more string hashes) can be skipped.
            var version = Volatile.Read(ref _version);

            if (_current.TryGetValue(expression, out var program))
                return program;

            var previous = _previous;
            if (previous != null && previous.TryGetValue(expression, out program))
            {
                lock (_insertLock)
                {
                    if (TryGetOrMoveToCurrent(expression, out var settled))
                        return settled;
                }

                return program;
            }

            var created = factory(expression);

            lock (_insertLock)
            {
                if (_version != version && TryGetOrMoveToCurrent(expression, out var existing))
                    return existing;

                InsertNew(expression, created);

                return created;
            }
        }

        /// <summary>
        ///     Finds or moves the text into the current generation, under <see cref="_insertLock" />.
        /// </summary>
        /// <param name="expression">The raw expression text.</param>
        /// <param name="program">[out] The program now held by the current generation, or null.</param>
        /// <returns>
        ///     True when the current generation holds the text on return.
        /// </returns>
        private bool TryGetOrMoveToCurrent(string expression, out ExpressionProgram program)
        {
            if (_current.TryGetValue(expression, out program))
                return true;

            var previous = _previous;
            if (previous == null || !previous.TryGetValue(expression, out program))
                return false;

            _current[expression] = program;
            previous.TryRemove(expression, out _);

            _currentEntries++;
            _currentNodes += program.NodeCount;
            _previousEntries--;
            _previousNodes -= program.NodeCount;
            Volatile.Write(ref _version, _version + 1);

            return true;
        }

        /// <summary>
        ///     Inserts a new program within the bounds, under <see cref="_insertLock" />.
        /// </summary>
        /// <param name="expression">The raw expression text.</param>
        /// <param name="program">The program to insert.</param>
        private void InsertNew(string expression, ExpressionProgram program)
        {
            var nodes = program.NodeCount;

            if (_currentInserts >= _generationSize)
                Rotate();

            while (IsFull(nodes))
                if (_previousEntries == 0)
                    Rotate();
                else
                    EvictFromPrevious(nodes);

            _current[expression] = program;
            _currentEntries++;
            _currentNodes += nodes;
            _currentInserts++;
            Volatile.Write(ref _version, _version + 1);
        }

        /// <summary>
        ///     Checks whether the cache lacks room, under <see cref="_insertLock" />.
        /// </summary>
        /// <param name="nodes">The node count of the entry to add.</param>
        /// <returns>
        ///     True when there is no room for the entry.
        /// </returns>
        private bool IsFull(int nodes)
        {
            var entries = _currentEntries + _previousEntries;

            return entries >= _capacity || (entries > 0 && _currentNodes + _previousNodes + nodes > NodeBudget);
        }

        /// <summary>
        ///     Evicts previous-generation entries, cold first, dropping them all if needed.
        /// </summary>
        /// <param name="nodes">The node count of the entry to add.</param>
        private void EvictFromPrevious(int nodes)
        {
            var previous = _previous;
            if (previous.IsNull())
                return;

            while (IsFull(nodes))
            {
                if (_evictionCursor.IsNull())
                {
                    if (_evictionPass >= 2)
                        break;

                    _evictionCursor = previous.GetEnumerator();
                    _evictionPass++;
                }

                if (!_evictionCursor!.MoveNext())
                {
                    _evictionCursor.Dispose();
                    _evictionCursor = null;

                    continue;
                }

                var candidate = _evictionCursor.Current;
                if (_evictionPass == 1 && !candidate.Value.CanTierUp)
                    continue;

                if (!previous.TryRemove(candidate.Key, out var removed))
                    continue;

                _previousEntries--;
                _previousNodes -= removed.NodeCount;
            }

            if (IsFull(nodes))
                DropPrevious();
        }

        /// <summary>
        ///     Discards the previous generation and its cursor, under <see cref="_insertLock" />.
        /// </summary>
        private void DropPrevious()
        {
            _previous = null;
            _previousEntries = 0;
            _previousNodes = 0;

            ResetEvictionCursor();
        }

        /// <summary>
        ///     Releases the eviction cursor and resets the pass, under <see cref="_insertLock" />.
        /// </summary>
        private void ResetEvictionCursor()
        {
            _evictionCursor?.Dispose();
            _evictionCursor = null;
            _evictionPass = 0;
        }

        /// <summary>
        ///     Demotes or discards the current generation and starts an empty one, under <see cref="_insertLock" />.
        /// </summary>
        private void Rotate()
        {
            if (_keepPrevious)
            {
                ResetEvictionCursor();
                _previous = _current;
                _previousEntries = _currentEntries;
                _previousNodes = _currentNodes;
            }
            else
            {
                DropPrevious();
            }

            _current = CreateGeneration();
            _currentEntries = 0;
            _currentNodes = 0;
            _currentInserts = 0;
        }

        /// <summary>
        ///     Creates an empty, pre-sized generation keyed by ordinal text.
        /// </summary>
        /// <returns>
        ///     The new generation.
        /// </returns>
        private ConcurrentDictionary<string, ExpressionProgram> CreateGeneration()
        {
            return new ConcurrentDictionary<string, ExpressionProgram>(Environment.ProcessorCount, _presize,
                StringComparer.Ordinal);
        }
    }
}