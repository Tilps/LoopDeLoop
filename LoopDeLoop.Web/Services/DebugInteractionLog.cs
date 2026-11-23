using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LoopDeLoop.Web.Services
{
    public sealed class SolveInteractionLog
    {
        public const int MaxNoteLength = 500;
        public const int MaxEvents = 2500;

        public int Version { get; set; } = 1;
        public string InstallationId { get; set; } = string.Empty;
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
        public int RestartCount { get; set; }
        public int TruncatedEventCount { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? DifficultyRating { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? QualityRating { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Note { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DebugClientInfo? Client { get; set; }

        public DebugSettingsSnapshot Settings { get; set; } = new();
        public List<SolveInteractionEvent> Events { get; set; } = [];
    }

    public sealed class DebugClientInfo
    {
        public int ScreenWidth { get; set; }
        public int ScreenHeight { get; set; }
        public int ViewportWidth { get; set; }
        public int ViewportHeight { get; set; }
        public double DevicePixelRatio { get; set; }
        public bool SawTouch { get; set; }
        public bool SawMouse { get; set; }
        public bool SawPen { get; set; }
        public bool SawKeyboard { get; set; }
    }

    public sealed class DebugSubmitResult
    {
        public bool Ok { get; set; }
        public int Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Id { get; set; }
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
