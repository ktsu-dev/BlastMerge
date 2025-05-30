using System;
using System.IO;
using System.Reflection;
using System.Linq;

// Load the DiffMore.Core assembly
var assemblyPath = @"DiffMore.Core\bin\Debug\net9.0\ktsu.DiffMore.Core.dll";
var assembly = Assembly.LoadFrom(assemblyPath);

// Get the DiffPlexDiffer type
var diffPlexDifferType = assembly.GetType("ktsu.DiffMore.Core.DiffPlexDiffer");

// Create temporary test files
var testDir = Path.Combine(Path.GetTempPath(), "DiffMoreTest");
Directory.CreateDirectory(testDir);

var file1 = Path.Combine(testDir, "file1.txt");
var file2 = Path.Combine(testDir, "file2.txt");

// Test the original DiffPlexDifferTests scenario
File.WriteAllText(file1, """
Line 1
Line 2
Line 3
Line 4
""");

File.WriteAllText(file2, """
Line 1
Modified Line 2
Line 3
New Line 4
Line 5
""");

try
{
    // Test FindDifferences
    var findDifferencesMethod = diffPlexDifferType.GetMethod("FindDifferences", new[] { typeof(string), typeof(string) });
    var differences = findDifferencesMethod.Invoke(null, new object[] { file1, file2 });

    // Use reflection to get the Count property
    var countProperty = differences.GetType().GetProperty("Count");
    var count = countProperty.GetValue(differences);

    Console.WriteLine($"DiffPlexDifferTests scenario:");
    Console.WriteLine($"Differences count: {count}");

    // Check for Added and Deleted types
    var enumerableType = typeof(System.Collections.IEnumerable);
    if (enumerableType.IsAssignableFrom(differences.GetType()))
    {
        var items = (System.Collections.IEnumerable)differences;
        int i = 0;
        bool hasAdded = false;
        bool hasDeleted = false;

        foreach (var item in items)
        {
            Console.WriteLine($"Difference {i}: {item}");

            // Get properties using reflection
            var type = item.GetType().GetProperty("Type");
            var content1 = item.GetType().GetProperty("Content1");
            var content2 = item.GetType().GetProperty("Content2");
            var lineNum1 = item.GetType().GetProperty("LineNumber1");
            var lineNum2 = item.GetType().GetProperty("LineNumber2");

            var typeValue = type?.GetValue(item)?.ToString();

            Console.WriteLine($"  Type: {typeValue}");
            Console.WriteLine($"  Content1: '{content1?.GetValue(item)}'");
            Console.WriteLine($"  Content2: '{content2?.GetValue(item)}'");
            Console.WriteLine($"  LineNumber1: {lineNum1?.GetValue(item)}");
            Console.WriteLine($"  LineNumber2: {lineNum2?.GetValue(item)}");

            if (typeValue == "Added") hasAdded = true;
            if (typeValue == "Deleted") hasDeleted = true;

            i++;
        }

        Console.WriteLine($"\nHas Added: {hasAdded}");
        Console.WriteLine($"Has Deleted: {hasDeleted}");
        Console.WriteLine($"Test expects both Added and Deleted: {hasAdded && hasDeleted}");
        Console.WriteLine($"Test result: {(hasAdded && hasDeleted ? "PASS" : "FAIL")}");
    }
}
finally
{
    // Cleanup
    if (Directory.Exists(testDir))
    {
        Directory.Delete(testDir, true);
    }
}
