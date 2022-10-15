using System;
using System.Collections.Generic;
using System.Drawing;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Media
{
    /// <summary>
    ///     Represents a sequence of glyphs from a single face of a single font at a single size, and with a single rendering style.
    /// </summary>
    public sealed class GlyphRun : IDisposable
    {
        private static readonly IComparer<int> s_ascendingComparer = Comparer<int>.Default;
        private static readonly IComparer<int> s_descendingComparer = new ReverseComparer<int>();

        private IGlyphRunImpl? _glyphRunImpl;
        private IGlyphTypeface _glyphTypeface;
        private double _fontRenderingEmSize;
        private int _biDiLevel;
        private Point? _baselineOrigin;
        private GlyphRunMetrics? _glyphRunMetrics;

        private ReadOnlySlice<char> _characters;

        private IReadOnlyList<ushort> _glyphIndices;
        private IReadOnlyList<double>? _glyphAdvances;
        private IReadOnlyList<Vector>? _glyphOffsets;
        private IReadOnlyList<int>? _glyphClusters;

        private int _offsetToFirstCharacter;

        /// <summary>
        ///     Initializes a new instance of the <see cref="GlyphRun"/> class by specifying properties of the class.
        /// </summary>
        /// <param name="glyphTypeface">The glyph typeface.</param>
        /// <param name="fontRenderingEmSize">The rendering em size.</param>
        /// <param name="glyphIndices">The glyph indices.</param>
        /// <param name="glyphAdvances">The glyph advances.</param>
        /// <param name="glyphOffsets">The glyph offsets.</param>
        /// <param name="characters">The characters.</param>
        /// <param name="glyphClusters">The glyph clusters.</param>
        /// <param name="biDiLevel">The bidi level.</param>
        public GlyphRun(
            IGlyphTypeface glyphTypeface,
            double fontRenderingEmSize,
            ReadOnlySlice<char> characters,
            IReadOnlyList<ushort> glyphIndices,
            IReadOnlyList<double>? glyphAdvances = null,
            IReadOnlyList<Vector>? glyphOffsets = null,
            IReadOnlyList<int>? glyphClusters = null,
            int biDiLevel = 0)
        {
            _glyphTypeface = glyphTypeface;

            FontRenderingEmSize = fontRenderingEmSize;

            Characters = characters;

            _glyphIndices = glyphIndices;

            GlyphAdvances = glyphAdvances;

            GlyphOffsets = glyphOffsets;

            GlyphClusters = glyphClusters;

            BiDiLevel = biDiLevel;
        }

        /// <summary>
        ///     Gets the <see cref="IGlyphTypeface"/> for the <see cref="GlyphRun"/>.
        /// </summary>
        public IGlyphTypeface GlyphTypeface => _glyphTypeface;

        /// <summary>
        ///     Gets or sets the em size used for rendering the <see cref="GlyphRun"/>.
        /// </summary>
        public double FontRenderingEmSize
        {
            get => _fontRenderingEmSize;
            set => Set(ref _fontRenderingEmSize, value);
        }

        /// <summary>
        ///     Gets or sets the conservative bounding box of the <see cref="GlyphRun"/>.
        /// </summary>
        public Size Size => new Size(Metrics.WidthIncludingTrailingWhitespace, Metrics.Height);

        /// <summary>
        /// 
        /// </summary>
        public GlyphRunMetrics Metrics
        {
            get
            {
                _glyphRunMetrics ??= CreateGlyphRunMetrics();

                return _glyphRunMetrics.Value;
            }
        }

        /// <summary>
        ///     Gets or sets the baseline origin of the<see cref="GlyphRun"/>.
        /// </summary>
        public Point BaselineOrigin
        {
            get
            {
                _baselineOrigin ??= CalculateBaselineOrigin();

                return _baselineOrigin.Value;
            }
            set => Set(ref _baselineOrigin, value);
        }

        /// <summary>
        ///     Gets or sets an array of <see cref="ushort"/> values that represent the glyph indices in the rendering physical font.
        /// </summary>
        public IReadOnlyList<ushort> GlyphIndices
        {
            get => _glyphIndices;
            set => Set(ref _glyphIndices, value);
        }

        /// <summary>
        ///     Gets or sets an array of <see cref="double"/> values that represent the advances corresponding to the glyph indices.
        /// </summary>
        public IReadOnlyList<double>? GlyphAdvances
        {
            get => _glyphAdvances;
            set => Set(ref _glyphAdvances, value);
        }

        /// <summary>
        ///     Gets or sets an array of <see cref="Vector"/> values representing the offsets of the glyphs in the <see cref="GlyphRun"/>.
        /// </summary>
        public IReadOnlyList<Vector>? GlyphOffsets
        {
            get => _glyphOffsets;
            set => Set(ref _glyphOffsets, value);
        }

        /// <summary>
        ///     Gets or sets the list of UTF16 code points that represent the Unicode content of the <see cref="GlyphRun"/>.
        /// </summary>
        public ReadOnlySlice<char> Characters
        {
            get => _characters;
            set => Set(ref _characters, value);
        }

        /// <summary>
        ///     Gets or sets a list of <see cref="int"/> values representing a mapping from character index to glyph index.
        /// </summary>
        public IReadOnlyList<int>? GlyphClusters
        {
            get => _glyphClusters;
            set => Set(ref _glyphClusters, value);
        }

        /// <summary>
        ///     Gets or sets the bidirectional nesting level of the <see cref="GlyphRun"/>.
        /// </summary>
        public int BiDiLevel
        {
            get => _biDiLevel;
            set => Set(ref _biDiLevel, value);
        }

        /// <summary>
        /// Gets the scale of the current <see cref="Media.GlyphTypeface"/>
        /// </summary>
        internal double Scale => FontRenderingEmSize / GlyphTypeface.Metrics.DesignEmHeight;

        /// <summary>
        /// Returns <c>true</c> if the text direction is left-to-right. Otherwise, returns <c>false</c>.
        /// </summary>
        public bool IsLeftToRight => ((BiDiLevel & 1) == 0);

        /// <summary>
        /// The platform implementation of the <see cref="GlyphRun"/>.
        /// </summary>
        public IGlyphRunImpl GlyphRunImpl
        {
            get
            {
                if (_glyphRunImpl == null)
                {
                    Initialize();
                }

                return _glyphRunImpl!;
            }
        }

        /// <summary>
        /// Obtains geometry for the glyph run.
        /// </summary>
        /// <returns>The geometry returned contains the combined geometry of all glyphs in the glyph run.</returns>
        public Geometry BuildGeometry()
        {
            var platformRenderInterface = AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>();

            var geometryImpl = platformRenderInterface.BuildGlyphRunGeometry(this);

            return new PlatformGeometry(geometryImpl);
        }

        /// <summary>
        /// Retrieves the offset from the leading edge of the <see cref="GlyphRun"/>
        /// to the leading or trailing edge of a caret stop containing the specified character hit.
        /// </summary>
        /// <param name="characterHit">The <see cref="CharacterHit"/> to use for computing the offset.</param>
        /// <returns>
        /// A <see cref="double"/> that represents the offset from the leading edge of the <see cref="GlyphRun"/>
        /// to the leading or trailing edge of a caret stop containing the character hit.
        /// </returns>
        public double GetDistanceFromCharacterHit(CharacterHit characterHit)
        {
            var characterIndex = characterHit.FirstCharacterIndex + characterHit.TrailingLength - _offsetToFirstCharacter;

            var distance = 0.0;

            if (IsLeftToRight)
            {
                if (GlyphClusters != null)
                {
                    if (characterIndex < GlyphClusters[0])
                    {
                        return 0;
                    }

                    if (characterIndex > GlyphClusters[GlyphClusters.Count - 1])
                    {
                        return Metrics.WidthIncludingTrailingWhitespace;
                    }
                }

                var glyphIndex = FindGlyphIndex(characterIndex);

                if (GlyphClusters != null)
                {
                    var currentCluster = GlyphClusters[glyphIndex];

                    //Move to the end of the glyph cluster
                    if (characterHit.TrailingLength > 0)
                    {
                        while (glyphIndex + 1 < GlyphClusters.Count && GlyphClusters[glyphIndex + 1] == currentCluster)
                        {
                            glyphIndex++;
                        }
                    }
                }

                for (var i = 0; i < glyphIndex; i++)
                {
                    distance += GetGlyphAdvance(i, out _);
                }

                return distance;
            }
            else
            {
                //RightToLeft
                var glyphIndex = FindGlyphIndex(characterIndex);

                if (GlyphClusters != null && GlyphClusters.Count > 0)
                {
                    if (characterIndex > GlyphClusters[0])
                    {
                        return 0;
                    }

                    if (characterIndex <= GlyphClusters[GlyphClusters.Count - 1])
                    {
                        return Size.Width;
                    }
                }

                for (var i = glyphIndex + 1; i < GlyphIndices.Count; i++)
                {
                    distance += GetGlyphAdvance(i, out _);
                }

                return Size.Width - distance;
            }
        }

        /// <summary>
        /// Retrieves the <see cref="CharacterHit"/> value that represents the character hit of the caret of the <see cref="GlyphRun"/>.
        /// </summary>
        /// <param name="distance">Offset to use for computing the caret character hit.</param>
        /// <param name="isInside">Determines whether the character hit is inside the <see cref="GlyphRun"/>.</param>
        /// <returns>
        /// A <see cref="CharacterHit"/> value that represents the character hit that is closest to the distance value.
        /// The out parameter <c>isInside</c> returns <c>true</c> if the character hit is inside the <see cref="GlyphRun"/>; otherwise, <c>false</c>.
        /// </returns>
        public CharacterHit GetCharacterHitFromDistance(double distance, out bool isInside)
        {
            var characterIndex = 0;

            // Before
            if (distance <= 0)
            {
                isInside = false;

                if (GlyphClusters != null)
                {
                    characterIndex = GlyphClusters[characterIndex];
                }

                var firstCharacterHit = FindNearestCharacterHit(characterIndex, out _);

                return IsLeftToRight ? new CharacterHit(firstCharacterHit.FirstCharacterIndex) : firstCharacterHit;
            }

            //After
            if (distance >= Size.Width)
            {
                isInside = false;

                characterIndex = GlyphIndices.Count - 1;

                if (GlyphClusters != null)
                {
                    characterIndex = GlyphClusters[characterIndex];
                }

                var lastCharacterHit = FindNearestCharacterHit(characterIndex, out _);

                return IsLeftToRight ? lastCharacterHit : new CharacterHit(lastCharacterHit.FirstCharacterIndex);
            }

            //Within
            var currentX = 0d;

            if (IsLeftToRight)
            {
                for (var index = 0; index < GlyphIndices.Count; index++)
                {
                    var advance = GetGlyphAdvance(index, out var cluster);

                    characterIndex = cluster;

                    if (distance > currentX && distance <= currentX + advance)
                    {
                        break;
                    }

                    currentX += advance;
                }
            }
            else
            {
                currentX = Size.Width;

                for (var index = GlyphIndices.Count - 1; index >= 0; index--)
                {
                    var advance = GetGlyphAdvance(index, out var cluster);

                    characterIndex = cluster;

                    var offsetX = currentX - advance;

                    if (offsetX < distance)
                    {
                        break;
                    }

                    currentX -= advance;
                }
            }

            isInside = true;

            var characterHit = FindNearestCharacterHit(characterIndex, out var width);

            var delta = width / 2;
            
            var offset = IsLeftToRight ? Math.Round(distance - currentX, 3) : Math.Round(currentX - distance, 3);

            var isTrailing = offset > delta;

            return isTrailing ? characterHit : new CharacterHit(characterHit.FirstCharacterIndex);
        }

        /// <summary>
        /// Retrieves the next valid caret character hit in the logical direction in the <see cref="GlyphRun"/>.
        /// </summary>
        /// <param name="characterHit">The <see cref="CharacterHit"/> to use for computing the next hit value.</param>
        /// <returns>
        /// A <see cref="CharacterHit"/> that represents the next valid caret character hit in the logical direction.
        /// If the return value is equal to <c>characterHit</c>, no further navigation is possible in the <see cref="GlyphRun"/>.
        /// </returns>
        public CharacterHit GetNextCaretCharacterHit(CharacterHit characterHit)
        {
            if (characterHit.TrailingLength == 0)
            {
                characterHit = FindNearestCharacterHit(characterHit.FirstCharacterIndex, out _);

                var textPosition = characterHit.FirstCharacterIndex + characterHit.TrailingLength;

                return textPosition > _characters.End ?
                    characterHit :
                    new CharacterHit(characterHit.FirstCharacterIndex + characterHit.TrailingLength);
            }

            var nextCharacterHit =
                FindNearestCharacterHit(characterHit.FirstCharacterIndex + characterHit.TrailingLength, out _);

            if (characterHit == nextCharacterHit)
            {
                return characterHit;
            }

            return characterHit.TrailingLength > 0 ?
                nextCharacterHit :
                new CharacterHit(nextCharacterHit.FirstCharacterIndex);
        }

        /// <summary>
        /// Retrieves the previous valid caret character hit in the logical direction in the <see cref="GlyphRun"/>.
        /// </summary>
        /// <param name="characterHit">The <see cref="CharacterHit"/> to use for computing the previous hit value.</param>
        /// <returns>
        /// A cref="CharacterHit"/> that represents the previous valid caret character hit in the logical direction.
        /// If the return value is equal to <c>characterHit</c>, no further navigation is possible in the <see cref="GlyphRun"/>.
        /// </returns>
        public CharacterHit GetPreviousCaretCharacterHit(CharacterHit characterHit)
        {
            if (characterHit.TrailingLength != 0)
            {
                return new CharacterHit(characterHit.FirstCharacterIndex);
            }

            var previousCharacterHit = FindNearestCharacterHit(characterHit.FirstCharacterIndex - 1, out _);

            return new CharacterHit(previousCharacterHit.FirstCharacterIndex);
        }

        /// <summary>
        /// Finds a glyph index for given character index.
        /// </summary>
        /// <param name="characterIndex">The character index.</param>
        /// <returns>
        /// The glyph index.
        /// </returns>
        public int FindGlyphIndex(int characterIndex)
        {
            if (GlyphClusters == null || GlyphClusters.Count == 0)
            {
                return characterIndex;
            }

            if (IsLeftToRight)
            {
                if (characterIndex < GlyphClusters[0])
                {
                    return 0;
                }

                if (characterIndex > GlyphClusters[GlyphClusters.Count - 1])
                {
                    return GlyphClusters.Count - 1;
                }
            }
            else
            {
                if (characterIndex < GlyphClusters[GlyphClusters.Count - 1])
                {
                    return GlyphClusters.Count - 1;
                }

                if (characterIndex > GlyphClusters[0])
                {
                    return 0;
                }
            }

            var comparer = IsLeftToRight ? s_ascendingComparer : s_descendingComparer;

            var clusters = GlyphClusters;

            // Find the start of the cluster at the character index.
            var start = clusters.BinarySearch(characterIndex, comparer);

            // No cluster found.
            if (start < 0)
            {
                while (characterIndex > 0 && start < 0)
                {
                    characterIndex--;

                    start = clusters.BinarySearch(characterIndex, comparer);
                }

                if (start < 0)
                {
                    return -1;
                }
            }

            if (IsLeftToRight)
            {
                while (start > 0 && clusters[start - 1] == clusters[start])
                {
                    start--;
                }
            }
            else
            {
                while (start + 1 < clusters.Count && clusters[start + 1] == clusters[start])
                {
                    start++;
                }
            }

            return start;
        }

        /// <summary>
        /// Finds the nearest <see cref="CharacterHit"/> at given index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="width">The width of found cluster.</param>
        /// <returns>
        /// The nearest <see cref="CharacterHit"/>.
        /// </returns>
        public CharacterHit FindNearestCharacterHit(int index, out double width)
        {
            width = 0.0;

            var start = FindGlyphIndex(index);

            if (GlyphClusters == null)
            {
                width = GetGlyphAdvance(index, out _);

                return new CharacterHit(start, 1);
            }

            var cluster = GlyphClusters[start];

            var nextCluster = cluster;

            var currentIndex = start;

            while (nextCluster == cluster)
            {
                width += GetGlyphAdvance(currentIndex, out _);

                if (IsLeftToRight)
                {
                    currentIndex++;

                    if (currentIndex == GlyphClusters.Count)
                    {
                        break;
                    }
                }
                else
                {
                    currentIndex--;

                    if (currentIndex < 0)
                    {
                        break;
                    }
                }

                nextCluster = GlyphClusters[currentIndex];
            }           

            int trailingLength;

            if (nextCluster == cluster)
            {
                trailingLength = Characters.Start + Characters.Length - _offsetToFirstCharacter - cluster;
            }
            else
            {
                trailingLength = nextCluster - cluster;
            }

            return new CharacterHit(_offsetToFirstCharacter + cluster, trailingLength);
        }

        /// <summary>
        /// Gets a glyph's width.
        /// </summary>
        /// <param name="index">The glyph index.</param>
        /// <param name="cluster">The current cluster.</param>
        /// <returns>The glyph's width.</returns>
        private double GetGlyphAdvance(int index, out int cluster)
        {
            cluster = GlyphClusters != null ? GlyphClusters[index] : index;

            if (GlyphAdvances != null)
            {
                return GlyphAdvances[index];
            }

            var glyph = GlyphIndices[index];

            return GlyphTypeface.GetGlyphAdvance(glyph) * Scale;
        }

        /// <summary>
        /// Calculates the default baseline origin of the <see cref="GlyphRun"/>.
        /// </summary>
        /// <returns>The baseline origin.</returns>
        private Point CalculateBaselineOrigin()
        {
            return new Point(0, -GlyphTypeface.Metrics.Ascent * Scale);
        }

        private GlyphRunMetrics CreateGlyphRunMetrics()
        {
            var firstCluster = 0;
            var lastCluster = Characters.Length - 1;

            if (!IsLeftToRight)
            {
                var cluster = firstCluster;
                firstCluster = lastCluster;
                lastCluster = cluster;
            }

            if (GlyphClusters != null && GlyphClusters.Count > 0)
            {
                firstCluster = GlyphClusters[0];
                lastCluster = GlyphClusters[GlyphClusters.Count - 1];

                _offsetToFirstCharacter = Math.Max(0, Characters.Start - firstCluster);
            }

            var isReversed = firstCluster > lastCluster;
            var height = GlyphTypeface.Metrics.LineSpacing * Scale;
            var widthIncludingTrailingWhitespace = 0d;

            var trailingWhitespaceLength = GetTrailingWhitespaceLength(isReversed, out var newLineLength, out var glyphCount);

            for (var index = 0; index < GlyphIndices.Count; index++)
            {
                var advance = GetGlyphAdvance(index, out _);

                widthIncludingTrailingWhitespace += advance;
            }

            var width = widthIncludingTrailingWhitespace;

            if (isReversed)
            {
                for (var index = 0; index < glyphCount; index++)
                {
                    width -= GetGlyphAdvance(index, out _);
                }
            }
            else
            {
                for (var index = GlyphIndices.Count - glyphCount; index < GlyphIndices.Count; index++)
                {
                    width -= GetGlyphAdvance(index, out _);
                }
            }

            return new GlyphRunMetrics(width, widthIncludingTrailingWhitespace, trailingWhitespaceLength, newLineLength,
                height);
        }

        private int GetTrailingWhitespaceLength(bool isReversed, out int newLineLength, out int glyphCount)
        {          
            if (isReversed)
            {
                return GetTralingWhitespaceLengthRightToLeft(out newLineLength, out glyphCount);
            }

            glyphCount = 0;
            newLineLength = 0;
            var trailingWhitespaceLength = 0;

            if (GlyphClusters == null)
            {
                for (var i = _characters.Length - 1; i >= 0;)
                {
                    var codepoint = Codepoint.ReadAt(_characters, i, out var count);

                    if (!codepoint.IsWhiteSpace)
                    {
                        break;
                    }

                    if (codepoint.IsBreakChar)
                    {
                        newLineLength++;
                    }

                    trailingWhitespaceLength++;

                    i -= count;
                    glyphCount++;
                }
            }
            else
            {
                for (var i = GlyphClusters.Count - 1; i >= 0; i--)
                {
                    var currentCluster = GlyphClusters[i];
                    var characterIndex = Math.Max(0, currentCluster - _characters.BufferOffset);
                    var codepoint = Codepoint.ReadAt(_characters, characterIndex, out _);

                    if (!codepoint.IsWhiteSpace)
                    {
                        break;
                    }

                    var clusterLength = 1;

                    while(i - 1 >= 0)
                    {
                        var nextCluster = GlyphClusters[i - 1];

                        if(currentCluster == nextCluster)
                        {
                            clusterLength++;
                            i--;

                            continue;
                        }

                        break;
                    }

                    if (codepoint.IsBreakChar)
                    {
                        newLineLength += clusterLength;
                    }

                    trailingWhitespaceLength += clusterLength;
                   
                    glyphCount++;                   
                }
            }

            return trailingWhitespaceLength;
        }

        private int GetTralingWhitespaceLengthRightToLeft(out int newLineLength, out int glyphCount)
        {
            glyphCount = 0;
            newLineLength = 0;
            var trailingWhitespaceLength = 0;

            if (GlyphClusters == null)
            {
                for (var i = 0; i < Characters.Length;)
                {
                    var codepoint = Codepoint.ReadAt(_characters, i, out var count);

                    if (!codepoint.IsWhiteSpace)
                    {
                        break;
                    }

                    if (codepoint.IsBreakChar)
                    {
                        newLineLength++;
                    }

                    trailingWhitespaceLength++;

                    i += count;
                    glyphCount++;
                }
            }
            else
            {
                for (var i = 0; i < GlyphClusters.Count; i++)
                {
                    var currentCluster = GlyphClusters[i];
                    var characterIndex = Math.Max(0, currentCluster - _characters.BufferOffset);
                    var codepoint = Codepoint.ReadAt(_characters, characterIndex, out _);

                    if (!codepoint.IsWhiteSpace)
                    {
                        break;
                    }

                    var clusterLength = 1;

                    var j = i;

                    while (j - 1 >= 0)
                    {
                        var nextCluster = GlyphClusters[--j];

                        if (currentCluster == nextCluster)
                        {
                            clusterLength++;                        

                            continue;
                        }

                        break;
                    }

                    if (codepoint.IsBreakChar)
                    {
                        newLineLength += clusterLength;
                    }

                    trailingWhitespaceLength += clusterLength;

                    glyphCount += clusterLength;
                }
            }

            return trailingWhitespaceLength;
        }

        private void Set<T>(ref T field, T value)
        {
            _glyphRunImpl?.Dispose();

            _glyphRunImpl = null;

            _glyphRunMetrics = null;

            _baselineOrigin = null;

            field = value;
        }

        /// <summary>
        /// Initializes the <see cref="GlyphRun"/>.
        /// </summary>
        private void Initialize()
        {
            if (GlyphIndices == null)
            {
                throw new InvalidOperationException();
            }

            var glyphCount = GlyphIndices.Count;

            if (GlyphAdvances != null && GlyphAdvances.Count > 0 && GlyphAdvances.Count != glyphCount)
            {
                throw new InvalidOperationException();
            }

            if (GlyphOffsets != null && GlyphOffsets.Count > 0 && GlyphOffsets.Count != glyphCount)
            {
                throw new InvalidOperationException();
            }

            _glyphRunImpl = CreateGlyphRunImpl();
        }

        private IGlyphRunImpl CreateGlyphRunImpl()
        {
            IGlyphRunImpl glyphRunImpl;

            var platformRenderInterface = AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>();
            var count = GlyphIndices.Count;
            var scale = (float)(FontRenderingEmSize / GlyphTypeface.Metrics.DesignEmHeight);

            if (GlyphOffsets == null)
            {
                if (GlyphTypeface.Metrics.IsFixedPitch)
                {
                    var buffer = platformRenderInterface.AllocateGlyphRun(GlyphTypeface, (float)FontRenderingEmSize, count);

                    var glyphs = buffer.GlyphIndices;

                    for (int i = 0; i < glyphs.Length; i++)
                    {
                        glyphs[i] = GlyphIndices[i];
                    }

                    glyphRunImpl = buffer.Build();
                }
                else
                {
                    var buffer = platformRenderInterface.AllocateHorizontalGlyphRun(GlyphTypeface, (float)FontRenderingEmSize, count);
                    var glyphs = buffer.GlyphIndices;
                    var positions = buffer.GlyphPositions;
                    var width = 0d;

                    for (var i = 0; i < count; i++)
                    {
                        positions[i] = (float)width;

                        if (GlyphAdvances == null)
                        {
                            width += GlyphTypeface.GetGlyphAdvance(GlyphIndices[i]) * scale;
                        }
                        else
                        {
                            width += GlyphAdvances[i];
                        }

                        glyphs[i] = GlyphIndices[i];
                    }

                    glyphRunImpl = buffer.Build();
                }
            }
            else
            {
                var buffer = platformRenderInterface.AllocatePositionedGlyphRun(GlyphTypeface, (float)FontRenderingEmSize, count);
                var glyphs = buffer.GlyphIndices;
                var glyphPositions = buffer.GlyphPositions;
                var currentX = 0.0;

                for (var i = 0; i < count; i++)
                {
                    var glyphOffset = GlyphOffsets[i];

                    glyphPositions[i] = new PointF((float)(currentX + glyphOffset.X), (float)glyphOffset.Y);

                    if (GlyphAdvances == null)
                    {
                        currentX += GlyphTypeface.GetGlyphAdvance(GlyphIndices[i]) * scale;
                    }
                    else
                    {
                        currentX += GlyphAdvances[i];
                    }

                    glyphs[i] = GlyphIndices[i];
                }

                glyphRunImpl = buffer.Build();
            }

            return glyphRunImpl;
        }

        void IDisposable.Dispose()
        {
            _glyphRunImpl?.Dispose();
        }

        private class ReverseComparer<T> : IComparer<T>
        {
            public int Compare(T? x, T? y)
            {
                return Comparer<T>.Default.Compare(y, x);
            }
        }
    }
}
