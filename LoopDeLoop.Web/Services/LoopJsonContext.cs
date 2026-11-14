using System.Text.Json.Serialization;

namespace LoopDeLoop.Web.Services
{
    [JsonSerializable(typeof(SavedGameState))]
    internal partial class LoopJsonContext : JsonSerializerContext
    {
    }
}
