using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LoopDeLoop;
using LoopDeLoop.Web.Services;

namespace LoopDeLoop.Tests
{
    [TestClass]
    public class Phase4And5Tests
    {
        private static Mesh CreateSquare2x2Puzzle()
        {
            var mesh = new Mesh(2, 2, MeshType.Square);
            mesh.SetClue(0, 2);
            mesh.SetClue(1, 2);
            mesh.SetClue(2, 2);
            mesh.SetClue(3, 2);
            return mesh;
        }

        [TestMethod]
        public void PuzzleFormatHelper_RoundTripsLoopText_Ascii_LoopyDescriptor_And_LoopySave()
        {
            var mesh = new Mesh(3, 2, MeshType.Square);
            mesh.SetClue(0, 3);
            mesh.SetClue(4, 1);
            // Set one filled and one excluded edge
            new SetAction(mesh, 0, EdgeState.Filled).Perform();
            new SetAction(mesh, 1, EdgeState.Excluded).Perform();

            Assert.IsTrue(PuzzleFormatHelper.CanExportSquareFormats(mesh));
            Assert.IsTrue(PuzzleFormatHelper.TryGetSquareDimensions(mesh, out int w, out int h));
            Assert.AreEqual(3, w);
            Assert.AreEqual(2, h);

            // 1. Native .loop format
            string loopText = PuzzleFormatHelper.ExportLoopText(mesh);
            var game = new GameService();
            Assert.IsTrue(game.ImportPuzzle(loopText, out string loopFmt));
            Assert.AreEqual("Loop File", loopFmt);
            Assert.AreEqual(EdgeState.Filled, game.CurrentMesh!.Edges[0].State);
            Assert.AreEqual(EdgeState.Excluded, game.CurrentMesh.Edges[1].State);
            Assert.AreEqual(3, game.CurrentMesh.Cells[0].TargetCount);

            // 2. Square ASCII .txt format
            string? ascii = PuzzleFormatHelper.ExportSquareAscii(mesh);
            Assert.IsNotNull(ascii);
            Assert.IsTrue(game.ImportPuzzle(ascii, out string asciiFmt));
            Assert.AreEqual("ASCII Grid", asciiFmt);
            Assert.AreEqual(EdgeState.Filled, game.CurrentMesh!.Edges[0].State);
            Assert.AreEqual(EdgeState.Excluded, game.CurrentMesh.Edges[1].State);
            Assert.AreEqual(3, game.CurrentMesh.Cells[0].TargetCount);
            Assert.AreEqual(1, game.CurrentMesh.Cells[4].TargetCount);

            // 3. Loopy Descriptor format
            string? loopyDesc = PuzzleFormatHelper.ExportLoopyDescriptor(mesh);
            Assert.IsNotNull(loopyDesc);
            Assert.StartsWith("3x2:", loopyDesc);
            Assert.IsTrue(game.ImportPuzzle(loopyDesc, out string descFmt));
            Assert.AreEqual("Loopy Puzzle", descFmt);
            Assert.AreEqual(3, game.CurrentMesh!.Cells[0].TargetCount);
            Assert.AreEqual(1, game.CurrentMesh.Cells[4].TargetCount);

            // 4. Loopy Save format
            string? loopySave = PuzzleFormatHelper.ExportLoopySave(mesh);
            Assert.IsNotNull(loopySave);
            Assert.Contains("GAME    :5:Loopy", loopySave);
            Assert.IsTrue(game.ImportPuzzle(loopySave, out string saveFmt));
            Assert.AreEqual("Loopy Save", saveFmt);
            Assert.AreEqual(EdgeState.Filled, game.CurrentMesh!.Edges[0].State);
            Assert.AreEqual(EdgeState.Excluded, game.CurrentMesh.Edges[1].State);
            Assert.AreEqual(3, game.CurrentMesh.Cells[0].TargetCount);
        }

        [TestMethod]
        public void PuzzleFormatHelper_ImportsShareUrl_AndRejectsInvalidInput()
        {
            var mesh = CreateSquare2x2Puzzle();
            string compact = PuzzleCodec.Encode(mesh, 2, 2);
            string fullUrl = $"https://www.themissingdocs.net/loopdeloop/#p={compact}";

            var game = new GameService();
            Assert.IsTrue(game.ImportPuzzle(fullUrl, out string fmt));
            Assert.AreEqual("Share Link", fmt);
            Assert.AreEqual(4, game.CurrentMesh!.Cells.Count);
            Assert.AreEqual("2x2", game.SizeText);

            Assert.IsFalse(game.ImportPuzzle("not a valid puzzle format!!!", out _));

            // Hexagonal mesh is valid for .loop export, not square formats
            var hexMesh = new Mesh(3, 3, MeshType.Hexagonal);
            Assert.IsFalse(PuzzleFormatHelper.CanExportSquareFormats(hexMesh));
        }

