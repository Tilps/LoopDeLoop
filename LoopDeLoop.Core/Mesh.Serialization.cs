using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace LoopDeLoop
{
    public partial class Mesh
    {
        public bool LoadFromText(ReadOnlySpan<char> text)
        {
            List<string> lines = new List<string>();
            foreach (var line in text.EnumerateLines())
            {
                lines.Add(line.ToString());
            }
            return LoadFromText(lines.ToArray());
        }

        public bool LoadFromText(string[] lines)
        {
            if (lines == null || lines.Length < 2)
                return false;

            Intersections.Clear();
            Edges.Clear();
            Cells.Clear();
            int curLine = 1;
            if (lines[curLine] != "Intersections")
                return false;
            curLine++;
            if (!int.TryParse(lines[curLine].AsSpan().Trim(), out int intersectionCount))
                return false;
            curLine++;
            for (int i = 0; i < intersectionCount; i++)
            {
                ReadOnlySpan<char> span = lines[curLine].AsSpan().Trim();
                int sp = span.IndexOf(' ');
                if (sp < 0) return false;
                Intersection inters = new Intersection();
                inters.X = float.Parse(span[..sp], CultureInfo.InvariantCulture);
                inters.Y = float.Parse(span[(sp + 1)..].TrimStart(), CultureInfo.InvariantCulture);
                Intersections.Add(inters);
                curLine++;
            }
            if (lines[curLine] != "Edges")
                return false;
            curLine++;
            if (!int.TryParse(lines[curLine].AsSpan().Trim(), out int edgeCount))
                return false;
            curLine++;
            List<IAction> settings = new List<IAction>();
            for (int i = 0; i < edgeCount; i++)
            {
                ReadOnlySpan<char> span = lines[curLine].AsSpan().Trim();
                int sp1 = span.IndexOf(' ');
                if (sp1 < 0) return false;
                int sp2 = span[(sp1 + 1)..].IndexOf(' ');
                if (sp2 < 0) return false;
                sp2 += sp1 + 1;

                int v1 = int.Parse(span[..sp1]);
                int v2 = int.Parse(span[(sp1 + 1)..sp2]);
                ReadOnlySpan<char> stateSpan = span[(sp2 + 1)..].Trim();
                EdgeState state = Enum.Parse<EdgeState>(stateSpan, true);

                AddEdge(v1, v2);
                if (state != EdgeState.Empty)
                {
                    settings.Add(new SetAction(this, GetEdgeJoining(v1, v2), state));
                }
                curLine++;
            }
            CreateCells();
            FullClear();
            if (lines[curLine] != "Cells")
                return false;
            curLine++;
            if (!int.TryParse(lines[curLine].AsSpan().Trim(), out int cellCount))
                return false;
            curLine++;
            for (int i = 0; i < cellCount; i++)
            {
                AddTarget(Cells[i], int.Parse(lines[curLine].AsSpan().Trim()));
                curLine++;
            }
            if (curLine < lines.Length && lines[curLine] == "EdgeColorSets")
            {
                curLine++;
                int edgeColorSetsCount = int.Parse(lines[curLine].AsSpan().Trim());
                curLine++;
                for (int i = 0; i < edgeColorSetsCount; i++)
                {
                    curLine++;
                    int edgeColorSetCount = int.Parse(lines[curLine].AsSpan().Trim());
                    curLine++;
                    int first = -1;
                    int firstColor = 0;
                    for (int j = 0; j < edgeColorSetCount; j++)
                    {
                        ReadOnlySpan<char> span = lines[curLine].AsSpan().Trim();
                        int sp = span.IndexOf(' ');
                        int edge = int.Parse(span[..sp]);
                        int color = int.Parse(span[(sp + 1)..].TrimStart());
                        if (first == -1)
                        {
                            first = edge;
                            firstColor = color;
                        }
                        else
                        {
                            settings.Add(new ColorJoinAction(this, first, edge, color == firstColor));
                        }
                        curLine++;
                    }
                }
            }
            if (curLine < lines.Length && lines[curLine] == "CellColorSets")
            {
                curLine++;
                int cellColorSetsCount = int.Parse(lines[curLine].AsSpan().Trim());
                curLine++;
                for (int i = 0; i < cellColorSetsCount; i++)
                {
                    curLine++;
                    int cellColorSetCount = int.Parse(lines[curLine].AsSpan().Trim());
                    curLine++;
                    int first = -1;
                    int firstColor = 0;
                    for (int j = 0; j < cellColorSetCount; j++)
                    {
                        ReadOnlySpan<char> span = lines[curLine].AsSpan().Trim();
                        int sp = span.IndexOf(' ');
                        int cell = int.Parse(span[..sp]);
                        int color = int.Parse(span[(sp + 1)..].TrimStart());
                        if (Math.Abs(color) != 1)
                        {
                            if (first == -1)
                            {
                                first = cell;
                                firstColor = color;
                            }
                            else
                            {
                                settings.Add(new CellColorJoinAction(this, first, cell, color == firstColor));
                            }
                        }
                        else
                        {
                            settings.Add(new CellColorJoinAction(this, cell, -1, color == 1));
                        }
                        curLine++;
                    }
                }
            }
            if (curLine < lines.Length && lines[curLine] == "EdgePairRestrictions")
            {
                curLine++;
                int edgePairRestrictionCount = int.Parse(lines[curLine].AsSpan().Trim());
                curLine++;
                for (int j = 0; j < edgePairRestrictionCount; j++)
                {
                    ReadOnlySpan<char> span = lines[curLine].AsSpan().Trim();
                    int sp1 = span.IndexOf(' ');
                    int sp2 = span[(sp1 + 1)..].IndexOf(' ');
                    sp2 += sp1 + 1;
                    int edge1 = int.Parse(span[..sp1]);
                    int edge2 = int.Parse(span[(sp1 + 1)..sp2]);
                    EdgePairRestriction restriction = Enum.Parse<EdgePairRestriction>(span[(sp2 + 1)..].Trim(), true);
                    settings.Add(new EdgeRestrictionAction(this, edge1, edge2, restriction));
                    curLine++;
                }
            }
            PerformListRegardless(settings);
            return true;
        }

        public void Save(TextWriter writer)
        {
            writer.WriteLine(MeshType.ToString());
            writer.WriteLine("Intersections");
            writer.WriteLine(Intersections.Count);
            foreach (Intersection inters in Intersections)
            {
                writer.Write(inters.X);
                writer.Write(" ");
                writer.Write(inters.Y);
                writer.WriteLine();
            }
            writer.WriteLine("Edges");
            writer.WriteLine(Edges.Count);
            foreach (Edge edge in Edges)
            {
                writer.Write(edge.Intersections[0]);
                writer.Write(" ");
                writer.Write(edge.Intersections[1]);
                writer.Write(" ");
                writer.WriteLine(edge.State.ToString());
            }
            writer.WriteLine("Cells");
            writer.WriteLine(Cells.Count);
            foreach (Cell cell in Cells)
            {
                writer.WriteLine(cell.TargetCount);
            }
            writer.WriteLine("EdgeColorSets");
            writer.WriteLine(colorSets.Count);
            foreach (List<int> colorSet in colorSets)
            {
                writer.WriteLine("EdgeColorSet");
                writer.WriteLine(colorSet.Count);
                foreach (int edge in colorSet)
                {
                    writer.Write(edge);
                    writer.Write(" ");
                    writer.WriteLine(edges[edge].Color);
                }
            }
            writer.WriteLine("CellColorSets");
            writer.WriteLine(cellColorSets.Count);
            foreach (List<int> colorSet in cellColorSets)
            {
                writer.WriteLine("CellColorSet");
                writer.WriteLine(colorSet.Count);
                foreach (int cell in colorSet)
                {
                    writer.Write(cell);
                    writer.Write(" ");
                    writer.WriteLine(cells[cell].Color);
                }
            }
            writer.WriteLine("EdgePairRestrictions");
            int counter = 0;
            for (int i = 0; i < edges.Count; i++)
                for (int j = i + 1; j < edges.Count; j++)
                    if (edgePairRestrictions[i, j] != EdgePairRestriction.None)
                        counter++;
            writer.WriteLine(counter);
            for (int i = 0; i < edges.Count; i++)
                for (int j = i + 1; j < edges.Count; j++)
                    if (edgePairRestrictions[i, j] != EdgePairRestriction.None)
                    {
                        writer.Write(i);
                        writer.Write(" ");
                        writer.Write(j);
                        writer.Write(" ");
                        writer.WriteLine(edgePairRestrictions[i, j].ToString());
                    }
        }

        public void Save(StringBuilder writer)
        {
            using (StringWriter sw = new StringWriter(writer))
            {
                Save(sw);
            }
        }

        public string SaveToString()
        {
            using (StringWriter sw = new StringWriter())
            {
                Save(sw);
                return sw.ToString();
            }
        }
    }
}
