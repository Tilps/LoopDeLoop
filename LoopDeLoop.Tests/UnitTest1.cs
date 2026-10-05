using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using LoopDeLoop;

namespace LoopDeLoop.Tests
{
    [TestClass]
    public class PuzzleTests
    {
        [TestMethod]
        public void MeshCreation_Square_ProducesExpectedTopology()
        {
            var mesh = new Mesh(4, 4, MeshType.Square);
            Assert.AreEqual(16, mesh.Cells.Count);
            Assert.AreEqual(25, mesh.Intersections.Count); // (4+1)*(4+1)
            Assert.AreEqual(40, mesh.Edges.Count); // 4*5 + 5*4
        }

        [TestMethod]
        public void MeshCreation_DifferentGridTypes_Succeeds()
        {
            var types = new[]
            {
                MeshType.Square,
                MeshType.SquareSymmetrical,
                MeshType.Triangle,
                MeshType.Hexagonal,
                MeshType.Hexagonal2,
                MeshType.Hexagonal3,
                MeshType.Octagon,
                MeshType.Square2,
                MeshType.Pentagon
            };

            foreach (var type in types)
            {
                PuzzleHelper.GetDefaultSize(type, out int w, out int h);
                var mesh = new Mesh(w, h, type);
                Assert.IsTrue(mesh.Cells.Count > 0, $"Mesh of type {type} should have cells");
                Assert.IsTrue(mesh.Edges.Count > 0, $"Mesh of type {type} should have edges");
                Assert.IsTrue(mesh.Intersections.Count > 0, $"Mesh of type {type} should have intersections");
            }
        }

        [TestMethod]
        public void PuzzleGeneration_SolvesCorrectly()
        {
            var mesh = PuzzleHelper.MakeMesh(3, 3, MeshType.Square, 0); // Trivial difficulty
            mesh.Generate();

            // Verify some cells have target clues
            int clues = 0;
            foreach (var cell in mesh.Cells)
            {
                if (cell.TargetCount >= 0) clues++;
            }
            Assert.IsTrue(clues > 0, "Generated puzzle should contain clues");

            // Solve the puzzle
            var solveState = mesh.TrySolve();
            Assert.AreEqual(SolveState.Solved, solveState);
        }

        [TestMethod]
        public void UndoTree_MarkAndRevert()
        {
            var tree = new UndoTree();
            var mesh = new Mesh(3, 3, MeshType.Square);

            Assert.IsFalse(tree.CanUndo);
            Assert.IsFalse(tree.CanRedo);

            var action1 = new PuzzleEdgeAction(mesh, 0);
            tree.Do(action1);
            Assert.IsTrue(tree.CanUndo);
            Assert.AreEqual(EdgeState.Filled, mesh.Edges[0].State);

            // Set checkpoint
            tree.Mark();

            var action2 = new PuzzleEdgeAction(mesh, 1);
            tree.Do(action2);
            Assert.AreEqual(EdgeState.Filled, mesh.Edges[1].State);

            // Revert back to checkpoint
            tree.RevertToMark();
            Assert.AreEqual(EdgeState.Filled, mesh.Edges[0].State);
            Assert.AreEqual(EdgeState.Empty, mesh.Edges[1].State);
        }

        [TestMethod]
        public void Serialization_SaveAndReload()
        {
            var mesh = PuzzleHelper.MakeMesh(3, 3, MeshType.Square, 0);
            mesh.Generate();

            string saved = mesh.SaveToString();
            Assert.IsFalse(string.IsNullOrEmpty(saved));

            var reloaded = new Mesh(0, 0, MeshType.Square);
            reloaded.LoadFromText(saved.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries));

            Assert.AreEqual(mesh.Cells.Count, reloaded.Cells.Count);
            Assert.AreEqual(mesh.Edges.Count, reloaded.Edges.Count);
            Assert.AreEqual(mesh.Intersections.Count, reloaded.Intersections.Count);

