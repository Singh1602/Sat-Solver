using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace SudokuSatSolver
{
    public class SATSolver
    {
        // Method to solve the SAT problem for given clauses and variable assignments
        public bool Solve(List<List<int>> clauses, Dictionary<int, bool> assignments)
        {
            // Check if there are no clauses left, indicating the problem is solved
            if (!clauses.Any())
            {
                return true;
            }

            // If there exists any clause that is empty, return false as it indicates a contradiction
            if (clauses.Any(clause => !clause.Any()))
            {
                return false;
            }

            // Find and process all unit clauses for unit propagation
            var unitClauses = clauses.Where(c => c.Count == 1).ToList();
            foreach (var unit in unitClauses)
            {
                int var = unit.First();
                assignments[Math.Abs(var)] = var > 0;
                var newClauses = Propagate(clauses, var);
                if (newClauses == null)
                {
                    return false;  // Propagation led to a contradiction
                }
                clauses = newClauses;
            }

            // Select an unassigned variable to try and solve next
            int? unassignedVariable = clauses.SelectMany(c => c)
                .Where(v => !assignments.ContainsKey(Math.Abs(v)))
                .Select(Math.Abs)
                .FirstOrDefault();

            if (!unassignedVariable.HasValue)
            {
                return true;  // All variables are assigned and no contradictions were found
            }

            // Decode the variable to get row, column, and number (used in debugging or complex decision making)
            DecodeVariable(unassignedVariable.Value, out int row, out int col, out int num);
            int variable = unassignedVariable.Value;

            // Try assigning both true and false to the unassigned variable and recurse
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

            return false;  // No solution was found with either assignment, backtrack needed
        }

        // Method to propagate the effects of a variable assignment throughout the remaining clauses
        public List<List<int>> Propagate(List<List<int>> clauses, int var)
        {
            List<List<int>> updatedClauses = new List<List<int>>();
            foreach (var clause in clauses)
            {
                if (clause.Contains(var))
                {
                    continue;  // Clause is satisfied, skip it
                }

                var updatedClause = clause.Where(literal => literal != -var).ToList();
                if (!updatedClause.Any())
                {
                    return null;  // A contradiction was found, return null to indicate failure
                }

                updatedClauses.Add(updatedClause);
            }

            return updatedClauses;
        }

        // Method to generate cell constraints for a Sudoku grid
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
                        clause.Add(EncodeVariable(row, col, num));  // Ensure each cell has at least one number
                    }
                    clauses.Add(clause);
                }
            }
            return clauses;
        }

        // Method to add Sudoku row constraints
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
                            // Add constraints to ensure each number appears only once per row
                            clauses.Add(new List<int> { -EncodeVariable(row, col1, num), -EncodeVariable(row, col2, num) });
                        }
                    }
                }
            }
            return clauses;
        }

        // Method to add Sudoku column constraints
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
                            // Add constraints to ensure each number appears only once per column
                            clauses.Add(new List<int> { -EncodeVariable(row1, col, num), -EncodeVariable(row2, col, num) });
                        }
                    }
                }
            }
            return clauses;
        }

        // Method to add Sudoku block constraints
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
                                // Add constraints to ensure each number appears only once per block
                                clauses.Add(new List<int> { -EncodeVariable(row1, col1, num), -EncodeVariable(row2, col2, num) });
                            }
                        }
                    }
                }
            }
            return clauses;
        }

        // Method to add clauses for prefilled cells
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
                        // Add a unit clause for each prefilled cell
                        clauses.Add(new List<int> { EncodeVariable(row + 1, col + 1, num) });
                    }
                }
            }
            return clauses;
        }

        // Utility to encode a variable from row, col, and num to a single integer
        internal static int EncodeVariable(int row, int col, int num)
        {
            const int SIZE = 9;
            return (row - 1) * SIZE * SIZE + (col - 1) * SIZE + num;
        }

        // Utility to decode a variable back to row, col, and num
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
            int[,] prefilled = new int[9, 9];

            Console.WriteLine("Please enter the Sudoku puzzle:");
            Console.WriteLine("Enter numbers row by row, use '0' for empty cells.");

            // Input each row of the Sudoku puzzle
            for (int i = 0; i < 9; i++)
            {
                Console.WriteLine($"Enter row {i + 1}, use spaces or commas between numbers:");
                string input = Console.ReadLine();
                // Split input based on spaces or commas and parse integers
                string[] numbers = input.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                
                for (int j = 0; j < 9; j++)
                {
                    if (int.TryParse(numbers[j], out int num) && num >= 0 && num <= 9)
                    {
                        prefilled[i, j] = num;
                    }
                    else
                    {
                        Console.WriteLine("Invalid input, please enter only numbers between 0 and 9.");
                        j--; // Decrease j to repeat input for this cell
                    }
                }
            }

            var solver = new SATSolver();
            List<List<int>> clauses = SATSolver.GenerateCellConstraints();
            clauses = SATSolver.AddRowConstraint(clauses);
            clauses = SATSolver.AddColumnConstraint(clauses);
            clauses = SATSolver.AddBlockConstraint(clauses);
            clauses = SATSolver.AddPrefilledCells(clauses, prefilled);

            var assignments = new Dictionary<int, bool>();
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
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
            Console.WriteLine($"Time taken to solve: {stopwatch.Elapsed.TotalSeconds} seconds");
        }
    }

}
