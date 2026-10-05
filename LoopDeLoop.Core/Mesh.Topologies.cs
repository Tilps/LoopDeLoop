using System;
using System.Collections.Generic;

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
    }
}
