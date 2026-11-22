using System;
using System.Text;

namespace LoopDeLoop
{
    public static class PuzzleCodec
    {
        public static string ToTypeCode(MeshType type) => type switch
        {
            MeshType.Square => "sq",
            MeshType.SquareSymmetrical => "sqs",
            MeshType.Triangle => "tri",
            MeshType.Hexagonal => "hex",
            MeshType.Hexagonal2 => "hex2",
            MeshType.Hexagonal3 => "hex3",
            MeshType.Octagon => "oct",
            MeshType.Square2 => "sq2",
            MeshType.Pentagon => "pen",
            MeshType.Kites => "kt",
            MeshType.AsymmetricPentagons => "asypen",
            MeshType.Diamonds => "dia",
            MeshType.DiamondSquare => "diasq",
            MeshType.PentagonHexagon => "penhex",
            MeshType.FloretPentagons => "floret",
            MeshType.CairoPentagons => "cairo",
            MeshType.Hexagonal4 => "hex4",
            MeshType.Square3 => "sq3",
            MeshType.PentagonHexagon2 => "penhex2",
            _ => "sq"
        };

        public static bool TryFromTypeCode(string code, out MeshType type)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                type = MeshType.Square;
                return false;
            }

            switch (code.Trim().ToLowerInvariant())
            {
                case "sq":
                case "square":
                    type = MeshType.Square; return true;
                case "sqs":
                case "squaresymmetrical":
                    type = MeshType.SquareSymmetrical; return true;
                case "tri":
                case "triangle":
                    type = MeshType.Triangle; return true;
                case "hex":
                case "hexagonal":
                    type = MeshType.Hexagonal; return true;
                case "hex2":
                case "hexagonal2":
                    type = MeshType.Hexagonal2; return true;
                case "hex3":
                case "hexagonal3":
                    type = MeshType.Hexagonal3; return true;
                case "oct":
                case "octagon":
                    type = MeshType.Octagon; return true;
                case "sq2":
                case "square2":
                    type = MeshType.Square2; return true;
                case "pen":
                case "pentagon":
                    type = MeshType.Pentagon; return true;
                case "kt":
                case "kites":
                    type = MeshType.Kites; return true;
                case "asypen":
                case "asymmetricpentagons":
                    type = MeshType.AsymmetricPentagons; return true;
                case "dia":
                case "diamonds":
                    type = MeshType.Diamonds; return true;
                case "diasq":
                case "diamondsquare":
                    type = MeshType.DiamondSquare; return true;
                case "penhex":
                case "pentagonhexagon":
                    type = MeshType.PentagonHexagon; return true;
                case "floret":
                case "floretpentagons":
                case "hexpen":
                case "hexpentagons":
                    type = MeshType.FloretPentagons; return true;
                case "cairo":
                case "cairopentagons":
                    type = MeshType.CairoPentagons; return true;
                case "hex4":
                case "hexagonal4":
                    type = MeshType.Hexagonal4; return true;
                case "sq3":
                case "square3":
                case "snub":
                case "snubsq":
                    type = MeshType.Square3; return true;
                case "penhex2":
                case "pentagonhexagon2":
                case "prism":
                case "prismatic":
                    type = MeshType.PentagonHexagon2; return true;
                default:
                    type = MeshType.Square;
                    return false;
            }
        }

        public static MeshType FromTypeCode(string code) => TryFromTypeCode(code, out MeshType type) ? type : MeshType.Square;

        public static string Encode(Mesh mesh, int width, int height)
        {
            if (mesh == null) return string.Empty;

            var sb = new StringBuilder();
            sb.Append(ToTypeCode(mesh.MeshType));
            sb.Append('/');
            sb.Append(width);
            sb.Append('x');
            sb.Append(height);
            sb.Append('/');

            int emptyRun = 0;
            for (int i = 0; i < mesh.Cells.Count; i++)
            {
                int clue = mesh.Cells[i].TargetCount;
                if (clue < 0)
                {
                    emptyRun++;
                }
                else
                {
                    while (emptyRun > 26)
                    {
                        sb.Append('z');
                        emptyRun -= 26;
                    }
                    if (emptyRun > 0)
                    {
                        sb.Append((char)('a' + emptyRun - 1));
                        emptyRun = 0;
                    }
                    sb.Append(clue);
                }
            }
            // Trailing empty cells do not need to be written.

            return sb.ToString();
        }

        public static bool TryDecode(string code, out Mesh? mesh, out int width, out int height, out MeshType type)
        {
            mesh = null;
            width = 0;
            height = 0;
            type = MeshType.Square;

            if (string.IsNullOrWhiteSpace(code)) return false;

            code = code.Trim();
            if (code.StartsWith('#')) code = code[1..];
            if (code.StartsWith("p=", StringComparison.Ordinal)) code = code[2..];
            else if (code.StartsWith("puzzle=", StringComparison.Ordinal)) code = code[7..];

            string[] parts = code.Split('/', StringSplitOptions.None);
            if (parts.Length is < 2 or > 3) return false;

            if (!TryFromTypeCode(parts[0], out type)) return false;

            string sizeStr = parts[1];
            if (string.IsNullOrWhiteSpace(sizeStr)) return false;
            if (!PuzzleHelper.ParseSize(sizeStr, type, out width, out height))
            {
                return false;
            }

            string clues = parts.Length == 3 ? parts[2] : string.Empty;
            for (int i = 0; i < clues.Length; i++)
            {
                char ch = clues[i];
                if (ch is not ((>= '0' and <= '9') or (>= 'a' and <= 'z')))
                    return false;
            }

            var candidate = new Mesh(width, height, type);
            if (candidate.Cells.Count == 0)
                return false;

            int cellIndex = 0;
            for (int i = 0; i < clues.Length; i++)
            {
                char ch = clues[i];
                if (ch is >= '0' and <= '9')
                {
                    if (cellIndex >= candidate.Cells.Count)
                        return false;
                    int clue = ch - '0';
                    if (clue > candidate.Cells[cellIndex].Edges.Count)
                        return false;
                    candidate.SetClue(cellIndex, clue);
                    cellIndex++;
                }
                else
                {
                    int run = ch - 'a' + 1;
                    cellIndex += run;
                    if (cellIndex > candidate.Cells.Count)
                        return false;
                }
            }

            mesh = candidate;
            return true;
        }
    }
}
