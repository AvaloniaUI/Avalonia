namespace BingSearchApp.Bing
{
    using System;
    using System.Collections.ObjectModel;

    public partial class ExpandableSearchResult {
        
        private Guid _ID;
        
        private Int64? _WebTotal;
        
        private Int64? _WebOffset;
        
        private Int64? _ImageTotal;
        
        private Int64? _ImageOffset;
        
        private Int64? _VideoTotal;
        
        private Int64? _VideoOffset;
        
        private Int64? _NewsTotal;
        
        private Int64? _NewsOffset;
        
        private Int64? _SpellingSuggestionsTotal;
        
        private String _AlteredQuery;
        
        private String _AlterationOverrideQuery;
        
        private Collection<WebResult> _Web;
        
        private Collection<ImageResult> _Image;
        
        private Collection<VideoResult> _Video;
        
        private Collection<NewsResult> _News;
        
        private Collection<RelatedSearchResult> _RelatedSearch;
        
        private Collection<SpellResult> _SpellingSuggestions;
        
        public Guid ID {
            get {
                return _ID;
            }
            set {
                _ID = value;
            }
        }
        
        public Int64? WebTotal {
            get {
                return _WebTotal;
            }
            set {
                _WebTotal = value;
            }
        }
        
        public Int64? WebOffset {
            get {
                return _WebOffset;
            }
            set {
                _WebOffset = value;
            }
        }
        
        public Int64? ImageTotal {
            get {
                return _ImageTotal;
            }
            set {
                _ImageTotal = value;
            }
        }
        
        public Int64? ImageOffset {
            get {
                return _ImageOffset;
            }
            set {
                _ImageOffset = value;
            }
        }
        
        public Int64? VideoTotal {
            get {
                return _VideoTotal;
            }
            set {
                _VideoTotal = value;
            }
        }
        
        public Int64? VideoOffset {
            get {
                return _VideoOffset;
            }
            set {
                _VideoOffset = value;
            }
        }
        
        public Int64? NewsTotal {
            get {
                return _NewsTotal;
            }
            set {
                _NewsTotal = value;
            }
        }
        
        public Int64? NewsOffset {
            get {
                return _NewsOffset;
            }
            set {
                _NewsOffset = value;
            }
        }
        
        public Int64? SpellingSuggestionsTotal {
            get {
                return _SpellingSuggestionsTotal;
            }
            set {
                _SpellingSuggestionsTotal = value;
            }
        }
        
        public String AlteredQuery {
            get {
                return _AlteredQuery;
            }
            set {
                _AlteredQuery = value;
            }
        }
        
        public String AlterationOverrideQuery {
            get {
                return _AlterationOverrideQuery;
            }
            set {
                _AlterationOverrideQuery = value;
            }
        }
        
        public Collection<WebResult> Web {
            get {
                return _Web;
            }
            set {
                _Web = value;
            }
        }
        
        public Collection<ImageResult> Image {
            get {
                return _Image;
            }
            set {
                _Image = value;
            }
        }
        
        public Collection<VideoResult> Video {
            get {
                return _Video;
            }
            set {
                _Video = value;
            }
        }
        
        public Collection<NewsResult> News {
            get {
                return _News;
            }
            set {
                _News = value;
            }
        }
        
        public Collection<RelatedSearchResult> RelatedSearch {
            get {
                return _RelatedSearch;
            }
            set {
                _RelatedSearch = value;
            }
        }
        
        public Collection<SpellResult> SpellingSuggestions {
            get {
                return _SpellingSuggestions;
            }
            set {
                _SpellingSuggestions = value;
            }
        }
    }
}