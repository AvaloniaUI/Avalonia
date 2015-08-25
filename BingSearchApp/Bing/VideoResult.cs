namespace BingSearchApp.Bing
{
    using System;

    public partial class VideoResult {
        
        private Guid _ID;
        
        private String _Title;
        
        private String _MediaUrl;
        
        private String _DisplayUrl;
        
        private Int32? _RunTime;
        
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
        
        public String DisplayUrl {
            get {
                return _DisplayUrl;
            }
            set {
                _DisplayUrl = value;
            }
        }
        
        public Int32? RunTime {
            get {
                return _RunTime;
            }
            set {
                _RunTime = value;
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