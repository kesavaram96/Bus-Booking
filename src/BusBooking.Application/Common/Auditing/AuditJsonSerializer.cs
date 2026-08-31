using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace BusBooking.Application.Common.Auditing;

/// <summary>
/// The one place every audit NewValues/OldValues payload is serialized — a single, reusable
/// choke point rather than trusting each of the ~18 audited commands' response DTOs to never
/// grow a sensitive field. Any property whose name contains one of the doc's "do not log"
/// terms (password, token, card number, CVV, ...) is redacted at serialization time, regardless
/// of which DTO it's found on — defense in depth, not a one-off fix for AuthResult specifically.
/// </summary>
public static class AuditJsonSerializer
{
    private const string RedactedValue = "***REDACTED***";

    private static readonly string[] SensitivePropertyNameFragments =
    [
        "password", "accesstoken", "refreshtoken", "token", "cvv", "cardnumber", "securitycode", "secret"
    ];

    private static readonly JsonSerializerOptions Options = new()
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers = { RedactSensitiveProperties }
        }
    };

    public static string? Serialize(object? value) =>
        value is null ? null : JsonSerializer.Serialize(value, value.GetType(), Options);

    private static void RedactSensitiveProperties(JsonTypeInfo typeInfo)
    {
        foreach (var property in typeInfo.Properties)
        {
            // Only string-typed properties: a name match on a non-string property (e.g.
            // AccessTokenExpiresAtUtc, a DateTime, matches the "token" fragment) isn't itself a
            // secret value, and replacing its getter with a string would throw an
            // InvalidCastException during serialization.
            if (property.PropertyType == typeof(string) &&
                SensitivePropertyNameFragments.Any(fragment => property.Name.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
            {
                property.Get = _ => RedactedValue;
            }
        }
    }
}
