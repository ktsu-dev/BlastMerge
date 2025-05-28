using System;
using System.IO;
using System.Linq;
using System.Reflection;

class StandaloneTest
{
    static void Main()
    {
        try
        {
            // Load the DiffMore.Core assembly
            var coreAssembly = Assembly.LoadFrom(@"DiffMore.Core\bin\Debug\net9.0\ktsu.DiffMore.Core.dll");
            var fileDifferType = coreAssembly.GetType("ktsu.DiffMore.Core.FileDiffer");
            var findDifferencesMethod = fileDifferType.GetMethod("FindDifferences", new[] { typeof(string), typeof(string) });

            Console.WriteLine("=== Testing Deleted Line Scenario ===");

            // Test case: Deleted line scenario (from failing test)
            Console.WriteLine("\n1. Testing deleted line scenario:");
            var file1 = "test1.txt";
            var file2 = "test2.txt";

            File.WriteAllLines(file1, new[] { "Line 1", "Line 2", "Line 3", "Line 4", "Line 5" });
            File.WriteAllLines(file2, new[] { "Line 1", "Line 3", "Line 4", "Line 5", "Line 6 added" });

            var differences = findDifferencesMethod.Invoke(null, new object[] { file1, file2 });
            var differencesList = differences as System.Collections.ICollection;

            Console.WriteLine($"Total differences: {differencesList.Count}");

            // Get the LineDifference type to inspect properties
            var lineDiffType = coreAssembly.GetType("ktsu.DiffMore.Core.LineDifference");
            var lineNumber1Prop = lineDiffType.GetProperty("LineNumber1");
            var lineNumber2Prop = lineDiffType.GetProperty("LineNumber2");
            var content1Prop = lineDiffType.GetProperty("Content1");
            var content2Prop = lineDiffType.GetProperty("Content2");

            bool foundDeletedLine = false;
            bool foundAddedLine = false;
            int index = 0;

            foreach (var diff in differencesList)
            {
                var line1 = (int)lineNumber1Prop.GetValue(diff);
                var line2 = (int)lineNumber2Prop.GetValue(diff);
                var content1 = (string)content1Prop.GetValue(diff);
                var content2 = (string)content2Prop.GetValue(diff);

                Console.WriteLine($"  [{index}] Line1: {line1}, Line2: {line2}");
                Console.WriteLine($"      Content1: '{content1}'");
                Console.WriteLine($"      Content2: '{content2}'");

                // Check for expected deleted line (Line1=2, Line2=0)
                if (line1 == 2 && line2 == 0)
                {
                    foundDeletedLine = true;
                }

                // Check for expected added line ('Line 6 added')
                if (content2 == "Line 6 added")
                {
                    foundAddedLine = true;
                }

                index++;
            }

            Console.WriteLine($"\nExpected deleted line (Line1=2, Line2=0): {(foundDeletedLine ? "FOUND" : "NOT FOUND")}");
            Console.WriteLine($"Expected added line ('Line 6 added'): {(foundAddedLine ? "FOUND" : "NOT FOUND")}");
            Console.WriteLine($"Test expects at least 2 differences, found: {differencesList.Count}, Result: {(differencesList.Count >= 2 ? "PASS" : "FAIL")}");

            // Clean up
            File.Delete(file1);
            File.Delete(file2);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}
