// See https://aka.ms/new-console-template for more information
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
        Console.WriteLine($"Starting Solve with {clauses.Count} clauses and {assignments.Count} assignments.");
        if (!clauses.Any())
        {
        Console.WriteLine("No clauses left, the problem is solved.");
        // If there are no clauses left, the problem is solved
        return true;
        }

        if (clauses.Any(clause => !clause.Any()))
        {
        Console.WriteLine("No clauses left, the problem is solved.");
        // If there's an empty clause, there's a contradiction
        return false;
        }

        // Unit Propagation

var unitClauses = clauses.Where(c => c.Count == 1).ToList();
    foreach (var unit in unitClauses)
    {
        int var = unit.First();
        assignments[Math.Abs(var)] = var > 0;
        var newClauses = Propagate(clauses, var);
        if (newClauses == null)
        {
            Console.WriteLine("Contradiction found during unit propagation.");
            return false; // Contradiction found during propagation
        }
        clauses = newClauses;
    }

    // Choose a variable that has not yet been assigned
    int? unassignedVariable = clauses.SelectMany(c => c)
                                     .Where(v => !assignments.ContainsKey(Math.Abs(v)))
                                     .Select(Math.Abs)
                                     .FirstOrDefault();
    if (!unassignedVariable.HasValue)
    {
        return true; // All variables are assigned and no contradictions
    }

    // Try assigning true and false to the unassigned variable
    int variable = unassignedVariable.Value;
    foreach (bool value in new[] { true, false })
    {
        var newAssignments = new Dictionary<int, bool>(assignments);
        newAssignments[variable] = value;

        var propagatedClauses = Propagate(clauses, value ? variable : -variable);
        if (propagatedClauses != null && Solve(propagatedClauses, newAssignments))
        {
            foreach (var kvp in newAssignments)
                assignments[kvp.Key] = kvp.Value; // Copy successful assignments back

            return true;
        }
    }

    return false; // No solution found with either assignment, backtrack

        

        // Select a variable to assign
        if (!clauses.SelectMany(c => c).Distinct().Any())
        {
        // This avoids the InvalidOperationException if there are no variables left to assign
        return true;
        }
       // int variable = clauses.SelectMany(c => c).Distinct().Min(Math.Abs);
        //Console.WriteLine($"Selecting variable {variable} for assignment.");

        var tryTrue = new Dictionary<int, bool>(assignments) { [variable] = true };
        var tryFalse = new Dictionary<int, bool>(assignments) { [variable] = false };

        // Recursively attempt to solve with variable set to true or false
        //Console.WriteLine($"Recursively solving with variable {variable} set to true.");
        var propagatedTrue = Propagate(clauses, variable);
        var propagatedFalse = Propagate(clauses, -variable);
        if ((propagatedTrue != null && Solve(propagatedTrue, tryTrue)) ||
        (propagatedFalse != null && Solve(propagatedFalse, tryFalse)))
        {
        return true;
        }

        return false;
        }

    

        private List<List<int>> Propagate(List<List<int>> clauses, int var)
    {
        List<List<int>> updatedClauses = new List<List<int>>();
        bool foundContradiction = false;

        foreach (var clause in clauses)
        {
            // If the clause contains the variable, it's satisfied; skip it.
            if (clause.Contains(var))
            {
                continue;
            }

            // Remove the negation of the assigned variable if it exists in the clause.
            var updatedClause = clause.Where(literal => literal != -var).ToList();

            // Check for a contradiction: if removing the negated variable results in an empty clause.
            if (!updatedClause.Any())
            {
                return null;
            }

            // Otherwise, add the updated clause to the list.
            updatedClauses.Add(updatedClause);
        }

        // If a contradiction was found (an empty clause), return null or handle appropriately.
        // This indicates the current assignment path cannot lead to a solution.
        if (foundContradiction)
        {
            return null; // Using null to indicate a contradiction was found. Caller needs to handle this.
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
                        int var = EncodeVariable(row, col, num);
                        clause.Add(var);
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
                            int var1 = EncodeVariable(row, col1, num);
                            int var2 = EncodeVariable(row, col2, num);
                            clauses.Add(new List<int> { -var1, -var2 });
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
                            int var1 = EncodeVariable(row1, col, num);
                            int var2 = EncodeVariable(row2, col, num);
                            clauses.Add(new List<int> { -var1, -var2 });
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
                            int row1 = blockRow * BLOCK_SIZE + pos1 / BLOCK_SIZE;
                            int col1 = blockCol * BLOCK_SIZE + pos1 % BLOCK_SIZE;

                            for (int pos2 = pos1 + 1; pos2 < SIZE; pos2++)
                            {
                                int row2 = blockRow * BLOCK_SIZE + pos2 / BLOCK_SIZE;
                                int col2 = blockCol * BLOCK_SIZE + pos2 % BLOCK_SIZE;

                                int var1 = EncodeVariable(row1 + 1, col1 + 1, num);
                                int var2 = EncodeVariable(row2 + 1, col2 + 1, num);
                                clauses.Add(new List<int> { -var1, -var2 });
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
                        int var = EncodeVariable(row + 1, col + 1, num);
                        // For a pre-filled cell, add a clause that this specific number is true in this position
                        clauses.Add(new List<int> { var });
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
        variable -= 1; // Adjust back to zero-based index.
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
        SATSolver.DecodeVariable(SATSolver.EncodeVariable(1, 1, 9), out int testRow, out int testCol, out int testNum);
        Console.WriteLine($"Encoding/Decoding Test: Row: {testRow}, Column: {testCol}, Number: {testNum}");
            


            var solver = new SATSolver();

            // Initialize clauses with cell constraints for Sudoku
            List<List<int>> clauses = SATSolver.GenerateCellConstraints();

            // Add constraints
            clauses = SATSolver.AddRowConstraint(clauses);
            clauses = SATSolver.AddColumnConstraint(clauses);
            clauses = SATSolver.AddBlockConstraint(clauses);
            //Console.WriteLine($"Total clauses after initialization: {clauses.Count}");


            // Assuming a predefined Sudoku puzzle (replace with actual puzzle)
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
                 //the last 2 digits should be 6 and 7      
            };
            // Before calling the solver
            List<(int row, int col)> emptyCells = new List<(int row, int col)>();
            for (int row = 0; row < 9; row++)
            {
            for (int col = 0; col < 9; col++)
            {
                if (prefilled[row, col] < 1 || prefilled[row, col] > 9 || prefilled[row, col] <= 0)
                {
                    emptyCells.Add((row + 1, col + 1)); // Store positions as 1-based indexes
                }
            }
            }

            clauses = SATSolver.AddPrefilledCells(clauses, prefilled);
            //Console.WriteLine($"Total clauses after initialization: {clauses.Count}");
            //Console.WriteLine("Empty Cells (to be solved):");
            foreach (var (row, col) in emptyCells)
            {
            int currentValue = prefilled[row - 1, col - 1]; // Adjust for zero-based indexing
            Console.WriteLine($"Cell at Row: {row}, Column: {col} has value: {currentValue}");
            }


            var assignments = new Dictionary<int, bool>();
            Stopwatch stopwatch = Stopwatch.StartNew();
            bool isSolvable = solver.Solve(clauses, assignments);
            stopwatch.Stop();
           

            Console.WriteLine($"Solvable: {isSolvable}");
            Console.WriteLine($"Time taken: {stopwatch.Elapsed.TotalSeconds} seconds");

            
             int[,] solution = new int[9, 9]; // Initialize an empty solution grid.
            if (isSolvable)
            {

                Console.WriteLine("Assignments:");
                foreach (var kvp in assignments)
                {
                    SATSolver.DecodeVariable(kvp.Key, out int row, out int col, out int num);
                    Console.WriteLine($"Var: {kvp.Key} Assigned: {kvp.Value} => Cell: ({row}, {col}) Number: {num}");
                }
             foreach (var assignment in assignments.Where(a => a.Value))
            {
            SATSolver.DecodeVariable(assignment.Key, out int r, out int c, out int n); // Use different variable names here to avoid conflict
            //Console.WriteLine("hello "+ n);
            solution[r - 1, c - 1] = n; // Adjust for zero-based indexing.
            }
                                Console.WriteLine("Assignments:");
    for (int i = 1; i <= 81; i++) // Assuming 81 variables for a standard Sudoku puzzle
    {
        if (assignments.TryGetValue(i, out bool isAssigned))
        {
            Console.WriteLine($"Variable {i} assigned: {isAssigned}");
        }
        else
        {
            Console.WriteLine($"Variable {i} not assigned");
        }
    }


            }
            if (isSolvable)
{
    Console.WriteLine("Solved Values for Originally Empty Cells:");
    for (int row = 0; row < 9; row++)
    {
        for (int col = 0; col < 9; col++)
        {
            // Check if the original cell was empty (i.e., had a value of 0)
            if (prefilled[row, col] == 0)
            {
                // Print the solved value from the solution array
                int solvedValue = solution[row, col];
                Console.WriteLine($"Replace 0 at Row: {row + 1}, Column: {col + 1} with: {solvedValue}");
                Console.WriteLine($"Cell at Row: {row + 1}, Column: {col + 1} was solved with value: {solution[row, col]}");
            }
        }
    }
    
    
    // Fill in the solution matrix with the solved values  
    foreach (var assignment in assignments.Where(a => a.Value))
    {
        SATSolver.DecodeVariable(assignment.Key, out int row, out int col, out int num);
        solution[row - 1, col - 1] = num;

    }
        // Additional debug information
    Console.WriteLine("Updated solution matrix:");
    for (int row = 0; row < 9; row++)
    {
        for (int col = 0; col < 9; col++)
        {
            // Print the value in the solution matrix
            Console.Write(solution[row, col] + " ");
        }
        Console.WriteLine(); // Newline for next row
    }

    // Print the entire solution matrix
    // Console.WriteLine("Sudoku Solution:");
    // for (int i = 0; i < 9; i++)
    // {
    //     for (int j = 0; j < 9; j++)
    //     {
    //         if (prefilled[i, j] == 0) // If the original cell was a zero, print the solved value
    //         {
    //             Console.Write($"{solution[i, j]} ");
    //         }
    //         else
    //         {
    //             Console.Write($"{prefilled[i, j]} "); // Otherwise, print the original number
    //         }

    //         if ((j + 1) % 3 == 0 && j < 8)
    //         {
    //             Console.Write("| ");
    //         }
    //     }

    //     Console.WriteLine();
    //     if ((i + 1) % 3 == 0 && i < 8)
    //     {
    //         Console.WriteLine("---------------------");
    //     }
    }


            else
            {
                Console.WriteLine("No solution found.");
                Console.WriteLine("End it all");
            }
        }
        
    }
}
