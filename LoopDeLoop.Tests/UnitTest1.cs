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
    }
}

