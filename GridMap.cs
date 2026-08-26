using System;
using System.Collections.Generic;

namespace RobotVacuum
{
    public class GridMap
    {
        private readonly char[,] _grid;
        private readonly int _rows;
        private readonly int _cols;

        // Cell representations in the grid map
        public const char Empty = '.';
        public const char Obstacle = 'X';
        public const char Cleaned = 'C';
        public const char Home = 'H';

        public GridMap(int rows = 10, int cols = 10)
        {
            _rows = rows;
            _cols = cols;
            _grid = new char[rows, cols];
            InitializeMap();
        }

        private void InitializeMap()
        {
            // Fill the map with empty cells
            for (int r = 0; r < _rows; r++)
            {
                for (int c = 0; c < _cols; c++)
                {
                    _grid[r, c] = Empty;
                }
            }

            // Set the charging station (Home) at the top-left corner (0,0)
            _grid[0, 0] = Home;

            // Place static obstacles representing walls or furniture
            _grid[2, 3] = Obstacle; _grid[2, 4] = Obstacle;
            _grid[5, 1] = Obstacle; _grid[5, 2] = Obstacle;
            _grid[7, 6] = Obstacle; _grid[7, 7] = Obstacle;
        }

        // Check if a coordinate is blocked by an obstacle
        public bool IsObstacle(int r, int c)
        {
            if (r < 0 || r >= _rows || c < 0 || c >= _cols) return true;
            return _grid[r, c] == Obstacle;
        }

        // Check if a cell is considered a perimeter (border wall or adjacent to an obstacle)
        public bool IsPerimeter(int r, int c)
        {
            if (r < 0 || r >= _rows || c < 0 || c >= _cols) return false;
            if (_grid[r, c] == Obstacle) return false;

            // Wall boundary check
            if (r == 0 || r == _rows - 1 || c == 0 || c == _cols - 1) return true;

            // Check if 4-way adjacent to an obstacle
            int[] dRow = { -1, 1, 0, 0 };
            int[] dCol = { 0, 0, -1, 1 };
            for (int i = 0; i < 4; i++)
            {
                int adjR = r + dRow[i];
                int adjC = c + dCol[i];
                if (adjR >= 0 && adjR < _rows && adjC >= 0 && adjC < _cols)
                {
                    if (_grid[adjR, adjC] == Obstacle) return true;
                }
            }

            return false;
        }

        // BFS to find the shortest path back to the charging station (0,0)
        public List<Tuple<int, int>>? FindPathToHome(int startR, int startC)
        {
            var target = new Tuple<int, int>(0, 0);
            var start = new Tuple<int, int>(startR, startC);

            if (start.Equals(target)) return new List<Tuple<int, int>>();

            var queue = new Queue<Tuple<int, int>>();
            var visited = new HashSet<Tuple<int, int>>();
            var parentMap = new Dictionary<Tuple<int, int>, Tuple<int, int>>();

            queue.Enqueue(start);
            visited.Add(start);

            bool found = false;
            int[] dRow = { -1, 1, 0, 0 };
            int[] dCol = { 0, 0, -1, 1 };

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (current.Equals(target))
                {
                    found = true;
                    break;
                }

                for (int i = 0; i < 4; i++)
                {
                    int nextR = current.Item1 + dRow[i];
                    int nextC = current.Item2 + dCol[i];
                    var next = new Tuple<int, int>(nextR, nextC);

                    if (nextR >= 0 && nextR < _rows && nextC >= 0 && nextC < _cols &&
                        _grid[nextR, nextC] != Obstacle && !visited.Contains(next))
                    {
                        visited.Add(next);
                        parentMap[next] = current;
                        queue.Enqueue(next);
                    }
                }
            }

            if (!found) return null;

            var path = new List<Tuple<int, int>>();
            var curr = target;
            while (!curr.Equals(start))
            {
                path.Add(curr);
                curr = parentMap[curr];
            }
            path.Reverse();
            return path;
        }

