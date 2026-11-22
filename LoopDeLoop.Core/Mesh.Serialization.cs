using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace LoopDeLoop
{
    public partial class Mesh
    {
        private const int MaxSerializedIntersections = 50_000;
        private const int MaxSerializedEdges = 50_000;

        public bool LoadFromText(ReadOnlySpan<char> text)
        {
            List<string> lines = new List<string>();
            foreach (var line in text.EnumerateLines())
            {
                lines.Add(line.ToString());
            }
            return LoadFromText(lines.ToArray());
        }

        private static bool TryReadLine(string[] lines, ref int curLine, out ReadOnlySpan<char> line)
        {
            if (curLine < 0 || curLine >= lines.Length)
            {
                line = default;
                return false;
            }
            line = lines[curLine++].AsSpan().Trim();
            return true;
        }

        private static bool TryReadInt(string[] lines, ref int curLine, out int value)
        {
            value = 0;
            return TryReadLine(lines, ref curLine, out var span) && int.TryParse(span, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        public bool LoadFromText(string[] lines)
        {
            if (lines == null || lines.Length < 4)
                return false;

            ReadOnlySpan<char> header = lines[0].AsSpan().Trim();
            if (!Enum.TryParse<MeshType>(header, ignoreCase: true, out MeshType parsedType) || !Enum.IsDefined(parsedType))
                return false;

            int curLine = 1;
            if (!TryReadLine(lines, ref curLine, out var section) || !section.SequenceEqual("Intersections"))
                return false;
            if (!TryReadInt(lines, ref curLine, out int intersectionCount) || intersectionCount < 0 || intersectionCount > MaxSerializedIntersections)
                return false;

            var parsedIntersections = new List<Intersection>(intersectionCount);
            for (int i = 0; i < intersectionCount; i++)
            {
                if (!TryReadLine(lines, ref curLine, out var span))
                    return false;
                int sp = span.IndexOf(' ');
                if (sp <= 0)
                    return false;
                if (!float.TryParse(span[..sp], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) || !float.IsFinite(x))
                    return false;
                if (!float.TryParse(span[(sp + 1)..].TrimStart(), NumberStyles.Float, CultureInfo.InvariantCulture, out float y) || !float.IsFinite(y))
                    return false;
                parsedIntersections.Add(new Intersection { X = x, Y = y });
            }

            if (!TryReadLine(lines, ref curLine, out section) || !section.SequenceEqual("Edges"))
                return false;
            if (!TryReadInt(lines, ref curLine, out int edgeCount) || edgeCount < 0 || edgeCount > MaxSerializedEdges)
                return false;

            var parsedEdges = new List<(int V1, int V2, EdgeState State)>(edgeCount);
            var seenEdgePairs = new HashSet<(int, int)>(edgeCount);
            for (int i = 0; i < edgeCount; i++)
            {
                if (!TryReadLine(lines, ref curLine, out var span))
                    return false;
                int sp1 = span.IndexOf(' ');
                if (sp1 <= 0) return false;
                ReadOnlySpan<char> rest = span[(sp1 + 1)..].TrimStart();
                int sp2 = rest.IndexOf(' ');
                if (sp2 <= 0) return false;

                if (!int.TryParse(span[..sp1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int v1))
                    return false;
                if (!int.TryParse(rest[..sp2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int v2))
                    return false;
                if (v1 < 0 || v1 >= intersectionCount || v2 < 0 || v2 >= intersectionCount || v1 == v2)
                    return false;

                ReadOnlySpan<char> stateSpan = rest[(sp2 + 1)..].Trim();
                if (!Enum.TryParse<EdgeState>(stateSpan, ignoreCase: true, out EdgeState state) || !Enum.IsDefined(state))
                    return false;

                var pair = v1 < v2 ? (v1, v2) : (v2, v1);
                if (!seenEdgePairs.Add(pair))
                    return false;

                parsedEdges.Add((v1, v2, state));
            }

            if (!TryReadLine(lines, ref curLine, out section) || !section.SequenceEqual("Cells"))
                return false;
            if (!TryReadInt(lines, ref curLine, out int cellCount) || cellCount < 0 || cellCount > MaxSerializedEdges)
                return false;

            var parsedCellTargets = new int[cellCount];
            for (int i = 0; i < cellCount; i++)
            {
                if (!TryReadInt(lines, ref curLine, out int target) || target < -1)
                    return false;
                parsedCellTargets[i] = target;
            }

            // Build topology on this instance now that header & geometry are validated.
            meshType = parsedType;
            Intersections.Clear();
            Edges.Clear();
            Cells.Clear();
            Intersections.AddRange(parsedIntersections);

            List<IAction> settings = new List<IAction>();
            for (int i = 0; i < parsedEdges.Count; i++)
            {
                var (v1, v2, state) = parsedEdges[i];
                AddEdge(v1, v2);
                if (state != EdgeState.Empty && TryGetEdgeJoining(v1, v2, out int edgeIdx))
                {
                    settings.Add(new SetAction(this, edgeIdx, state));
                }
            }

            CreateCells();
            FullClear();
            if (cellCount != Cells.Count)
                return false;

            for (int i = 0; i < cellCount; i++)
            {
                if (parsedCellTargets[i] > Cells[i].Edges.Count)
                    return false;
                AddTarget(Cells[i], parsedCellTargets[i]);
            }

            if (curLine < lines.Length && lines[curLine].AsSpan().Trim().SequenceEqual("EdgeColorSets"))
            {
                curLine++;
                if (!TryReadInt(lines, ref curLine, out int edgeColorSetsCount) || edgeColorSetsCount < 0 || edgeColorSetsCount > Edges.Count)
                    return false;
                for (int i = 0; i < edgeColorSetsCount; i++)
                {
                    if (!TryReadLine(lines, ref curLine, out _))
                        return false;
                    if (!TryReadInt(lines, ref curLine, out int edgeColorSetCount) || edgeColorSetCount < 0 || edgeColorSetCount > Edges.Count)
                        return false;
                    int first = -1;
                    int firstColor = 0;
                    for (int j = 0; j < edgeColorSetCount; j++)
                    {
                        if (!TryReadLine(lines, ref curLine, out var span))
                            return false;
                        int sp = span.IndexOf(' ');
                        if (sp <= 0) return false;
                        if (!int.TryParse(span[..sp], NumberStyles.Integer, CultureInfo.InvariantCulture, out int edge) || edge < 0 || edge >= Edges.Count)
                            return false;
                        if (!int.TryParse(span[(sp + 1)..].TrimStart(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int color))
                            return false;
                        if (first == -1)
                        {
                            first = edge;
                            firstColor = color;
                        }
                        else
                        {
                            settings.Add(new ColorJoinAction(this, first, edge, color == firstColor));
                        }
                    }
                }
            }

            if (curLine < lines.Length && lines[curLine].AsSpan().Trim().SequenceEqual("CellColorSets"))
            {
                curLine++;
                if (!TryReadInt(lines, ref curLine, out int cellColorSetsCount) || cellColorSetsCount < 0 || cellColorSetsCount > Cells.Count)
                    return false;
                for (int i = 0; i < cellColorSetsCount; i++)
                {
                    if (!TryReadLine(lines, ref curLine, out _))
                        return false;
                    if (!TryReadInt(lines, ref curLine, out int cellColorSetCount) || cellColorSetCount < 0 || cellColorSetCount > Cells.Count)
                        return false;
                    int first = -1;
                    int firstColor = 0;
                    for (int j = 0; j < cellColorSetCount; j++)
                    {
                        if (!TryReadLine(lines, ref curLine, out var span))
                            return false;
                        int sp = span.IndexOf(' ');
                        if (sp <= 0) return false;
                        if (!int.TryParse(span[..sp], NumberStyles.Integer, CultureInfo.InvariantCulture, out int cell) || cell < 0 || cell >= Cells.Count)
                            return false;
                        if (!int.TryParse(span[(sp + 1)..].TrimStart(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int color))
                            return false;
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
                    }
                }
            }

            if (curLine < lines.Length && lines[curLine].AsSpan().Trim().SequenceEqual("EdgePairRestrictions"))
            {
                curLine++;
                if (!TryReadInt(lines, ref curLine, out int edgePairRestrictionCount) || edgePairRestrictionCount < 0 || edgePairRestrictionCount > MaxSerializedEdges * 4)
                    return false;
                for (int j = 0; j < edgePairRestrictionCount; j++)
                {
                    if (!TryReadLine(lines, ref curLine, out var span))
                        return false;
                    int sp1 = span.IndexOf(' ');
                    if (sp1 <= 0) return false;
                    ReadOnlySpan<char> rest = span[(sp1 + 1)..].TrimStart();
                    int sp2 = rest.IndexOf(' ');
                    if (sp2 <= 0) return false;
                    if (!int.TryParse(span[..sp1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int edge1) || edge1 < 0 || edge1 >= Edges.Count)
                        return false;
                    if (!int.TryParse(rest[..sp2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int edge2) || edge2 < 0 || edge2 >= Edges.Count)
                        return false;
                    if (!Enum.TryParse<EdgePairRestriction>(rest[(sp2 + 1)..].Trim(), ignoreCase: true, out EdgePairRestriction restriction) || !Enum.IsDefined(restriction))
                        return false;
                    settings.Add(new EdgeRestrictionAction(this, edge1, edge2, restriction));
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
                writer.Write(inters.X.ToString(CultureInfo.InvariantCulture));
                writer.Write(" ");
                writer.Write(inters.Y.ToString(CultureInfo.InvariantCulture));
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
