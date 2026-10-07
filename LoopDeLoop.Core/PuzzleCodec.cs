#nullable enable
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

        public static MeshType FromTypeCode(string code) => code.ToLowerInvariant() switch
        {
            "sq" or "square" => MeshType.Square,
            "sqs" or "squaresymmetrical" => MeshType.SquareSymmetrical,
            "tri" or "triangle" => MeshType.Triangle,
            "hex" or "hexagonal" => MeshType.Hexagonal,
            "hex2" or "hexagonal2" => MeshType.Hexagonal2,
            "hex3" or "hexagonal3" => MeshType.Hexagonal3,
            "oct" or "octagon" => MeshType.Octagon,
            "sq2" or "square2" => MeshType.Square2,
            "pen" or "pentagon" => MeshType.Pentagon,
            "kt" or "kites" => MeshType.Kites,
            "asypen" or "asymmetricpentagons" => MeshType.AsymmetricPentagons,
            "dia" or "diamonds" => MeshType.Diamonds,
            "diasq" or "diamondsquare" => MeshType.DiamondSquare,
            "penhex" or "pentagonhexagon" => MeshType.PentagonHexagon,
            "floret" or "floretpentagons" or "hexpen" or "hexpentagons" => MeshType.FloretPentagons,
            "cairo" or "cairopentagons" => MeshType.CairoPentagons,
            "hex4" or "hexagonal4" => MeshType.Hexagonal4,
            "sq3" or "square3" or "snub" or "snubsq" => MeshType.Square3,
            "penhex2" or "pentagonhexagon2" or "prism" or "prismatic" => MeshType.PentagonHexagon2,
            _ => MeshType.Square
        };

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
            if (code.StartsWith("#")) code = code.Substring(1);
            if (code.StartsWith("p=")) code = code.Substring(2);
            else if (code.StartsWith("puzzle=")) code = code.Substring(7);

            string[] parts = code.Split('/', StringSplitOptions.None);
            if (parts.Length < 2) return false;

            type = FromTypeCode(parts[0]);

            string sizeStr = parts[1];
            if (!PuzzleHelper.ParseSize(sizeStr, type, out width, out height))
            {
                return false;
            }

            try
            {
                mesh = new Mesh(width, height, type);
                string clues = parts.Length >= 3 ? parts[2] : string.Empty;

                int cellIndex = 0;
                for (int i = 0; i < clues.Length && cellIndex < mesh.Cells.Count; i++)
                {
                    char ch = clues[i];
                    if (ch >= '0' && ch <= '9')
                    {
                        mesh.SetClue(cellIndex, ch - '0');
                        cellIndex++;
                    }
                    else if (ch >= 'a' && ch <= 'z')
                    {
                        int run = ch - 'a' + 1;
                        cellIndex += run;
                    }
                }
                return true;
            }
            catch
            {
                mesh = null;
                return false;
            }
        }
    }
}
