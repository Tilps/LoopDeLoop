using System;
using System.Linq;
using System.Text.Json;
using LoopDeLoop;
using LoopDeLoop.Web.Services;

namespace LoopDeLoop.Tests;

[TestClass]
public sealed class DebugInteractionLogTests
{
    [TestMethod]
    public void IsDebugQueryEnabled_ParsesQueryStringsAccurately()
    {
        Assert.IsFalse(GameService.IsDebugQueryEnabled(null));
        Assert.IsFalse(GameService.IsDebugQueryEnabled(""));
        Assert.IsFalse(GameService.IsDebugQueryEnabled("?"));
        Assert.IsFalse(GameService.IsDebugQueryEnabled("?foo=1"));
        Assert.IsFalse(GameService.IsDebugQueryEnabled("?debug=0"));
        Assert.IsFalse(GameService.IsDebugQueryEnabled("?debug=false"));
        Assert.IsFalse(GameService.IsDebugQueryEnabled("?debug=off"));

        Assert.IsTrue(GameService.IsDebugQueryEnabled("?debug"));
        Assert.IsTrue(GameService.IsDebugQueryEnabled("?debug=1"));
        Assert.IsTrue(GameService.IsDebugQueryEnabled("?debug=true"));
        Assert.IsTrue(GameService.IsDebugQueryEnabled("?foo=bar&DEBUG=yes"));
        Assert.IsTrue(GameService.IsDebugQueryEnabled("?a=1&debug&b=2"));

        // Combined with #p= hash fragments (both ?debug#p=... and #p=...&debug / #p=...?debug)
        Assert.IsTrue(GameService.IsDebugQueryEnabled("?debug", "#p=sq/3x3/a1b2"));
        Assert.IsTrue(GameService.IsDebugQueryEnabled("", "#p=sq/3x3/a1b2&debug"));
        Assert.IsTrue(GameService.IsDebugQueryEnabled("", "#p=sq/3x3/a1b2?debug"));
        Assert.IsTrue(GameService.IsDebugQueryEnabled("", "#debug&p=sq/3x3/a1b2"));
        Assert.IsFalse(GameService.IsDebugQueryEnabled("", "#p=sq/3x3/a1b2&debug=0"));

        Assert.IsTrue(GameService.TryExtractPuzzleHashPayload("#p=sq/3x3/a1b2&debug", out _, out string p1));
        Assert.AreEqual("sq/3x3/a1b2", p1);
        Assert.IsTrue(GameService.TryExtractPuzzleHashPayload("#p=sq/3x3/a1b2?debug", out _, out string p2));
        Assert.AreEqual("sq/3x3/a1b2", p2);
        Assert.IsTrue(GameService.TryExtractPuzzleHashPayload("#debug&p=sq/3x3/a1b2", out _, out string p3));
        Assert.AreEqual("sq/3x3/a1b2", p3);
    }

    [TestMethod]
    public void DebugRecording_DisabledByDefault_RecordsNothing()
    {
        using var game = new GameService();
        Assert.IsFalse(game.DebugRecordingEnabled);

        Assert.IsTrue(game.LoadPuzzleText("sq/3x3/a1b2"));
        game.ToggleEdge(0, isAlternative: false);
        game.Undo();

        Assert.AreEqual(0, game.DebugEventCount);
        Assert.IsNull(game.ExportDebugLog());
        Assert.AreEqual(string.Empty, game.ExportDebugLogJson());
    }

