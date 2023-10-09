using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia.Media.TextFormatting;
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
        private readonly static IPlatformRenderInterface s_renderInterface;

        private IRef<IGlyphRunImpl>? _platformImpl;
        private double _fontRenderingEmSize;
        private int _biDiLevel;
        private GlyphRunMetrics? _glyphRunMetrics;
        private ReadOnlyMemory<char> _characters;
        private IReadOnlyList<GlyphInfo> _glyphInfos;
        private Point? _baselineOrigin;
        private bool _hasOneCharPerCluster; // if true, character index and cluster are similar

        static GlyphRun()
        {
            s_renderInterface = AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GlyphRun"/> class by specifying properties of the class.
        /// </summary>
        /// <param name="glyphTypeface">The glyph typeface.</param>
        /// <param name="fontRenderingEmSize">The rendering em size.</param>
        /// <param name="characters">The characters.</param>
        /// <param name="glyphIndices">The glyph indices.</param>
        /// <param name="baselineOrigin">The baseline origin of the run.</param>
        /// <param name="biDiLevel">The bidi level.</param>
        public GlyphRun(
            IGlyphTypeface glyphTypeface,
            double fontRenderingEmSize,
            ReadOnlyMemory<char> characters,
            IReadOnlyList<ushort> glyphIndices,
            Point? baselineOrigin = null,
            int biDiLevel = 0)
            : this(glyphTypeface, fontRenderingEmSize, characters,
                CreateGlyphInfos(glyphIndices, fontRenderingEmSize, glyphTypeface), baselineOrigin, biDiLevel)
        {
            _hasOneCharPerCluster = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GlyphRun"/> class by specifying properties of the class.
        /// </summary>
        /// <param name="glyphTypeface">The glyph typeface.</param>
        /// <param name="fontRenderingEmSize">The rendering em size.</param>
        /// <param name="characters">The characters.</param>
        /// <param name="glyphInfos">The list of glyphs used.</param>
        /// <param name="baselineOrigin">The baseline origin of the run.</param>
        /// <param name="biDiLevel">The bidi level.</param>
        public GlyphRun(
            IGlyphTypeface glyphTypeface,
            double fontRenderingEmSize,
            ReadOnlyMemory<char> characters,
            IReadOnlyList<GlyphInfo> glyphInfos,
            Point? baselineOrigin = null,
            int biDiLevel = 0)
        {
            GlyphTypeface = glyphTypeface;

            _fontRenderingEmSize = fontRenderingEmSize;

            _characters = characters;

            _glyphInfos = glyphInfos;

            _baselineOrigin = baselineOrigin;

            _biDiLevel = biDiLevel;
        }

        internal GlyphRun(IRef<IGlyphRunImpl> platformImpl)
        {
            _glyphInfos = Array.Empty<GlyphInfo>();
            GlyphTypeface = Typeface.Default.GlyphTypeface;
            _platformImpl = platformImpl;
            _baselineOrigin = platformImpl.Item.BaselineOrigin;
        }

        private static IReadOnlyList<GlyphInfo> CreateGlyphInfos(IReadOnlyList<ushort> glyphIndices,
            double fontRenderingEmSize, IGlyphTypeface glyphTypeface)
        {
            var glyphIndexSpan = ListToSpan(glyphIndices);
            var glyphAdvances = glyphTypeface.GetGlyphAdvances(glyphIndexSpan);

            var glyphInfos = new GlyphInfo[glyphIndexSpan.Length];
            var scale = fontRenderingEmSize / glyphTypeface.Metrics.DesignEmHeight;

            for (var i = 0; i < glyphIndexSpan.Length; ++i)
            {
                glyphInfos[i] = new GlyphInfo(glyphIndexSpan[i], i, glyphAdvances[i] * scale);
            }

            return glyphInfos;
        }

        private static ReadOnlySpan<ushort> ListToSpan(IReadOnlyList<ushort> list)
        {
            var count = list.Count;

            if (count == 0)
            {
                return default;
            }

            if (list is ushort[] array)
            {
                return array.AsSpan();
            }

#if NET6_0_OR_GREATER
            if (list is List<ushort> concreteList)
            {
                return CollectionsMarshal.AsSpan(concreteList);
            }
#endif

            array = new ushort[count];
            for (var i = 0; i < count; ++i)
            {
                array[i] = list[i];
            }

            return array.AsSpan();
        }

        /// <summary>
        ///     Gets the <see cref="IGlyphTypeface"/> for the <see cref="GlyphRun"/>.
        /// </summary>
        public IGlyphTypeface GlyphTypeface { get; }

        /// <summary>
        ///     Gets or sets the em size used for rendering the <see cref="GlyphRun"/>.
        /// </summary>
        public double FontRenderingEmSize
        {
            get => _fontRenderingEmSize;
            set => Set(ref _fontRenderingEmSize, value);
        }

        /// <summary>
        ///     Gets the conservative bounding box of the <see cref="GlyphRun"/>.
        /// </summary>
        public Rect Bounds => new Rect(new Size(Metrics.WidthIncludingTrailingWhitespace, Metrics.Height));

        public Rect InkBounds => PlatformImpl.Item.Bounds;

        /// <summary>
        /// 
        /// </summary>
        public GlyphRunMetrics Metrics
            => _glyphRunMetrics ??= CreateGlyphRunMetrics();

        /// <summary>
        ///     Gets or sets the baseline origin of the<see cref="GlyphRun"/>.
        /// </summary>
        public Point BaselineOrigin
        {
            get => _baselineOrigin ?? new Point(0, Metrics.Baseline);
            set => Set(ref _baselineOrigin, value);
        }

        /// <summary>
        ///     Gets or sets the list of UTF16 code points that represent the Unicode content of the <see cref="GlyphRun"/>.
        /// </summary>
        public ReadOnlyMemory<char> Characters
        {
            get => _characters;
            set => Set(ref _characters, value);
        }

        /// <summary>
        /// Gets or sets the list of glyphs to use to render this run.
        /// </summary>
        public IReadOnlyList<GlyphInfo> GlyphInfos
        {
            get => _glyphInfos;
            set
            {
                Set(ref _glyphInfos, value);
                _hasOneCharPerCluster = false;
            }
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
        /// Gets the scale of the current <see cref="IGlyphTypeface"/>
        /// </summary>
        internal double Scale => FontRenderingEmSize / GlyphTypeface.Metrics.DesignEmHeight;

        /// <summary>
        /// Returns <c>true</c> if the text direction is left-to-right. Otherwise, returns <c>false</c>.
        /// </summary>
        public bool IsLeftToRight => ((BiDiLevel & 1) == 0);

        /// <summary>
        /// The platform implementation of the <see cref="GlyphRun"/>.
        /// </summary>
        internal IRef<IGlyphRunImpl> PlatformImpl
            => _platformImpl ??= CreateGlyphRunImpl();

        /// <summary>
        /// Obtains geometry for the glyph run.
        /// </summary>
        /// <returns>The geometry returned contains the combined geometry of all glyphs in the glyph run.</returns>
        public Geometry BuildGeometry()
        {
            var geometryImpl = s_renderInterface.BuildGlyphRunGeometry(this);

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
            var characterIndex = characterHit.FirstCharacterIndex + characterHit.TrailingLength;

            var distance = 0.0;

            if (IsLeftToRight)
            {
                if (characterIndex < Metrics.FirstCluster)
                {
                    return 0;
                }

                if (characterIndex > Metrics.LastCluster)
                {
                    return Bounds.Width;
                }

                var glyphIndex = FindGlyphIndex(characterIndex);

                var currentCluster = _glyphInfos[glyphIndex].GlyphCluster;

                //Move to the end of the glyph cluster
                if (characterHit.TrailingLength > 0)
                {
                    while (glyphIndex + 1 < _glyphInfos.Count && _glyphInfos[glyphIndex + 1].GlyphCluster == currentCluster)
                    {
                        glyphIndex++;
                    }
                }

                for (var i = 0; i < glyphIndex; i++)
                {
                    distance += _glyphInfos[i].GlyphAdvance;
                }

                return distance;
            }
            else
            {
                //RightToLeft
                var glyphIndex = FindGlyphIndex(characterIndex);

                if (characterIndex > Metrics.LastCluster)
                {
                    return 0;
                }

                if (characterIndex <= Metrics.FirstCluster)
                {
                    return Bounds.Width;
                }

                for (var i = glyphIndex + 1; i < _glyphInfos.Count; i++)
                {
                    distance += _glyphInfos[i].GlyphAdvance;
                }

                return Bounds.Width - distance;
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
            // Before
            if (distance <= 0)
            {
                isInside = false;

                var firstCharacterHit = FindNearestCharacterHit(IsLeftToRight ? Metrics.FirstCluster : Metrics.LastCluster, out _);

                return IsLeftToRight ? new CharacterHit(firstCharacterHit.FirstCharacterIndex) : firstCharacterHit;
            }

            //After
            if (distance >= Bounds.Width)
            {
                isInside = false;

                var lastCharacterHit = FindNearestCharacterHit(IsLeftToRight ? Metrics.LastCluster : Metrics.FirstCluster, out _);

                return IsLeftToRight ? lastCharacterHit : new CharacterHit(lastCharacterHit.FirstCharacterIndex);
            }

            var characterIndex = 0;

            //Within
            var currentX = 0d;

            if (IsLeftToRight)
            {
                for (var index = 0; index < _glyphInfos.Count; index++)
                {
                    var glyphInfo = _glyphInfos[index];
                    var advance = glyphInfo.GlyphAdvance;

                    characterIndex = glyphInfo.GlyphCluster;

                    if (distance > currentX && distance <= currentX + advance)
                    {
                        break;
                    }

                    currentX += advance;
                }
            }
            else
            {
                currentX = Bounds.Width;

                for (var index = _glyphInfos.Count - 1; index >= 0; index--)
                {
                    var glyphInfo = _glyphInfos[index];
                    var advance = glyphInfo.GlyphAdvance;

                    characterIndex = glyphInfo.GlyphCluster;

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

                if (characterHit.FirstCharacterIndex == Metrics.LastCluster)
                {
                    return characterHit;
                }

                return new CharacterHit(characterHit.FirstCharacterIndex + characterHit.TrailingLength);
            }

            return FindNearestCharacterHit(characterHit.FirstCharacterIndex + characterHit.TrailingLength, out _);
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
            //Always produce a hit that is on the left edge

            if (characterHit.TrailingLength > 0)
            {
                var previousCharacterHit = FindNearestCharacterHit(characterHit.FirstCharacterIndex, out _);

                return new CharacterHit(previousCharacterHit.FirstCharacterIndex);
            }
            else
            {
                var previousCharacterHit = FindNearestCharacterHit(characterHit.FirstCharacterIndex - 1, out _);

                return new CharacterHit(previousCharacterHit.FirstCharacterIndex);
            }
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
            if (_hasOneCharPerCluster)
            {
                return characterIndex;
            }

            if (characterIndex > Metrics.LastCluster)
            {
                if (IsLeftToRight)
                {
                    return _glyphInfos.Count - 1;
                }

                return 0;
            }

            if (characterIndex < Metrics.FirstCluster)
            {
                if (IsLeftToRight)
                {
                    return 0;
                }

                return _glyphInfos.Count - 1;
            }

            var comparer = IsLeftToRight ? GlyphInfo.ClusterAscendingComparer : GlyphInfo.ClusterDescendingComparer;

            // Find the start of the cluster at the character index.
            var start = _glyphInfos.BinarySearch(new GlyphInfo(default, characterIndex, default), comparer);

            // No cluster found.
            if (start < 0)
            {
                while (characterIndex > 0 && start < 0)
                {
                    characterIndex--;

                    start = _glyphInfos.BinarySearch(new GlyphInfo(default, characterIndex, default), comparer);
                }

                if (start < 0)
                {
                    return 0;
                }
            }

            if (IsLeftToRight)
            {
                while (start > 0 && _glyphInfos[start - 1].GlyphCluster == _glyphInfos[start].GlyphCluster)
                {
                    start--;
                }
            }
            else
            {
                while (start + 1 < _glyphInfos.Count && _glyphInfos[start + 1].GlyphCluster == _glyphInfos[start].GlyphCluster)
                {
                    start++;
                }
            }

            if (start < 0)
            {
                return 0;
            }

            if (start > _glyphInfos.Count - 1)
            {
                return _glyphInfos.Count - 1;
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

            var glyphIndex = FindGlyphIndex(index);

            if (_hasOneCharPerCluster)
            {
                width = _glyphInfos[index].GlyphAdvance;

                return new CharacterHit(glyphIndex, 1);
            }

            var cluster = _glyphInfos[glyphIndex].GlyphCluster;

            var nextCluster = cluster;

            var currentIndex = glyphIndex;

            while (nextCluster == cluster)
            {
                width += _glyphInfos[currentIndex].GlyphAdvance;

                if (IsLeftToRight)
                {
                    currentIndex++;

                    if (currentIndex == _glyphInfos.Count)
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

                nextCluster = _glyphInfos[currentIndex].GlyphCluster;
            }

            var clusterLength = Math.Max(0, nextCluster - cluster);

            if (cluster == Metrics.LastCluster && clusterLength == 0)
            {
                var characterLength = 0;

                var currentCluster = Metrics.FirstCluster;

                if (IsLeftToRight)
                {
                    for (int i = 1; i < _glyphInfos.Count; i++)
                    {
                        nextCluster = _glyphInfos[i].GlyphCluster;

                        if (currentCluster > cluster)
                        {
                            break;
                        }

                        var length = nextCluster - currentCluster;

                        characterLength += length;

                        currentCluster = nextCluster;
                    }
                }
                else
                {
                    for (int i = _glyphInfos.Count - 1; i >= 0; i--)
                    {
                        nextCluster = _glyphInfos[i].GlyphCluster;

                        if (currentCluster > cluster)
                        {
                            break;
                        }

                        var length = nextCluster - currentCluster;

                        characterLength += length;

                        currentCluster = nextCluster;
                    }
                }

                if (!Characters.IsEmpty)
                {
                    clusterLength = Characters.Length - characterLength;
                }
                else
                {
                    clusterLength = 1;
                }
            }

            return new CharacterHit(cluster, clusterLength);
        }

        private GlyphRunMetrics CreateGlyphRunMetrics()
        {
            int firstCluster, lastCluster;

            if (Characters.IsEmpty)
            {
                firstCluster = 0;
                lastCluster = 0;
            }
            else
            {
                firstCluster = _glyphInfos[0].GlyphCluster;
                lastCluster = _glyphInfos[_glyphInfos.Count - 1].GlyphCluster;
            }

            var isReversed = firstCluster > lastCluster;

            if (!IsLeftToRight)
            {
                (lastCluster, firstCluster) = (firstCluster, lastCluster);
            }

            var height = GlyphTypeface.Metrics.LineSpacing * Scale;
            var widthIncludingTrailingWhitespace = 0d;

            var trailingWhitespaceLength = GetTrailingWhitespaceLength(isReversed, out var newLineLength, out var glyphCount);

            for (var index = 0; index < _glyphInfos.Count; index++)
            {
                var advance = _glyphInfos[index].GlyphAdvance;

                widthIncludingTrailingWhitespace += advance;
            }

            var width = widthIncludingTrailingWhitespace;

            if (isReversed)
            {
                for (var index = 0; index < glyphCount; index++)
                {
                    width -= _glyphInfos[index].GlyphAdvance;
                }
            }
            else
            {
                for (var index = _glyphInfos.Count - glyphCount; index < _glyphInfos.Count; index++)
                {
                    width -= _glyphInfos[index].GlyphAdvance;
                }
            }

            return new GlyphRunMetrics
            {
                Baseline = -GlyphTypeface.Metrics.Ascent * Scale,
                Width = width,
                WidthIncludingTrailingWhitespace = widthIncludingTrailingWhitespace,
                Height = height,
                NewLineLength = newLineLength,
                TrailingWhitespaceLength = trailingWhitespaceLength,
                FirstCluster = firstCluster,
                LastCluster = lastCluster
            };
        }

        private int GetTrailingWhitespaceLength(bool isReversed, out int newLineLength, out int glyphCount)
        {
            if (isReversed)
            {
                return GetTrailingWhitespaceLengthRightToLeft(out newLineLength, out glyphCount);
            }

            glyphCount = 0;
            newLineLength = 0;
            var trailingWhitespaceLength = 0;
            var charactersSpan = _characters.Span;

            if (!charactersSpan.IsEmpty)
            {
                var characterIndex = charactersSpan.Length - 1;

                for (var i = _glyphInfos.Count - 1; i >= 0; i--)
                {
                    var currentCluster = _glyphInfos[i].GlyphCluster;
                    var codepoint = Codepoint.ReadAt(charactersSpan, characterIndex, out var characterLength);

                    characterIndex -= characterLength;

                    if (!codepoint.IsWhiteSpace)
                    {
                        break;
                    }

                    var clusterLength = 1;

                    while (i - 1 >= 0)
                    {
                        var nextCluster = _glyphInfos[i - 1].GlyphCluster;

                        if (currentCluster == nextCluster)
                        {
                            clusterLength++;
                            i--;

                            if (characterIndex >= 0)
                            {
                                codepoint = Codepoint.ReadAt(charactersSpan, characterIndex, out characterLength);

                                characterIndex -= characterLength;
                            }

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

        private int GetTrailingWhitespaceLengthRightToLeft(out int newLineLength, out int glyphCount)
        {
            glyphCount = 0;
            newLineLength = 0;
            var trailingWhitespaceLength = 0;
            var charactersSpan = Characters.Span;

            if (!charactersSpan.IsEmpty)
            {
                var characterIndex = charactersSpan.Length - 1;

                for (var i = 0; i < _glyphInfos.Count; i++)
                {
                    var currentCluster = _glyphInfos[i].GlyphCluster;
                    var codepoint = Codepoint.ReadAt(charactersSpan, characterIndex, out var characterLength);

                    if (!codepoint.IsWhiteSpace)
                    {
                        break;
                    }

                    var clusterLength = 1;

                    var j = i;

                    while (j + 1 < _glyphInfos.Count)
                    {
                        var nextCluster = _glyphInfos[++j].GlyphCluster;

                        if (currentCluster == nextCluster)
                        {
                            clusterLength++;

                            continue;
                        }

                        break;
                    }

                    characterIndex -= clusterLength;

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
            _platformImpl?.Dispose();

            _platformImpl = null;

            _glyphRunMetrics = null;

            field = value;
        }

        private IRef<IGlyphRunImpl> CreateGlyphRunImpl()
        {
            var platformImpl = s_renderInterface.CreateGlyphRun(
                GlyphTypeface,
                FontRenderingEmSize,
                GlyphInfos,
                BaselineOrigin);

            _platformImpl = RefCountable.Create(platformImpl);

            return _platformImpl;
        }

        public void Dispose()
        {
            _platformImpl?.Dispose();
            _platformImpl = null;
        }

        /// <summary>
        /// Gets the intersections of specified upper and lower limit.
        /// </summary>
        /// <param name="lowerLimit">Upper limit.</param>
        /// <param name="upperLimit">Lower limit.</param>
        /// <returns></returns>
        public IReadOnlyList<float> GetIntersections(float lowerLimit, float upperLimit)
        {
            return PlatformImpl.Item.GetIntersections(lowerLimit, upperLimit);
        }

        public IImmutableGlyphRunReference? TryCreateImmutableGlyphRunReference()
            => new ImmutableGlyphRunReference(PlatformImpl.Clone());
    }
}
