// Copyright 2006 Alp Toker <alp@atoker.com>
// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System.Text;

namespace Tmds.DBus
{
    internal class SignalMatchRule
    {
        public string Interface { get; set; }
        public string Member { get; set; }
        public ObjectPath2? Path { get; set; }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + (Interface == null ? 0 : Interface.GetHashCode());
            hash = hash * 23 + (Member == null ? 0 : Member.GetHashCode());
            hash = hash * 23 + Path.GetHashCode();
            return hash;
        }

        public override bool Equals(object o)
        {
            SignalMatchRule r = o as SignalMatchRule;
            if (o == null)
                return false;

            return Interface == r.Interface &&
                Member == r.Member &&
                Path == r.Path;
        }

        public override string ToString()
        {
            return ToStringWithSender(null);
        }

        private string ToStringWithSender(string sender)
        {
            StringBuilder sb = new StringBuilder();

            Append(sb, "type", "signal");

            if (Interface != null)
            {
                Append(sb, "interface", Interface);
            }
            if (Member != null)
            {
                Append(sb, "member", Member);
            }
            if (Path != null)
            {
                Append(sb, "path", Path.Value);
            }
            if (sender != null)
            {
                Append(sb, "sender", sender);
            }

            return sb.ToString();
        }

        protected static void Append(StringBuilder sb, string key, object value)
        {
            Append(sb, key, value.ToString());
        }

        static void Append(StringBuilder sb, string key, string value)
        {
            if (sb.Length != 0)
                sb.Append(',');

            sb.Append(key);
            sb.Append("='");
            sb.Append(value.Replace(@"\", @"\\").Replace(@"'", @"\'"));
            sb.Append('\'');
        }
    }
}