        [TestMethod]
        public void HandleEdgeGroupingClick_LinksSameAndOppositeParity_AndSupportsUndo()
        {
            var game = new GameService();
            game.LoadPuzzleText(PuzzleCodec.Encode(CreateSquare2x2Puzzle(), 2, 2));
            game.ShowEdgeColors = true;
            Assert.IsNotNull(game.CurrentMesh);

            // First click sets pending edge; clicking same edge cancels pending state
            Assert.IsTrue(game.HandleEdgeGroupingClick(0, sameLoop: true));
            Assert.AreEqual(0, game.PendingGroupEdgeIndex);
            Assert.IsTrue(game.HandleEdgeGroupingClick(0, sameLoop: true));
            Assert.IsNull(game.PendingGroupEdgeIndex);

            // Link edge 0 and edge 1 as same loop parity
            Assert.IsTrue(game.HandleEdgeGroupingClick(0, sameLoop: true));
            Assert.IsTrue(game.HandleEdgeGroupingClick(1, sameLoop: true));
            Assert.IsNull(game.PendingGroupEdgeIndex);
            Assert.AreNotEqual(0, game.CurrentMesh.Edges[0].Color);
            Assert.AreEqual(game.CurrentMesh.Edges[0].Color, game.CurrentMesh.Edges[1].Color);

            // Link edge 1 and edge 2 as opposite parity
            Assert.IsTrue(game.HandleEdgeGroupingClick(1, sameLoop: false));
            Assert.IsTrue(game.HandleEdgeGroupingClick(2, sameLoop: false));
            Assert.AreEqual(-game.CurrentMesh.Edges[1].Color, game.CurrentMesh.Edges[2].Color);

            // Contradictory link (0 and 2 are opposite, try forcing them same) should fail cleanly
            Assert.IsTrue(game.HandleEdgeGroupingClick(0, sameLoop: true));
            Assert.IsFalse(game.HandleEdgeGroupingClick(2, sameLoop: true));

            // Undo reverses the opposite-parity link cleanly
            game.Undo();
            Assert.AreEqual(0, game.CurrentMesh.Edges[2].Color);

            // Disabling ShowEdgeColors clears any pending selection and round-trips in preferences
            Assert.IsTrue(game.HandleEdgeGroupingClick(0, sameLoop: true));
            Assert.AreEqual(0, game.PendingGroupEdgeIndex);

            var exported = game.ExportPreferences();
            Assert.IsTrue(exported.ShowEdgeColors);
            game.ShowEdgeColors = false;
            Assert.IsNull(game.PendingGroupEdgeIndex);
        }

        [TestMethod]
        public void AnalyzeCurrentPuzzle_ReportsUniqueMultipleAndUnsolvableCorrectly()
        {
            var game = new GameService();

            // 1. Unique solvable puzzle
            game.LoadPuzzleText(PuzzleCodec.Encode(CreateSquare2x2Puzzle(), 2, 2));
            var unique = game.AnalyzeCurrentPuzzle();
            Assert.IsNotNull(unique);
            Assert.AreEqual("Unique Solution", unique.SolveStatus);
            Assert.AreEqual(0, unique.BranchDepth);
            Assert.AreEqual(4, unique.ClueCount);
            Assert.AreEqual(4, unique.TotalCells);
            Assert.Contains("S", unique.TechniqueTier);

            // 2. Empty grid -> Multiple Solutions
            var blank = new Mesh(2, 2, MeshType.Square);
            game.LoadPuzzleText(PuzzleCodec.Encode(blank, 2, 2));
            Assert.IsNull(game.CurrentAnalysis, "Loading new puzzle must clear prior analysis");
            var multiple = game.AnalyzeCurrentPuzzle();
            Assert.IsNotNull(multiple);
            Assert.AreEqual("Multiple Solutions", multiple.SolveStatus);

            // 3. Impossible clues -> No Solution (2x2 corner with clue 3 surrounded by 0s)
            var impossible = new Mesh(2, 2, MeshType.Square);
            impossible.SetClue(0, 3);
            impossible.SetClue(1, 0);
            impossible.SetClue(2, 0);
            game.LoadPuzzleText(PuzzleCodec.Encode(impossible, 2, 2));
            var unsolvable = game.AnalyzeCurrentPuzzle();
            Assert.IsNotNull(unsolvable);
            Assert.AreEqual("No Solution", unsolvable.SolveStatus);
        }
    }
}
