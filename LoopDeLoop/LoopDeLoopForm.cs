using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using LoopDeLoop.Properties;
using System.Threading;

namespace LoopDeLoop
{
    public partial class LoopDeLoopForm : Form
    {
        public LoopDeLoopForm()
        {
            InitializeComponent();
            comboMeshType.SelectedIndex = 0;
            loopDisplay1.DisallowTriviallyFalse = !Settings.Default.AllowObviousFalseMoves;
            loopDisplay1.AutoMove = Settings.Default.AutoMove;
            loopDisplay1.UseICInAuto = Settings.Default.UseICinAuto;
            loopDisplay1.ConsiderMultipleLoopsInAuto = Settings.Default.ConsiderMultipleLoopsInAuto;
            loopDisplay1.Font = Settings.Default.BoardFont;
            loopDisplay1.ShowColors = Settings.Default.ShowColors;
            loopDisplay1.ShowCellColors = Settings.Default.ShowCellColors;
            loopDisplay1.ShowCellColorsAdvanced = Settings.Default.ShowCellColorsAdvanced;
            loopDisplay1.UseColoringInAuto = Settings.Default.UseColoringInAuto;
            loopDisplay1.UseEdgeRestrictsInAuto = Settings.Default.UseEdgeRestrictsInAuto;
            loopDisplay1.UseCellColoringInAuto = Settings.Default.UseCellColoringInAuto;
            loopDisplay1.UseCellPairsInAuto = Settings.Default.UseCellPairsInAuto;
            loopDisplay1.Solved += new EventHandler(loopDisplay1_Solved);
            loopDisplay1.CanUndoRedoMaybeChanged += new EventHandler(UndoTree_CanUndoRedoMaybeChanged);
        }

        void UndoTree_CanUndoRedoMaybeChanged(object? sender, EventArgs e)
        {
            redoToolStripMenuItem.Enabled = loopDisplay1.UndoTree.CanRedo;
            undoToolStripMenuItem.Enabled = loopDisplay1.UndoTree.CanUndo;
        }

        void loopDisplay1_Solved(object? sender, EventArgs e)
        {
            timer1.Enabled = false;
        }

        ProgressBar? bar;

        readonly Lock generatorLock = new();
        bool generating = false;
        CancellationTokenSource? generatorCts = null;
        Mesh? generatingMesh = null;
        bool stopping;

        private void button1_Click(object sender, EventArgs e)
        {
            string typeName = comboMeshType.SelectedItem?.ToString() ?? string.Empty;
            MeshType type = MeshTypeFromString(typeName);
            int width;
            int height;
            string val = textSize.Text;
            if (!ParseSize(val, type, out width, out height))
                return;
            Mesh newMesh = new Mesh(width, height, type);
            newMesh.SolverMethod = (SolverMethod)Enum.Parse(typeof(SolverMethod), Settings.Default.SolverType);
            newMesh.IterativeSolverDepth = Settings.Default.SimpleSolverDepth;
            newMesh.IterativeRecMaxDepth = Settings.Default.SimpleSolverRecurseDepth;
            newMesh.ConsiderMultipleLoops = Settings.Default.ConsiderMultipleLoops;
            newMesh.UseIntersectCellInteractsInSolver = Settings.Default.UseICinSolver;
            newMesh.GenerateLengthFraction = Settings.Default.GenerateLengthFraction;
            newMesh.GenerateBoringFraction = Settings.Default.GenerateBoringFraction;
            newMesh.UseColoring = Settings.Default.UseColoringInSolver;
            newMesh.UseCellPairs = Settings.Default.UseCellPairsInSolver;
            newMesh.UseCellPairsTopLevel = Settings.Default.UseCellPairsTopLevelInSolver;
            newMesh.UseEdgeRestricts = Settings.Default.UseEdgeRestrictsInSolver;
            newMesh.UseMerging = Settings.Default.UseMergeInSolver;
            newMesh.UseDerivedColoring = Settings.Default.UseDerivedColoringInSolver;
            newMesh.UseCellColoring = Settings.Default.UseCellColoringInSolver;
            newMesh.ColoringCheats = false;

            lock (generatorLock)
            {
                if (generating && !stopping)
                {
                    buttonNew.Text = "Abort";
                    stopping = true;
                    if (generatingMesh != null)
                        generatingMesh.AbortPrune = true;
                }
                else if (generating)
                {
                    generating = false;
                    buttonNew.Text = "New";
                    if (generatingMesh != null)
                        generatingMesh.AbortPrune = true;
                    generatorCts?.Cancel();
                }
                else
                {
                    generatingMesh = newMesh;
                    generating = true;
                    stopping = false;
                    buttonNew.Text = "Stop";
                    var cts = new CancellationTokenSource();
                    generatorCts = cts;
                    Thread generatorThread = new Thread(() => Generate(newMesh, cts));
                    generatorThread.IsBackground = true;
                    generatorThread.Start();
                }
            }
        }