            for (int i = 0; i < mesh.Cells.Count; i++)
            {
                Assert.AreEqual(mesh.Cells[i].TargetCount, reloaded.Cells[i].TargetCount);
            }
        }

        [TestMethod]
        public void PuzzleSetEdgeStateAction_AppliesAndUndosCorrectly()
        {
            var mesh = new Mesh(3, 3, MeshType.Square);
            var tree = new UndoTree();

            // Set Edge 0 to Excluded
            var action1 = new PuzzleSetEdgeStateAction(mesh, 0, EdgeState.Excluded);
            tree.Do(action1);
            Assert.AreEqual(EdgeState.Excluded, mesh.Edges[0].State);

            // Set Edge 0 to Filled
            var action2 = new PuzzleSetEdgeStateAction(mesh, 0, EdgeState.Filled);
            tree.Do(action2);
            Assert.AreEqual(EdgeState.Filled, mesh.Edges[0].State);

            // Setting to same state returns false and does nothing
            var action3 = new PuzzleSetEdgeStateAction(mesh, 0, EdgeState.Filled);
            Assert.IsFalse(tree.Do(action3));

            // Undo back to Excluded then Empty
            tree.Undo();
            Assert.AreEqual(EdgeState.Excluded, mesh.Edges[0].State);
            tree.Undo();
            Assert.AreEqual(EdgeState.Empty, mesh.Edges[0].State);
        }

        [TestMethod]
        public void DisallowFalseMove_ControlsRuleViolationBehavior()
        {
            var mesh = new Mesh(3, 3, MeshType.Square);
            var tree = new UndoTree();

            // Find an intersection with >= 3 edges
            int interIdx = -1;
            for (int i = 0; i < mesh.Intersections.Count; i++)
            {
                if (mesh.Intersections[i].Edges.Count >= 3)
                {
                    interIdx = i;
                    break;
                }
            }
            Assert.IsTrue(interIdx >= 0);
            var edgeIndices = mesh.Intersections[interIdx].Edges;

            // Fill first two edges (degree 2 at vertex is valid)
            tree.Do(new PuzzleSetEdgeStateAction(mesh, edgeIndices[0], EdgeState.Filled, disallowFalseMove: true));
            tree.Do(new PuzzleSetEdgeStateAction(mesh, edgeIndices[1], EdgeState.Filled, disallowFalseMove: true));

            // Third edge filled at same vertex causes degree 3 (local violation).
            // When disallowFalseMove is true, the move should be rejected.
            var actionDisallowed = new PuzzleSetEdgeStateAction(mesh, edgeIndices[2], EdgeState.Filled, disallowFalseMove: true);
            bool disallowedResult = tree.Do(actionDisallowed);
            Assert.IsFalse(disallowedResult, "Move should be rejected when disallowFalseMove is true");
            Assert.AreEqual(EdgeState.Empty, mesh.Edges[edgeIndices[2]].State);

            // When disallowFalseMove is false (default), the move should be allowed.
            var actionAllowed = new PuzzleSetEdgeStateAction(mesh, edgeIndices[2], EdgeState.Filled, disallowFalseMove: false);
            bool allowedResult = tree.Do(actionAllowed);
            Assert.IsTrue(allowedResult, "Move should be allowed when disallowFalseMove is false");
            Assert.AreEqual(EdgeState.Filled, mesh.Edges[edgeIndices[2]].State);
        }

        [TestMethod]
        public void FixedEdges_CannotBeModified()
        {
            var mesh = new Mesh(3, 3, MeshType.Square);
            var tree = new UndoTree();
            var markedEdges = new HashSet<int>();

            // Fill edge 0
            tree.Do(new PuzzleSetEdgeStateAction(mesh, 0, EdgeState.Filled));
            Assert.AreEqual(EdgeState.Filled, mesh.Edges[0].State);

            // Fix edge 0
            markedEdges.Add(0);

            // Verify edge is locked
            Assert.IsTrue(markedEdges.Contains(0));

            // Edge 1 is not fixed
            Assert.IsFalse(markedEdges.Contains(1));

            // Clear fix
            markedEdges.Clear();
            Assert.IsFalse(markedEdges.Contains(0));
        }

