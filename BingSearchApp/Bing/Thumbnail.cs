namespace BingSearchApp.Bing
{
    using System;

    public partial class Thumbnail {
        
        private String _MediaUrl;
        
        private String _ContentType;
        
        private Int32? _Width;
        
        private Int32? _Height;
        
        private Int64? _FileSize;
        
        public String MediaUrl {
            get {
                return _MediaUrl;
            }
            set {
                _MediaUrl = value;
            }
        }
        
        public String ContentType {
            get {
                return _ContentType;
            }
            set {
                _ContentType = value;
            }
        }
        
        public Int32? Width {
            get {
                return _Width;
            }
            set {
                _Width = value;
            }
        }
        
        public Int32? Height {
            get {
                return _Height;
            }
            set {
                _Height = value;
            }
        }
        
        public Int64? FileSize {
            get {
                return _FileSize;
            }
            set {
                _FileSize = value;
            }
        }
    }
}