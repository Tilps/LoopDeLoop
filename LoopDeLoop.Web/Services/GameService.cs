using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LoopDeLoop;
using LoopDeLoop.TrialOrdering;

namespace LoopDeLoop.Web.Services
{
    public class GameService : IDisposable
    {
        public Mesh? CurrentMesh { get; private set; }
        public UndoTree UndoTree { get; private set; } = new();
        public HashSet<int> MarkedEdges { get; private set; } = [];

        public MeshType CurrentType { get; set; } = MeshType.Square;
        public string SizeText { get; set; } = "10x10";
        public int Difficulty { get; set; } = 1; // Easy

        // Active puzzle geometry metadata (independent of pending form edits)
        public int ActiveWidth { get; private set; }
        public int ActiveHeight { get; private set; }
        public MeshType ActiveType => CurrentMesh?.MeshType ?? CurrentType;

        public bool DisallowFalseMove { get; set; } = false;
        public bool ShowCellColors { get; set; } = false;
        public bool ShowEdgeColors
        {
            get => field;
            set
            {
                field = value;
                if (!value) PendingGroupEdgeIndex = null;
            }
        } = false;

        public bool ShowHintHeatMap
        {
            get => field;
            set
            {
                if (field == value) return;
                field = value;
                RefreshHintHeatMap();
            }
        } = false;

        public bool UseNeuralTrialOrdering
        {
            get => field;
            set
            {
                field = value;
                PuzzleHelper.UseNeuralTrialOrdering = value;
            }
        } = true;

        private HintHeatMapScorer? heatMapScorer;
        public float[]? EdgeHeatScores { get; private set; }

        public bool DebugRecordingEnabled { get; private set; }
        private SolveInteractionLog? currentDebugLog;
        private long lastDebugEventElapsedMs;
        public int DebugEventCount => currentDebugLog?.Events.Count ?? 0;
        public int DebugSessionId { get; private set; }
        public string DebugInstallationId { get; private set; } = string.Empty;
        public int? DebugDifficultyRating { get; private set; }
        public int? DebugQualityRating { get; private set; }
        public string DebugNote { get; private set; } = string.Empty;
        public DebugClientInfo? DebugClientInfo { get; private set; }

        public void SetDebugInstallationId(string? installationId)
        {
            if (string.IsNullOrWhiteSpace(installationId))
                return;
            string trimmed = installationId.Trim();
            if (Guid.TryParse(trimmed, out Guid parsed))
                DebugInstallationId = parsed.ToString("D");
        }

        public void SetDebugDifficultyRating(int? rating)
        {
            DebugDifficultyRating = rating is >= 1 and <= 5 ? rating : null;
        }

        public void ToggleDebugDifficultyRating(int star)
        {
            if (star is < 1 or > 5) return;
            DebugDifficultyRating = DebugDifficultyRating == star ? null : star;
        }

        public void SetDebugQualityRating(int? rating)
        {
            DebugQualityRating = rating is >= 1 and <= 5 ? rating : null;
        }

        public void ToggleDebugQualityRating(int star)
        {
            if (star is < 1 or > 5) return;
            DebugQualityRating = DebugQualityRating == star ? null : star;
        }

        public void SetDebugNote(string? note)
        {
            if (string.IsNullOrEmpty(note))
            {
                DebugNote = string.Empty;
                return;
            }
            string cleaned = note.Replace("\0", string.Empty);
            DebugNote = cleaned.Length > SolveInteractionLog.MaxNoteLength
                ? cleaned[..SolveInteractionLog.MaxNoteLength]
                : cleaned;
        }

        public void UpdateDebugClientInfo(DebugClientInfo? info)
        {
            DebugClientInfo = info;
        }

        public static bool IsDebugQueryEnabled(string? queryString, string? hashFragment = null)
        {
            bool? queryFlag = TryReadDebugFlag(queryString);
            if (queryFlag.HasValue)
                return queryFlag.Value;

            if (!string.IsNullOrWhiteSpace(hashFragment))
            {
                string trimmed = hashFragment.Trim().TrimStart('#');
                // Support both #p=...&debug and #p=...?debug (or #debug&p=...)
                int qIdx = trimmed.IndexOf('?');
                if (qIdx >= 0)
                {
                    bool? afterQ = TryReadDebugFlag(trimmed[(qIdx + 1)..]);
                    if (afterQ.HasValue)
                        return afterQ.Value;
                }
                bool? hashFlag = TryReadDebugFlag(trimmed);
                if (hashFlag.HasValue)
                    return hashFlag.Value;
            }

            return false;
        }