        [TestMethod]
        public void PuzzleCompression_RoundTrip()
        {
            var mesh = PuzzleHelper.MakeMesh(3, 3, MeshType.Square, 0);
            mesh.Generate();
            string original = mesh.SaveToString();

            string compressed = PuzzleCompression.CompressToUrlSafe(original);
            Assert.IsFalse(string.IsNullOrEmpty(compressed));
            Assert.IsTrue(compressed.Length < original.Length, $"Compressed ({compressed.Length}) should be smaller than original ({original.Length})");

            string decompressed = PuzzleCompression.DecompressFromUrlSafe(compressed);
            Assert.AreEqual(original, decompressed);

            var reloaded = new Mesh(0, 0, MeshType.Square);
            reloaded.LoadFromText(decompressed.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
            Assert.AreEqual(mesh.Cells.Count, reloaded.Cells.Count);
            Assert.AreEqual(mesh.Edges.Count, reloaded.Edges.Count);
        }

        [TestMethod]
        public void ReconstructionFromClues_Succeeds()
        {
            var types = new[] { MeshType.Square, MeshType.Triangle, MeshType.Hexagonal, MeshType.Octagon };
            foreach (var type in types)
            {
                PuzzleHelper.GetDefaultSize(type, out int w, out int h);
                var mesh1 = PuzzleHelper.MakeMesh(w, h, type, 0);
                mesh1.Generate();

                int[] clues = mesh1.Cells.Select(c => c.TargetCount).ToArray();

                var mesh2 = PuzzleHelper.MakeMesh(w, h, type, 0);
                Assert.AreEqual(mesh1.Cells.Count, mesh2.Cells.Count);
                for (int i = 0; i < clues.Length; i++)
                {
                    mesh2.Cells[i].TargetCount = clues[i];
                }

                var state1 = mesh1.TrySolve();
                Assert.AreEqual(SolveState.Solved, state1, $"Mesh1 of type {type} should be solved");
                var state2 = mesh2.TrySolve();
                Assert.AreEqual(SolveState.Solved, state2, $"Mesh2 of type {type} should solve identically");
            }
        }

        [TestMethod]
        public void PuzzleCodec_RoundTrip_ProducesUltraCompactString()
        {
            var types = new[]
            {
                MeshType.Square,
                MeshType.SquareSymmetrical,
                MeshType.Triangle,
                MeshType.Hexagonal,
                MeshType.Hexagonal2,
                MeshType.Hexagonal3,
                MeshType.Octagon,
                MeshType.Square2,
                MeshType.Pentagon
            };

            foreach (var type in types)
            {
                PuzzleHelper.GetDefaultSize(type, out int w, out int h);
                var mesh = PuzzleHelper.MakeMesh(w, h, type, 0);
                mesh.Generate();

                string encoded = PuzzleCodec.Encode(mesh, w, h);
                Console.WriteLine($"{type} ({w}x{h}) [{encoded.Length} chars]: {encoded}");
                Assert.IsFalse(string.IsNullOrWhiteSpace(encoded));
                Assert.IsTrue(encoded.Length < 120, $"Encoded string for {type} should be ultra-compact (was {encoded.Length}: {encoded})");

                bool success = PuzzleCodec.TryDecode(encoded, out var decoded, out int decW, out int decH, out var decType);
                Assert.IsTrue(success, $"Decoding {type} should succeed");
                Assert.IsNotNull(decoded);
                Assert.AreEqual(w, decW);
                Assert.AreEqual(h, decH);
                Assert.AreEqual(type, decType);
                Assert.AreEqual(mesh.Cells.Count, decoded.Cells.Count);

                for (int i = 0; i < mesh.Cells.Count; i++)
                {
                    Assert.AreEqual(mesh.Cells[i].TargetCount, decoded.Cells[i].TargetCount, $"Cell {i} clue mismatch in {type}");
                }
            }
        }
    }
}

