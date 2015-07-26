using System;
using System.Collections.Generic;
using System.Text;
#if false
namespace LoopDeLoop
{
    public enum LoopLinkState
    {
        Empty,
        Filled,
        Excluded,
    }

    public class Board
    {
        public Board()
        {
            // Dodgy uglyness - we set the width only to save allocation twice - height default is already correct.
            Width = 10;
            FullClear();
        }

        public Board(Board other)
        {
            width = other.width;
            height = other.height;
            horizSet = (LoopLinkState[,])other.horizSet.Clone();
            vertSet = (LoopLinkState[,])other.vertSet.Clone();
            counts = (int[,])other.counts.Clone();
            solverMethod = other.solverMethod;
        }

        public SolverMethod SolverMethod
        {
            get
            {
                return solverMethod;
            }
            set
            {
                solverMethod = value;
            }
        }
        private SolverMethod solverMethod = SolverMethod.Iterative;

        public int Width
        {
            get
            {
                return width;
            }
            set
            {
                if (width != value)
                {
                    width = value;
                    horizSet = new LoopLinkState[width, height + 1];
                    vertSet = new LoopLinkState[width + 1, height];
                    counts = new int[width, height];
                }
            }
        }
        private int width = 0;

        public int Height
        {
            get
            {
                return height;
            }
            set
            {
                if (height != value)
                {
                    height = value;
                    horizSet = new LoopLinkState[width, height + 1];
                    vertSet = new LoopLinkState[width + 1, height];
                    counts = new int[width, height];
                }
            }
        }
        private int height = 10;

        public void Clear()
        {
            for (int i = 0; i <= width; i++)
            {
                for (int j = 0; j <= height; j++)
                {
                    if (i < width)
                        horizSet[i, j] = LoopLinkState.Empty;
                    if (j < height)
                        vertSet[i, j] = LoopLinkState.Empty;
                }
            }
        }
        public void FullClear()
        {
            for (int i = 0; i <= width; i++)
            {
                for (int j = 0; j <= height; j++)
                {
                    if (i < width)
                    {
                        horizSet[i, j] = LoopLinkState.Empty;
                        if (j < height)
                            counts[i, j] = -1;
                    }
                    if (j < height)
                        vertSet[i, j] = LoopLinkState.Empty;
                }
            }
        }

        public LoopLinkState[,] HorizSet
        {
            get
            {
                return horizSet;
            }
        }
        private LoopLinkState[,] horizSet;

        public LoopLinkState[,] VertSet
        {
            get
            {
                return vertSet;
            }
        }
        private LoopLinkState[,] vertSet;

        public int[,] Counts
        {
            get
            {
                return counts;
            }
        }
        private int[,] counts;

        public bool ValidDirection(int x, int y, Direction d)
        {
            if (x > 0 && x < width && y > 0 && y < height)
                return true;
            if (d.Dx != 0)
            {
                int offset = d.Dx > 0 ? 0 : -1;
                if (x + offset < 0 || x + offset >= width)
                    return false;
                if (y < 0 || y > height)
                    return false;
                return true;
            }
            if (d.Dy != 0)
            {
                int offset = d.Dy > 0 ? 0 : -1;
                if (y + offset < 0 || y + offset >= height)
                    return false;
                if (x < 0 || x > width)
                    return false;
                return true;
            }
            throw new ArgumentException("dx and dy cannot both be zero");
        }

        public LoopLinkState GetState(int x, int y, Direction d)
        {
            if (d.Dx == 1)
                return horizSet[x, y];
            if (d.Dy == 1)
                return vertSet[x, y];
            if (d.Dx == -1)
                return horizSet[x - 1, y];
            if (d.Dy == -1)
                return vertSet[x, y-1];
            throw new ArgumentException("dx and dy cannot both be zero");
        }
        public void SetState(int x, int y, Direction d, LoopLinkState newState)
        {
            if (d.Dx != 0)
            {
                int offset = d.Dx > 0 ? 0 : -1;
                horizSet[x + offset, y] = newState;
                return;
            }
            if (d.Dy != 0)
            {
                int offset = d.Dy > 0 ? 0 : -1;
                vertSet[x, y + offset] = newState;
                return;
            }
            throw new ArgumentException("dx and dy cannot both be zero");
        }

        internal void Generate()
        {
            bool done = false;
            while (!done)
            {
                done = true;
                bool loopToSmall = true;
                while (loopToSmall)
                {
                    loopToSmall = false;
                    FullClear();
                    Random rnd = new Random();
                    int startX = rnd.Next(width - 1) + 1;
                    int startY = rnd.Next(height - 1) + 1;
                    List<Direction> valids = new List<Direction>();
                    foreach (Direction d in Direction.All)
                    {
                        if (ValidDirection(startX, startY, d))
                            valids.Add(d);
                    }
                    int choice = rnd.Next(valids.Count);
                    Direction startD = valids[choice];
                    SetState(startX, startY, startD, LoopLinkState.Filled);
                    bool success = CreateLoop(rnd, startX, startY, startX, startY, startD);
                    int count = 0;
                    for (int i = 0; i <= width; i++)
                    {
                        for (int j = 0; j <= height; j++)
                        {
                            if (i < width)
                            {
                                if (horizSet[i, j] == LoopLinkState.Filled)
                                    count++;
                            }
                            if (j < height)
                            {
                                if (vertSet[i, j] == LoopLinkState.Filled)
                                    count++;
                            }
                        }
                    }
                    if (count < (width + height) * 2)
                        loopToSmall = true;
                }
                UpdateCounts();
                Clear();
                try
                {
                    PruneCounts();
                }
                catch (Exception e)
                {
                    if (e.Message == "Can't solve it anyway")
                        done = false;
                    else
                        throw;
                }
            }
        }

