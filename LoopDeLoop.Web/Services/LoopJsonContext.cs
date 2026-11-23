using System.Text.Json.Serialization;

namespace LoopDeLoop.Web.Services
{
    [JsonSerializable(typeof(SavedGameState))]
    [JsonSerializable(typeof(UserPreferences))]
    [JsonSerializable(typeof(SolveInteractionLog))]
    [JsonSerializable(typeof(DebugClientInfo))]
    [JsonSerializable(typeof(DebugSubmitResult))]
    public partial class LoopJsonContext : JsonSerializerContext
    {
    }
}
