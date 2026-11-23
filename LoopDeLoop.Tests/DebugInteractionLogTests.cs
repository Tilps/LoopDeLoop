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
}
