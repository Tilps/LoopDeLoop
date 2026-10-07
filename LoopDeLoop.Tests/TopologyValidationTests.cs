using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LoopDeLoop;

namespace LoopDeLoop.Tests
{
    [TestClass]
    public class TopologyValidationTests
    {
        private static void AssertMeshStructureValid(Mesh mesh, string label)
        {
            Assert.IsTrue(mesh.Cells.Count > 0, $"{label}: Cell count must be > 0");
            Assert.IsTrue(mesh.Edges.Count > 0, $"{label}: Edge count must be > 0");
            Assert.IsTrue(mesh.Intersections.Count > 0, $"{label}: Vertex count must be > 0");

            for (int eIdx = 0; eIdx < mesh.Edges.Count; eIdx++)
            {
                var edge = mesh.Edges[eIdx];
                Assert.AreEqual(2, edge.Intersections.Length, $"{label}: Edge {eIdx} must have 2 endpoints");
                int v1 = edge.Intersections[0];
                int v2 = edge.Intersections[1];
                Assert.IsTrue(v1 >= 0 && v1 < mesh.Intersections.Count, $"{label}: Invalid v1 {v1}");
                Assert.IsTrue(v2 >= 0 && v2 < mesh.Intersections.Count, $"{label}: Invalid v2 {v2}");
                Assert.AreNotEqual(v1, v2, $"{label}: Self-loop on edge {eIdx}");
                Assert.IsTrue(edge.Cells.Count == 1 || edge.Cells.Count == 2,
                    $"{label}: Edge {eIdx} must belong to 1 or 2 cells (got {edge.Cells.Count})");
            }

            for (int cIdx = 0; cIdx < mesh.Cells.Count; cIdx++)
            {
                var cell = mesh.Cells[cIdx];
                Assert.IsTrue(cell.Intersections.Count >= 3, $"{label}: Cell {cIdx} has fewer than 3 vertices");
                Assert.AreEqual(cell.Intersections.Count, cell.Edges.Count, $"{label}: Cell {cIdx} vertices != edges");

                int n = cell.Intersections.Count;
                for (int j = 0; j < n; j++)
                {
                    int v1 = cell.Intersections[j];
                    int v2 = cell.Intersections[(j + 1) % n];
                    int e = mesh.GetEdgeJoining(v1, v2);
                    Assert.IsTrue(e >= 0, $"{label}: Missing edge between cell {cIdx} vertices {v1} and {v2}");
                    CollectionAssert.Contains(cell.Edges, e, $"{label}: Cell {cIdx} missing edge {e}");
                }
            }

            for (int vIdx = 0; vIdx < mesh.Intersections.Count; vIdx++)
            {
                var v = mesh.Intersections[vIdx];
                Assert.IsFalse(float.IsNaN(v.X) || float.IsInfinity(v.X), $"{label}: Vertex {vIdx} invalid X");
                Assert.IsFalse(float.IsNaN(v.Y) || float.IsInfinity(v.Y), $"{label}: Vertex {vIdx} invalid Y");
                Assert.IsTrue(v.Edges.Count >= 2, $"{label}: Vertex {vIdx} has degree < 2 ({v.Edges.Count})");
            }
        }

        [TestMethod]
        [DataRow(3, 3)]
        [DataRow(5, 5)]
        [DataRow(6, 4)]
        public void Validate_Kites(int w, int h)
        {
            var mesh = new Mesh(w, h, MeshType.Kites);
            AssertMeshStructureValid(mesh, $"Kites_{w}x{h}");
        }

        [TestMethod]
        [DataRow(2, 2)]
        [DataRow(4, 4)]
        [DataRow(5, 3)]
        public void Validate_Diamonds(int w, int h)
        {
            var mesh = new Mesh(w, h, MeshType.Diamonds);
            AssertMeshStructureValid(mesh, $"Diamonds_{w}x{h}");
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(4)]
        public void Validate_DiamondSquare(int layers)
        {
            var mesh = new Mesh(layers, layers, MeshType.DiamondSquare);
            AssertMeshStructureValid(mesh, $"DiamondSquare_L{layers}");
        }

        [TestMethod]
        [DataRow(2, 2)]
        [DataRow(3, 3)]
        [DataRow(4, 2)]
        public void Validate_FloretPentagons(int w, int h)
        {
            var mesh = new Mesh(w, h, MeshType.FloretPentagons);
            AssertMeshStructureValid(mesh, $"FloretPentagons_{w}x{h}");
        }

        [TestMethod]
        [DataRow(2, 2)]
        [DataRow(3, 3)]
        [DataRow(4, 4)]
        public void Validate_CairoPentagons(int w, int h)
        {
            var mesh = new Mesh(w, h, MeshType.CairoPentagons);
            AssertMeshStructureValid(mesh, $"CairoPentagons_{w}x{h}");
        }

        [TestMethod]
        [DataRow(2, 2)]
        [DataRow(3, 3)]
        [DataRow(4, 2)]
        public void Validate_PentagonHexagon(int w, int h)
        {
            var mesh = new Mesh(w, h, MeshType.PentagonHexagon);
            AssertMeshStructureValid(mesh, $"PentagonHexagon_{w}x{h}");
        }

        [TestMethod]
        [DataRow(2, 2)]
        [DataRow(3, 3)]
        [DataRow(4, 3)]
        public void Validate_Hexagonal4(int w, int h)
        {
            var mesh = new Mesh(w, h, MeshType.Hexagonal4);
            AssertMeshStructureValid(mesh, $"Hexagonal4_{w}x{h}");
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        [DataRow(4)]
        [DataRow(5)]
        [DataRow(6)]
        [DataRow(7)]
        [DataRow(8)]
        public void Validate_AsymmetricPentagons(int layers)
        {
            var mesh = new Mesh(layers, layers, MeshType.AsymmetricPentagons);
            AssertMeshStructureValid(mesh, $"AsymmetricPentagons_L{layers}");
        }

        [TestMethod]
        [DataRow(2, 2)]
        [DataRow(3, 3)]
        [DataRow(4, 3)]
        [DataRow(5, 5)]
        public void Validate_Square3(int w, int h)
        {
            var mesh = new Mesh(w, h, MeshType.Square3);
            AssertMeshStructureValid(mesh, $"Square3_{w}x{h}");
        }

        [TestMethod]
        [DataRow(2, 2)]
        [DataRow(3, 3)]
        [DataRow(4, 3)]
        [DataRow(5, 4)]
        [DataRow(8, 4)]
        public void Validate_PentagonHexagon2(int w, int h)
        {
            var mesh = new Mesh(w, h, MeshType.PentagonHexagon2);
            AssertMeshStructureValid(mesh, $"PentagonHexagon2_{w}x{h}");
        }
    }
}

