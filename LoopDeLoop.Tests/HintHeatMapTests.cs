using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using LoopDeLoop;
using LoopDeLoop.TrialOrdering;
using LoopDeLoop.Web.Services;

namespace LoopDeLoop.Tests;

[TestClass]
[DoNotParallelize]
public sealed class HintHeatMapTests
{
    [TestMethod]
    public void HintHeatMapScorer_ScoresOnlyEmptyEdgesInUnitRange()
    {
        Mesh mesh = PuzzleHelper.MakeMesh(8, 8, MeshType.Square, 3);
        mesh.Generate(new Random(42));
        mesh.Clear();

        var scorer = new HintHeatMapScorer();
        float[] scores = scorer.ComputeEdgeHeat(mesh);

        Assert.AreEqual(mesh.Edges.Count, scores.Length);
        Assert.IsTrue(scores.All(s => float.IsFinite(s) && s >= 0f && s <= 1f));
        Assert.IsTrue(scores.Any(s => s > 0.5f), "Expected at least one hot edge on a generated puzzle.");
        Assert.IsTrue(scores.Any(s => s == 0f), "Expected cold edges to stay at 0.");

        // Mark an edge filled and another excluded; both must drop to 0 on rescore.
        int filledIdx = 0;
        int excludedIdx = 1;
        new SetAction(mesh, filledIdx, EdgeState.Filled).Perform();
        new SetAction(mesh, excludedIdx, EdgeState.Excluded).Perform();

        float[] updated = scorer.ComputeEdgeHeat(mesh);
        Assert.AreEqual(0f, updated[filledIdx]);
        Assert.AreEqual(0f, updated[excludedIdx]);
    }

    [TestMethod]
    public void HintHeatMapScorer_IgnoresManualCellShadingAndPreservesMeshFlags()
    {
        Mesh mesh = PuzzleHelper.MakeMesh(7, 7, MeshType.Square, 3);
        mesh.Generate(new Random(99));
        mesh.Clear();

        // Capture initial solver flags set by MakeMesh(difficulty 3)
        int origDepth = mesh.IterativeSolverDepth;
        bool origColor = mesh.UseColoring;
        bool origCellColor = mesh.UseCellColoring;
        bool origRestricts = mesh.UseEdgeRestricts;
        bool origMerging = mesh.UseMerging;
        bool origPairsTop = mesh.UseCellPairsTopLevel;

        var scorer = new HintHeatMapScorer();
        float[] baseline = scorer.ComputeEdgeHeat(mesh).ToArray();

        // Verify flags were completely restored
        Assert.AreEqual(origDepth, mesh.IterativeSolverDepth);
        Assert.AreEqual(origColor, mesh.UseColoring);
        Assert.AreEqual(origCellColor, mesh.UseCellColoring);
        Assert.AreEqual(origRestricts, mesh.UseEdgeRestricts);
        Assert.AreEqual(origMerging, mesh.UseMerging);
        Assert.AreEqual(origPairsTop, mesh.UseCellPairsTopLevel);

        // Apply manual player cell shading without populating CellColorSets (like web UI / RestoreState)
        mesh.Cells[0].Color = 1;
        mesh.Cells[1].Color = -1;

        float[] withShading = scorer.ComputeEdgeHeat(mesh).ToArray();
        Assert.AreEqual(baseline.Length, withShading.Length);
        for (int i = 0; i < baseline.Length; i++)
        {
            Assert.AreEqual(baseline[i], withShading[i], 1e-5f, $"Manual cell shading should not perturb heat score for edge {i}.");
        }
    }

    [TestMethod]
    public void HintHeatMapScorer_HighlightsImmediateZeroClue()
    {
        Mesh mesh = new Mesh(5, 5, MeshType.Square);
        mesh.Cells[0].TargetCount = 0;
        mesh.Cells[12].TargetCount = 2;

        var scorer = new HintHeatMapScorer();
        float[] scores = scorer.ComputeEdgeHeat(mesh);

        foreach (int edgeIdx in mesh.Cells[0].Edges)
        {
            Assert.IsTrue(scores[edgeIdx] >= 0.90f, $"Edge {edgeIdx} adjacent to 0-clue should be hot (>= 0.90), got {scores[edgeIdx]}.");
        }
    }

