using System;
using System.IO;
using System.Linq;
using ktsu.DiffMore.Core;

class DebugTest
{
    static void Main()
    {
        // Test the specific case that was failing
        var file1 = "test1.txt";
        var file2 = "test2.txt";

        File.WriteAllLines(file1, new[] { "Line 1", "Line 2", "Line 3" });
        File.WriteAllLines(file2, new[] { "Different Line 1", "Different Line 2", "Different Line 3" });

        var differences = FileDiffer.FindDifferences(file1, file2);

        Console.WriteLine($"Total differences found: {differences.Count}");
        Console.WriteLine();

        foreach (var diff in differences)
        {
            Console.WriteLine($"Line1: {diff.LineNumber1}, Line2: {diff.LineNumber2}");
            Console.WriteLine($"Content1: '{diff.Content1}'");
            Console.WriteLine($"Content2: '{diff.Content2}'");
            Console.WriteLine("---");
        }

        // Count modifications (where both line numbers > 0)
        var modifications = differences.Count(d => d.LineNumber1 > 0 && d.LineNumber2 > 0);
        Console.WriteLine($"Modifications (both line numbers > 0): {modifications}");

        // Test the color diff as well
        var coloredDiff = FileDiffer.GenerateColoredDiff(file1, file2,
            File.ReadAllLines(file1),
            File.ReadAllLines(file2));

        Console.WriteLine("\nColored diff:");
        foreach (var line in coloredDiff)
        {
            Console.WriteLine($"{line.Color}: {line.Content}");
        }

        // Clean up
        File.Delete(file1);
        File.Delete(file2);
    }
}
