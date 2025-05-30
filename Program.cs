using System;
using System.IO;
using ktsu.BlastMerge.Core;

class Program
{
    static void Main()
    {
        // Create test files
        var file1 = "test1.txt";
        var file2 = "test2.txt";

        File.WriteAllLines(file1, new[] { "Line 1", "Line 2", "Line 3" });
        File.WriteAllLines(file2, new[] { "Different Line 1", "Different Line 2", "Different Line 3" });

        // Test the diff
        var differences = FileDiffer.FindDifferences(file1, file2);

        Console.WriteLine($"Found {differences.Count} differences:");
        foreach (var diff in differences)
        {
            Console.WriteLine($"Line1: {diff.LineNumber1}, Line2: {diff.LineNumber2}");
            Console.WriteLine($"Content1: '{diff.Content1}'");
            Console.WriteLine($"Content2: '{diff.Content2}'");
            Console.WriteLine("---");
        }

        // Clean up
        File.Delete(file1);
        File.Delete(file2);
    }
}
