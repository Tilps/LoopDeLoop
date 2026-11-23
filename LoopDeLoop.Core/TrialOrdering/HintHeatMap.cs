using System;
using System.Collections.Generic;

namespace LoopDeLoop.TrialOrdering
{
    /// <summary>
    /// Computes per-edge normalized [0, 1] heat scores for an interactive puzzle board using
    /// the embedded IncidenceGNN model combined with immediate local deduction detection.
    /// Higher heat indicates edges that are most promising to examine next.
    /// Not thread-safe: hold one instance per UI session.
    /// </summary>
    public sealed class HintHeatMapScorer
    {
        private readonly IncidenceGnnPolicy _policy;
        private float[] _heat = Array.Empty<float>();
        private bool[] _immediate = Array.Empty<bool>();

        public HintHeatMapScorer(IncidenceGnnModel? model = null)
        {
            _policy = new IncidenceGnnPolicy(model ?? IncidenceGnnModel.Default);
        }

        /// <summary>
        /// Scores every edge in <paramref name="mesh"/> and returns an array of length
        /// <c>mesh.Edges.Count</c> with normalized heat values in [0, 1].
        /// Non-empty edges always receive 0.
        /// </summary>
        public float[] ComputeEdgeHeat(Mesh mesh)
        {
            ArgumentNullException.ThrowIfNull(mesh);

            List<Edge> edges = mesh.Edges;
            int eCount = edges.Count;
            if (_heat.Length != eCount)
            {
                _heat = new float[eCount];
                _immediate = new bool[eCount];
            }
            else
            {
                Array.Clear(_heat, 0, eCount);
                Array.Clear(_immediate, 0, eCount);
            }

            if (eCount == 0)
                return _heat;

            int emptyCount = 0;
            bool anyImmediate = false;
            for (int e = 0; e < eCount; e++)
            {
                if (edges[e].State != EdgeState.Empty)
                    continue;
                emptyCount++;
                if (IsImmediateLocalDeduction(mesh, e))
                {
                    _immediate[e] = true;
                    anyImmediate = true;
                }
            }

            if (emptyCount == 0)
                return _heat;

            // Temporarily set global solver-mode features to match visible human board state
            // (no internal solver coloring/restriction tables populated) without mutating puzzle state.
            int prevDepth = mesh.IterativeSolverDepth;
            bool prevMulti = mesh.ConsiderMultipleLoops;
            bool prevColor = mesh.UseColoring;
            bool prevCellColor = mesh.UseCellColoring;
            bool prevRestricts = mesh.UseEdgeRestricts;
            bool prevMerging = mesh.UseMerging;
            bool prevCellPairs = mesh.UseCellPairs;
            bool prevCellPairsTop = mesh.UseCellPairsTopLevel;

            try
            {
                mesh.IterativeSolverDepth = 1;
                mesh.ConsiderMultipleLoops = true;
                mesh.UseColoring = false;
                mesh.UseCellColoring = false;
                mesh.UseEdgeRestricts = false;
                mesh.UseMerging = false;
                mesh.UseCellPairs = false;
                mesh.UseCellPairsTopLevel = false;

                _policy.ScoreAllNodes(mesh, iterativeRecDepth: 1);
            }
            finally
            {
                mesh.IterativeSolverDepth = prevDepth;
                mesh.ConsiderMultipleLoops = prevMulti;
                mesh.UseColoring = prevColor;
                mesh.UseCellColoring = prevCellColor;
                mesh.UseEdgeRestricts = prevRestricts;
                mesh.UseMerging = prevMerging;
                mesh.UseCellPairs = prevCellPairs;
                mesh.UseCellPairsTopLevel = prevCellPairsTop;
            }

            float[] rawScores = _policy.EdgeScores;

            // Compute mean, std, and max over all empty edges
            double sum = 0.0;
            float maxRaw = float.NegativeInfinity;
            for (int e = 0; e < eCount; e++)
            {
                if (edges[e].State != EdgeState.Empty)
                    continue;
                float s = rawScores[e];
                if (!float.IsFinite(s))
                    continue;
                sum += s;
                if (s > maxRaw)
                    maxRaw = s;
            }

            float mean = (float)(sum / emptyCount);
            double varSum = 0.0;
            for (int e = 0; e < eCount; e++)
            {
                if (edges[e].State != EdgeState.Empty)
                    continue;
                float s = rawScores[e];
                if (!float.IsFinite(s))
                    continue;
                double d = s - mean;
                varSum += d * d;
            }

            float std = MathF.Sqrt((float)(varSum / emptyCount));
            float zMax = std > 1e-4f ? (maxRaw - mean) / std : 0f;
            float spreadScale = zMax > 0f ? MathF.Min(1.0f, zMax / 1.5f) : 0f;
            float zDenom = MathF.Max(zMax, 1.5f);

            // When immediate 0-step local deductions exist, reserve the top heat band [0.90, 1.0]
            // for them while keeping strategic GNN hotspots visible in [0, 0.72].
            float gnnCap = anyImmediate ? 0.72f : 1.0f;

            for (int e = 0; e < eCount; e++)
            {
                if (edges[e].State != EdgeState.Empty)
                    continue;

                float gnnHeat = 0f;
                if (std > 1e-4f && spreadScale > 0f)
                {
                    float s = rawScores[e];
                    if (float.IsFinite(s) && s > mean)
                    {
                        float z = (s - mean) / std;
                        float t = Math.Clamp(z / zDenom, 0f, 1f);
                        gnnHeat = spreadScale * MathF.Pow(t, 1.25f);
                    }
                }

                if (_immediate[e])
                {
                    _heat[e] = 0.90f + 0.10f * gnnHeat;
                }
                else
                {
                    _heat[e] = gnnHeat * gnnCap;
                }
            }

            return _heat;
        }

        private static bool IsImmediateLocalDeduction(Mesh mesh, int edgeIndex)
        {
            Edge edge = mesh.Edges[edgeIndex];
            List<Cell> cells = mesh.Cells;
            List<Intersection> inters = mesh.Intersections;

            if (edge.Cells != null)
            {
                for (int i = 0; i < edge.Cells.Count; i++)
                {
                    int cIdx = edge.Cells[i];
                    if (cIdx < 0 || cIdx >= cells.Count)
                        continue;

                    Cell cell = cells[cIdx];
                    if (cell.TargetCount < 0)
                        continue;

                    if (cell.TargetCount == 0)
                        return true;
                    if (cell.FilledCount == cell.TargetCount)
                        return true;
                    if (cell.Edges.Count - cell.ExcludedCount == cell.TargetCount)
                        return true;
                }
            }

            if (edge.Intersections != null)
            {
                for (int i = 0; i < edge.Intersections.Length; i++)
                {
                    int vIdx = edge.Intersections[i];
                    if (vIdx < 0 || vIdx >= inters.Count)
                        continue;

                    Intersection inter = inters[vIdx];
                    int rem = inter.Edges.Count - inter.FilledCount - inter.ExcludedCount;
                    if (inter.FilledCount == 2)
                        return true;
                    if (inter.FilledCount == 1 && rem == 1)
                        return true;
                    if (inter.FilledCount == 0 && rem == 1)
                        return true;
                }
            }

            return false;
        }
    }
}
