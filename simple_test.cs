using System;
using System.IO;
using System.Linq;
using ktsu.DiffMore.Core;

class SimpleTest
{
    static void Main()
    {
        Console.WriteLine("Testing diff algorithm...");

        // Create test files
        var file1 = "test1.txt";
        var file2 = "test2.txt";

        File.WriteAllLines(file1, new[] { "Line 1", "Line 2", "Line 3" });
        File.WriteAllLines(file2, new[] { "Different Line 1", "Different Line 2", "Different Line 3" });

        Console.WriteLine("File 1 contents:");
        foreach (var line in File.ReadAllLines(file1))
        {
            Console.WriteLine($"  '{line}'");
        }

        Console.WriteLine("File 2 contents:");
        foreach (var line in File.ReadAllLines(file2))
        {
            Console.WriteLine($"  '{line}'");
        }

        var differences = FileDiffer.FindDifferences(file1, file2);

        Console.WriteLine($"\nTotal differences found: {differences.Count}");

        foreach (var diff in differences)
        {
            Console.WriteLine($"Line1: {diff.LineNumber1}, Line2: {diff.LineNumber2}");
            Console.WriteLine($"Content1: '{diff.Content1}'");
            Console.WriteLine($"Content2: '{diff.Content2}'");
            Console.WriteLine("---");
        }

        // Count modifications (where both line numbers > 0)
        var modifications = differences.Count(d => d.LineNumber1 > 0 && d.LineNumber2 > 0);
        Console.WriteLine($"\nModifications (both line numbers > 0): {modifications}");
        Console.WriteLine($"Expected: 3");
        Console.WriteLine($"Test result: {(modifications == 3 ? "PASS" : "FAIL")}");

        // Clean up
        File.Delete(file1);
        File.Delete(file2);
    }
}
