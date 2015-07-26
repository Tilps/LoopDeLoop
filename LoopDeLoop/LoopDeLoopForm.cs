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

        void UndoTree_CanUndoRedoMaybeChanged(object sender, EventArgs e)
        {
            redoToolStripMenuItem.Enabled = loopDisplay1.UndoTree.CanRedo;
            undoToolStripMenuItem.Enabled = loopDisplay1.UndoTree.CanUndo;
        }

        void loopDisplay1_Solved(object sender, EventArgs e)
        {
            timer1.Enabled = false;
        }

        ProgressBar bar;

        object generatorLock = new object();
        bool generating = false;
        Thread generatorThread = null;
        Mesh generatingMesh = null;
        bool stopping;

        private void button1_Click(object sender, EventArgs e)
        {
            string typeName =comboMeshType.SelectedItem.ToString();
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
                    generatingMesh.AbortPrune = true;
                }
                else if (generating)
                {
                    generating = false;
                    buttonNew.Text = "New";
                    generatorThread.Abort();
                    generatorThread = null;
                }
                else
                {
                    generatingMesh = newMesh;
                    generating = true;
                    stopping = false;
                    buttonNew.Text = "Stop";
                    generatorThread = new Thread(Generate);
                    generatorThread.IsBackground = true;
                    generatorThread.Start(newMesh);
                }
            }
        }

        private void Generate(object newMeshObj)
        {
            try
            {
                Mesh newMesh = (Mesh)newMeshObj;
                try
                {
                    this.Invoke(new ParameterizedThreadStart(ShowBar), newMesh.Cells.Count);
                }
                catch
                {
                }
                newMesh.PrunedCountProgress += new EventHandler(Mesh_PrunedCountProgress);
                try
                {
                    newMesh.Generate();
                }
                catch
                {
                    return;
                }
                newMesh.PrunedCountProgress -= new EventHandler(Mesh_PrunedCountProgress);
                if (Settings.Default.AutoStart)
                {
                    AutoStart(newMesh);
                }
                try
                {
                    this.Invoke(new ParameterizedThreadStart(UpdateMesh), newMesh);
                }
                catch (InvalidOperationException)
                {
                    // We're shuting down? whatever.
                }
                lock (generatorLock)
                {
                    generating = false;
                    generatorThread = null;
                    this.Invoke(new MethodInvoker(ResetGeneratorButton));
                }
            }
            catch (Exception e)
            {
                if (!(e is ThreadAbortException))
                {
                    try
                    {
                        this.BeginInvoke(new ParameterizedThreadStart(UnexpectedExceptionMessage), e);
                    }
                    catch
                    {
                    }
                }
            }
            finally
            {
                this.BeginInvoke(new MethodInvoker(HideBar));
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

        private void UpdateMesh(object meshObj)
        {
            Mesh newMesh = (Mesh)meshObj;
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
                labelDepthPatern.Parent.Controls.Remove(bar);
                bar = null;
            }
        }

        private void ShowBar(object countObj)
        {
            int count = (int)countObj;
            bar = new ProgressBar();
            bar.Top = labelDepthPatern.Top;
            bar.Left = labelDepthPatern.Left;
            bar.Width = this.ClientRectangle.Width - bar.Left - bar.Left;
            bar.Height = labelDepthPatern.Height;
            bar.Step = 1;
            bar.Maximum = count;
            bar.Minimum = 0;
            labelDepthPatern.Parent.Controls.Add(bar);
            bar.BringToFront();
        }

        void Mesh_PrunedCountProgress(object sender, EventArgs e)
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
            bar.PerformStep();
            bar.Refresh();
        }

        public static bool ParseSize(string val, MeshType type, out int width, out int height)
        {
            val = val.Trim();
            width = 10;
            height = 10;
            if (val.Length == 0)
            {
                if (type == MeshType.Octagon)
                {
                    width = 5;
                    height = 5;
                }
                else if (type == MeshType.Square2)
                {
                    width = 5;
                    height = 5;
                }
                else if (type == MeshType.Hexagonal)
                {
                    width = 5;
                    height = 10;
                }
                else if (type == MeshType.Hexagonal2)
                {
                    width = 6;
                    height = 6;
                }
                else if (type == MeshType.Hexagonal3)
                {
                    width = 4;
                    height = 4;
                }
                else if (type == MeshType.Triangle)
                {
                    width = 6;
                    height = 6;
                }
                else if (type == MeshType.Pentagon)
                {
                    width = 6;
                    height = 6;
                }
               return true;
            }
            string[] bits = val.Split('x');
            if (bits.Length == 2)
            {
                if (!int.TryParse(bits[0], out width))
                    return false;
                if (!int.TryParse(bits[1], out height))
                    return false;
                return true;
            }
            else if (bits.Length == 1)
            {
                if (!int.TryParse(bits[0], out width))
                    return false;
                height = width;
                return true;
            }
            return false;

        }

        public static MeshType MeshTypeFromString(string typeName)
        {
            MeshType type = MeshType.Square;
            if (typeName == "Square")
                type = MeshType.Square;
            else if (typeName == "Square Symmetrical")
                type = MeshType.SquareSymmetrical;
            else if (typeName == "Triangle")
                type = MeshType.Triangle;
            else if (typeName == "Hexagon")
                type = MeshType.Hexagonal;
            else if (typeName == "Hexagon2")
                type = MeshType.Hexagonal2;
            else if (typeName == "Hexagon3")
                type = MeshType.Hexagonal3;
            else if (typeName == "Octagon")
                type = MeshType.Octagon;
            else if (typeName == "Square2")
                type = MeshType.Square2;
            else if (typeName == "Pentagon")
                type = MeshType.Pentagon;
            return type;
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
            }
            return "Square";

        }

        object ratingThreadLock = new object();
        Thread ratingThread = null;

        private void Rate()
        {
            Mesh toRate = new Mesh(loopDisplay1.Mesh);
            lock (ratingThreadLock)
            {
                if (ratingThread != null)
                    ratingThread.Abort();
                ratingThread = null;
                labelDepthPatern.Text = "Rating...";
                ratingThread = new Thread(RatingThreadProc);
                ratingThread.IsBackground = true;
                ratingThread.Priority = ThreadPriority.Lowest;
                ratingThread.Start(toRate);
            }

        }

        internal static void UnexpectedExceptionMessage(object o)
        {
            Exception e = (Exception)o;
            MessageBox.Show("An unexpected error has occured. Error details: " + e.ToString());
        }

        private void RatingThreadProc(object toRateObj)
        {
            try
            {
                Mesh toRate = (Mesh)toRateObj;
                toRate.Clear();
                string rating = string.Empty;
                toRate.SolverMethod = SolverMethod.Recursive;
                toRate.IterativeSolverDepth = int.MaxValue;
                toRate.UseIntersectCellInteractsInSolver = false;
                toRate.UseCellPairsTopLevel = true;
                // TODO: Consider whether this is a performance loss or win.
                toRate.UseCellPairs = false;
                toRate.UseColoring = true;
                toRate.UseEdgeRestricts = true;
                toRate.UseCellColoring = true;
                toRate.ConsiderMultipleLoops = true;
                toRate.UseMerging = true;
                toRate.UseDerivedColoring = true;
                // TODO: match this value to solve to get the best performance.
                toRate.IterativeRecMaxDepth = 1;
                SolveState solveState = toRate.TrySolve();
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
                    this.Invoke(new ParameterizedThreadStart(SetLabel), rating + " - Rating...");
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
                        toRate.SetRatingCodeOptions(code);
                        if (toRate.TrySolve() == SolveState.Solved)
                        {
                            bool found = false;
                            string alternate = code + " - Depth:" + toRate.DepthPatern.Length;
                            if (!code.Contains("F"))
                            {
                                int min = -1;
                                int max = toRate.Edges.Count;
                                int lastSuccessLength = -1;
                                while (min < max - 1)
                                {

                                    int mid = (min + max) / 2;
                                    toRate.IterativeSolverDepth = mid;
                                    if (toRate.TrySolve() == SolveState.Solved)
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
                            this.Invoke(new ParameterizedThreadStart(SetLabel), rating + " - Rating...");
                        }
                        else
                        {
                            if (!code.Contains("F"))
                            {
                                if (rating.Length > 0)
                                    rating += " and ";
                                rating += code + " - UnFin:" + (toRate.PercentSolved * 100).ToString("G2") + "%";
                                this.Invoke(new ParameterizedThreadStart(SetLabel), rating + " - Rating...");
                            }
                       }
                    }
                }
                this.Invoke(new ParameterizedThreadStart(SetLabel), rating);
                lock (ratingThreadLock)
                {
                    ratingThread = null;
                }
            }
            catch (Exception e)
            {
                if (!(e is ThreadAbortException))
                {
                    try
                    {
                        this.BeginInvoke(new ParameterizedThreadStart(UnexpectedExceptionMessage), e);
                    }
                    catch
                    {
                    }
                }
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

        private void SetLabel(object newLabelObj)
        {
            labelDepthPatern.Text = (string)newLabelObj;
        }

        private bool VerifyBoardSize(int width, int height)
        {
            if (width > 20 || height > 20)
            {
                if (DialogResult.No == MessageBox.Show("You have chosen a large size, it may take an extremely long time to generate, are you sure you wish to continue?", "Large size grid detected.", MessageBoxButtons.YesNo))
                {
                    return false;
                }
            }
            return true;
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
                original.ConsiderMultipleLoops = true;
                original.UseIntersectCellInteractsInSolver = false;
                original.UseColoring = true;
                original.UseEdgeRestricts = true;
                original.UseCellColoring = true;
                original.SolverMethod = SolverMethod.Recursive;
                original.ContaminateFullSolver = true;
                original.ColoringCheats = true;
                original.UseDerivedColoring = true;
                original.UseMerging = true;
                original.UseCellPairsTopLevel = true;
                // TODO: consider whether having this on makes it faster or not.
                original.UseCellPairs = false;
                // TODO: adjust this number for best performance. (When does true recursion become a win...)
                original.IterativeRecMaxDepth = 1;
                if (Settings.Default.VisualSolve)
                    original.MeshChangeUpdate += new MeshChangeUpdateEventHandler(Mesh_MeshChangeUpdate);
                SolveState res = original.TrySolve();
                if (res == SolveState.Solved)
                {
                    SolveComplete(original);
                }
                if (Settings.Default.VisualSolve)
                    original.MeshChangeUpdate -= new MeshChangeUpdateEventHandler(Mesh_MeshChangeUpdate);
            }
            catch (Exception e)
            {
                if (!(e is ThreadAbortException))
                {
                    try
                    {
                        this.BeginInvoke(new ParameterizedThreadStart(UnexpectedExceptionMessage), e);
                    }
                    catch
                    {
                    }
                }
            }
        }

        private void SolveComplete(object mesh)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new ParameterizedThreadStart(SolveComplete), mesh);
                return;
            }
            Mesh original = (Mesh)mesh;
            if (loopDisplay1.Mesh == original)
            {
                labelDepthPatern.Text = ArrayToText(loopDisplay1.Mesh.DepthPatern);
                loopDisplay1.Mesh = loopDisplay1.Mesh.SolutionFound;
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
                try
                {
                    this.BeginInvoke(new MeshChangeUpdateEventHandler(Mesh_MeshChangeUpdate), sender, args);
                }
                catch (InvalidOperationException)
                {
                    // Ignore shutdown errors.
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
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.DefaultExt = "loop";
            dialog.Filter = "Loop-De-Loop files|*.loop|Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
            dialog.RestoreDirectory = true;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string[] lines = File.ReadAllLines(dialog.FileName);
                    if (lines.Length == 0)
                        throw new Exception("File format invalid");
                    if (lines[0].Length == 0)
                        throw new Exception("File format invalid");
                    // The old 'standard format'.  Used for mesh type square.
                    if (lines[0][0] == '+')
                    {
                        LoadSimpleSquare(lines);
                        labelDepthPatern.Text = string.Empty;
                        loopDisplay1.Refresh();
                    }
                    else if (IsLoopySave(lines))
                    {
                        LoadLoopySave(lines);
                        labelDepthPatern.Text = string.Empty;
                        loopDisplay1.Refresh();

                    }
                    else if (IsLoopy(lines))
                    {
                        LoadLoopyPuzzle(lines);
                        labelDepthPatern.Text = string.Empty;
                        loopDisplay1.Refresh();
                    }
                    else
                    {
                        MeshType type = (MeshType)Enum.Parse(typeof(MeshType), lines[0]);
                        Mesh mesh = new Mesh(0, 0, type);
                        if (!mesh.LoadFromText(lines))
                            return;
                        loopDisplay1.Mesh = mesh;
                        labelDepthPatern.Text = string.Empty;
                        loopDisplay1.Refresh();
                    }
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

        private void LoadLoopyPuzzle(string[] lines)
        {
            string[] parts = lines[0].Split(':');
            string[] parts2 = parts[0].Split('x');
            int width = int.Parse(parts2[0]);
            int height = int.Parse(parts2[1]);
            loopDisplay1.Mesh = new Mesh(width, height, MeshType.Square);
            int counter = 0;
            for (int i = 0; i < parts[1].Length; i++)
            {
                if (char.IsDigit(parts[1][i]))
                {
                    loopDisplay1.Mesh.AddTarget(loopDisplay1.Mesh.Cells[counter / width + (counter % width) * height], int.Parse("" + parts[1][i]));
                    counter++;
                }
                else
                {
                    for (int j = 0; j < parts[1][i] - 'a' + 1; j++)
                    {
                        loopDisplay1.Mesh.AddTarget(loopDisplay1.Mesh.Cells[counter / width + (counter % width) * height], -1);
                        counter++;
                    }
                }
            }
        }

        private void LoadSimpleSquare(string[] lines)
        {
            loopDisplay1.Mesh = new Mesh(lines[0].Length / 2, lines.Length / 2, MeshType.Square);
            int height = lines.Length / 2;
            for (int i = 0; i < lines.Length; i++)
            {
                if (i % 2 == 0)
                {
                    int row = i / 2;

                    for (int j = 1; j < lines[i].Length; j += 2)
                    {
                        int edgeIndex = loopDisplay1.Mesh.GetEdgeJoining((j / 2) * (height + 1) + row, (j / 2 + 1) * (height + 1) + row);
                        List<IAction> backup = new List<IAction>();
                        if (lines[i][j] != ' ')
                            loopDisplay1.Mesh.Perform(edgeIndex, (lines[i][j] == '-' ? EdgeState.Filled : EdgeState.Excluded), backup, 0);
                    }
                }
                else
                {
                    int row = i / 2;
                    for (int j = 1; j < lines[i].Length; j += 2)
                    {
                        Cell c = loopDisplay1.Mesh.Cells[(j / 2) * height + row];
                        loopDisplay1.Mesh.AddTarget(c, lines[i][j] == ' ' ? -1 : int.Parse("" + lines[i][j]));
                    }
                    for (int j = 0; j < lines[i].Length; j += 2)
                    {
                        int edgeIndex = loopDisplay1.Mesh.GetEdgeJoining((j / 2) * (height + 1) + row, (j / 2) * (height + 1) + row + 1);
                        List<IAction> backup = new List<IAction>();
                        if (lines[i][j] != ' ')
                            loopDisplay1.Mesh.Perform(edgeIndex, (lines[i][j] == '|' ? EdgeState.Filled : EdgeState.Excluded), backup, 0);
                    }
                }
            }
        }

        private void LoadLoopySave(string[] lines)
        {
            int width = 0;
            int height = 0;
            foreach (string line in lines)
            {
                string[] bits = line.Split(new char[] { ':' }, 3, StringSplitOptions.None);
                string type = bits[0].Trim();
                switch (type)
                {
                    case "PARAMS":
                        {
                            int first = bits[2].IndexOf('x');
                            int second = bits[2].IndexOf('r');
                            width = int.Parse(bits[2].Substring(0, first));
                            height = int.Parse(bits[2].Substring(first + 1, second - first - 1));
                            loopDisplay1.Mesh = new Mesh(width, height, MeshType.Square);
                        }
                        break;
                    case "DESC":
                        {
                            int counter = 0;
                            for (int i = 0; i < bits[2].Length; i++)
                            {
                                if (char.IsDigit(bits[2][i]))
                                {
                                    loopDisplay1.Mesh.AddTarget(loopDisplay1.Mesh.Cells[counter / width + (counter % width) * height], int.Parse("" + bits[2][i]));
                                    counter++;
                                }
                                else
                                {
                                    for (int j = 0; j < bits[2][i] - 'a' + 1; j++)
                                    {
                                        loopDisplay1.Mesh.AddTarget(loopDisplay1.Mesh.Cells[counter / width + (counter % width) * height], -1);
                                        counter++;
                                    }
                                }
                            }
                        }
                        break;
                    case "MOVE":
                        {
                            string[] coords = bits[2].Split(',');
                            int x = int.Parse(coords[0]);
                            int y = int.Parse(coords[1].Substring(0, coords[1].Length - 2));
                            bool horiz = coords[1][coords[1].Length - 2] == 'h';
                            if (coords[1][coords[1].Length - 1] == 'u')
                            {
                                ForceClearEdge(x, y, horiz, height);
                            }
                            else
                            {
                                bool isSet = coords[1][coords[1].Length - 1] == 'y';
                                EnsureEdge(x, y, horiz, isSet, height);
                            }
                        }
                        break;
                    case "SOLVE":
                        {
                            int x = 0;
                            int y = 0;
                            bool horiz = false;
                            string accumulator = string.Empty;
                            foreach (char c in bits[2])
                            {
                                if (char.IsDigit(c))
                                {
                                    accumulator += c;
                                }
                                if (c == ',')
                                {
                                    x = int.Parse(accumulator);
                                    accumulator = string.Empty;
                                }
                                else if (c == 'h' || c == 'v')
                                {
                                    y = int.Parse(accumulator);
                                    accumulator = string.Empty;
                                    horiz = c == 'h';
                                }
                                else if (c == 'n')
                                {
                                    EnsureEdge(x, y, horiz, false, height);
                                }
                                else if (c == 'y')
                                {
                                    EnsureEdge(x, y, horiz, true, height);
                                }
                            }
                        }
                        break;
                }
            }
        }
        
        private void EnsureEdge(int x, int y, bool horiz, bool line, int height)
        {
            int start = x * (height + 1) + y;
            int end = start + (horiz ? height : 0) + 1;
            int edge = loopDisplay1.Mesh.GetEdgeJoining(start, end);
            EdgeState curState = loopDisplay1.Mesh.Edges[edge].State;
            EdgeState newState = line ? EdgeState.Filled : EdgeState.Excluded;
            if (curState != EdgeState.Empty)
            {
                if (curState != newState)
                {
                    loopDisplay1.UndoTree.Do(new UnsetAction(loopDisplay1.Mesh, edge));
                    loopDisplay1.UndoTree.Do(new SetAction(loopDisplay1.Mesh, edge, newState));
                }
            }
            else
                loopDisplay1.UndoTree.Do(new SetAction(loopDisplay1.Mesh, edge, newState));
        }

        private void ForceClearEdge(int x, int y, bool horiz, int height)
        {
            int start = x * (height + 1) + y;
            int end = start + (horiz ? height : 0) + 1;
            int edge = loopDisplay1.Mesh.GetEdgeJoining(start, end);
            EdgeState curState = loopDisplay1.Mesh.Edges[edge].State;
            if (curState != EdgeState.Empty)
            {
                loopDisplay1.UndoTree.Do(new UnsetAction(loopDisplay1.Mesh, edge));
            }
        }

        private bool IsLoopy(string[] lines)
        {
            string[] parts = lines[0].Split(':');
            if (parts.Length != 2)
                return false;
            string[] parts2 = parts[0].Split('x');
            if (parts2.Length != 2)
                return false;
            return true;
        }


        private bool IsLoopySave(string[] lines)
        {
            if (lines.Length < 3)
                return false;
            if (lines[0] != @"SAVEFILE:41:Simon Tatham's Portable Puzzle Collection")
                return false;
            if (lines[1] != @"VERSION :1:1")
                return false;
            if (lines[2] != @"GAME    :5:Loopy")
                return false;
            return true;
        }


        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.RestoreDirectory = true;
            dialog.DefaultExt = "loop";
            dialog.Filter = "Loop-De-Loop Files|*.loop";
            bool canUseNiceFormats = (loopDisplay1.Mesh.MeshType == MeshType.Square || loopDisplay1.Mesh.MeshType == MeshType.SquareSymmetrical) && !loopDisplay1.ShowColors && !loopDisplay1.ShowCellColors;
            if (canUseNiceFormats)
            {
                dialog.Filter = dialog.Filter + "|Text files|*.txt";
                dialog.Filter = dialog.Filter + "|Loopy puzzles|*.loopy";
                dialog.Filter = dialog.Filter + "|Loopy save game|*.loopy";
            }
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                if (canUseNiceFormats)
                {
                    bool found = true;
                    int iSearch = 0;
                    while (found)
                    {
                        found = true;
                        try
                        {
                            loopDisplay1.Mesh.GetEdgeJoining(iSearch, iSearch + 1);
                        }
                        catch
                        {
                            found = false;
                        }
                        iSearch++;
                    }
                    int height = iSearch - 1;
                    int width = loopDisplay1.Mesh.Cells.Count / height;
                    using (TextWriter writer = File.CreateText(dialog.FileName))
                    {
                        if (dialog.FilterIndex == 4)
                        {
                            writer.WriteLine(@"SAVEFILE:41:Simon Tatham's Portable Puzzle Collection");
                            writer.WriteLine(@"VERSION :1:1");
                            writer.WriteLine(@"GAME    :5:Loopy");
                            LoopySaveWrite(writer, "PARAMS", string.Format("{0}x{1}r0de", width, height));
                            LoopySaveWrite(writer, "CPARAMS", string.Format("{0}x{1}r0de", width, height));
                            LoopySaveWrite(writer, "SEED", 0.ToString());
                            StringBuilder builder = new StringBuilder();
                            int blankCount = 0;
                            for (int j = 0; j < height; j++)
                            {
                                for (int i = 0; i < width; i++)
                                {
                                    Cell cell = loopDisplay1.Mesh.Cells[i * height + j];
                                    if (cell.TargetCount >= 0)
                                    {
                                        if (blankCount > 0)
                                            builder.Append((char)('a' + (blankCount - 1)));
                                        blankCount = 0;
                                        builder.Append(cell.TargetCount);
                                    }
                                    else
                                        blankCount++;

                                }
                                if (blankCount > 0)
                                    builder.Append((char)('a' + (blankCount - 1)));
                                blankCount = 0;
                            }
                            LoopySaveWrite(writer, "DESC", builder.ToString());
                            LoopySaveWrite(writer, "NSTATES", builder.Length.ToString());
                            LoopySaveWrite(writer, "STATEPOS", builder.Length.ToString());
                            bool complete = true;
                            builder.Length = 0;
                            builder.Append('S');
                            for (int j = 0; j <= height; j++)
                            {
                                for (int k = 0; k < 2; k++)
                                {
                                    for (int i = 0; i <= width; i++)
                                    {
                                        if (k == 0)
                                        {
                                            if (i < width)
                                            {
                                                StringBuilder moveBuilder = new StringBuilder();
                                                moveBuilder.AppendFormat("{0},{1}h", i, j); 
                                                int start = i * (height + 1) + j;
                                                int end = start + height + 1;
                                                Edge edge = loopDisplay1.Mesh.Edges[loopDisplay1.Mesh.GetEdgeJoining(start, end)];
                                                if (edge.State == EdgeState.Filled)
                                                    moveBuilder.Append('y');
                                                else if (edge.State == EdgeState.Excluded)
                                                    moveBuilder.Append('n');
                                                else
                                                    complete = false;
                                                if (!moveBuilder.ToString().EndsWith("h"))
                                                {
                                                    builder.Append(moveBuilder.ToString());
                                                    LoopySaveWrite(writer, "MOVE", moveBuilder.ToString());
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (j < height)
                                            {
                                                StringBuilder moveBuilder = new StringBuilder();
                                                moveBuilder.AppendFormat("{0},{1}v", i, j);
                                                int start = i * (height + 1) + j;
                                                int end = start + 1;
                                                Edge edge = loopDisplay1.Mesh.Edges[loopDisplay1.Mesh.GetEdgeJoining(start, end)];
                                                if (edge.State == EdgeState.Filled)
                                                    moveBuilder.Append('y');
                                                else if (edge.State == EdgeState.Excluded)
                                                    moveBuilder.Append('n');
                                                else
                                                    complete = false;
                                                if (!moveBuilder.ToString().EndsWith("v"))
                                                {
                                                    builder.Append(moveBuilder.ToString());
                                                    LoopySaveWrite(writer, "MOVE", moveBuilder.ToString());
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            if (complete)
                            {
                                LoopySaveWrite(writer, "SOLVE", builder.ToString());
                            }
                        }
                        else if (dialog.FilterIndex == 3)
                        {
                            writer.Write("{0}x{1}:", width, height);
                            int blankCount = 0;
                            for (int j = 0; j < height; j++)
                            {
                                for (int i = 0; i < width; i++)
                                {
                                    Cell cell = loopDisplay1.Mesh.Cells[i * height + j];
                                    if (cell.TargetCount >= 0)
                                    {
                                        if (blankCount > 0)
                                            writer.Write((char)('a' + (blankCount - 1)));
                                        blankCount = 0;
                                        writer.Write(cell.TargetCount);
                                    }
                                    else
                                        blankCount++;

                                }
                                if (blankCount > 0)
                                    writer.Write((char)('a' + (blankCount - 1)));
                                blankCount = 0;
                            }
                            writer.WriteLine();
                        }
                        else
                        {
                            for (int j = 0; j <= height; j++)
                            {
                                for (int k = 0; k < 2; k++)
                                {
                                    for (int i = 0; i <= width; i++)
                                    {
                                        if (k == 0)
                                        {
                                            writer.Write("+");
                                            if (i < width)
                                            {
                                                int start = i * (height + 1) + j;
                                                int end = start + height + 1;
                                                Edge edge = loopDisplay1.Mesh.Edges[loopDisplay1.Mesh.GetEdgeJoining(start, end)];
                                                if (edge.State == EdgeState.Filled)
                                                    writer.Write("-");
                                                else if (edge.State == EdgeState.Excluded)
                                                    writer.Write("x");
                                                else
                                                    writer.Write(" ");
                                            }
                                        }
                                        else
                                        {
                                            if (j < height)
                                            {
                                                int start = i * (height + 1) + j;
                                                int end = start + 1;
                                                Edge edge = loopDisplay1.Mesh.Edges[loopDisplay1.Mesh.GetEdgeJoining(start, end)];
                                                if (edge.State == EdgeState.Filled)
                                                    writer.Write("|");
                                                else if (edge.State == EdgeState.Excluded)
                                                    writer.Write("x");
                                                else
                                                    writer.Write(" ");

                                                if (i < width)
                                                {
                                                    Cell cell = loopDisplay1.Mesh.Cells[i * height + j];
                                                    if (cell.TargetCount >= 0)
                                                        writer.Write(cell.TargetCount);
                                                    else
                                                        writer.Write(" ");
                                                }
                                            }
                                        }
                                    }
                                    if (k == 0 || j < height)
                                        writer.WriteLine();
                                }
                            }
                        }
                    }
                }
                else
                {
                    Mesh mesh = loopDisplay1.Mesh;
                    using (TextWriter writer = File.CreateText(dialog.FileName))
                    {
                        mesh.Save(writer);
                    }
                }
            }

        }

        private void LoopySaveWrite(TextWriter writer, string name, string value)
        {
            writer.Write(name.PadRight(8, ' '));
            writer.Write(':');
            writer.Write(value.Length);
            writer.Write(':');
            writer.Write(value);
            writer.WriteLine();
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