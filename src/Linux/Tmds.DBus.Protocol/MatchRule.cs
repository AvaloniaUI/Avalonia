namespace Tmds.DBus.Protocol;

struct MatchRuleData
{
    public MessageType? MessageType { get; set; }

    public string? Sender { get; set; }

    public string? Interface { get; set; }

    public string? Member { get; set; }

    public string? Path { get; set; }

    public string? PathNamespace { get; set; }

    public string? Destination { get; set; }

    public string? Arg0 { get; set; }

    public string? Arg0Path { get; set; }

    public string? Arg0Namespace { get; set; }

    public string GetRuleString()
    {
        var sb = new StringBuilder(); // TODO (perf): pool

        if (MessageType.HasValue)
        {
            string? typeMatch = MessageType switch
            {
                Protocol.MessageType.MethodCall => "type=method_call",
                Protocol.MessageType.MethodReturn => "type=method_return",
                Protocol.MessageType.Error => "type=error",
                Protocol.MessageType.Signal => "type=signal",
                _ => null
            };

            if (typeMatch is not null)
            {
                sb.Append(typeMatch);
            }
        }

        Append(sb, "sender", Sender);
        Append(sb, "interface", Interface);
        Append(sb, "member", Member);
        Append(sb, "path", Path);
        Append(sb, "pathNamespace", PathNamespace);
        Append(sb, "destination", Destination);
        Append(sb, "arg0", Arg0);
        Append(sb, "arg0Path", Arg0Path);
        Append(sb, "arg0Namespace", Arg0Namespace);

        return sb.ToString();

        static void Append(StringBuilder sb, string key, string? value)
        {
            if (value is null)
            {
                return;
            }

            sb.Append($"{(sb.Length > 0 ? ',' : "")}{key}=");

            bool quoting = false;

            ReadOnlySpan<char> span = value.AsSpan();
            while (!span.IsEmpty)
            {
                int specialPos = span.IndexOfAny((ReadOnlySpan<char>)new char[] { ',', '\'' });
                if (specialPos == -1)
                {
                    sb.Append(span);
                    break;
                }
                bool isComma = span[specialPos] == ',';
                if (isComma && !quoting ||
                    !isComma && quoting)
                {
                    sb.Append("'");
                    quoting = !quoting;
                }
                sb.Append(span.Slice(0, specialPos + (isComma ? 1 : 0)));
                if (!isComma)
                {
                    sb.Append("\\'");
                }
                span = span.Slice(specialPos + 1);
            }

            if (quoting)
            {
                sb.Append("'");
                quoting = false;
            }
        }
    }
}

public sealed class MatchRule
{
    private MatchRuleData _data;

    internal MatchRuleData Data => _data;

    public MessageType? Type { get => _data.MessageType; set => _data.MessageType = value; }
    public string? Sender { get => _data.Sender; set => _data.Sender = value; }
    public string? Interface { get => _data.Interface; set => _data.Interface = value; }
    public string? Member { get => _data.Member; set => _data.Member = value; }
    public string? Path { get => _data.Path; set => _data.Path = value; }
    public string? PathNamespace { get => _data.PathNamespace; set => _data.PathNamespace = value; }
    public string? Destination { get => _data.Destination; set => _data.Destination = value; }
    public string? Arg0 { get => _data.Arg0; set => _data.Arg0 = value; }
    public string? Arg0Path { get => _data.Arg0Path; set => _data.Arg0Path = value; }
    public string? Arg0Namespace { get => _data.Arg0Namespace; set => _data.Arg0Namespace = value; }

    public override string ToString() => _data.GetRuleString();
}