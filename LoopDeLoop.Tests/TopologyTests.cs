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
            MeshType.Pentagon,
            MeshType.Kites,
            MeshType.AsymmetricPentagons,
            MeshType.Diamonds,
            MeshType.DiamondSquare,
            MeshType.PentagonHexagon,
            MeshType.FloretPentagons,
            MeshType.CairoPentagons,
            MeshType.Hexagonal4,
            MeshType.Square3,
            MeshType.PentagonHexagon2
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

        [TestMethod]
        public void AllTopologies_CellPerimetersAreContinuousCycles()
        {
            foreach (var type in AllMeshTypes)
            {
                PuzzleHelper.GetDefaultSize(type, out int w, out int h);
                var mesh = new Mesh(w, h, type);

                for (int cIdx = 0; cIdx < mesh.Cells.Count; cIdx++)
                {
                    var cell = mesh.Cells[cIdx];
                    Assert.AreEqual(cell.Intersections.Count, cell.Edges.Count,
                        $"{type}: Cell {cIdx} vertex count ({cell.Intersections.Count}) != edge count ({cell.Edges.Count})");

                    int n = cell.Intersections.Count;
                    for (int j = 0; j < n; j++)
                    {
                        int v1 = cell.Intersections[j];
                        int v2 = cell.Intersections[(j + 1) % n];
                        int edge = mesh.GetEdgeJoining(v1, v2);
                        Assert.IsTrue(edge >= 0,
                            $"{type}: Cell {cIdx} consecutive vertices {v1} and {v2} are not joined by an edge");
                        CollectionAssert.Contains(cell.Edges, edge,
                            $"{type}: Cell {cIdx} does not contain edge {edge} connecting vertices {v1} and {v2}");
                    }
                }
            }
        }

        [TestMethod]
        public void NewTopologies_CodecRoundTrip()
        {
            MeshType[] newTypes =
            {
                MeshType.Kites,
                MeshType.AsymmetricPentagons,
                MeshType.Diamonds,
                MeshType.DiamondSquare,
                MeshType.PentagonHexagon,
                MeshType.FloretPentagons,
                MeshType.CairoPentagons,
                MeshType.Hexagonal4,
                MeshType.Square3,
                MeshType.PentagonHexagon2
            };

            foreach (var type in newTypes)
            {
                PuzzleHelper.GetDefaultSize(type, out int w, out int h);
                var mesh = new Mesh(w, h, type);

                // Set sample clues
                for (int i = 0; i < mesh.Cells.Count; i += 3)
                {
                    mesh.SetClue(i, i % 3);
                }

                string encoded = PuzzleCodec.Encode(mesh, w, h);
                Assert.IsFalse(string.IsNullOrEmpty(encoded), $"{type}: Encoded string should not be empty");

                bool success = PuzzleCodec.TryDecode(encoded, out var decoded, out int decW, out int decH, out var decType);
                Assert.IsTrue(success, $"{type}: TryDecode should succeed");
                Assert.IsNotNull(decoded, $"{type}: Decoded mesh should not be null");
                Assert.AreEqual(w, decW, $"{type}: Width mismatch after round-trip");
                Assert.AreEqual(h, decH, $"{type}: Height mismatch after round-trip");
                Assert.AreEqual(mesh.MeshType, decoded!.MeshType, $"{type}: MeshType mismatch after round-trip");
                Assert.AreEqual(mesh.Cells.Count, decoded.Cells.Count, $"{type}: Cells count mismatch after round-trip");
                Assert.AreEqual(mesh.Edges.Count, decoded.Edges.Count, $"{type}: Edges count mismatch after round-trip");
                Assert.AreEqual(mesh.Intersections.Count, decoded.Intersections.Count, $"{type}: Intersections count mismatch after round-trip");

                for (int i = 0; i < mesh.Cells.Count; i++)
                {
                    Assert.AreEqual(mesh.Cells[i].TargetCount, decoded.Cells[i].TargetCount,
                        $"{type}: Cell {i} clue mismatch after round-trip");
                }
            }
        }

        [TestMethod]
        public void NewTopologies_CanSolveWithClues()
        {
            MeshType[] newTypes =
            {
                MeshType.Kites,
                MeshType.AsymmetricPentagons,
                MeshType.Diamonds,
                MeshType.DiamondSquare,
                MeshType.PentagonHexagon,
                MeshType.FloretPentagons,
                MeshType.CairoPentagons,
                MeshType.Hexagonal4
            };

            foreach (var type in newTypes)
            {
                PuzzleHelper.GetDefaultSize(type, out int w, out int h);
                var mesh = new Mesh(w, h, type);

                // Setting a 0 clue forces all incident edges to be Empty
                mesh.SetClue(0, 0);

                var state = mesh.TrySolve();
                Assert.AreNotEqual(SolveState.NoSolutions, state,
                    $"{type}: Solver should not conclude NoSolutions for empty grid with single clue 0");

                foreach (int edgeIdx in mesh.Cells[0].Edges)
                {
                    Assert.AreEqual(EdgeState.Empty, mesh.Edges[edgeIdx].State,
                        $"{type}: Edge {edgeIdx} of cell 0 should be marked Empty by clue 0");
                }
            }
        }

        [TestMethod]
        public void DefaultSizes_MatchExpectedDefaults()
        {
            PuzzleHelper.GetDefaultSize(MeshType.Kites, out int kw, out int kh);
            Assert.AreEqual(5, kw);
            Assert.AreEqual(5, kh);

            PuzzleHelper.GetDefaultSize(MeshType.AsymmetricPentagons, out int apw, out int aph);
            Assert.AreEqual(6, apw);
            Assert.AreEqual(6, aph);

            PuzzleHelper.GetDefaultSize(MeshType.CairoPentagons, out int cw, out int ch);
            Assert.AreEqual(5, cw);
            Assert.AreEqual(5, ch);

            PuzzleHelper.GetDefaultSize(MeshType.FloretPentagons, out int fw, out int fh);
            Assert.AreEqual(5, fw);
            Assert.AreEqual(5, fh);
        }

        [TestMethod]
        public void CairoPentagons_HasExpectedCellCountAndManifoldEdges()
        {
            var mesh = new Mesh(3, 3, MeshType.CairoPentagons);
            // 3x3 units, 4 pentagons each = 36 cells
            Assert.AreEqual(36, mesh.Cells.Count);

            foreach (var edge in mesh.Edges)
            {
                Assert.IsTrue(edge.Cells.Count == 1 || edge.Cells.Count == 2,
                    $"CairoPentagons edge has invalid cell count: {edge.Cells.Count}");
            }
        }

        [TestMethod]
        public void Test_Kites_ApproxPointStorage()
        {
            var oldMesh = new Mesh(3, 3, MeshType.Kites);

            var newMesh = new Mesh(0, 0, MeshType.Square);
            newMesh.Intersections.Clear();
            newMesh.Edges.Clear();
            newMesh.Cells.Clear();

            var storage = new ApproxPointStorage(0.001f);
            var intersects = new List<int>();

            float hexWidth = 2.0f;
            float halfWidth = hexWidth / 2f;
            float quarterWidth = halfWidth / 2f;
            float sqrt3 = MathF.Sqrt(3f);
            float rowHeight = sqrt3 * halfWidth;
            float shortDist = rowHeight / 3f;
            float longDist = rowHeight - shortDist;

            for (int row = 0; row < 3; row++)
            {
                int rowWidth = (row % 2 == 1) ? 4 : 3;
                for (int col = 0; col < rowWidth; col++)
                {
                    float cx = col * hexWidth - (row % 2) * halfWidth;
                    float cy = row * rowHeight;

                    void AddPoly(params (float X, float Y)[] pts)
                    {
                        intersects.Clear();
                        foreach (var pt in pts)
                        {
                            int beforeCount = newMesh.Intersections.Count;
                            int idx = storage.Add(pt.X, pt.Y, beforeCount);
                            intersects.Add(idx);
                            if (idx == beforeCount)
                            {
                                var inters = new Intersection { X = pt.X, Y = pt.Y };
                                newMesh.Intersections.Add(inters);
                                if (Math.Abs(pt.X - 2.0f) < 0.01f && Math.Abs(pt.Y - 1.1547f) < 0.01f)
                                {
                                    Console.WriteLine($"Added new intersection {idx} at ({pt.X}, {pt.Y}) in row={row}, col={col}");
                                }
                            }
                            else
                            {
                                if (Math.Abs(pt.X - 2.0f) < 0.01f && Math.Abs(pt.Y - 1.1547f) < 0.01f)
                                {
                                    Console.WriteLine($"Reused intersection {idx} for ({pt.X}, {pt.Y}) in row={row}, col={col}");
                                }
                            }
                        }
                        for (int k = 0; k < intersects.Count; k++)
                        {
                            int s = intersects[k];
                            int e = intersects[(k + 1) % intersects.Count];
                            try { newMesh.GetEdgeJoining(s, e); }
                            catch { newMesh.AddEdge(s, e); }
                        }
                    }

                    var top = (cx, cy - longDist);
                    var bottom = (cx, cy + longDist);
                    var topLeft = (cx - halfWidth, cy - shortDist);
                    var topRight = (cx + halfWidth, cy - shortDist);
                    var midLeft = (cx - halfWidth, cy);
                    var center = (cx, cy);
                    var midRight = (cx + halfWidth, cy);
                    var bottomLeft = (cx - halfWidth, cy + shortDist);
                    var bottomRight = (cx + halfWidth, cy + shortDist);
                    var upperMidLeft = (cx - quarterWidth, cy - longDist + shortDist / 2f);
                    var upperMidRight = (cx + quarterWidth, cy - longDist + shortDist / 2f);
                    var lowerMidLeft = (cx - quarterWidth, cy + longDist - shortDist / 2f);
                    var lowerMidRight = (cx + quarterWidth, cy + longDist - shortDist / 2f);

                    AddPoly(upperMidLeft, top, upperMidRight, center);
                    AddPoly(topLeft, upperMidLeft, center, midLeft);
                    AddPoly(upperMidRight, topRight, midRight, center);
                    AddPoly(center, lowerMidRight, bottom, lowerMidLeft);
                    AddPoly(midLeft, center, lowerMidLeft, bottomLeft);
                    AddPoly(midRight, bottomRight, lowerMidRight, center);
                }
            }

            newMesh.CreateCells();

            // Normalize newMesh coordinates to minX=0, minY=0 like oldMesh
            float minX = float.MaxValue, minY = float.MaxValue;
            foreach (var inters in newMesh.Intersections)
            {
                if (inters.X < minX) minX = inters.X;
                if (inters.Y < minY) minY = inters.Y;
            }
            foreach (var inters in newMesh.Intersections)
            {
                inters.X -= minX;
                inters.Y -= minY;
            }

            Console.WriteLine($"oldMesh cells={oldMesh.Cells.Count}, edges={oldMesh.Edges.Count}, vertices={oldMesh.Intersections.Count}");
            Console.WriteLine($"newMesh cells={newMesh.Cells.Count}, edges={newMesh.Edges.Count}, vertices={newMesh.Intersections.Count}");

            Assert.AreEqual(oldMesh.Cells.Count, newMesh.Cells.Count, "Cell count mismatch");
            Assert.AreEqual(oldMesh.Edges.Count, newMesh.Edges.Count, "Edge count mismatch");
            Assert.AreEqual(oldMesh.Intersections.Count, newMesh.Intersections.Count, "Vertex count mismatch");

            // Verify vertex correspondence
            int[] oldToNewVertex = new int[oldMesh.Intersections.Count];
            for (int i = 0; i < oldMesh.Intersections.Count; i++)
            {
                var ov = oldMesh.Intersections[i];
                int match = -1;
                for (int j = 0; j < newMesh.Intersections.Count; j++)
                {
                    var nv = newMesh.Intersections[j];
                    if (MathF.Abs(ov.X - nv.X) < 0.001f && MathF.Abs(ov.Y - nv.Y) < 0.001f)
                    {
                        match = j;
                        break;
                    }
                }
                Assert.IsTrue(match >= 0, $"No matching vertex in newMesh for oldMesh vertex {i} at ({ov.X}, {ov.Y})");
                oldToNewVertex[i] = match;
            }

            // Verify edge correspondence
            for (int i = 0; i < oldMesh.Edges.Count; i++)
            {
                var oe = oldMesh.Edges[i];
                int nv1 = oldToNewVertex[oe.Intersections[0]];
                int nv2 = oldToNewVertex[oe.Intersections[1]];
                int newEdge = newMesh.GetEdgeJoining(nv1, nv2);
                Assert.IsTrue(newEdge >= 0, $"No matching edge in newMesh between vertices {nv1} and {nv2}");
            }
        }

        [TestMethod]
        public void Test_Diamonds_ApproxPointStorage()
        {
            var oldMesh = new Mesh(4, 4, MeshType.Diamonds);

            var newMesh = new Mesh(0, 0, MeshType.Square);
            newMesh.Intersections.Clear();
            newMesh.Edges.Clear();
            newMesh.Cells.Clear();

            var storage = new ApproxPointStorage(0.001f);
            var intersects = new List<int>();

            float side = 1.5f;
            float halfSide = side / 2f;
            float sqrt3 = MathF.Sqrt(3f);
            float radius = sqrt3 * halfSide;
            float colSpacing = 2f * radius;
            float rowSpacing = 2f * side - halfSide;

            void AddPoly(params (float X, float Y)[] pts)
            {
                intersects.Clear();
                foreach (var pt in pts)
                {
                    int idx = storage.Add(pt.X, pt.Y, newMesh.Intersections.Count);
                    intersects.Add(idx);
                    if (idx == newMesh.Intersections.Count)
                    {
                        var inters = new Intersection { X = pt.X, Y = pt.Y };
                        newMesh.Intersections.Add(inters);
                    }
                }
                for (int k = 0; k < intersects.Count; k++)
                {
                    int s = intersects[k];
                    int e = intersects[(k + 1) % intersects.Count];
                    try { newMesh.GetEdgeJoining(s, e); }
                    catch { newMesh.AddEdge(s, e); }
                }
            }

            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    float cx = radius + col * colSpacing + (row % 2) * radius;
                    float cy = row * rowSpacing + side;

                    var top = (cx, cy - side);
                    var topLeft = (cx - radius, cy - halfSide);
                    var topRight = (cx + radius, cy - halfSide);
                    var center = (cx, cy);
                    var bottomLeft = (cx - radius, cy + halfSide);
                    var bottomRight = (cx + radius, cy + halfSide);
                    var bottom = (cx, cy + side);

                    AddPoly(topLeft, top, topRight, center);
                    AddPoly(topLeft, center, bottom, bottomLeft);
                    AddPoly(center, topRight, bottomRight, bottom);
                }
            }

            newMesh.CreateCells();

            // Normalize newMesh coordinates to minX=0, minY=0 like oldMesh
            float minX = float.MaxValue, minY = float.MaxValue;
            foreach (var inters in newMesh.Intersections)
            {
                if (inters.X < minX) minX = inters.X;
                if (inters.Y < minY) minY = inters.Y;
            }
            foreach (var inters in newMesh.Intersections)
            {
                inters.X -= minX;
                inters.Y -= minY;
            }

            Assert.AreEqual(oldMesh.Cells.Count, newMesh.Cells.Count, "Cell count mismatch");
            Assert.AreEqual(oldMesh.Edges.Count, newMesh.Edges.Count, "Edge count mismatch");
            Assert.AreEqual(oldMesh.Intersections.Count, newMesh.Intersections.Count, "Vertex count mismatch");

            // Verify vertex correspondence
            int[] oldToNewVertex = new int[oldMesh.Intersections.Count];
            for (int i = 0; i < oldMesh.Intersections.Count; i++)
            {
                var ov = oldMesh.Intersections[i];
                int match = -1;
                for (int j = 0; j < newMesh.Intersections.Count; j++)
                {
                    var nv = newMesh.Intersections[j];
                    if (MathF.Abs(ov.X - nv.X) < 0.001f && MathF.Abs(ov.Y - nv.Y) < 0.001f)
                    {
                        match = j;
                        break;
                    }
                }
                Assert.IsTrue(match >= 0, $"No matching vertex in newMesh for oldMesh vertex {i} at ({ov.X}, {ov.Y})");
                oldToNewVertex[i] = match;
            }

            // Verify edge correspondence
            for (int i = 0; i < oldMesh.Edges.Count; i++)
            {
                var oe = oldMesh.Edges[i];
                int nv1 = oldToNewVertex[oe.Intersections[0]];
                int nv2 = oldToNewVertex[oe.Intersections[1]];
                int newEdge = newMesh.GetEdgeJoining(nv1, nv2);
                Assert.IsTrue(newEdge >= 0, $"No matching edge in newMesh between vertices {nv1} and {nv2}");
            }
        }
    }
}