    [TestMethod]
    public void DebugRecording_CapturesFullSolveHistoryWithTimingsAndRoundTripsJson()
    {
        using var game = new GameService();
        game.SetDebugRecordingEnabled(true);

        // Simple 2x2 puzzle with all 4 outer edges forming a valid loop around a single 2x2 perimeter?
        // Use a real generated 4x4 puzzle and solve it completely.
        Mesh puzzle = PuzzleHelper.MakeMesh(4, 4, MeshType.Square, 1);
        puzzle.Generate(new Random(42));
        puzzle.Clear();
        string code = PuzzleCodec.Encode(puzzle, 4, 4);

        Assert.IsTrue(game.LoadPuzzleText(code, "hash"));
        game.ToggleEdge(0, isAlternative: false);
        game.SetEdgeDirect(1, EdgeState.Excluded);
        game.ToggleCellColor(0, isAlternative: false);
        game.Undo();
        game.Redo();
        game.Fix();
        game.Unfix();
        game.ToggleHintHeatMap();
        game.RequestHint(applyMove: false);
        Assert.IsTrue(game.SolvePuzzle());

        string json = game.ExportDebugLogJson();
        Assert.IsFalse(string.IsNullOrWhiteSpace(json));

        SolveInteractionLog? restored = JsonSerializer.Deserialize(json, LoopJsonContext.Default.SolveInteractionLog);
        Assert.IsNotNull(restored);
        Assert.AreEqual(1, restored.Version);
        Assert.AreEqual(code, restored.PuzzleCode);
        Assert.AreEqual("Square", restored.MeshType);
        Assert.AreEqual(4, restored.Width);
        Assert.AreEqual(4, restored.Height);
        Assert.IsTrue(restored.IsSolved);
        Assert.IsTrue(restored.WasAutoSolved);
        Assert.IsNotNull(restored.CompletedAtUtc);

        string[] kinds = restored.Events.Select(e => e.Kind).ToArray();
        CollectionAssert.Contains(kinds, "puzzle_start");
        CollectionAssert.Contains(kinds, "edge_toggle");
        CollectionAssert.Contains(kinds, "edge_set");
        CollectionAssert.Contains(kinds, "cell_color");
        CollectionAssert.Contains(kinds, "undo");
        CollectionAssert.Contains(kinds, "redo");
        CollectionAssert.Contains(kinds, "fix");
        CollectionAssert.Contains(kinds, "unfix");
        CollectionAssert.Contains(kinds, "heatmap_toggle");
        CollectionAssert.Contains(kinds, "hint");
        CollectionAssert.Contains(kinds, "solve");
        CollectionAssert.Contains(kinds, "puzzle_solved");

        // Verify sequence numbers and monotonic timings
        long prevElapsed = 0;
        for (int i = 0; i < restored.Events.Count; i++)
        {
            var ev = restored.Events[i];
            Assert.AreEqual(i, ev.Seq);
            Assert.IsTrue(ev.ElapsedMs >= prevElapsed, $"Event {i} elapsed time moved backwards.");
            Assert.IsTrue(ev.DeltaMs >= 0);
            Assert.AreEqual(ev.ElapsedMs - prevElapsed, ev.DeltaMs);
            prevElapsed = ev.ElapsedMs;
        }
    }

    [TestMethod]
    public void TryImportDebugLog_LoadsPuzzleAndRejectsInvalidInput()
    {
        using var sourceGame = new GameService();
        sourceGame.SetDebugRecordingEnabled(true);
        Assert.IsTrue(sourceGame.LoadPuzzleText("sq/3x3/a1b2", "hash"));
        sourceGame.ToggleEdge(0, isAlternative: false);
        string json = sourceGame.ExportDebugLogJson();

        using var targetGame = new GameService();
        targetGame.SetDebugRecordingEnabled(true);
        Assert.IsTrue(targetGame.TryImportDebugLog(json, out var importedLog, out string msg));
        Assert.IsNotNull(importedLog);
        Assert.IsTrue(msg.Contains("Loaded puzzle from log"));
        Assert.IsNotNull(targetGame.CurrentMesh);
        Assert.AreEqual(9, targetGame.CurrentMesh.Cells.Count);

        // Invalid JSON and missing puzzleCode fail gracefully
        Assert.IsFalse(targetGame.TryImportDebugLog("{not valid json", out _, out string badJsonMsg));
        Assert.IsTrue(badJsonMsg.Contains("Invalid debug log JSON"));

        Assert.IsFalse(targetGame.TryImportDebugLog("{}", out _, out string missingCodeMsg));
        Assert.IsTrue(missingCodeMsg.Contains("missing a valid puzzleCode"));
    }

