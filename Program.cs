using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace SudokuSatSolver
{
    public class SATSolver
    {
        public bool Solve(List<List<int>> clauses, Dictionary<int, bool> assignments)
        {
            if (!clauses.Any())
            {
                return true;
            }

            if (clauses.Any(clause => !clause.Any()))
            {
                return false;
            }

            var unitClauses = clauses.Where(c => c.Count == 1).ToList();
            foreach (var unit in unitClauses)
            {
                int var = unit.First();
                assignments[Math.Abs(var)] = var > 0;
                var newClauses = Propagate(clauses, var);
                if (newClauses == null)
                {
                    return false;
                }
                clauses = newClauses;
            }

            int? unassignedVariable = clauses.SelectMany(c => c)
                .Where(v => !assignments.ContainsKey(Math.Abs(v)))
                .Select(Math.Abs)
                .FirstOrDefault();

            if (!unassignedVariable.HasValue)
            {
                return true;
            }

            DecodeVariable(unassignedVariable.Value, out int row, out int col, out int num);
            int variable = unassignedVariable.Value;
            foreach (bool value in new[] { true, false })
            {
                var newAssignments = new Dictionary<int, bool>(assignments);
                newAssignments[variable] = value;

                var propagatedClauses = Propagate(clauses, value ? variable : -variable);
                if (propagatedClauses != null && Solve(propagatedClauses, newAssignments))
                {
                    foreach (var kvp in newAssignments)
                        assignments[kvp.Key] = kvp.Value;

                    return true;
                }
            }

            return false;
        }

        public List<List<int>> Propagate(List<List<int>> clauses, int var)
        {
            List<List<int>> updatedClauses = new List<List<int>>();
            foreach (var clause in clauses)
            {
                if (clause.Contains(var))
                {
                    continue;
                }

                var updatedClause = clause.Where(literal => literal != -var).ToList();
                if (!updatedClause.Any())
                {
                    return null;
                }

                updatedClauses.Add(updatedClause);
            }

            return updatedClauses;
        }

        public static List<List<int>> GenerateCellConstraints()
        {
            const int SIZE = 9;
            List<List<int>> clauses = new List<List<int>>();
            for (int row = 1; row <= SIZE; row++)
            {
                for (int col = 1; col <= SIZE; col++)
                {
                    List<int> clause = new List<int>();
                    for (int num = 1; num <= SIZE; num++)
                    {
                        clause.Add(EncodeVariable(row, col, num));
                    }
                    clauses.Add(clause);
                }
            }
            return clauses;
        }

        public static List<List<int>> AddRowConstraint(List<List<int>> clauses)
        {
            const int SIZE = 9;
            for (int num = 1; num <= SIZE; num++)
            {
                for (int row = 1; row <= SIZE; row++)
                {
                    for (int col1 = 1; col1 <= SIZE; col1++)
                    {
                        for (int col2 = col1 + 1; col2 <= SIZE; col2++)
                        {
                            clauses.Add(new List<int> { -EncodeVariable(row, col1, num), -EncodeVariable(row, col2, num) });
                        }
                    }
                }
            }
            return clauses;
        }

        public static List<List<int>> AddColumnConstraint(List<List<int>> clauses)
        {
            const int SIZE = 9;
            for (int num = 1; num <= SIZE; num++)
            {
                for (int col = 1; col <= SIZE; col++)
                {
                    for (int row1 = 1; row1 <= SIZE; row1++)
                    {
                        for (int row2 = row1 + 1; row2 <= SIZE; row2++)
                        {
                            clauses.Add(new List<int> { -EncodeVariable(row1, col, num), -EncodeVariable(row2, col, num) });
                        }
                    }
                }
            }
            return clauses;
        }

        public static List<List<int>> AddBlockConstraint(List<List<int>> clauses)
        {
            const int SIZE = 9;
            const int BLOCK_SIZE = 3;
            for (int num = 1; num <= SIZE; num++)
            {
                for (int blockRow = 0; blockRow < BLOCK_SIZE; blockRow++)
                {
                    for (int blockCol = 0; blockCol < BLOCK_SIZE; blockCol++)
                    {
                        for (int pos1 = 0; pos1 < SIZE; pos1++)
                        {
                            int row1 = blockRow * BLOCK_SIZE + pos1 / BLOCK_SIZE + 1;
                            int col1 = blockCol * BLOCK_SIZE + pos1 % BLOCK_SIZE + 1;
                            for (int pos2 = pos1 + 1; pos2 < SIZE; pos2++)
                            {
                                int row2 = blockRow * BLOCK_SIZE + pos2 / BLOCK_SIZE + 1;
                                int col2 = blockCol * BLOCK_SIZE + pos2 % BLOCK_SIZE + 1;
                                clauses.Add(new List<int> { -EncodeVariable(row1, col1, num), -EncodeVariable(row2, col2, num) });
                            }
                        }
                    }
                }
            }
            return clauses;
        }

        public static List<List<int>> AddPrefilledCells(List<List<int>> clauses, int[,] prefilled)
        {
            const int SIZE = 9;
            for (int row = 0; row < SIZE; row++)
            {
                for (int col = 0; col < SIZE; col++)
                {
                    int num = prefilled[row, col];
                    if (num > 0)
                    {
                        clauses.Add(new List<int> { EncodeVariable(row + 1, col + 1, num) });
                    }
                }
            }
            return clauses;
        }

        internal static int EncodeVariable(int row, int col, int num)
        {
            const int SIZE = 9;
            return (row - 1) * SIZE * SIZE + (col - 1) * SIZE + num;
        }

        public static void DecodeVariable(int variable, out int row, out int col, out int num)
        {
            const int SIZE = 9;
            variable -= 1;
            num = variable % SIZE + 1;
            variable /= SIZE;
            col = variable % SIZE + 1;
            row = variable / SIZE + 1;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var solver = new SATSolver();
            List<List<int>> clauses = SATSolver.GenerateCellConstraints();
            clauses = SATSolver.AddRowConstraint(clauses);
            clauses = SATSolver.AddColumnConstraint(clauses);
            clauses = SATSolver.AddBlockConstraint(clauses);

            int[,] prefilled = new int[,]
            {
                {0, 2, 0, 0, 0, 0, 0, 0, 0},
                {0, 0, 0, 6, 0, 0, 0, 0, 3},
                {0, 7, 4, 0, 8, 0, 0, 0, 0},
                {0, 0, 0, 0, 0, 3, 0, 0, 2},
                {0, 8, 0, 0, 4, 0, 0, 1, 0},
                {6, 0, 0, 5, 0, 0, 0, 0, 0},
                {0, 0, 0, 0, 1, 0, 7, 8, 0},
                {5, 0, 0, 0, 0, 9, 0, 0, 0},
                {0, 0, 0, 0, 0, 0, 0, 4, 0}
            };

            clauses = SATSolver.AddPrefilledCells(clauses, prefilled);

            var assignments = new Dictionary<int, bool>();
            Stopwatch stopwatch = Stopwatch.StartNew();
            bool isSolvable = solver.Solve(clauses, assignments);
            stopwatch.Stop();

            int[,] solution = new int[9, 9];
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    if (prefilled[row, col] == 0)
                    {
                        solution[row, col] = -1; // Mark as unsolved
                    }
                    else
                    {
                        solution[row, col] = prefilled[row, col]; // Pre-fill with given number
                    }
                }
            }

            if (isSolvable)
            {
                foreach (var assignment in assignments.Where(a => a.Value))
                {
                    SATSolver.DecodeVariable(assignment.Key, out int r, out int c, out int n);
                    if (solution[r - 1, c - 1] == -1)
                    {
                        solution[r - 1, c - 1] = n; // Only update unsolved cells
                    }
                }

                Console.WriteLine("Solved Sudoku Grid:");
                for (int row = 0; row < 9; row++)
                {
                    for (int col = 0; col < 9; col++)
                    {
                        Console.Write(solution[row, col] + " ");
                    }
                    Console.WriteLine();
                }
            }
            else
            {
                Console.WriteLine("No solution found.");
            }
        }
    }
}
