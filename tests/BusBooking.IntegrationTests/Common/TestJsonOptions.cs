using System.Text.Json;
using System.Text.Json.Serialization;

namespace BusBooking.IntegrationTests.Common;

/// <summary>
/// Mirrors the server's JSON configuration (Program.cs registers JsonStringEnumConverter)
/// so test clients can deserialize enum-bearing DTOs like BusDto.
/// </summary>
public static class TestJsonOptions
{
    public static readonly JsonSerializerOptions Default = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
