using System;
using System.IO;
using System.Linq;
using ktsu.DiffMore.Core;

class DebugTest
{
    static void Main()
    {
        Console.WriteLine("=== Debugging DiffMore Algorithm ===");

        // Test case: All lines different
        Console.WriteLine("\n1. Testing all lines different:");
        var file1 = "test1.txt";
        var file2 = "test2.txt";

        File.WriteAllLines(file1, new[] { "Line 1", "Line 2", "Line 3" });
        File.WriteAllLines(file2, new[] { "Different Line 1", "Different Line 2", "Different Line 3" });

        var differences = FileDiffer.FindDifferences(file1, file2);

        Console.WriteLine($"Total differences: {differences.Count}");
        for (int i = 0; i < differences.Count; i++)
        {
            var diff = differences.ElementAt(i);
            Console.WriteLine($"  [{i}] Line1: {diff.LineNumber1}, Line2: {diff.LineNumber2}");
            Console.WriteLine($"      Content1: '{diff.Content1}'");
            Console.WriteLine($"      Content2: '{diff.Content2}'");
        }

        var modifications = differences.Count(d => d.LineNumber1 > 0 && d.LineNumber2 > 0);
        Console.WriteLine($"Modifications (both line numbers > 0): {modifications}");
        Console.WriteLine($"Expected: 3, Actual: {modifications}, Result: {(modifications == 3 ? "PASS" : "FAIL")}");

        // Test case: One line deleted, one added
        Console.WriteLine("\n2. Testing one deletion, one addition:");
        File.WriteAllLines(file1, new[] { "Line 1", "Line 2", "Line 3" });
        File.WriteAllLines(file2, new[] { "Line 1", "Line 3", "New Line" });

        differences = FileDiffer.FindDifferences(file1, file2);

        Console.WriteLine($"Total differences: {differences.Count}");
        for (int i = 0; i < differences.Count; i++)
        {
            var diff = differences.ElementAt(i);
            Console.WriteLine($"  [{i}] Line1: {diff.LineNumber1}, Line2: {diff.LineNumber2}");
            Console.WriteLine($"      Content1: '{diff.Content1}'");
            Console.WriteLine($"      Content2: '{diff.Content2}'");
        }

        Console.WriteLine($"Expected at least 2 differences, Actual: {differences.Count}, Result: {(differences.Count >= 2 ? "PASS" : "FAIL")}");

        // Clean up
        File.Delete(file1);
        File.Delete(file2);
    }
}
