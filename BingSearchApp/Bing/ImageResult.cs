namespace BingSearchApp.Bing
{
    using System;

    public partial class ImageResult {
        
        private Guid _ID;
        
        private String _Title;
        
        private String _MediaUrl;
        
        private String _SourceUrl;
        
        private String _DisplayUrl;
        
        private Int32? _Width;
        
        private Int32? _Height;
        
        private Int64? _FileSize;
        
        private String _ContentType;
        
        private Thumbnail _Thumbnail;
        
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
        
        public String MediaUrl {
            get {
                return _MediaUrl;
            }
            set {
                _MediaUrl = value;
            }
        }
        
        public String SourceUrl {
            get {
                return _SourceUrl;
            }
            set {
                _SourceUrl = value;
            }
        }
        
        public String DisplayUrl {
            get {
                return _DisplayUrl;
            }
            set {
                _DisplayUrl = value;
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
        
        public String ContentType {
            get {
                return _ContentType;
            }
            set {
                _ContentType = value;
            }
        }
        
        public Thumbnail Thumbnail {
            get {
                return _Thumbnail;
            }
            set {
                _Thumbnail = value;
            }
        }
    }
}