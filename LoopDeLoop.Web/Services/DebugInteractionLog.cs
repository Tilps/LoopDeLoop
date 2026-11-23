using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LoopDeLoop.Web.Services
{
    public sealed class SolveInteractionLog
    {
        public int Version { get; set; } = 1;
        public string PuzzleCode { get; set; } = string.Empty;
        public string MeshType { get; set; } = "Square";
        public int Width { get; set; }
        public int Height { get; set; }
        public int Difficulty { get; set; }
        public int EdgeCount { get; set; }
        public int CellCount { get; set; }
        public DateTimeOffset StartedAtUtc { get; set; }
        public DateTimeOffset? CompletedAtUtc { get; set; }
        public long TotalElapsedMs { get; set; }
        public bool IsSolved { get; set; }
        public bool WasAutoSolved { get; set; }
        public DebugSettingsSnapshot Settings { get; set; } = new();
        public List<SolveInteractionEvent> Events { get; set; } = [];
    }

    public sealed class DebugSettingsSnapshot
    {
        public int AutoMoveLevel { get; set; }
        public bool DisallowFalseMove { get; set; }
        public bool ShowCellColors { get; set; }
        public bool ShowEdgeColors { get; set; }
        public bool ShowHintHeatMap { get; set; }
        public bool AutoStart { get; set; }
    }

    public sealed class SolveInteractionEvent
    {
        public int Seq { get; set; }
        public long ElapsedMs { get; set; }
        public long DeltaMs { get; set; }
        public string Kind { get; set; } = string.Empty;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Index { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? SecondaryIndex { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? FromState { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ToState { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? IsAlternative { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Succeeded { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Detail { get; set; }
    }
}
