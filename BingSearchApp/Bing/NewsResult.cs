namespace BingSearchApp.Bing
{
    using System;

    public partial class NewsResult {
        
        private Guid _ID;
        
        private String _Title;
        
        private String _Url;
        
        private String _Source;
        
        private String _Description;
        
        private DateTime? _Date;
        
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
        
        public String Url {
            get {
                return _Url;
            }
            set {
                _Url = value;
            }
        }
        
        public String Source {
            get {
                return _Source;
            }
            set {
                _Source = value;
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
        
        public DateTime? Date {
            get {
                return _Date;
            }
            set {
                _Date = value;
            }
        }
    }
}