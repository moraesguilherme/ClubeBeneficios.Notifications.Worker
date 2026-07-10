using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using ClubeBeneficios.Notifications.Worker.Models;

namespace ClubeBeneficios.Notifications.Worker.Services;

public partial class SimpleTemplateRenderer : ITemplateRenderer
{
    public RenderedNotification Render(NotificationMessage message)
    {
        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        var values = ParsePayload(message.PayloadJson);

        var subjectTemplate = message.SubjectTemplate ?? string.Empty;
        var bodyHtmlTemplate = message.BodyHtmlTemplate ?? string.Empty;
        var bodyTextTemplate = message.BodyTextTemplate;

        if (string.IsNullOrWhiteSpace(subjectTemplate))
        {
            throw new InvalidOperationException($"Template de assunto nÃ£o encontrado para a notificaÃ§Ã£o {message.Id}.");
        }

        if (string.IsNullOrWhiteSpace(bodyHtmlTemplate) && string.IsNullOrWhiteSpace(bodyTextTemplate))
        {
            throw new InvalidOperationException($"Template de corpo nÃ£o encontrado para a notificaÃ§Ã£o {message.Id}.");
        }

        return new RenderedNotification
        {
            Subject = RenderText(subjectTemplate, values),
            BodyHtml = RenderHtml(bodyHtmlTemplate, values),
            BodyText = string.IsNullOrWhiteSpace(bodyTextTemplate)
                ? null
                : RenderText(bodyTextTemplate, values)
        };
    }

    private static Dictionary<string, string?> ParsePayload(string? payloadJson)
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return values;
        }

        using var document = JsonDocument.Parse(payloadJson);

        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            return values;
        }

        foreach (var property in document.RootElement.EnumerateObject())
        {
            values[property.Name] = ConvertJsonElementToString(property.Value);
        }

        return values;
    }

    private static string? ConvertJsonElementToString(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            _ => element.GetRawText()
        };
    }

    private static string RenderText(string template, IReadOnlyDictionary<string, string?> values)
    {
        return PlaceholderRegex().Replace(template, match =>
        {
            var key = match.Groups[1].Value;
            return values.TryGetValue(key, out var value)
                ? value ?? string.Empty
                : string.Empty;
        });
    }

    private static string RenderHtml(string template, IReadOnlyDictionary<string, string?> values)
    {
        return PlaceholderRegex().Replace(template, match =>
        {
            var key = match.Groups[1].Value;

            if (!values.TryGetValue(key, out var value) || value is null)
            {
                return string.Empty;
            }

            return WebUtility.HtmlEncode(value);
        });
    }

    [GeneratedRegex(@"\{\{\s*([A-Za-z0-9_]+)\s*\}\}", RegexOptions.Compiled)]
    private static partial Regex PlaceholderRegex();
}
