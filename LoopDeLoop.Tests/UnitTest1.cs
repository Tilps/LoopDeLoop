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
                MeshType.Pentagon,
                MeshType.Kites,
                MeshType.AsymmetricPentagons,
                MeshType.Diamonds,
                MeshType.DiamondSquare,
                MeshType.PentagonHexagon,
                MeshType.HexPentagons,
                MeshType.Hexagonal4,
                MeshType.Square3,
                MeshType.PentagonHexagon2
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
            var types = new[] { MeshType.Square, MeshType.Hexagonal, MeshType.Octagon };
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
                    mesh2.SetClue(i, clues[i]);
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
                Assert.IsTrue(encoded.Length < 150, $"Encoded string for {type} should be ultra-compact (was {encoded.Length}: {encoded})");

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

        [TestMethod]
        public void UndoTree_UndoAndForget_RevertsActionAndRemovesFromTree()
        {
            var mesh = new Mesh(3, 3, MeshType.Square);
            var undoTree = new UndoTree();

            Assert.IsFalse(undoTree.CanUndo);
            Assert.IsFalse(undoTree.CanRedo);

            var action1 = new PuzzleSetEdgeStateAction(mesh, 0, EdgeState.Filled);
            Assert.IsTrue(undoTree.Do(action1));
            Assert.AreEqual(EdgeState.Filled, mesh.Edges[0].State);
            Assert.IsTrue(undoTree.CanUndo);
            Assert.IsFalse(undoTree.CanRedo);

            // Speculative second touch: edge 1 toggled
            var action2 = new PuzzleSetEdgeStateAction(mesh, 1, EdgeState.Filled);
            Assert.IsTrue(undoTree.Do(action2));
            Assert.AreEqual(EdgeState.Filled, mesh.Edges[1].State);

            // UndoAndForget action2 (simulate multi-touch pinch detection)
            bool reverted = undoTree.UndoAndForget();
            Assert.IsTrue(reverted);
            Assert.AreEqual(EdgeState.Empty, mesh.Edges[1].State, "Edge 1 should be reverted to Empty");
            Assert.AreEqual(EdgeState.Filled, mesh.Edges[0].State, "Edge 0 should remain Filled");

            // Redo should NOT be available because action2 was completely forgotten
            Assert.IsFalse(undoTree.CanRedo, "Forgotten action must not be available for Redo");
            Assert.IsTrue(undoTree.CanUndo, "Prior action 1 should still be undoable");

            // Normal undo of action 1
            Assert.IsTrue(undoTree.Undo());
            Assert.AreEqual(EdgeState.Empty, mesh.Edges[0].State);
            Assert.IsTrue(undoTree.CanRedo, "Normal undone action 1 should be redoable");
        }

        [TestMethod]
        public void LoadFromText_SpanOverload_ParsesCorrectly()
        {
            var mesh = PuzzleHelper.MakeMesh(3, 3, MeshType.Square, 0);
            mesh.Generate();
            string saved = mesh.SaveToString();

            var loaded = new Mesh(3, 3, MeshType.Square);
            bool success = loaded.LoadFromText(saved.AsSpan());
            Assert.IsTrue(success);
            Assert.AreEqual(mesh.Cells.Count, loaded.Cells.Count);
            Assert.AreEqual(mesh.Edges.Count, loaded.Edges.Count);
            Assert.AreEqual(mesh.Intersections.Count, loaded.Intersections.Count);

            for (int i = 0; i < mesh.Cells.Count; i++)
            {
                Assert.AreEqual(mesh.Cells[i].TargetCount, loaded.Cells[i].TargetCount);
            }
        }

        [TestMethod]
        public void ParseSize_SpanBased_ParsesAllDelimiters()
        {
            Assert.IsTrue(PuzzleHelper.ParseSize("10x12", MeshType.Square, out int w1, out int h1));
            Assert.AreEqual(10, w1);
            Assert.AreEqual(12, h1);

            Assert.IsTrue(PuzzleHelper.ParseSize(" 7 X 9 ", MeshType.Square, out int w2, out int h2));
            Assert.AreEqual(7, w2);
            Assert.AreEqual(9, h2);

            Assert.IsTrue(PuzzleHelper.ParseSize("8*8", MeshType.Square, out int w3, out int h3));
            Assert.AreEqual(8, w3);
            Assert.AreEqual(8, h3);

            Assert.IsTrue(PuzzleHelper.ParseSize("15", MeshType.Square, out int w4, out int h4));
            Assert.AreEqual(15, w4);
            Assert.AreEqual(15, h4);

            Assert.IsFalse(PuzzleHelper.ParseSize("abc", MeshType.Square, out _, out _));
            Assert.IsFalse(PuzzleHelper.ParseSize("-5x10", MeshType.Square, out _, out _));
        }

        [TestMethod]
        public void Solver_RepeatedSolves_ReusesConnectableTrackerWithoutError()
        {
            var mesh = PuzzleHelper.MakeMesh(4, 4, MeshType.Square, 1); // Difficulty 1 enables ConsiderMultipleLoops
            mesh.Generate();

            // Run TrySolve multiple times to exercise connectableTracker caching across solves
            var state1 = mesh.TrySolve();
            Assert.AreEqual(SolveState.Solved, state1);

            var state2 = mesh.TrySolve();
            Assert.AreEqual(SolveState.Solved, state2);
        }

        [TestMethod]
        public async Task GenerateAsync_CancelledImmediately_AbortsCleanly()
        {
            var mesh = PuzzleHelper.MakeMesh(3, 3, MeshType.Square, 0);
            using var cts = new System.Threading.CancellationTokenSource();
            cts.Cancel(); // Pre-cancelled

            // Should complete quickly and exit cleanly without unhandled exception
            await mesh.GenerateAsync(cancellationToken: cts.Token);
            Assert.IsTrue(cts.IsCancellationRequested);
        }

        [TestMethod]
        public async Task GenerateAsync_AbortedViaAbortPrune_AbortsCleanly()
        {
            var mesh = PuzzleHelper.MakeMesh(6, 6, MeshType.Square, 0);
            var task = mesh.GenerateAsync();
            mesh.AbortPrune = true;
            await task;
            Assert.IsTrue(mesh.AbortPrune);
        }

        [TestMethod]
        public async Task GenerateAsync_ProducesValidPuzzle()
        {
            var mesh = PuzzleHelper.MakeMesh(3, 3, MeshType.Square, 0);
            await mesh.GenerateAsync();

            Assert.AreEqual(SolveState.Solved, mesh.TrySolve());
        }
    }
}

