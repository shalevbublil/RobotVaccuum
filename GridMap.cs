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

        // BFS algorithm to find the shortest path to Home (0,0) avoiding obstacles
        public List<Tuple<int, int>>? FindPathToHome(int startR, int startC)
        {
            var target = new Tuple<int, int>(0, 0);
            var start = new Tuple<int, int>(startR, startC);

            if (start.Equals(target)) return new List<Tuple<int, int>>();

            // Queue for BFS traversal
            var queue = new Queue<Tuple<int, int>>();
            // Track visited cells
            var visited = new HashSet<Tuple<int, int>>();
            // Map to reconstruct the path: key = cell, value = parent cell
            var parentMap = new Dictionary<Tuple<int, int>, Tuple<int, int>>();

            queue.Enqueue(start);
            visited.Add(start);

            bool found = false;

            // 4-way movement directions (Up, Down, Left, Right)
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

                    // Check grid boundaries, obstacles, and visited status
                    if (nextR >= 0 && nextR < _rows && nextC >= 0 && nextC < _cols &&
                        _grid[nextR, nextC] != Obstacle && !visited.Contains(next))
                    {
                        visited.Add(next);
                        parentMap[next] = current;
                        queue.Enqueue(next);
                    }
                }
            }

            if (!found) return null; // No available path

            // Reconstruct path from target back to start
            var path = new List<Tuple<int, int>>();
            var curr = target;
            while (!curr.Equals(start))
            {
                path.Add(curr);
                curr = parentMap[curr];
            }
            path.Reverse(); // Reverse so it starts from robot's current position and ends at Home
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
                        Console.Write("R "); // Robot's current position
                    else
                        Console.Write(_grid[r, c] + " ");
                }
                Console.WriteLine();
            }
            Console.WriteLine("------------------------");
        }
    }
}