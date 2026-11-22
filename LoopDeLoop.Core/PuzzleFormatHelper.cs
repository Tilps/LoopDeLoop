using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LoopDeLoop
{
    public static class PuzzleFormatHelper
    {
        private static readonly char[] LineSeparators = ['\r', '\n'];

        public static bool CanExportSquareFormats(Mesh? mesh)
            => mesh != null && TryGetSquareDimensions(mesh, out _, out _);

        public static bool TryGetSquareDimensions(Mesh mesh, out int width, out int height)
        {
            width = 0;
            height = 0;
            if (mesh.MeshType != MeshType.Square && mesh.MeshType != MeshType.SquareSymmetrical)
                return false;

            if (mesh.Cells.Count == 0 || mesh.Intersections.Count < 4)
                return false;

            int v = 0;
            while (v + 1 < mesh.Intersections.Count && TryFindEdgeJoining(mesh, v, v + 1, out _))
            {
                v++;
            }

            if (v <= 0 || mesh.Cells.Count % v != 0)
                return false;

            height = v;
            width = mesh.Cells.Count / height;
            return width > 0 && (width + 1) * (height + 1) == mesh.Intersections.Count;
        }

        private static bool TryFindEdgeJoining(Mesh mesh, int v1, int v2, out int edgeIndex)
        {
            edgeIndex = -1;
            if (v1 < 0 || v2 < 0 || v1 >= mesh.Intersections.Count || v2 >= mesh.Intersections.Count)
                return false;

            var e1 = mesh.Intersections[v1].Edges;
            var e2 = mesh.Intersections[v2].Edges;
            for (int i = 0; i < e1.Count; i++)
            {
                int candidate = e1[i];
                for (int j = 0; j < e2.Count; j++)
                {
                    if (e2[j] == candidate)
                    {
                        edgeIndex = candidate;
                        return true;
                    }
                }
            }
            return false;
        }

        public static string ExportLoopText(Mesh mesh) => mesh.SaveToString();

        public static string? ExportLoopyDescriptor(Mesh mesh)
        {
            if (!TryGetSquareDimensions(mesh, out int width, out int height))
                return null;

            var sb = new StringBuilder();
            sb.Append(width).Append('x').Append(height).Append(':');
            AppendLoopyDesc(mesh, width, height, sb);
            return sb.ToString();
        }

        public static string? ExportLoopySave(Mesh mesh)
        {
            if (!TryGetSquareDimensions(mesh, out int width, out int height))
                return null;

            using var sw = new StringWriter();
            sw.WriteLine("SAVEFILE:41:Simon Tatham's Portable Puzzle Collection");
            sw.WriteLine("VERSION :1:1");
            sw.WriteLine("GAME    :5:Loopy");
            WriteLoopyField(sw, "PARAMS", $"{width}x{height}r0de");
            WriteLoopyField(sw, "CPARAMS", $"{width}x{height}r0de");
            WriteLoopyField(sw, "SEED", "0");

            var descBuilder = new StringBuilder();
            AppendLoopyDesc(mesh, width, height, descBuilder);
            string desc = descBuilder.ToString();
            WriteLoopyField(sw, "DESC", desc);

            bool complete = true;
            var moves = new List<string>();
            var solveBuilder = new StringBuilder("S");
            for (int j = 0; j <= height; j++)
            {
                for (int k = 0; k < 2; k++)
                {
                    for (int i = 0; i <= width; i++)
                    {
                        if (k == 0 && i < width)
                        {
                            int start = i * (height + 1) + j;
                            int end = start + height + 1;
                            if (TryFindEdgeJoining(mesh, start, end, out int edgeIdx))
                            {
                                EdgeState st = mesh.Edges[edgeIdx].State;
                                if (st == EdgeState.Empty)
                                {
                                    complete = false;
                                }
                                else
                                {
                                    string move = $"{i},{j}h{(st == EdgeState.Filled ? 'y' : 'n')}";
                                    solveBuilder.Append(move);
                                    moves.Add(move);
                                }
                            }
                        }
                        else if (k == 1 && j < height)
                        {
                            int start = i * (height + 1) + j;
                            int end = start + 1;
                            if (TryFindEdgeJoining(mesh, start, end, out int edgeIdx))
                            {
                                EdgeState st = mesh.Edges[edgeIdx].State;
                                if (st == EdgeState.Empty)
                                {
                                    complete = false;
                                }
                                else
                                {
                                    string move = $"{i},{j}v{(st == EdgeState.Filled ? 'y' : 'n')}";
                                    solveBuilder.Append(move);
                                    moves.Add(move);
                                }
                            }
                        }
                    }
                }
            }

            int stateCount = 1 + moves.Count + (complete ? 1 : 0);
            WriteLoopyField(sw, "NSTATES", stateCount.ToString());
            WriteLoopyField(sw, "STATEPOS", stateCount.ToString());
            foreach (string move in moves)
            {
                WriteLoopyField(sw, "MOVE", move);
            }
            if (complete)
            {
                WriteLoopyField(sw, "SOLVE", solveBuilder.ToString());
            }

            return sw.ToString();
        }

        public static string? ExportSquareAscii(Mesh mesh)
        {
            if (!TryGetSquareDimensions(mesh, out int width, out int height))
                return null;

            var sb = new StringBuilder();
            for (int j = 0; j <= height; j++)
            {
                for (int k = 0; k < 2; k++)
                {
                    if (k == 1 && j == height)
                        break;

                    for (int i = 0; i <= width; i++)
                    {
                        if (k == 0)
                        {
                            sb.Append('+');
                            if (i < width)
                            {
                                int start = i * (height + 1) + j;
                                int end = start + height + 1;
                                EdgeState st = TryFindEdgeJoining(mesh, start, end, out int idx) ? mesh.Edges[idx].State : EdgeState.Empty;
                                sb.Append(st == EdgeState.Filled ? '-' : (st == EdgeState.Excluded ? 'x' : ' '));
                            }
                        }
                        else
                        {
                            int start = i * (height + 1) + j;
                            int end = start + 1;
                            EdgeState st = TryFindEdgeJoining(mesh, start, end, out int idx) ? mesh.Edges[idx].State : EdgeState.Empty;
                            sb.Append(st == EdgeState.Filled ? '|' : (st == EdgeState.Excluded ? 'x' : ' '));

                            if (i < width)
                            {
                                Cell cell = mesh.Cells[i * height + j];
                                sb.Append(cell.TargetCount >= 0 ? (char)('0' + cell.TargetCount) : ' ');
                            }
                        }
                    }
                    sb.AppendLine();
                }
            }
            return sb.ToString();
        }

        private static void AppendLoopyDesc(Mesh mesh, int width, int height, StringBuilder sb)
        {
            int blankCount = 0;
            for (int j = 0; j < height; j++)
            {
                for (int i = 0; i < width; i++)
                {
                    Cell cell = mesh.Cells[i * height + j];
                    if (cell.TargetCount >= 0)
                    {
                        FlushBlanks(sb, ref blankCount);
                        sb.Append(cell.TargetCount);
                    }
                    else
                    {
                        blankCount++;
                    }
                }
            }
            FlushBlanks(sb, ref blankCount);
        }

        private static void FlushBlanks(StringBuilder sb, ref int blankCount)
        {
            while (blankCount > 26)
            {
                sb.Append('z');
                blankCount -= 26;
            }
            if (blankCount > 0)
            {
                sb.Append((char)('a' + (blankCount - 1)));
                blankCount = 0;
            }
        }

        private static void WriteLoopyField(TextWriter writer, string key, string value)
        {
            writer.WriteLine($"{key.PadRight(8)}:{value.Length}:{value}");
        }

        public static bool TryParsePuzzle(string rawInput, out Mesh? mesh, out string detectedFormat, out int width, out int height)
        {
            mesh = null;
            detectedFormat = string.Empty;
            width = 0;
            height = 0;
            if (string.IsNullOrWhiteSpace(rawInput))
                return false;

            string trimmed = rawInput.Trim();

            // 1. Compact URL or #p= / #puzzle= hash or bare compact codec
            string codeCandidate = ExtractShareCode(trimmed);
            if (PuzzleCodec.TryDecode(codeCandidate, out var decodedMesh, out int dw, out int dh, out _) && decodedMesh != null)
            {
                mesh = decodedMesh;
                detectedFormat = "Share Link";
                width = dw;
                height = dh;
                return true;
            }

            // 2. Loopy descriptor (e.g. "10x10:0a2..." or "10x10r0de:0a2...")
            if (!trimmed.Contains('\n') && TryParseLoopyDescriptor(trimmed, out mesh, out width, out height))
            {
                detectedFormat = "Loopy Puzzle";
                return true;
            }

            string[] lines = rawInput.Split(LineSeparators, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0)
                return false;

            // 3. Simon Tatham Loopy SAVEFILE
            if (IsLoopySave(lines) && TryParseLoopySave(lines, out mesh, out width, out height))
            {
                detectedFormat = "Loopy Save";
                return true;
            }

            // 4. Square ASCII format (+ - +)
            if (lines[0].StartsWith('+') && TryParseSquareAscii(lines, out mesh, out width, out height))
            {
                detectedFormat = "ASCII Grid";
                return true;
            }

            // 5. Native .loop serialization
            var nativeMesh = new Mesh(0, 0, MeshType.Square);
            if (nativeMesh.LoadFromText(lines))
            {
                mesh = nativeMesh;
                detectedFormat = "Loop File";
                if (TryGetSquareDimensions(nativeMesh, out int sw, out int sh))
                {
                    width = sw;
                    height = sh;
                }
                return true;
            }

            // 6. Legacy compressed share token fallback
            if (!trimmed.Contains('\n') && PuzzleCompression.TryDecompressFromUrlSafe(codeCandidate, out string decompressed) && !string.IsNullOrWhiteSpace(decompressed))
            {
                var legacyMesh = new Mesh(0, 0, MeshType.Square);
                if (legacyMesh.LoadFromText(decompressed.AsSpan()))
                {
                    mesh = legacyMesh;
                    detectedFormat = "Legacy Share Link";
                    return true;
                }
            }

            return false;
        }

        private static string ExtractShareCode(string trimmed)
        {
            int hashIdx = trimmed.IndexOf('#');
            string fragment = hashIdx >= 0 ? trimmed[(hashIdx + 1)..] : trimmed;
            if (fragment.StartsWith("p=", StringComparison.Ordinal))
                return fragment[2..];
            if (fragment.StartsWith("puzzle=", StringComparison.Ordinal))
                return fragment[7..];
            return fragment;
        }

        private static bool IsLoopySave(string[] lines)
        {
            return lines.Length >= 3
                && lines[0] == "SAVEFILE:41:Simon Tatham's Portable Puzzle Collection"
                && lines[1] == "VERSION :1:1"
                && lines[2] == "GAME    :5:Loopy";
        }

        private static bool TryParseLoopyDescriptor(string text, out Mesh? mesh, out int width, out int height)
        {
            mesh = null;
            width = 0;
            height = 0;

            int colon = text.IndexOf(':');
            if (colon <= 0 || colon == text.Length - 1)
                return false;

            string paramPart = text[..colon];
            string descPart = text[(colon + 1)..];
            if (!TryParseLoopyParams(paramPart, out width, out height))
                return false;

            var candidate = new Mesh(width, height, MeshType.Square);
            if (!ApplyLoopyDescription(candidate, width, height, descPart))
                return false;

            mesh = candidate;
            return true;
        }

        private static bool TryParseLoopyParams(string paramPart, out int width, out int height)
        {
            width = 0;
            height = 0;
            int xIdx = paramPart.IndexOf('x');
            if (xIdx <= 0)
                return false;

            if (!int.TryParse(paramPart.AsSpan(0, xIdx), out width) || width <= 0 || width > PuzzleHelper.MaxGridDimension)
                return false;

            int endH = xIdx + 1;
            while (endH < paramPart.Length && char.IsDigit(paramPart[endH]))
                endH++;

            if (endH == xIdx + 1)
                return false;

            if (!int.TryParse(paramPart.AsSpan(xIdx + 1, endH - (xIdx + 1)), out height) || height <= 0 || height > PuzzleHelper.MaxGridDimension)
                return false;

            // Validate optional Loopy suffix: reject non-square grid types (t1, t2, etc.; t0 is square).
            ReadOnlySpan<char> suffix = paramPart.AsSpan(endH);
            int idx = 0;
            while (idx < suffix.Length)
            {
                char flag = suffix[idx++];
                if (flag == 't')
                {
                    int numStart = idx;
                    while (idx < suffix.Length && char.IsDigit(suffix[idx]))
                        idx++;
                    if (numStart == idx || !int.TryParse(suffix[numStart..idx], out int gridType) || gridType != 0)
                        return false;
                }
                else if (flag == 'r')
                {
                    while (idx < suffix.Length && char.IsDigit(suffix[idx]))
                        idx++;
                }
                else if (flag is 'd' or 'e' or 'n' or 'h' or 'i')
                {
                    // Known difficulty / option suffixes (e.g., de, dn, dh, di)
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ApplyLoopyDescription(Mesh mesh, int width, int height, string desc)
        {
            int counter = 0;
            int totalCells = width * height;
            foreach (char c in desc)
            {
                if (c is >= '0' and <= '4')
                {
                    if (counter >= totalCells) return false;
                    int cellIdx = (counter % width) * height + (counter / width);
                    mesh.AddTarget(mesh.Cells[cellIdx], c - '0');
                    counter++;
                }
                else if (c is >= 'a' and <= 'z')
                {
                    int skip = c - 'a' + 1;
                    if (counter + skip > totalCells) return false;
                    counter += skip;
                }
                else if (!char.IsWhiteSpace(c))
                {
                    return false;
                }
            }
            return counter == totalCells;
        }

        private static bool TryParseLoopySave(string[] lines, out Mesh? mesh, out int width, out int height)
        {
            mesh = null;
            width = 0;
            height = 0;

            foreach (string line in lines)
            {
                string[] bits = line.Split(':', 3, StringSplitOptions.None);
                if (bits.Length < 3) continue;

                string type = bits[0].Trim();
                string payload = bits[2];
                switch (type)
                {
                    case "PARAMS":
                        if (!TryParseLoopyParams(payload, out width, out height))
                            return false;
                        mesh = new Mesh(width, height, MeshType.Square);
                        break;

                    case "DESC":
                        if (mesh == null) return false;
                        if (!ApplyLoopyDescription(mesh, width, height, payload))
                            return false;
                        break;

                    case "MOVE":
                        if (mesh == null) return false;
                        if (!ApplyLoopyMove(mesh, width, height, payload))
                            return false;
                        break;

                    case "SOLVE":
                        if (mesh == null) return false;
                        ApplyLoopySolveString(mesh, width, height, payload);
                        break;
                }
            }

            return mesh != null && width > 0 && height > 0;
        }

        private static bool ApplyLoopyMove(Mesh mesh, int width, int height, string payload)
        {
            int comma = payload.IndexOf(',');
            if (comma <= 0 || payload.Length < comma + 4) return false;

            if (!int.TryParse(payload.AsSpan(0, comma), out int x)) return false;
            char op = payload[^1];
            char dir = payload[^2];
            if (dir != 'h' && dir != 'v') return false;
            if (!int.TryParse(payload.AsSpan(comma + 1, payload.Length - comma - 3), out int y)) return false;

            if (!TryGetLoopyEdgeIndex(mesh, x, y, dir == 'h', width, height, out int edgeIdx))
                return false;

            if (op == 'u')
            {
                if (mesh.Edges[edgeIdx].State != EdgeState.Empty)
                    new UnsetAction(mesh, edgeIdx).Perform();
                return true;
            }
            if (op == 'y' || op == 'n')
            {
                EdgeState target = op == 'y' ? EdgeState.Filled : EdgeState.Excluded;
                if (mesh.Edges[edgeIdx].State != EdgeState.Empty)
                    new UnsetAction(mesh, edgeIdx).Perform();
                new SetAction(mesh, edgeIdx, target).Perform();
                return true;
            }
            return false;
        }

        private static void ApplyLoopySolveString(Mesh mesh, int width, int height, string payload)
        {
            int x = 0, y = 0;
            bool horiz = false;
            var acc = new StringBuilder();
            foreach (char c in payload)
            {
                if (char.IsDigit(c))
                {
                    acc.Append(c);
                }
                else if (c == ',' && acc.Length > 0 && int.TryParse(acc.ToString(), out x))
                {
                    acc.Clear();
                }
                else if ((c == 'h' || c == 'v') && acc.Length > 0 && int.TryParse(acc.ToString(), out y))
                {
                    acc.Clear();
                    horiz = c == 'h';
                }
                else if ((c == 'y' || c == 'n') && TryGetLoopyEdgeIndex(mesh, x, y, horiz, width, height, out int edgeIdx))
                {
                    EdgeState target = c == 'y' ? EdgeState.Filled : EdgeState.Excluded;
                    if (mesh.Edges[edgeIdx].State != EdgeState.Empty)
                        new UnsetAction(mesh, edgeIdx).Perform();
                    new SetAction(mesh, edgeIdx, target).Perform();
                }
            }
        }

        private static bool TryGetLoopyEdgeIndex(Mesh mesh, int x, int y, bool horiz, int width, int height, out int edgeIndex)
        {
            edgeIndex = -1;
            if (x < 0 || y < 0)
                return false;
            if (horiz)
            {
                if (x >= width || y > height)
                    return false;
            }
            else
            {
                if (x > width || y >= height)
                    return false;
            }

            int start = x * (height + 1) + y;
            int end = horiz ? start + height + 1 : start + 1;
            return TryFindEdgeJoining(mesh, start, end, out edgeIndex);
        }

        private static bool TryParseSquareAscii(string[] lines, out Mesh? mesh, out int width, out int height)
        {
            mesh = null;
            width = 0;
            height = 0;

            if (lines.Length < 3 || lines.Length % 2 == 0)
                return false;

            int lineLen = lines[0].Length;
            if (lineLen < 3 || lineLen % 2 == 0)
                return false;

            width = lineLen / 2;
            height = lines.Length / 2;
            if (width <= 0 || height <= 0 || width > PuzzleHelper.MaxGridDimension || height > PuzzleHelper.MaxGridDimension)
                return false;

            // Validate grid characters strictly before mutating a mesh.
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.Length > lineLen)
                    return false;
                if (line.Length < lineLen)
                    line = line.PadRight(lineLen);

                if (i % 2 == 0)
                {
                    for (int j = 0; j < lineLen; j += 2)
                    {
                        if (line[j] != '+') return false;
                    }
                    for (int j = 1; j < lineLen; j += 2)
                    {
                        if (line[j] is not (' ' or '-' or 'x' or 'X')) return false;
                    }
                }
                else
                {
                    for (int j = 0; j < lineLen; j += 2)
                    {
                        if (line[j] is not (' ' or '|' or 'x' or 'X')) return false;
                    }
                    for (int j = 1; j < lineLen; j += 2)
                    {
                        if (line[j] is not (' ' or (>= '0' and <= '4'))) return false;
                    }
                }
            }

            var candidate = new Mesh(width, height, MeshType.Square);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.Length < lineLen)
                    line = line.PadRight(lineLen);

                int row = i / 2;
                if (i % 2 == 0)
                {
                    for (int j = 1; j < lineLen; j += 2)
                    {
                        char ch = line[j];
                        if (ch == ' ') continue;
                        int start = (j / 2) * (height + 1) + row;
                        int end = (j / 2 + 1) * (height + 1) + row;
                        if (TryFindEdgeJoining(candidate, start, end, out int edgeIdx))
                        {
                            EdgeState st = ch == '-' ? EdgeState.Filled : EdgeState.Excluded;
                            new SetAction(candidate, edgeIdx, st).Perform();
                        }
                    }
                }
                else
                {
                    for (int j = 1; j < lineLen; j += 2)
                    {
                        char ch = line[j];
                        if (ch is >= '0' and <= '4')
                        {
                            Cell cell = candidate.Cells[(j / 2) * height + row];
                            candidate.AddTarget(cell, ch - '0');
                        }
                    }

                    for (int j = 0; j < lineLen; j += 2)
                    {
                        char ch = line[j];
                        if (ch == ' ') continue;
                        int start = (j / 2) * (height + 1) + row;
                        int end = start + 1;
                        if (TryFindEdgeJoining(candidate, start, end, out int edgeIdx))
                        {
                            EdgeState st = ch == '|' ? EdgeState.Filled : EdgeState.Excluded;
                            new SetAction(candidate, edgeIdx, st).Perform();
                        }
                    }
                }
            }

            mesh = candidate;
            return true;
        }
    }
}
