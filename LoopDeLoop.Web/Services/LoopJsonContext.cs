using System.Text.Json.Serialization;

namespace LoopDeLoop.Web.Services
{
    [JsonSerializable(typeof(SavedGameState))]
    [JsonSerializable(typeof(UserPreferences))]
    internal partial class LoopJsonContext : JsonSerializerContext
    {
    }
}