    [TestMethod]
    public void HintHeatMapScorer_ProducesGnnRankingAfterPerformStart()
    {
        Mesh mesh = PuzzleHelper.MakeMesh(10, 10, MeshType.Square, 4);
        mesh.Generate(new Random(101));
        mesh.Clear();

        // Advance past trivial 0-step local deductions so pure GNN predictions drive the heat map
        mesh.PerformStart(new List<IAction>());
        if (mesh.CountEmptyEdges() == 0)
            Assert.Inconclusive("Generated puzzle solved in PerformStart.");

        var scorer = new HintHeatMapScorer();
        float[] scores = scorer.ComputeEdgeHeat(mesh);

        float maxHeat = 0f;
        int hotCount = 0;
        int emptyCount = 0;
        for (int e = 0; e < mesh.Edges.Count; e++)
        {
            if (mesh.Edges[e].State == EdgeState.Empty)
            {
                emptyCount++;
                if (scores[e] > maxHeat) maxHeat = scores[e];
                if (scores[e] >= 0.12f) hotCount++;
            }
            else
            {
                Assert.AreEqual(0f, scores[e]);
            }
        }

        Assert.IsTrue(maxHeat >= 0.70f, $"Expected a strong GNN hotspot on post-PerformStart state, got max={maxHeat}.");
        Assert.IsTrue(hotCount > 0 && hotCount < emptyCount, $"Expected selective highlighting (got {hotCount}/{emptyCount} hot edges).");
    }

    [TestMethod]
    [DataRow(MeshType.CairoPentagons, 5, 5, 3, 202)]
    [DataRow(MeshType.Hexagonal, 5, 8, 3, 303)]
    [DataRow(MeshType.Triangle, 6, 6, 3, 404)]
    public void HintHeatMapScorer_WorksAcrossNonSquareGrids(MeshType type, int w, int h, int diff, int seed)
    {
        Mesh mesh = PuzzleHelper.MakeMesh(w, h, type, diff);
        mesh.Generate(new Random(seed));
        mesh.Clear();

        var scorer = new HintHeatMapScorer();
        float[] scores = scorer.ComputeEdgeHeat(mesh);

        Assert.AreEqual(mesh.Edges.Count, scores.Length);
        Assert.IsTrue(scores.All(s => float.IsFinite(s) && s >= 0f && s <= 1f));
        Assert.IsTrue(scores.Any(s => s > 0.5f));
    }

    [TestMethod]
    public void GameService_HintHeatMapLifecycleAndPreferencesRoundTrip()
    {
        bool prevNeural = PuzzleHelper.UseNeuralTrialOrdering;
        try
        {
            using var game = new GameService();
            Mesh puzzle = PuzzleHelper.MakeMesh(6, 6, MeshType.Square, 2);
            puzzle.Generate(new Random(77));
            puzzle.Clear();
            string code = PuzzleCodec.Encode(puzzle, 6, 6);

            Assert.IsTrue(game.LoadPuzzleText(code));
            Assert.IsFalse(game.ShowHintHeatMap);
            Assert.IsNull(game.EdgeHeatScores);
            Assert.IsTrue(game.UseNeuralTrialOrdering);

            // Toggle ON
            game.ToggleHintHeatMap();
            Assert.IsTrue(game.ShowHintHeatMap);
            Assert.IsNotNull(game.EdgeHeatScores);
            Assert.AreEqual(game.CurrentMesh!.Edges.Count, game.EdgeHeatScores.Length);

            // Make a move -> that edge's heat becomes 0
            game.ToggleEdge(0, isAlternative: false);
            Assert.IsNotNull(game.EdgeHeatScores);
            Assert.AreEqual(0f, game.EdgeHeatScores[0]);

            // Start visual solve -> heat map hidden during playback; dismiss -> restored
            Assert.IsTrue(game.StartVisualSolve());
            Assert.IsNull(game.EdgeHeatScores);
            game.DismissVisualSolve();
            Assert.IsNotNull(game.EdgeHeatScores);

            // Solve -> heat map cleared
            Assert.IsTrue(game.SolvePuzzle());
            Assert.IsTrue(game.IsSolved);
            Assert.IsNull(game.EdgeHeatScores);

            // Preferences round-trip & legacy JSON defaults
            game.UseNeuralTrialOrdering = false;
            Assert.IsFalse(PuzzleHelper.UseNeuralTrialOrdering);
            string json = JsonSerializer.Serialize(game.ExportPreferences(), LoopJsonContext.Default.UserPreferences);
            UserPreferences? restored = JsonSerializer.Deserialize(json, LoopJsonContext.Default.UserPreferences);
            Assert.IsNotNull(restored);
            Assert.IsTrue(restored.ShowHintHeatMap);
            Assert.IsFalse(restored.UseNeuralTrialOrdering);

            UserPreferences? legacy = JsonSerializer.Deserialize("{}", LoopJsonContext.Default.UserPreferences);
            Assert.IsNotNull(legacy);
            Assert.IsFalse(legacy.ShowHintHeatMap);
            Assert.IsTrue(legacy.UseNeuralTrialOrdering);
        }
        finally
        {
            PuzzleHelper.UseNeuralTrialOrdering = prevNeural;
        }
    }
}
