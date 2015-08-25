namespace BingSearchApp.Bing
{
    using System;

    public partial class RelatedSearchResult {
        
        private Guid _ID;
        
        private String _Title;
        
        private String _BingUrl;
        
        public Guid ID {
            get {
                return _ID;
            }
            set {
                _ID = value;
            }
        }
        
        public String Title {
            get {
                return _Title;
            }
            set {
                _Title = value;
            }
        }
        
        public String BingUrl {
            get {
                return _BingUrl;
            }
            set {
                _BingUrl = value;
            }
        }
    }
}