        private void Generate(Mesh newMesh, CancellationTokenSource cts)
        {
            try
            {
                if (!IsDisposed && IsHandleCreated)
                {
                    try { this.Invoke(new ParameterizedThreadStart(ShowBar), newMesh.Cells.Count); }
                    catch (ObjectDisposedException) { }
                    catch (InvalidOperationException) { }
                }

                newMesh.PrunedCountProgress += new EventHandler(Mesh_PrunedCountProgress);
                try
                {
                    newMesh.GenerateAsync(null, cts.Token).GetAwaiter().GetResult();
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                finally
                {
                    newMesh.PrunedCountProgress -= new EventHandler(Mesh_PrunedCountProgress);
                }

                if (cts.IsCancellationRequested)
                    return;

                if (Settings.Default.AutoStart)
                {
                    AutoStart(newMesh);
                }
                if (!IsDisposed && IsHandleCreated)
                {
                    try
                    {
                        this.Invoke(new ParameterizedThreadStart(UpdateMesh), newMesh);
                    }
                    catch (ObjectDisposedException) { }
                    catch (InvalidOperationException) { }
                }
            }
            catch (Exception e)
            {
                if (!IsDisposed && IsHandleCreated)
                {
                    try
                    {
                        this.BeginInvoke(new ParameterizedThreadStart(UnexpectedExceptionMessage), e);
                    }
                    catch (ObjectDisposedException) { }
                    catch (InvalidOperationException) { }
                }
            }
            finally
            {
                bool shouldResetButton = false;
                lock (generatorLock)
                {
                    if (ReferenceEquals(generatorCts, cts))
                    {
                        generating = false;
                        generatingMesh = null;
                        generatorCts = null;
                        shouldResetButton = true;
                    }
                }
                cts.Dispose();

                if (!IsDisposed && IsHandleCreated)
                {
                    try
                    {
                        if (shouldResetButton)
                            this.BeginInvoke(new MethodInvoker(ResetGeneratorButton));
                        this.BeginInvoke(new MethodInvoker(HideBar));
                    }
                    catch (ObjectDisposedException) { }
                    catch (InvalidOperationException) { }
                }
            }
        }

        private static void AutoStart(Mesh newMesh)
        {
            newMesh.ColoringCheats = false;
            newMesh.UseColoring = Settings.Default.UseColoringInAuto;
            newMesh.UseCellPairs = Settings.Default.UseCellPairsInAuto;
            newMesh.UseCellPairsTopLevel = false;
            newMesh.UseEdgeRestricts = Settings.Default.UseEdgeRestrictsInAuto;
            newMesh.ConsiderIntersectCellInteractsAsSimple = Settings.Default.UseICinAuto;
            newMesh.ConsiderMultipleLoops = Settings.Default.ConsiderMultipleLoopsInAuto;
            newMesh.UseCellColoring = Settings.Default.UseCellColoringInAuto;
            // TODO: add 'in auto' settings.
            newMesh.UseMerging = false;
            newMesh.UseDerivedColoring = false;
            List<IAction> backup = new List<IAction>();
            newMesh.PerformStart(backup);
        }

        private void ResetGeneratorButton()
        {
            this.buttonNew.Text = "New";
        }

        private void UpdateMesh(object? meshObj)
        {
            Mesh newMesh = (Mesh)meshObj!;
            loopDisplay1.Mesh = newMesh;
            loopDisplay1.Refresh();
            if (Settings.Default.ShowClock)
                label2.Text = "Time: 00:00:00";
            this.secs = 0;
            timer1.Enabled = false;
            this.timer1.Enabled = true;
            Rate();
        }

        private void HideBar()
        {
            if (bar != null)
            {
                labelDepthPatern.Parent?.Controls.Remove(bar);
                bar.Dispose();
                bar = null;
            }
        }

        private void ShowBar(object? countObj)
        {
            int count = (int)countObj!;
            bar = new ProgressBar();
            bar.Top = labelDepthPatern.Top;
            bar.Left = labelDepthPatern.Left;
            bar.Width = this.ClientRectangle.Width - bar.Left - bar.Left;
            bar.Height = labelDepthPatern.Height;
            bar.Step = 1;
            bar.Maximum = count;
            bar.Minimum = 0;
            labelDepthPatern.Parent?.Controls.Add(bar);
            bar.BringToFront();
        }

        void Mesh_PrunedCountProgress(object? sender, EventArgs e)
        {
            if (this.InvokeRequired)
            {
                try
                {
                    this.BeginInvoke(new EventHandler(Mesh_PrunedCountProgress));
                }
                catch (InvalidOperationException)
                {
                    // Ignore shutdown errors.
                }
                return;
            }
            bar?.PerformStep();
            bar?.Refresh();
        }

        public static bool ParseSize(string val, MeshType type, out int width, out int height)
        {
            return PuzzleHelper.ParseSize(val, type, out width, out height);
        }

        public static MeshType MeshTypeFromString(string typeName)
        {
            return PuzzleHelper.MeshTypeFromString(typeName);
        }

        internal static string StringFromMeshType(MeshType value)
        {
            switch (value)
            {
                case MeshType.Square:
                    return "Square";
                case MeshType.SquareSymmetrical:
                    return "Square Symmetrical";
                case MeshType.Triangle:
                    return "Triangle";
                case MeshType.Hexagonal:
                    return "Hexagon";
                case MeshType.Hexagonal2:
                    return "Hexagon2";
                case MeshType.Hexagonal3:
                    return "Hexagon3";
                case MeshType.Octagon:
                    return "Octagon";
                case MeshType.Square2:
                    return "Square2";
                case MeshType.Pentagon:
                    return "Pentagon";
                case MeshType.Kites:
                    return "Kites";
                case MeshType.AsymmetricPentagons:
                    return "Asymmetric Pentagons";
                case MeshType.Diamonds:
                    return "Diamonds";
                case MeshType.DiamondSquare:
                    return "Diamond-Square";
                case MeshType.PentagonHexagon:
                    return "Pentagon-Hexagon";
                case MeshType.FloretPentagons:
                    return "Floret Pentagons";
                case MeshType.CairoPentagons:
                    return "Cairo Pentagons";
                case MeshType.Hexagonal4:
                    return "Hexagon 4";
                case MeshType.Square3:
                    return "Square 3";
                case MeshType.PentagonHexagon2:
                    return "Pentagon-Hexagon 2";
            }
            return "Square";

        }

        readonly Lock ratingThreadLock = new();
        CancellationTokenSource? ratingCts = null;

        private void Rate()
        {
            Mesh toRate = new Mesh(loopDisplay1.Mesh);
            CancellationTokenSource cts = new();
            lock (ratingThreadLock)
            {
                ratingCts?.Cancel();
                ratingCts = cts;
                labelDepthPatern.Text = "Rating...";
                Thread ratingThread = new Thread(() => RatingThreadProc(toRate, cts));
                ratingThread.IsBackground = true;
                ratingThread.Priority = ThreadPriority.Lowest;
                ratingThread.Start();
            }
        }

        internal static void UnexpectedExceptionMessage(object? o)
        {
            if (o is Exception e)
                MessageBox.Show("An unexpected error has occured. Error details: " + e.ToString());
        }

        private void SafeSetLabel(string text, CancellationToken token)
        {
            if (token.IsCancellationRequested || IsDisposed || !IsHandleCreated)
                return;
            try
            {
                this.Invoke(new ParameterizedThreadStart(SetLabel), text);
            }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
        }

        private void RatingThreadProc(Mesh sourceMesh, CancellationTokenSource cts)
        {
            CancellationToken token = cts.Token;
            try
            {
                Mesh toRate = PuzzleHelper.CreateFullSolverMesh(sourceMesh);
                toRate.Clear();
                string rating = string.Empty;
                SolveState solveState = toRate.TrySolve(false, token);
                if (token.IsCancellationRequested)
                    return;

                if (solveState != SolveState.Solved)
                {
                    if (solveState == SolveState.MultipleSolutions)
                    {
                        rating = "Multiple Solutions.";
                    }
                    else
                        rating = "No solutions.";
                }
                else
                {
                    int[] bestDepth = toRate.DepthPatern;
                    rating = "F - Depth:" + bestDepth.Length;
                    SafeSetLabel(rating + " - Rating...", token);
                    rating = string.Empty;
                    string codes = Settings.Default.RatingCodes;
                    string[] codeArray = codes.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    List<string> codeList = new List<string>();
                    for (int i = 0; i < codeArray.Length; i++)
                    {
                        codeArray[i] = codeArray[i].Trim().ToUpper();
                        if (codeArray[i] == "G")
                            codeArray[i] = CurrentGeneratingSettingsAsString();
                        if (!codeList.Contains(codeArray[i]))
                            codeList.Add(codeArray[i]);
                    }
                    foreach (string code in codeList)
                    {
                        if (token.IsCancellationRequested)
                            return;
                        toRate.SetRatingCodeOptions(code);
                        if (toRate.TrySolve(false, token) == SolveState.Solved)
                        {
                            if (token.IsCancellationRequested)
                                return;
                            bool found = false;
                            string alternate = code + " - Depth:" + toRate.DepthPatern.Length;
                            if (!code.Contains("F"))
                            {
                                int min = -1;
                                int max = toRate.Edges.Count;
                                int lastSuccessLength = -1;
                                while (min < max - 1)
                                {
                                    if (token.IsCancellationRequested)
                                        return;
                                    int mid = (min + max) / 2;
                                    toRate.IterativeSolverDepth = mid;
                                    if (toRate.TrySolve(false, token) == SolveState.Solved)
                                    {
                                        max = mid;
                                        lastSuccessLength = toRate.DepthPatern.Length;
                                    }
                                    else
                                    {
                                        min = mid;
                                    }
                                }
                                if (max < toRate.Edges.Count)
                                {
                                    if (rating.Length > 0)
                                        rating += " and ";
                                    rating += code + max.ToString() + " - Depth:" + lastSuccessLength.ToString();
                                    found = true;
                                }
                            }
                            if (!found)
                            {
                                if (rating.Length > 0)
                                    rating += " and ";
                                rating += alternate;
                            }
                            SafeSetLabel(rating + " - Rating...", token);
                        }
                        else if (!token.IsCancellationRequested)
                        {
                            if (!code.Contains("F"))
                            {
                                if (rating.Length > 0)
                                    rating += " and ";
                                rating += code + " - Solved:" + (toRate.PercentSolved * 100).ToString("G2") + "%";
                                SafeSetLabel(rating + " - Rating...", token);
                            }
                        }
                    }
                }
                SafeSetLabel(rating, token);
            }
            catch (OperationCanceledException)
            {
                // Replaced by newer rating request
            }
            catch (Exception e)
            {
                if (!token.IsCancellationRequested && !IsDisposed && IsHandleCreated)
                {
                    try
                    {
                        this.BeginInvoke(new ParameterizedThreadStart(UnexpectedExceptionMessage), e);
                    }
                    catch (ObjectDisposedException) { }
                    catch (InvalidOperationException) { }
                }
            }
            finally
            {
                lock (ratingThreadLock)
                {
                    if (ReferenceEquals(ratingCts, cts))
                        ratingCts = null;
                }
                cts.Dispose();
            }
        }

        private string CurrentGeneratingSettingsAsString()
        {
            string res = string.Empty;
            if (Settings.Default.SolverType == SolverMethod.Iterative.ToString())
                res += "S";
            if (Settings.Default.SolverType == SolverMethod.Recursive.ToString())
                res += "F";
            if (Settings.Default.UseICinSolver)
                res += "I";
            if (!Settings.Default.ConsiderMultipleLoops)
                res += "N";
            if (Settings.Default.UseColoringInSolver)
            {
                res += "C";
                if (Settings.Default.UseDerivedColoringInSolver)
                    res += "+";
            }
            if (Settings.Default.UseEdgeRestrictsInSolver)
                res += "E";
            if (Settings.Default.SimpleSolverRecurseDepth > 1)
                res += "R";
            if (Settings.Default.UseCellColoringInSolver)
                res += "O";
            if (Settings.Default.UseMergeInSolver)
                res += "M";
            if (Settings.Default.UseCellPairsInSolver || Settings.Default.UseCellPairsTopLevelInSolver)
            {
                res += "P";
                if (Settings.Default.UseCellPairsInSolver)
                    res += "+";
            }
            return res;
        }

        private void SetLabel(object? newLabelObj)
        {
            labelDepthPatern.Text = (string)newLabelObj!;
        }

        private string ArrayToText(int[] array)
        {
            if (array == null)
                return string.Empty;
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < array.Length; i++)
            {
                if (i > 0)
                    builder.Append(", ");
                builder.Append(array[i]);
            }
            return builder.ToString();
        }

