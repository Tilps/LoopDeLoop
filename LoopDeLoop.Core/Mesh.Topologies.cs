using System;
using System.Collections.Generic;
using System.Linq;

namespace LoopDeLoop
{
    public partial class Mesh
    {
        private void ConstructGrid(int width, int height, MeshType type)
        {
            if (type == MeshType.Square || type == MeshType.SquareSymmetrical)
            {
                for (int i = 0; i <= width; i++)
                {
                    for (int j = 0; j <= height; j++)
                    {
                        Intersection newInters = new Intersection();
                        intersections.Add(newInters);
                        newInters.X = i;
                        newInters.Y = j;
                        if (i > 0 && j < height)
                            newInters.Cells.Add(j + (i - 1) * height);
                        if (i < width && j < height)
                            newInters.Cells.Add(j + i * height);
                        if (j > 0 && i < width)
                            newInters.Cells.Add((j - 1) + i * height);
                        if (i > 0 && j > 0)
                            newInters.Cells.Add((j - 1) + (i - 1) * height);
                        if (i < width && j < height)
                        {
                            Cell newCell = new Cell();
                            cells.Add(newCell);
                            newCell.Intersections.Add(j + i * (height + 1));
                            newCell.Intersections.Add(j + 1 + i * (height + 1));
                            newCell.Intersections.Add(j + 1 + (i + 1) * (height + 1));
                            newCell.Intersections.Add(j + (i + 1) * (height + 1));
                        }
                    }
                }
                for (int i = 0; i <= width; i++)
                {
                    for (int j = 0; j <= height; j++)
                    {
                        if (i < width)
                        {
                            int index = edges.Count;
                            Edge newEdge = new Edge();
                            edges.Add(newEdge);
                            newEdge.Intersections[0] = j + i * (height + 1);
                            newEdge.Intersections[1] = j + (i + 1) * (height + 1);
                            foreach (int interIndex in newEdge.Intersections)
                            {
                                intersections[interIndex].Edges.Add(index);
                            }
                            if (j > 0)
                                newEdge.Cells.Add(j - 1 + i * height);
                            if (j < height)
                                newEdge.Cells.Add(j + i * height);
                            foreach (int cellIndex in newEdge.Cells)
                            {
                                cells[cellIndex].Edges.Add(index);
                            }
                        }
                        if (j < height)
                        {
                            int index = edges.Count;
                            Edge newEdge = new Edge();
                            edges.Add(newEdge);
                            newEdge.Intersections[0] = j + i * (height + 1);
                            newEdge.Intersections[1] = j + 1 + i * (height + 1);
                            foreach (int interIndex in newEdge.Intersections)
                            {
                                intersections[interIndex].Edges.Add(index);
                            }
                            if (i > 0)
                                newEdge.Cells.Add(j + (i - 1) * height);
                            if (i < width)
                                newEdge.Cells.Add(j + i * height);
                            foreach (int cellIndex in newEdge.Cells)
                            {
                                cells[cellIndex].Edges.Add(index);
                            }
                        }
                    }
                }
            }
            else if (type == MeshType.Triangle)
            {
                for (int i = 0; i <= width; i++)
                {
                    for (int j = 0; j <= height; j++)
                    {
                        Intersection newInters = new Intersection();
                        intersections.Add(newInters);
                        newInters.X = i;
                        newInters.X += 0.5f * (height - j);
                        newInters.Y = (float)(j * Math.Sqrt(3) / 2);
                    }

                }
                for (int i = 0; i <= width; i++)
                {
                    for (int j = 0; j <= height; j++)
                    {
                        int start = j + i * (height + 1);
                        if (i < width)
                        {
                            int end = start + (height + 1);
                            AddEdge(start, end);
                        }
                        if (j < height)
                        {
                            int end = start + 1;
                            AddEdge(start, end);
                        }
                        if (i < width && j < height)
                        {
                            int end = start + (height + 1) + 1;
                            AddEdge(start, end);
                        }
                    }
                }
                CreateCells();
            }
            else if (type == MeshType.Hexagonal)
            {
                int[,] grid = new int[width * 5 + 1, height * 3 + 1];
                for (int i = 0; i < width * 5 + 1; i++)
                {
                    for (int j = 0; j < height * 3 + 1; j++)
                        grid[i, j] = -1;
                }
                for (int i = 0; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        List<int> intersects = new List<int>();
                        int topX = i * 4 + (j % 2 == 1 ? 2 : 0);
                        int topY = j;
                        AddIntersectionOnGrid(grid, intersects, topX, topY + 1);
                        AddIntersectionOnGrid(grid, intersects, topX + 1, topY);
                        AddIntersectionOnGrid(grid, intersects, topX + 2, topY);
                        AddIntersectionOnGrid(grid, intersects, topX + 3, topY + 1);
                        AddIntersectionOnGrid(grid, intersects, topX + 2, topY + 2);
                        AddIntersectionOnGrid(grid, intersects, topX + 1, topY + 2);
                        AddPolyBoundry(intersects);
                    }
                }
                CreateCells();
            }
            else if (type == MeshType.Octagon)
            {
                int[,] grid = new int[width * 5 + 1, height * 5 + 1];
                for (int i = 0; i < width * 5 + 1; i++)
                {
                    for (int j = 0; j < height * 5 + 1; j++)
                        grid[i, j] = -1;
                }
                for (int i = 0; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        List<int> intersects = new List<int>();
                        int topX = i * 3;
                        int topY = j * 3;
                        AddIntersectionOnGrid(grid, intersects, topX, topY + 1);
                        AddIntersectionOnGrid(grid, intersects, topX + 1, topY);
                        AddIntersectionOnGrid(grid, intersects, topX + 2, topY);
                        AddIntersectionOnGrid(grid, intersects, topX + 3, topY + 1);
                        AddIntersectionOnGrid(grid, intersects, topX + 3, topY + 2);
                        AddIntersectionOnGrid(grid, intersects, topX + 2, topY + 3);
                        AddIntersectionOnGrid(grid, intersects, topX + 1, topY + 3);
                        AddIntersectionOnGrid(grid, intersects, topX, topY + 2);
                        AddPolyBoundry(intersects);
                    }
                }
                CreateCells();
            }
            else if (type == MeshType.Hexagonal2)
            {
                ApproxPointStorage storage = new ApproxPointStorage(0.001f);
                float hexagonWidth = 1 + 2 * (float)Math.Cos(Math.PI / 3);
                float hexagonHeight = 2 * (float)Math.Cos(Math.PI / 6);
                float scalingFactor = 1.5f;
                float hexagonWidthBit = (float)Math.Cos(Math.PI / 3) * scalingFactor;
                hexagonHeight *= scalingFactor;
                hexagonWidth *= scalingFactor;

                for (int i = 0; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        List<int> intersects = new List<int>();
                        float topX = hexagonWidth * i + (j % 2 == 0 ? hexagonWidth / 2 : 0);
                        float topY = hexagonHeight * j;
                        AddIntersection(storage, intersects, topX, topY + hexagonHeight / 2);
                        AddIntersection(storage, intersects, topX + hexagonWidthBit, topY);
                        AddIntersection(storage, intersects, topX + hexagonWidthBit + scalingFactor, topY);
                        AddIntersection(storage, intersects, topX + hexagonWidthBit * 2 + scalingFactor, topY + hexagonHeight / 2);
                        AddIntersection(storage, intersects, topX + hexagonWidthBit + scalingFactor, topY + hexagonHeight);
                        AddIntersection(storage, intersects, topX + hexagonWidthBit, topY + hexagonHeight);
                        AddPolyBoundry(intersects);
                    }
                }
                CreateCells();
            }
            else if (type == MeshType.Square2)
            {
                ApproxPointStorage storage = new ApproxPointStorage(0.001f);
                float triangleBit = (float)Math.Cos(Math.PI / 6);
                float scalingFactor = 1.5f;
                triangleBit *= scalingFactor;

                for (int i = 0; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        List<int> intersects = new List<int>();
                        float topX = scalingFactor * (height - j) / 2 + i * (scalingFactor + triangleBit);
                        float topY = scalingFactor * (i) / 2 + j * (scalingFactor + triangleBit);
                        AddIntersection(storage, intersects, topX, topY + triangleBit + scalingFactor / 2);
                        AddIntersection(storage, intersects, topX + triangleBit, topY + triangleBit);
                        AddIntersection(storage, intersects, topX + triangleBit, topY + triangleBit + scalingFactor);
                        AddPolyBoundry(intersects);
                        intersects.Clear();
                        AddIntersection(storage, intersects, topX + triangleBit + scalingFactor / 2, topY);
                        AddIntersection(storage, intersects, topX + triangleBit + scalingFactor, topY + triangleBit);
                        AddIntersection(storage, intersects, topX + triangleBit, topY + triangleBit);
                        AddPolyBoundry(intersects);
                        intersects.Clear();
                        AddIntersection(storage, intersects, topX + triangleBit * 2 + scalingFactor, topY + triangleBit + scalingFactor / 2);
                        AddIntersection(storage, intersects, topX + triangleBit + scalingFactor, topY + triangleBit);
                        AddIntersection(storage, intersects, topX + triangleBit + scalingFactor, topY + triangleBit + scalingFactor);
                        AddPolyBoundry(intersects);
                        intersects.Clear();
                        AddIntersection(storage, intersects, topX + triangleBit + scalingFactor / 2, topY + triangleBit * 2 + scalingFactor);
                        AddIntersection(storage, intersects, topX + triangleBit, topY + triangleBit + scalingFactor);
                        AddIntersection(storage, intersects, topX + triangleBit + scalingFactor, topY + triangleBit + scalingFactor);
                        AddPolyBoundry(intersects);
                        intersects.Clear();
                    }
                }
                CreateCells();
            }
            else if (type == MeshType.Pentagon)
            {
                ApproxPointStorage storage = new ApproxPointStorage(0.001f);
                float scalingFactor = 1.5f;
                float pentTopHeight = (float)Math.Cos(Math.PI / 2 - Math.PI / 5) * scalingFactor;
                float pentTopWidth = (float)Math.Sin(Math.PI / 2 - Math.PI / 5) * scalingFactor;
                float pentBottomWidth = (float)Math.Sin(Math.PI / 10) * scalingFactor;
                float pentBottomHeight = (float)Math.Cos(Math.PI / 10) * scalingFactor;

                for (int i = 0; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        List<int> intersects = new List<int>();
                        float topX = pentTopWidth * 2 * i + (j % 4 > 1 ? pentTopWidth : 0);
                        float topY = pentBottomHeight * j + pentTopHeight * ((j + 1) / 2);
                        if (j % 2 == 0)
                        {
                            AddIntersection(storage, intersects, topX, topY + pentTopHeight);
                            AddIntersection(storage, intersects, topX + pentTopWidth, topY);
                            AddIntersection(storage, intersects, topX + 2 * pentTopWidth, topY + pentTopHeight);
                            AddIntersection(storage, intersects, topX + pentBottomWidth + scalingFactor, topY + pentTopHeight + pentBottomHeight);
                            AddIntersection(storage, intersects, topX + pentBottomWidth, topY + pentTopHeight + pentBottomHeight);
                        }
                        else
                        {
                            AddIntersection(storage, intersects, topX + pentBottomWidth, topY);
                            AddIntersection(storage, intersects, topX + pentBottomWidth + scalingFactor, topY);
                            AddIntersection(storage, intersects, topX + 2 * pentTopWidth, topY + pentBottomHeight);
                            AddIntersection(storage, intersects, topX + pentTopWidth, topY + pentTopHeight + pentBottomHeight);
                            AddIntersection(storage, intersects, topX, topY + pentBottomHeight);
                        }
                        AddPolyBoundry(intersects);
                    }
                }
                CreateCells();
            }
            else if (type == MeshType.Hexagonal3)
            {
                ApproxPointStorage storage = new ApproxPointStorage(0.001f);
                float hexagonWidth = 1 + 2 * (float)Math.Cos(Math.PI / 3);
                float hexagonHeight = 2 * (float)Math.Cos(Math.PI / 6);
                float scalingFactor = 1.5f;
                float hexagonWidthBit = (float)Math.Cos(Math.PI / 3) * scalingFactor;
                hexagonHeight *= scalingFactor;
                hexagonWidth *= scalingFactor;

                for (int i = 0; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        List<int> intersects = new List<int>();
                        float topX = hexagonHeight * i + scalingFactor * i + (j % 2 == 1 ? hexagonHeight / 2 + scalingFactor / 2 : 0) + hexagonHeight + scalingFactor;
                        float topY = (float)Math.Sqrt(3) / 2 * (hexagonHeight + scalingFactor) * j + hexagonWidth;
                        AddIntersection(storage, intersects, topX - hexagonHeight / 2 - scalingFactor, topY + scalingFactor / 2);
                        AddIntersection(storage, intersects, topX - hexagonHeight / 2, topY + scalingFactor / 2);
                        AddIntersection(storage, intersects, topX - hexagonHeight / 2, topY - scalingFactor / 2);
                        AddIntersection(storage, intersects, topX - hexagonHeight / 2 - scalingFactor, topY - scalingFactor / 2);
                        AddPolyBoundry(intersects);
                        intersects.Clear();
                        AddIntersection(storage, intersects, topX + hexagonHeight / 2 + scalingFactor, topY + scalingFactor / 2);
                        AddIntersection(storage, intersects, topX + hexagonHeight / 2, topY + scalingFactor / 2);
                        AddIntersection(storage, intersects, topX + hexagonHeight / 2, topY - scalingFactor / 2);
                        AddIntersection(storage, intersects, topX + hexagonHeight / 2 + scalingFactor, topY - scalingFactor / 2);
                        AddPolyBoundry(intersects);
                        intersects.Clear();
                        AddIntersection(storage, intersects, topX - hexagonHeight / 2, topY - scalingFactor / 2);
                        AddIntersection(storage, intersects, topX - hexagonHeight / 2 - hexagonWidthBit, topY - scalingFactor / 2 - hexagonHeight / 2);
                        AddIntersection(storage, intersects, topX - hexagonWidthBit, topY - scalingFactor / 2 - hexagonHeight / 2 - hexagonWidthBit);
                        AddIntersection(storage, intersects, topX, topY - scalingFactor / 2 - hexagonWidthBit);
                        AddPolyBoundry(intersects);
                        intersects.Clear();
                        AddIntersection(storage, intersects, topX, topY - scalingFactor / 2 - hexagonWidthBit);
                        AddIntersection(storage, intersects, topX + hexagonWidthBit, topY - scalingFactor / 2 - hexagonHeight / 2 - hexagonWidthBit);
                        AddIntersection(storage, intersects, topX + hexagonHeight / 2 + hexagonWidthBit, topY - scalingFactor / 2 - hexagonHeight / 2);
                        AddIntersection(storage, intersects, topX + hexagonHeight / 2, topY - scalingFactor / 2);
                        AddPolyBoundry(intersects);
                        intersects.Clear();
                        AddIntersection(storage, intersects, topX - hexagonHeight / 2, topY + scalingFactor / 2);
                        AddIntersection(storage, intersects, topX - hexagonHeight / 2 - hexagonWidthBit, topY + scalingFactor / 2 + hexagonHeight / 2);
                        AddIntersection(storage, intersects, topX - hexagonWidthBit, topY + scalingFactor / 2 + hexagonHeight / 2 + hexagonWidthBit);
                        AddIntersection(storage, intersects, topX, topY + scalingFactor / 2 + hexagonWidthBit);
                        AddPolyBoundry(intersects);
                        intersects.Clear();
                        AddIntersection(storage, intersects, topX, topY + scalingFactor / 2 + hexagonWidthBit);
                        AddIntersection(storage, intersects, topX + hexagonWidthBit, topY + scalingFactor / 2 + hexagonHeight / 2 + hexagonWidthBit);
                        AddIntersection(storage, intersects, topX + hexagonHeight / 2 + hexagonWidthBit, topY + scalingFactor / 2 + hexagonHeight / 2);
                        AddIntersection(storage, intersects, topX + hexagonHeight / 2, topY + scalingFactor / 2);
                        AddPolyBoundry(intersects);
                        intersects.Clear();
                    }
                }
                CreateCells();
            }
            else if (type == MeshType.Kites)
            {
                ConstructKites(width, height);
            }
            else if (type == MeshType.AsymmetricPentagons)
            {
                ConstructAsymmetricPentagons(width);
            }
            else if (type == MeshType.Diamonds)
            {
                ConstructDiamonds(width, height);
            }
            else if (type == MeshType.DiamondSquare)
            {
                ConstructDiamondSquare(width);
            }
            else if (type == MeshType.PentagonHexagon)
            {
                ConstructPentagonHexagon(width, height);
            }
            else if (type == MeshType.FloretPentagons)
            {
                ConstructFloretPentagons(width, height);
            }
            else if (type == MeshType.CairoPentagons)
            {
                ConstructCairoPentagons(width, height);
            }
            else if (type == MeshType.Hexagonal4)
            {
                ConstructHexagonal4(width, height);
            }
            else if (type == MeshType.Square3)
            {
                ConstructSquare3(width, height);
            }
            else if (type == MeshType.PentagonHexagon2)
            {
                ConstructPentagonHexagon2(width, height);
            }
        }

