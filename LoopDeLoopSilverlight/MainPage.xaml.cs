using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Threading;

namespace LoopDeLoop
{
    public partial class MainPage : UserControl
    {
        public MainPage()
        {
            InitializeComponent();
            Display.Generation += new EventHandler<ProgressEventArgs>(Display_Generation);
            timer = new Timer(new TimerCallback(TimerFired), null, -1, -1);
            // Kick off the first generate.
            Change_Click(this, new RoutedEventArgs());
        }

        Timer timer = null;
        int secCount = 0;

        void TimerFired(object ignore)
        {
            int mins = secCount / 60;
            int secs = secCount % 60;
            this.Dispatcher.BeginInvoke(delegate()
            {
                TimerDisplay.Text = mins.ToString() + ":" + secs.ToString("00");
            });
            secCount++;
        }

        void Display_Generation(object sender, ProgressEventArgs e)
        {
            this.Dispatcher.BeginInvoke(delegate()
            {
                GenerateProgress.Value = e.CurrentPruned; 
                GenerateProgress.Maximum = e.TargetPruned; 
                GenerateProgress.Minimum = 0; 
                GenerateProgress.Visibility = e.CurrentPruned >= e.TargetPruned ? Visibility.Collapsed : Visibility.Visible;
                if (e.CurrentPruned < e.TargetPruned)
                {
                    timer.Change(-1, -1);
                }
                else
                {
                    TimerDisplay.Text = "0:00";
                    secCount = 0;
                    timer.Change(0, 1000);
                }
            });
        }

        int prunedCount = 0;
        int targetPrunedCount = 0;
        private void GenerateFirst(Mesh mesh)
        {
            targetPrunedCount = mesh.Cells.Count;
            prunedCount = 0;
            Display_Generation(this, new ProgressEventArgs(prunedCount, targetPrunedCount));
            mesh.PrunedCountProgress += new EventHandler(mesh_PrunedCountProgress);
            mesh.Generate();
            Display_Generation(this, new ProgressEventArgs(targetPrunedCount, targetPrunedCount));
            this.Dispatcher.BeginInvoke(delegate() { Display.Mesh = mesh; });
        }

        void mesh_PrunedCountProgress(object sender, EventArgs e)
        {
            prunedCount++;
            Display_Generation(this, new ProgressEventArgs(prunedCount, targetPrunedCount));
        }

        private void UserControl_KeyDown(object sender, KeyEventArgs e)
        {
            Display.OnKeyDown(e);

        }

        private void Change_Click(object sender, RoutedEventArgs e)
        {
            MeshType type;
            if (StyleSelection.SelectedIndex < 0)
                type = MeshType.Square;
            else
               type = MeshTypeFromString((string)((ComboBoxItem)StyleSelection.SelectedItem).Content);
            int width;
            int height;
            if (!ParseSize(Size.Text, type, out width, out height))
                return; 
            Mesh mesh = new Mesh(width, height, type);
            mesh.ConsiderMultipleLoops = DifficultySelector.SelectedIndex > 0;
            mesh.IterativeRecMaxDepth = 1;
            if (DifficultySelector.SelectedIndex > 1)
            {
                mesh.UseCellColoring = true;
                mesh.UseCellColoringTrials = true;
                mesh.UseColoring = true;
                mesh.UseEdgeRestricts = true;
                mesh.UseDerivedColoring = true;
                mesh.UseMerging = true;
                mesh.UseCellPairsTopLevel = true;
            }
            if (DifficultySelector.SelectedIndex < 5)
            {
                mesh.IterativeSolverDepth = Math.Max(0, DifficultySelector.SelectedIndex-2);
            }
            else
            {
                mesh.SolverMethod = SolverMethod.Recursive;
            }
            mesh.GenerateBoringFraction = 0.01;
            Display.Mesh = new Mesh(mesh);
            ThreadPool.QueueUserWorkItem(new WaitCallback(delegate(object o) { GenerateFirst(mesh); }));
        }

        public static MeshType MeshTypeFromString(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return MeshType.Square;
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

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            HelpDisplay.Visibility = System.Windows.Visibility.Visible;
        }

        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            Display.Undo();
        }

        private void RedoButton_Click(object sender, RoutedEventArgs e)
        {
            Display.Redo();
        }

        private void UnfixButton_Click(object sender, RoutedEventArgs e)
        {
            Display.Unfix();
        }

        private void FixButton_Click(object sender, RoutedEventArgs e)
        {
            Display.Fix();
        }

        private void RevertButton_Click(object sender, RoutedEventArgs e)
        {
            Display.RevertToFix();
        }
    }
}
