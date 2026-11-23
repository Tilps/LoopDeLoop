using System.Text.Json.Serialization;

namespace LoopDeLoop.Web.Services
{
    [JsonSerializable(typeof(SavedGameState))]
    [JsonSerializable(typeof(UserPreferences))]
    [JsonSerializable(typeof(SolveInteractionLog))]
    public partial class LoopJsonContext : JsonSerializerContext
    {
    }
}