        private void PruneCounts()
        {
            Board cloneOrig = this.Clone();
            SolveState state = cloneOrig.TrySolve();
            if (state != SolveState.Solved)
                throw new Exception("Can't solve it anyway");
            finalSolution = cloneOrig.solutionsFound[0];
            finalDepthPatern = cloneOrig.solutionDepthPatern[0];
            int[] trials = new int[width * height];
            for (int i = 0; i < trials.Length; i++)
                trials[i] = i;
            Random rnd = new Random();
            for (int i = 0; i < trials.Length * 2; i++)
            {
                int a = rnd.Next(trials.Length);
                int b = rnd.Next(trials.Length);
                int tmp = trials[a];
                trials[a] = trials[b];
                trials[b] = tmp;
            }
            foreach (int trial in trials)
            {
                int x = trial % width;
                int y = trial / width;
                int oldVal = counts[x, y];
                counts[x, y] = -1;
                Board clone = this.Clone();
                if (clone.TrySolve() != SolveState.Solved)
                    counts[x, y] = oldVal;
                else
                {
                    finalSolution = clone.solutionsFound[0];
                    finalDepthPatern = clone.solutionDepthPatern[0];
                }
            }
        }

        public Board FinalSolution
        {
            get
            {
                return finalSolution;
            }
        }
        Board finalSolution;

        public int[] FinalDepthPatern
        {
            get
            {
                return finalDepthPatern;
            }
        }
        int[] finalDepthPatern;

        List<Board> solutionsFound = new List<Board>();
        public int[] DepthPatern
        {
            get
            {
                return solutionDepthPatern[0];
            }
        }
        List<int[]> solutionDepthPatern = new List<int[]>();
        List<int> curDepthPatern = new List<int>();

