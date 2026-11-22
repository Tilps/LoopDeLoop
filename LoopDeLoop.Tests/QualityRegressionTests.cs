using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LoopDeLoop;
using LoopDeLoop.Web.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LoopDeLoop.Tests
{
    [TestClass]
    public class QualityRegressionTests
    {
        [TestMethod]
        public void PerformUnsetZero_SplitsEdgeSetsCorrectly_AndGuardsEmptyEdge()
        {
            var mesh = new Mesh(3, 1, MeshType.Square);
            // Find 3 consecutive top horizontal edges: (0,0)-(1,0), (1,0)-(2,0), (2,0)-(3,0)
            int e0 = mesh.GetEdgeJoining(0, 2);
            int e1 = mesh.GetEdgeJoining(2, 4);
            int e2 = mesh.GetEdgeJoining(4, 6);

            Assert.IsTrue(new SetAction(mesh, e0, EdgeState.Filled).Perform());
            Assert.IsTrue(new SetAction(mesh, e1, EdgeState.Filled).Perform());
            Assert.IsTrue(new SetAction(mesh, e2, EdgeState.Filled).Perform());

            Assert.AreNotEqual(0, mesh.Edges[e0].EdgeSet);
            Assert.AreEqual(mesh.Edges[e0].EdgeSet, mesh.Edges[e1].EdgeSet);
            Assert.AreEqual(mesh.Edges[e0].EdgeSet, mesh.Edges[e2].EdgeSet);

            // Break the middle edge e1: e0 and e2 must now belong to DISTINCT non-zero edge sets
            var unsetMiddle = new UnsetAction(mesh, e1);
            Assert.IsTrue(unsetMiddle.Perform());
            Assert.AreEqual(EdgeState.Empty, mesh.Edges[e1].State);
            Assert.AreEqual(0, mesh.Edges[e1].EdgeSet);
            Assert.AreNotEqual(0, mesh.Edges[e0].EdgeSet);
            Assert.AreNotEqual(0, mesh.Edges[e2].EdgeSet);
            Assert.AreNotEqual(mesh.Edges[e0].EdgeSet, mesh.Edges[e2].EdgeSet,
                "Disconnected filled chains must have distinct EdgeSet IDs after unset");

            // Unperforming the unset must cleanly rejoin them into the original EdgeSet
            unsetMiddle.Unperform();
            Assert.AreEqual(EdgeState.Filled, mesh.Edges[e1].State);
            Assert.AreEqual(mesh.Edges[e0].EdgeSet, mesh.Edges[e2].EdgeSet);

            // Unsetting an already-empty edge must fail cleanly without corrupting ExcludedCount
            int emptyEdge = mesh.GetEdgeJoining(0, 1);
            var unsetEmpty = new UnsetAction(mesh, emptyEdge);
            Assert.IsFalse(unsetEmpty.Perform());
            Assert.IsFalse(unsetEmpty.Successful);
            Assert.AreEqual(0, mesh.Cells[0].ExcludedCount);
            Assert.AreEqual(0, mesh.Intersections[0].ExcludedCount);
        }

        [TestMethod]
        public void MeshLoadFromText_PreservesNonSquareMeshType_AndRejectsMalformedInputs()
        {
            var hex = new Mesh(3, 3, MeshType.Hexagonal);
            hex.SetClue(0, 3);
            string serialized = hex.SaveToString();

            Assert.IsTrue(PuzzleFormatHelper.TryParsePuzzle(serialized, out Mesh? loaded, out string fmt, out _, out _));
            Assert.AreEqual("Loop File", fmt);
            Assert.IsNotNull(loaded);
            Assert.AreEqual(MeshType.Hexagonal, loaded.MeshType, "LoadFromText must preserve non-square MeshType from line 0");
            Assert.AreEqual(hex.Cells.Count, loaded.Cells.Count);
            Assert.AreEqual(3, loaded.Cells[0].TargetCount);

            // Malformed inputs must return false cleanly (never throw)
            string[] badPayloads =
            [
                "Square\nIntersections\n2\n0 0", // truncated
                "Square\nIntersections\n1\nNaN 0\nEdges\n0\nCells\n0", // NaN coordinate
                "Square\nIntersections\n2\n0 0\n1 0\nEdges\n1\n0 99 Filled\nCells\n0", // out-of-range vertex
                "Square\nIntersections\n2\n0 0\n1 0\nEdges\n1\n0 0 Filled\nCells\n0", // self-loop
                "Square\nIntersections\n2\n0 0\n1 0\nEdges\n2\n0 1 Filled\n1 0 Excluded\nCells\n0", // duplicate edge
                "NotARealMeshType\nIntersections\n0\nEdges\n0\nCells\n0" // invalid MeshType
            ];

            foreach (string payload in badPayloads)
            {
                var target = new Mesh(0, 0, MeshType.Square);
                Assert.IsFalse(target.LoadFromText(payload.AsSpan()), $"Expected malformed payload to fail cleanly: {payload}");
            }
        }

        [TestMethod]
        public void PuzzleCodec_RejectsArbitraryUrls_OversizedGrids_AndInvalidClues()
        {
            Assert.IsFalse(PuzzleCodec.TryDecode("https://example.com/foo", out _, out _, out _, out _));
            Assert.IsFalse(PuzzleCodec.TryDecode("unknown/3x3/", out _, out _, out _, out _));
            Assert.IsFalse(PuzzleCodec.TryDecode("sq/500x500/", out _, out _, out _, out _));
            Assert.IsFalse(PuzzleCodec.TryDecode("sq//", out _, out _, out _, out _));
            Assert.IsFalse(PuzzleCodec.TryDecode("sq/2x2/9", out _, out _, out _, out _), "Clue 9 is impossible on a 4-sided square");
            Assert.IsFalse(PuzzleCodec.TryDecode("sq/2x2/2!2", out _, out _, out _, out _), "Invalid characters in clues must be rejected");
            Assert.IsFalse(PuzzleHelper.ParseSize("1000x1000", MeshType.Square, out _, out _));
            Assert.AreEqual(MeshType.Square, PuzzleHelper.MeshTypeFromString("99999"), "Undefined numeric enum values must not parse as raw enum");
            Assert.AreEqual(MeshType.Hexagonal, PuzzleHelper.MeshTypeFromString("hexagon"));
        }

        [TestMethod]
        public void PuzzleCompression_RejectsOversizedDeflatePayload()
        {
            // Compress a 32 KB repetitive payload, then decode with a strict 1 KB cap
            string bigText = new string('A', 32 * 1024);
            string compressed = PuzzleCompression.CompressToUrlSafe(bigText);

            Assert.IsFalse(PuzzleCompression.TryDecompressFromUrlSafe(compressed, out _, maxBytes: 1024));
            Assert.IsTrue(PuzzleCompression.TryDecompressFromUrlSafe(compressed, out string ok, maxBytes: 64 * 1024));
            Assert.AreEqual(bigText, ok);
        }

        [TestMethod]
        public void PuzzleFormatHelper_EnforcesLoopyNStates_GridTypes_Bounds_AndStrictAscii()
        {
            var mesh = new Mesh(2, 2, MeshType.Square);
            mesh.SetClue(0, 2);
            new SetAction(mesh, 0, EdgeState.Filled).Perform();

            string? loopySave = PuzzleFormatHelper.ExportLoopySave(mesh);
            Assert.IsNotNull(loopySave);
            // 1 initial state + 1 MOVE (incomplete, no SOLVE) => NSTATES = 2
            Assert.IsTrue(loopySave.Contains("NSTATES :1:2"), $"Expected NSTATES=2, got:\n{loopySave}");
            Assert.IsTrue(loopySave.Contains("STATEPOS:1:2"), $"Expected STATEPOS=2, got:\n{loopySave}");

            // Reject non-square Loopy grid types (e.g. t2 = hexagonal) and truncated descriptors
            Assert.IsFalse(PuzzleFormatHelper.TryParsePuzzle("3x3t2:a2a", out _, out _, out _, out _));
            Assert.IsFalse(PuzzleFormatHelper.TryParsePuzzle("10x10:0", out _, out _, out _, out _));

            // Reject Loopy save with out-of-bounds move coordinate (would previously alias onto another column)
            string badCoordSave = string.Join("\n",
                "SAVEFILE:41:Simon Tatham's Portable Puzzle Collection",
                "VERSION :1:1",
                "GAME    :5:Loopy",
                "PARAMS  :7:2x2r0de",
                "DESC    :4:2222",
                "MOVE    :6:0,99vy");
            Assert.IsFalse(PuzzleFormatHelper.TryParsePuzzle(badCoordSave, out _, out _, out _, out _));

            // Reject arbitrary text starting with '+'
            string bogusAscii = "+abc+\nhello\n+xyz+";
            Assert.IsFalse(PuzzleFormatHelper.TryParsePuzzle(bogusAscii, out _, out _, out _, out _));
        }

        [TestMethod]
        public void UndoTree_RevertToMark_IgnoresNullOrDetachedMark_AndHashCodeUsesBothCells()
        {
            var mesh = new Mesh(2, 2, MeshType.Square);
            var tree = new UndoTree();

            Assert.IsTrue(tree.Do(new PuzzleSetEdgeStateAction(mesh, 0, EdgeState.Filled)));
            Assert.IsTrue(tree.Do(new PuzzleSetEdgeStateAction(mesh, 1, EdgeState.Filled)));

            // RevertToMark with no mark must be a no-op (not undo all the way to root)
            tree.RevertToMark();
            Assert.AreEqual(EdgeState.Filled, mesh.Edges[0].State);
            Assert.AreEqual(EdgeState.Filled, mesh.Edges[1].State);

            // Mark current node, then UndoAndForget it -> mark moves cleanly to parent
            tree.Mark();
            Assert.IsTrue(tree.UndoAndForget());
            Assert.IsTrue(tree.Do(new PuzzleSetEdgeStateAction(mesh, 2, EdgeState.Filled)));
            tree.RevertToMark();
            Assert.AreEqual(EdgeState.Filled, mesh.Edges[0].State);
            Assert.AreEqual(EdgeState.Empty, mesh.Edges[2].State);

            // CellColorJoinAction.GetHashCode must distinguish cell2
            var a1 = new CellColorJoinAction(mesh, 0, 1, true);
            var a2 = new CellColorJoinAction(mesh, 0, 2, true);
            Assert.AreNotEqual(a1.GetHashCode(), a2.GetHashCode());
        }

        [TestMethod]
        public void GameService_PendingGridAndSizeEdits_DoNotCorruptActiveExportOrShare()
        {
            var game = new GameService();
            var sq2 = new Mesh(2, 2, MeshType.Square);
            for (int i = 0; i < 4; i++) sq2.SetClue(i, 2);
            Assert.IsTrue(game.LoadPuzzleText(PuzzleCodec.Encode(sq2, 2, 2)));
            Assert.AreEqual(2, game.ActiveWidth);
            Assert.AreEqual(2, game.ActiveHeight);

            // User edits pending form inputs without clicking "New Puzzle"
            game.CurrentType = MeshType.Hexagonal;
            game.SizeText = "7x9";

            string share = game.GetShareCode();
            Assert.AreEqual("sq/2x2/2222", share);

            var exported = game.ExportState();
            Assert.IsNotNull(exported);
            Assert.AreEqual("sq/2x2/2222", exported.Puzzle);
            Assert.AreEqual("2x2", exported.Size);
            Assert.AreEqual("Square", exported.Type);
        }

        [TestMethod]
        public void GameService_RestoreState_PreservesPreferences_SanitizesBadData_AndFreezesElapsed()
        {
            var game = new GameService();
            game.DisallowFalseMove = true;
            game.ShowCellColors = true;

            var state = new SavedGameState
            {
                Puzzle = "sq/2x2/2222",
                EdgeStates = [99, -5, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0], // contains invalid enum ints
                CellColors = [1, 42, -1, 0], // contains invalid color 42
                MarkedEdges = [-1, 0, 999],
                ElapsedSeconds = double.NaN,
                IsSolved = false,
                DisallowFalseMove = false, // stale preference in saved game state
                ShowCellColors = false
            };

            Assert.IsTrue(game.RestoreState(state));
            Assert.IsTrue(game.DisallowFalseMove, "RestoreState must not overwrite UserPreferences.DisallowFalseMove");
            Assert.IsTrue(game.ShowCellColors, "RestoreState must not overwrite UserPreferences.ShowCellColors");
            Assert.AreEqual(EdgeState.Empty, game.CurrentMesh!.Edges[0].State);
            Assert.AreEqual(EdgeState.Empty, game.CurrentMesh.Edges[1].State);
            Assert.AreEqual(EdgeState.Filled, game.CurrentMesh.Edges[2].State);
            Assert.AreEqual(0, game.CurrentMesh.Cells[1].Color);

            // Solving freezes ElapsedTime
            Assert.IsTrue(game.SolvePuzzle());
            Assert.IsTrue(game.IsSolved);
            TimeSpan frozen1 = game.ElapsedTime;
            Thread.Sleep(25);
            TimeSpan frozen2 = game.ElapsedTime;
            Assert.AreEqual(frozen1, frozen2, "ElapsedTime must stay frozen once solved");
        }

        [TestMethod]
        public void AotJsonContext_RoundTripsPreferencesAndSavedState()
        {
            var prefs = new UserPreferences
            {
                DisallowFalseMove = true,
                ShowCellColors = true,
                ShowEdgeColors = true,
                AutoMoveLevel = 3,
                AutoStart = true,
                WheelMode = MouseWheelMode.UndoRedo,
                ShowClock = false,
                GenerateLengthFraction = 0.65,
                GenerateBoringFraction = 0.05,
                SolverModeOverride = GeneratorSolverMode.Recursive
            };

            string prefsJson = JsonSerializer.Serialize(prefs, LoopJsonContext.Default.UserPreferences);
            var prefsBack = JsonSerializer.Deserialize(prefsJson, LoopJsonContext.Default.UserPreferences);
            Assert.IsNotNull(prefsBack);
            Assert.AreEqual(prefs.AutoMoveLevel, prefsBack.AutoMoveLevel);
            Assert.AreEqual(prefs.WheelMode, prefsBack.WheelMode);
            Assert.AreEqual(prefs.ShowEdgeColors, prefsBack.ShowEdgeColors);

            var saved = new SavedGameState
            {
                Puzzle = "sq/2x2/2222",
                EdgeStates = [1, 2, 0],
                CellColors = [1, -1, 0, 0],
                MarkedEdges = [0],
                ElapsedSeconds = 123.5,
                IsSolved = true,
                WasAutoSolved = false,
                Type = "Square",
                Size = "2x2",
                Difficulty = 2
            };
            string savedJson = JsonSerializer.Serialize(saved, LoopJsonContext.Default.SavedGameState);
            var savedBack = JsonSerializer.Deserialize(savedJson, LoopJsonContext.Default.SavedGameState);
            Assert.IsNotNull(savedBack);
            Assert.AreEqual(saved.Puzzle, savedBack.Puzzle);
            Assert.AreEqual(saved.ElapsedSeconds, savedBack.ElapsedSeconds);
            Assert.IsTrue(savedBack.IsSolved);
        }

        [TestMethod]
        public void MeshCopyConstructor_CopiesAllOptions_AndSeededGenerationIsDeterministic()
        {
            var src = new Mesh(2, 2, MeshType.Square)
            {
                UseMerging = true,
                UseCellColoring = true,
                UseCellColoringTrials = false,
                UseDerivedColoring = false,
                SuperSlowMo = true
            };

            var clone = new Mesh(src);
            Assert.IsTrue(clone.UseMerging);
            Assert.IsTrue(clone.UseCellColoring);
            Assert.IsFalse(clone.UseCellColoringTrials);
            Assert.IsFalse(clone.UseDerivedColoring);
            Assert.IsTrue(clone.SuperSlowMo);

            // Deterministic seeded generation produces identical puzzles
            var m1 = PuzzleHelper.MakeMesh(3, 3, MeshType.Square, 1);
            var m2 = PuzzleHelper.MakeMesh(3, 3, MeshType.Square, 1);
            m1.Generate(new Random(12345));
            m2.Generate(new Random(12345));
            Assert.AreEqual(PuzzleCodec.Encode(m1, 3, 3), PuzzleCodec.Encode(m2, 3, 3));

            // PercentSolved on iterative solve must be 1.0 (not 0.0) when solved
            m1.SetRatingCodeOptions("S");
            Assert.AreEqual(SolveState.Solved, m1.TrySolve());
            Assert.AreEqual(1.0, m1.PercentSolved, 1e-6);
        }
    }
}