        private void buttonClear_Click(object sender, EventArgs e)
        {
            loopDisplay1.Mesh.Clear();
            if (Settings.Default.AutoStart)
            {
                Mesh mesh = loopDisplay1.Mesh;
                AutoStart(mesh);
            }
            loopDisplay1.Refresh();
        }

        private void buttonSolve_Click(object sender, EventArgs e)
        {
            loopDisplay1.Mesh.Clear();
            if (Settings.Default.VisualSolve)
            {
                Thread solveThread = new Thread(Solve);
                solveThread.IsBackground = true;
                solveThread.Start();
            }
            else
                Solve();
            loopDisplay1.Refresh();
        }

        private void Solve()
        {
            try
            {
                Mesh original = loopDisplay1.Mesh;
                Mesh solver = PuzzleHelper.CreateFullSolverMesh(original);
                solver.Clear();
                if (Settings.Default.VisualSolve)
                    solver.MeshChangeUpdate += new MeshChangeUpdateEventHandler(Mesh_MeshChangeUpdate);
                SolveState res = solver.TrySolve();
                if (res == SolveState.Solved)
                {
                    SolveComplete((original, solver));
                }
                if (Settings.Default.VisualSolve)
                    solver.MeshChangeUpdate -= new MeshChangeUpdateEventHandler(Mesh_MeshChangeUpdate);
            }
            catch (Exception e)
            {
                if (!IsDisposed && IsHandleCreated)
                {
                    try
                    {
                        this.BeginInvoke(new ParameterizedThreadStart(UnexpectedExceptionMessage), e);
                    }
                    catch (ObjectDisposedException) { }
                    catch (InvalidOperationException) { }
                }
            }
        }