        private static bool? TryReadDebugFlag(string? paramString)
        {
            if (string.IsNullOrWhiteSpace(paramString))
                return null;

            string trimmed = paramString.Trim();
            if (trimmed.StartsWith('?'))
                trimmed = trimmed[1..];
            if (string.IsNullOrEmpty(trimmed))
                return null;

            foreach (string part in trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                int eq = part.IndexOf('=');
                string key = (eq >= 0 ? part[..eq] : part).Trim();
                if (!key.Equals("debug", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (eq < 0)
                    return true;

                string val = part[(eq + 1)..].Trim();
                if (val.Equals("0", StringComparison.OrdinalIgnoreCase) ||
                    val.Equals("false", StringComparison.OrdinalIgnoreCase) ||
                    val.Equals("no", StringComparison.OrdinalIgnoreCase) ||
                    val.Equals("off", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                return val.Length == 0 ||
                       val.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                       val.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                       val.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
                       val.Equals("on", StringComparison.OrdinalIgnoreCase);
            }

            return null;
        }

        public void SetDebugRecordingEnabled(bool enabled)
        {
            if (DebugRecordingEnabled == enabled) return;
            DebugRecordingEnabled = enabled;
            if (!enabled)
            {
                currentDebugLog = null;
                lastDebugEventElapsedMs = 0;
            }
            else if (CurrentMesh != null && currentDebugLog == null)
            {
                BeginDebugSession("active");
            }
            NotifyVisualChanged();
        }

        private DebugSettingsSnapshot CaptureDebugSettings() => new()
        {
            AutoMoveLevel = AutoMoveLevel,
            DisallowFalseMove = DisallowFalseMove,
            ShowCellColors = ShowCellColors,
            ShowEdgeColors = ShowEdgeColors,
            ShowHintHeatMap = ShowHintHeatMap,
            AutoStart = AutoStart
        };

        private void BeginDebugSession(string source)
        {
            DebugDifficultyRating = null;
            DebugQualityRating = null;
            DebugNote = string.Empty;

            if (!DebugRecordingEnabled || CurrentMesh == null)
            {
                currentDebugLog = null;
                lastDebugEventElapsedMs = 0;
                return;
            }

            DebugSessionId++;
            lastDebugEventElapsedMs = 0;
            currentDebugLog = new SolveInteractionLog
            {
                Version = 1,
                InstallationId = DebugInstallationId,
                PuzzleCode = GetShareCode(),
                MeshType = ActiveType.ToString(),
                Width = ActiveWidth,
                Height = ActiveHeight,
                Difficulty = Difficulty,
                EdgeCount = CurrentMesh.Edges.Count,
                CellCount = CurrentMesh.Cells.Count,
                StartedAtUtc = DateTimeOffset.UtcNow,
                CompletedAtUtc = IsSolved ? DateTimeOffset.UtcNow : null,
                TotalElapsedMs = Math.Max(0L, (long)Math.Round(ElapsedTime.TotalMilliseconds)),
                IsSolved = IsSolved,
                WasAutoSolved = WasAutoSolved,
                Settings = CaptureDebugSettings()
            };

            RecordDebugEvent("puzzle_start", detail: source);
        }

        private void RecordDebugEvent(
            string kind,
            int? index = null,
            int? secondaryIndex = null,
            string? fromState = null,
            string? toState = null,
            bool? isAlternative = null,
            bool? succeeded = null,
            string? detail = null)
        {
            if (!DebugRecordingEnabled || currentDebugLog == null)
                return;

            long elapsedMs = Math.Max(0L, (long)Math.Round(ElapsedTime.TotalMilliseconds));
            long deltaMs = Math.Max(0L, elapsedMs - lastDebugEventElapsedMs);
            lastDebugEventElapsedMs = elapsedMs;

            currentDebugLog.Events.Add(new SolveInteractionEvent
            {
                Seq = currentDebugLog.Events.Count,
                ElapsedMs = elapsedMs,
                DeltaMs = deltaMs,
                Kind = kind,
                Index = index,
                SecondaryIndex = secondaryIndex,
                FromState = fromState,
                ToState = toState,
                IsAlternative = isAlternative,
                Succeeded = succeeded,
                Detail = detail
            });
            currentDebugLog.TotalElapsedMs = elapsedMs;
        }

        public SolveInteractionLog? ExportDebugLog()
        {
            if (!DebugRecordingEnabled || CurrentMesh == null)
                return null;

            if (currentDebugLog == null)
                BeginDebugSession("active");
            if (currentDebugLog == null)
                return null;

            currentDebugLog.InstallationId = DebugInstallationId;
            currentDebugLog.TotalElapsedMs = Math.Max(0L, (long)Math.Round(ElapsedTime.TotalMilliseconds));
            currentDebugLog.IsSolved = IsSolved;
            currentDebugLog.WasAutoSolved = WasAutoSolved;
            currentDebugLog.DifficultyRating = DebugDifficultyRating;
            currentDebugLog.QualityRating = DebugQualityRating;
            string trimmedNote = DebugNote.Trim();
            currentDebugLog.Note = string.IsNullOrEmpty(trimmedNote) ? null : trimmedNote;
            currentDebugLog.Client = DebugClientInfo;
            currentDebugLog.Settings = CaptureDebugSettings();
            if (IsSolved && currentDebugLog.CompletedAtUtc == null)
                currentDebugLog.CompletedAtUtc = DateTimeOffset.UtcNow;

            return currentDebugLog;
        }

        public string ExportDebugLogJson()
        {
            SolveInteractionLog? log = ExportDebugLog();
            if (log == null)
                return string.Empty;
            return JsonSerializer.Serialize(log, LoopJsonContext.Default.SolveInteractionLog);
        }

        public bool TryImportDebugLog(string rawJson, out SolveInteractionLog? log, out string statusMessage)
        {
            log = null;
            statusMessage = string.Empty;
            if (string.IsNullOrWhiteSpace(rawJson))
            {
                statusMessage = "Paste a debug interaction JSON log first.";
                return false;
            }

            try
            {
                log = JsonSerializer.Deserialize(rawJson.Trim(), LoopJsonContext.Default.SolveInteractionLog);
            }
            catch (JsonException ex)
            {
                statusMessage = $"Invalid debug log JSON: {ex.Message}";
                return false;
            }

            if (log == null || string.IsNullOrWhiteSpace(log.PuzzleCode))
            {
                statusMessage = "Debug log is missing a valid puzzleCode.";
                return false;
            }

            if (!LoadPuzzleText(log.PuzzleCode, "debug_import"))
            {
                statusMessage = "Could not load puzzle from debug log.";
                return false;
            }

            double secs = log.TotalElapsedMs / 1000.0;
            statusMessage = $"Loaded puzzle from log ({log.Events.Count} events, {secs:0.0}s).";
            return true;
        }

        // Phase 2 Settings (persisted via UserPreferences, hidden in Settings modal)
        public int AutoMoveLevel
        {
            get => field;
            set => field = Math.Clamp(value, 0, 4);
        } = 0; // 0 = Off, 1..3 = Depth 1..3, 4 = Full Cascade

        public bool AutoMoveConsiderMultipleLoops { get; set; } = true;
        public bool AutoMoveUseIC { get; set; } = false;
        public bool AutoStart { get; set; } = false;
        public MouseWheelMode WheelMode { get; set; } = MouseWheelMode.Zoom;
        public bool ShowClock { get; set; } = true;

        public double GenerateLengthFraction
        {
            get => field;
            set => field = double.IsNaN(value) ? 0.5 : Math.Clamp(value, 0.25, 0.75);
        } = 0.5;

        public double GenerateBoringFraction
        {
            get => field;
            set => field = double.IsNaN(value) ? 0.01 : Math.Clamp(value, 0.0, 0.20);
        } = 0.01;

        public GeneratorSolverMode SolverModeOverride { get; set; } = GeneratorSolverMode.Auto;

        public bool IsGenerating { get; private set; }
        public bool IsBusy { get; private set; }
        public string? BusyMessage { get; private set; }
        public int PrunedProgress { get; private set; }
        public int TotalCells { get; private set; }

        private TimeSpan? solvedElapsed;
        public DateTime? StartTime { get; private set; }
        public TimeSpan ElapsedTime => solvedElapsed ?? (StartTime.HasValue ? DateTime.UtcNow - StartTime.Value : TimeSpan.Zero);
        public bool IsSolved { get; private set; }
        public bool WasAutoSolved { get; private set; }

        public int? InvalidEdgeIndex { get; private set; }
        public int? PendingGroupEdgeIndex { get; private set; }
        public PuzzleAnalysisResult? CurrentAnalysis { get; private set; }
        public int? HintEdgeIndex { get; private set; }
        public EdgeState HintTargetState { get; private set; } = EdgeState.Filled;
        public bool HintIsApplied { get; private set; }
        public string? HintMessage { get; private set; }

        public event Action? OnStateChanged;
        public event Action? OnBoardPersistedStateChanged;

        public bool CanUndo => UndoTree.CanUndo;
        public bool CanRedo => UndoTree.CanRedo;

        public void RefreshHintHeatMap()
        {
            if (!ShowHintHeatMap || CurrentMesh == null || IsSolved || IsGenerating || IsVisualSolving)
            {
                EdgeHeatScores = null;
                return;
            }

            heatMapScorer ??= new HintHeatMapScorer();
            EdgeHeatScores = heatMapScorer.ComputeEdgeHeat(CurrentMesh);
        }

        public void ToggleHintHeatMap()
        {
            ShowHintHeatMap = !ShowHintHeatMap;
            RecordDebugEvent("heatmap_toggle", detail: ShowHintHeatMap ? "on" : "off");
            NotifyVisualChanged();
        }

        public void NotifyChanged()
        {
            RefreshHintHeatMap();
            OnStateChanged?.Invoke();
            OnBoardPersistedStateChanged?.Invoke();
        }

        public void NotifyVisualChanged()
        {
            OnStateChanged?.Invoke();
        }

        private Mesh? generatingMesh;
        private CancellationTokenSource? generationCts;
        public DateTime? GenerationStartTime { get; private set; }

        public double GenerationElapsedSeconds => GenerationStartTime.HasValue
            ? (DateTime.UtcNow - GenerationStartTime.Value).TotalSeconds
            : 0;

        private static CancellationToken ResetTokenSource(ref CancellationTokenSource? cts)
        {
            var previous = cts;
            cts = new CancellationTokenSource();
            if (previous != null)
            {
                previous.Cancel();
                previous.Dispose();
            }
            return cts.Token;
        }

        private static void CancelAndDispose(ref CancellationTokenSource? cts)
        {
            var previous = cts;
            cts = null;
            if (previous != null)
            {
                previous.Cancel();
                previous.Dispose();
            }
        }

        private void ClearTransientBoardState()
        {
            preVictoryMarkedEdges = null;
            preVictoryUndoMark = null;
            preVictoryUndoTree = null;
            InvalidEdgeIndex = null;
            PendingGroupEdgeIndex = null;
            CurrentAnalysis = null;
            DismissVisualSolveInternal();
            ClearHintInternal();
        }

        public void CancelGeneration()
        {
            if (generatingMesh != null)
            {
                generatingMesh.AbortPrune = true;
            }
            generationCts?.Cancel();
            IsGenerating = false;
            GenerationStartTime = null;
            RefreshHintHeatMap();
            NotifyVisualChanged();
        }

        public async Task GenerateNewPuzzleAsync()
        {
            if (IsGenerating || IsBusy) return;

            var myCts = new CancellationTokenSource();
            var previous = generationCts;
            generationCts = myCts;
            if (previous != null)
            {
                previous.Cancel();
            }
            var token = myCts.Token;

            DismissVisualSolveInternal();
            IsGenerating = true;
            EdgeHeatScores = null;
            GenerationStartTime = DateTime.UtcNow;
            PrunedProgress = 0;
            TotalCells = 0;
            NotifyVisualChanged();

            bool generatedNewBoard = false;
            try
            {
                // Allow UI to render the loading overlay before computation begins
                await Task.Delay(20, token);

                MeshType requestedType = CurrentType;
                int requestedDifficulty = Difficulty;
                if (!PuzzleHelper.ParseSize(SizeText, requestedType, out int width, out int height))
                {
                    PuzzleHelper.GetDefaultSize(requestedType, out width, out height);
                    SizeText = $"{width}x{height}";
                }

                Mesh mesh = PuzzleHelper.MakeMesh(width, height, requestedType, requestedDifficulty);
                ApplyGeneratorOverrides(mesh);
                generatingMesh = mesh;
                TotalCells = mesh.Cells.Count;
                NotifyVisualChanged();

                var progress = new Progress<int>(p =>
                {
                    if (ReferenceEquals(generationCts, myCts))
                    {
                        PrunedProgress = p;
                        NotifyVisualChanged();
                    }
                });

                await mesh.GenerateAsync(progress, token);

                if (!token.IsCancellationRequested && !mesh.AbortPrune && ReferenceEquals(generationCts, myCts))
                {
                    CurrentMesh = mesh;
                    ActiveWidth = width;
                    ActiveHeight = height;
                    MarkedEdges.Clear();
                    UndoTree = new UndoTree();
                    ApplyAutoStartIfEnabled(mesh);
                    StartTime = DateTime.UtcNow;
                    solvedElapsed = null;
                    IsSolved = false;
                    WasAutoSolved = false;
                    ClearTransientBoardState();
                    BeginDebugSession("generated");
                    CheckSolution();
                    generatedNewBoard = true;
                }
            }
            catch (OperationCanceledException)
            {
                // Gracefully cancelled by user
            }
            finally
            {
                if (ReferenceEquals(generationCts, myCts))
                {
                    generatingMesh = null;
                    generationCts = null;
                    GenerationStartTime = null;
                    IsGenerating = false;
                }
                myCts.Dispose();
                if (generatedNewBoard)
                {
                    NotifyChanged();
                }
                else
                {
                    RefreshHintHeatMap();
                    NotifyVisualChanged();
                }
            }
        }

        private int GetEffectiveAutoMoveDepth(Mesh mesh)
        {
            return AutoMoveLevel switch
            {
                1 => 1,
                2 => 2,
                3 => 3,
                >= 4 => mesh.Edges.Count + 1,
                _ => 0
            };
        }

        public void ConfigureMeshForInteractiveMove(Mesh mesh)
        {
            mesh.ConsiderMultipleLoops = AutoMoveConsiderMultipleLoops;
            mesh.ConsiderIntersectCellInteractsAsSimple = AutoMoveUseIC;
            mesh.UseColoring = false;
            mesh.UseCellPairs = false;
            mesh.UseCellPairsTopLevel = false;
            mesh.UseEdgeRestricts = false;
            mesh.UseCellColoring = false;
            mesh.UseDerivedColoring = false;
            mesh.UseMerging = false;
            mesh.ColoringCheats = false;
        }

        public void ApplyGeneratorOverrides(Mesh mesh)
        {
            mesh.GenerateLengthFraction = GenerateLengthFraction;
            mesh.GenerateBoringFraction = GenerateBoringFraction;
            if (SolverModeOverride == GeneratorSolverMode.Iterative)
                mesh.SolverMethod = SolverMethod.Iterative;
            else if (SolverModeOverride == GeneratorSolverMode.Recursive)
                mesh.SolverMethod = SolverMethod.Recursive;
        }

        public void ApplyAutoStartIfEnabled(Mesh mesh)
        {
            if (!AutoStart) return;
            ConfigureMeshForInteractiveMove(mesh);
            mesh.PerformStart([]);
        }

        public UserPreferences ExportPreferences() => new()
        {
            DisallowFalseMove = DisallowFalseMove,
            ShowCellColors = ShowCellColors,
            ShowEdgeColors = ShowEdgeColors,
            ShowHintHeatMap = ShowHintHeatMap,
            UseNeuralTrialOrdering = UseNeuralTrialOrdering,
            AutoMoveLevel = AutoMoveLevel,
            AutoMoveConsiderMultipleLoops = AutoMoveConsiderMultipleLoops,
            AutoMoveUseIC = AutoMoveUseIC,
            AutoStart = AutoStart,
            WheelMode = WheelMode,
            ShowClock = ShowClock,
            GenerateLengthFraction = GenerateLengthFraction,
            GenerateBoringFraction = GenerateBoringFraction,
            SolverModeOverride = SolverModeOverride
        };

        public void ApplyPreferences(UserPreferences prefs)
        {
            DisallowFalseMove = prefs.DisallowFalseMove;
            ShowCellColors = prefs.ShowCellColors;
            ShowEdgeColors = prefs.ShowEdgeColors;
            ShowHintHeatMap = prefs.ShowHintHeatMap;
            UseNeuralTrialOrdering = prefs.UseNeuralTrialOrdering;
            AutoMoveLevel = prefs.AutoMoveLevel;
            AutoMoveConsiderMultipleLoops = prefs.AutoMoveConsiderMultipleLoops;
            AutoMoveUseIC = prefs.AutoMoveUseIC;
            AutoStart = prefs.AutoStart;
            WheelMode = Enum.IsDefined(prefs.WheelMode) ? prefs.WheelMode : MouseWheelMode.Zoom;
            ShowClock = prefs.ShowClock;
            GenerateLengthFraction = prefs.GenerateLengthFraction;
            GenerateBoringFraction = prefs.GenerateBoringFraction;
            SolverModeOverride = Enum.IsDefined(prefs.SolverModeOverride) ? prefs.SolverModeOverride : GeneratorSolverMode.Auto;
            NotifyChanged();
        }

        public void ResetPreferencesToDefaults()
        {
            ApplyPreferences(new UserPreferences());
        }

        public bool HandleEdgeGroupingClick(int edgeIndex, bool sameLoop)
        {
            if (CurrentMesh == null || IsGenerating || edgeIndex < 0 || edgeIndex >= CurrentMesh.Edges.Count)
                return false;

            bool dismissedVisual = DismissVisualSolveInternal();
            if (!ShowEdgeColors || MarkedEdges.Contains(edgeIndex))
            {
                if (dismissedVisual) NotifyChanged();
                return false;
            }

            if (PendingGroupEdgeIndex is not int firstEdge)
            {
                PendingGroupEdgeIndex = edgeIndex;
                NotifyVisualChanged();
                return true;
            }

            PendingGroupEdgeIndex = null;
            if (firstEdge == edgeIndex)
            {
                NotifyVisualChanged();
                return true;
            }

            var action = new ColorJoinAction(CurrentMesh, firstEdge, edgeIndex, sameLoop);
            if (UndoTree.Do(action))
            {
                InvalidEdgeIndex = null;
                RecordDebugEvent("edge_group", index: firstEdge, secondaryIndex: edgeIndex, succeeded: true, detail: sameLoop ? "same" : "opposite");
                NotifyChanged();
                return true;
            }

            RecordDebugEvent("edge_group", index: firstEdge, secondaryIndex: edgeIndex, succeeded: false, detail: sameLoop ? "same" : "opposite");
            FlashInvalidEdge(edgeIndex);
            return false;
        }

        public void ClearPendingEdgeGroup()
        {
            if (PendingGroupEdgeIndex.HasValue)
            {
                PendingGroupEdgeIndex = null;
                NotifyVisualChanged();
            }
        }

        public bool CanExportSquareFormats => PuzzleFormatHelper.CanExportSquareFormats(CurrentMesh);

        public string ExportLoopText() => CurrentMesh != null ? PuzzleFormatHelper.ExportLoopText(CurrentMesh) : string.Empty;

        public string? ExportLoopyDescriptor() => CurrentMesh != null ? PuzzleFormatHelper.ExportLoopyDescriptor(CurrentMesh) : null;

        public string? ExportLoopySave() => CurrentMesh != null ? PuzzleFormatHelper.ExportLoopySave(CurrentMesh) : null;

        public string? ExportSquareAscii() => CurrentMesh != null ? PuzzleFormatHelper.ExportSquareAscii(CurrentMesh) : null;

        public string GetShareCode()
        {
            if (CurrentMesh == null) return string.Empty;
            if (ActiveWidth > 0 && ActiveHeight > 0)
            {
                return PuzzleCodec.Encode(CurrentMesh, ActiveWidth, ActiveHeight);
            }
            if (PuzzleFormatHelper.TryGetSquareDimensions(CurrentMesh, out int sw, out int sh))
            {
                return PuzzleCodec.Encode(CurrentMesh, sw, sh);
            }
            return PuzzleCompression.Compress(CurrentMesh.SaveToString());
        }

        public bool ImportPuzzle(string rawInput, out string detectedFormat)
        {
            detectedFormat = string.Empty;
            if (IsGenerating || IsBusy || !PuzzleFormatHelper.TryParsePuzzle(rawInput, out Mesh? mesh, out detectedFormat, out int w, out int h) || mesh == null)
                return false;

            CurrentMesh = mesh;
            CurrentType = mesh.MeshType;
            ActiveWidth = w;
            ActiveHeight = h;
            if (w > 0 && h > 0)
            {
                SizeText = $"{w}x{h}";
            }
            else
            {
                PuzzleHelper.GetDefaultSize(mesh.MeshType, out int dw, out int dh);
                SizeText = $"{dw}x{dh}";
            }
            TotalCells = mesh.Cells.Count;
            PrunedProgress = 0;
            IsGenerating = false;
            UndoTree = new UndoTree();
            MarkedEdges.Clear();
            StartTime = DateTime.UtcNow;
            solvedElapsed = null;
            IsSolved = false;
            WasAutoSolved = false;
            ClearTransientBoardState();
            BeginDebugSession($"imported:{detectedFormat}");

            CheckSolution();
            NotifyChanged();
            return true;
        }

        public PuzzleAnalysisResult? AnalyzeCurrentPuzzle()
        {
            if (CurrentMesh == null || IsGenerating) return null;

            CurrentAnalysis = PuzzleHelper.AnalyzePuzzle(CurrentMesh);
            NotifyVisualChanged();
            return CurrentAnalysis;
        }

        public async Task<PuzzleAnalysisResult?> AnalyzeCurrentPuzzleAsync()
        {
            if (CurrentMesh == null || IsGenerating || IsBusy) return null;
            IsBusy = true;
            BusyMessage = "Analyzing puzzle...";
            NotifyVisualChanged();
            try
            {
                await Task.Delay(20);
                return AnalyzeCurrentPuzzle();
            }
            finally
            {
                IsBusy = false;
                BusyMessage = null;
                NotifyVisualChanged();
            }
        }

        public void ToggleEdge(int edgeIndex, bool isAlternative)
        {
            if (CurrentMesh == null || edgeIndex < 0 || edgeIndex >= CurrentMesh.Edges.Count)
                return;

            PendingGroupEdgeIndex = null;
            bool dismissedVisual = DismissVisualSolveInternal();

            if (MarkedEdges.Contains(edgeIndex))
            {
                if (dismissedVisual) NotifyChanged();
                return;
            }

            var current = CurrentMesh.Edges[edgeIndex].State;
            var next = !isAlternative
                ? (current == EdgeState.Empty ? EdgeState.Filled : (current == EdgeState.Filled ? EdgeState.Excluded : EdgeState.Empty))
                : (current == EdgeState.Empty ? EdgeState.Excluded : (current == EdgeState.Excluded ? EdgeState.Filled : EdgeState.Empty));

            int autoMove = GetEffectiveAutoMoveDepth(CurrentMesh);
            if (autoMove > 0)
                ConfigureMeshForInteractiveMove(CurrentMesh);

            var action = new PuzzleSetEdgeStateAction(CurrentMesh, edgeIndex, next, DisallowFalseMove, autoMove);
            if (UndoTree.Do(action))
            {
                InvalidEdgeIndex = null;
                HintEdgeIndex = null;
                RecordDebugEvent(
                    "edge_toggle",
                    index: edgeIndex,
                    fromState: current.ToString(),
                    toState: next.ToString(),
                    isAlternative: isAlternative,
                    succeeded: true,
                    detail: autoMove > 0 ? $"automove:{autoMove}" : null);
                CheckSolution();
                NotifyChanged();
            }
            else
            {
                RecordDebugEvent(
                    "edge_toggle",
                    index: edgeIndex,
                    fromState: current.ToString(),
                    toState: next.ToString(),
                    isAlternative: isAlternative,
                    succeeded: false);
                if (dismissedVisual) NotifyChanged();
                if (DisallowFalseMove)
                {
                    FlashInvalidEdge(edgeIndex);
                }
            }
        }

        public void SetEdgeDirect(int edgeIndex, EdgeState targetState)
        {
            if (CurrentMesh == null || edgeIndex < 0 || edgeIndex >= CurrentMesh.Edges.Count)
                return;

            bool dismissedVisual = DismissVisualSolveInternal();

            if (MarkedEdges.Contains(edgeIndex) || CurrentMesh.Edges[edgeIndex].State == targetState)
            {
                if (dismissedVisual) NotifyChanged();
                return;
            }

            var current = CurrentMesh.Edges[edgeIndex].State;
            int autoMove = GetEffectiveAutoMoveDepth(CurrentMesh);
            if (autoMove > 0)
                ConfigureMeshForInteractiveMove(CurrentMesh);

            var action = new PuzzleSetEdgeStateAction(CurrentMesh, edgeIndex, targetState, DisallowFalseMove, autoMove);
            if (UndoTree.Do(action))
            {
                InvalidEdgeIndex = null;
                HintEdgeIndex = null;
                RecordDebugEvent(
                    "edge_set",
                    index: edgeIndex,
                    fromState: current.ToString(),
                    toState: targetState.ToString(),
                    succeeded: true,
                    detail: autoMove > 0 ? $"automove:{autoMove}" : null);
                CheckSolution();
                NotifyChanged();
            }
            else
            {
                RecordDebugEvent(
                    "edge_set",
                    index: edgeIndex,
                    fromState: current.ToString(),
                    toState: targetState.ToString(),
                    succeeded: false);
                if (dismissedVisual) NotifyChanged();
                if (DisallowFalseMove)
                {
                    FlashInvalidEdge(edgeIndex);
                }
            }
        }

        public void ToggleCellColor(int cellIndex, bool isAlternative)
        {
            if (CurrentMesh == null || cellIndex < 0 || cellIndex >= CurrentMesh.Cells.Count)
                return;

            bool dismissedVisual = DismissVisualSolveInternal();
            int prevColor = CurrentMesh.Cells[cellIndex].Color;
            var action = new PuzzleCellColorAction(CurrentMesh, cellIndex, isAlternative);
            if (UndoTree.Do(action))
            {
                int nextColor = CurrentMesh.Cells[cellIndex].Color;
                RecordDebugEvent(
                    "cell_color",
                    index: cellIndex,
                    fromState: prevColor.ToString(),
                    toState: nextColor.ToString(),
                    isAlternative: isAlternative,
                    succeeded: true);
                NotifyChanged();
            }
            else if (dismissedVisual)
            {
                NotifyChanged();
            }
        }

        public void Undo()
        {
            bool dismissedVisual = DismissVisualSolveInternal();
            if (UndoTree.CanUndo)
            {
                UndoTree.Undo();
                WasAutoSolved = false;
                RecordDebugEvent("undo");
                CheckSolution();
                NotifyChanged();
            }
            else if (dismissedVisual)
            {
                NotifyChanged();
            }
        }

        public bool UndoAndForget()
        {
            bool dismissedVisual = DismissVisualSolveInternal();
            if (UndoTree.CanUndo && UndoTree.UndoAndForget())
            {
                WasAutoSolved = false;
                RecordDebugEvent("undo_forget");
                CheckSolution();
                NotifyChanged();
                return true;
            }
            if (dismissedVisual) NotifyChanged();
            return false;
        }

        public void Redo()
        {
            bool dismissedVisual = DismissVisualSolveInternal();
            if (UndoTree.CanRedo)
            {
                UndoTree.Redo();
                WasAutoSolved = UndoTree.CurrentAction is PuzzleBatchSolveAction;
                RecordDebugEvent("redo");
                CheckSolution();
                NotifyChanged();
            }
            else if (dismissedVisual)
            {
                NotifyChanged();
            }
        }

        public void Fix() => Fix(notify: true);

        private void Fix(bool notify)
        {
            if (CurrentMesh == null) return;
            DismissVisualSolveInternal();
            MarkedEdges.Clear();
            for (int i = 0; i < CurrentMesh.Edges.Count; i++)
            {
                if (CurrentMesh.Edges[i].State != EdgeState.Empty)
                    MarkedEdges.Add(i);
            }
            UndoTree.Mark();
            if (notify)
            {
                RecordDebugEvent("fix", detail: $"marked:{MarkedEdges.Count}");
                NotifyChanged();
            }
        }

        public void Unfix()
        {
            DismissVisualSolveInternal();
            MarkedEdges.Clear();
            UndoTree.ClearMark();
            RecordDebugEvent("unfix");
            CheckSolution();
            NotifyChanged();
        }

        public void RevertToFix()
        {
            DismissVisualSolveInternal();
            UndoTree.RevertToMark();
            RecordDebugEvent("revert_to_fix");
            CheckSolution();
            NotifyChanged();
        }

        // Checkpoint state captured just before the victory auto-Fix, so it can be
        // restored if the puzzle becomes unsolved again (e.g. via Undo).
        private int[]? preVictoryMarkedEdges;
        private object? preVictoryUndoMark;
        private UndoTree? preVictoryUndoTree;

        public void CheckSolution()
        {
            if (CurrentMesh == null) return;
            bool currentlySolved = PuzzleHelper.CheckIsSolved(CurrentMesh);
            if (currentlySolved && !IsSolved)
            {
                solvedElapsed = StartTime.HasValue ? DateTime.UtcNow - StartTime.Value : TimeSpan.Zero;
                IsSolved = true;
                preVictoryMarkedEdges = MarkedEdges.ToArray();
                preVictoryUndoMark = UndoTree.ClearMark();
                UndoTree.SetMarkedDirect(preVictoryUndoMark);
                preVictoryUndoTree = UndoTree;
                Fix(notify: false); // Auto-fix solution upon victory
                if (currentDebugLog != null)
                {
                    currentDebugLog.CompletedAtUtc = DateTimeOffset.UtcNow;
                    currentDebugLog.IsSolved = true;
                    currentDebugLog.WasAutoSolved = WasAutoSolved;
                }
                RecordDebugEvent("puzzle_solved", detail: WasAutoSolved ? "auto" : "manual");
            }
            else if (!currentlySolved && IsSolved)
            {
                if (solvedElapsed.HasValue)
                {
                    StartTime = DateTime.UtcNow - solvedElapsed.Value;
                    solvedElapsed = null;
                }
                IsSolved = false;
                WasAutoSolved = false;
                if (preVictoryUndoTree != null && ReferenceEquals(preVictoryUndoTree, UndoTree) && preVictoryMarkedEdges != null)
                {
                    MarkedEdges.Clear();
                    foreach (int m in preVictoryMarkedEdges)
                        MarkedEdges.Add(m);
                    UndoTree.SetMarkedDirect(preVictoryUndoMark);
                }
                preVictoryMarkedEdges = null;
                preVictoryUndoMark = null;
                preVictoryUndoTree = null;
            }
        }

        public SavedGameState? ExportState()
        {
            if (CurrentMesh == null) return null;

            int[] edgeStates = new int[CurrentMesh.Edges.Count];
            for (int i = 0; i < CurrentMesh.Edges.Count; i++)
            {
                edgeStates[i] = (int)CurrentMesh.Edges[i].State;
            }

            int[] cellColors = new int[CurrentMesh.Cells.Count];
            for (int i = 0; i < CurrentMesh.Cells.Count; i++)
            {
                cellColors[i] = CurrentMesh.Cells[i].Color;
            }

            string puzzleEncoded = GetShareCode();
            string activeSize = ActiveWidth > 0 && ActiveHeight > 0
                ? $"{ActiveWidth}x{ActiveHeight}"
                : SizeText;

            return new SavedGameState
            {
                Puzzle = puzzleEncoded,
                EdgeStates = edgeStates,
                CellColors = cellColors,
                MarkedEdges = MarkedEdges.ToArray(),
                ElapsedSeconds = ElapsedTime.TotalSeconds,
                IsSolved = IsSolved,
                WasAutoSolved = WasAutoSolved,
                DisallowFalseMove = DisallowFalseMove,
                ShowCellColors = ShowCellColors,
                Type = ActiveType.ToString(),
                Size = activeSize,
                Difficulty = Difficulty
            };
        }

        private static readonly char[] LineSeparators = ['\r', '\n'];
        private const double MaxPersistedElapsedSeconds = 365.0 * 24.0 * 3600.0;

        public bool RestoreState(SavedGameState state)
        {
            if (state == null || string.IsNullOrEmpty(state.Puzzle)) return false;

            Mesh? mesh;
            int w = 0, h = 0;
            if (PuzzleCodec.TryDecode(state.Puzzle, out var decodedMesh, out w, out h, out var _))
            {
                mesh = decodedMesh;
            }
            else if (PuzzleCompression.TryDecompressFromUrlSafe(state.Puzzle, out string decompressed) && !string.IsNullOrWhiteSpace(decompressed))
            {
                mesh = new Mesh(0, 0, MeshType.Square);
                if (!mesh.LoadFromText(decompressed.AsSpan()))
                    return false;
                if (PuzzleFormatHelper.TryGetSquareDimensions(mesh, out int sw, out int sh))
                {
                    w = sw;
                    h = sh;
                }
            }
            else
            {
                var lines = state.Puzzle.Split(LineSeparators, StringSplitOptions.RemoveEmptyEntries);
                mesh = new Mesh(0, 0, MeshType.Square);
                if (!mesh.LoadFromText(lines)) return false;
                if (PuzzleFormatHelper.TryGetSquareDimensions(mesh, out int sw, out int sh))
                {
                    w = sw;
                    h = sh;
                }
            }

            if (mesh == null) return false;

            CurrentMesh = mesh;
            CurrentType = mesh.MeshType;
            ActiveWidth = w;
            ActiveHeight = h;
            if (w > 0 && h > 0)
            {
                SizeText = $"{w}x{h}";
            }
            else if (!string.IsNullOrEmpty(state.Size) && PuzzleHelper.ParseSize(state.Size, mesh.MeshType, out int pw, out int ph))
            {
                SizeText = $"{pw}x{ph}";
            }
            Difficulty = Math.Clamp(state.Difficulty, 0, 5);
            TotalCells = mesh.Cells.Count;
            PrunedProgress = 0;
            IsGenerating = false;
            UndoTree = new UndoTree();
            MarkedEdges.Clear();

            if (state.EdgeStates != null && state.EdgeStates.Length == mesh.Edges.Count)
            {
                mesh.Clear();
                for (int i = 0; i < state.EdgeStates.Length; i++)
                {
                    int raw = state.EdgeStates[i];
                    if (raw is >= 0 and <= byte.MaxValue && Enum.IsDefined((EdgeState)(byte)raw))
                    {
                        var st = (EdgeState)(byte)raw;
                        if (st != EdgeState.Empty)
                            new SetAction(mesh, i, st).Perform();
                    }
                }
            }

            if (state.CellColors != null && state.CellColors.Length == mesh.Cells.Count)
            {
                for (int i = 0; i < state.CellColors.Length; i++)
                {
                    int c = state.CellColors[i];
                    mesh.Cells[i].Color = c is -1 or 0 or 1 ? c : 0;
                }
            }

            if (state.MarkedEdges != null)
            {
                foreach (int m in state.MarkedEdges)
                {
                    if (m >= 0 && m < mesh.Edges.Count)
                        MarkedEdges.Add(m);
                }
            }

            double clampedElapsed = double.IsFinite(state.ElapsedSeconds)
                ? Math.Clamp(state.ElapsedSeconds, 0.0, MaxPersistedElapsedSeconds)
                : 0.0;

            StartTime = clampedElapsed > 0
                ? DateTime.UtcNow.AddSeconds(-clampedElapsed)
                : DateTime.UtcNow;

            IsSolved = state.IsSolved;
            solvedElapsed = state.IsSolved ? TimeSpan.FromSeconds(clampedElapsed) : null;
            WasAutoSolved = state.IsSolved && state.WasAutoSolved;
            ClearTransientBoardState();
            BeginDebugSession("restored");
            CheckSolution();
            RefreshHintHeatMap();
            NotifyVisualChanged();
            return true;
        }

        public static bool TryExtractPuzzleHashPayload(string? rawHash, out string normalizedHash, out string payload)
        {
            normalizedHash = string.Empty;
            payload = string.Empty;
            if (string.IsNullOrWhiteSpace(rawHash))
                return false;

            string trimmed = rawHash.Trim();
            if (trimmed.StartsWith('#'))
                trimmed = trimmed[1..].Trim();
            if (string.IsNullOrEmpty(trimmed))
                return false;

            // Strip any trailing ?query appended after the hash (e.g. #p=sq/3x3/a1b2?debug)
            int qIdx = trimmed.IndexOf('?');
            string hashPart = qIdx >= 0 ? trimmed[..qIdx].Trim() : trimmed;
            if (string.IsNullOrEmpty(hashPart))
                return false;

            string? code = null;
            string[] segments = hashPart.Split('&', StringSplitOptions.RemoveEmptyEntries);
            foreach (string seg in segments)
            {
                string s = seg.Trim();
                if (s.StartsWith("p=", StringComparison.OrdinalIgnoreCase))
                {
                    code = s[2..];
                    break;
                }
                if (s.StartsWith("puzzle=", StringComparison.OrdinalIgnoreCase))
                {
                    code = s[7..];
                    break;
                }
            }

            // Fallback: bare hash without p=/puzzle= prefix (ignore lone debug flag)
            if (code == null && segments.Length == 1 && !segments[0].Trim().StartsWith("debug", StringComparison.OrdinalIgnoreCase))
            {
                code = segments[0];
            }

            if (string.IsNullOrWhiteSpace(code))
                return false;

            code = code.Trim();
            if (code.Contains('%'))
            {
                try
                {
                    code = Uri.UnescapeDataString(code);
                }
                catch (UriFormatException)
                {
                    // Keep original code if percent-escape sequence is malformed
                }
            }

            normalizedHash = $"p={code}";
            payload = code;
            return true;
        }

        public bool LoadPuzzleText(string puzzleText, string source = "loaded")
        {
            if (string.IsNullOrEmpty(puzzleText)) return false;

            Mesh? mesh;
            int w = 0, h = 0;
            if (PuzzleCodec.TryDecode(puzzleText, out var decodedMesh, out w, out h, out var _))
            {
                mesh = decodedMesh;
            }
            else
            {
                var lines = puzzleText.Split(LineSeparators, StringSplitOptions.RemoveEmptyEntries);
                mesh = new Mesh(0, 0, MeshType.Square);
                if (!mesh.LoadFromText(lines)) return false;
                if (PuzzleFormatHelper.TryGetSquareDimensions(mesh, out int sw, out int sh))
                {
                    w = sw;
                    h = sh;
                }
            }

            if (mesh == null) return false;

            CurrentMesh = mesh;
            CurrentType = mesh.MeshType;
            ActiveWidth = w;
            ActiveHeight = h;
            if (w > 0 && h > 0)
                SizeText = $"{w}x{h}";
            TotalCells = mesh.Cells.Count;
            PrunedProgress = 0;
            IsGenerating = false;
            UndoTree = new UndoTree();
            MarkedEdges.Clear();
            StartTime = DateTime.UtcNow;
            solvedElapsed = null;
            IsSolved = false;
            WasAutoSolved = false;
            ClearTransientBoardState();
            BeginDebugSession(source);

            CheckSolution();
            NotifyChanged();
            return true;
        }

        private CancellationTokenSource? invalidFlashCts;
        private CancellationTokenSource? hintFlashCts;
        private CancellationTokenSource? hintMessageCts;

        private async Task ClearAfterDelayAsync(int delayMs, CancellationToken token, Action clearAction)
        {
            try
            {
                await Task.Delay(delayMs, token);
                if (!token.IsCancellationRequested)
                {
                    clearAction();
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when superseded or cleared early
            }
        }

        public void FlashInvalidEdge(int edgeIndex)
        {
            var token = ResetTokenSource(ref invalidFlashCts);

            InvalidEdgeIndex = edgeIndex;
            NotifyVisualChanged();

            _ = ClearAfterDelayAsync(600, token, () =>
            {
                if (InvalidEdgeIndex == edgeIndex)
                {
                    InvalidEdgeIndex = null;
                    NotifyVisualChanged();
                }
            });
        }

        private bool ClearHintInternal()
        {
            CancelAndDispose(ref hintFlashCts);
            CancelAndDispose(ref hintMessageCts);
            bool hadHint = HintEdgeIndex.HasValue || HintMessage != null;
            HintEdgeIndex = null;
            HintMessage = null;
            return hadHint;
        }

        public void ClearHint()
        {
            if (ClearHintInternal())
                NotifyVisualChanged();
        }

        public void ResetCurrentPuzzle()
        {
            if (CurrentMesh == null || IsGenerating || IsBusy) return;

            CurrentMesh.Clear();
            MarkedEdges.Clear();
            UndoTree = new UndoTree();
            ApplyAutoStartIfEnabled(CurrentMesh);
            StartTime = DateTime.UtcNow;
            solvedElapsed = null;
            IsSolved = false;
            WasAutoSolved = false;
            ClearTransientBoardState();
            BeginDebugSession("reset");
            CheckSolution();
            NotifyChanged();
        }

        private const int MaxVisualSolveFrames = 1500;
        private readonly List<VisualSolveStep> visualSolveSteps = [];
        private Mesh? visualSolveSolutionMesh;

        public IReadOnlyList<VisualSolveStep> VisualSolveSteps => visualSolveSteps;
        public bool IsVisualSolving => visualSolveSteps.Count > 0;
        public int VisualSolveStepIndex { get; private set; }
        public int VisualSolveTotalSteps => visualSolveSteps.Count;
        public VisualSolveStep? CurrentVisualSolveStep => IsVisualSolving && VisualSolveStepIndex >= 0 && VisualSolveStepIndex < visualSolveSteps.Count
            ? visualSolveSteps[VisualSolveStepIndex]
            : null;

        public EdgeState GetVisualSolveEdgeState(int edgeIndex)
        {
            if (CurrentVisualSolveStep is { } step && edgeIndex >= 0 && edgeIndex < step.EdgeStates.Length)
                return step.EdgeStates[edgeIndex];
            if (CurrentMesh != null && edgeIndex >= 0 && edgeIndex < CurrentMesh.Edges.Count)
                return CurrentMesh.Edges[edgeIndex].State;
            return EdgeState.Empty;
        }

        private static EdgeState[] CaptureEdgeStates(Mesh mesh)
        {
            var states = new EdgeState[mesh.Edges.Count];
            for (int i = 0; i < states.Length; i++)
            {
                states[i] = mesh.Edges[i].State;
            }
            return states;
        }

        private bool DismissVisualSolveInternal()
        {
            if (visualSolveSteps.Count == 0 && visualSolveSolutionMesh == null)
                return false;

            visualSolveSteps.Clear();
            visualSolveSolutionMesh = null;
            VisualSolveStepIndex = 0;
            return true;
        }

        public void DismissVisualSolve()
        {
            if (DismissVisualSolveInternal())
            {
                RecordDebugEvent("visual_solve_dismiss");
                RefreshHintHeatMap();
                NotifyVisualChanged();
            }
        }

        public bool StartVisualSolve()
        {
            if (CurrentMesh == null || IsGenerating) return false;

            DismissVisualSolveInternal();
            ClearHintInternal();

            Mesh solver = PuzzleHelper.CreateFullSolverMesh(CurrentMesh);
            solver.Clear();
            solver.SuperSlowMo = true;

            visualSolveSteps.Add(new VisualSolveStep(CaptureEdgeStates(solver), "Starting solver..."));

            void OnUpdate(object sender, MeshChangeUpdateEventArgs args)
            {
                if (visualSolveSteps.Count >= MaxVisualSolveFrames - 1)
                    return;

                EdgeState[] snapshot = CaptureEdgeStates(args.CurrentMesh);
                if (snapshot.AsSpan().SequenceEqual(visualSolveSteps[^1].EdgeStates))
                    return;

                string status;
                bool isBacktrack = false;
                bool isBranching = false;

                if (args.Starting)
                {
                    status = "Testing branch...";
                    isBranching = true;
                }
                else if (!args.SuccessfulAttempt && args.JustDone != null)
                {
                    status = "Contradiction found — backtracking";
                    isBacktrack = true;
                }
                else if (args.JustDone != null)
                {
                    status = "Branch consistent — evaluating opposite";
                    isBranching = true;
                }
                else if (!args.SuccessfulAttempt)
                {
                    status = "Propagating deduction wave...";
                }
                else
                {
                    status = "Applying deduced moves";
                }

                visualSolveSteps.Add(new VisualSolveStep(snapshot, status, isBacktrack, isBranching));
            }

            solver.MeshChangeUpdate += OnUpdate;
            SolveState res;
            try
            {
                res = solver.TrySolve();
            }
            finally
            {
                solver.MeshChangeUpdate -= OnUpdate;
            }

            if ((res == SolveState.Solved || res == SolveState.MultipleSolutions) && solver.SolutionFound != null)
            {
                EdgeState[] finalStates = CaptureEdgeStates(solver.SolutionFound);
                visualSolveSolutionMesh = solver.SolutionFound;

                if (!finalStates.AsSpan().SequenceEqual(visualSolveSteps[^1].EdgeStates))
                {
                    visualSolveSteps.Add(new VisualSolveStep(finalStates, "Solution complete!"));
                }
                else
                {
                    visualSolveSteps[^1] = visualSolveSteps[^1] with { Status = "Solution complete!", IsBacktrack = false, IsBranching = false };
                }

                VisualSolveStepIndex = 0;
                EdgeHeatScores = null;
                RecordDebugEvent("visual_solve_start", succeeded: true, detail: $"steps:{visualSolveSteps.Count}");
                NotifyVisualChanged();
                return true;
            }

            DismissVisualSolveInternal();
            RecordDebugEvent("visual_solve_start", succeeded: false);
            NotifyVisualChanged();
            return false;
        }

        public async Task<bool> StartVisualSolveAsync()
        {
            if (CurrentMesh == null || IsGenerating || IsBusy) return false;
            IsBusy = true;
            BusyMessage = "Recording solver steps...";
            NotifyVisualChanged();
            try
            {
                await Task.Delay(20);
                return StartVisualSolve();
            }
            finally
            {
                IsBusy = false;
                BusyMessage = null;
                NotifyVisualChanged();
            }
        }

        public void SetVisualSolveStep(int index)
        {
            if (!IsVisualSolving) return;
            int clamped = Math.Clamp(index, 0, visualSolveSteps.Count - 1);
            if (clamped == VisualSolveStepIndex) return;
            VisualSolveStepIndex = clamped;
            NotifyVisualChanged();
        }

        public bool StepVisualSolveForward()
        {
            if (!IsVisualSolving || VisualSolveStepIndex >= visualSolveSteps.Count - 1)
                return false;
            VisualSolveStepIndex++;
            NotifyVisualChanged();
            return true;
        }

        public bool StepVisualSolveBackward()
        {
            if (!IsVisualSolving || VisualSolveStepIndex <= 0)
                return false;
            VisualSolveStepIndex--;
            NotifyVisualChanged();
            return true;
        }

        public bool ApplyVisualSolveSolution()
        {
            if (!IsVisualSolving || CurrentMesh == null || visualSolveSolutionMesh == null)
                return false;

            var action = new PuzzleBatchSolveAction(CurrentMesh, visualSolveSolutionMesh);
            if (UndoTree.Do(action))
            {
                WasAutoSolved = true;
                DismissVisualSolveInternal();
                RecordDebugEvent("solve", detail: "visual_apply");
                CheckSolution();
                NotifyChanged();
                return true;
            }

            return false;
        }

        public bool SolvePuzzle()
        {
            if (CurrentMesh == null || IsGenerating) return false;

            DismissVisualSolveInternal();
            Mesh original = PuzzleHelper.CreateFullSolverMesh(CurrentMesh);
            original.Clear();

            SolveState res = original.TrySolve();
            if ((res == SolveState.Solved || res == SolveState.MultipleSolutions) && original.SolutionFound != null)
            {
                var action = new PuzzleBatchSolveAction(CurrentMesh, original.SolutionFound);
                if (UndoTree.Do(action))
                {
                    WasAutoSolved = true;
                    RecordDebugEvent("solve", detail: "auto_solve");
                    CheckSolution();
                    NotifyChanged();
                    return true;
                }
            }
            return false;
        }

        public async Task<bool> SolvePuzzleAsync()
        {
            if (CurrentMesh == null || IsGenerating || IsBusy) return false;
            IsBusy = true;
            BusyMessage = "Solving puzzle...";
            NotifyVisualChanged();
            try
            {
                await Task.Delay(20);
                return SolvePuzzle();
            }
            finally
            {
                IsBusy = false;
                BusyMessage = null;
                NotifyVisualChanged();
            }
        }

        public bool RequestHint(bool applyMove)
        {
            if (CurrentMesh == null || IsGenerating) return false;
            DismissVisualSolveInternal();

            var (edgeIdx, targetState, message) = ComputeHint();
            HintMessage = message;
            bool boardMutated = false;

            RecordDebugEvent(
                "hint",
                index: edgeIdx,
                toState: edgeIdx.HasValue ? targetState.ToString() : null,
                succeeded: edgeIdx.HasValue,
                detail: applyMove ? "apply" : "highlight");

            if (edgeIdx.HasValue)
            {
                HintEdgeIndex = edgeIdx.Value;
                HintTargetState = targetState;
                HintIsApplied = applyMove;
                if (applyMove)
                {
                    var action = new PuzzleSetEdgeStateAction(CurrentMesh, edgeIdx.Value, targetState, DisallowFalseMove);
                    if (UndoTree.Do(action))
                    {
                        boardMutated = true;
                        CheckSolution();
                    }
                }
                else
                {
                    // Don't give away whether it's a line or a cross.
                    HintMessage = "The highlighted edge can be determined from what's around it.";
                }

                var flashToken = ResetTokenSource(ref hintFlashCts);
                int delayMs = applyMove ? 1500 : 3500;
                int capturedEdge = edgeIdx.Value;
                _ = ClearAfterDelayAsync(delayMs, flashToken, () =>
                {
                    if (HintEdgeIndex == capturedEdge)
                    {
                        HintEdgeIndex = null;
                        NotifyVisualChanged();
                    }
                });
            }

            var msgToken = ResetTokenSource(ref hintMessageCts);
            _ = ClearAfterDelayAsync(5000, msgToken, () =>
            {
                HintMessage = null;
                NotifyVisualChanged();
            });

            if (boardMutated)
                NotifyChanged();
            else
                NotifyVisualChanged();
            return edgeIdx.HasValue;
        }

        public async Task<bool> RequestHintAsync(bool applyMove)
        {
            if (CurrentMesh == null || IsGenerating || IsBusy) return false;
            IsBusy = true;
            BusyMessage = "Computing hint...";
            NotifyVisualChanged();
            try
            {
                await Task.Delay(20);
                return RequestHint(applyMove);
            }
            finally
            {
                IsBusy = false;
                BusyMessage = null;
                NotifyVisualChanged();
            }
        }

        public (int? EdgeIndex, EdgeState TargetState, string Message) ComputeHint()
        {
            if (CurrentMesh == null)
                return (null, EdgeState.Empty, "No puzzle active.");

            if (IsSolved)
                return (null, EdgeState.Empty, "The puzzle is already solved!");

            // 1. Check for immediate user contradictions on the board
            for (int c = 0; c < CurrentMesh.Cells.Count; c++)
            {
                var cell = CurrentMesh.Cells[c];
                if (cell.TargetCount >= 0 && cell.FilledCount > cell.TargetCount)
                {
                    return (null, EdgeState.Empty, $"Contradiction: Cell clue {cell.TargetCount} has {cell.FilledCount} filled edges.");
                }
            }
            for (int v = 0; v < CurrentMesh.Intersections.Count; v++)
            {
                var inter = CurrentMesh.Intersections[v];
                if (inter.FilledCount > 2)
                {
                    return (null, EdgeState.Empty, "Contradiction: A vertex has more than 2 connected lines.");
                }
            }

            // 2. Clone mesh and run iterative deduction pass
            Mesh hintMesh = new Mesh(CurrentMesh);
            hintMesh.ConsiderIntersectCellInteractsAsSimple = true;
            hintMesh.ConsiderMultipleLoops = true;
            hintMesh.IterativeSolverDepth = 0;
            hintMesh.IterativeRecMaxDepth = 1;
            hintMesh.UseColoring = false;
            hintMesh.UseCellPairs = false;
            hintMesh.UseCellPairsTopLevel = false;
            hintMesh.UseEdgeRestricts = false;
            hintMesh.UseCellColoring = false;
            hintMesh.UseDerivedColoring = false;
            hintMesh.UseMerging = false;
            hintMesh.UseIntersectCellInteractsInSolver = true;

            List<IAction> changes = new List<IAction>();
            bool startOk = hintMesh.PerformStart(changes);
            if (!startOk)
            {
                return (null, EdgeState.Empty, "Contradiction detected in current board state.");
            }

            SetAction? action = changes.OfType<SetAction>().FirstOrDefault(a =>
                a.EdgeIndex >= 0 && a.EdgeIndex < CurrentMesh.Edges.Count &&
                CurrentMesh.Edges[a.EdgeIndex].State != a.EdgeState);

            while (action == null && hintMesh.IterativeSolverDepth <= 10)
            {
                changes.Clear();
                hintMesh.GetSomething(changes);
                action = changes.OfType<SetAction>().FirstOrDefault(a =>
                    a.EdgeIndex >= 0 && a.EdgeIndex < CurrentMesh.Edges.Count &&
                    CurrentMesh.Edges[a.EdgeIndex].State != a.EdgeState);
                if (action != null)
                    break;
                hintMesh.IterativeSolverDepth++;
            }

            if (action != null)
            {
                int edgeIdx = action.EdgeIndex;
                EdgeState state = action.EdgeState;
                string reason = ExplainDeduction(CurrentMesh, edgeIdx, state);
                return (edgeIdx, state, reason);
            }

            // 3. Fallback: full solver search on current state
            Mesh fullSolverMesh = PuzzleHelper.CreateFullSolverMesh(CurrentMesh);

            SolveState solveRes = fullSolverMesh.TrySolve();
            if ((solveRes == SolveState.Solved || solveRes == SolveState.MultipleSolutions) && fullSolverMesh.SolutionFound != null)
            {
                var sol = fullSolverMesh.SolutionFound;
                for (int i = 0; i < CurrentMesh.Edges.Count; i++)
                {
                    if (CurrentMesh.Edges[i].State == EdgeState.Empty && sol.Edges[i].State != EdgeState.Empty)
                    {
                        string stateStr = sol.Edges[i].State == EdgeState.Filled ? "line" : "cross (✕)";
                        return (i, sol.Edges[i].State, $"Advanced deduction: This edge must be a {stateStr}.");
                    }
                }
            }
            else if (solveRes == SolveState.NoSolutions)
            {
                return (null, EdgeState.Empty, "Contradiction: No valid solution exists from this board state.");
            }

            return (null, EdgeState.Empty, "No logical deduction found at this step.");
        }

        private static string ExplainDeduction(Mesh mesh, int edgeIndex, EdgeState targetState)
        {
            Edge edge = mesh.Edges[edgeIndex];

            // Check adjacent cells
            foreach (int cIdx in edge.Cells)
            {
                if (cIdx < 0 || cIdx >= mesh.Cells.Count) continue;
                var cell = mesh.Cells[cIdx];
                if (cell.TargetCount == 0 && targetState == EdgeState.Excluded)
                {
                    return "Cell clue 0: all surrounding edges must be excluded.";
                }
                if (cell.TargetCount >= 0)
                {
                    if (cell.FilledCount == cell.TargetCount && targetState == EdgeState.Excluded)
                    {
                        return $"Clue {cell.TargetCount} already satisfied: remaining edges must be excluded.";
                    }
                    if (cell.Edges.Count - cell.ExcludedCount == cell.TargetCount && targetState == EdgeState.Filled)
                    {
                        return $"Clue {cell.TargetCount} needs all remaining edges: must be filled.";
                    }
                }
            }

            // Check adjacent intersections
            if (edge.Intersections != null && edge.Intersections.Length >= 2)
            {
                foreach (int vIdx in edge.Intersections)
                {
                    if (vIdx < 0 || vIdx >= mesh.Intersections.Count) continue;
                    var inter = mesh.Intersections[vIdx];
                    if (inter.FilledCount == 2 && targetState == EdgeState.Excluded)
                    {
                        return "Vertex already has 2 connections: remaining edges must be excluded.";
                    }
                    if (inter.FilledCount == 1 && inter.Edges.Count - inter.ExcludedCount == 2 && targetState == EdgeState.Filled)
                    {
                        return "Loop entering vertex has only one exit path: must continue here.";
                    }
                    if (inter.FilledCount == 0 && inter.Edges.Count - inter.ExcludedCount < 2 && targetState == EdgeState.Excluded)
                    {
                        return "Dead end: loop cannot enter vertex without an exit path.";
                    }
                }
            }

            return targetState == EdgeState.Filled
                ? "Logical deduction: This edge must be part of the loop."
                : "Logical deduction: This edge cannot be part of the loop.";
        }

        public void Dispose()
        {
            generationCts?.Cancel();
            generationCts?.Dispose();
            invalidFlashCts?.Cancel();
            invalidFlashCts?.Dispose();
            hintFlashCts?.Cancel();
            hintFlashCts?.Dispose();
            hintMessageCts?.Cancel();
            hintMessageCts?.Dispose();
        }
    }

    public enum MouseWheelMode
    {
        Zoom = 0,
        UndoRedo = 1
    }

    public enum GeneratorSolverMode
    {
        Auto = 0,
        Iterative = 1,
        Recursive = 2
    }

    public sealed record VisualSolveStep(
        EdgeState[] EdgeStates,
        string Status,
        bool IsBacktrack = false,
        bool IsBranching = false);

    public class UserPreferences
    {
        public bool DisallowFalseMove { get; set; } = false;
        public bool ShowCellColors { get; set; } = false;
        public bool ShowEdgeColors { get; set; } = false;
        public bool ShowHintHeatMap { get; set; } = false;
        public bool UseNeuralTrialOrdering { get; set; } = true;
        public int AutoMoveLevel { get; set; } = 0;
        public bool AutoMoveConsiderMultipleLoops { get; set; } = true;
        public bool AutoMoveUseIC { get; set; } = false;
        public bool AutoStart { get; set; } = false;
        public MouseWheelMode WheelMode { get; set; } = MouseWheelMode.Zoom;
        public bool ShowClock { get; set; } = true;
        public double GenerateLengthFraction { get; set; } = 0.5;
        public double GenerateBoringFraction { get; set; } = 0.01;
        public GeneratorSolverMode SolverModeOverride { get; set; } = GeneratorSolverMode.Auto;
    }

    public class SavedGameState
    {
        public string Puzzle { get; set; } = string.Empty;
        public int[]? EdgeStates { get; set; }
        public int[]? CellColors { get; set; }
        public int[]? MarkedEdges { get; set; }
        public double ElapsedSeconds { get; set; }
        public bool IsSolved { get; set; }
        public bool WasAutoSolved { get; set; }
        public bool DisallowFalseMove { get; set; }
        public bool ShowCellColors { get; set; }
        public string Type { get; set; } = "Square";
        public string Size { get; set; } = "10x10";
        public int Difficulty { get; set; } = 1;
    }
}

