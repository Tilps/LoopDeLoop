using System.Text.Json.Serialization;

namespace LoopDeLoop.Web.Services
{
    [JsonSerializable(typeof(SavedGameState))]
    [JsonSerializable(typeof(UserPreferences))]
    public partial class LoopJsonContext : JsonSerializerContext
    {
    }
}
