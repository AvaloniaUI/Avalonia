namespace BingSearchApp.Bing
{
    using System;

    public partial class WebResult {
        
        private Guid _ID;
        
        private String _Title;
        
        private String _Description;
        
        private String _DisplayUrl;
        
        private String _Url;
        
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
        
        public String Description {
            get {
                return _Description;
            }
            set {
                _Description = value;
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
        
        public String Url {
            get {
                return _Url;
            }
            set {
                _Url = value;
            }
        }
    }
}