#region Mesh construction helpers.

        private void AddPolyBoundry(List<int> intersects)
        {
            for (int k = 0; k < intersects.Count; k++)
            {
                int start = intersects[k];
                int end = intersects[(k + 1) % intersects.Count];
                try
                {
                    GetEdgeJoining(start, end);
                }
                catch
                {
                    AddEdge(start, end);
                }
            }
        }

        private void AddIntersection(ApproxPointStorage storage, List<int> intersects, float topX, float topY)
        {
            int intersectNum = storage.Add(topX, topY, intersections.Count);
            intersects.Add(intersectNum);
            if (intersectNum == intersections.Count)
            {
                Intersection inters = new Intersection();
                intersections.Add(inters);
                inters.X = topX;
                inters.Y = topY;
            }
        }

        private void AddIntersectionOnGrid(int[,] grid, List<int> intersects, int topX, int topY)
        {
            if (grid[topX, topY] == -1)
            {
                grid[topX, topY] = intersections.Count;
                intersects.Add(intersections.Count);
                Intersection inters = new Intersection();
                intersections.Add(inters);
                inters.X = topX;
                inters.Y = topY;
            }
            else
                intersects.Add(grid[topX, topY]);
        }

        public void CreateCells()
        {
            for (int i = 0; i < intersections.Count; i++)
            {
                List<List<int>> cellCornersSets = CreateCells(i);
                foreach (List<int> cellCorners in cellCornersSets)
                {
                    if (cellCorners.Count > 0)
                    {
                        int cellIndex = cells.Count;
                        Cell cell = new Cell();
                        cells.Add(cell);
                        cell.Intersections.AddRange(cellCorners);
                        for (int j = 0; j < cellCorners.Count; j++)
                        {
                            int cellCorner = cellCorners[j];
                            Intersection inters = intersections[cellCorner];
                            inters.Cells.Add(cellIndex);
                            int eIndex = GetEdgeJoining(cellCorner, cellCorners[(j + 1) % cellCorners.Count]);
                            Edge e = edges[eIndex];
                            e.Cells.Add(cellIndex);
                            cell.Edges.Add(eIndex);
                        }
                    }
                }
            }
        }

        private List<List<int>> CreateCells(int i)
        {
            List<List<int>> result = new List<List<int>>();
            Intersection inters = intersections[i];
            foreach (int eIndex in inters.Edges)
            {
                List<int> corners = new List<int>();
                int last = i;
                int next = GetOtherInters(eIndex, last);
                Intersection nextInter = intersections[next];
                Intersection lastInter = inters;
                double lastAngle = Math.Atan2(nextInter.Y - inters.Y, nextInter.X - inters.X);
                corners.Add(i);
                while (next != i)
                {
                    corners.Add(next);
                    int realLast = last;
                    last = next;
                    lastInter = nextInter;
                    next = -1;
                    // switch lastAngle into our space.
                    lastAngle = Math.PI + lastAngle;
                    if (lastAngle > Math.PI)
                        lastAngle -= Math.PI * 2;
                    double nextAngle = lastAngle + Math.PI * 2;
                    foreach (int edgeIndex in nextInter.Edges)
                    {
                        int possibleNext = GetOtherInters(edgeIndex, last);
                        if (possibleNext == realLast)
                            continue;
                        Intersection possibleNextInter = intersections[possibleNext];
                        double possibleNextAngle = Math.Atan2(possibleNextInter.Y - lastInter.Y, possibleNextInter.X - lastInter.X);
                        if (possibleNextAngle > lastAngle && possibleNextAngle < nextAngle)
                        {
                            nextAngle = possibleNextAngle;
                            next = possibleNext;
                        }
                        else
                        {
                            possibleNextAngle += Math.PI * 2;
                            if (possibleNextAngle > lastAngle && possibleNextAngle < nextAngle)
                            {
                                nextAngle = possibleNextAngle;
                                next = possibleNext;
                            }
                        }
                    }
                    if (nextAngle > Math.PI)
                        nextAngle -= Math.PI * 2;
                    lastAngle = nextAngle;
                    nextInter = intersections[next];
                }
                int min = int.MaxValue;
                foreach (int corner in corners)
                {
                    if (corner < min)
                        min = corner;
                }
                if (min == i)
                {
                    double total = 0.0;
                    // Verify anticlockwise.
                    for (int cIndex = 0; cIndex < corners.Count; cIndex++)
                    {
                        int start = corners[cIndex];
                        int mid = corners[(cIndex + 1) % corners.Count];
                        int end = corners[(cIndex + 2) % corners.Count];
                        Intersection startInter = intersections[start];
                        Intersection midInter = intersections[mid];
                        Intersection endInter = intersections[end];
                        float dy1 = midInter.Y - startInter.Y;
                        float dx1 = midInter.X - startInter.X;
                        float dy2 = endInter.Y - midInter.Y;
                        float dx2 = endInter.X - midInter.X;
                        double cross = dy1 * dx2 - dx1 * dy2;
                        double dot = dx1 * dx2 + dy1 * dy2;
                        double angle = Math.Acos(dot / Math.Sqrt(dx1 * dx1 + dy1 * dy1) / Math.Sqrt(dx2 * dx2 + dy2 * dy2));
                        if (cross < 0)
                            angle = -angle;

                        total += angle;
                    }
                    if (total > 0)
                        result.Add(corners);
                }
            }
            return result;
        }

        private int GetOtherInters(int eIndex, int last)
        {
            Edge e = edges[eIndex];
            if (e.Intersections[0] == last)
                return e.Intersections[1];
            else
                return e.Intersections[0];
        }

        public void AddEdge(int start, int end)
        {
            int index = edges.Count;
            Edge newEdge = new Edge();
            edges.Add(newEdge);
            newEdge.Intersections[0] = start;
            newEdge.Intersections[1] = end;
            foreach (int interIndex in newEdge.Intersections)
            {
                intersections[interIndex].Edges.Add(index);
            }

        }