    [TestMethod]
    public void DebugFeedback_RatingsNotesInstallIdAndClientInfo_RoundTripAndResetPerPuzzle()
    {
        using var game = new GameService();
        game.SetDebugRecordingEnabled(true);
        string installId = Guid.NewGuid().ToString("D");
        game.SetDebugInstallationId(installId);
        Assert.IsTrue(game.LoadPuzzleText("sq/3x3/a1b2", "hash"));

        // Clearable difficulty & quality star ratings
        game.ToggleDebugDifficultyRating(4);
        Assert.AreEqual(4, game.DebugDifficultyRating);
        game.ToggleDebugDifficultyRating(4); // clicking same star clears it
        Assert.IsNull(game.DebugDifficultyRating);
        game.SetDebugDifficultyRating(5);
        game.SetDebugQualityRating(3);
        Assert.AreEqual(5, game.DebugDifficultyRating);
        Assert.AreEqual(3, game.DebugQualityRating);

        // Out-of-range rating clears safely
        game.SetDebugQualityRating(99);
        Assert.IsNull(game.DebugQualityRating);
        game.ToggleDebugQualityRating(4);

        // Note length capped at 500 chars and null bytes stripped
        string oversizedNote = "abc\0" + new string('x', 600);
        game.SetDebugNote(oversizedNote);
        Assert.AreEqual(SolveInteractionLog.MaxNoteLength, game.DebugNote.Length);
        Assert.IsFalse(game.DebugNote.Contains('\0'));

        // Client screen & input modality info
        var client = new DebugClientInfo
        {
            ScreenWidth = 1920,
            ScreenHeight = 1080,
            ViewportWidth = 1280,
            ViewportHeight = 720,
            DevicePixelRatio = 2.0,
            SawTouch = true,
            SawMouse = false,
            SawPen = false,
            SawKeyboard = true
        };
        game.UpdateDebugClientInfo(client);

        string json = game.ExportDebugLogJson();
        SolveInteractionLog? restored = JsonSerializer.Deserialize(json, LoopJsonContext.Default.SolveInteractionLog);
        Assert.IsNotNull(restored);
        Assert.AreEqual(installId, restored.InstallationId);
        Assert.AreEqual(5, restored.DifficultyRating);
        Assert.AreEqual(4, restored.QualityRating);
        Assert.AreEqual(SolveInteractionLog.MaxNoteLength, restored.Note!.Length);
        Assert.IsNotNull(restored.Client);
        Assert.AreEqual(1920, restored.Client.ScreenWidth);
        Assert.AreEqual(1080, restored.Client.ScreenHeight);
        Assert.AreEqual(1280, restored.Client.ViewportWidth);
        Assert.AreEqual(720, restored.Client.ViewportHeight);
        Assert.AreEqual(2.0, restored.Client.DevicePixelRatio);
        Assert.IsTrue(restored.Client.SawTouch);
        Assert.IsFalse(restored.Client.SawMouse);
        Assert.IsTrue(restored.Client.SawKeyboard);

        // Starting a new puzzle clears ratings/notes but preserves installation ID
        int prevSession = game.DebugSessionId;
        Assert.IsTrue(game.LoadPuzzleText("sq/4x4/3a2b1", "hash"));
        Assert.IsTrue(game.DebugSessionId > prevSession);
        Assert.IsNull(game.DebugDifficultyRating);
        Assert.IsNull(game.DebugQualityRating);
        Assert.AreEqual(string.Empty, game.DebugNote);
        Assert.AreEqual(installId, game.DebugInstallationId);

        // Verify DebugSubmitResult source-gen deserialization
        DebugSubmitResult? res = JsonSerializer.Deserialize(
            """{"Ok":true,"Status":201,"Message":"Saved.","Id":"20261124_000000_abcdef123456"}""",
            LoopJsonContext.Default.DebugSubmitResult);
        Assert.IsNotNull(res);
        Assert.IsTrue(res.Ok);
        Assert.AreEqual(201, res.Status);
        Assert.AreEqual("20261124_000000_abcdef123456", res.Id);
    }

