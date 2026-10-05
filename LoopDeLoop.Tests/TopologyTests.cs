using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LoopDeLoop;

namespace LoopDeLoop.Tests
{
    [TestClass]
    public class TopologyTests
    {
        private static readonly MeshType[] AllMeshTypes =
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

        [TestMethod]
        public void AllTopologies_HaveValidEdgeIntersectionPointers()
        {
            foreach (var type in AllMeshTypes)
            {
                PuzzleHelper.GetDefaultSize(type, out int w, out int h);
                var mesh = new Mesh(w, h, type);

                for (int eIdx = 0; eIdx < mesh.Edges.Count; eIdx++)
                {
                    var edge = mesh.Edges[eIdx];
                    Assert.AreEqual(2, edge.Intersections.Length,
                        $"{type}: Edge {eIdx} must have exactly 2 intersection endpoints");

                    int i1 = edge.Intersections[0];
                    int i2 = edge.Intersections[1];

                    Assert.IsTrue(i1 >= 0 && i1 < mesh.Intersections.Count,
                        $"{type}: Edge {eIdx} intersection 0 ({i1}) out of range [0, {mesh.Intersections.Count})");
                    Assert.IsTrue(i2 >= 0 && i2 < mesh.Intersections.Count,
                        $"{type}: Edge {eIdx} intersection 1 ({i2}) out of range [0, {mesh.Intersections.Count})");
                    Assert.AreNotEqual(i1, i2,
                        $"{type}: Edge {eIdx} has identical endpoints ({i1} == {i2})");

                    // Verify reciprocal adjacency: both intersections reference this edge
                    CollectionAssert.Contains(mesh.Intersections[i1].Edges, eIdx,
                        $"{type}: Intersection {i1} does not reference incident edge {eIdx}");
                    CollectionAssert.Contains(mesh.Intersections[i2].Edges, eIdx,
                        $"{type}: Intersection {i2} does not reference incident edge {eIdx}");
                }
            }
        }

        [TestMethod]
        public void AllTopologies_CellEdgeConsistencyAndManifoldProperty()
        {
            foreach (var type in AllMeshTypes)
            {
                PuzzleHelper.GetDefaultSize(type, out int w, out int h);
                var mesh = new Mesh(w, h, type);

                // Cell cycle sanity: each cell has at least 3 edges forming a polygon
                for (int cIdx = 0; cIdx < mesh.Cells.Count; cIdx++)
                {
                    var cell = mesh.Cells[cIdx];
                    Assert.IsTrue(cell.Edges.Count >= 3,
                        $"{type}: Cell {cIdx} has fewer than 3 edges ({cell.Edges.Count})");

                    foreach (int eIdx in cell.Edges)
                    {
                        Assert.IsTrue(eIdx >= 0 && eIdx < mesh.Edges.Count,
                            $"{type}: Cell {cIdx} references invalid edge index {eIdx}");
                        CollectionAssert.Contains(mesh.Edges[eIdx].Cells, cIdx,
                            $"{type}: Edge {eIdx} does not reciprocally reference cell {cIdx}");
                    }
                }

                // Manifold boundary property: an edge belongs to either 1 cell (boundary) or 2 cells (internal)
                for (int eIdx = 0; eIdx < mesh.Edges.Count; eIdx++)
                {
                    var edge = mesh.Edges[eIdx];
                    Assert.IsTrue(edge.Cells.Count == 1 || edge.Cells.Count == 2,
                        $"{type}: Edge {eIdx} belongs to {edge.Cells.Count} cells (expected 1 or 2)");
                }
            }
        }

        [TestMethod]
        public void AllTopologies_VertexCoordinatesAreFinite()
        {
            foreach (var type in AllMeshTypes)
            {
                PuzzleHelper.GetDefaultSize(type, out int w, out int h);
                var mesh = new Mesh(w, h, type);

                for (int i = 0; i < mesh.Intersections.Count; i++)
                {
                    var inter = mesh.Intersections[i];
                    Assert.IsFalse(double.IsNaN(inter.X) || double.IsInfinity(inter.X),
                        $"{type}: Intersection {i} has invalid X: {inter.X}");
                    Assert.IsFalse(double.IsNaN(inter.Y) || double.IsInfinity(inter.Y),
                        $"{type}: Intersection {i} has invalid Y: {inter.Y}");
                }
            }
        }
    }
}

