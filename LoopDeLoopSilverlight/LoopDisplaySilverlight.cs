using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

namespace LoopDeLoop
{
    public class LoopDisplaySilverlight : Panel
    {        
        protected override Size MeasureOverride(Size availableSize)
        {
            Size res = availableSize;
            if (double.IsPositiveInfinity(res.Width))
                res.Width = 800;
            if (double.IsPositiveInfinity(res.Height))
                res.Height = 600;
            OnScaleChanged(res);
            foreach (UIElement child in Children)
            {
                child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            }
            return res;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            Size res = base.ArrangeOverride(finalSize);
            foreach (UIElement child in Children)
            {
                child.Arrange(new Rect(Canvas.GetLeft(child), Canvas.GetTop(child), child.DesiredSize.Width, child.DesiredSize.Height));
            }
            return res;
        }
        public LoopDisplaySilverlight()
        {
            this.Mesh = new Mesh(10, 10, MeshType.Square);
            this.MouseLeftButtonDown += new MouseButtonEventHandler(LoopDisplaySilverlight_MouseLeftButtonDown);
            this.MouseLeftButtonUp += new MouseButtonEventHandler(LoopDisplaySilverlight_MouseLeftButtonUp);
            this.MouseRightButtonDown += new MouseButtonEventHandler(LoopDisplaySilverlight_MouseRightButtonDown);
            this.MouseRightButtonUp += new MouseButtonEventHandler(LoopDisplaySilverlight_MouseRightButtonUp);
            this.MouseMove += new MouseEventHandler(LoopDisplaySilverlight_MouseMove);
            this.LostMouseCapture += new MouseEventHandler(LoopDisplaySilverlight_LostMouseCapture);
            this.MouseLeave += new MouseEventHandler(LoopDisplaySilverlight_MouseLeave);
        }

        void LoopDisplaySilverlight_MouseLeave(object sender, MouseEventArgs e)
        {
            clicking = false;
        }

        void LoopDisplaySilverlight_LostMouseCapture(object sender, MouseEventArgs e)
        {
            clicking = false;
        }
        bool clicking = false;
        void LoopDisplaySilverlight_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            clicking = false;
        }

