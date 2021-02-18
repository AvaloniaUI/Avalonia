using System.Collections.Generic;
using MicroComGenerator.Ast;

namespace MicroComGenerator
{
    public class AstParser
    {
        public static AstIdlNode Parse(string source)
        {
            var parser = new TokenParser(source);
            var idl = new AstIdlNode { Attributes = ParseGlobalAttributes(ref parser) };

            while (!parser.Eof)
            {
                var attrs = ParseLocalAttributes(ref parser);
                if (parser.TryConsume(";"))
                    continue;
                if (parser.TryParseKeyword("enum"))
                    idl.Enums.Add(ParseEnum(attrs, ref parser));
                else if (parser.TryParseKeyword("struct"))
                    idl.Structs.Add(ParseStruct(attrs, ref parser));
                else if (parser.TryParseKeyword("interface"))
                    idl.Interfaces.Add(ParseInterface(attrs, ref parser));
                else
                    throw new ParseException("Unexpected character", ref parser);
            }

            return idl;
        }

        static AstAttributes ParseGlobalAttributes(ref TokenParser parser)
        {
            var rv = new AstAttributes();
            while (!parser.Eof)
            {
                parser.SkipWhitespace();
                if (parser.TryConsume('@'))
                {
                    var ident = parser.ParseIdentifier("-");
                    var value = parser.ReadToEol().Trim();
                    if (value == "@@")
                    {
                        parser.Advance(1);
                        value = "";
                        while (true)
                        {
                            var l = parser.ReadToEol();
                            if (l == "@@")
                                break;
                            else
                                value = value.Length == 0 ? l : (value + "\n" + l);
                            parser.Advance(1);
                        }

                    }
                    rv.Add(new AstAttributeNode(ident, value));
                }
                else
                    return rv;
            }

            return rv;
        }

        static AstAttributes ParseLocalAttributes(ref TokenParser parser)
        {
            var rv = new AstAttributes();
            while (parser.TryConsume("["))
            {
                while (!parser.TryConsume("]") && !parser.Eof)
                {
                    if (parser.TryConsume(','))
                        continue;

                    // Get identifier
                    var ident = parser.ParseIdentifier("-");

                    // No value, end of attribute list
                    if (parser.TryConsume(']'))
                    {
                        rv.Add(new AstAttributeNode(ident, null));
                        break;
                    }
                    // No value, next attribute
                    else if (parser.TryConsume(','))
                        rv.Add(new AstAttributeNode(ident, null));
                    // Has value
                    else if (parser.TryConsume('('))
                    {
                        var value = parser.ReadTo(')');
                        parser.Consume(')');
                        rv.Add(new AstAttributeNode(ident, value));
                    }
                    else
                        throw new ParseException("Unexpected character", ref parser);
                }

                if (parser.Eof)
                    throw new ParseException("Unexpected EOF", ref parser);
            }

            return rv;
        }

        static void EnsureOpenBracket(ref TokenParser parser)
        {
            if (!parser.TryConsume('{'))
                throw new ParseException("{ expected", ref parser);
        }

        static AstEnumNode ParseEnum(AstAttributes attrs, ref TokenParser parser)
        {
            var name = parser.ParseIdentifier();
            EnsureOpenBracket(ref parser);
            var rv = new AstEnumNode { Name = name, Attributes = attrs };
            while (!parser.TryConsume('}') && !parser.Eof)
            {
                if (parser.TryConsume(','))
                    continue;

                var ident = parser.ParseIdentifier();

                // Automatic value
                if (parser.TryConsume(',') || parser.Peek == '}')
                {
                    rv.Add(new AstEnumMemberNode(ident, null));
                    continue;
                }

                if (!parser.TryConsume('='))
                    throw new ParseException("Unexpected character", ref parser);

                var value = parser.ReadToAny(",}").Trim();
                rv.Add(new AstEnumMemberNode(ident, value));
                
                if (parser.Eof)
                    throw new ParseException("Unexpected EOF", ref parser);
            }


            return rv;
        }

        static AstTypeNode ParseType(ref TokenParser parser)
        {
            var ident = parser.ParseIdentifier();
            var t = new AstTypeNode { Name = ident };
            while (parser.TryConsume('*'))
                t.PointerLevel++;
            if (parser.TryConsume("&"))
                t.IsLink = true;
            return t;
        }

        static AstStructNode ParseStruct(AstAttributes attrs, ref TokenParser parser)
        {
            var name = parser.ParseIdentifier();
            EnsureOpenBracket(ref parser);
            var rv = new AstStructNode { Name = name, Attributes = attrs };
            while (!parser.TryConsume('}') && !parser.Eof)
            {
                var memberAttrs = ParseLocalAttributes(ref parser);
                var t = ParseType(ref parser);
                bool parsedAtLeastOneMember = false;
                while (!parser.TryConsume(';'))
                {
                    // Skip any ,
                    while (parser.TryConsume(',')) { }

                    var ident = parser.ParseIdentifier();
                    parsedAtLeastOneMember = true;
                    rv.Add(new AstStructMemberNode { Name = ident, Type = t, Attributes = memberAttrs});
                }

                if (!parsedAtLeastOneMember)
                    throw new ParseException("Expected at least one enum member with declared type " + t, ref parser);
            }

            return rv;
        }

        static AstInterfaceNode ParseInterface(AstAttributes interfaceAttrs, ref TokenParser parser)
        {
            var interfaceName = parser.ParseIdentifier();
            string inheritsFrom = null; 
            if (parser.TryConsume(":")) 
                inheritsFrom = parser.ParseIdentifier();

            EnsureOpenBracket(ref parser);
            var rv = new AstInterfaceNode
            {
                Name = interfaceName, Attributes = interfaceAttrs, Inherits = inheritsFrom
            };
            while (!parser.TryConsume('}') && !parser.Eof)
            {
                var memberAttrs = ParseLocalAttributes(ref parser);
                var returnType = ParseType(ref parser);
                var name = parser.ParseIdentifier();
                var member = new AstInterfaceMemberNode
                {
                    Name = name, ReturnType = returnType, Attributes = memberAttrs
                };
                rv.Add(member);

                parser.Consume('(');
                while (true)
                {
                    if (parser.TryConsume(')'))
                        break;
                    
                    var argumentAttrs = ParseLocalAttributes(ref parser);
                    var type = ParseType(ref parser);
                    var argName = parser.ParseIdentifier();
                    member.Add(new AstInterfaceMemberArgumentNode
                    {
                        Name = argName, Type = type, Attributes = argumentAttrs
                    });

                    if (parser.TryConsume(')'))
                        break;
                    if (parser.TryConsume(','))
                        continue;
                    throw new ParseException("Unexpected character", ref parser);
                }

                parser.Consume(';');
            }

            return rv;
        }
    }
}
