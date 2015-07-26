using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace LoopDeLoop
{

    public partial class LoopDisplay : Control
    {
        public LoopDisplay()
        {
            InitializeComponent();
            Mesh = new Mesh(10, 10, MeshType.Square);
            this.DoubleBuffered = true;
        }

        public override Font Font
        {
            get
            {
                return base.Font;
            }
            set
            {
                base.Font = value;
                CalculateScale();
            }
        }

        public event EventHandler CanUndoRedoMaybeChanged;

        private void OnCanUndoRedoMaybeChanged(EventArgs e)
        {
            if (CanUndoRedoMaybeChanged != null)
                CanUndoRedoMaybeChanged(this, e);
        }

        public Mesh Mesh
        {
            get
            {
                return mesh;
            }
            set
            {
                mesh = value;
                undoTree = new UndoTree();
                undoTree.CanUndoRedoMaybeChanged += new EventHandler(undoTree_CanUndoRedoMaybeChanged);
                if (showColors)
                {
                    BuildPenTable();
                }
                CalculateScale();
           }
        }

        void undoTree_CanUndoRedoMaybeChanged(object sender, EventArgs e)
        {
            OnCanUndoRedoMaybeChanged(e);
        }
        private Mesh mesh;

        public bool ShowColors
        {
            get
            {
                return showColors;
            }
            set
            {
                showColors = value;
                if (showColors)
                {
                    BuildPenTable();
                }
            }
        }
        private bool showColors = false;

        public bool ShowCellColors
        {
            get
            {
                return showCellColors;
            }
            set
            {
                showCellColors = value;
            }
        }
        private bool showCellColors = true;

        public bool ShowCellColorsAdvanced
        {
            get
            {
                return showCellColorsAdvanced;
            }
            set
            {
                showCellColorsAdvanced = value;
            }
        }
        private bool showCellColorsAdvanced = true;

        private void BuildPenTable()
        {
            penTable = new Pen[mesh.Edges.Count + 1];
            List<Color> colorList = new List<Color>();
            colorList.Add(BackColor);
            colorList.Add(Color.Black);
            colorList.Add(ForeColor);
            colorList.Add(Color.LightGray);
            Random rnd = new Random();
            for (int i = 0; i < mesh.Edges.Count; i++)
            {
                Color c;
                int loopCount = 0;
                while (true)
                {
                    c = Color.FromArgb(rnd.Next(256), rnd.Next(256), rnd.Next(256));
                    if (!TooClose(c, colorList, loopCount))
                        break;
                    loopCount++;
                }
                colorList.Add(c);
                if (colorList.Count > 15)
                    colorList.RemoveAt(4);
                penTable[i + 1] = new Pen(c);
            }
        }

        private bool TooClose(Color c, List<Color> colorList, int loopCount)
        {
            if (loopCount > 0)
                if (TooCloseBackgroundForeground(c, colorList[0]))
                    return true;
            for (int i = colorList.Count - 1; i >= loopCount/4; i--)
            {
                if (TooClose(c, colorList[i]))
                    return true;
            }
            // The first color is special, we always want to be different to it.
            return false;
        }

        private int Brightness(Color c)
        {
            /*
             *     Colour Brightness Formula

    The following is the formula suggested by the World Wide Web Consortium (W3C) to determine the brightness of a colour.

    ((Red value X 299) + (Green value X 587) + (Blue value X 114)) / 1000

    The difference between the background brightness, and the foreground brightness should be greater than 125.
             */
            return (c.R * 299 + c.G * 587 + c.B * 114) / 1000;
        }
        private int HueDif(Color a, Color b)
        {
            /*
The formula for hue difference is slightly more complicated.

    (maximum (Red value 1, Red value 2) - minimum (Red value 1, Red value 2)) + (maximum (Green value 1, Green value 2) - minimum (Green value 1, Green value 2)) + (maximum (Blue value 1, Blue value 2) - minimum (Blue value 1, Blue value 2))

    The difference between the background colour and the foreground colour should be greater than 500.

             * */
            return Math.Max(a.R, b.R) - Math.Min(a.R, b.R) + Math.Max(a.G, b.G) - Math.Min(a.G, b.G) + Math.Max(a.B, b.B) - Math.Min(a.B, b.B);
        }

        private bool TooCloseBackgroundForeground(Color c, Color color)
        {
            if (Math.Abs(Brightness(c) - Brightness(color)) < 125)
                return true;
            if (Math.Abs(HueDif(c, color)) < 400)
                return true;
            return false;
        }
        private bool TooClose(Color c, Color color)
        {
            float cr = c.GetBrightness() * c.GetSaturation();
            float or = color.GetBrightness() * c.GetSaturation();
            float ctheta = c.GetHue();
            float otheta = color.GetHue();
            float cz = c.GetBrightness();
            float oz = color.GetBrightness();

            float cx = cr * (float)Math.Cos(ctheta / 360 * 2 * Math.PI);
            float cy = cr * (float)Math.Sin(ctheta / 360 * 2 * Math.PI);
            float ox = or * (float)Math.Cos(otheta / 360 * 2 * Math.PI);
            float oy = or * (float)Math.Sin(otheta / 360 * 2 * Math.PI);
            float difSquare = (float)(Math.Pow(cx - ox, 2) + Math.Pow(cy - oy, 2) + Math.Pow(cz - oz, 2));
            if (difSquare > 0.1)
                return false;
            else
                return true;
            /*
            float hueDif = Math.Abs(c.GetHue() - color.GetHue());
            if (hueDif > 180)
                hueDif = 360 - hueDif;
            float satDif = Math.Abs(c.GetSaturation() - color.GetSaturation());
            float brightDif = Math.Abs(c.GetBrightness() - color.GetBrightness());
            if (hueDif + 10 * satDif + 10 * brightDif < 20)
                return true;
            else
                return false;
             * */
        }

        Pen[] penTable;

        private float scaleSize;

        private int additionalXMargin = 0;
        private int additionalYMargin = 0;

        private void CalculateScale()
        {
            float minX = float.PositiveInfinity;
            float minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float maxY = float.NegativeInfinity;
            foreach (Intersection inter in mesh.Intersections)
            {
                if (inter.X < minX)
                    minX = inter.X;
                if (inter.X > maxX)
                    maxX = inter.X;
                if (inter.Y < minY)
                    minY = inter.Y;
                if (inter.Y > maxY)
                    maxY = inter.Y;
            }
            int minScale = FontHeight + 2*linkBuffer;
            if (mesh.MeshType == MeshType.Triangle)
                minScale *= 2;
            int maxScale = minScale * 3 / 2;
            float meshWidth = maxX - minX;
            float meshHeight = maxY - minY;
            float heightScale = (this.Height - Margin.Top - Margin.Bottom) / meshHeight;
            float widthScale = (this.Width - Margin.Left - Margin.Right) / meshWidth;
            float optScale = Math.Min(heightScale, widthScale);
            scaleSize = Math.Max(Math.Min(optScale, maxScale), minScale);
            float scaledMeshMiddleX = scaleSize * (maxX + minX) / 2;
            float scaledMeshMiddleY = scaleSize * (maxY + minY) / 2;
            float marginNeededX = this.Width / 2 - scaledMeshMiddleX;
            float marginNeededY = this.Height / 2 - scaledMeshMiddleY;
            float scaledMeshLeft = scaleSize * (minX);
            float scaledMeshTop = scaleSize * minY;
            if (scaledMeshLeft + marginNeededX > Margin.Left)
                additionalXMargin = (int)marginNeededX - Margin.Left;
            else
                additionalXMargin = (int)-scaledMeshLeft;
            if (scaledMeshTop + marginNeededY > Margin.Top)
                additionalYMargin = (int)marginNeededY - Margin.Top;
            else
                additionalYMargin = (int)-scaledMeshTop;
            try
            {
                if (Handle != IntPtr.Zero)
                    Refresh();
            }
            catch (ObjectDisposedException)
            {
                // Ignore shutdown errors.
            }
        }

        protected override void OnResize(EventArgs e)
        {
            CalculateScale();
            base.OnResize(e);
        }

        public UndoTree UndoTree
        {
            get
            {
                return undoTree;
            }
        }
        UndoTree undoTree;

        public int AutoMove
        {
            get
            {
                return autoMove;
            }
            set
            {
                autoMove = value;
            }
        }
        private int autoMove = 0;

        public bool UseICInAuto
        {
            get
            {
                return useICInAuto;
            }
            set
            {
                useICInAuto = value;
            }
        }
        private bool useICInAuto;

        public bool UseColoringInAuto
        {
            get
            {
                return useColoringInAuto;
            }
            set
            {
                useColoringInAuto = value;
            }
        }
        private bool useColoringInAuto;

        public bool UseEdgeRestrictsInAuto
        {
            get
            {
                return useEdgeRestrictsInAuto;
            }
            set
            {
                useEdgeRestrictsInAuto = value;
            }
        }
        private bool useEdgeRestrictsInAuto;

        public bool UseCellColoringInAuto
        {
            get
            {
                return useCellColoringInAuto;
            }
            set
            {
                useCellColoringInAuto = value;
            }
        }
        private bool useCellColoringInAuto;

        public bool UseCellPairsInAuto
        {
            get
            {
                return useCellPairsInAuto;
            }
            set
            {
                useCellPairsInAuto = value;
            }
        }
        private bool useCellPairsInAuto;

        public bool ConsiderMultipleLoopsInAuto
        {
            get
            {
                return considerMultipleLoopsInAuto;
            }
            set
            {
                considerMultipleLoopsInAuto = value;
            }
        }
        private bool considerMultipleLoopsInAuto;

        public bool NoToggle
        {
            get
            {
                return noToggle;
            }
            set
            {
                noToggle = value;
            }
        }
        private bool noToggle = false;

        public bool DisallowTriviallyFalse
        {
            get
            {
                return disallowFalseMove;
            }
            set
            {
                disallowFalseMove = value;
            }
        }
        private bool disallowFalseMove = true;

        private const int linkBuffer = 4;

        int lastControl = -1;
        int lastShift = -1;

        public event EventHandler Solved;

        protected override void OnMouseHover(EventArgs e)
        {
            base.OnMouseHover(e);
        }

        int hoveringEdge = -1;

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
#if DEBUG
            int x = this.PointToClient(Control.MousePosition).X - Margin.Left - additionalXMargin;
            int y = this.PointToClient(Control.MousePosition).Y - Margin.Top - additionalYMargin;
            float linkLength = scaleSize;
            float realX = x / linkLength;
            float realY = y / linkLength;
            int closestEdge = -1;
            float distance = 1;
            for (int i = 0; i < mesh.Edges.Count; i++)
            {
                Edge edge = mesh.Edges[i];
                float sx, sy, ex, ey;
                mesh.GetEdgeExtent(edge, out sx, out sy, out ex, out ey);
                float curDist = DistanceToSegment(realX, realY, sx, sy, ex, ey);
                if (curDist <= distance)
                {
                    closestEdge = i;
                    distance = curDist;
                }
            }
            if (hoveringEdge != closestEdge)
            {
                hoveringEdge = closestEdge;
                this.Refresh();
            }
#endif
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            bool shiftPressed = ((Control.ModifierKeys & Keys.Shift) != Keys.None);
            bool controlPressed = ((Control.ModifierKeys & Keys.Control) != Keys.None);
            if (!ShowColors && ( shiftPressed|| controlPressed))
                return;
            if (ShowColors && !shiftPressed)
                lastShift = -1;
            if (ShowColors && !controlPressed)
                lastControl = -1;
            float linkLength = scaleSize ;
            int x = e.X - Margin.Left - additionalXMargin;
            int y = e.Y - Margin.Top - additionalYMargin;
            float realX = x/linkLength;
            float realY = y/linkLength;
            int closestEdge = -1;
            int closestCell = -1;
            float distance = 1;
            for (int i = 0; i < mesh.Edges.Count; i++)
            {
                Edge edge = mesh.Edges[i];
                float sx, sy, ex, ey;
                mesh.GetEdgeExtent(edge, out sx, out sy, out ex, out ey);
                float curDist = DistanceToSegment(realX, realY, sx, sy, ex, ey);
                if (curDist <= distance)
                {
                    closestEdge = i;
                    distance = curDist;
                }
            }
            if (showCellColors)
            {
                for (int i = 0; i < mesh.Cells.Count; i++)
                {
                    float cx = 0.0F;
                    float cy = 0.0F;
                    foreach (int inters in mesh.Cells[i].Intersections)
                    {
                        cx += mesh.Intersections[inters].X;
                        cy += mesh.Intersections[inters].Y;
                    }
                    cx /= mesh.Cells[i].Intersections.Count;
                    cy /= mesh.Cells[i].Intersections.Count;
                    float distx = cx - realX;
                    float disty = cy - realY;
                    float curDist = (float)Math.Sqrt(distx * distx + disty * disty);
                    if (curDist <= distance)
                    {
                        closestEdge = -1;
                        closestCell = i;
                        distance = curDist;
                    }
                }
            }
            if (closestEdge != -1)
            {
                if (noToggle && mesh.Edges[closestEdge].State != EdgeState.Empty)
                    return;
                if (shiftPressed || controlPressed)
                {
                    if (shiftPressed)
                    {
                        if (lastShift == -1)
                            lastShift = closestEdge;
                        else
                        {
                            ColorJoinAction colorAction = new ColorJoinAction(mesh, lastShift, closestEdge, true);
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
                            ColorJoinAction colorAction = new ColorJoinAction(mesh, lastControl, closestEdge, false);
                            if (lastControl != closestEdge)
                                undoTree.Do(colorAction);
                            lastControl = -1;
                        }
                    }
                    this.Refresh();
                    return;
                }
                LoopClickAction action = new LoopClickAction(mesh, closestEdge, e.Button, autoMove, disallowFalseMove, useICInAuto, considerMultipleLoopsInAuto, useColoringInAuto, useCellColoringInAuto, useEdgeRestrictsInAuto, useCellPairsInAuto);
                if (!undoTree.Do(action))
                {
                    redEdge = closestEdge;
                    Thread thread = new Thread(new ThreadStart(ClearRed));
                    thread.IsBackground = true;
                    thread.Start();
                }
                else if (noToggle)
                {
                    if (MovePerformed != null)
                        MovePerformed(this, new MoveEventArgs(closestEdge, e.Button == MouseButtons.Left));
                }
                else
                {
                    bool satisified = true;
                    for (int i = 0; i < mesh.Cells.Count; i++)
                    {
                        if (mesh.Cells[i].TargetCount >= 0 && mesh.Cells[i].FilledCount != mesh.Cells[i].TargetCount)
                            satisified = false;
                    }
                    if (satisified)
                    {
                        Mesh copy = new Mesh(mesh);
                        try
                        {
                            copy.SolverMethod = SolverMethod.Recursive;
                            copy.UseIntersectCellInteractsInSolver = false;
                            copy.Clear();
                            if (copy.TrySolve() == SolveState.Solved)
                            {
                                bool done = true;
                                for (int i = 0; i < mesh.Edges.Count; i++)
                                {
                                    if (copy.SolutionFound.Edges[i].State == EdgeState.Filled)
                                    {
                                        if (mesh.Edges[i].State != EdgeState.Filled)
                                            done = false;
                                    }
                                    else if (mesh.Edges[i].State == EdgeState.Filled)
                                        done = false;
                                }
                                if (done)
                                {
                                    allDone = true;
                                    if (Solved != null)
                                        Solved(this, EventArgs.Empty);
                                    Thread thread = new Thread(new ThreadStart(Done));
                                    thread.IsBackground = true;
                                    thread.Start();
                                }
                            }
                        }
                        catch
                        {
                        }
                    }
                }
                this.Refresh();
            }
            else if (closestCell != -1)
            {
                if (noToggle && mesh.Cells[closestCell].Color != 0)
                    return;
                CellClickAction action = new CellClickAction(mesh, closestCell, e.Button);
                undoTree.Do(action);
                this.Refresh();
            }
            base.OnMouseUp(e);
        }

        bool allDone = false;

        public event MoveEventHandler MovePerformed;

        private void ClearRed()
        {
            try
            {
                int curRed = redEdge;
                Thread.Sleep(1000);
                if (curRed == redEdge)
                    redEdge = -1;
                this.BeginInvoke(new MethodInvoker(Refresh));
            }
            catch (Exception e)
            {
                if (!(e is ThreadAbortException))
                {
                    try
                    {
                        this.BeginInvoke(new ParameterizedThreadStart(LoopDeLoopForm.UnexpectedExceptionMessage), e);
                    }
                    catch
                    {
                    }
                }
            }

        }
        private void Done()
        {
            try
            {
                Thread.Sleep(1000);
                allDone = false;
                this.BeginInvoke(new MethodInvoker(Refresh));
            }
            catch (Exception e)
            {
                if (!(e is ThreadAbortException))
                {
                    try
                    {
                        this.BeginInvoke(new ParameterizedThreadStart(LoopDeLoopForm.UnexpectedExceptionMessage), e);
                    }
                    catch
                    {
                    }
                }
            }
        }
        int redEdge = -1;
        public void RaiseMouseWheel(MouseEventArgs e)
        {
            OnMouseWheel(e);
        }
        int deltaTotal = 0;
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (!noToggle)
            {
                int deltaTest = SystemInformation.MouseWheelScrollDelta;
                if (e.Delta > 0 && deltaTotal > 0)
                    deltaTotal = 0;
                else if (e.Delta < 0 && deltaTotal < 0)
                    deltaTotal = 0;
                deltaTotal -= e.Delta;
                while (deltaTotal > deltaTest)
                {
                    if (undoTree.CanUndo)
                    {
                        undoTree.Undo();
                        this.Refresh();
                    }
                    deltaTotal -= deltaTest;
                }
                while (deltaTotal < -deltaTest)
                {
                    if (undoTree.CanRedo)
                    {
                        undoTree.Redo();
                        this.Refresh();
                    }
                    deltaTotal += deltaTest;
                }
            }
            base.OnMouseWheel(e);
        }

        private float DistanceToSegment(float realX, float realY, float sx, float sy, float ex, float ey)
        {
            float dx = ex - sx;
            float dy = ey - sy;
            float t = ((realX - sx) * dx + (realY - sy) * dy) / (dx * dx + dy * dy);
            float nearX;
            float nearY;
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
            float distx = nearX - realX;
            float disty = nearY - realY;
            return (float)Math.Sqrt(distx * distx + disty * disty);
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            Brush fontBrush = new SolidBrush(this.ForeColor);
            Pen emptyLink = Pens.LightGray;
            Pen redLink = Pens.Red;
            Pen greenLink = Pens.Green;
            Pen fullLink = Pens.Black;
            if (showColors || showCellColors)
                fullLink = new Pen(Color.Black, 3);
            Brush dot = new SolidBrush(this.ForeColor);
            Rectangle border = this.ClientRectangle;
            border.Width = border.Width - 1;
            border.Height = border.Height - 1;
            ControlPaint.DrawVisualStyleBorder(pe.Graphics, border);
            DrawMesh(pe.Graphics, fontBrush, emptyLink, redLink, greenLink, fullLink, dot);
            if (allDone)
            {
                pe.Graphics.DrawString("DONE!", new Font(Font.FontFamily, Font.Size * 3), fontBrush, this.Width / 2 - FontHeight * 3, this.Height / 2 - FontHeight * 3);
            }
            // Calling the base class OnPaint
            base.OnPaint(pe);
        }

        private void DrawMesh(Graphics g, Brush fontBrush, Pen emptyLink, Pen redLink, Pen greenLink, Pen fullLink, Brush dot)
        {
            Font cellColorFont = new Font(Font, FontStyle.Regular);
            Brush posCellColor = Brushes.Blue;
            Brush negCellColor = Brushes.Red;
            Brush outsideCellColor = Brushes.PowderBlue;
            Brush insideCellColor = Brushes.Plum;
            float linkLength = scaleSize;
            foreach (Cell cell in mesh.Cells)
            {
                if (showCellColors && cell.Color != 0 && Math.Abs(cell.Color) <= 1)
                {
                    Brush color = cell.Color == 1 ? outsideCellColor : insideCellColor;
                    float[] xs = new float[cell.Intersections.Count];
                    float[] ys = new float[cell.Intersections.Count];
                    float cx = 0.0F;
                    float cy = 0.0F;
                    for (int i = 0; i < cell.Intersections.Count; i++)
                    {
                        Intersection inters = mesh.Intersections[cell.Intersections[i]];
                        xs[i] = inters.X;
                        ys[i] = inters.Y;
                        cx += inters.X;
                        cy += inters.Y;
                    }
                    cx /= cell.Intersections.Count;
                    cy /= cell.Intersections.Count;
                    for (int i = 0; i < xs.Length; i++)
                    {
                        xs[i] *= 0.8F;
                        xs[i] += 0.2F * cx;
                        ys[i] *= 0.8F;
                        ys[i] += 0.2F * cy;
                        float x = xs[i];
                        float y = ys[i];
                        ScalePoint(linkLength, ref x, ref y);
                        xs[i] = x;
                        ys[i] = y;
                    }
                    Point[] points = new Point[cell.Intersections.Count];
                    for (int i = 0; i < xs.Length; i++)
                    {
                        points[i] = new Point((int)xs[i], (int)ys[i]);
                    }
                    g.FillPolygon(color, points);
                }
            }
            EdgePairRestriction[] restricts = null;
            if (hoveringEdge != -1)
            {
                if (hoveringEdge >= mesh.Edges.Count)
                    hoveringEdge = -1;
                else
                    restricts = mesh.GetEdgePairRestrictionsForEdge(hoveringEdge);
            }
            for (int i = 0; i < mesh.Edges.Count; i++)
            {
                Edge e = mesh.Edges[i];
                float sx, sy, ex, ey;
                mesh.GetEdgeExtent(e, out sx, out sy, out ex, out ey);
                ScalePoint(linkLength, ref sx, ref sy);
                ScalePoint(linkLength, ref ex, ref ey);
                int startPointX = (int)sx;
                int startPointY = (int)sy;
                int endPointX = (int)ex;
                int endPointY = (int)ey;
                Pen toDraw = null;
                if (restricts != null && restricts[i] != EdgePairRestriction.None)
                {
                    if (restricts[i] == EdgePairRestriction.NotBoth)
                        toDraw = redLink;
                    else
                        toDraw = greenLink;
                }
                else if (i == redEdge)
                    toDraw = redLink;
                else if (e.State == EdgeState.Filled)
                    toDraw = fullLink;
                else if (!showColors)
                {
                    toDraw = emptyLink;
                }
                else
                {
                    if (e.State == EdgeState.Excluded)
                        toDraw = emptyLink;
                    else
                    {
                        int color = e.Color;
                        if (color > 0)
                        {
                            toDraw = penTable[color];
                            toDraw.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;
                        }
                        else if (color < 0)
                        {
                            toDraw = penTable[-color];
//                            toDraw.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                            toDraw.DashStyle = System.Drawing.Drawing2D.DashStyle.Custom;
                            toDraw.DashPattern = new float[] { 3,2 };
                        }
                        else
                            toDraw = emptyLink;
                    }
                }
                g.DrawLine(toDraw, startPointX, startPointY, endPointX, endPointY);
                if (e.State == EdgeState.Excluded)
                    DrawX(g, fullLink, (startPointX + endPointX) / 2, (startPointY + endPointY) / 2);
            }
            foreach (Intersection inters in mesh.Intersections)
            {
                float cx = inters.X;
                float cy = inters.Y;
                ScalePoint(linkLength, ref cx, ref cy);
                int elipseX = (int)cx;
                int elipseY = (int)cy;
                g.FillEllipse(dot, elipseX - linkBuffer / 2, elipseY - linkBuffer / 2, linkBuffer, linkBuffer);
            }
            foreach (Cell cell in mesh.Cells)
            {
                if (cell.TargetCount >= 0)
                {
                    int total = cell.Intersections.Count;
                    float cx = 0;
                    float cy = 0;
                    foreach (int inters in cell.Intersections)
                    {
                        Intersection otherInters = mesh.Intersections[inters];
                        cx += otherInters.X;
                        cy += otherInters.Y;
                    }
                    cx /= total;
                    cy /= total;
                    ScalePoint(linkLength, ref cx, ref cy);
                    cx -= FontHeight / 2.0f;
                    cy -= FontHeight / 2.0f;
                    g.DrawString(cell.TargetCount.ToString(), Font, fontBrush, cx, cy);
                }
                if (showCellColors && cell.Color != 0)
                {
                    if (showCellColorsAdvanced && Math.Abs(cell.Color) > 1)
                    {
                        int total = cell.Intersections.Count;
                        float cx = 0;
                        float cy = 0;
                        foreach (int inters in cell.Intersections)
                        {
                            Intersection otherInters = mesh.Intersections[inters];
                            cx += otherInters.X;
                            cy += otherInters.Y;
                        }
                        cx /= total;
                        cy /= total;
                        ScalePoint(linkLength, ref cx, ref cy);
                        cx += FontHeight / 3.5f;
                        cy += FontHeight / 3.5f;
                        g.DrawString(Math.Abs(cell.Color).ToString(), cellColorFont, cell.Color > 0 ? posCellColor : negCellColor, cx, cy);
                    }
                }
            }
        }

        private void ScalePoint(float linkLength, ref float cx, ref float cy)
        {
            cx *= linkLength;
            cy *= linkLength;
            cx += Margin.Left + additionalXMargin;
            cy += Margin.Right + additionalYMargin;
        }

        private static void DrawX(Graphics g, Pen fullLink, int centreX, int centreY)
        {
            g.DrawLine(fullLink, centreX - linkBuffer, centreY - linkBuffer, centreX + linkBuffer , centreY + linkBuffer );
            g.DrawLine(fullLink, centreX + linkBuffer , centreY - linkBuffer , centreX - linkBuffer , centreY + linkBuffer );
        }

        internal void Print(Graphics graphics)
        {
            Brush fontBrush = new SolidBrush(this.ForeColor);
            Pen emptyLink = Pens.LightGray;
            Pen redLink = Pens.Red;
            Pen greenLink = Pens.Green;
            Pen fullLink = Pens.Black;
            Brush dot = new SolidBrush(this.ForeColor);
            DrawMesh(graphics, fontBrush, emptyLink, redLink, greenLink, fullLink, dot);

        }

        internal void Flash(IAction iAction)
        {
            SetAction setAction = (SetAction)iAction;
            redEdge = setAction.EdgeIndex;
            Thread thread = new Thread(new ThreadStart(ClearRed));
            thread.IsBackground = true;
            thread.Start();
            this.Refresh();
        }

        internal void Perform(IAction iAction)
        {
            SetAction setAction = (SetAction)iAction;
            SetAction newAction = new SetAction(mesh, setAction.EdgeIndex, setAction.EdgeState);
            if (!undoTree.Do(newAction))
                throw new Exception("Invalid move suggested.");
            if (newAction.Successful == false)
                throw new Exception("Invalid move suggested.");
            this.Refresh();
        }
    }
    class LoopClickAction : IAction
    {
        public LoopClickAction(Mesh mesh, int edgeIndex, MouseButtons buttons, int autoMove, bool disallowFalseMove, bool useICInAuto, bool considerMultipleLoopsInAuto, bool useColoringInAuto, bool useCellColoringInAuto, bool useEdgeRestrictsInAuto, bool useCellPairsInAuto)
        {
            this.mesh = mesh;
            this.edgeIndex = edgeIndex;
            this.buttons = buttons;
            this.autoMove = autoMove;
            this.disallowFalseMove = disallowFalseMove;
            this.useICInAuto = useICInAuto;
            this.considerMultipleLoopsInAuto = considerMultipleLoopsInAuto;
            this.useColoringInAuto = useColoringInAuto;
            this.useCellPairsInAuto = useCellPairsInAuto;
            this.useEdgeRestrictsInAuto = useEdgeRestrictsInAuto;
            this.useCellColoringInAuto = useCellColoringInAuto;
        }

        Mesh mesh;
        int edgeIndex;
        MouseButtons buttons;
        int autoMove;
        bool disallowFalseMove;
        bool useICInAuto;
        bool considerMultipleLoopsInAuto;
        bool useColoringInAuto;
        bool useCellPairsInAuto;
        bool useEdgeRestrictsInAuto;
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



        private EdgeState Toggle(EdgeState loopLinkState, MouseButtons mouseButtons)
        {
            if (mouseButtons == MouseButtons.Left)
            {
                if (loopLinkState == EdgeState.Filled)
                    return EdgeState.Empty;
                else
                    return EdgeState.Filled;
            }
            else if (mouseButtons == MouseButtons.Right)
            {
                if (loopLinkState == EdgeState.Excluded)
                    return EdgeState.Empty;
                else
                    return EdgeState.Excluded;
            }
            return loopLinkState;
        }
        #region IAction Members

        public string Name
        {
            get
            {
                string clickName = string.Empty;
                if (buttons == MouseButtons.Left)
                    clickName = "Left Click";
                else if (buttons == MouseButtons.Right)
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
            mesh.UseCellPairs = useCellPairsInAuto;
            mesh.UseEdgeRestricts = useEdgeRestrictsInAuto;
            mesh.UseCellColoring = useCellColoringInAuto;
            // TODO: add settings for use derived/merge in auto.
            mesh.UseDerivedColoring = false;
            mesh.UseMerging = false;
            mesh.ColoringCheats = false;
            mesh.UseCellPairsTopLevel = false;
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
                this.useEdgeRestrictsInAuto == realOther.useEdgeRestrictsInAuto &&
                this.useCellColoringInAuto == realOther.useCellColoringInAuto &&
                this.useCellPairsInAuto == realOther.useCellPairsInAuto &&
                this.disallowFalseMove == realOther.disallowFalseMove)
                return true;
            return false;
        }

        #endregion
    }
    class CellClickAction : IAction
    {
        public CellClickAction(Mesh mesh, int cellIndex, MouseButtons buttons)
        {
            this.mesh = mesh;
            this.cellIndex = cellIndex;
            this.buttons = buttons;
        }

        Mesh mesh;
        int cellIndex;
        MouseButtons buttons;

        List<IAction> actionsPerformed;

        public bool Successful
        {
            get
            {
                return successful;
            }
        }
        private bool successful;



        private int Toggle(int color, MouseButtons mouseButtons)
        {
            if (mouseButtons == MouseButtons.Left)
            {
                if (color == 1)
                    return -1;
                else if (color == -1)
                    return 0;
                return 1;
            }
            else if (mouseButtons == MouseButtons.Right)
            {
                if (color == 1)
                    return 0;
                else if (color == -1)
                    return 1;
                return -1;
            }
            return color;
        }
        #region IAction Members

        public string Name
        {
            get
            {
                string clickName = string.Empty;
                if (buttons == MouseButtons.Left)
                    clickName = "Left Click";
                else if (buttons == MouseButtons.Right)
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

    public delegate void MoveEventHandler(object sender, MoveEventArgs args);

    public class MoveEventArgs : EventArgs
    {
        public MoveEventArgs(int edge, bool state)
        {
            this.Edge = edge;
            this.Set = state;
        }
        public int Edge;

        public bool Set;
    }
}
