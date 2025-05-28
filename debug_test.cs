using System;
using System.IO;
using System.Linq;
using ktsu.DiffMore.Core;

class DebugTest
{
    static void Main()
    {
        Console.WriteLine("=== Debugging DiffMore Algorithm ===");

        // Test case: Deleted line scenario (from failing test)
        Console.WriteLine("\n1. Testing deleted line scenario:");
        var file1 = "test1.txt";
        var file2 = "test2.txt";

        File.WriteAllLines(file1, new[] { "Line 1", "Line 2", "Line 3", "Line 4", "Line 5" });
        File.WriteAllLines(file2, new[] { "Line 1", "Line 3", "Line 4", "Line 5", "Line 6 added" });

        var differences = FileDiffer.FindDifferences(file1, file2);

        Console.WriteLine($"Total differences: {differences.Count}");
        for (int i = 0; i < differences.Count; i++)
        {
            var diff = differences.ElementAt(i);
            Console.WriteLine($"  [{i}] Line1: {diff.LineNumber1}, Line2: {diff.LineNumber2}");
            Console.WriteLine($"      Content1: '{diff.Content1}'");
            Console.WriteLine($"      Content2: '{diff.Content2}'");
        }

        // Check for specific expectations from the test
        var deletedLine = differences.FirstOrDefault(d => d.LineNumber1 == 2 && d.LineNumber2 == 0);
        var addedLine = differences.FirstOrDefault(d => d.Content2 == "Line 6 added");

        Console.WriteLine($"\nExpected deleted line (Line1=2, Line2=0): {(deletedLine != null ? "FOUND" : "NOT FOUND")}");
        if (deletedLine != null)
        {
            Console.WriteLine($"  Content1: '{deletedLine.Content1}', Content2: '{deletedLine.Content2}'");
        }

        Console.WriteLine($"Expected added line ('Line 6 added'): {(addedLine != null ? "FOUND" : "NOT FOUND")}");
        if (addedLine != null)
        {
            Console.WriteLine($"  Line1: {addedLine.LineNumber1}, Line2: {addedLine.LineNumber2}");
        }

        Console.WriteLine($"Test expects at least 2 differences, found: {differences.Count}, Result: {(differences.Count >= 2 ? "PASS" : "FAIL")}");

        // Clean up
        File.Delete(file1);
        File.Delete(file2);
    }
}
