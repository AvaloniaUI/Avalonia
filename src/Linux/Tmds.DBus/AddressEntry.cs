// Copyright 2006 Alp Toker <alp@atoker.com>
// Copyright 2010 Alan McGovern <alan.mcgovern@gmail.com>
// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus.Transports;

namespace Tmds.DBus
{
    internal class AddressEntry
    {
        public static AddressEntry[] ParseEntries(string addresses)
        {
            if (addresses == null)
                throw new ArgumentNullException(nameof(addresses));

            List<AddressEntry> entries = new List<AddressEntry>();

            foreach (string entryStr in addresses.Split(';'))
                entries.Add(AddressEntry.Parse(entryStr));

            return entries.ToArray();
        }

        public string Method;
        public readonly IDictionary<string, string> Properties = new Dictionary<string, string>();
        public Guid Guid = Guid.Empty;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Method);
            sb.Append(':');

            bool first = true;
            foreach (KeyValuePair<string, string> prop in Properties)
            {
                if (first)
                    first = false;
                else
                    sb.Append(',');

                sb.Append(prop.Key);
                sb.Append('=');
                sb.Append(Escape(prop.Value));
            }

            if (Guid != Guid.Empty)
            {
                if (Properties.Count != 0)
                    sb.Append(',');
                sb.Append("guid");
                sb.Append('=');
                sb.Append(Guid.ToString("N"));
            }

            return sb.ToString();
        }

        static string Escape(string str)
        {
            if (str == null)
                return String.Empty;

            StringBuilder sb = new StringBuilder();
            int len = str.Length;

            for (int i = 0; i != len; i++)
            {
                char c = str[i];

                //everything other than the optionally escaped chars _must_ be escaped
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')
                    || c == '-' || c == '_' || c == '/' || c == '\\' || c == '.')
                    sb.Append(c);
                else
                    sb.Append(HexEscape(c));
            }

            return sb.ToString();
        }

        static string Unescape(string str)
        {
            if (str == null)
                return String.Empty;

            StringBuilder sb = new StringBuilder();
            int len = str.Length;
            int i = 0;
            while (i != len)
            {
                if (IsHexEncoding(str, i))
                    sb.Append(HexUnescape(str, ref i));
                else
                    sb.Append(str[i++]);
            }

            return sb.ToString();
        }

        public static string HexEscape(char character)
        {
            if (character > '\xff')
            {
                throw new ArgumentOutOfRangeException("character");
            }
            char[] chars = new char[3];
            int pos = 0;
            EscapeAsciiChar(character, chars, ref pos);
            return new string(chars);
        }

        private const char c_DummyChar = (char)0xFFFF;
        private static readonly char[] s_hexUpperChars = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };


        internal static void EscapeAsciiChar(char ch, char[] to, ref int pos)
        {
            to[pos++] = '%';
            to[pos++] = s_hexUpperChars[(ch & 0xf0) >> 4];
            to[pos++] = s_hexUpperChars[ch & 0xf];
        }


        public static char HexUnescape(string pattern, ref int index)
        {
            if ((index < 0) || (index >= pattern.Length))
            {
                throw new ArgumentOutOfRangeException("index");
            }
            if ((pattern[index] == '%')
                && (pattern.Length - index >= 3))
            {
                char ret = EscapedAscii(pattern[index + 1], pattern[index + 2]);
                if (ret != c_DummyChar)
                {
                    index += 3;
                    return ret;
                }
            }
            return pattern[index++];
        }
        public static bool IsHexEncoding(string pattern, int index)
        {
            if ((pattern.Length - index) < 3)
            {
                return false;
            }
            if ((pattern[index] == '%') && EscapedAscii(pattern[index + 1], pattern[index + 2]) != c_DummyChar)
            {
                return true;
            }
            return false;
        }

        private static char EscapedAscii(char digit, char next)
        {
            if (!(((digit >= '0') && (digit <= '9'))
                || ((digit >= 'A') && (digit <= 'F'))
                || ((digit >= 'a') && (digit <= 'f'))))
            {
                return c_DummyChar;
            }

            int res = (digit <= '9')
                ? ((int)digit - (int)'0')
                : (((digit <= 'F')
                ? ((int)digit - (int)'A')
                : ((int)digit - (int)'a'))
                   + 10);

            if (!(((next >= '0') && (next <= '9'))
                || ((next >= 'A') && (next <= 'F'))
                || ((next >= 'a') && (next <= 'f'))))
            {
                return c_DummyChar;
            }

            return (char)((res << 4) + ((next <= '9')
                    ? ((int)next - (int)'0')
                    : (((next <= 'F')
                        ? ((int)next - (int)'A')
                        : ((int)next - (int)'a'))
                       + 10)));
        }


        public static AddressEntry Parse(string s)
        {
            AddressEntry entry = new AddressEntry();

            string[] parts = s.Split(new[] { ':' }, 2);

            if (parts.Length < 2)
                throw new FormatException("No colon found");

            entry.Method = parts[0];

            if (parts[1].Length > 0)
            {
                foreach (string propStr in parts[1].Split(','))
                {
                    parts = propStr.Split('=');

                    if (parts.Length < 2)
                        throw new FormatException("No equals sign found");
                    if (parts.Length > 2)
                        throw new FormatException("Too many equals signs found");

                    if (parts[0] == "guid")
                    {
                        try
                        {
                            entry.Guid = Guid.ParseExact(parts[1], "N");
                        }
                        catch
                        {
                            throw new FormatException("Invalid guid specified");
                        }
                        continue;
                    }

                    entry.Properties[parts[0]] = Unescape(parts[1]);
                }
            }

            return entry;
        }

        public async Task<EndPoint[]> ResolveAsync(bool listen = false)
        {
            switch (Method)
            {
                case "tcp":
                    {
                        string host, portStr, family;
                        int port = 0;

                        if (!Properties.TryGetValue ("host", out host))
                            host = "localhost";

                        if (!Properties.TryGetValue ("port", out portStr) && !listen)
                            throw new FormatException ("No port specified");

                        if (portStr != null && !Int32.TryParse (portStr, out port))
                            throw new FormatException("Invalid port: \"" + port + "\"");

                        if (!Properties.TryGetValue ("family", out family))
                            family = null;

                        if (string.IsNullOrEmpty(host))
                        {
                            throw new ArgumentException("host");
                        }

                        IPAddress[] addresses = await Dns.GetHostAddressesAsync(host).ConfigureAwait(false);

                        var endpoints = new IPEndPoint[addresses.Length];
                        for (int i = 0; i < endpoints.Length; i++)
                        {
                            endpoints[i] = new IPEndPoint(addresses[i], port);
                        }
                        return endpoints;
                    }
                case "unix":
                    {
                        string path;
                        bool abstr;

                        if (Properties.TryGetValue("path", out path))
                            abstr = false;
                        else if (Properties.TryGetValue("abstract", out path))
                            abstr = true;
                        else
                            throw new ArgumentException("No path specified for UNIX transport");

                        if (String.IsNullOrEmpty(path))
                        {
                            throw new ArgumentException("path");
                        }

                        if (abstr)
                        {
                            path = (char)'\0' + path;
                        }

                        return new EndPoint[] { new UnixDomainSocketEndPoint(path) };
                    }
                default:
                    throw new NotSupportedException("Transport method \"" + Method + "\" not supported");
            }
        }
    }
}
