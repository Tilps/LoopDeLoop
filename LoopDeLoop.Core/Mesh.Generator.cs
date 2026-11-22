using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace LoopDeLoop
{
    public partial class Mesh
    {
        private const int MaxGenerateAttempts = 25;

        public double GenerateLengthFraction { get; set; } = 0.5;

        public double GenerateBoringFraction { get; set; } = 0.01;

        private bool GenerateInitialLoop(Random rnd, List<IAction> backup)
        {
            if (intersections.Count == 0 || edges.Count == 0)
                return false;

            int targetCount = (int)Math.Floor(intersections.Count * GenerateLengthFraction);
            long countSum = 0;
            int tries = 0;
            int loopTries = 0;
            bool loopTooSmall = true;
            while (loopTooSmall)
            {
                loopTooSmall = false;
                FullClear();
                backup.Clear();
                int start = rnd.Next(intersections.Count);
                Intersection inters = intersections[start];
                if (inters.Edges.Count == 0)
                    return false;
                int edgeIntersIndex = rnd.Next(inters.Edges.Count);
                int edgeIndex = inters.Edges[edgeIntersIndex];
                Perform(edgeIndex, EdgeState.Filled, backup, 0);
                bool success = CreateLoop(rnd, start, start, edgeIntersIndex);
                if (!success)
                {
                    loopTooSmall = true;
                    loopTries++;
                    if (loopTries > 1000)
                        return false;
                    continue;
                }
                int count = 0;
                for (var index = 0; index < edges.Count; index++)
                {
                    Edge edge = edges[index];
                    if (edge.State == EdgeState.Filled)
                        count++;
                }
                countSum += count;
                tries++;
                if (count < targetCount || RateBoringness() > GenerateBoringFraction)
                {
                    if (loopTries > 100 && count >= countSum / tries)
                    {
                        if (TryExpandLoop(rnd, targetCount - count))
                            return true;
                    }
                    loopTooSmall = true;
                    loopTries++;
                    if (loopTries > 1000)
                        return true;
                }
            }
            return true;
        }

        private async Task<bool> GenerateInitialLoopAsync(Random rnd, List<IAction> backup, CancellationToken cancellationToken = default)
        {
            if (intersections.Count == 0 || edges.Count == 0)
                return false;

            int targetCount = (int)Math.Floor(intersections.Count * GenerateLengthFraction);
            long countSum = 0;
            int tries = 0;
            int loopTries = 0;
            bool loopTooSmall = true;
            long lastYieldTicks = Stopwatch.GetTimestamp();

            while (loopTooSmall)
            {
                if (AbortPrune || cancellationToken.IsCancellationRequested)
                    return false;

                if (Stopwatch.GetElapsedTime(lastYieldTicks).TotalMilliseconds >= 16.0)
                {
                    lastYieldTicks = Stopwatch.GetTimestamp();
                    await Task.Delay(1, cancellationToken);
                    if (AbortPrune || cancellationToken.IsCancellationRequested)
                        return false;
                }

                loopTooSmall = false;
                FullClear();
                backup.Clear();
                int start = rnd.Next(intersections.Count);
                Intersection inters = intersections[start];
                if (inters.Edges.Count == 0)
                    return false;
                int edgeIntersIndex = rnd.Next(inters.Edges.Count);
                int edgeIndex = inters.Edges[edgeIntersIndex];
                Perform(edgeIndex, EdgeState.Filled, backup, 0);
                bool success = CreateLoop(rnd, start, start, edgeIntersIndex);
                if (!success)
                {
                    loopTooSmall = true;
                    loopTries++;
                    if (loopTries > 1000)
                        return false;
                    continue;
                }
                int count = 0;
                for (var index = 0; index < edges.Count; index++)
                {
                    Edge edge = edges[index];
                    if (edge.State == EdgeState.Filled)
                        count++;
                }
                countSum += count;
                tries++;
                if (count < targetCount || RateBoringness() > GenerateBoringFraction)
                {
                    if (loopTries > 100 && count >= countSum / tries)
                    {
                        if (TryExpandLoop(rnd, targetCount - count))
                            return true;
                    }
                    loopTooSmall = true;
                    loopTries++;
                    if (loopTries > 1000)
                        return true;
                }
            }
            return true;
        }

        public void Generate() => Generate(null);

        public void Generate(Random? rng)
        {
            Random rnd = rng ?? Random.Shared;
            AbortPrune = false;
            List<IAction> backup = [];
            for (int attempt = 0; attempt < MaxGenerateAttempts && !AbortPrune; attempt++)
            {
                if (!GenerateInitialLoop(rnd, backup))
                    continue;
                UpdateCounts();
                List<int> cellsOfVariance = new List<int>();
                List<int> cellsOfDoubleVariance = new List<int>();
                CalculateCellsOfVariance(cellsOfVariance, cellsOfDoubleVariance);
                Clear();
                if (PruneCounts(cellsOfVariance, cellsOfDoubleVariance, rnd))
                    return;
            }
        }

        public async Task GenerateAsync(IProgress<int>? progress = null, CancellationToken cancellationToken = default, Random? rng = null)
        {
            Random rnd = rng ?? Random.Shared;
            AbortPrune = false;
            List<IAction> backup = [];
            for (int attempt = 0; attempt < MaxGenerateAttempts && !cancellationToken.IsCancellationRequested && !AbortPrune; attempt++)
            {
                if (!await GenerateInitialLoopAsync(rnd, backup, cancellationToken))
                {
                    if (cancellationToken.IsCancellationRequested || AbortPrune) break;
                    continue;
                }
                if (cancellationToken.IsCancellationRequested || AbortPrune) break;
                UpdateCounts();
                List<int> cellsOfVariance = new List<int>();
                List<int> cellsOfDoubleVariance = new List<int>();
                CalculateCellsOfVariance(cellsOfVariance, cellsOfDoubleVariance);
                Clear();
                try
                {
                    if (await PruneCountsAsync(cellsOfVariance, cellsOfDoubleVariance, progress, cancellationToken, rnd))
                        break;
                    progress?.Report(0);
                    await Task.Delay(1, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            if (!cancellationToken.IsCancellationRequested && !AbortPrune)
            {
                progress?.Report(cells.Count);
            }
        }

        bool[]? boringEdges;

        private double RateBoringness()
        {
            if (boringEdges == null)
                boringEdges = new bool[edges.Count];
            else
                Array.Clear(boringEdges, 0, boringEdges.Length);
            int boringCount = 0;
            int total = edges.Count;
            for (int index = 0; index < edges.Count; index++)
            {
                Edge e = edges[index];
                if (e.State != EdgeState.Empty)
                    continue;
                bool found = false;
                for (var i = 0; i < e.Intersections.Length; i++)
                {
                    int interIndex = e.Intersections[i];
                    Intersection inter = intersections[interIndex];
                    for (var index1 = 0; index1 < inter.Edges.Count; index1++)
                    {
                        int edgeIndex = inter.Edges[index1];
                        if (edges[edgeIndex].State != EdgeState.Empty)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (found)
                        break;
                }
                if (!found)
                {
                    boringCount++;
                    boringEdges[index] = true;
                }
            }
            return (double)boringCount / (double)total;
        }

        private bool TryExpandLoop(Random rnd, int targetAdditional)
        {
            int lastTarget = int.MinValue;
            while (lastTarget != targetAdditional)
            {
                lastTarget = targetAdditional;
                List<int> cellsToExpand = new List<int>();
                for (int tries = 0; tries < 2; tries++)
                {
                    for (int i = 0; i < cells.Count; i++)
                    {
                        if (IsExpandable(cells[i]))
                        {
                            if (targetAdditional > 0 || tries != 0 || NearBoringEdge(cells[i]))
                                cellsToExpand.Add(i);
                        }
                    }
                    if (cellsToExpand.Count > 0 || targetAdditional > 0)
                        break;
                }
                rnd.Shuffle(CollectionsMarshal.AsSpan(cellsToExpand));
                int index = 0;
                while ((targetAdditional > 0 || RateBoringness() > GenerateBoringFraction) && index < cellsToExpand.Count)
                {
                    targetAdditional -= Expand(cells[cellsToExpand[index]]);
                    if (rnd.Next(2) == 0)
                        break;
                    index++;
                }
                if (targetAdditional <= 0 && RateBoringness() <= GenerateBoringFraction)
                    return true;
            }
            return false;
        }

        private bool NearBoringEdge(Cell cell)
        {
            if (boringEdges == null)
                RateBoringness();
            for (var index = 0; index < cell.Intersections.Count; index++)
            {
                int interIndex = cell.Intersections[index];
                Intersection inter = intersections[interIndex];
                for (var i = 0; i < inter.Edges.Count; i++)
                {
                    int edge = inter.Edges[i];
                    if (boringEdges![edge])
                        return true;
                }
            }
            return false;
        }

        private bool IsExpandable(Cell cell)
        {
            if (cell.FilledCount == 0)
                return false;
            if (cell.FilledCount >= (cell.Edges.Count + 1) / 2)
                return false;
            if (GlancingTouch(cell))
                return false;
            if (!ContiguousFilledEdge(cell))
                return false;
            return true;
        }

        private bool GlancingTouch(Cell cell)
        {
            for (var index = 0; index < cell.Intersections.Count; index++)
            {
                int interIndex = cell.Intersections[index];
                Intersection inter = intersections[interIndex];
                if (inter.FilledCount != 2)
                    continue;
                bool found = false;
                for (var i = 0; i < inter.Edges.Count; i++)
                {
                    int edgeIndex = inter.Edges[i];
                    Edge edge = edges[edgeIndex];
                    if (edge.State == EdgeState.Filled)
                    {
                        for (var index1 = 0; index1 < edge.Cells.Count; index1++)
                        {
                            int cellIndex = edge.Cells[index1];
                            if (cells[cellIndex] == cell)
                                found = true;
                        }
                    }
                }
                if (!found)
                    return true;
            }
            return false;
        }

        private bool ContiguousFilledEdge(Cell cell)
        {
            if (cell.FilledCount < 2)
                return true;
            // TODO: detect actual continuity of filled edges.
            return false;
        }

        private int Expand(Cell cell)
        {
            if (!IsExpandable(cell))
                return 0;
            int oldCount = cell.FilledCount;
            for (var index = 0; index < cell.Edges.Count; index++)
            {
                int edge = cell.Edges[index];
                if (edges[edge].State == EdgeState.Filled)
                {
                    Perform(new UnsetAction(this, edge), new List<IAction>(), 0, null);
                }
                else
                {
                    if (edges[edge].State == EdgeState.Excluded)
                    {
                        Perform(new UnsetAction(this, edge), new List<IAction>(), 0, null);
                    }
                    Perform(edge, EdgeState.Filled, new List<IAction>(), 0);
                    Edge e = edges[edge];
                    for (var i = 0; i < e.Intersections.Length; i++)
                    {
                        int interIndex = e.Intersections[i];
                        Intersection inter = intersections[interIndex];
                        if (inter.FilledCount == 2)
                        {
                            for (var index1 = 0; index1 < inter.Edges.Count; index1++)
                            {
                                int edgeIndex = inter.Edges[index1];
                                Edge otherEdge = edges[edgeIndex];
                                if (otherEdge.State == EdgeState.Empty)
                                {
                                    Perform(edgeIndex, EdgeState.Excluded, new List<IAction>(), 0);
                                }
                            }
                        }
                    }
                }
            }
            return cell.FilledCount - oldCount;
        }

        private void CalculateCellsOfVariance(List<int> cellsOfVariance, List<int> cellsOfDoubleVariance)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                Cell cell = cells[i];
                if (cell.TargetCount < 1)
                    continue;
                bool dbl = false;
                if (cell.FilledCount == cell.Edges.Count / 2)
                    dbl = true;
                int start = -1;
                int end = -1;
                for (int j = 0; j < cell.Intersections.Count; j++)
                {
                    int next = (j + 1) % cell.Intersections.Count;
                    int edge = GetEdgeJoining(cell.Intersections[j], cell.Intersections[next]);
                    EdgeState state = edges[edge].State;
                    if (state == EdgeState.Filled)
                    {
                        if (start == -1)
                        {
                            start = j;
                        }
                        else if (end != -1 && start == 0)
                        {
                            start = j;
                        }
                    }
                    else
                    {
                        if (start != -1 && (end == -1 || start > end))
                        {
                            end = j;
                        }
                    }
                }
                int distFound = 0;
                if (start < end)
                    distFound = end - start;
                else
                    distFound = cell.Edges.Count - (start - end);
                if (distFound == cell.TargetCount)
                {
                    if (dbl)
                        cellsOfDoubleVariance.Add(i);
                    else
                        cellsOfVariance.Add(i);
                }
            }
        }

        private bool CreateLoop(Random rnd, int start, int prev, int prevIntersEdge)
        {
            Intersection prevInters = intersections[prev];
            Edge prevEdge = edges[prevInters.Edges[prevIntersEdge]];
            Intersection? curInters = null;
            int curIntersIndex = -1;
            for (var index = 0; index < prevEdge.Intersections.Length; index++)
            {
                int intersPos = prevEdge.Intersections[index];
                Intersection other = intersections[intersPos];
                if (other != prevInters)
                {
                    curIntersIndex = intersPos;
                    curInters = other;
                    break;
                }
            }
            if (curInters == null)
                return false;
            if (curIntersIndex == start)
                return true;
            if (!CanReach(start, curIntersIndex))
                return false;
            List<int> availEdges = new List<int>();
            for (int i = 0; i < curInters.Edges.Count; i++)
            {
                Edge edge = edges[curInters.Edges[i]];
                if (edge.State == EdgeState.Empty)
                {
                    availEdges.Add(i);
                }
            }
            if (availEdges.Count == 0)
                return false;
            if (availEdges.Count > 1)
            {
                for (int i = 0; i < 2 * availEdges.Count; i++)
                {
                    int a = rnd.Next(availEdges.Count);
                    int b = rnd.Next(availEdges.Count);
                    if (a == b)
                        continue;
                    int tmp = availEdges[a];
                    availEdges[a] = availEdges[b];
                    availEdges[b] = tmp;
                }
            }
            List<IAction> backup = new List<IAction>();
            for (var index = 0; index < availEdges.Count; index++)
            {
                int availEdge = availEdges[index];
                backup.Clear();
                int edgeIndex = curInters.Edges[availEdge];
                Perform(edgeIndex, EdgeState.Filled, backup, 0);

                for (int i = 0; i < curInters.Edges.Count; i++)
                {
                    int otherEdgeIndex = curInters.Edges[i];
                    Edge edge = edges[otherEdgeIndex];
                    if (edge.State == EdgeState.Empty)
                        Perform(otherEdgeIndex, EdgeState.Excluded, backup, 0);
                }
                if (!CreateLoop(rnd, start, curIntersIndex, availEdge))
                {
                    Unperform(backup);
                }
                else
                    return true;
            }
            return false;

        }

        private bool CanReach(int start, int curIntersIndex)
        {
            bool[] reached = new bool[intersections.Count];
            List<int> newReached = new List<int>();
            reached[curIntersIndex] = true;
            newReached.Add(curIntersIndex);
            for (int i = 0; i < newReached.Count; i++)
            {
                int index = newReached[i];
                Intersection inters = intersections[index];
                for (var index1 = 0; index1 < inters.Edges.Count; index1++)
                {
                    int edgeIndex = inters.Edges[index1];
                    Edge edge = edges[edgeIndex];
                    if (edge.State == EdgeState.Empty)
                    {
                        for (var i1 = 0; i1 < edge.Intersections.Length; i1++)
                        {
                            int otherInters = edge.Intersections[i1];
                            if (otherInters != index)
                            {
                                if (!reached[otherInters])
                                {
                                    if (otherInters == start)
                                        return true;
                                    reached[otherInters] = true;
                                    newReached.Add(otherInters);
                                }
                            }
                        }
                    }
                }
            }
            return reached[start];
        }

        private void UpdateCounts()
        {
            for (var index = 0; index < cells.Count; index++)
            {
                Cell cell = cells[index];
                AddTarget(cell, cell.FilledCount);
            }
        }

        public void FullClear()
        {
            Clear();
            for (var index = 0; index < cells.Count; index++)
            {
                Cell cell = cells[index];
                RemoveTarget(cell);
            }
            successLookup.Clear();
            patternLookup.Clear();
            boringEdges = null;
            edgeDistancesCache = null;
        }
        public void Clear()
        {
            satisifiedCount = 0;
            satisifiedIntersCount = intersections.Count;
            for (var index = 0; index < edges.Count; index++)
            {
                Edge edge = edges[index];
                edge.State = EdgeState.Empty;
                edge.Color = 0;
                edge.EdgeSet = 0;
            }
            edgeSets.Clear();
            colorSets.Clear();
            for (var index = 0; index < cells.Count; index++)
            {
                Cell cell = cells[index];
                cell.ExcludedCount = 0;
                cell.FilledCount = 0;
                cell.Color = 0;
                if (cell.TargetCount == 0)
                    satisifiedCount++;
            }
            cellColorSets.Clear();
            for (var index = 0; index < intersections.Count; index++)
            {
                Intersection inters = intersections[index];
                inters.ExcludedCount = 0;
                inters.FilledCount = 0;
            }
            if (edgePairRestrictions != null && edgePairRestrictions.GetLength(0) == edges.Count && edgePairRestrictions.GetLength(1) == edges.Count)
                Array.Clear(edgePairRestrictions);
            else
                edgePairRestrictions = new EdgePairRestriction[edges.Count, edges.Count];
        }

        private int satisifiedCount;
        private int satisifiedIntersCount;
        private int numberOfNumbers;
        private List<List<int>> edgeSets = new List<List<int>>();
        private List<List<int>> colorSets = new List<List<int>>();
        private List<List<int>> cellColorSets = new List<List<int>>();
        public EdgePairRestriction[] GetEdgePairRestrictionsForEdge(int edge)
        {
            EdgePairRestriction[] restricts = new EdgePairRestriction[edges.Count];
            if (edgePairRestrictions != null)
            {
                for (int i = 0; i < edges.Count; i++)
                {
                    restricts[i] = edgePairRestrictions[edge, i];
                }
            }
            return restricts;
        }
        private EdgePairRestriction[,] edgePairRestrictions = null!;

        public void SetClue(int cellIndex, int target)
        {
            if (cellIndex >= 0 && cellIndex < cells.Count)
            {
                AddTarget(cells[cellIndex], target);
            }
        }

        internal void AddTarget(Cell cell, int target)
        {
            if (cell.TargetCount != -1)
                RemoveTarget(cell);
            if (target == -1)
                return;
            if (target <= cell.FilledCount)
                satisifiedCount++;
            numberOfNumbers++;
            cell.TargetCount = target;
        }

        private void RemoveTarget(Cell cell)
        {
            if (cell.TargetCount == -1)
                return;
            if (cell.TargetCount <= cell.FilledCount)
                satisifiedCount--;
            numberOfNumbers--;
            cell.TargetCount = -1;
        }

        public event EventHandler? PrunedCountProgress;

        private bool abortPrune;
        public bool AbortPrune
        {
            get => Volatile.Read(ref abortPrune);
            set => Volatile.Write(ref abortPrune, value);
        }

        private bool pruning = false;

        private void PreparePruneTrials(Random rnd, out bool[] tried, out int[] trials, out int width, out int height)
        {
            tried = new bool[cells.Count];
            trials = new int[cells.Count];
            for (int i = 0; i < trials.Length; i++)
                trials[i] = i;
            rnd.Shuffle(trials);

            width = 0;
            height = 0;
            if (meshType == MeshType.SquareSymmetrical && intersections.Count > 1)
            {
                int iSearch = 0;
                while (TryGetEdgeJoining(iSearch, iSearch + 1, out _))
                {
                    iSearch++;
                }
                height = iSearch;
                width = height > 0 ? Cells.Count / height : 0;
            }
        }

        private int PruneCellStep(int trial, bool[] tried, int width, int height, List<int> cellsOfVariance, List<int> cellsOfDoubleVariance, CancellationToken cancellationToken = default)
        {
            List<int> prot = CalcProtectedCells(cellsOfVariance, cellsOfDoubleVariance);
            if (prot.Contains(trial))
                return 1;

            Cell cell = cells[trial];
            if (meshType != MeshType.SquareSymmetrical || width <= 0 || height <= 0)
            {
                int oldVal = cell.TargetCount;
                RemoveTarget(cell);
                if (TrySolve(false, cancellationToken) != SolveState.Solved)
                {
                    AddTarget(cell, oldVal);
                }
                else
                {
                    finalSolution = solutionsFound[0];
                    finalDepthPatern = solutionDepthPatern[0];
                }
                return 1;
            }

            if (tried[trial])
                return 0;

            int symOldVal = cell.TargetCount;
            RemoveTarget(cell);
            int y = trial / width;
            int x = trial % width;
            int otherTrial = (height - y - 1) * width + (width - x - 1);
            if (otherTrial == trial || otherTrial < 0 || otherTrial >= cells.Count)
            {
                if (TrySolve(false, cancellationToken) != SolveState.Solved)
                {
                    AddTarget(cell, symOldVal);
                }
                else
                {
                    finalSolution = solutionsFound[0];
                    finalDepthPatern = solutionDepthPatern[0];
                }
                tried[trial] = true;
                return 1;
            }

            // While we have just removed a cell, there is no way trial and otherTrial can be adjacent
            // in square symmetrical - so we don't need to recalculate the protected list.
            if (prot.Contains(otherTrial))
            {
                AddTarget(cell, symOldVal);
                return 1;
            }

            Cell otherCell = cells[otherTrial];
            int otherOldVal = otherCell.TargetCount;
            RemoveTarget(otherCell);
            if (TrySolve(false, cancellationToken) != SolveState.Solved)
            {
                AddTarget(cell, symOldVal);
                AddTarget(otherCell, otherOldVal);
            }
            else
            {
                finalSolution = solutionsFound[0];
                finalDepthPatern = solutionDepthPatern[0];
            }
            tried[trial] = true;
            tried[otherTrial] = true;
            return 2;
        }

        private bool PruneCounts(List<int> cellsOfVariance, List<int> cellsOfDoubleVariance, Random rnd)
        {
            SolveState state = TrySolve();
            if (state != SolveState.Solved)
                return false;
            try
            {
                pruning = true;
                finalSolution = solutionsFound[0];
                finalDepthPatern = solutionDepthPatern[0];
                PreparePruneTrials(rnd, out bool[] tried, out int[] trials, out int width, out int height);

                for (int index = 0; index < trials.Length; index++)
                {
                    if (AbortPrune)
                        break;

                    int steps = PruneCellStep(trials[index], tried, width, height, cellsOfVariance, cellsOfDoubleVariance);
                    for (int s = 0; s < steps; s++)
                        PrunedCountProgress?.Invoke(this, EventArgs.Empty);
                }
                return true;
            }
            finally
            {
                pruning = false;
            }
        }

        private async Task<bool> PruneCountsAsync(List<int> cellsOfVariance, List<int> cellsOfDoubleVariance, IProgress<int>? progress = null, CancellationToken cancellationToken = default, Random? rng = null)
        {
            if (AbortPrune || cancellationToken.IsCancellationRequested)
                return true;

            await Task.Delay(1, cancellationToken);

            SolveState state = TrySolve(false, cancellationToken);
            if (AbortPrune || cancellationToken.IsCancellationRequested)
                return true;

            if (state != SolveState.Solved)
                return false;
            try
            {
                pruning = true;
                finalSolution = solutionsFound[0];
                finalDepthPatern = solutionDepthPatern[0];
                PreparePruneTrials(rng ?? Random.Shared, out bool[] tried, out int[] trials, out int width, out int height);

                int progressCounter = 0;
                long lastYieldTicks = Stopwatch.GetTimestamp();

                for (int index = 0; index < trials.Length; index++)
                {
                    if (AbortPrune || cancellationToken.IsCancellationRequested)
                        break;

                    int steps = PruneCellStep(trials[index], tried, width, height, cellsOfVariance, cellsOfDoubleVariance, cancellationToken);
                    if (steps == 0)
                        continue;

                    progressCounter += steps;
                    for (int s = 0; s < steps; s++)
                        PrunedCountProgress?.Invoke(this, EventArgs.Empty);
                    progress?.Report(Math.Min(progressCounter, cells.Count));

                    if (Stopwatch.GetElapsedTime(lastYieldTicks).TotalMilliseconds >= 16.0)
                    {
                        lastYieldTicks = Stopwatch.GetTimestamp();
                        await Task.Delay(1, cancellationToken);
                    }
                }
                return true;
            }
            finally
            {
                pruning = false;
            }
        }

        private List<int> CalcProtectedCells(List<int> cellsOfVariance, List<int> cellsOfDoubleVariance)
        {
            List<int> prot = new List<int>();
            for (int i = 0; i < cellsOfVariance.Count; i++)
            {
                int cellIndex = cellsOfVariance[i];
                CollectProtectedCellsFromCell(prot, cellIndex, true);
            }
            for (int i = 0; i < cellsOfDoubleVariance.Count; i++)
            {
                int cellIndex = cellsOfDoubleVariance[i];
                CollectProtectedCellsFromCell(prot, cellIndex, false);
            }
            return prot;
        }

        private void CollectProtectedCellsFromCell(List<int> prot, int cellIndex, bool includeSelf)
        {
            List<int> known = new List<int>();
            Cell c = cells[cellIndex];
            if (includeSelf)
            {
                if (c.TargetCount >= 0)
                    known.Add(cellIndex);
            }
            for (int j = 0; j < c.Edges.Count; j++)
            {
                int edgeIndex = c.Edges[j];
                Edge edge = edges[edgeIndex];
                for (int k = 0; k < edge.Cells.Count; k++)
                {
                    int cellIndex2 = edge.Cells[k];
                    if (cellIndex2 != cellIndex)
                    {
                        if (cells[cellIndex2].TargetCount >= 0)
                            known.Add(cellIndex2);
                    }
                }
            }
            if (known.Count == 1)
                prot.Add(known[0]);
        }

        public void SetRatingCodeOptions(string? code)
        {
            IterativeSolverDepth = int.MaxValue;
            IterativeRecMaxDepth = 1;
            ConsiderMultipleLoops = true;
            SolverMethod = SolverMethod.Iterative;
            UseIntersectCellInteractsInSolver = false;
            UseColoring = false;
            UseCellPairs = false;
            UseCellPairsTopLevel = false;
            UseEdgeRestricts = false;
            UseDerivedColoring = false;
            UseCellColoring = false;
            ColoringCheats = false;
            UseMerging = false;
            if (string.IsNullOrEmpty(code))
                return;
            int state = -1;
            string numAccumulator = string.Empty;
            for (var index = 0; index < code.Length; index++)
            {
                char c = code[index];
                if (char.IsDigit(c) || (numAccumulator.Length == 0 && c == '-'))
                {
                    numAccumulator += c;
                }
                else
                {
                    numAccumulator = HandlePotentialNumber(numAccumulator, state);
                }
                switch (c)
                {
                    case 'F':
                        SolverMethod = SolverMethod.Recursive;
                        state = 1;
                        break;
                    case 'S':
                        SolverMethod = SolverMethod.Iterative;
                        state = 2;
                        break;
                    case 'R':
                        IterativeRecMaxDepth = 2;
                        state = 3;
                        break;
                    case 'I':
                        UseIntersectCellInteractsInSolver = true;
                        state = 4;
                        break;
                    case 'C':
                        UseColoring = true;
                        state = 5;
                        break;
                    case 'O':
                        UseCellColoring = true;
                        state = 6;
                        break;
                    case 'N':
                        ConsiderMultipleLoops = false;
                        state = 7;
                        break;
                    case 'H':
                        ColoringCheats = true;
                        state = 8;
                        break;
                    case 'M':
                        UseMerging = true;
                        state = 9;
                        break;
                    case '+':
                        if (state == 5)
                            UseDerivedColoring = true;
                        else if (state == 11)
                            UseCellPairs = true;
                        break;
                    case 'E':
                        UseEdgeRestricts = true;
                        state = 10;
                        break;
                    case 'P':
                        UseCellPairsTopLevel = true;
                        state = 11;
                        break;
                }
            }
            HandlePotentialNumber(numAccumulator, state);
        }
    }
}
