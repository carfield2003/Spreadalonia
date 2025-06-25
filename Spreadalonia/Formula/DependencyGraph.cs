using System;
using System.Collections.Generic;
using System.Linq;

namespace Spreadalonia.Formula
{
    /// <summary>
    /// Manages the dependency graph between cells, enabling cascade updates
    /// when a cell's value changes. Uses topological ordering to ensure
    /// formulas are recalculated in the correct order.
    /// </summary>
    public class DependencyGraph
    {
        // Cell -> set of cells it depends on (precedents)
        private readonly Dictionary<(int, int), HashSet<(int, int)>> _dependencies
            = new Dictionary<(int, int), HashSet<(int, int)>>();

        // Cell -> set of cells that depend on it (dependents)
        private readonly Dictionary<(int, int), HashSet<(int, int)>> _dependents
            = new Dictionary<(int, int), HashSet<(int, int)>>();

        /// <summary>
        /// Updates the dependency information for a formula cell.
        /// </summary>
        /// <param name="cell">The formula cell.</param>
        /// <param name="astNode">The parsed AST of the formula.</param>
        public void UpdateDependencies((int, int) cell, AstNode astNode)
        {
            // Remove old dependencies for this cell
            RemoveDependencies(cell);

            // Collect new cell references
            var refs = CellRefCollector.Collect(astNode);

            // Register new dependencies
            _dependencies[cell] = refs;

            // Register reverse (dependents)
            foreach (var dep in refs)
            {
                if (!_dependents.ContainsKey(dep))
                    _dependents[dep] = new HashSet<(int, int)>();
                _dependents[dep].Add(cell);
            }
        }

        /// <summary>
        /// Removes all dependency information for a cell.
        /// </summary>
        public void RemoveDependencies((int, int) cell)
        {
            // Remove this cell from each of its precedent's dependent lists
            if (_dependencies.TryGetValue(cell, out var oldDeps))
            {
                foreach (var dep in oldDeps)
                {
                    if (_dependents.TryGetValue(dep, out var deps))
                    {
                        deps.Remove(cell);
                        if (deps.Count == 0)
                            _dependents.Remove(dep);
                    }
                }
            }

            _dependencies.Remove(cell);
        }

        /// <summary>
        /// Gets the topological order of cells that need recalculation
        /// starting from a changed cell. Ensures formulas are recomputed
        /// in the correct dependency order.
        /// </summary>
        /// <param name="changedCell">The cell whose value changed.</param>
        /// <returns>Ordered list of cells to recalculate.</returns>
        public List<(int, int)> GetRecalculationOrder((int, int) changedCell)
        {
            var result = new List<(int, int)>();
            var visited = new HashSet<(int, int)>();

            // BFS to find all transitively dependent cells
            var queue = new Queue<(int, int)>();
            queue.Enqueue(changedCell);
            visited.Add(changedCell);

            var allAffected = new HashSet<(int, int)>();

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (_dependents.TryGetValue(current, out var deps))
                {
                    foreach (var dep in deps)
                    {
                        if (!visited.Contains(dep))
                        {
                            visited.Add(dep);
                            queue.Enqueue(dep);
                            allAffected.Add(dep);
                        }
                    }
                }
            }

            // Topological sort of affected cells
            var sorted = TopologicalSort(allAffected);
            return sorted;
        }

        /// <summary>
        /// Topologically sorts a set of cells based on their dependencies.
        /// Uses Kahn's algorithm.
        /// </summary>
        private List<(int, int)> TopologicalSort(HashSet<(int, int)> cells)
        {
            var result = new List<(int, int)>();
            var inDegree = new Dictionary<(int, int), int>();

            // Initialize in-degree; only consider edges within the set
            foreach (var cell in cells)
            {
                inDegree[cell] = 0;
            }

            foreach (var cell in cells)
            {
                if (_dependencies.TryGetValue(cell, out var deps))
                {
                    foreach (var dep in deps)
                    {
                        if (cells.Contains(dep))
                        {
                            int current = inDegree.TryGetValue(cell, out int val) ? val : 0;
                            inDegree[cell] = current + 1;
                        }
                    }
                }
            }

            // Recalculate: for formula cells, we need cells with fewer dependencies first
            // Cells with 0 dependencies within the set go first
            var ready = new Queue<(int, int)>();
            foreach (var cell in cells)
            {
                int deg = inDegree.TryGetValue(cell, out int d) ? d : 0;
                if (deg == 0)
                    ready.Enqueue(cell);
            }

            while (ready.Count > 0)
            {
                var cell = ready.Dequeue();
                result.Add(cell);

                if (_dependents.TryGetValue(cell, out var deps))
                {
                    foreach (var dep in deps)
                    {
                        if (cells.Contains(dep))
                        {
                            int current = inDegree.TryGetValue(dep, out int deg) ? deg : 0;
                            inDegree[dep] = current - 1;
                            if (inDegree[dep] == 0)
                                ready.Enqueue(dep);
                        }
                    }
                }
            }

            // Add any remaining cells (possible circular dependency, add them anyway)
            foreach (var cell in cells)
            {
                if (!result.Contains(cell))
                    result.Add(cell);
            }

            return result;
        }

        /// <summary>
        /// Clears all dependency information.
        /// </summary>
        public void Clear()
        {
            _dependencies.Clear();
            _dependents.Clear();
        }
    }
}