        // BFS to find the nearest uncleaned perimeter cell ('.')
        public List<Tuple<int, int>>? FindPathToNearestUncleanedPerimeter(int startR, int startC)
        {
            var start = new Tuple<int, int>(startR, startC);
            var queue = new Queue<Tuple<int, int>>();
            var visited = new HashSet<Tuple<int, int>>();
            var parentMap = new Dictionary<Tuple<int, int>, Tuple<int, int>>();

            queue.Enqueue(start);
            visited.Add(start);

            Tuple<int, int>? target = null;
            int[] dRow = { -1, 1, 0, 0 };
            int[] dCol = { 0, 0, -1, 1 };

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                int r = current.Item1;
                int c = current.Item2;

                // Match uncleaned ('.') and perimeter cells
                if (_grid[r, c] == Empty && IsPerimeter(r, c))
                {
                    target = current;
                    break;
                }

                for (int i = 0; i < 4; i++)
                {
                    int nextR = r + dRow[i];
                    int nextC = c + dCol[i];
                    var next = new Tuple<int, int>(nextR, nextC);

                    if (nextR >= 0 && nextR < _rows && nextC >= 0 && nextC < _cols &&
                        _grid[nextR, nextC] != Obstacle && !visited.Contains(next))
                    {
                        visited.Add(next);
                        parentMap[next] = current;
                        queue.Enqueue(next);
                    }
                }
            }

            if (target == null) return null;

            var path = new List<Tuple<int, int>>();
            var curr = target;
            while (!curr.Equals(start))
            {
                path.Add(curr);
                curr = parentMap[curr];
            }
            path.Reverse();
            return path;
        }

        // BFS to find the nearest uncleaned interior cell (not perimeter)
        public List<Tuple<int, int>>? FindPathToNearestUncleanedInterior(int startR, int startC)
        {
            var start = new Tuple<int, int>(startR, startC);
            var queue = new Queue<Tuple<int, int>>();
            var visited = new HashSet<Tuple<int, int>>();
            var parentMap = new Dictionary<Tuple<int, int>, Tuple<int, int>>();

            queue.Enqueue(start);
            visited.Add(start);

            Tuple<int, int>? target = null;
            int[] dRow = { -1, 1, 0, 0 };
            int[] dCol = { 0, 0, -1, 1 };

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                int r = current.Item1;
                int c = current.Item2;

                // Match uncleaned ('.') and interior cells
                if (_grid[r, c] == Empty && !IsPerimeter(r, c))
                {
                    target = current;
                    break;
                }

                for (int i = 0; i < 4; i++)
                {
                    int nextR = r + dRow[i];
                    int nextC = c + dCol[i];
                    var next = new Tuple<int, int>(nextR, nextC);

                    if (nextR >= 0 && nextR < _rows && nextC >= 0 && nextC < _cols &&
                        _grid[nextR, nextC] != Obstacle && !visited.Contains(next))
                    {
                        visited.Add(next);
                        parentMap[next] = current;
                        queue.Enqueue(next);
                    }
                }
            }

            if (target == null) return null;

            var path = new List<Tuple<int, int>>();
            var curr = target;
            while (!curr.Equals(start))
            {
                path.Add(curr);
                curr = parentMap[curr];
            }
            path.Reverse();
            return path;
        }

        public void MarkCleaned(int r, int c)
        {
            if (_grid[r, c] == Empty)
            {
                _grid[r, c] = Cleaned;
            }
        }

        public void PrintMap(int robotR, int robotC)
        {
            Console.WriteLine("\n--- Current Grid Map ---");
            for (int r = 0; r < _rows; r++)
            {
                for (int c = 0; c < _cols; c++)
                {
                    if (r == robotR && c == robotC)
                        Console.Write("R "); // Robot's location
                    else
                        Console.Write(_grid[r, c] + " ");
                }
                Console.WriteLine();
            }
            Console.WriteLine("------------------------");
        }
    }
}