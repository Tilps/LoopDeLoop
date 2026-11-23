using System;
using System.Collections.Generic;

namespace LoopDeLoop.TrialOrdering
{
    /// <summary>
    /// Cached static planar incidence topology for a <see cref="Mesh"/>, avoiding repeated
    /// list traversals and allocations during trial scoring.
    /// </summary>
    public sealed class MeshTopologyCache
    {
        public readonly int NumVerts;
        public readonly int NumEdges;
        public readonly int NumCells;

        // Per-edge incidences (exact 2 intersections, 1 or 2 cells with -1 for outside)
        public readonly int[] EdgeV0;
        public readonly int[] EdgeV1;
        public readonly int[] EdgeC0;
        public readonly int[] EdgeC1;

        // CSR: Intersection -> incident Edges
        public readonly int[] VertEdgeOff;
        public readonly int[] VertEdgeIdx;

        // CSR: Cell -> incident Edges
        public readonly int[] CellEdgeOff;
        public readonly int[] CellEdgeIdx;

        // CSR: Edge -> neighbor Edges sharing an intersection or cell (deduplicated, excluding self)
        public readonly int[] EdgeNbrOff;
        public readonly int[] EdgeNbrIdx;

        public MeshTopologyCache(Mesh mesh)
        {
            NumVerts = mesh.Intersections.Count;
            NumEdges = mesh.Edges.Count;
            NumCells = mesh.Cells.Count;

            EdgeV0 = new int[NumEdges];
            EdgeV1 = new int[NumEdges];
            EdgeC0 = new int[NumEdges];
            EdgeC1 = new int[NumEdges];

            for (int e = 0; e < NumEdges; e++)
            {
                Edge edge = mesh.Edges[e];
                EdgeV0[e] = edge.Intersections[0];
                EdgeV1[e] = edge.Intersections[1];
                EdgeC0[e] = edge.Cells.Count > 0 ? edge.Cells[0] : -1;
                EdgeC1[e] = edge.Cells.Count > 1 ? edge.Cells[1] : -1;
            }

            VertEdgeOff = new int[NumVerts + 1];
            int totalVE = 0;
            for (int v = 0; v < NumVerts; v++)
            {
                VertEdgeOff[v] = totalVE;
                totalVE += mesh.Intersections[v].Edges.Count;
            }
            VertEdgeOff[NumVerts] = totalVE;
            VertEdgeIdx = new int[totalVE];
            for (int v = 0; v < NumVerts; v++)
            {
                int off = VertEdgeOff[v];
                List<int> ve = mesh.Intersections[v].Edges;
                for (int i = 0; i < ve.Count; i++)
                    VertEdgeIdx[off + i] = ve[i];
            }

            CellEdgeOff = new int[NumCells + 1];
            int totalCE = 0;
            for (int c = 0; c < NumCells; c++)
            {
                CellEdgeOff[c] = totalCE;
                totalCE += mesh.Cells[c].Edges.Count;
            }
            CellEdgeOff[NumCells] = totalCE;
            CellEdgeIdx = new int[totalCE];
            for (int c = 0; c < NumCells; c++)
            {
                int off = CellEdgeOff[c];
                List<int> ce = mesh.Cells[c].Edges;
                for (int i = 0; i < ce.Count; i++)
                    CellEdgeIdx[off + i] = ce[i];
            }

            // Build deduplicated local neighbor edges per edge (sharing a vertex or cell) without HashSet allocations
            List<int> nbrList = new List<int>(NumEdges * 8);
            EdgeNbrOff = new int[NumEdges + 1];
            int[] seenMark = new int[NumEdges];
            for (int e = 0; e < NumEdges; e++)
            {
                EdgeNbrOff[e] = nbrList.Count;
                int mark = e + 1;
                seenMark[e] = mark;

                int v0 = EdgeV0[e], v1 = EdgeV1[e];
                for (int k = VertEdgeOff[v0]; k < VertEdgeOff[v0 + 1]; k++)
                {
                    int oe = VertEdgeIdx[k];
                    if (seenMark[oe] != mark) { seenMark[oe] = mark; nbrList.Add(oe); }
                }
                for (int k = VertEdgeOff[v1]; k < VertEdgeOff[v1 + 1]; k++)
                {
                    int oe = VertEdgeIdx[k];
                    if (seenMark[oe] != mark) { seenMark[oe] = mark; nbrList.Add(oe); }
                }

                int c0 = EdgeC0[e], c1 = EdgeC1[e];
                if (c0 >= 0)
                {
                    for (int k = CellEdgeOff[c0]; k < CellEdgeOff[c0 + 1]; k++)
                    {
                        int oe = CellEdgeIdx[k];
                        if (seenMark[oe] != mark) { seenMark[oe] = mark; nbrList.Add(oe); }
                    }
                }
                if (c1 >= 0)
                {
                    for (int k = CellEdgeOff[c1]; k < CellEdgeOff[c1 + 1]; k++)
                    {
                        int oe = CellEdgeIdx[k];
                        if (seenMark[oe] != mark) { seenMark[oe] = mark; nbrList.Add(oe); }
                    }
                }
            }
            EdgeNbrOff[NumEdges] = nbrList.Count;
            EdgeNbrIdx = nbrList.ToArray();
        }
    }

