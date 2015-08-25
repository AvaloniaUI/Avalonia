namespace BingSearchApp.Bing
{
    using System;

    public partial class SpellResult {
        
        private Guid _ID;
        
        private String _Value;
        
        public Guid ID {
            get {
                return _ID;
            }
            set {
                _ID = value;
            }
        }
        
        public String Value {
            get {
                return _Value;
            }
            set {
                _Value = value;
            }
        }
    }
}