#endregion

#region New Topologies (KTL-compatible)

        private void NormalizeCoordinates()
        {
            if (intersections.Count == 0) return;
            float minX = float.MaxValue, minY = float.MaxValue;
            foreach (var inters in intersections)
            {
                if (inters.X < minX) minX = inters.X;
                if (inters.Y < minY) minY = inters.Y;
            }
            foreach (var inters in intersections)
            {
                inters.X -= minX;
                inters.Y -= minY;
            }
        }

        private void ConstructKites(int width, int height)
        {
            ApproxPointStorage storage = new ApproxPointStorage(0.001f);
            List<int> intersects = new List<int>();

            float hexWidth = 2.0f;
            float halfWidth = hexWidth / 2f;
            float quarterWidth = halfWidth / 2f;
            float sqrt3 = MathF.Sqrt(3f);
            float rowHeight = sqrt3 * halfWidth;
            float shortDist = rowHeight / 3f;
            float longDist = rowHeight - shortDist;

            void AddPoly(params (float X, float Y)[] pts)
            {
                intersects.Clear();
                foreach (var pt in pts)
                    AddIntersection(storage, intersects, pt.X, pt.Y);
                AddPolyBoundry(intersects);
            }

            for (int row = 0; row < height; row++)
            {
                int rowWidth = (row % 2 == 1) ? width + 1 : width;
                for (int col = 0; col < rowWidth; col++)
                {
                    float cx = col * hexWidth - (row % 2) * halfWidth;
                    float cy = row * rowHeight;

                    var top = (cx, cy - longDist);
                    var bottom = (cx, cy + longDist);
                    var topLeft = (cx - halfWidth, cy - shortDist);
                    var topRight = (cx + halfWidth, cy - shortDist);
                    var midLeft = (cx - halfWidth, cy);
                    var center = (cx, cy);
                    var midRight = (cx + halfWidth, cy);
                    var bottomLeft = (cx - halfWidth, cy + shortDist);
                    var bottomRight = (cx + halfWidth, cy + shortDist);
                    var upperMidLeft = (cx - quarterWidth, cy - longDist + shortDist / 2f);
                    var upperMidRight = (cx + quarterWidth, cy - longDist + shortDist / 2f);
                    var lowerMidLeft = (cx - quarterWidth, cy + longDist - shortDist / 2f);
                    var lowerMidRight = (cx + quarterWidth, cy + longDist - shortDist / 2f);

                    AddPoly(upperMidLeft, top, upperMidRight, center);
                    AddPoly(topLeft, upperMidLeft, center, midLeft);
                    AddPoly(upperMidRight, topRight, midRight, center);
                    AddPoly(center, lowerMidRight, bottom, lowerMidLeft);
                    AddPoly(midLeft, center, lowerMidLeft, bottomLeft);
                    AddPoly(midRight, bottomRight, lowerMidRight, center);
                }
            }

            CreateCells();
            NormalizeCoordinates();
        }

        private void ConstructDiamonds(int width, int height)
        {
            ApproxPointStorage storage = new ApproxPointStorage(0.001f);
            List<int> intersects = new List<int>();

            float side = 1.5f;
            float halfSide = side / 2f;
            float sqrt3 = MathF.Sqrt(3f);
            float radius = sqrt3 * halfSide;
            float colSpacing = 2f * radius;
            float rowSpacing = 2f * side - halfSide;

            void AddPoly(params (float X, float Y)[] pts)
            {
                intersects.Clear();
                foreach (var pt in pts)
                    AddIntersection(storage, intersects, pt.X, pt.Y);
                AddPolyBoundry(intersects);
            }

            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    float cx = radius + col * colSpacing + (row % 2) * radius;
                    float cy = row * rowSpacing + side;

                    var top = (cx, cy - side);
                    var topLeft = (cx - radius, cy - halfSide);
                    var topRight = (cx + radius, cy - halfSide);
                    var center = (cx, cy);
                    var bottomLeft = (cx - radius, cy + halfSide);
                    var bottomRight = (cx + radius, cy + halfSide);
                    var bottom = (cx, cy + side);

                    AddPoly(topLeft, top, topRight, center);
                    AddPoly(topLeft, center, bottom, bottomLeft);
                    AddPoly(center, topRight, bottomRight, bottom);
                }
            }

            CreateCells();
            NormalizeCoordinates();
        }

        private void ConstructDiamondSquare(int layersCount)
        {
            int layers = Math.Max(1, Math.Min(10, layersCount));
            ApproxPointStorage storage = new ApproxPointStorage(0.001f);
            List<int> intersects = new List<int>();

            float unit = 1.0f;
            float diag = unit / MathF.Sqrt(2f);
            float step = 2f * diag;

            void AddPoly(params (float X, float Y)[] pts)
            {
                intersects.Clear();
                foreach (var pt in pts)
                    AddIntersection(storage, intersects, pt.X, pt.Y);
                AddPolyBoundry(intersects);
            }

            (float X, float Y) Rot((float X, float Y) pt, int rot) => rot switch
            {
                1 => (-pt.Y, pt.X),
                2 => (-pt.X, -pt.Y),
                3 => (pt.Y, -pt.X),
                _ => pt
            };

            for (int layer = 1; layer <= layers; layer++)
            {
                for (int rot = 0; rot < 4; rot++)
                {
                    float offsetDiag = (layer - 1) * diag;
                    float offsetBase = (layer - 1) * (unit + diag);
                    for (int c = 0; c < layer; c++)
                    {
                        float u = c * step - offsetDiag;
                        float s = -offsetBase;

                        var dmOrigin = Rot((u, s), rot);
                        var dmLeft = Rot((u - diag, s - diag), rot);
                        var dmLeftDown = Rot((u - diag, s - diag - unit), rot);
                        var dmCenter = Rot((u, s - unit), rot);
                        var dmRight = Rot((u + diag, s - diag), rot);
                        var dmRightDown = Rot((u + diag, s - diag - unit), rot);
                        var dmTip = Rot((u, s - unit - step), rot);

                        AddPoly(dmOrigin, dmLeft, dmLeftDown, dmCenter);
                        AddPoly(dmOrigin, dmCenter, dmRightDown, dmRight);
                        AddPoly(dmLeftDown, dmCenter, dmRightDown, dmTip);

                        float mx = c * unit + offsetDiag;
                        float my = c * unit - offsetBase;

                        var sqBottomLeft = Rot((mx, my), rot);
                        var sqTopLeft = Rot((mx, my - unit), rot);
                        var sqTopMid = Rot((mx + diag, my - unit - diag), rot);
                        var sqRightMid = Rot((mx + diag, my - diag), rot);
                        var sqBottomRight = Rot((mx + unit, my), rot);
                        var sqFarRight = Rot((mx + unit + diag, my - diag), rot);
                        var sqFarTop = Rot((mx + unit + diag, my - unit - diag), rot);

                        if (c > 0)
                            AddPoly(sqBottomLeft, sqTopLeft, sqTopMid, sqRightMid);
                        if (c < layer - 1)
                            AddPoly(sqBottomLeft, sqRightMid, sqFarRight, sqBottomRight);
                        AddPoly(sqTopMid, sqFarTop, sqFarRight, sqRightMid);
                    }
                }
            }

            CreateCells();
            NormalizeCoordinates();
        }

        private void ConstructFloretPentagons(int width, int height)
        {
            ApproxPointStorage storage = new ApproxPointStorage(0.001f);
            List<int> intersects = new List<int>();

            float side = 1.0f;
            float halfSide = side / 2f;
            float quarterSide = halfSide / 2f;
            float sqrt3 = MathF.Sqrt(3f);
            float hBit = sqrt3 * halfSide / 2f;
            float colSpacing = 2f * side + quarterSide;
            float rowSpacing = 5f * hBit;
            float skewX = halfSide + quarterSide;
            float skewY = hBit;

            void AddPoly(params (float X, float Y)[] pts)
            {
                intersects.Clear();
                foreach (var pt in pts)
                    AddIntersection(storage, intersects, pt.X, pt.Y);
                AddPolyBoundry(intersects);
            }

            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    float cx = col * colSpacing + row * skewX;
                    float cy = row * rowSpacing + col * skewY;

                    var topInner = (cx - quarterSide, cy - 3 * hBit);
                    var topOuter = (cx + quarterSide, cy - 3 * hBit);
                    var upperLeftFar = (cx - side, cy - 2 * hBit);
                    var upperLeftMid = (cx - halfSide, cy - 2 * hBit);
                    var upperRightMid = (cx + halfSide, cy - 2 * hBit);
                    var upperRightFar = (cx + side, cy - 2 * hBit);
                    var upperLeftOuter = (cx - side - quarterSide, cy - hBit);
                    var upperRightOuter = (cx + side + quarterSide, cy - hBit);
                    var midLeft = (cx - side, cy);
                    var center = (cx, cy);
                    var midRight = (cx + side, cy);
                    var lowerLeftOuter = (cx - side - quarterSide, cy + hBit);
                    var lowerRightOuter = (cx + side + quarterSide, cy + hBit);
                    var lowerLeftFar = (cx - side, cy + 2 * hBit);
                    var lowerLeftMid = (cx - halfSide, cy + 2 * hBit);
                    var lowerRightMid = (cx + halfSide, cy + 2 * hBit);
                    var lowerRightFar = (cx + side, cy + 2 * hBit);
                    var bottomInner = (cx - quarterSide, cy + 3 * hBit);
                    var bottomOuter = (cx + quarterSide, cy + 3 * hBit);

                    AddPoly(topInner, topOuter, upperRightMid, center, upperLeftMid);
                    AddPoly(upperLeftFar, upperLeftMid, center, midLeft, upperLeftOuter);
                    AddPoly(upperRightMid, upperRightFar, upperRightOuter, midRight, center);
                    AddPoly(center, lowerLeftMid, lowerLeftFar, lowerLeftOuter, midLeft);
                    AddPoly(center, midRight, lowerRightOuter, lowerRightFar, lowerRightMid);
                    AddPoly(center, lowerRightMid, bottomOuter, bottomInner, lowerLeftMid);
                }
            }

            CreateCells();
            NormalizeCoordinates();
        }

        private void ConstructCairoPentagons(int width, int height)
        {
            ApproxPointStorage storage = new ApproxPointStorage(0.001f);
            List<int> intersects = new List<int>();

            float side = 1.0f;
            float angle = MathF.PI / 12f;
            float sinAngle = MathF.Sin(angle) * side;
            float cosAngle = MathF.Cos(angle) * side;
            float unitSize = 2f * sinAngle + 2f * cosAngle;

            void AddPoly(params (float X, float Y)[] pts)
            {
                intersects.Clear();
                foreach (var pt in pts)
                    AddIntersection(storage, intersects, pt.X, pt.Y);
                AddPolyBoundry(intersects);
            }

            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    float tx = col * unitSize;
                    float ty = row * unitSize;

                    var center = (tx, ty);
                    var topInner = (tx + sinAngle, ty - cosAngle);
                    var rightInner = (tx + cosAngle, ty + sinAngle);
                    var bottomInner = (tx - sinAngle, ty + cosAngle);
                    var leftInner = (tx - cosAngle, ty - sinAngle);

                    var bottomLeftCorner = (tx - sinAngle - cosAngle, ty + cosAngle + sinAngle);
                    var bottomRightCorner = (tx + cosAngle + sinAngle, ty + sinAngle + cosAngle);
                    var topLeftCorner = (tx - cosAngle - sinAngle, ty - sinAngle - cosAngle);
                    var topRightCorner = (tx + sinAngle + cosAngle, ty - cosAngle - sinAngle);

                    var leftMid = (tx - 2f * sinAngle - cosAngle, ty + sinAngle);
                    var bottomMid = (tx + sinAngle, ty + 2f * sinAngle + cosAngle);
                    var topMid = (tx - sinAngle, ty - 2f * sinAngle - cosAngle);
                    var rightMid = (tx + 2f * sinAngle + cosAngle, ty - sinAngle);

                    AddPoly(topLeftCorner, topMid, topInner, center, leftInner);
                    AddPoly(topInner, topRightCorner, rightMid, rightInner, center);
                    AddPoly(rightInner, bottomRightCorner, bottomMid, bottomInner, center);
                    AddPoly(bottomInner, bottomLeftCorner, leftMid, leftInner, center);
                }
            }

            CreateCells();
            NormalizeCoordinates();
        }

        private void ConstructPentagonHexagon(int width, int height)
        {
            ApproxPointStorage storage = new ApproxPointStorage(0.001f);
            List<int> intersects = new List<int>();

            float side = 1.0f;
            float halfSide = side / 2f;
            float quarterSide = halfSide / 2f;
            float sqrt3 = MathF.Sqrt(3f);
            float hBit = sqrt3 * halfSide / 2f;
            float colSpacing = 8f * hBit;
            float rowSpacing = 3f * side;

            void AddPoly(params (float X, float Y)[] pts)
            {
                intersects.Clear();
                foreach (var pt in pts)
                    AddIntersection(storage, intersects, pt.X, pt.Y);
                AddPolyBoundry(intersects);
            }

            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    float cx = col * colSpacing;
                    float cy = row * rowSpacing;

                    var topPeak = (cx, cy - halfSide - side);
                    var topFarLeft = (cx - 3 * hBit, cy - 2 * halfSide - quarterSide);
                    var topFarRight = (cx + 3 * hBit, cy - 2 * halfSide - quarterSide);
                    var topUpperLeft = (cx - 4 * hBit, cy - 2 * halfSide);
                    var topUpperMidLeft = (cx - 2 * hBit, cy - 2 * halfSide);
                    var topUpperMidRight = (cx + 2 * hBit, cy - 2 * halfSide);
                    var topUpperRight = (cx + 4 * hBit, cy - 2 * halfSide);
                    var midUpperLeft = (cx - 2 * hBit, cy - halfSide);
                    var midUpperCenter = (cx, cy - halfSide);
                    var midUpperRight = (cx + 2 * hBit, cy - halfSide);
                    var midUpperHexLeft = (cx - hBit, cy - quarterSide);
                    var midUpperHexRight = (cx + hBit, cy - quarterSide);
                    var midLeftEdge = (cx - 4 * hBit, cy);
                    var midRightEdge = (cx + 4 * hBit, cy);
                    var midLowerHexLeft = (cx - hBit, cy + quarterSide);
                    var midLowerHexRight = (cx + hBit, cy + quarterSide);
                    var midLowerLeft = (cx - 2 * hBit, cy + halfSide);
                    var midLowerCenter = (cx, cy + halfSide);
                    var midLowerRight = (cx + 2 * hBit, cy + halfSide);
                    var bottomLowerLeft = (cx - 4 * hBit, cy + 2 * halfSide);
                    var bottomLowerMidLeft = (cx - 2 * hBit, cy + 2 * halfSide);
                    var bottomLowerMidRight = (cx + 2 * hBit, cy + 2 * halfSide);
                    var bottomLowerRight = (cx + 4 * hBit, cy + 2 * halfSide);
                    var bottomFarLeft = (cx - 3 * hBit, cy + 2 * halfSide + quarterSide);
                    var bottomFarRight = (cx + 3 * hBit, cy + 2 * halfSide + quarterSide);
                    var bottomPeak = (cx, cy + halfSide + side);

                    AddPoly(topUpperLeft, topFarLeft, topUpperMidLeft, midUpperLeft, midLeftEdge);
                    AddPoly(topUpperMidLeft, topPeak, midUpperCenter, midUpperHexLeft, midUpperLeft);
                    AddPoly(topPeak, topUpperMidRight, midUpperRight, midUpperHexRight, midUpperCenter);
                    AddPoly(topUpperMidRight, topFarRight, topUpperRight, midRightEdge, midUpperRight);
                    AddPoly(midLeftEdge, midUpperLeft, midUpperHexLeft, midLowerHexLeft, midLowerLeft);
                    AddPoly(midUpperHexLeft, midUpperCenter, midUpperHexRight, midLowerHexRight, midLowerCenter, midLowerHexLeft);
                    AddPoly(midUpperHexRight, midUpperRight, midRightEdge, midLowerRight, midLowerHexRight);
                    AddPoly(bottomLowerLeft, midLeftEdge, midLowerLeft, bottomLowerMidLeft, bottomFarLeft);
                    AddPoly(midLowerLeft, midLowerHexLeft, midLowerCenter, bottomPeak, bottomLowerMidLeft);
                    AddPoly(midLowerCenter, midLowerHexRight, midLowerRight, bottomLowerMidRight, bottomPeak);
                    AddPoly(midLowerRight, midRightEdge, bottomLowerRight, bottomFarRight, bottomLowerMidRight);

                    if (row < height - 1)
                    {
                        var linkLeft = (cx - 3 * hBit, cy + side + halfSide + quarterSide);
                        var linkMidLeft = (cx - 2 * hBit, cy + 2 * side);
                        var linkMidRight = (cx + 2 * hBit, cy + 2 * side);
                        var linkRight = (cx + 3 * hBit, cy + side + halfSide + quarterSide);

                        AddPoly(bottomFarLeft, bottomLowerMidLeft, bottomPeak, linkMidLeft, linkLeft);
                        AddPoly(bottomPeak, bottomLowerMidRight, bottomFarRight, linkRight, linkMidRight);

                        if (col < width - 1)
                        {
                            var gapUpperRight = (cx + 5 * hBit, cy + 2 * halfSide + quarterSide);
                            var gapMidRight = (cx + 5 * hBit, cy + side + halfSide + quarterSide);
                            var gapLowerMidRight = (cx + 4 * hBit, cy + 2 * side);

                            AddPoly(bottomLowerRight, bottomFarRight, linkRight, gapLowerMidRight, gapMidRight, gapUpperRight);
                        }
                    }
                }
            }

            CreateCells();
            NormalizeCoordinates();
        }

        private void ConstructHexagonal4(int width, int height)
        {
            ApproxPointStorage storage = new ApproxPointStorage(0.001f);
            List<int> intersects = new List<int>();

            float side = 1.0f;
            float halfSide = side / 2f;
            float sqrt3 = MathF.Sqrt(3f);
            float radius = sqrt3 * halfSide;
            float unitWidth = 2f * side + 2f * radius;
            float halfUnitWidth = unitWidth / 2f;
            float colSpacing = unitWidth - side;
            float rowSpacing = radius + side + halfSide;
            float evenRowOffset = unitWidth - halfSide;

            void AddPoly(params (float X, float Y)[] pts)
            {
                intersects.Clear();
                foreach (var pt in pts)
                    AddIntersection(storage, intersects, pt.X, pt.Y);
                AddPolyBoundry(intersects);
            }

            for (int row = 0; row < height; row++)
            {
                bool isOddRow = (row % 2 == 1);
                int rowCount = isOddRow ? width : (width - 1);
                float rowOffset = isOddRow ? halfUnitWidth : evenRowOffset;

                for (int col = 0; col < rowCount; col++)
                {
                    float cx = colSpacing * col + rowOffset;
                    float cy = rowSpacing * row + halfUnitWidth;

                    var topInner = (cx, cy - side);
                    var topRightInner = (cx + radius, cy - halfSide);
                    var bottomRightInner = (cx + radius, cy + halfSide);
                    var bottomInner = (cx, cy + side);
                    var bottomLeftInner = (cx - radius, cy + halfSide);
                    var topLeftInner = (cx - radius, cy - halfSide);

                    var topLeftMid = (cx - radius - halfSide, cy - halfSide - radius);
                    var topOuterLeft = (cx - halfSide, cy - halfUnitWidth);
                    var topOuterRight = (cx + halfSide, cy - halfUnitWidth);
                    var topRightMid = (cx + radius + halfSide, cy - halfSide - radius);
                    var rightOuterTop = (cx + halfUnitWidth, cy - halfSide);
                    var rightOuterBottom = (cx + halfUnitWidth, cy + halfSide);
                    var bottomRightMid = (cx + radius + halfSide, cy + halfSide + radius);
                    var bottomOuterRight = (cx + halfSide, cy + halfUnitWidth);
                    var bottomOuterLeft = (cx - halfSide, cy + halfUnitWidth);
                    var bottomLeftMid = (cx - radius - halfSide, cy + halfSide + radius);
                    var leftOuterBottom = (cx - halfUnitWidth, cy + halfSide);
                    var leftOuterTop = (cx - halfUnitWidth, cy - halfSide);

                    AddPoly(topInner, topRightInner, bottomRightInner, bottomInner, bottomLeftInner, topLeftInner);
                    AddPoly(topRightInner, rightOuterTop, rightOuterBottom, bottomRightInner);
                    AddPoly(bottomRightInner, rightOuterBottom, bottomRightMid);
                    AddPoly(bottomRightInner, bottomRightMid, bottomOuterRight, bottomInner);
                    AddPoly(bottomInner, bottomOuterRight, bottomOuterLeft);
                    AddPoly(bottomInner, bottomOuterLeft, bottomLeftMid, bottomLeftInner);

                    if (row == 0 || (isOddRow && col == 0))
                    {
                        AddPoly(topInner, topLeftInner, topLeftMid, topOuterLeft);
                    }

                    if (row == 0)
                    {
                        AddPoly(topOuterLeft, topOuterRight, topInner);
                    }

                    if (row == 0 || (isOddRow && col == width - 1))
                    {
                        AddPoly(topInner, topOuterRight, topRightMid, topRightInner);
                        AddPoly(topRightInner, topRightMid, rightOuterTop);
                    }

                    if (col == 0)
                    {
                        AddPoly(bottomLeftInner, bottomLeftMid, leftOuterBottom);
                        AddPoly(bottomLeftInner, leftOuterBottom, leftOuterTop, topLeftInner);
                        if (row == 0 || isOddRow)
                        {
                            AddPoly(topLeftInner, leftOuterTop, topLeftMid);
                        }
                    }
                }
            }

            CreateCells();
            NormalizeCoordinates();
        }

        private void ConstructAsymmetricPentagons(int layersCount)
        {
            int layers = Math.Max(1, layersCount);
            ApproxPointStorage storage = new ApproxPointStorage(0.001f);
            List<int> intersects = new List<int>();

            float angle60 = MathF.PI / 3f;
            float angle20 = angle60 / 3f;
            float angle100 = angle60 + 2f * angle20;
            float angle120 = angle100 + angle20;
            float angle140 = angle120 + angle20;
            float angle200 = 2f * angle100;
            float angle240 = 2f * angle120;
            float sideLen = 1.0f;

            (float X, float Y) Rotate((float X, float Y) pt, float rad) =>
                (pt.X * MathF.Cos(rad) - pt.Y * MathF.Sin(rad), pt.X * MathF.Sin(rad) + pt.Y * MathF.Cos(rad));

            (float X, float Y)[] CalcVertices((float X, float Y) start, float angle, bool mirrored)
            {
                float a1 = mirrored ? angle + angle100 : angle + angle20;
                float a2 = angle + angle120;
                float a3 = mirrored ? angle + angle240 : angle + angle200;

                (float X, float Y) v0 = start;
                (float X, float Y) v1 = (v0.X + MathF.Sin(angle) * sideLen, v0.Y - MathF.Cos(angle) * sideLen);
                (float X, float Y) v2 = (v1.X + MathF.Sin(a1) * sideLen, v1.Y - MathF.Cos(a1) * sideLen);
                (float X, float Y) v3 = (v2.X + MathF.Sin(a2) * sideLen, v2.Y - MathF.Cos(a2) * sideLen);
                (float X, float Y) v4 = (v3.X + MathF.Sin(a3) * sideLen, v3.Y - MathF.Cos(a3) * sideLen);
                return new[] { v0, v1, v2, v3, v4 };
            }

            void AddPentagonPiece((float X, float Y) start, float angle, bool mirrored = false)
            {
                intersects.Clear();
                foreach (var pt in CalcVertices(start, angle, mirrored))
                    AddIntersection(storage, intersects, pt.X, pt.Y);
                AddPolyBoundry(intersects);
            }

            // Layer 0: 6 pentagons meeting at center (0, 0)
            for (int t = 0; t < 6; t++)
                AddPentagonPiece((0, 0), t * angle60);

            if (layers > 1)
            {
                var l0Sec0 = CalcVertices((0, 0), 0, false);
                var l1Sec0 = CalcVertices(l0Sec0[4], angle20, false);

                // Layer 1: 6 pentagons
                for (int t = 0; t < 6; t++)
                    AddPentagonPiece(Rotate(l0Sec0[4], t * angle60), angle20 + t * angle60);

                if (layers > 2)
                {
                    // Layer 2: 6 pentagons
                    for (int t = 0; t < 6; t++)
                        AddPentagonPiece(Rotate(l1Sec0[3], t * angle60), angle20 + angle140 + t * angle60);

                    if (layers > 3)
                    {
                        // Sector 0 seeds for Layers 3 and 4
                        var p3_0 = CalcVertices(l1Sec0[2], angle20 + angle60, false);
                        var p3_1 = CalcVertices(p3_0[4], angle20 + angle60 + angle20, false);
                        var p3_2 = CalcVertices(p3_1[4], angle20 + angle60 + 2f * angle20, false);
                        (float X, float Y)[] seedL3 = new[] { p3_0[0], p3_1[0], p3_2[0] };

                        float uAng = (angle20 + angle60) - angle60;
                        var p4_0 = CalcVertices(p3_0[3], uAng, true);
                        var p4_1 = CalcVertices(p3_1[3], uAng + angle20, true);
                        var p4_2 = CalcVertices(p3_2[3], uAng + 2f * angle20, true);
                        (float X, float Y)[] seedL4 = new[] { p4_0[0], p4_1[0], p4_2[0] };

                        // Radial step vector (horizontal reflection of seedL3)
                        (float X, float Y)[] stepS = new[]
                        {
                            (seedL3[2].X, -seedL3[2].Y),
                            (seedL3[1].X, -seedL3[1].Y),
                            (seedL3[0].X, -seedL3[0].Y)
                        };

                        // Intra-group step vectors along the radial rays
                        (float X, float Y)[] stepD = new[]
                        {
                            (-MathF.Sin(angle20 * 1), MathF.Cos(angle20 * 1)),
                            (-MathF.Sin(angle20 * 2), MathF.Cos(angle20 * 2)),
                            (-MathF.Sin(angle20 * 3), MathF.Cos(angle20 * 3))
                        };

                        // Closed-form generation for arbitrary layers >= 3 without tracking previous layers
                        for (int l = 3; l < layers; l++)
                        {
                            bool isOdd = (l % 2 != 0);
                            int m = (l - 1) / 2;
                            bool mirrored = !isOdd;
                            float baseAngle = isOdd ? 4f * angle20 : angle20; // 80 deg (odd) or 20 deg (even)

                            for (int t = 0; t < 6; t++)
                            {
                                float rot = t * angle60;
                                for (int g = 0; g < 3; g++)
                                {
                                    (float X, float Y) baseSeed = isOdd ? seedL3[g] : seedL4[g];
                                    (float X, float Y) baseV0 = (baseSeed.X + (m - 1) * stepS[g].X, baseSeed.Y + (m - 1) * stepS[g].Y);
                                    float ang = baseAngle + g * angle20;

                                    for (int k = 0; k < m; k++)
                                    {
                                        (float X, float Y) v0Sec0 = (baseV0.X + k * stepD[g].X, baseV0.Y + k * stepD[g].Y);
                                        AddPentagonPiece(Rotate(v0Sec0, rot), ang + rot, mirrored);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            CreateCells();
            NormalizeCoordinates();
        }

        private void ConstructSquare3(int width, int height)
        {
            ApproxPointStorage storage = new ApproxPointStorage(0.001f);
            List<int> intersects = new List<int>();

            float side = 1.0f;
            float halfSide = side / 2f;
            float triangleHeight = MathF.Sqrt(3f) * halfSide; // altitude of equilateral triangle (Square2's triangleBit)
            float colSpacing = triangleHeight + halfSide;
            float rowSpacing = triangleHeight + halfSide;

            void AddPoly(params (float X, float Y)[] pts)
            {
                intersects.Clear();
                foreach (var pt in pts)
                    AddIntersection(storage, intersects, pt.X, pt.Y);
                AddPolyBoundry(intersects);
            }

            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    // Alternating checkerboard tilt: squares tilt at +/- 30 degrees
                    bool tiltClockwise = (row + col) % 2 == 0;

                    float cellX = colSpacing * col + (tiltClockwise ? halfSide : 0f);
                    float cellY = halfSide + rowSpacing * row + (tiltClockwise ? 0f : halfSide);

                    var sqTopLeft = (cellX, cellY);
                    var sqTopRight = (cellX + triangleHeight, tiltClockwise ? cellY + halfSide : cellY - halfSide);
                    var sqBottomRight = (cellX + triangleHeight + (tiltClockwise ? -halfSide : halfSide),
                                         cellY + triangleHeight + (tiltClockwise ? halfSide : -halfSide));
                    var sqBottomLeft = (cellX + (tiltClockwise ? -halfSide : halfSide), cellY + triangleHeight);

                    // 1. Tilted square
                    AddPoly(sqTopLeft, sqTopRight, sqBottomRight, sqBottomLeft);

                    // 2. Top boundary triangle (row == 0)
                    if (row == 0)
                    {
                        var topPeak = (cellX + (tiltClockwise ? triangleHeight : 0f),
                                       tiltClockwise ? cellY - halfSide : cellY - side);
                        AddPoly(sqTopLeft, topPeak, sqTopRight);
                    }

                    // 3. Bottom triangle
                    var bottomPeak = (cellX + (tiltClockwise ? -halfSide : triangleHeight + halfSide),
                                      cellY + triangleHeight + (tiltClockwise ? side : halfSide));
                    AddPoly(bottomPeak, sqBottomLeft, sqBottomRight);

                    // 4. Right triangle (col < width - 1)
                    if (col < width - 1)
                    {
                        var rightPeak = (cellX + triangleHeight + (tiltClockwise ? halfSide : side),
                                         cellY + (tiltClockwise ? triangleHeight + halfSide : -halfSide));
                        AddPoly(sqTopRight, rightPeak, sqBottomRight);
                    }
                }
            }

            CreateCells();
            NormalizeCoordinates();
        }

        private void ConstructPentagonHexagon2(int width, int height)
        {
            ApproxPointStorage storage = new ApproxPointStorage(0.001f);
            List<int> intersects = new List<int>();

            float side = 1.0f;
            float sqrt3 = MathF.Sqrt(3f);
            float halfSide = 0.5f * side;
            float triangleHeight = sqrt3 * halfSide; // altitude of 30-60-90 triangle
            float hexWidth = 2f * triangleHeight;    // full width of the hexagon
            float colSpacing = hexWidth;
            float rowSpacing = 4f * side;

            void AddPoly(params (float X, float Y)[] pts)
            {
                intersects.Clear();
                foreach (var pt in pts)
                    AddIntersection(storage, intersects, pt.X, pt.Y);
                AddPolyBoundry(intersects);
            }

            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    float cellX = col * colSpacing;
                    float cellY = row * rowSpacing;

                    // 1. Top pentagon (flat top at cellY, pointing downward)
                    var topTopLeft = (cellX, cellY);
                    var topTopRight = (cellX + hexWidth, cellY);
                    var topMidRight = (cellX + hexWidth, cellY + side);
                    var topPeak = (cellX + triangleHeight, cellY + side + halfSide);
                    var topMidLeft = (cellX, cellY + side);
                    AddPoly(topTopLeft, topTopRight, topMidRight, topPeak, topMidLeft);

                    // 2. Bottom pentagon (flat bottom at cellY + rowSpacing, pointing upward)
                    float botY = cellY + rowSpacing;
                    var botBotLeft = (cellX, botY);
                    var botMidLeft = (cellX, botY - side);
                    var botPeak = (cellX + triangleHeight, botY - side - halfSide);
                    var botMidRight = (cellX + hexWidth, botY - side);
                    var botBotRight = (cellX + hexWidth, botY);
                    AddPoly(botBotLeft, botMidLeft, botPeak, botMidRight, botBotRight);

                    // 3. Hexagon (connecting the pentagons to the right)
                    float hexCenterColX = cellX + hexWidth;
                    float hexTopY = cellY + side;
                    var hexTop = (hexCenterColX, hexTopY);
                    var hexTopRight = (hexCenterColX + triangleHeight, hexTopY + halfSide);
                    var hexBotRight = (hexCenterColX + triangleHeight, hexTopY + halfSide + side);
                    var hexBottom = (hexCenterColX, hexTopY + 2f * side);
                    var hexBotLeft = (hexCenterColX - triangleHeight, hexTopY + halfSide + side);
                    var hexTopLeft = (hexCenterColX - triangleHeight, hexTopY + halfSide);
                    AddPoly(hexTop, hexTopRight, hexBotRight, hexBottom, hexBotLeft, hexTopLeft);
                }
            }

            CreateCells();
            NormalizeCoordinates();
        }

#endregion
    }
}