    /// <summary>
    /// Zero-allocation O(V + E + C) feature extractor capturing visible puzzle state,
    /// solver-strength flags, internal coloring sets, loop edge sets, and local projections
    /// of <c>edgePairRestrictions</c> without O(E^2) overhead.
    /// </summary>
    public static class GraphFeatureExtractor
    {
        public const int GlobalFeatDim = 12;
        public const int EdgeFeatDim = 18;
        public const int CellFeatDim = 14;
        public const int VertFeatDim = 8;

        private static readonly float[] Log2Div5 = BuildLogTable(512, 5.0f);
        private static readonly float[] Log2Div6 = BuildLogTable(512, 6.0f);

        private static float[] BuildLogTable(int n, float div)
        {
            float[] t = new float[n];
            for (int i = 0; i < n; i++) t[i] = MathF.Log2(i + 1) / div;
            return t;
        }

        private static float FastLog2Div5(int n) => (uint)n < (uint)Log2Div5.Length ? Log2Div5[n] : MathF.Log2(n + 1) / 5.0f;
        private static float FastLog2Div6(int n) => (uint)n < (uint)Log2Div6.Length ? Log2Div6[n] : MathF.Log2(n + 1) / 6.0f;

        public static void Extract(
            Mesh mesh,
            MeshTopologyCache topo,
            float[] globalFeats,
            float[] edgeFeats,
            float[] cellFeats,
            float[] vertFeats,
            int[] edgeColors,
            int[] cellColors,
            int[] vertEdgeSetScratch,
            int iterativeRecDepth)
        {
            int V = topo.NumVerts;
            int E = topo.NumEdges;
            int C = topo.NumCells;

            List<Edge> edges = mesh.Edges;
            List<Cell> cells = mesh.Cells;
            List<Intersection> inters = mesh.Intersections;
            IReadOnlyList<List<int>> edgeSets = mesh.EdgeSets;
            IReadOnlyList<List<int>> colorSets = mesh.ColorSets;
            IReadOnlyList<List<int>> cellColorSets = mesh.CellColorSets;

            int emptyEdges = 0;
            for (int e = 0; e < E; e++)
                if (edges[e].State == EdgeState.Empty) emptyEdges++;

            int activeEdgeSets = 0;
            for (int i = 0; i < edgeSets.Count; i++)
                if (edgeSets[i].Count > 0) activeEdgeSets++;

            // Global solver-strength + board-progress features
            globalFeats[0] = mesh.IterativeSolverDepth > 1000 ? 1.0f : MathF.Log2(Math.Max(0, mesh.IterativeSolverDepth) + 1) / 8.0f;
            globalFeats[1] = mesh.IterativeSolverDepth >= E ? 1.0f : 0.0f;
            globalFeats[2] = iterativeRecDepth >= 2 ? 1.0f : 0.0f;
            globalFeats[3] = mesh.ConsiderMultipleLoops ? 1.0f : 0.0f;
            globalFeats[4] = mesh.UseColoring ? 1.0f : 0.0f;
            globalFeats[5] = mesh.UseCellColoring ? 1.0f : 0.0f;
            globalFeats[6] = mesh.UseEdgeRestricts ? 1.0f : 0.0f;
            globalFeats[7] = mesh.UseMerging ? 1.0f : 0.0f;
            globalFeats[8] = (mesh.UseCellPairs || mesh.UseCellPairsTopLevel) ? 1.0f : 0.0f;
            globalFeats[9] = E > 0 ? (float)emptyEdges / E : 0f;
            globalFeats[10] = mesh.ClueCellCount > 0 ? (float)(mesh.ClueCellCount - mesh.SatisfiedCellCount) / mesh.ClueCellCount : 0f;
            globalFeats[11] = MathF.Min(activeEdgeSets, 10) / 10.0f;

            // Intersection features + precompute each intersection's active edgeSet
            for (int v = 0; v < V; v++)
            {
                Intersection inter = inters[v];
                int deg = inter.Edges.Count;
                int rem = deg - inter.FilledCount - inter.ExcludedCount;
                int es = 0;
                if (inter.FilledCount > 0)
                {
                    int start = topo.VertEdgeOff[v], end = topo.VertEdgeOff[v + 1];
                    for (int k = start; k < end; k++)
                    {
                        Edge ie = edges[topo.VertEdgeIdx[k]];
                        if (ie.State == EdgeState.Filled)
                        {
                            es = ie.EdgeSet;
                            break;
                        }
                    }
                }
                vertEdgeSetScratch[v] = es;

                int o = v * VertFeatDim;
                vertFeats[o + 0] = deg / 6.0f;
                vertFeats[o + 1] = inter.FilledCount == 0 ? 1f : 0f;
                vertFeats[o + 2] = inter.FilledCount == 1 ? 1f : 0f;
                vertFeats[o + 3] = inter.FilledCount == 2 ? 1f : 0f;
                vertFeats[o + 4] = deg > 0 ? (float)rem / deg : 0f;
                vertFeats[o + 5] = rem == 1 ? 1f : 0f;
                vertFeats[o + 6] = rem == 2 ? 1f : 0f;
                vertFeats[o + 7] = es != 0 ? 1f : 0f;
            }

            // Cell features
            bool useCellColoring = mesh.UseCellColoring;
            for (int c = 0; c < C; c++)
            {
                Cell cell = cells[c];
                int deg = cell.Edges.Count;
                int rem = deg - cell.FilledCount - cell.ExcludedCount;
                bool hasClue = cell.TargetCount >= 0;
                int need = hasClue ? cell.TargetCount - cell.FilledCount : -1;
                int slack = hasClue ? rem - need : -1;
                int col = useCellColoring ? cell.Color : 0;
                cellColors[c] = col;

                int adjColoredEmpty = 0;
                int emptyBoundary = 0;
                int cStart = topo.CellEdgeOff[c], cEnd = topo.CellEdgeOff[c + 1];
                for (int k = cStart; k < cEnd; k++)
                {
                    int eIdx = topo.CellEdgeIdx[k];
                    if (edges[eIdx].State == EdgeState.Empty)
                    {
                        emptyBoundary++;
                        int oc = topo.EdgeC0[eIdx] == c ? topo.EdgeC1[eIdx] : topo.EdgeC0[eIdx];
                        if (oc == -1 || (useCellColoring && cells[oc].Color != 0))
                            adjColoredEmpty++;
                    }
                }

                int o = c * CellFeatDim;
                cellFeats[o + 0] = hasClue ? 1f : 0f;
                cellFeats[o + 1] = deg / 8.0f;
                cellFeats[o + 2] = (hasClue && deg > 0) ? (float)cell.TargetCount / deg : 0f;
                cellFeats[o + 3] = deg > 0 ? (float)cell.FilledCount / deg : 0f;
                cellFeats[o + 4] = deg > 0 ? (float)cell.ExcludedCount / deg : 0f;
                cellFeats[o + 5] = deg > 0 ? (float)rem / deg : 0f;
                cellFeats[o + 6] = (hasClue && need == 0) ? 1f : 0f;
                cellFeats[o + 7] = (hasClue && need == 1) ? 1f : 0f;
                cellFeats[o + 8] = (hasClue && slack == 0) ? 1f : 0f;
                cellFeats[o + 9] = (hasClue && slack == 1) ? 1f : 0f;
                cellFeats[o + 10] = Math.Abs(col) == 1 ? 1f : 0f;
                cellFeats[o + 11] = Math.Abs(col) > 1 ? 1f : 0f;
                int cellColorIdx = Math.Abs(col) - 1;
                cellFeats[o + 12] = (cellColorIdx >= 0 && cellColorIdx < cellColorSets.Count)
                    ? FastLog2Div5(cellColorSets[cellColorIdx].Count)
                    : 0f;
                cellFeats[o + 13] = emptyBoundary > 0 ? (float)adjColoredEmpty / emptyBoundary : 0f;
            }

            // Edge features (including O(E) sparse local projection of edgePairRestrictions & colors)
            bool useRestricts = mesh.UseEdgeRestricts;
            bool useColoring = mesh.UseColoring;
            for (int e = 0; e < E; e++)
            {
                Edge edge = edges[e];
                int col = useColoring ? edge.Color : 0;
                edgeColors[e] = col;

                int sameCol = 0, oppCol = 0, notBoth = 0, notNeither = 0;
                int nStart = topo.EdgeNbrOff[e], nEnd = topo.EdgeNbrOff[e + 1];
                int nCnt = nEnd - nStart;
                if (useColoring || useRestricts)
                {
                    for (int k = nStart; k < nEnd; k++)
                    {
                        int oe = topo.EdgeNbrIdx[k];
                        if (useColoring && col != 0)
                        {
                            int ocol = edges[oe].Color;
                            if (ocol == col) sameCol++;
                            else if (ocol == -col) oppCol++;
                        }
                        if (useRestricts)
                        {
                            EdgePairRestriction r = mesh.GetEdgePairRestriction(e, oe);
                            if (r == EdgePairRestriction.NotBoth) notBoth++;
                            else if (r == EdgePairRestriction.NotNeither) notNeither++;
                        }
                    }
                }

                int es0 = vertEdgeSetScratch[topo.EdgeV0[e]];
                int es1 = vertEdgeSetScratch[topo.EdgeV1[e]];
                int c0 = topo.EdgeC0[e];
                int c1 = topo.EdgeC1[e];
                int c0Col = c0 >= 0 ? (useCellColoring ? cells[c0].Color : 0) : 1;
                int c1Col = c1 >= 0 ? (useCellColoring ? cells[c1].Color : 0) : 1;

                bool cellsSame = c0Col != 0 && c0Col == c1Col;
                bool cellsOpp = c0Col != 0 && c0Col == -c1Col;
                bool cellsDiffNonZero = c0Col != 0 && c1Col != 0 && Math.Abs(c0Col) != Math.Abs(c1Col);

                int o = e * EdgeFeatDim;
                edgeFeats[o + 0] = edge.State == EdgeState.Empty ? 1f : 0f;
                edgeFeats[o + 1] = edge.State == EdgeState.Filled ? 1f : 0f;
                edgeFeats[o + 2] = edge.State == EdgeState.Excluded ? 1f : 0f;
                edgeFeats[o + 3] = c1 < 0 ? 1f : 0f;
                edgeFeats[o + 4] = col != 0 ? 1f : 0f;
                int edgeColorIdx = Math.Abs(col) - 1;
                edgeFeats[o + 5] = (edgeColorIdx >= 0 && edgeColorIdx < colorSets.Count)
                    ? FastLog2Div5(colorSets[edgeColorIdx].Count)
                    : 0f;
                edgeFeats[o + 6] = nCnt > 0 ? (float)sameCol / nCnt : 0f;
                edgeFeats[o + 7] = nCnt > 0 ? (float)oppCol / nCnt : 0f;
                edgeFeats[o + 8] = MathF.Min(notBoth, 6) / 6.0f;
                edgeFeats[o + 9] = MathF.Min(notNeither, 6) / 6.0f;
                edgeFeats[o + 10] = edge.EdgeSet != 0 ? 1f : 0f;
                int edgeSetIdx = edge.EdgeSet - 1;
                edgeFeats[o + 11] = (edgeSetIdx >= 0 && edgeSetIdx < edgeSets.Count)
                    ? FastLog2Div6(edgeSets[edgeSetIdx].Count)
                    : 0f;
                edgeFeats[o + 12] = (es0 != 0 && es0 == es1) ? 1f : 0f;
                edgeFeats[o + 13] = (es0 != 0 && es1 != 0 && es0 != es1) ? 1f : 0f;
                edgeFeats[o + 14] = ((es0 != 0) ^ (es1 != 0)) ? 1f : 0f;
                edgeFeats[o + 15] = cellsSame ? 1f : 0f;
                edgeFeats[o + 16] = cellsOpp ? 1f : 0f;
                edgeFeats[o + 17] = cellsDiffNonZero ? 1f : 0f;
            }
        }
    }
}