        public SolveState TrySolve()
        {
            solutionsFound.Clear();
            solutionDepthPatern.Clear();
            int[,] curCounts = new int[width, height];
            int[,] curExclusions = new int[width, height];
            bool failed = false;
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    if (counts[i, j] == -1)
                        continue;
                    int total = 0;
                    int ex = 0;
                    if (GetState(i, j, Direction.Right) == LoopLinkState.Filled)
                        total++;
                    if (GetState(i, j, Direction.Down) == LoopLinkState.Filled)
                        total++;
                    if (GetState(i + 1, j + 1, Direction.Left) == LoopLinkState.Filled)
                        total++;
                    if (GetState(i + 1, j + 1, Direction.Up) == LoopLinkState.Filled)
                        total++;
                    if (GetState(i, j, Direction.Right) == LoopLinkState.Excluded)
                        ex++;
                    if (GetState(i, j, Direction.Down) == LoopLinkState.Excluded)
                        ex++;
                    if (GetState(i + 1, j + 1, Direction.Left) == LoopLinkState.Excluded)
                        ex++;
                    if (GetState(i + 1, j + 1, Direction.Up) == LoopLinkState.Excluded)
                        ex++;
                    curCounts[i, j] = total;
                    curExclusions[i, j] = ex;
                    if (total > counts[i, j])
                    {
                        failed = true;
                        break;
                    }
                    if (total + 4 - ex < counts[i, j])
                    {
                        failed = true;
                        break;
                    }
                }
                if (failed)
                    break;
            }
            if (failed)
                return SolveState.NoSolutions;
            bool[, ,] done = new bool[width+1, height+1, 2];
            for (int i = 0; i <= width; i++)
            {
                for (int j = 0; j <= height; j++)
                {
                    if (i < width)
                        done[i, j, 0] = horizSet[i, j] != LoopLinkState.Empty;
                    else
                        done[i, j, 0] = true;
                    if (j < height)
                        done[i, j, 1] = vertSet[i, j] != LoopLinkState.Empty;
                    else
                        done[i, j, 1] = true;
                }
            }
            List<int> changeXs = new List<int>();
            List<int> changeYs = new List<int>();
            List<Direction> changeDs = new List<Direction>();
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    if (counts[i, j] == 0)
                    {
                        if (!done[i,j, 1])
                            PerformChange(i, j, 1, LoopLinkState.Excluded, changeXs, changeYs, changeDs, curCounts, curExclusions, done, true);
                        if (!done[i, j, 0])
                            PerformChange(i, j, 0, LoopLinkState.Excluded, changeXs, changeYs, changeDs, curCounts, curExclusions, done, true);
                        if (!done[i+1, j, 1])
                            PerformChange(i + 1, j, 1, LoopLinkState.Excluded, changeXs, changeYs, changeDs, curCounts, curExclusions, done, true);
                        if (!done[i, j+1, 0])
                            PerformChange(i, j + 1, 0, LoopLinkState.Excluded, changeXs, changeYs, changeDs, curCounts, curExclusions, done, true);
                    }
                }
            }
            curDepthPatern.Clear();
            if (solverMethod == SolverMethod.Iterative)
                return IterativeTrySolve(curCounts, curExclusions, done);
            else
                return RecursiveTrySolve2(curCounts, curExclusions, done);
        }

        public int ProgressCount()
        {
            int count = 0;
            for (int i = 0; i <= width; i++)
            {
                for (int j = 0; j <= height; j++)
                {
                    if (i < width)
                    {
                        if (horizSet[i, j] == LoopLinkState.Empty)
                            count++;                        
                    }
                    if (j < height)
                    {
                        if (vertSet[i, j] == LoopLinkState.Empty)
                            count++;
                    }
                }
            }
            return count;
        }

        private SolveState RecursiveTrySolve2(int[,] curCounts, int[,] curEx, bool[, ,] done)
        {
            int count = 0;
            for (int i = 0; i <= width; i++)
            {
                for (int j = 0; j <= height; j++)
                {
                    for (int k = 0; k < 2; k++)
                    {
                        if (done[i, j, k])
                            continue;
                        count++;
                    }
                }
            }
            curDepthPatern.Add(count);
            // TODO: add heuristic for choosing best place to switch on.
            List<Direction> changeDs = new List<Direction>();
            List<int> changeXs = new List<int>();
            List<int> changeYs = new List<int>();
            for (int i = 0; i <= width; i++)
            {
                for (int j = 0; j <= height; j++)
                {
                    for (int k = 0; k < 2; k++)
                    {
                        if (done[i, j, k])
                            continue;
                        bool filledFine = PerformChange(i, j, k, LoopLinkState.Filled, changeXs, changeYs, changeDs, curCounts, curEx, done, true);
                        if (filledFine)
                        {
                            SolveState res = RecursiveTrySolve2(curCounts, curEx, done);
                            if (res == SolveState.MultipleSolutions)
                                return res;
                            if (res != SolveState.Solved)
                            {
                                filledFine = false;
                            }
                        }
                        Unperform(changeXs, changeYs, changeDs, curCounts, curEx, done);
                        bool filledFine2 = PerformChange(i, j, k, LoopLinkState.Excluded, changeXs, changeYs, changeDs, curCounts, curEx, done, true);
                        if (filledFine2)
                        {
                            SolveState res = RecursiveTrySolve2(curCounts, curEx, done);
                            if (res == SolveState.MultipleSolutions)
                                return res;
                            if (res != SolveState.Solved)
                            {
                                filledFine2 = false;
                            }
                        }
                        Unperform(changeXs, changeYs, changeDs, curCounts, curEx, done);
                        curDepthPatern.RemoveAt(curDepthPatern.Count - 1);
                        if (filledFine && filledFine2)
                            return SolveState.MultipleSolutions;
                        else
                            if (!filledFine && !filledFine2)
                                return SolveState.NoSolutions;
                            else
                                return SolveState.Solved;
                    }
                }
            }
            // TODO: validate that solution is a single closed loop, not multiple.  For bad puzzle identification only, generation gaurantees this will pass.
            if (solutionsFound.Count > 0)
            {
                for (int i = 0; i <= width; i++)
                {
                    for (int j = 0; j <= height; j++)
                    {
                        if (i < width)
                        {
                            if (horizSet[i, j] != solutionsFound[0].horizSet[i, j])
                                return SolveState.MultipleSolutions;
                        }
                        if (j < height)
                        {
                            if (vertSet[i, j] != solutionsFound[0].vertSet[i, j])
                                return SolveState.MultipleSolutions;
                        }
                    }
                }
            }
            else
            {
                solutionsFound.Add(this.Clone());
                solutionDepthPatern.Add(curDepthPatern.ToArray());
            }
            curDepthPatern.RemoveAt(curDepthPatern.Count - 1);
            return SolveState.Solved;
        }

        private SolveState IterativeTrySolve(int[,] curCounts, int[,] curEx, bool[, ,] done)
        {
            bool changed = true;
            int count = 0;
                List<Direction> changeDsLong = new List<Direction>();
                List<int> changeXsLong = new List<int>();
                List<int> changeYsLong = new List<int>();
            while (changed)
            {
                changed = false;
                count = 0;
                for (int i = 0; i <= width; i++)
                {
                    for (int j = 0; j <= height; j++)
                    {
                        for (int k = 0; k < 2; k++)
                        {
                            if (done[i, j, k])
                                continue;
                            count++;
                        }
                    }
                }
                curDepthPatern.Add(count);
                // TODO: add heuristic for choosing best place to switch on.
                List<Direction> changeDs = new List<Direction>();
                List<int> changeXs = new List<int>();
                List<int> changeYs = new List<int>();
                for (int i = 0; i <= width; i++)
                {
                    for (int j = 0; j <= height; j++)
                    {
                        for (int k = 0; k < 2; k++)
                        {
                            if (done[i, j, k])
                                continue;
                            List<int[]> changeSet1 = null;
                            List<int[]> changeSet2 = null;
                            bool filledFine = PerformChange(i, j, k, LoopLinkState.Filled, changeXs, changeYs, changeDs, curCounts, curEx, done, true);
                            if (filledFine)
                            {
                                changeSet1 = ToChangeset(changeXs, changeYs, changeDs);
                            }
                            Unperform(changeXs, changeYs, changeDs, curCounts, curEx, done);
                            bool filledFine2 = PerformChange(i, j, k, LoopLinkState.Excluded, changeXs, changeYs, changeDs, curCounts, curEx, done, true);
                            if (filledFine2)
                            {
                                changeSet2 = ToChangeset(changeXs, changeYs, changeDs);
                            }
                            Unperform(changeXs, changeYs, changeDs, curCounts, curEx, done);
                            if (filledFine && filledFine2)
                            {
                                List<List<int[]>> changeSets = new List<List<int[]>>();
                                changeSets.Add(changeSet1);
                                changeSets.Add(changeSet2);
                                List<int[]> commonChanges = MergeChangesets(changeSets);
                                if (commonChanges.Count > 0)
                                {
                                    PerformChangeSet(commonChanges, changeXsLong, changeYsLong, changeDsLong, curCounts, curEx, done);
                                    changed = true;
                                }
                            }
                            else
                                if (!filledFine && !filledFine2)
                                    return SolveState.NoSolutions;
                                else if (filledFine)
                                {
                                    PerformChange(i, j, k, LoopLinkState.Filled, changeXsLong, changeYsLong, changeDsLong, curCounts, curEx, done, true);
                                    changed = true;
                                }
                                else
                                {
                                    PerformChange(i, j, k, LoopLinkState.Excluded, changeXsLong, changeYsLong, changeDsLong, curCounts, curEx, done, true);
                                    changed = true;
                                }
                        }
                    }
                }
            }
            if (count > 0)
                return SolveState.MultipleSolutions;
            // TODO: validate that solution is a single closed loop, not multiple.  For bad puzzle identification only, generation gaurantees this will pass.
            if (solutionsFound.Count > 0)
            {
                for (int i = 0; i <= width; i++)
                {
                    for (int j = 0; j <= height; j++)
                    {
                        if (i < width)
                        {
                            if (horizSet[i, j] != solutionsFound[0].horizSet[i, j])
                                return SolveState.MultipleSolutions;
                        }
                        if (j < height)
                        {
                            if (vertSet[i, j] != solutionsFound[0].vertSet[i, j])
                                return SolveState.MultipleSolutions;
                        }
                    }
                }
            }
            else
            {
                solutionsFound.Add(this.Clone());
                solutionDepthPatern.Add(curDepthPatern.ToArray());
            }
            Unperform(changeXsLong, changeYsLong, changeDsLong, curCounts, curEx, done);
            return SolveState.Solved;
        }

        private void PerformChangeSet(List<int[]> commonChanges, List<int> changeXsLong, List<int> changeYsLong, List<Direction> changeDsLong, int[,] curCounts, int[,] curEx, bool[, ,] done)
        {
            foreach (int[] change in commonChanges)
            {
                if (!done[change[0], change[1], change[2]])
                    PerformChange(change[0], change[1], change[2], change[3] == 1 ? LoopLinkState.Filled : LoopLinkState.Excluded, changeXsLong, changeYsLong, changeDsLong, curCounts, curEx, done, true);
            }
        }

        private List<int[]> MergeChangesets(List<List<int[]>> changeSets)
        {
            List<int[]> res = new List<int[]>();
            res.AddRange(changeSets[0]);
            for (int i = 1; i < changeSets.Count; i++)
            {
                for (int j = res.Count-1; j >= 0; j--)
                {
                    bool found = false;
                    for (int k = 0; k < changeSets[i].Count; k++)
                    {
                        bool match = true;
                        for (int a = 0; a < 4; a++)
                        {
                            if (changeSets[i][k][a] != res[j][a])
                            {
                                match = false;
                                break;
                            }
                        }
                        if (match)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        res.RemoveAt(j);
                    }
                }
            }
            return res;
        }

        private List<int[]> ToChangeset(List<int> changeXs, List<int> changeYs, List<Direction> changeDs)
        {
            List<int[]> res = new List<int[]>();
            for (int i = 0; i < changeXs.Count; i++)
            {
                int x = changeXs[i];
                int y = changeYs[i];
                Direction d = changeDs[i];
                res.Add(new int[] { x, y, d == Direction.Right ? 0 : 1, GetState(x, y, d) == LoopLinkState.Filled ? 1 : 0 });
            }
            return res;
        }

        /* Old solver, slightly faster possibly, but less useful for analysis.
                private SolveState RecursiveTrySolve(int[,] curCounts, int[,] curEx, bool[,,] done)
                {
                    int solCount = 0;
                    List<Direction> changeDs = new List<Direction>();
                    List<int> changeXs = new List<int>();
                    List<int> changeYs = new List<int>();
                    List<Direction> changeDsLong = new List<Direction>();
                    List<int> changeXsLong = new List<int>();
                    List<int> changeYsLong = new List<int>();
                    bool allDone = true;
                    for (int i = 0; i <= width; i++)
                    {
                        for (int j = 0; j <= height; j++)
                        {
                            for (int k = 0; k < 2; k++)
                            {
                                if (done[i, j, k])
                                    continue;
                                int oldCount = solCount;
                                bool permSet = false;
                                // Setup for this option.
                                bool okay = true;
                                bool filledFine = PerformChange(i, j, k, LoopLinkState.Filled, changeXs, changeYs, changeDs, curCounts, curEx, done, true);
                                if (!filledFine)
                                    okay = false;
                                if (okay)
                                {
                                    SolveState res = RecursiveTrySolve(curCounts, curEx, done);
                                    if (res == SolveState.MultipleSolutions)
                                        return res;
                                    if (res == SolveState.Solved)
                                    {
                                        solCount++;
                                        Unperform(changeXs, changeYs, changeDs, curCounts, curEx, done);
                                        bool filled = PerformChange(i, j, k, LoopLinkState.Excluded, changeXs, changeYs, changeDs, curCounts, curEx, done, true);
                                        if (filled)
                                        {
                                            SolveState failRes = RecursiveTrySolve(curCounts, curEx, done);
                                            if (res == SolveState.MultipleSolutions)
                                                return res;
                                            // This shouldn't occur, multiple solutions should be detected.
                                            if (res == SolveState.Solved)
                                                return SolveState.MultipleSolutions;
                                        }
                                        // now for evilness. we've proven there is a solution with it set, and we've proven there are no solutions with it unset.
                                        // So we can set it for the rest of this recursion level now, and avoid a stack of getting the same solution over and over.
                                        permSet = true;
                                    }
                                }
                                Unperform(changeXs, changeYs, changeDs, curCounts, curEx, done);
                                if (oldCount == solCount)
                                {
                                    bool filled = PerformChange(i, j, k, LoopLinkState.Excluded, changeXsLong, changeYsLong, changeDsLong, curCounts, curEx, done, true);
                                    if (!filled)
                                    {
                                        Unperform(changeXsLong, changeYsLong, changeDsLong, curCounts, curEx, done);
                                        return SolveState.NoSolutions;
                                    }
                                }
                                else if (permSet)
                                {
                                    bool filled = PerformChange(i, j, k, LoopLinkState.Filled, changeXsLong, changeYsLong, changeDsLong, curCounts, curEx, done, true);
                                    if (!filled)
                                        throw new InvalidOperationException("No way this could possibly have occured.  We already tested it worked.");
                                }
                                else
                                    allDone = false;
                            }
                        }
                    }
                    if (allDone)
                    {
                        // TODO: validate that solution is a single closed loop, not multiple.  For bad puzzle identification only, generation gaurantees this will pass.
                        if (solutionsFound.Count > 0)
                        {
                            for (int i = 0; i <= width; i++)
                            {
                                for (int j = 0; j <= height; j++)
                                {
                                    if (i < width)
                                    {
                                        if (horizSet[i, j] != solutionsFound[0].horizSet[i, j])
                                            return SolveState.MultipleSolutions;
                                    }
                                    if (j < height)
                                    {
                                        if (vertSet[i, j] != solutionsFound[0].vertSet[i, j])
                                            return SolveState.MultipleSolutions;
                                    }
                                }
                            }
                        }
                        else
                        {
                            solutionsFound.Add(this.Clone());
                        }
                    }
                    Unperform(changeXsLong, changeYsLong, changeDsLong, curCounts, curEx, done);
                    if (allDone || solCount > 0)
                        return SolveState.Solved;
                    else
                        return SolveState.NoSolutions;
                }
        */
        private void Unperform(List<int> changeXs, List<int> changeYs, List<Direction> changeDs, int[,] curCounts, int[,] curEx, bool[, ,] done)
        {
            for (int i = 0; i < changeXs.Count; i++)
            {
                int x = changeXs[i];
                int y = changeYs[i];
                Direction d = changeDs[i];
                int k = d == Direction.Right ? 0 : 1;
                done[x, y, k] = false;
                LoopLinkState oldState = GetState(x, y, d);
                SetState(x, y, d, LoopLinkState.Empty);
                int[,] curToUpdate = oldState == LoopLinkState.Filled ? curCounts : curEx;
                if (k == 0)
                {
                    if (y > 0)
                    {
                        curToUpdate[x, y - 1]--;
                    }
                    if (y < height)
                    {
                        curToUpdate[x, y]--;
                    }
                }
                else
                {
                    if (x > 0)
                    {
                        curToUpdate[x - 1, y]--;
                    }
                    if (x < width)
                    {
                        curToUpdate[x, y]--;
                    }
                }
            }
            changeDs.Clear();
            changeXs.Clear();
            changeYs.Clear();
        }

        // Old perform change, not really recursive, 
        /*
        private bool PerformChange(int i, int j, int k, LoopLinkState loopLinkState, List<int> changeXs, List<int> changeYs, List<Direction> changeDs, int[,] curCounts, int[,] curEx, bool[, ,] done, bool topLevel)
        {
            done[i, j, k] = true;
            changeXs.Add(i);
            changeYs.Add(j);
            Direction d = k == 0 ? Direction.Right : Direction.Down;
            changeDs.Add(d);
            SetState(i, j, d, loopLinkState);
            int[,] curToUpdate = loopLinkState == LoopLinkState.Filled ? curCounts : curEx;
            bool failed = false;
            if (k == 0)
            {
                if (j > 0)
                {
                    curToUpdate[i, j - 1]++;
                    if (!CheckCounts(curCounts, curEx, i, j - 1))
                        failed = true;
                }
                if (j < height)
                {
                    curToUpdate[i, j]++;
                    if (!CheckCounts(curCounts, curEx, i, j))
                        failed = true;
                }
            }
            else
            {
                if (i > 0)
                {
                    curToUpdate[i - 1, j]++;
                    if (!CheckCounts(curCounts, curEx, i - 1, j))
                        failed = true;
                }
                if (i < width)
                {
                    curToUpdate[i, j]++;
                    if (!CheckCounts(curCounts, curEx, i, j))
                        failed = true;
                }
            }
            if (failed)
                return false;
            if (topLevel)
            {
                bool[] avail = new bool[Direction.All.Length];
                // Perform forced moves.
                bool moveOccured = true;
                while (moveOccured)
                {
                    moveOccured = false;
                    // Check intersections
                    for (int x = 0; x <= width; x++)
                    {
                        for (int y = 0; y <= height; y++)
                        {
                            int optionsCount = 0;
                            int filledCount = 0;
                            int exCount = 0;
                            int availCount = 0;
                            for (int aIndex = 0; aIndex < avail.Length; aIndex++)
                                avail[aIndex] = false;
                            int index = 0;
                            foreach (Direction dSearch in Direction.All)
                            {
                                if (ValidDirection(x, y, dSearch))
                                {
                                    optionsCount++;
                                    switch (GetState(x, y, dSearch))
                                    {
                                        case LoopLinkState.Empty:
                                            avail[index] = true;
                                            availCount++;
                                            break;
                                        case LoopLinkState.Excluded:
                                            exCount++;
                                            break;
                                        case LoopLinkState.Filled:
                                            filledCount++;
                                            break;
                                    }
                                }
                                index++;
                            }
                            if (availCount == 0 && (filledCount == 1 || filledCount >= 3))
                                return false;
                            if (exCount == optionsCount - 1 || filledCount == 2)
                            {
                                int checkIndex = 0;
                                foreach (Direction dAvail in Direction.All)
                                {
                                    if (!avail[checkIndex])
                                    {
                                        checkIndex++;
                                        continue;
                                    }
                                    bool res = PerformChange(changeXs, changeYs, changeDs, curCounts, curEx, done, x, y, dAvail, LoopLinkState.Excluded);
                                    moveOccured = true;
                                    if (!res)
                                        return false;
                                    checkIndex++;
                                }
                            }
                            if (filledCount == 1 && availCount == 1)
                            {
                                int checkIndex = 0;
                                foreach (Direction dAvail in Direction.All)
                                {
                                    if (!avail[checkIndex])
                                    {
                                        checkIndex++;
                                        continue;
                                    }
                                    bool res = PerformChange(changeXs, changeYs, changeDs, curCounts, curEx, done, x, y, dAvail, LoopLinkState.Filled);
                                    moveOccured = true;
                                    if (!res)
                                        return false;
                                    checkIndex++;
                                }
                            }
                        }
                    }
                    for (int x = 0; x < width; x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            if (counts[x, y] < 0)
                                continue;
                            if (curCounts[x, y] == counts[x, y] && curEx[x, y] < 4 - counts[x, y])
                            {
                                if (horizSet[x, y] == LoopLinkState.Empty)
                                {
                                    bool res = PerformChange(changeXs, changeYs, changeDs, curCounts, curEx, done, x, y, Direction.Right, LoopLinkState.Excluded);
                                    moveOccured = true;
                                    if (!res)
                                        return false;
                                }
                                if (vertSet[x, y] == LoopLinkState.Empty)
                                {
                                    bool res = PerformChange(changeXs, changeYs, changeDs, curCounts, curEx, done, x, y, Direction.Down, LoopLinkState.Excluded);
                                    moveOccured = true;
                                    if (!res)
                                        return false;
                                }
                                if (horizSet[x, y + 1] == LoopLinkState.Empty)
                                {
                                    bool res = PerformChange(changeXs, changeYs, changeDs, curCounts, curEx, done, x, y + 1, Direction.Right, LoopLinkState.Excluded);
                                    moveOccured = true;
                                    if (!res)
                                        return false;
                                }
                                if (vertSet[x + 1, y] == LoopLinkState.Empty)
                                {
                                    bool res = PerformChange(changeXs, changeYs, changeDs, curCounts, curEx, done, x + 1, y, Direction.Down, LoopLinkState.Excluded);
                                    moveOccured = true;
                                    if (!res)
                                        return false;
                                }
                            }
                            if (curCounts[x, y] < counts[x, y] && curEx[x, y] == 4 - counts[x, y])
                            {
                                if (horizSet[x, y] == LoopLinkState.Empty)
                                {
                                    bool res = PerformChange(changeXs, changeYs, changeDs, curCounts, curEx, done, x, y, Direction.Right, LoopLinkState.Filled);
                                    moveOccured = true;
                                    if (!res)
                                        return false;
                                }
                                if (vertSet[x, y] == LoopLinkState.Empty)
                                {
                                    bool res = PerformChange(changeXs, changeYs, changeDs, curCounts, curEx, done, x, y, Direction.Down, LoopLinkState.Filled);
                                    moveOccured = true;
                                    if (!res)
                                        return false;
                                }
                                if (horizSet[x, y + 1] == LoopLinkState.Empty)
                                {
                                    bool res = PerformChange(changeXs, changeYs, changeDs, curCounts, curEx, done, x, y + 1, Direction.Right, LoopLinkState.Filled);
                                    moveOccured = true;
                                    if (!res)
                                        return false;
                                }
                                if (vertSet[x + 1, y] == LoopLinkState.Empty)
                                {
                                    bool res = PerformChange(changeXs, changeYs, changeDs, curCounts, curEx, done, x + 1, y, Direction.Down, LoopLinkState.Filled);
                                    moveOccured = true;
                                    if (!res)
                                        return false;
                                }
                            }
                        }
                    }
                }
            }
            return true;
        }

        private bool PerformChange(List<int> changeXs, List<int> changeYs, List<Direction> changeDs, int[,] curCounts, int[,] curEx, bool[, ,] done, int x, int y, Direction dAvail, LoopLinkState state)
        {
            int rX = x;
            int rY = y;
            Direction real;
            if (dAvail == Direction.Left)
            {
                real = dAvail.Reverse();
                rX--;
            }
            else if (dAvail == Direction.Up)
            {
                real = dAvail.Reverse();
                rY--;
            }
            else
                real = dAvail;
            int rK = real == Direction.Right ? 0 : 1;
            bool res = PerformChange(rX, rY, rK, state, changeXs, changeYs, changeDs, curCounts, curEx, done, false);
            return res;
        }*/

        // Stub to call the new perform change so I don't have to change code :P
        private bool PerformChange(int i, int j, int k, LoopLinkState loopLinkState, List<int> changeXs, List<int> changeYs, List<Direction> changeDs, int[,] curCounts, int[,] curEx, bool[, ,] done, bool topLevel)
        {
            return PerformChange2(i, j, k, loopLinkState, changeXs, changeYs, changeDs, curCounts, curEx, done, topLevel);
        }

        // New recursive perform change which should have better performance characteristics.
        private bool PerformChange2(int i, int j, int k, LoopLinkState loopLinkState, List<int> changeXs, List<int> changeYs, List<Direction> changeDs, int[,] curCounts, int[,] curEx, bool[, ,] done, bool topLevel)
        {
            if (done[i, j, k])
                throw new InvalidOperationException("Screwed up big time.");
            done[i, j, k] = true;
            changeXs.Add(i);
            changeYs.Add(j);
            Direction d = k == 0 ? Direction.Right : Direction.Down;
            changeDs.Add(d);
            SetState(i, j, d, loopLinkState);
            int[,] curToUpdate = loopLinkState == LoopLinkState.Filled ? curCounts : curEx;
            bool failed = false;
            List<int[]> cellsUpdated = new List<int[]>();
            if (k == 0)
            {
                if (j > 0)
                {
                    curToUpdate[i, j - 1]++;
                    cellsUpdated.Add(new int[] { i, j - 1 });
                    if (!CheckCounts(curCounts, curEx, i, j - 1))
                        failed = true;
                }
                if (j < height)
                {
                    curToUpdate[i, j]++;
                    cellsUpdated.Add(new int[] { i, j });
                    if (!CheckCounts(curCounts, curEx, i, j))
                        failed = true;
                }
            }
            else
            {
                if (i > 0)
                {
                    curToUpdate[i - 1, j]++;
                    cellsUpdated.Add(new int[] { i - 1, j });
                    if (!CheckCounts(curCounts, curEx, i - 1, j))
                        failed = true;
                }
                if (i < width)
                {
                    curToUpdate[i, j]++;
                    cellsUpdated.Add(new int[] { i, j });
                    if (!CheckCounts(curCounts, curEx, i, j))
                        failed = true;
                }
            }
            if (failed)
                return false;
            bool[] avail = new bool[Direction.All.Length];
            // Check intersections
            for (int endPointIndex = 0; endPointIndex < 2; endPointIndex++)
            {
                int x;
                int y;
                if (endPointIndex == 0)
                {
                    x = i;
                    y = j;
                }
                else
                {
                    if (k == 0)
                    {
                        x = i + 1;
                        y = j;
                    }
                    else
                    {
                        x = i;
                        y = j + 1;
                    }
                }
                int optionsCount = 0;
                int filledCount = 0;
                int exCount = 0;
                int availCount = 0;
                for (int aIndex = 0; aIndex < avail.Length; aIndex++)
                    avail[aIndex] = false;
                int index = 0;
                foreach (Direction dSearch in Direction.All)
                {
                    if (ValidDirection(x, y, dSearch))
                    {
                        optionsCount++;
                        switch (GetState(x, y, dSearch))
                        {
                            case LoopLinkState.Empty:
                                avail[index] = true;
                                availCount++;
                                break;
                            case LoopLinkState.Excluded:
                                exCount++;
                                break;
                            case LoopLinkState.Filled:
                                filledCount++;
                                break;
                        }
                    }
                    index++;
                }
                if (availCount == 0 && (filledCount == 1 || filledCount >= 3))
                    return false;
                if (exCount == optionsCount - 1 || filledCount == 2)
                {
                    int checkIndex = 0;
                    foreach (Direction dAvail in Direction.All)
                    {
                        if (!avail[checkIndex])
                        {
                            checkIndex++;
                            continue;
                        }
                        if (GetState(x, y, dAvail) == LoopLinkState.Empty)
                        {
                            bool res = PerformChange2(changeXs, changeYs, changeDs, curCounts, curEx, done, x, y, dAvail, LoopLinkState.Excluded);
                            if (!res)
                                return false;
                        }
                        checkIndex++;
                    }
                }
                if (filledCount == 1 && availCount == 1)
                {
                    int checkIndex = 0;
                    foreach (Direction dAvail in Direction.All)
                    {
                        if (!avail[checkIndex])
                        {
                            checkIndex++;
                            continue;
                        }
                        if (GetState(x, y, dAvail) == LoopLinkState.Empty)
                        {
                            bool res = PerformChange2(changeXs, changeYs, changeDs, curCounts, curEx, done, x, y, dAvail, LoopLinkState.Filled);
                            if (!res)
                                return false;
                        }
                        checkIndex++;
                    }
                }
            }
            foreach (int[] changeCell in cellsUpdated)
            {
                int x = changeCell[0];
                int y = changeCell[1];
                if (counts[x, y] < 0)
                    continue;
                if (curCounts[x, y] == counts[x, y] && curEx[x, y] < 4 - counts[x, y])
                {
                    if (horizSet[x, y] == LoopLinkState.Empty)
                    {
                        bool res = PerformChange2(changeXs, changeYs, changeDs, curCounts, curEx, done, x, y, Direction.Right, LoopLinkState.Excluded);
                        if (!res)
                            return false;
                    }
                    if (vertSet[x, y] == LoopLinkState.Empty)
                    {
                        bool res = PerformChange2(changeXs, changeYs, changeDs, curCounts, curEx, done, x, y, Direction.Down, LoopLinkState.Excluded);
                        if (!res)
                            return false;
                    }
                    if (horizSet[x, y + 1] == LoopLinkState.Empty)
                    {
                        bool res = PerformChange2(changeXs, changeYs, changeDs, curCounts, curEx, done, x, y + 1, Direction.Right, LoopLinkState.Excluded);
                        if (!res)
                            return false;
                    }
                    if (vertSet[x + 1, y] == LoopLinkState.Empty)
                    {
                        bool res = PerformChange2(changeXs, changeYs, changeDs, curCounts, curEx, done, x + 1, y, Direction.Down, LoopLinkState.Excluded);
                        if (!res)
                            return false;
                    }
                }
                if (curCounts[x, y] < counts[x, y] && curEx[x, y] == 4 - counts[x, y])
                {
                    if (horizSet[x, y] == LoopLinkState.Empty)
                    {
                        bool res = PerformChange2(changeXs, changeYs, changeDs, curCounts, curEx, done, x, y, Direction.Right, LoopLinkState.Filled);
                        if (!res)
                            return false;
                    }
                    if (vertSet[x, y] == LoopLinkState.Empty)
                    {
                        bool res = PerformChange2(changeXs, changeYs, changeDs, curCounts, curEx, done, x, y, Direction.Down, LoopLinkState.Filled);
                        if (!res)
                            return false;
                    }
                    if (horizSet[x, y + 1] == LoopLinkState.Empty)
                    {
                        bool res = PerformChange2(changeXs, changeYs, changeDs, curCounts, curEx, done, x, y + 1, Direction.Right, LoopLinkState.Filled);
                        if (!res)
                            return false;
                    }
                    if (vertSet[x + 1, y] == LoopLinkState.Empty)
                    {
                        bool res = PerformChange2(changeXs, changeYs, changeDs, curCounts, curEx, done, x + 1, y, Direction.Down, LoopLinkState.Filled);
                        if (!res)
                            return false;
                    }
                }
            }
            return true;
        }

        private bool PerformChange2(List<int> changeXs, List<int> changeYs, List<Direction> changeDs, int[,] curCounts, int[,] curEx, bool[, ,] done, int x, int y, Direction dAvail, LoopLinkState state)
        {
            int rX = x;
            int rY = y;
            Direction real;
            if (dAvail == Direction.Left)
            {
                real = dAvail.Reverse();
                rX--;
            }
            else if (dAvail == Direction.Up)
            {
                real = dAvail.Reverse();
                rY--;
            }
            else
                real = dAvail;
            int rK = real == Direction.Right ? 0 : 1;
            bool res = PerformChange2(rX, rY, rK, state, changeXs, changeYs, changeDs, curCounts, curEx, done, false);
            return res;
        }

        private bool CheckCounts(int[,] curCounts, int[,] curEx, int i, int j)
        {
            if (counts[i, j] < 0)
                return true;
            if (curCounts[i, j] > counts[i, j])
            {
                return false;
            }
            if (curCounts[i,j] + 4 - curEx[i,j] < counts[i, j])
            {
                return false;
            }
            return true;
        }

        private Board Clone()
        {
            return new Board(this);
        }

        private void UpdateCounts()
        {
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    int total = 0;
                    if (GetState(i, j, Direction.Right) == LoopLinkState.Filled)
                        total++;
                    if (GetState(i, j, Direction.Down) == LoopLinkState.Filled)
                        total++;
                    if (GetState(i+1, j+1, Direction.Left) == LoopLinkState.Filled)
                        total++;
                    if (GetState(i+1, j+1, Direction.Up) == LoopLinkState.Filled)
                        total++;
                    counts[i, j] = total;
                }
            }
        }

        private bool CreateLoop(Random rnd, int startX, int startY, int lastX, int lastY, Direction lastD)
        {
            int curX = lastX + lastD.Dx;
            int curY = lastY + lastD.Dy;
            if (curX == startX && curY == startY)
                return true;
            Direction reverse = lastD.Reverse();
            bool needReachTest = true;
/*            if (curX == 0 && lastD == Direction.Left || curY == 0 && lastD == Direction.Up || curX == width && lastD == Direction.Right || curY == height && lastD == Direction.Down)
            {
                needReachTest = true;
            }
            if (Math.Abs(startX - curX) + Math.Abs(startY - curY) == 1)
                needReachTest = true;
  */          List<Direction> valids = new List<Direction>();
            foreach (Direction d in Direction.All)
            {
                if (d == reverse)
                    continue;
                if (ValidDirection(curX, curY, d))
                {
                    if (GetState(curX, curY, d) == LoopLinkState.Empty)
                    {
                        if (!needReachTest || Reachable(curX, curY, d, startX, startY))
                            valids.Add(d);
                    }
                }
            }
            if (valids.Count == 0)
                return false;
            if (valids.Count > 1)
            {
                for (int i = 0; i < valids.Count * 2; i++)
                {
                    int a = rnd.Next(valids.Count);
                    int b = rnd.Next(valids.Count);
                    if (a == b)
                        continue;
                    Direction tmp = valids[a];
                    valids[a] = valids[b];
                    valids[b] = tmp;
                }
            }
            foreach (Direction d in valids)
            {
                SetState(curX, curY, d, LoopLinkState.Filled);
                List<Direction> crosses = new List<Direction>();
                foreach (Direction others in Direction.All)
                    if (others != d && others != reverse && ValidDirection(curX, curY, others) && GetState(curX, curY, others) == LoopLinkState.Empty)
                        crosses.Add(others);
                foreach (Direction cross in crosses)
                {
                    SetState(curX, curY, cross, LoopLinkState.Excluded);
                }
                if (CreateLoop(rnd, startX, startY, curX, curY, d))
                    return true;
                else
                {
                    SetState(curX, curY, d, LoopLinkState.Empty);
                    foreach (Direction cross in crosses)
                    {
                        SetState(curX, curY, cross, LoopLinkState.Empty);
                    }
                }
            }
            return false;

        }

        private bool Reachable(int curX, int curY, Direction d, int startX, int startY)
        {
            SetState(curX, curY, d, LoopLinkState.Filled);
            bool[,] reached = new bool[width + 1, height + 1];
            bool changed = true;
            reached[curX + d.Dx, curY + d.Dy] = true;
            while (changed)
            {
                changed = false;
                for (int i = 0; i <= width; i++)
                {
                    for (int j = 0; j <= height; j++)
                    {
                        if (reached[i, j])
                        {
                            foreach (Direction a in Direction.All)
                            {
                                if (ValidDirection(i, j, a))
                                {
                                    if (GetState(i, j, a) == LoopLinkState.Empty)
                                    {
                                        int nextX = i + a.Dx;
                                        int nextY = j + a.Dy;
                                        if (!reached[nextX, nextY])
                                        {
                                            changed = true;
                                            reached[nextX, nextY] = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            bool success = false;
            if (reached[startX, startY])
                success = true;
            SetState(curX, curY, d, LoopLinkState.Empty);
            return success;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            for (int y = 0; y <= height * 2; y++)
            {
                if (y % 2 == 0)
                {
                    int row = y / 2;
                    for (int x = 0; x < width; x++)
                    {
                        builder.Append("+");
                        if (horizSet[x, row] == LoopLinkState.Filled)
                            builder.Append("-");
                        else if (horizSet[x, row] == LoopLinkState.Excluded)
                            builder.Append("x");
                        else
                            builder.Append(" ");
                    }
                    builder.AppendLine("+");
                }
                else
                {
                    int row = y / 2;
                    for (int x = 0; x <= width; x++)
                    {
                        if (vertSet[x, row] == LoopLinkState.Filled)
                            builder.Append("|");
                        else if (vertSet[x, row] == LoopLinkState.Excluded)
                            builder.Append("x");
                        else
                            builder.Append(" ");
                        if (x < width)
                            if (counts[x, row] >= 0)
                                builder.Append(counts[x, row]);
                            else
                                builder.Append(" ");
                    }
                    builder.AppendLine();
                }
            }
            return builder.ToString();
        }

        internal void BecomeSolution()
        {
            Board other = solutionsFound[0];
            this.horizSet = (LoopLinkState[,])other.horizSet.Clone();
            this.vertSet = (LoopLinkState[,])other.vertSet.Clone();
        }
    }

    public struct Direction
    {
        public Direction(int dx, int dy)
        {
            Dx = dx;
            Dy = dy;
        }
        public int Dx;
        public int Dy;

        public Direction Reverse()
        {
            if (Dx != 0)
            {
                if (Dx == -1)
                    return Right;
                else
                    return Left;
            }
            if (Dy != 0)
            {
                if (Dy == -1)
                    return Down;
                else
                    return Up;
            }
            throw new InvalidOperationException("Not a valid direction to start with.");
        }

        public static bool operator ==(Direction a, Direction b)
        {
            return a.Dx == b.Dx && a.Dy == b.Dy;
        }
        public static bool operator !=(Direction a, Direction b)
        {
            return !(a==b);
        }

        public static Direction Left = new Direction(-1, 0);
        public static Direction Right = new Direction(1, 0);
        public static Direction Up = new Direction(0, -1);
        public static Direction Down = new Direction(0, 1);
        public static Direction[] All = new Direction[] { Left, Right, Up, Down };
    }
}
#endif