        private void SolveComplete(object? stateObj)
        {
            if (this.InvokeRequired)
            {
                if (!IsDisposed && IsHandleCreated)
                    this.BeginInvoke(new ParameterizedThreadStart(SolveComplete), stateObj);
                return;
            }
            if (stateObj is (Mesh original, Mesh solver) && loopDisplay1.Mesh == original && solver.SolutionFound != null)
            {
                labelDepthPatern.Text = ArrayToText(solver.DepthPatern);
                loopDisplay1.Mesh = solver.SolutionFound;
            }
        }

        void Mesh_MeshChangeUpdate(object sender, MeshChangeUpdateEventArgs args)
        {
            if (this.InvokeRequired)
            {
                if (args.Starting)
                    System.Threading.Thread.Sleep(10);
                else
                    System.Threading.Thread.Sleep(100);
                if (!IsDisposed && IsHandleCreated)
                {
                    try
                    {
                        this.BeginInvoke(new MeshChangeUpdateEventHandler(Mesh_MeshChangeUpdate), sender, args);
                    }
                    catch (ObjectDisposedException) { }
                    catch (InvalidOperationException) { }
                }
                return;
            }
            loopDisplay1.Refresh();
        }

        private void LoopDeLoopForm_Load(object sender, EventArgs e)
        {
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using OpenFileDialog dialog = new OpenFileDialog();
            dialog.DefaultExt = "loop";
            dialog.Filter = "Loop-De-Loop files|*.loop|Text Files (*.txt)|*.txt|Loopy Files (*.loopy)|*.loopy|All Files (*.*)|*.*";
            dialog.RestoreDirectory = true;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string rawText = File.ReadAllText(dialog.FileName);
                    if (!PuzzleFormatHelper.TryParsePuzzle(rawText, out Mesh? loadedMesh, out _, out _, out _) || loadedMesh == null)
                    {
                        MessageBox.Show("Failed to open puzzle, file does not contain valid data.");
                        return;
                    }
                    loopDisplay1.Mesh = loadedMesh;
                    labelDepthPatern.Text = string.Empty;
                    loopDisplay1.Refresh();
                    Rate();
                    secs = 0;
                    if (Settings.Default.ShowClock)
                        label2.Text = "Time: 00:00:00";
                    timer1.Enabled = false;
                    timer1.Enabled = true;
                }
                catch
                {
                    MessageBox.Show("Failed to open puzzle, file does not contain valid data.");
                }
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using SaveFileDialog dialog = new SaveFileDialog();
            dialog.RestoreDirectory = true;
            dialog.DefaultExt = "loop";
            dialog.Filter = "Loop-De-Loop Files|*.loop";
            bool canUseNiceFormats = PuzzleFormatHelper.CanExportSquareFormats(loopDisplay1.Mesh)
                && !loopDisplay1.ShowColors
                && !loopDisplay1.ShowCellColors;
            if (canUseNiceFormats)
            {
                dialog.Filter += "|Text files|*.txt|Loopy puzzles|*.loopy|Loopy save game|*.loopy";
            }

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string? content = (canUseNiceFormats, dialog.FilterIndex) switch
                {
                    (true, 2) => PuzzleFormatHelper.ExportSquareAscii(loopDisplay1.Mesh),
                    (true, 3) => PuzzleFormatHelper.ExportLoopyDescriptor(loopDisplay1.Mesh) + Environment.NewLine,
                    (true, 4) => PuzzleFormatHelper.ExportLoopySave(loopDisplay1.Mesh),
                    _ => PuzzleFormatHelper.ExportLoopText(loopDisplay1.Mesh)
                };