    [TestMethod]
    public void ResetCurrentPuzzle_RecordsRestartInSameSession_WhileChangingPuzzleStartsFresh()
    {
        using var game = new GameService();
        game.SetDebugRecordingEnabled(true);
        Assert.IsTrue(game.LoadPuzzleText("sq/3x3/a1b2", "hash"));
        int session1 = game.DebugSessionId;

        game.ToggleEdge(0, isAlternative: false);
        game.ResetCurrentPuzzle();
        game.ToggleEdge(1, isAlternative: false);
        game.ResetCurrentPuzzle();
        game.ToggleEdge(2, isAlternative: false);

        SolveInteractionLog? log1 = game.ExportDebugLog();
        Assert.IsNotNull(log1);
        Assert.AreEqual(session1, game.DebugSessionId);
        Assert.AreEqual(2, log1.RestartCount);
        Assert.AreEqual(2, log1.Events.Count(e => e.Kind == "puzzle_restart"));
        Assert.AreEqual(1, log1.Events.Count(e => e.Kind == "puzzle_start"));
        Assert.AreEqual(3, log1.Events.Count(e => e.Kind == "edge_toggle"));

        // Verify monotonic Seq & ElapsedMs across restarts
        for (int i = 1; i < log1.Events.Count; i++)
        {
            Assert.IsTrue(log1.Events[i].Seq > log1.Events[i - 1].Seq);
            Assert.IsTrue(log1.Events[i].ElapsedMs >= log1.Events[i - 1].ElapsedMs);
        }

        // Changing to a different puzzle is NOT a restart
        Assert.IsTrue(game.LoadPuzzleText("sq/4x4/3a2b1", "hash"));
        SolveInteractionLog? log2 = game.ExportDebugLog();
        Assert.IsNotNull(log2);
        Assert.AreNotEqual(session1, game.DebugSessionId);
        Assert.AreEqual(0, log2.RestartCount);
        Assert.AreEqual(0, log2.Events.Count(e => e.Kind == "puzzle_restart"));
        Assert.AreEqual(1, log2.Events.Count);
        Assert.AreEqual("puzzle_start", log2.Events[0].Kind);
        Assert.AreEqual(0, log2.Events[0].Seq);
    }

    [TestMethod]
    public void DebugEventTruncation_DiscardsOldestNonRestartEventsAndPreservesRestarts()
    {
        using var game = new GameService { DebugMaxEvents = 6 };
        game.SetDebugRecordingEnabled(true);
        Assert.IsTrue(game.LoadPuzzleText("sq/3x3/a1b2", "hash")); // seq 0: puzzle_start

        game.ToggleEdge(0, isAlternative: false); // seq 1
        game.ToggleEdge(1, isAlternative: false); // seq 2
        game.ResetCurrentPuzzle();                // seq 3: puzzle_restart #1
        game.ToggleEdge(2, isAlternative: false); // seq 4
        game.ToggleEdge(3, isAlternative: false); // seq 5
        game.ResetCurrentPuzzle();                // seq 6: puzzle_restart #2 (triggers drop of seq 0)
        game.ToggleEdge(4, isAlternative: false); // seq 7 (drops seq 1)
        game.ToggleEdge(5, isAlternative: false); // seq 8 (drops seq 2)
        game.ToggleEdge(6, isAlternative: false); // seq 9 (skips seq 3 restart, drops seq 4!)
        game.ToggleEdge(7, isAlternative: false); // seq 10 (skips seq 3 restart, drops seq 5!)

        SolveInteractionLog? log = game.ExportDebugLog();
        Assert.IsNotNull(log);
        Assert.AreEqual(6, log.Events.Count);
        Assert.AreEqual(2, log.RestartCount);
        Assert.AreEqual(5, log.TruncatedEventCount);

        // Both restart markers must survive truncation from the front
        var restarts = log.Events.Where(e => e.Kind == "puzzle_restart").ToList();
        Assert.AreEqual(2, restarts.Count);
        Assert.AreEqual(3, restarts[0].Seq);
        Assert.AreEqual(6, restarts[1].Seq);

        // Remaining events should be the 2 preserved restarts + the 4 most recent edge toggles (seq 7..10)
        CollectionAssert.AreEqual(new[] { 3, 6, 7, 8, 9, 10 }, log.Events.Select(e => e.Seq).ToArray());
    }
}
