// source/FlowHub.Core/Captures/CaptureCursor.cs
using System.Text;
using System.Text.Json;

namespace FlowHub.Core.Captures;

public sealed record CaptureCursor(DateTimeOffset CreatedAt, Guid Id)
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    public string Encode()
    {
        var json = JsonSerializer.Serialize(this, Options);
        var bytes = Encoding.UTF8.GetBytes(json);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public static CaptureCursor Decode(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            throw new FormatException("Cursor must be a non-empty Base64Url-encoded value.");
        }

        try
        {
            var padded = token.Replace('-', '+').Replace('_', '/');
            padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
            var bytes = Convert.FromBase64String(padded);
            var json = Encoding.UTF8.GetString(bytes);
            var cursor = JsonSerializer.Deserialize<CaptureCursor>(json, Options);
            return cursor ?? throw new FormatException("Cursor decoded to null.");
        }
        catch (Exception ex) when (ex is not FormatException)
        {
            throw new FormatException("Cursor is not a valid Base64Url-encoded JSON document.", ex);
        }
    }
}