                if (content != null)
                {
                    File.WriteAllText(dialog.FileName, content);
                }
            }
        }


        protected override void OnMouseWheel(MouseEventArgs e)
        {
            loopDisplay1.RaiseMouseWheel(e);
            base.OnMouseWheel(e);
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SettingsForm form = new SettingsForm();
            form.SimpleSolver = Settings.Default.SolverType == SolverMethod.Iterative.ToString();
            form.SimpleSolverDepth = Settings.Default.SimpleSolverDepth;
            form.SimpleSolverNestedGuess = Settings.Default.SimpleSolverRecurseDepth > 1;
            form.AutoMove = Settings.Default.AutoMove;
            form.AllowInvalidMoves = Settings.Default.AllowObviousFalseMoves;
            form.AllowMultipleLoops = !Settings.Default.ConsiderMultipleLoops;
            form.UseICinAutoMove = Settings.Default.UseICinAuto;
            form.ConsiderMultipleLoopsInAuto = Settings.Default.ConsiderMultipleLoopsInAuto;
            form.UseICinSolver = Settings.Default.UseICinSolver;
            form.AutoStart = Settings.Default.AutoStart;
            form.BoardFont = Settings.Default.BoardFont;
            form.UseVisualSolver = Settings.Default.VisualSolve;
            form.ShowColors = Settings.Default.ShowColors;
            form.LineLengthFraction = Settings.Default.GenerateLengthFraction;
            form.BoringFraction = Settings.Default.GenerateBoringFraction;
            form.RatingCodes = Settings.Default.RatingCodes;
            form.UseColoringInSolver = Settings.Default.UseColoringInSolver;
            form.UseEdgeRestrictsInSolver = Settings.Default.UseEdgeRestrictsInSolver;
            form.UseDerivedColoringInSolver = Settings.Default.UseDerivedColoringInSolver;
            form.UseMergingInSolver = Settings.Default.UseMergeInSolver;
            form.UseColoringInAuto = Settings.Default.UseColoringInAuto;
            form.UseEdgeRestrictsInAuto = Settings.Default.UseEdgeRestrictsInAuto;
            form.UseCellColoringInSolver = Settings.Default.UseCellColoringInSolver;
            form.UseCellColoringInAuto = Settings.Default.UseCellColoringInAuto;
            form.UseCellPairsInSolver = Settings.Default.UseCellPairsInSolver;
            form.UseCellPairsTopLevelInSolver = Settings.Default.UseCellPairsTopLevelInSolver;
            form.UseCellPairsInAuto = Settings.Default.UseCellPairsInAuto;
            form.ShowCellColors = Settings.Default.ShowCellColors;
            form.ShowCellColorsAdvanced = Settings.Default.ShowCellColorsAdvanced;
            form.ShowClock = Settings.Default.ShowClock;

            if (form.ShowDialog() == DialogResult.OK)
            {
                if (form.SimpleSolver)
                {
                    Settings.Default.SolverType = SolverMethod.Iterative.ToString();
                }
                else
                {
                    Settings.Default.SolverType = SolverMethod.Recursive.ToString();
                }
                Settings.Default.SimpleSolverDepth = form.SimpleSolverDepth;
                Settings.Default.SimpleSolverRecurseDepth = form.SimpleSolverNestedGuess ? 2 : 1;
                Settings.Default.AutoMove = form.AutoMove;
                Settings.Default.AllowObviousFalseMoves = form.AllowInvalidMoves;
                Settings.Default.ConsiderMultipleLoops = !form.AllowMultipleLoops;
                Settings.Default.UseICinSolver = form.UseICinSolver;
                Settings.Default.UseICinAuto = form.UseICinAutoMove;
                Settings.Default.AutoStart = form.AutoStart;
                Settings.Default.BoardFont = form.BoardFont;
                Settings.Default.VisualSolve = form.UseVisualSolver;
                Settings.Default.ShowColors = form.ShowColors;
                Settings.Default.ConsiderMultipleLoopsInAuto = form.ConsiderMultipleLoopsInAuto;
                Settings.Default.GenerateLengthFraction = form.LineLengthFraction;
                Settings.Default.GenerateBoringFraction = form.BoringFraction;
                Settings.Default.RatingCodes = form.RatingCodes;
                Settings.Default.UseColoringInAuto = form.UseColoringInAuto;
                Settings.Default.UseColoringInSolver = form.UseColoringInSolver;
                Settings.Default.UseEdgeRestrictsInAuto = form.UseEdgeRestrictsInAuto;
                Settings.Default.UseEdgeRestrictsInSolver = form.UseEdgeRestrictsInSolver;
                Settings.Default.UseCellColoringInAuto = form.UseCellColoringInAuto;
                Settings.Default.UseCellColoringInSolver = form.UseCellColoringInSolver;
                Settings.Default.ShowCellColors = form.ShowCellColors;
                Settings.Default.ShowCellColorsAdvanced = form.ShowCellColorsAdvanced;
                Settings.Default.ShowClock = form.ShowClock;
                Settings.Default.UseDerivedColoringInSolver = form.UseDerivedColoringInSolver;
                Settings.Default.UseMergeInSolver = form.UseMergingInSolver;
                Settings.Default.UseCellPairsInAuto = form.UseCellPairsInAuto;
                Settings.Default.UseCellPairsInSolver = form.UseCellPairsInSolver;
                Settings.Default.UseCellPairsTopLevelInSolver = form.UseCellPairsTopLevelInSolver;
                Settings.Default.Save();
                this.loopDisplay1.AutoMove = Settings.Default.AutoMove;
                this.loopDisplay1.DisallowTriviallyFalse = !Settings.Default.AllowObviousFalseMoves;
                this.loopDisplay1.UseICInAuto = Settings.Default.UseICinAuto;
                this.loopDisplay1.UseColoringInAuto = Settings.Default.UseColoringInAuto;
                this.loopDisplay1.UseEdgeRestrictsInAuto = Settings.Default.UseEdgeRestrictsInAuto;
                this.loopDisplay1.UseCellColoringInAuto = Settings.Default.UseCellColoringInAuto;
                this.loopDisplay1.UseCellPairsInAuto = Settings.Default.UseCellPairsInAuto;
                this.loopDisplay1.ConsiderMultipleLoopsInAuto = Settings.Default.ConsiderMultipleLoopsInAuto;
                this.loopDisplay1.ShowColors = Settings.Default.ShowColors;
                this.loopDisplay1.ShowCellColors = Settings.Default.ShowCellColors;
                this.loopDisplay1.ShowCellColorsAdvanced = Settings.Default.ShowCellColorsAdvanced;
                this.loopDisplay1.Font = Settings.Default.BoardFont;
                UpdateClockLabel();
            }
        }

        private void readmeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            HelpForm form = new HelpForm();
            form.ShowDialog();
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (loopDisplay1.UndoTree.CanUndo)
            {
                loopDisplay1.UndoTree.Undo();
                loopDisplay1.Refresh();
            }
        }

        private void redoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (loopDisplay1.UndoTree.CanRedo)
            {
                loopDisplay1.UndoTree.Redo();
                loopDisplay1.Refresh();
            }
        }

        private void actionsToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            redoToolStripMenuItem.Enabled = loopDisplay1.UndoTree.CanRedo;
            undoToolStripMenuItem.Enabled = loopDisplay1.UndoTree.CanUndo;
        }

        private void playToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Network.Client.GameClientForm form = new LoopDeLoop.Network.Client.GameClientForm();
            form.Show();
        }

        private void runServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Network.Server.GameServerForm form = new LoopDeLoop.Network.Server.GameServerForm();
            form.Show();
        }

        private void printToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Drawing.Printing.PrintDocument doc = new System.Drawing.Printing.PrintDocument();
            doc.OriginAtMargins = true;
            PrintDialog dialog = new PrintDialog();
            dialog.PrinterSettings = doc.PrinterSettings;
            dialog.UseEXDialog = true;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                doc.PrinterSettings = dialog.PrinterSettings;
                doc.PrintPage += new System.Drawing.Printing.PrintPageEventHandler(doc_PrintPage);
                PrintPreviewDialog dialog2 = new PrintPreviewDialog();
                dialog2.Document = doc;
                dialog2.ShowDialog();
                doc.PrintPage -= new System.Drawing.Printing.PrintPageEventHandler(doc_PrintPage);
            }
        }

        void doc_PrintPage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {
            e.HasMorePages = false;
            this.loopDisplay1.Print(e.Graphics);
        }

        int secs = 0;

        private void timer1_Tick(object sender, EventArgs e)
        {
            secs++;
            UpdateClockLabel();
        }

        private void UpdateClockLabel()
        {
            if (Settings.Default.ShowClock)
                label2.Text = "Time: " + TimeSpan.FromSeconds(secs).ToString();
            else
                label2.Text = string.Empty;
        }

        private void hintToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Hint(false);
        }

        private void Hint(bool perform)
        {
            Mesh hintMesh = new Mesh(loopDisplay1.Mesh);
            hintMesh.ConsiderIntersectCellInteractsAsSimple = true;
            hintMesh.ConsiderMultipleLoops = true;
            hintMesh.IterativeSolverDepth = 0;
            hintMesh.IterativeRecMaxDepth = 1;
            hintMesh.UseColoring = false;
            hintMesh.UseCellPairs = false;
            hintMesh.UseCellPairsTopLevel = false;
            hintMesh.UseEdgeRestricts = false;
            hintMesh.UseCellColoring = false;
            hintMesh.UseDerivedColoring = false;
            hintMesh.UseMerging = false;
            hintMesh.UseIntersectCellInteractsInSolver = true;
            List<IAction> changes = new List<IAction>();
            hintMesh.PerformStart(changes);
            while (changes.Count == 0)
            {
                hintMesh.GetSomething(changes);
                if (hintMesh.IterativeSolverDepth > 10)
                    break;
                hintMesh.IterativeSolverDepth = hintMesh.IterativeSolverDepth + 1;
            }
            if (changes.Count > 0)
            {
                if (perform)
                    loopDisplay1.Perform(changes[0]);
                else
                    loopDisplay1.Flash(changes[0]);
            }
        }

        private void doHintToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Hint(true);
        }

        private void markToolStripMenuItem_Click(object sender, EventArgs e)
        {
            loopDisplay1.UndoTree.Mark();
        }

        private void revertTiToolStripMenuItem_Click(object sender, EventArgs e)
        {
            loopDisplay1.UndoTree.RevertToMark();
            loopDisplay1.Refresh();
        }

    }
}