        void LoopDisplaySilverlight_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.ReleaseMouseCapture();
            clicking = false;
        }

        void LoopDisplaySilverlight_MouseMove(object sender, MouseEventArgs e)
        {
            if (clicking)
            {
                int closestEdge;
                int closestCell;
                FindClosest(e.GetPosition(this).X, e.GetPosition(this).Y, out closestEdge, out closestCell, 8.0f/(float)scaleFactor);
                if (closestEdge != -1 || closestCell != -1)
                {
                    if (lastClosestCell != closestCell || lastClosestEdge != closestEdge)
                    {
                        lastClosestEdge = closestEdge;
                        lastClosestCell = closestCell;
                        PerformAction(null, closestEdge, closestCell);
                    }
                }
            }
        }

        void LoopDisplaySilverlight_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            clicking = true;
            e.Handled = true;
            OnMouseButtonDown(e, true);
        }

        void LoopDisplaySilverlight_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.CaptureMouse();
            clicking = true;
            OnMouseButtonDown(e, false);
        }
        HashSet<int> markedEdges = new HashSet<int>();

        internal void OnKeyDown(KeyEventArgs e)
        {

            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Z)
            {
                if (this.undoTree.CanUndo)
                {
                    this.undoTree.Undo();
                    this.UpdateChildControls();
                }
                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Y)
            {
                if (this.undoTree.CanRedo)
                {
                    this.undoTree.Redo();
                    this.UpdateChildControls();
                }
                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.F)
            {
                FixPosition();
                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && (e.Key == Key.R || e.Key==Key.M))
            {
                ResetToFixed();
                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.U)
            {
                Unfix();
                e.Handled = true;
            }
        }

        internal void Unfix()
        {
            this.undoTree.Do(new FixAction(this, false));
            this.UpdateChildControls();
        }

        private void ResetToFixed()
        {
            this.undoTree.RevertToMark();
            this.UpdateChildControls();
        }

        private void FixPosition()
        {
            this.undoTree.Do(new FixAction(this, true));
            this.UpdateChildControls();
        }

        
        public static readonly DependencyProperty MeshProperty = DependencyProperty.Register("Mesh", typeof(Mesh), typeof(LoopDisplaySilverlight), new PropertyMetadata(new PropertyChangedCallback(OnMeshChanged)));

        public Mesh Mesh
        {
            get
            {
                return (Mesh)this.GetValue(MeshProperty);
            }
            set
            {
                this.SetValue(MeshProperty, value);
                this.InvalidateMeasure();
            }
        }

        
        private double scaleFactor = 20;
        private double xOffset = 10;
        private double yOffset = 10;

        private static void OnMeshChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
        {
            ((LoopDisplaySilverlight)source).OnMeshChanged((Mesh)args.NewValue);
        }

        private void OnScaleChanged(Size res)
        {
            EnsureChildControls();
            double sf = scaleFactor;
            double xo = xOffset;
            double yo = yOffset;
            UpdateScaleFactor(this.Mesh, res);
            if (sf != scaleFactor || xo != xOffset || yo != yOffset)
            {
                UpdateChildControls();
           }
        }

        private void UpdateChildControls()
        {
            int childIndex = 0;
            for (int i = 0; i < this.Mesh.Edges.Count; i++)
            {
                Edge edge = this.Mesh.Edges[i];
                EdgeDisplay edgeDisplay = (EdgeDisplay)this.Children[childIndex];
                float x1, x2, y1, y2;
                this.Mesh.GetEdgeExtent(edge, out x1, out y1, out x2, out y2);
                edgeDisplay.XDiff = Math.Abs(x2 - x1) * scaleFactor;
                edgeDisplay.Width = edgeDisplay.XDiff + 1;
                edgeDisplay.YDiff = Math.Abs(y2 - y1) * scaleFactor;
                edgeDisplay.Height = edgeDisplay.YDiff + 1;
                edgeDisplay.EdgeState = edge.State;
                edgeDisplay.EdgeColor = edge.Color;
                edgeDisplay.Marked = markedEdges.Contains(i);
                edgeDisplay.NegGradient = x1 == x2 ? false : (y2 - y1) / (x2 - x1) < 0;
                edgeDisplay.SetValue(Canvas.TopProperty, Math.Min(y2, y1) * scaleFactor + yOffset);
                edgeDisplay.SetValue(Canvas.LeftProperty, Math.Min(x2, x1) * scaleFactor + xOffset);
                childIndex++;
            }
            for (int i = 0; i < this.Mesh.Intersections.Count; i++)
            {
                Intersection inters = this.Mesh.Intersections[i];
                Ellipse ellipse = (Ellipse)this.Children[childIndex];
                ellipse.Width = 4;
                ellipse.Height = 4;
                ellipse.SetValue(Canvas.TopProperty, inters.Y * scaleFactor + yOffset - ellipse.Height / 2.0);
                ellipse.SetValue(Canvas.LeftProperty, inters.X * scaleFactor + xOffset - ellipse.Width / 2.0);
                ellipse.Stroke = new SolidColorBrush(Colors.Black);
                ellipse.Fill = new SolidColorBrush(Colors.Black);
                ellipse.StrokeThickness = 1.0;
                childIndex++;
            }
            for (int i = 0; i < this.Mesh.Cells.Count; i++)
            {
                Cell cell = this.Mesh.Cells[i];
                CellDisplay cellDisplay = (CellDisplay)this.Children[childIndex];
                PointCollection points = new PointCollection();
                for (int j = 0; j < cell.Intersections.Count; j++)
                {
                    Intersection inters = this.Mesh.Intersections[cell.Intersections[j]];
                    points.Add(new Point(inters.X * scaleFactor + xOffset, inters.Y * scaleFactor + yOffset));
                }
                cellDisplay.Points = points;
                cellDisplay.FontFamily = new FontFamily("Tahoma");
                cellDisplay.FontSize = 18;
                cellDisplay.CellColor = cell.Color;
                cellDisplay.TargetCount = cell.TargetCount;
                cellDisplay.Width = scaleFactor;
                cellDisplay.Height = scaleFactor;
                childIndex++;
            }
        }

        private void EnsureChildControls()
        {
            if (Children.Count > 0)
                return;
            OnMeshChanged(this.Mesh);
        }

        private void OnMeshChanged(Mesh newMesh)
        {
            markedEdges.Clear();
            undoTree = new UndoTree();
            if (double.IsNaN(this.ActualHeight) || double.IsNaN(this.ActualWidth))
                return;
            UpdateScaleFactor(newMesh, new Size(this.ActualWidth, this.ActualHeight));
  //          RectangleGeometry rectGeo = new RectangleGeometry();
    //        rectGeo.Rect = new Rect(0.0, 0.0, this.ActualWidth, this.ActualHeight);
      //      this.Clip = rectGeo;
            this.Children.Clear();
            if (newMesh == null)
                return;
            for (int i = 0; i < newMesh.Edges.Count; i++)
            {
                Edge edge = newMesh.Edges[i];
                float x1,x2,y1,y2;
                newMesh.GetEdgeExtent(edge, out x1, out y1, out x2, out y2);
                EdgeDisplay edgeDisplay = new EdgeDisplay();
                edgeDisplay.XDiff = Math.Abs(x2 - x1)*scaleFactor;
                edgeDisplay.Width = edgeDisplay.XDiff + 1;
                edgeDisplay.YDiff = Math.Abs(y2 - y1) * scaleFactor;
                edgeDisplay.Height = edgeDisplay.YDiff + 1;
                edgeDisplay.EdgeState = edge.State;
                edgeDisplay.EdgeColor = edge.Color;
                edgeDisplay.Marked = markedEdges.Contains(i);
                edgeDisplay.NegGradient = x1 == x2 ? false : (y2 - y1) / (x2 - x1) < 0;
                edgeDisplay.SetValue(Canvas.TopProperty, Math.Min(y2, y1) * scaleFactor + yOffset);
                edgeDisplay.SetValue(Canvas.LeftProperty, Math.Min(x2, x1) * scaleFactor + xOffset);
                this.Children.Add(edgeDisplay);
            }
            for (int i = 0; i < newMesh.Intersections.Count; i++)
            {
                Intersection inters = newMesh.Intersections[i];
                Ellipse ellipse = new Ellipse();
                ellipse.Width = 4;
                ellipse.Height = 4;
                ellipse.SetValue(Canvas.TopProperty, inters.Y * scaleFactor + yOffset - ellipse.Height / 2.0);
                ellipse.SetValue(Canvas.LeftProperty, inters.X * scaleFactor + xOffset - ellipse.Width / 2.0);
                ellipse.Stroke = new SolidColorBrush(Colors.Black);
                ellipse.Fill = new SolidColorBrush(Colors.Black);
                ellipse.StrokeThickness = 1.0;
                this.Children.Add(ellipse);
            }
            for (int i = 0; i < newMesh.Cells.Count; i++)
            {
                Cell cell = newMesh.Cells[i];
                CellDisplay cellDisplay = new CellDisplay();
                PointCollection points = new PointCollection();
                for (int j = 0; j < cell.Intersections.Count; j++)
                {
                    Intersection inters = newMesh.Intersections[cell.Intersections[j]];
                    points.Add(new Point(inters.X * scaleFactor + xOffset, inters.Y * scaleFactor + yOffset));
                }
                cellDisplay.Points = points;
                cellDisplay.FontFamily = new FontFamily("Tahoma");
                cellDisplay.FontSize = 18;
                cellDisplay.CellColor = cell.Color;
                cellDisplay.TargetCount = cell.TargetCount;
                cellDisplay.Width = scaleFactor;
                cellDisplay.Height = scaleFactor;
                this.Children.Add(cellDisplay);
            }

        }

        private void UpdateScaleFactor(Mesh newMesh, Size res)
        {
            if (newMesh == null)
                return;
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;
            for (int i = 0; i < newMesh.Intersections.Count; i++)
            {
                Intersection inters = newMesh.Intersections[i];
                if (inters.X < minX)
                    minX = inters.X;
                if (inters.Y < minY)
                    minY = inters.Y;
                if (inters.X > maxX)
                    maxX = inters.X;
                if (inters.Y > maxY)
                    maxY = inters.Y;
            }
            scaleFactor = (res.Height - this.Margin.Top - this.Margin.Bottom) / (maxY - minY + 1);
            scaleFactor = Math.Min(scaleFactor, (res.Width - this.Margin.Left - this.Margin.Right) / (maxX - minX + 1));
            scaleFactor = Math.Max(scaleFactor, 25.0);
            scaleFactor = Math.Min(scaleFactor, 50.0);
            yOffset = res.Height / 2.0 - (maxY + minY) * scaleFactor / 2.0;
            xOffset = res.Width / 2.0 - (maxX + minX) * scaleFactor / 2.0;
        }

        int lastControl = -1;
        int lastShift = -1;


        private double DistanceToSegment(double realX, double realY, double sx, double sy, double ex, double ey)
        {
            double dx = ex - sx;
            double dy = ey - sy;
            double t = ((realX - sx) * dx + (realY - sy) * dy) / (dx * dx + dy * dy);
            double nearX;
            double nearY;
            if (t < 0)
            {
                nearX = sx;
                nearY = sy;
            }
            else if (t > 1)
            {
                nearX = ex;
                nearY = ey;
            }
            else
            {
                nearX = sx + t * dx;
                nearY = sy + t * dy;
            }
            double distx = nearX - realX;
            double disty = nearY - realY;
            return Math.Sqrt(distx * distx + disty * disty);
        }

        private bool showCellColors = false;
        public bool ShowColors
        {
            get
            {
                return showColors;
            }
            set
            {
                showColors = value;
            }
        }
        private bool showColors = false;
        private bool noToggle = false;
        private int autoMove = 0;
        private bool disallowFalseMove = false;
        private bool useICInAuto = false;
        private bool considerMultipleLoopsInAuto = false;
        private bool useCellColoringInAuto = false;
        private bool useColoringInAuto = false;


        private UndoTree undoTree = new UndoTree();


        internal void OnMouseButtonDown(MouseButtonEventArgs e, bool right)
        {
            Point clickLoc = e.GetPosition(this);
            OnMouseButtonDown(clickLoc.X, clickLoc.Y, right);
        }
        internal void OnMouseButtonDown(double clickX, double clickY, bool right)
        {
            bool shiftPressed = ((Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None);
            bool controlPressed = ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None);
            if (!ShowColors && shiftPressed)
                right = !right;
            else if (!ShowColors && (shiftPressed || controlPressed))
                return;
            if (ShowColors && !shiftPressed)
                lastShift = -1;
            if (ShowColors && !controlPressed)
                lastControl = -1;
            int closestEdge;
            int closestCell;
            FindClosest(clickX, clickY, out closestEdge, out closestCell, 0.0f);
            if (closestEdge != -1 || closestCell != -1)
            {
                lastClosestEdge = closestEdge;
                lastClosestCell = closestCell;
            }
            PerformAction(right, closestEdge, closestCell);
        }
        int lastClosestEdge = -1;
        int lastClosestCell = -1;

        private void FindClosest(double clickX, double clickY, out int closestEdge, out int closestCell, float minSep)
        {
            double linkLength = scaleFactor;
            double x = clickX - xOffset;
            double y = clickY - yOffset;
            double realX = x / linkLength;
            double realY = y / linkLength;
            closestEdge = -1;
            closestCell = -1;
            double distance = 1;
            double secondDistance = 200;
            for (int i = 0; i < this.Mesh.Edges.Count; i++)
            {
                Edge edge = this.Mesh.Edges[i];
                float sx, sy, ex, ey;
                this.Mesh.GetEdgeExtent(edge, out sx, out sy, out ex, out ey);
                double curDist = DistanceToSegment(realX, realY, sx, sy, ex, ey);
                if (curDist <= distance)
                {
                    closestEdge = i;
                    secondDistance = distance;
                    distance = curDist;
                }
                else if (curDist <= secondDistance)
                {
                    secondDistance = curDist;
                }
            }
            if (showCellColors)
            {
                for (int i = 0; i < this.Mesh.Cells.Count; i++)
                {
                    float cx = 0.0F;
                    float cy = 0.0F;
                    foreach (int inters in Mesh.Cells[i].Intersections)
                    {
                        cx += Mesh.Intersections[inters].X;
                        cy += Mesh.Intersections[inters].Y;
                    }
                    cx /= Mesh.Cells[i].Intersections.Count;
                    cy /= Mesh.Cells[i].Intersections.Count;
                    double distx = cx - realX;
                    double disty = cy - realY;
                    double curDist = Math.Sqrt(distx * distx + disty * disty);
                    if (curDist <= distance)
                    {
                        closestEdge = -1;
                        closestCell = i;
                        secondDistance = distance;
                        distance = curDist;
                    }
                    else if (curDist <= secondDistance)
                    {
                        secondDistance = curDist;
                    }
                }
            }
            if (distance+minSep > secondDistance)
            {
                closestEdge = -1;
                closestCell = -1;
            }
        }


        EdgeState lastState = EdgeState.Filled;

        private void PerformAction(bool? right, int closestEdge, int closestCell)
        {
            if (closestEdge != -1)
            {
                if (markedEdges.Contains(closestEdge))
                    return;
                if (noToggle && Mesh.Edges[closestEdge].State != EdgeState.Empty)
                    return;
                /*if (shiftPressed || controlPressed)
                {
                    if (shiftPressed)
                    {
                        if (lastShift == -1)
                            lastShift = closestEdge;
                        else
                        {
                            ColorJoinAction colorAction = new ColorJoinAction(Mesh, lastShift, closestEdge, true);
                            if (lastShift != closestEdge)
                                undoTree.Do(colorAction);
                            lastShift = -1;
                        }
                    }
                    else if (controlPressed)
                    {
                        if (lastControl == -1)
                            lastControl = closestEdge;
                        else
                        {
                            ColorJoinAction colorAction = new ColorJoinAction(Mesh, lastControl, closestEdge, false);
                            if (lastControl != closestEdge)
                                undoTree.Do(colorAction);
                            lastControl = -1;
                        }
                    }
                    UpdateChildControls();
                    return;
                }*/
                LoopClickAction action;
                if (right.HasValue)
                {
                    action = new LoopClickAction(Mesh, closestEdge, right.Value, autoMove, disallowFalseMove, useICInAuto, considerMultipleLoopsInAuto, useColoringInAuto, useCellColoringInAuto);
                }
                else
                {
                    if (Mesh.Edges[closestEdge].State == lastState)
                        return;
                    bool pretendRight = false;
                    switch (lastState)
                    {
                        case EdgeState.Filled:
                            if (Mesh.Edges[closestEdge].State == EdgeState.Excluded)
                                pretendRight = true;
                            break;
                        case EdgeState.Excluded:
                            if (Mesh.Edges[closestEdge].State == EdgeState.Empty)
                                pretendRight = true;
                            break;
                        case EdgeState.Empty:
                            if (Mesh.Edges[closestEdge].State == EdgeState.Filled)
                                pretendRight = true;
                            break;
                    }
                    action = new LoopClickAction(Mesh, closestEdge, pretendRight, autoMove, disallowFalseMove, useICInAuto, considerMultipleLoopsInAuto, useColoringInAuto, useCellColoringInAuto);
                }
                if (!undoTree.Do(action))
                {
                    /*
                    redEdge = closestEdge;
                    Thread thread = new Thread(new ThreadStart(ClearRed));
                    thread.IsBackground = true;
                    thread.Start();
                     */
                }
                else if (noToggle)
                {
                    /*
                    if (MovePerformed != null)
                        MovePerformed(this, new MoveEventArgs(closestEdge, e.Button == MouseButtons.Left));
                     * */
                }
                else
                {
                    if (right.HasValue)
                    {
                        lastState = Mesh.Edges[closestEdge].State;
                    }
                    bool satisified = true;
                    for (int i = 0; i < Mesh.Cells.Count; i++)
                    {
                        if (Mesh.Cells[i].TargetCount >= 0 && Mesh.Cells[i].FilledCount != Mesh.Cells[i].TargetCount)
                            satisified = false;
                    }
                    if (satisified)
                    {
                        Mesh copy = new Mesh(Mesh);
                        try
                        {
                            copy.Clear();
                            bool failed = false;
                            if (copy.TrySolve() != SolveState.Solved)
                            {
                                copy.SolverMethod = SolverMethod.Recursive;
                                copy.UseIntersectCellInteractsInSolver = false;
                                copy.UseCellColoringTrials = false;
                                copy.UseCellColoring = true;
                                copy.UseCellPairs = false;
                                copy.UseCellPairsTopLevel = true;
                                copy.UseColoring = true;
                                copy.UseDerivedColoring = true;
                                copy.UseEdgeRestricts = true;
                                copy.UseMerging = true;
                                copy.ConsiderMultipleLoops = true;
                                copy.ColoringCheats = true;
                                if (copy.TrySolve() != SolveState.Solved)
                                {
                                    failed = true;
                                }
                            }
                            if (!failed) 
                            {
                                bool done = true;
                                for (int i = 0; i < Mesh.Edges.Count; i++)
                                {
                                    if (copy.SolutionFound.Edges[i].State == EdgeState.Filled)
                                    {
                                        if (Mesh.Edges[i].State != EdgeState.Filled)
                                            done = false;
                                    }
                                    else if (Mesh.Edges[i].State == EdgeState.Filled)
                                        done = false;
                                }
                                if (done)
                                {
                                    FixPosition();
                                    copy.FullClear();
                                    ThreadPool.QueueUserWorkItem(new WaitCallback(delegate(object o) { GenerateNew(copy); }));
                                }
                            }
                        }
                        catch
                        {
                        }
                    }
                }
                UpdateChildControls();
            }
            else if (closestCell != -1)
            {
                if (noToggle && Mesh.Cells[closestCell].Color != 0)
                    return;
                CellClickAction action = new CellClickAction(Mesh, closestCell, right.Value);
                undoTree.Do(action);
                UpdateChildControls();
            }
        }

        public event EventHandler<ProgressEventArgs> Generation;

        private int currentPruned = 0;
        private int targetPruned = 0;
        private void GenerateNew(Mesh copy)
        {
            targetPruned = copy.Cells.Count;
            currentPruned = 0;
            OnGenerate(new ProgressEventArgs(currentPruned, targetPruned));
            copy.PrunedCountProgress += new EventHandler(copy_PrunedCountProgress);
            copy.Generate();
            OnGenerate(new ProgressEventArgs(targetPruned, targetPruned));
            this.Dispatcher.BeginInvoke(delegate()
            {
                this.Mesh = copy;
            });
        }

        private void OnGenerate(ProgressEventArgs progressEventArgs)
        {
            if (Generation != null)
                Generation(this, progressEventArgs);
        }

        void copy_PrunedCountProgress(object sender, EventArgs e)
        {
            currentPruned++;
            OnGenerate(new ProgressEventArgs(currentPruned, targetPruned));
        }

        internal List<int> FixPositionInternal(bool fix, out object lastMark)
        {
            List<int> result = markedEdges.ToList();
            markedEdges.Clear();
            if (fix)
            {
                for (int i = 0; i < this.Mesh.Edges.Count; i++)
                {
                    if (this.Mesh.Edges[i].State != EdgeState.Empty)
                        markedEdges.Add(i);
                }
                lastMark = this.undoTree.MarkNext();
            }
            else
            {
                lastMark = this.undoTree.ClearMark();
            }
            return result;
        }

        internal void SetMarkedDirectInternal(List<int> prevMarked)
        {
            markedEdges.Clear();
            markedEdges.UnionWith(prevMarked);
        }

        internal void SetPrevMarkPos(object prevMarkPos)
        {
            this.undoTree.SetMarkedDirect(prevMarkPos);
        }

        internal void Undo()
        {
            if (undoTree.CanUndo)
            {
                undoTree.Undo();
                UpdateChildControls();
            }
        }

        internal void Redo()
        {
            if (undoTree.CanRedo)
            {
                undoTree.Redo();
                UpdateChildControls();
            }
        }


        internal void Fix()
        {
            FixPosition();
        }

        internal void RevertToFix()
        {
            ResetToFixed();
        }
    }

    class FixAction : IAction
    {
        public FixAction(LoopDisplaySilverlight display, bool fix)
        {
            this.display = display;
            this.fix = fix;
        }
        LoopDisplaySilverlight display;
        bool fix = true;
        List<int> prevMarked;
        object prevMarkPos;

        public string Name
        {
            get { return fix ? "Fix Position" : "Unfix Position"; }
        }

        public bool Successful
        {
            get { return true; }
        }

        public bool Perform()
        {
            prevMarked = display.FixPositionInternal(fix, out prevMarkPos);
            return true;
        }

        public void Unperform()
        {
            display.SetMarkedDirectInternal(prevMarked);
            display.SetPrevMarkPos(prevMarkPos);
        }

        public bool Equals(IAction other)
        {
            FixAction realOther = other as FixAction;
            if (realOther == null)
                return false;
            return realOther.display == this.display && realOther.fix == this.fix;
        }
    }

    class LoopClickAction : IAction
    {
        public LoopClickAction(Mesh mesh, int edgeIndex, bool buttons, int autoMove, bool disallowFalseMove, bool useICInAuto, bool considerMultipleLoopsInAuto, bool useColoringInAuto, bool useCellColoringInAuto)
        {
            this.mesh = mesh;
            this.edgeIndex = edgeIndex;
            this.buttons = buttons;
            this.autoMove = autoMove;
            this.disallowFalseMove = disallowFalseMove;
            this.useICInAuto = useICInAuto;
            this.considerMultipleLoopsInAuto = considerMultipleLoopsInAuto;
            this.useColoringInAuto = useColoringInAuto;
            this.useCellColoringInAuto = useCellColoringInAuto;
        }

        Mesh mesh;
        int edgeIndex;
        bool buttons;
        int autoMove;
        bool disallowFalseMove;
        bool useICInAuto;
        bool considerMultipleLoopsInAuto;
        bool useColoringInAuto;
        bool useCellColoringInAuto;

        List<IAction> actionsPerformed;

        public bool Successful
        {
            get
            {
                return successful;
            }
        }
        private bool successful;



        private EdgeState Toggle(EdgeState loopLinkState, bool mouseButtons)
        {
            if (!mouseButtons)
            {
                if (loopLinkState == EdgeState.Filled)
                    return EdgeState.Excluded;
                else if (loopLinkState == EdgeState.Excluded)
                    return EdgeState.Empty;
                else
                    return EdgeState.Filled;
            }
            else
            {
                if (loopLinkState == EdgeState.Excluded)
                    return EdgeState.Filled;
                else if (loopLinkState == EdgeState.Filled)
                    return EdgeState.Empty;
                else
                    return EdgeState.Excluded;
            }
        }
        #region IAction Members

        public string Name
        {
            get
            {
                string clickName = string.Empty;
                if (!buttons)
                    clickName = "Left Click";
                else
                    clickName = "Right Click";
                return clickName + " Edge: " + edgeIndex.ToString();
            }
        }

        public bool Perform()
        {
            successful = true;
            mesh.ConsiderIntersectCellInteractsAsSimple = useICInAuto;
            mesh.ConsiderMultipleLoops = considerMultipleLoopsInAuto;
            mesh.UseColoring = useColoringInAuto;
            mesh.UseCellColoring = useCellColoringInAuto;
            // TODO: add settings for use derived/merge in auto.
            mesh.UseDerivedColoring = false;
            mesh.UseMerging = false;
            mesh.ColoringCheats = false;
            Edge closest = mesh.Edges[edgeIndex];
            EdgeState toggled = Toggle(closest.State, buttons);
            actionsPerformed = new List<IAction>();
            if (closest.State != EdgeState.Empty)
            {
                IAction unsetAction = new UnsetAction(mesh, edgeIndex);
                bool res = unsetAction.Perform();
                if ((!res || !unsetAction.Successful) && disallowFalseMove)
                {
                    if (res && !unsetAction.Successful)
                        actionsPerformed.Add(unsetAction);
                    Unperform();
                    return false;
                }
                else if (!res || !unsetAction.Successful)
                    successful = false;
                actionsPerformed.Add(unsetAction);
            }
            if (toggled != EdgeState.Empty)
            {
                bool res = mesh.Perform(edgeIndex, toggled, actionsPerformed, autoMove);
                if (!res && disallowFalseMove)
                {
                    Unperform();
                    return false;
                }
                else if (!res)
                    successful = false;
            }
            return true;
        }

        public void Unperform()
        {
            if (actionsPerformed.Count > 0)
            {
                mesh.Unperform(actionsPerformed);
            }
        }

        #endregion

        #region IEquatable<IAction> Members

        public bool Equals(IAction other)
        {
            LoopClickAction realOther = other as LoopClickAction;
            if (realOther == null)
                return false;
            if (this.mesh == realOther.mesh &&
                realOther.buttons == this.buttons &&
                realOther.edgeIndex == this.edgeIndex &&
                this.autoMove == realOther.autoMove &&
                this.considerMultipleLoopsInAuto == realOther.considerMultipleLoopsInAuto &&
                this.useICInAuto == realOther.useICInAuto &&
                this.useColoringInAuto == realOther.useColoringInAuto &&
                this.disallowFalseMove == realOther.disallowFalseMove)
                return true;
            return false;
        }

        #endregion
    }

    class CellClickAction : IAction
    {
        public CellClickAction(Mesh mesh, int cellIndex, bool buttons)
        {
            this.mesh = mesh;
            this.cellIndex = cellIndex;
            this.buttons = buttons;
        }

        Mesh mesh;
        int cellIndex;
        bool buttons;

        List<IAction> actionsPerformed;

        public bool Successful
        {
            get
            {
                return successful;
            }
        }
        private bool successful;



        private int Toggle(int color, bool mouseButtons)
        {
            if (!mouseButtons)
            {
                if (color == 1)
                    return -1;
                else if (color == -1)
                    return 0;
                return 1;
            }
            else
            {
                if (color == 1)
                    return 0;
                else if (color == -1)
                    return 1;
                return -1;
            }
        }
        #region IAction Members

        public string Name
        {
            get
            {
                string clickName = string.Empty;
                if (!buttons)
                    clickName = "Left Click";
                else 
                    clickName = "Right Click";
                return clickName + " Cell: " + cellIndex.ToString();
            }
        }

        public bool Perform()
        {
            successful = true;
            Cell closest = mesh.Cells[cellIndex];
            int newColor = Toggle(closest.Color, buttons);
            actionsPerformed = new List<IAction>();
            if (closest.Color != 0)
            {
                IAction unsetAction = new CellColorClearAction(mesh, cellIndex);
                bool res = unsetAction.Perform();
                if ((!res || !unsetAction.Successful))
                {
                    if (res && !unsetAction.Successful)
                        actionsPerformed.Add(unsetAction);
                    Unperform();
                    return false;
                }
                actionsPerformed.Add(unsetAction);
            }
            if (newColor != 0)
            {
                IAction setAction = new CellColorJoinAction(mesh, cellIndex, -1, newColor == 1);
                bool res = setAction.Perform();
                if ((!res || !setAction.Successful))
                {
                    if (res && !setAction.Successful)
                        actionsPerformed.Add(setAction);
                    Unperform();
                    return false;
                }
                actionsPerformed.Add(setAction);
            }
            return true;
        }

        public void Unperform()
        {
            if (actionsPerformed.Count > 0)
            {
                mesh.Unperform(actionsPerformed);
            }
        }

        #endregion

        #region IEquatable<IAction> Members

        public bool Equals(IAction other)
        {
            CellClickAction realOther = other as CellClickAction;
            if (realOther == null)
                return false;
            if (this.mesh == realOther.mesh &&
                realOther.buttons == this.buttons &&
                realOther.cellIndex == this.cellIndex)
                return true;
            return false;
        }

        #endregion
    }
}
