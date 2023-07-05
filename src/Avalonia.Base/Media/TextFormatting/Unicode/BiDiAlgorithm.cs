// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.
// Ported from: https://github.com/SixLabors/Fonts/

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting.Unicode
{
    /// <summary>
    /// Implementation of Unicode bidirectional algorithm (UAX #9)
    /// https://unicode.org/reports/tr9/
    /// </summary>
    /// <remarks>
    /// <para>
    /// The Bidi algorithm uses a number of memory arrays for resolved
    /// types, level information, bracket types, x9 removal maps and
    /// more...
    /// </para>
    /// <para>
    /// This implementation of the BiDi algorithm has been designed
    /// to reduce memory pressure on the GC by re-using the same
    /// work buffers, so instances of this class should be re-used
    /// as much as possible.
    /// </para>
    /// </remarks>
    internal sealed class BidiAlgorithm
    {
        /// <summary>
        /// Whether the state is clean and can be reused without a reset.
        /// </summary>
        private bool _hasCleanState = true;

        /// <summary>
        /// The original BiDiClass classes as provided by the caller
        /// </summary>
        private ArraySlice<BidiClass> _originalClasses;

        /// <summary>
        /// Paired bracket types as provided by caller
        /// </summary>
        private ArraySlice<BidiPairedBracketType> _pairedBracketTypes;

        /// <summary>
        /// Paired bracket values as provided by caller
        /// </summary>
        private ArraySlice<int> _pairedBracketValues;

        /// <summary>
        /// Try if the incoming data is known to contain brackets
        /// </summary>
        private bool _hasBrackets;

        /// <summary>
        /// True if the incoming data is known to contain embedding runs
        /// </summary>
        private bool _hasEmbeddings;

        /// <summary>
        /// True if the incoming data is known to contain isolating runs
        /// </summary>
        private bool _hasIsolates;

        /// <summary>
        /// Two directional mapping of isolate start/end pairs
        /// </summary>
        /// <remarks>
        /// The forward mapping maps the start index to the end index.
        /// The reverse mapping maps the end index to the start index.
        /// </remarks>
        private readonly BidiDictionary<int, int> _isolatePairs = new();

        /// <summary>
        /// The working BiDi classes
        /// </summary>
        private ArraySlice<BidiClass> _workingClasses;

        /// <summary>
        /// The working classes buffer
        /// </summary>
        private ArrayBuilder<BidiClass> _workingClassesBuffer;

        /// <summary>
        /// A slice of the resolved levels
        /// </summary>
        private ArraySlice<sbyte> _resolvedLevels;

        /// <summary>
        /// The buffer underlying resolvedLevels
        /// </summary>
        private ArrayBuilder<sbyte> _resolvedLevelsBuffer;

        /// <summary>
        /// The resolve paragraph embedding level
        /// </summary>
        private sbyte _paragraphEmbeddingLevel;

        /// <summary>
        /// The status stack used during resolution of explicit
        /// embedding and isolating runs
        /// </summary>
        private readonly Stack<Status> _statusStack = new();

        /// <summary>
        /// Mapping used to virtually remove characters for rule X9
        /// </summary>
        private ArrayBuilder<int> _x9Map;

        /// <summary>
        /// Re-usable list of level runs
        /// </summary>
        private readonly List<LevelRun> _levelRuns = new();

        /// <summary>
        /// Mapping for the current isolating sequence, built
        /// by joining level runs from the x9 map.
        /// </summary>
        private ArrayBuilder<int> _isolatedRunMapping;

        /// <summary>
        /// A stack of pending isolate openings used by FindIsolatePairs()
        /// </summary>
        private readonly Stack<int> _pendingIsolateOpenings = new();

        /// <summary>
        /// The level of the isolating run currently being processed
        /// </summary>
        private int _runLevel;

        /// <summary>
        /// The direction of the isolating run currently being processed
        /// </summary>
        private BidiClass _runDirection;

        /// <summary>
        /// The length of the isolating run currently being processed
        /// </summary>
        private int _runLength;

        /// <summary>
        /// A mapped slice of the resolved types for the isolating run currently
        /// being processed
        /// </summary>
        private MappedArraySlice<BidiClass> _runResolvedClasses;

        /// <summary>
        /// A mapped slice of the original types for the isolating run currently
        /// being processed
        /// </summary>
        private MappedArraySlice<BidiClass> _runOriginalClasses;

        /// <summary>
        /// A mapped slice of the run levels for the isolating run currently
        /// being processed
        /// </summary>
        private MappedArraySlice<sbyte> _runLevels;

        /// <summary>
        /// A mapped slice of the paired bracket types of the isolating
        /// run currently being processed
        /// </summary>
        private MappedArraySlice<BidiPairedBracketType> _runBiDiPairedBracketTypes;

        /// <summary>
        /// A mapped slice of the paired bracket values of the isolating
        /// run currently being processed
        /// </summary>
        private MappedArraySlice<int> _runPairedBracketValues;

        /// <summary>
        /// Maximum pairing depth for paired brackets
        /// </summary>
        private const int MaxPairedBracketDepth = 63;

        /// <summary>
        /// Reusable list of pending opening brackets used by the
        /// LocatePairedBrackets method
        /// </summary>
        private readonly List<int> _pendingOpeningBrackets = new();

        /// <summary>
        /// Resolved list of paired brackets
        /// </summary>
        private readonly List<BracketPair> _pairedBrackets = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="BidiAlgorithm"/> class.
        /// </summary>
        public BidiAlgorithm()
        {
        }

        /// <summary>
        /// Gets the resolved levels.
        /// </summary>
        public ArraySlice<sbyte> ResolvedLevels => _resolvedLevels;

        /// <summary>
        /// Gets the resolved paragraph embedding level
        /// </summary>
        public int ResolvedParagraphEmbeddingLevel => _paragraphEmbeddingLevel;

        /// <summary>
        /// Process data from a BiDiData instance
        /// </summary>
        /// <param name="data">The BiDi Unicode data.</param>
        public void Process(BidiData data)
            => Process(
                data.Classes,
                data.PairedBracketTypes,
                data.PairedBracketValues,
                data.ParagraphEmbeddingLevel,
                data.HasBrackets,
                data.HasEmbeddings,
                data.HasIsolates,
                null);

        /// <summary>
        /// Processes Bidi Data
        /// </summary>
        public void Process(
            ArraySlice<BidiClass> types,
            ArraySlice<BidiPairedBracketType> pairedBracketTypes,
            ArraySlice<int> pairedBracketValues,
            sbyte paragraphEmbeddingLevel,
            bool? hasBrackets,
            bool? hasEmbeddings,
            bool? hasIsolates,
            ArraySlice<sbyte>? outLevels)
        {
            // Reset state
            Reset();

            if (types.IsEmpty)
            {
                return;
            }

            _hasCleanState = false;

            // Setup original types and working types
            _originalClasses = types;
            _workingClasses = _workingClassesBuffer.Add(types);

            // Capture paired bracket values and types
            _pairedBracketTypes = pairedBracketTypes;
            _pairedBracketValues = pairedBracketValues;

            // Store things we know
            _hasBrackets = hasBrackets ?? _pairedBracketTypes.Length == _originalClasses.Length;
            _hasEmbeddings = hasEmbeddings ?? true;
            _hasIsolates = hasIsolates ?? true;

            // Find all isolate pairs
            FindIsolatePairs();

            // Resolve the paragraph embedding level
            if (paragraphEmbeddingLevel == 2)
            {
                _paragraphEmbeddingLevel = ResolveEmbeddingLevel(_originalClasses);
            }
            else
            {
                _paragraphEmbeddingLevel = paragraphEmbeddingLevel;
            }

            // Create resolved levels buffer
            if (outLevels.HasValue)
            {
                if (outLevels.Value.Length != _originalClasses.Length)
                {
                    throw new ArgumentException("Out levels must be the same length as the input data");
                }

                _resolvedLevels = outLevels.Value;
            }
            else
            {
                _resolvedLevels = _resolvedLevelsBuffer.Add(_originalClasses.Length);
                _resolvedLevels.Fill(_paragraphEmbeddingLevel);
            }

            // Resolve explicit embedding levels (Rules X1-X8)
            ResolveExplicitEmbeddingLevels();

            // Build the rule X9 map
            BuildX9RemovalMap();

            // Process all isolated run sequences
            ProcessIsolatedRunSequences();

            // Reset whitespace levels
            ResetWhitespaceLevels();

            // Clean up
            AssignLevelsToCodePointsRemovedByX9();
        }

        /// <summary>
        /// Resolve the paragraph embedding level if not explicitly passed
        /// by the caller. Also used by rule X5c for FSI isolating sequences.
        /// </summary>
        /// <param name="data">The data to be evaluated</param>
        /// <returns>The resolved embedding level</returns>
        public sbyte ResolveEmbeddingLevel(ArraySlice<BidiClass> data)
        {
            // P2
            for (var i = 0; i < data.Length; ++i)
            {
                switch (data[i])
                {
                    case BidiClass.LeftToRight:
                        // P3
                        return 0;

                    case BidiClass.ArabicLetter:
                    case BidiClass.RightToLeft:
                        // P3
                        return 1;

                    case BidiClass.FirstStrongIsolate:
                    case BidiClass.LeftToRightIsolate:
                    case BidiClass.RightToLeftIsolate:
                        // Skip isolate pairs
                        // (Because we're working with a slice, we need to adjust the indices
                        //  we're using for the isolatePairs map)
                        if (_isolatePairs.TryGetValue(data.Start + i, out i))
                        {
                            i -= data.Start;
                        }
                        else
                        {
                            i = data.Length;
                        }

                        break;
                }
            }

            // P3
            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsIsolateStart(BidiClass type)
        {
            const uint mask =
                (1U << (int)BidiClass.LeftToRightIsolate) |
                (1U << (int)BidiClass.RightToLeftIsolate) |
                (1U << (int)BidiClass.FirstStrongIsolate);

            return ((1U << (int)type) & mask) != 0U;
        }

        /// <summary>
        /// Build a list of matching isolates for a directionality slice
        /// Implements BD9
        /// </summary>
        private void FindIsolatePairs()
        {
            // Redundant?
            if (!_hasIsolates)
            {
                return;
            }

            // Lets double check this as we go and clear the flag
            // if there actually aren't any isolate pairs as this might
            // mean we can skip some later steps
            _hasIsolates = false;

            // BD9...
            _pendingIsolateOpenings.Clear();
            
            for (var i = 0; i < _originalClasses.Length; i++)
            {
                var t = _originalClasses[i];

                switch (t)
                {
                    case BidiClass.LeftToRightIsolate:
                    case BidiClass.RightToLeftIsolate:
                    case BidiClass.FirstStrongIsolate:
                    {
                        _pendingIsolateOpenings.Push(i);
                        _hasIsolates = true;
                        break;
                    }
                    case BidiClass.PopDirectionalIsolate:
                    {
                        if (_pendingIsolateOpenings.Count > 0)
                        {
                            _isolatePairs.Add(_pendingIsolateOpenings.Pop(), i);
                        }

                        _hasIsolates = true;
                        
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Resolve the explicit embedding levels from the original
        /// data.  Implements rules X1 to X8.
        /// </summary>
        private void ResolveExplicitEmbeddingLevels()
        {
            // Redundant?
            if (!_hasIsolates && !_hasEmbeddings)
            {
                return;
            }

            // Work variables
            _statusStack.Clear();
            var overflowIsolateCount = 0;
            var overflowEmbeddingCount = 0;
            var validIsolateCount = 0;

            // Constants
            const int maxStackDepth = 125;

            // Rule X1 - setup initial state
            _statusStack.Clear();

            // Neutral
            _statusStack.Push(new Status(_paragraphEmbeddingLevel, BidiClass.OtherNeutral, false));

            // Process all characters
            for (var i = 0; i < _originalClasses.Length; i++)
            {
                switch (_originalClasses[i])
                {
                    case BidiClass.RightToLeftEmbedding:
                    {
                        // Rule X2
                        var newLevel = (sbyte)((_statusStack.Peek().EmbeddingLevel + 1) | 1);
                        if (newLevel <= maxStackDepth && overflowIsolateCount == 0 && overflowEmbeddingCount == 0)
                        {
                            _statusStack.Push(new Status(newLevel, BidiClass.OtherNeutral, false));
                            _resolvedLevels[i] = newLevel;
                        }
                        else if (overflowIsolateCount == 0)
                        {
                            overflowEmbeddingCount++;
                        }

                        break;
                    }

                    case BidiClass.LeftToRightEmbedding:
                    {
                        // Rule X3
                        var newLevel = (sbyte)((_statusStack.Peek().EmbeddingLevel + 2) & ~1);
                        if (newLevel < maxStackDepth && overflowIsolateCount == 0 && overflowEmbeddingCount == 0)
                        {
                            _statusStack.Push(new Status(newLevel, BidiClass.OtherNeutral, false));
                            _resolvedLevels[i] = newLevel;
                        }
                        else if (overflowIsolateCount == 0)
                        {
                            overflowEmbeddingCount++;
                        }

                        break;
                    }

                    case BidiClass.RightToLeftOverride:
                    {
                        // Rule X4
                        var newLevel = (sbyte)((_statusStack.Peek().EmbeddingLevel + 1) | 1);
                        if (newLevel <= maxStackDepth && overflowIsolateCount == 0 && overflowEmbeddingCount == 0)
                        {
                            _statusStack.Push(new Status(newLevel, BidiClass.RightToLeft, false));
                            _resolvedLevels[i] = newLevel;
                        }
                        else if (overflowIsolateCount == 0)
                        {
                            overflowEmbeddingCount++;
                        }

                        break;
                    }

                    case BidiClass.LeftToRightOverride:
                    {
                        // Rule X5
                        var newLevel = (sbyte)((_statusStack.Peek().EmbeddingLevel + 2) & ~1);
                        if (newLevel <= maxStackDepth && overflowIsolateCount == 0 && overflowEmbeddingCount == 0)
                        {
                            _statusStack.Push(new Status(newLevel, BidiClass.LeftToRight, false));
                            _resolvedLevels[i] = newLevel;
                        }
                        else if (overflowIsolateCount == 0)
                        {
                            overflowEmbeddingCount++;
                        }

                        break;
                    }

                    case BidiClass.RightToLeftIsolate:
                    case BidiClass.LeftToRightIsolate:
                    case BidiClass.FirstStrongIsolate:
                    {
                        // Rule X5a, X5b and X5c
                        var resolvedIsolate = _originalClasses[i];

                        if (resolvedIsolate == BidiClass.FirstStrongIsolate)
                        {
                            if (!_isolatePairs.TryGetValue(i, out var endOfIsolate))
                            {
                                endOfIsolate = _originalClasses.Length;
                            }

                            // Rule X5c
                            if (ResolveEmbeddingLevel(_originalClasses.Slice(i + 1,endOfIsolate - (i + 1))) == 1)
                            {
                                resolvedIsolate = BidiClass.RightToLeftIsolate;
                            }
                            else
                            {
                                resolvedIsolate = BidiClass.LeftToRightIsolate;
                            }
                        }

                        // Replace RLI's level with current embedding level
                        var tos = _statusStack.Peek();
                        _resolvedLevels[i] = tos.EmbeddingLevel;

                        // Apply override
                        if (tos.OverrideStatus != BidiClass.OtherNeutral)
                        {
                            _workingClasses[i] = tos.OverrideStatus;
                        }

                        // Work out new level
                        sbyte newLevel;
                        if (resolvedIsolate == BidiClass.RightToLeftIsolate)
                        {
                            newLevel = (sbyte)((tos.EmbeddingLevel + 1) | 1);
                        }
                        else
                        {
                            newLevel = (sbyte)((tos.EmbeddingLevel + 2) & ~1);
                        }

                        // Valid?
                        if (newLevel <= maxStackDepth && overflowIsolateCount == 0 && overflowEmbeddingCount == 0)
                        {
                            validIsolateCount++;
                            _statusStack.Push(new Status(newLevel, BidiClass.OtherNeutral, true));
                        }
                        else
                        {
                            overflowIsolateCount++;
                        }

                        break;
                    }

                    case BidiClass.BoundaryNeutral:
                    {
                        // Mentioned in rule X6 - "for all types besides ..., BN, ..."
                        // no-op
                        break;
                    }

                    default:
                    {
                        // Rule X6
                        var tos = _statusStack.Peek();
                        _resolvedLevels[i] = tos.EmbeddingLevel;
                        if (tos.OverrideStatus != BidiClass.OtherNeutral)
                        {
                            _workingClasses[i] = tos.OverrideStatus;
                        }

                        break;
                    }

                    case BidiClass.PopDirectionalIsolate:
                    {
                        // Rule X6a
                        if (overflowIsolateCount > 0)
                        {
                            overflowIsolateCount--;
                        }
                        else if (validIsolateCount != 0)
                        {
                            overflowEmbeddingCount = 0;
                            while (!_statusStack.Peek().IsolateStatus)
                            {
                                _statusStack.Pop();
                            }

                            _statusStack.Pop();
                            validIsolateCount--;
                        }

                        var tos = _statusStack.Peek();
                        _resolvedLevels[i] = tos.EmbeddingLevel;
                        if (tos.OverrideStatus != BidiClass.OtherNeutral)
                        {
                            _workingClasses[i] = tos.OverrideStatus;
                        }

                        break;
                    }

                    case BidiClass.PopDirectionalFormat:
                    {
                        // Rule X7
                        if (overflowIsolateCount == 0)
                        {
                            if (overflowEmbeddingCount > 0)
                            {
                                overflowEmbeddingCount--;
                            }
                            else if (!_statusStack.Peek().IsolateStatus && _statusStack.Count >= 2)
                            {
                                _statusStack.Pop();
                            }
                        }

                        break;
                    }

                    case BidiClass.ParagraphSeparator:
                    {
                        // Rule X8
                        _resolvedLevels[i] = _paragraphEmbeddingLevel;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Build a map to the original data positions that excludes all
        /// the types defined by rule X9
        /// </summary>
        private void BuildX9RemovalMap()
        {
            // Reserve room for the x9 map
            _x9Map.Length = _originalClasses.Length;

            if (_hasEmbeddings || _hasIsolates)
            {
                // Build a map the removes all x9 characters
                var j = 0;
                for (var i = 0; i < _originalClasses.Length; i++)
                {
                    if (!IsRemovedByX9(_originalClasses[i]))
                    {
                        _x9Map[j++] = i;
                    }
                }

                // Set the final length
                _x9Map.Length = j;
            }
            else
            {
                for (int i = 0, count = _originalClasses.Length; i < count; i++)
                {
                    _x9Map[i] = i;
                }
            }
        }

        /// <summary>
        /// Find the original character index for an entry in the X9 map
        /// </summary>
        /// <param name="index">Index in the x9 removal map</param>
        /// <returns>Index to the original data</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int MapX9(int index) => _x9Map[index];

        /// <summary>
        /// Add a new level run
        /// </summary>
        /// <remarks>
        /// This method resolves the sos and eos values for the run
        /// and adds the run to the list
        /// </remarks>
        /// <param name="start">The index of the start of the run (in x9 removed units)</param>
        /// <param name="length">The length of the run (in x9 removed units)</param>
        /// <param name="level">The level of the run</param>
        private void AddLevelRun(int start, int length, int level)
        {
            // Get original indices to first and last character in this run
            var firstCharIndex = MapX9(start);
            var lastCharIndex = MapX9(start + length - 1);

            // Work out sos
            var i = firstCharIndex - 1;
            
            while (i >= 0 && IsRemovedByX9(_originalClasses[i]))
            {
                i--;
            }

            var prevLevel = i < 0 ? _paragraphEmbeddingLevel : _resolvedLevels[i];
            var sos = DirectionFromLevel(Math.Max(prevLevel, level));

            // Work out eos
            var lastType = _workingClasses[lastCharIndex];
            int nextLevel;

            if (IsIsolateStart(lastType))
            {
                nextLevel = _paragraphEmbeddingLevel;
            }
            else
            {
                i = lastCharIndex + 1;
                while (i < _originalClasses.Length && IsRemovedByX9(_originalClasses[i]))
                {
                    i++;
                }

                nextLevel = i >= _originalClasses.Length ? _paragraphEmbeddingLevel : _resolvedLevels[i];
            }

            var eos = DirectionFromLevel(Math.Max(nextLevel, level));

            // Add the run
            _levelRuns.Add(new LevelRun(start, length, level, sos, eos));
        }

        /// <summary>
        /// Find all runs of the same level, populating the _levelRuns
        /// collection
        /// </summary>
        private void FindLevelRuns()
        {
            var currentLevel = -1;
            var runStart = 0;
            
            for (var i = 0; i < _x9Map.Length; ++i)
            {
                int level = _resolvedLevels[MapX9(i)];

                if (level == currentLevel)
                {
                    continue;
                }

                if (currentLevel != -1)
                {
                    AddLevelRun(runStart, i - runStart, currentLevel);
                }

                currentLevel = level;
                runStart = i;
            }

            // Don't forget the final level run
            if (currentLevel != -1)
            {
                AddLevelRun(runStart, _x9Map.Length - runStart, currentLevel);
            }
        }

        /// <summary>
        /// Given a character index, find the level run that starts at that position
        /// </summary>
        /// <param name="index">The index into the original (unmapped) data</param>
        /// <returns>The index of the run that starts at that index</returns>
        private int FindRunForIndex(int index)
        {
            for (var i = 0; i < _levelRuns.Count; i++)
            {
                // Passed index is for the original non-x9 filtered data, however
                // the level run ranges are for the x9 filtered data.  Convert before
                // comparing
                if (MapX9(_levelRuns[i].Start) == index)
                {
                    return i;
                }
            }

            throw new InvalidOperationException("Internal error");
        }

        /// <summary>
        /// Determine and the process all isolated run sequences
        /// </summary>
        private void ProcessIsolatedRunSequences()
        {
            // Find all runs with the same level
            FindLevelRuns();

            // Process them one at a time by first building
            // a mapping using slices from the x9 map for each
            // run section that needs to be joined together to
            // form an complete run.  That full run mapping
            // will be placed in _isolatedRunMapping and then
            // processed by ProcessIsolatedRunSequence().
            while (_levelRuns.Count > 0)
            {
                // Clear the mapping
                _isolatedRunMapping.Clear();

                // Combine mappings from this run and all runs that continue on from it
                var runIndex = 0;
                BidiClass eos;
                var sos = _levelRuns[0].Sos;
                var level = _levelRuns[0].Level;
                
                while (true)
                {
                    // Get the run
                    var r = _levelRuns[runIndex];

                    // The eos of the isolating run is the eos of the
                    // last level run that comprises it.
                    eos = r.Eos;

                    // Remove this run as we've now processed it
                    _levelRuns.RemoveAt(runIndex);

                    // Add the x9 map indices for the run range to the mapping
                    // for this isolated run
                    _isolatedRunMapping.Add(_x9Map.AsSlice(r.Start, r.Length));

                    // Get the last character and see if it's an isolating run with a matching
                    // PDI and concatenate that run to this one
                    var lastCharacterIndex = _isolatedRunMapping[_isolatedRunMapping.Length - 1];
                    var lastType = _originalClasses[lastCharacterIndex];
                    if (IsIsolateStart(lastType) && _isolatePairs.TryGetValue(lastCharacterIndex, out var nextRunIndex))
                    {
                        // Find the continuing run index
                        runIndex = FindRunForIndex(nextRunIndex);
                    }
                    else
                    {
                        break;
                    }
                }

                // Process this isolated run
                ProcessIsolatedRunSequence(sos, eos, level);
            }
        }

        /// <summary>
        /// Process a single isolated run sequence, where the character sequence
        /// mapping is currently held in _isolatedRunMapping.
        /// </summary>
        private void ProcessIsolatedRunSequence(BidiClass sos, BidiClass eos, int runLevel)
        {
            // Create mappings onto the underlying data
            var isolatedRunMapping = _isolatedRunMapping.AsSlice();
            _runResolvedClasses = new MappedArraySlice<BidiClass>(_workingClasses, isolatedRunMapping);
            _runOriginalClasses = new MappedArraySlice<BidiClass>(_originalClasses, isolatedRunMapping);
            _runLevels = new MappedArraySlice<sbyte>(_resolvedLevels, isolatedRunMapping);
            if (_hasBrackets)
            {
                _runBiDiPairedBracketTypes = new MappedArraySlice<BidiPairedBracketType>(_pairedBracketTypes, isolatedRunMapping);
                _runPairedBracketValues = new MappedArraySlice<int>(_pairedBracketValues, isolatedRunMapping);
            }

            _runLevel = runLevel;
            _runDirection = DirectionFromLevel(runLevel);
            _runLength = _runResolvedClasses.Length;

            // Rule W1
            // Also, set hasXX flags
            int i;
            var previousClass = sos;
            
            const uint isolateMask =
                (1U << (int)BidiClass.LeftToRightIsolate) |
                (1U << (int)BidiClass.RightToLeftIsolate) |
                (1U << (int)BidiClass.FirstStrongIsolate) |
                (1U << (int)BidiClass.PopDirectionalIsolate);

            const uint wRulesMask =
                (1U << (int)BidiClass.EuropeanNumber) |
                (1U << (int)BidiClass.ArabicLetter) |
                (1U << (int)BidiClass.EuropeanSeparator) |
                (1U << (int)BidiClass.CommonSeparator) |
                (1U << (int)BidiClass.ArabicNumber) |
                (1U << (int)BidiClass.EuropeanTerminator);

            uint wRules = 0;

            for (i = 0; i < _runLength; i++)
            {
                var resolvedClass = _runResolvedClasses[i];

                if (resolvedClass == BidiClass.NonspacingMark)
                {
                    _runResolvedClasses[i] = previousClass;
                }
                else
                {
                    var classBit = 1U << (int)resolvedClass;
                    if ((classBit & isolateMask) != 0U)
                    {
                        previousClass = BidiClass.OtherNeutral;
                    }
                    else
                    {
                        wRules |= classBit & wRulesMask;
                        previousClass = resolvedClass;
                    }
                }
            }

            // By tracking the types of characters known to be in the current run, we can
            // skip some of the rules that we know won't apply.
            var hasEN = (wRules & (1U << (int)BidiClass.EuropeanNumber)) != 0U;
            var hasAL = (wRules & (1U << (int)BidiClass.ArabicLetter)) != 0U;
            var hasES = (wRules & (1U << (int)BidiClass.EuropeanSeparator)) != 0U;
            var hasCS = (wRules & (1U << (int)BidiClass.CommonSeparator)) != 0U;
            var hasAN = (wRules & (1U << (int)BidiClass.ArabicNumber)) != 0U;
            var hasET = (wRules & (1U << (int)BidiClass.EuropeanTerminator)) != 0U;

            // Rule W2
            if (hasEN)
            {
                for (i = 0; i < _runLength; i++)
                {
                    if (_runResolvedClasses[i] != BidiClass.EuropeanNumber)
                    {
                        continue;
                    }

                    for (var j = i - 1; j >= 0; j--)
                    {
                        var resolvedClass = _runResolvedClasses[j];

                        switch (resolvedClass)
                        {
                            case BidiClass.LeftToRight:
                            case BidiClass.RightToLeft:
                            case BidiClass.ArabicLetter:
                            {
                                if (resolvedClass == BidiClass.ArabicLetter)
                                {
                                    _runResolvedClasses[i] = BidiClass.ArabicNumber;
                                    hasAN = true;
                                }

                                j = -1;
                                    
                                break;
                            }
                        }
                    }
                }
            }

            // Rule W3
            if (hasAL)
            {
                for (i = 0; i < _runLength; i++)
                {
                    if (_runResolvedClasses[i] == BidiClass.ArabicLetter)
                    {
                        _runResolvedClasses[i] = BidiClass.RightToLeft;
                    }
                }
            }

            // Rule W4
            if ((hasES || hasCS) && (hasEN || hasAN))
            {
                for (i = 1; i < _runLength - 1; ++i)
                {
                    ref var resolvedClass = ref _runResolvedClasses[i];
                    
                    if (resolvedClass == BidiClass.EuropeanSeparator)
                    {
                        var previousSeparatorClass = _runResolvedClasses[i - 1];
                        var nextSeparatorClass = _runResolvedClasses[i + 1];

                        if (previousSeparatorClass == BidiClass.EuropeanNumber && nextSeparatorClass == BidiClass.EuropeanNumber)
                        {
                            // ES between EN and EN
                            resolvedClass = BidiClass.EuropeanNumber;
                        }
                    }
                    else if (resolvedClass == BidiClass.CommonSeparator)
                    {
                        var previousSeparatorClass = _runResolvedClasses[i - 1];
                        var nextSeparatorClass = _runResolvedClasses[i + 1];

                        if ((previousSeparatorClass == BidiClass.ArabicNumber && nextSeparatorClass == BidiClass.ArabicNumber) ||
                             (previousSeparatorClass == BidiClass.EuropeanNumber && nextSeparatorClass == BidiClass.EuropeanNumber))
                        {
                            // CS between (AN and AN) or (EN and EN)
                            resolvedClass = previousSeparatorClass;
                        }
                    }
                }
            }

            // Rule W5
            if (hasET && hasEN)
            {
                for (i = 0; i < _runLength; ++i)
                {
                    if (_runResolvedClasses[i] != BidiClass.EuropeanTerminator)
                    {
                        continue;
                    }
                    
                    // Locate end of sequence
                    var sequenceStart = i;
                    var sequenceEnd = i;
                    
                    while (sequenceEnd < _runLength && _runResolvedClasses[sequenceEnd] == BidiClass.EuropeanTerminator)
                    {
                        sequenceEnd++;
                    }

                    // Preceded by, or followed by EN?
                    if ((sequenceStart == 0 ? sos : _runResolvedClasses[sequenceStart - 1]) == BidiClass.EuropeanNumber
                        || (sequenceEnd == _runLength ? eos : _runResolvedClasses[sequenceEnd]) == BidiClass.EuropeanNumber)
                    {
                        // Change the entire range
                        for (var j = sequenceStart; i < sequenceEnd; ++i)
                        {
                            _runResolvedClasses[i] = BidiClass.EuropeanNumber;
                        }
                    }

                    // continue at end of sequence
                    i = sequenceEnd;
                }
            }

            // Rule W6
            if (hasES || hasET || hasCS)
            {
                for (i = 0; i < _runLength; ++i)
                {
                    ref var resolvedClass = ref _runResolvedClasses[i];

                    switch (resolvedClass)
                    {
                        case BidiClass.EuropeanSeparator:
                        case BidiClass.EuropeanTerminator:
                        case BidiClass.CommonSeparator:
                        {
                            resolvedClass = BidiClass.OtherNeutral;
                            
                            break;
                        }
                    }
                }
            }

            // Rule W7.
            if (hasEN)
            {
                var previousStrongClass = sos;
                
                for (i = 0; i < _runLength; ++i)
                {
                    ref var resolvedClass = ref _runResolvedClasses[i];

                    switch (resolvedClass)
                    {
                        case BidiClass.EuropeanNumber:
                        {
                            // If prev strong type was an L change this to L too
                            if (previousStrongClass == BidiClass.LeftToRight)
                            {
                                _runResolvedClasses[i] = BidiClass.LeftToRight;
                            }
                            
                            break;
                        }
                       
                        case BidiClass.LeftToRight:
                        case BidiClass.RightToLeft:
                        {
                            // Remember previous strong type (NB: AL should already be changed to R)
                            previousStrongClass = resolvedClass;
                            break;
                        }
                    }
                }
            }

            // Rule N0 - process bracket pairs
            if (_hasBrackets)
            {
                int count;
                var pairedBrackets = LocatePairedBrackets();
                
                for (i = 0, count = pairedBrackets.Count; i < count; i++)
                {
                    var pairedBracket = pairedBrackets[i];
                    
                    var strongDirection = InspectPairedBracket(pairedBracket);

                    // Case "d" - no strong types in the brackets, ignore
                    if (strongDirection == BidiClass.OtherNeutral)
                    {
                        continue;
                    }

                    // Case "b" - strong type found that matches the embedding direction
                    if ((strongDirection == BidiClass.LeftToRight || strongDirection == BidiClass.RightToLeft) && strongDirection == _runDirection)
                    {
                        SetPairedBracketDirection(pairedBracket, strongDirection);
                        continue;
                    }

                    // Case "c" - found opposite strong type found, look before to establish context
                    strongDirection = InspectBeforePairedBracket(pairedBracket, sos);
                    
                    if (strongDirection == _runDirection || strongDirection == BidiClass.OtherNeutral)
                    {
                        strongDirection = _runDirection;
                    }

                    SetPairedBracketDirection(pairedBracket, strongDirection);
                }
            }

            // Rules N1 and N2 - resolve neutral types
            for (i = 0; i < _runLength; ++i)
            {
                var resolvedClass = _runResolvedClasses[i];
                
                if (IsNeutralClass(resolvedClass))
                {
                    // Locate end of sequence
                    var seqStart = i;
                    var seqEnd = i;
                    
                    while (seqEnd < _runLength && IsNeutralClass(_runResolvedClasses[seqEnd]))
                    {
                        seqEnd++;
                    }

                    // Work out the preceding class
                    BidiClass classBefore;
                    
                    if (seqStart == 0)
                    {
                        classBefore = sos;
                    }
                    else
                    {
                        classBefore = _runResolvedClasses[seqStart - 1];
                        
                        switch (classBefore)
                        {
                            case BidiClass.ArabicNumber:
                            case BidiClass.EuropeanNumber:
                            {
                                classBefore = BidiClass.RightToLeft;
                                
                                break;
                            }
                        }
                    }

                    // Work out the following class
                    BidiClass classAfter;
                    
                    if (seqEnd == _runLength)
                    {
                        classAfter = eos;
                    }
                    else
                    {
                        classAfter = _runResolvedClasses[seqEnd];

                        switch (classAfter)
                        {
                            case BidiClass.ArabicNumber:
                            case BidiClass.EuropeanNumber:
                            {
                                classAfter = BidiClass.RightToLeft;
                                
                                break;
                            }
                        }
                    }

                    // Work out the final resolved type
                    BidiClass finalResolveClass;
                    
                    if (classBefore == classAfter)
                    {
                        // Rule N1
                        finalResolveClass = classBefore;
                    }
                    else
                    {
                        // Rule N2
                        finalResolveClass = _runDirection;
                    }

                    // Apply changes
                    for (var j = seqStart; j < seqEnd; j++)
                    {
                        _runResolvedClasses[j] = finalResolveClass;
                    }

                    // continue after this run
                    i = seqEnd;
                }
            }

            // Rules I1 and I2 - resolve implicit types
            if ((_runLevel & 0x01) == 0)
            {
                // Rule I1 - even
                for (i = 0; i < _runLength; i++)
                {
                    var resolvedClass = _runResolvedClasses[i];
                    ref var currentRunLevel = ref _runLevels[i];

                    switch (resolvedClass)
                    {
                        case BidiClass.RightToLeft:
                        {
                            currentRunLevel++;
                            break;
                        }
                        case BidiClass.ArabicNumber:
                        case BidiClass.EuropeanNumber:
                        {
                            currentRunLevel += 2;
                            
                            break;
                        }
                    }
                }
            }
            else
            {
                // Rule I2 - odd
                for (i = 0; i < _runLength; i++)
                {
                    var resolvedClass = _runResolvedClasses[i];
                    ref var currentRunLevel = ref _runLevels[i];
                    
                    if (resolvedClass != BidiClass.RightToLeft)
                    {
                        currentRunLevel++;
                    }
                }
            }
        }

        /// <summary>
        /// Locate all pair brackets in the current isolating run
        /// </summary>
        /// <returns>A sorted list of BracketPairs</returns>
        private List<BracketPair> LocatePairedBrackets()
        {
            // Clear work collections
            _pendingOpeningBrackets.Clear();
            _pairedBrackets.Clear();

            // Since List.Sort is expensive on memory if called often (it internally
            // allocates an ArraySorted object) and since we will rarely have many
            // items in this list (most paragraphs will only have a handful of bracket
            // pairs - if that), we use a simple linear lookup and insert most of the
            // time.  If there are more that `sortLimit` paired brackets we abort th
            // linear searching/inserting and using List.Sort at the end.
            const int sortLimit = 8;

            // Process all characters in the run, looking for paired brackets
            for (int i = 0, length = _runLength; i < length; i++)
            {
                // Ignore non-neutral characters
                if (_runResolvedClasses[i] != BidiClass.OtherNeutral)
                {
                    continue;
                }

                switch (_runBiDiPairedBracketTypes[i])
                {
                    case BidiPairedBracketType.Open:
                        if (_pendingOpeningBrackets.Count == MaxPairedBracketDepth)
                        {
                            goto exit;
                        }

                        _pendingOpeningBrackets.Insert(0, i);
                        break;

                    case BidiPairedBracketType.Close:
                        // see if there is a match
                        for (var j = 0; j < _pendingOpeningBrackets.Count; j++)
                        {
                            if (_runPairedBracketValues[i] != _runPairedBracketValues[_pendingOpeningBrackets[j]])
                            {
                                continue;
                            }
                            
                            // Add this paired bracket set
                            var opener = _pendingOpeningBrackets[j];
                            
                            if (_pairedBrackets.Count < sortLimit)
                            {
                                var ppi = 0;
                                while (ppi < _pairedBrackets.Count && _pairedBrackets[ppi].OpeningIndex < opener)
                                {
                                    ppi++;
                                }

                                _pairedBrackets.Insert(ppi, new BracketPair(opener, i));
                            }
                            else
                            {
                                _pairedBrackets.Add(new BracketPair(opener, i));
                            }

                            // remove up to and including matched opener
                            _pendingOpeningBrackets.RemoveRange(0, j + 1);
                            break;
                        }

                        break;
                }
            }

            exit:

            // Is a sort pending?
            if (_pairedBrackets.Count > sortLimit)
            {
                _pairedBrackets.Sort();
            }

            return _pairedBrackets;
        }

        /// <summary>
        /// Inspect a paired bracket set and determine its strong direction
        /// </summary>
        /// <param name="bracketPair">The paired bracket to be inspected</param>
        /// <returns>The direction of the bracket set content</returns>
        private BidiClass InspectPairedBracket(in BracketPair bracketPair)
        {
            var directionFromLevel = DirectionFromLevel(_runLevel);
            var directionOpposite = BidiClass.OtherNeutral;
            
            for (var i = bracketPair.OpeningIndex + 1; i < bracketPair.ClosingIndex; i++)
            {
                var dir = GetStrongClassN0(_runResolvedClasses[i]);
                
                if (dir == BidiClass.OtherNeutral)
                {
                    continue;
                }

                if (dir == directionFromLevel)
                {
                    return dir;
                }

                directionOpposite = dir;
            }

            return directionOpposite;
        }

        /// <summary>
        /// Look for a strong type before a paired bracket
        /// </summary>
        /// <param name="bracketPair">The paired bracket set to be inspected</param>
        /// <param name="sos">The sos in case nothing found before the bracket</param>
        /// <returns>The strong direction before the brackets</returns>
        private BidiClass InspectBeforePairedBracket(in BracketPair bracketPair, BidiClass sos)
        {
            for (var i = bracketPair.OpeningIndex - 1; i >= 0; --i)
            {
                var direction = GetStrongClassN0(_runResolvedClasses[i]);
                
                if (direction != BidiClass.OtherNeutral)
                {
                    return direction;
                }
            }

            return sos;
        }

        /// <summary>
        /// Sets the direction of a bracket pair, including setting the direction of
        /// NSM's inside the brackets and following.
        /// </summary>
        /// <param name="pairedBracket">The paired brackets</param>
        /// <param name="direction">The resolved direction for the bracket pair</param>
        private void SetPairedBracketDirection(in BracketPair pairedBracket, BidiClass direction)
        {
            // Set the direction of the brackets
            _runResolvedClasses[pairedBracket.OpeningIndex] = direction;
            _runResolvedClasses[pairedBracket.ClosingIndex] = direction;

            // Set the directionality of NSM's inside the brackets
            // BN  characters (such as ZWJ or ZWSP) that appear between the base bracket character
            // and the nonspacing mark should be ignored.
            for (int i = pairedBracket.OpeningIndex + 1; i < pairedBracket.ClosingIndex; i++)
            {
                if (_runOriginalClasses[i] == BidiClass.NonspacingMark)
                {
                    _runOriginalClasses[i] = direction;
                }
                else if (_runOriginalClasses[i] != BidiClass.BoundaryNeutral)
                {
                    break;
                }
            }

            // Set the directionality of NSM's following the brackets
            for (int i = pairedBracket.ClosingIndex + 1; i < _runLength; i++)
            {
                if (_runOriginalClasses[i] == BidiClass.NonspacingMark)
                {
                    _runOriginalClasses[i] = direction;
                }
                else if (_runOriginalClasses[i] != BidiClass.BoundaryNeutral)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Resets whitespace levels. Implements rule L1
        /// </summary>
        private void ResetWhitespaceLevels()
        {
            for (var i = 0; i < _resolvedLevels.Length; i++)
            {
                var originalClass = _originalClasses[i];

                switch (originalClass)
                {
                    case BidiClass.ParagraphSeparator:
                    case BidiClass.SegmentSeparator:
                    {
                        // Rule L1, clauses one and two.
                        _resolvedLevels[i] = _paragraphEmbeddingLevel;

                        // Rule L1, clause three.
                        for (var j = i - 1; j >= 0; --j)
                        {
                            if (IsWhitespace(_originalClasses[j]))
                            {
                                // including format codes
                                _resolvedLevels[j] = _paragraphEmbeddingLevel;
                            }
                            else
                            {
                                break;
                            }
                        }
                        
                        break;
                    }
                }
            }

            // Rule L1, clause four.
            for (var j = _resolvedLevels.Length - 1; j >= 0; j--)
            {
                if (IsWhitespace(_originalClasses[j]))
                { // including format codes
                    _resolvedLevels[j] = _paragraphEmbeddingLevel;
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Assign levels to any characters that would be have been
        /// removed by rule X9.  The idea is to keep level runs together
        /// that would otherwise be broken by an interfering isolate/embedding
        /// control character.
        /// </summary>
        private void AssignLevelsToCodePointsRemovedByX9()
        {
            // Redundant?
            if (!_hasIsolates && !_hasEmbeddings)
            {
                return;
            }

            // No-op?
            if (_workingClasses.Length == 0)
            {
                return;
            }

            // Fix up first character
            if (_resolvedLevels[0] < 0)
            {
                _resolvedLevels[0] = _paragraphEmbeddingLevel;
            }

            if (IsRemovedByX9(_originalClasses[0]))
            {
                _workingClasses[0] = _originalClasses[0];
            }

            for (int i = 1, length = _workingClasses.Length; i < length; i++)
            {
                var originalClass = _originalClasses[i];
                
                if (IsRemovedByX9(originalClass))
                {
                    _workingClasses[i] = originalClass;
                    _resolvedLevels[i] = _resolvedLevels[i - 1];
                }
            }
        }

        /// <summary>
        /// Check if a directionality type represents whitespace
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsWhitespace(BidiClass biDiClass)
        {
            const uint mask =
                (1U << (int)BidiClass.LeftToRightEmbedding) |
                (1U << (int)BidiClass.RightToLeftEmbedding) |
                (1U << (int)BidiClass.LeftToRightOverride) |
                (1U << (int)BidiClass.RightToLeftOverride) |
                (1U << (int)BidiClass.PopDirectionalFormat) |
                (1U << (int)BidiClass.LeftToRightIsolate) |
                (1U << (int)BidiClass.RightToLeftIsolate) |
                (1U << (int)BidiClass.FirstStrongIsolate) |
                (1U << (int)BidiClass.PopDirectionalIsolate) |
                (1U << (int)BidiClass.BoundaryNeutral) |
                (1U << (int)BidiClass.WhiteSpace);

            return ((1U << (int)biDiClass) & mask) != 0U;
        }

        /// <summary>
        /// Convert a level to a direction where odd is RTL and
        /// even is LTR
        /// </summary>
        /// <param name="level">The level to convert</param>
        /// <returns>A directionality</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static BidiClass DirectionFromLevel(int level)
            => ((level & 0x1) == 0) ? BidiClass.LeftToRight : BidiClass.RightToLeft;

        /// <summary>
        /// Helper to check if a directionality is removed by rule X9
        /// </summary>
        /// <param name="biDiClass">The bidi type to check</param>
        /// <returns>True if rule X9 would remove this character; otherwise false</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsRemovedByX9(BidiClass biDiClass)
        {
            const uint mask =
                (1U << (int)BidiClass.LeftToRightEmbedding) |
                (1U << (int)BidiClass.RightToLeftEmbedding) |
                (1U << (int)BidiClass.LeftToRightOverride) |
                (1U << (int)BidiClass.RightToLeftOverride) |
                (1U << (int)BidiClass.PopDirectionalFormat) |
                (1U << (int)BidiClass.BoundaryNeutral);

            return ((1U << (int)biDiClass) & mask) != 0U;
        }

        /// <summary>
        /// Check if a a directionality is neutral for rules N1 and N2
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsNeutralClass(BidiClass direction)
        {
            const uint mask =
                (1U << (int)BidiClass.ParagraphSeparator) |
                (1U << (int)BidiClass.SegmentSeparator) |
                (1U << (int)BidiClass.WhiteSpace) |
                (1U << (int)BidiClass.OtherNeutral) |
                (1U << (int)BidiClass.RightToLeftIsolate) |
                (1U << (int)BidiClass.LeftToRightIsolate) |
                (1U << (int)BidiClass.FirstStrongIsolate) |
                (1U << (int)BidiClass.PopDirectionalIsolate);

            return ((1U << (int)direction) & mask) != 0U;
        }

        /// <summary>
        /// Maps a direction to a strong class for rule N0
        /// </summary>
        /// <param name="direction">The direction to map</param>
        /// <returns>A strong direction - R, L or ON</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static BidiClass GetStrongClassN0(BidiClass direction)
        {
            switch (direction)
            {
                    case BidiClass.EuropeanNumber:
                    case BidiClass.ArabicNumber:
                    case BidiClass.ArabicLetter:
                    case BidiClass.RightToLeft:
                        return BidiClass.RightToLeft;
                    case BidiClass.LeftToRight:
                        return BidiClass.LeftToRight;
                    default:
                        return BidiClass.OtherNeutral;
            }
        }

        /// <summary>
        /// Resets the bidi algorithm to a clean state.
        /// </summary>
        public void Reset()
        {
            if (_hasCleanState)
            {
                return;
            }

            _originalClasses = default;
            _pairedBracketTypes = default;
            _pairedBracketValues = default;
            _hasBrackets = default;
            _hasEmbeddings = default;
            _hasIsolates = default;
            _isolatePairs.ClearThenResetIfTooLarge();
            _workingClasses = default;
            FormattingBufferHelper.ClearThenResetIfTooLarge(ref _workingClassesBuffer);
            _resolvedLevels = default;
            FormattingBufferHelper.ClearThenResetIfTooLarge(ref _resolvedLevelsBuffer);
            _paragraphEmbeddingLevel = default;
            FormattingBufferHelper.ClearThenResetIfTooLarge(_statusStack);
            FormattingBufferHelper.ClearThenResetIfTooLarge(ref _x9Map);
            FormattingBufferHelper.ClearThenResetIfTooLarge(_levelRuns);
            FormattingBufferHelper.ClearThenResetIfTooLarge(ref _isolatedRunMapping);
            FormattingBufferHelper.ClearThenResetIfTooLarge(_pendingIsolateOpenings);
            _runLevel = default;
            _runDirection = default;
            _runLength = default;
            _runResolvedClasses = default;
            _runOriginalClasses = default;
            _runLevels = default;
            _runBiDiPairedBracketTypes = default;
            _runPairedBracketValues = default;
            FormattingBufferHelper.ClearThenResetIfTooLarge(_pendingOpeningBrackets);
            FormattingBufferHelper.ClearThenResetIfTooLarge(_pairedBrackets);

            _hasCleanState = true;
        }

        /// <summary>
        /// Hold the start and end index of a pair of brackets
        /// </summary>
        private readonly struct BracketPair : IComparable<BracketPair>
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="BracketPair"/> struct.
            /// </summary>
            /// <param name="openingIndex">Index of the opening bracket</param>
            /// <param name="closingIndex">Index of the closing bracket</param>
            public BracketPair(int openingIndex, int closingIndex)
            {
                OpeningIndex = openingIndex;
                ClosingIndex = closingIndex;
            }

            /// <summary>
            /// Gets the index of the opening bracket
            /// </summary>
            public int OpeningIndex { get; }

            /// <summary>
            /// Gets the index of the closing bracket
            /// </summary>
            public int ClosingIndex { get; }

            public int CompareTo(BracketPair other)
                => OpeningIndex.CompareTo(other.OpeningIndex);
        }

        /// <summary>
        /// Status stack entry used while resolving explicit
        /// embedding levels
        /// </summary>
        private readonly struct Status
        {
            public Status(sbyte embeddingLevel, BidiClass overrideStatus, bool isolateStatus)
            {
                EmbeddingLevel = embeddingLevel;
                OverrideStatus = overrideStatus;
                IsolateStatus = isolateStatus;
            }

            public sbyte EmbeddingLevel { get; }

            public BidiClass OverrideStatus { get; }

            public bool IsolateStatus { get; }
        }

        /// <summary>
        /// Provides information about a level run - a continuous
        /// sequence of equal levels.
        /// </summary>
        private readonly struct LevelRun
        {
            public LevelRun(int start, int length, int level, BidiClass sos, BidiClass eos)
            {
                Start = start;
                Length = length;
                Level = level;
                Sos = sos;
                Eos = eos;
            }

            public int Start { get; }

            public int Length { get; }

            public int Level { get; }

            public BidiClass Sos { get; }

            public BidiClass Eos { get; }
        }
    }
}
