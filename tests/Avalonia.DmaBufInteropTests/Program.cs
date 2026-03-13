using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia.DmaBufInteropTests;
using Avalonia.DmaBufInteropTests.Tests;

// Parse args
var mode = "both";
bool verbose = false;
foreach (var arg in args)
{
    switch (arg)
    {
        case "--egl": mode = "egl"; break;
        case "--vulkan": mode = "vulkan"; break;
        case "--both": mode = "both"; break;
        case "--logic": mode = "logic"; break;
        case "-v" or "--verbose": verbose = true; break;
        case "-h" or "--help":
            PrintUsage();
            return 0;
    }
}

Console.WriteLine($"DMA-BUF Interop Tests — mode: {mode}");
Console.WriteLine(new string('=', 60));

var allResults = new List<TestResult>();
var sw = Stopwatch.StartNew();

// Pure logic tests (always run)
RunSuite("DRM Format Mapping", DrmFormatMappingTests.Run(), allResults);

if (mode is "egl" or "both")
    RunSuite("EGL DMA-BUF Import", EglDmaBufImportTests.Run(), allResults);

if (mode is "vulkan" or "both")
    RunSuite("Vulkan DMA-BUF Import", VulkanDmaBufImportTests.Run(), allResults);

sw.Stop();

// Summary
Console.WriteLine();
Console.WriteLine(new string('=', 60));
var passed = allResults.Count(r => r.Status == TestStatus.Passed);
var failed = allResults.Count(r => r.Status == TestStatus.Failed);
var skipped = allResults.Count(r => r.Status == TestStatus.Skipped);
Console.WriteLine($"Total: {allResults.Count} | Passed: {passed} | Failed: {failed} | Skipped: {skipped} | Time: {sw.ElapsedMilliseconds}ms");

if (failed > 0)
{
    Console.WriteLine();
    Console.WriteLine("FAILURES:");
    foreach (var f in allResults.Where(r => r.Status == TestStatus.Failed))
        Console.WriteLine($"  {f}");
}

return failed > 0 ? 1 : 0;

void RunSuite(string name, IEnumerable<TestResult> results, List<TestResult> accumulator)
{
    Console.WriteLine();
    Console.WriteLine($"--- {name} ---");
    foreach (var result in results)
    {
        accumulator.Add(result);
        if (verbose || result.Status != TestStatus.Passed)
            Console.WriteLine($"  {result}");
        else
            Console.Write(".");
    }
    if (!verbose)
        Console.WriteLine();
}

void PrintUsage()
{
    Console.WriteLine("Usage: Avalonia.DmaBufInteropTests [options]");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  --egl       Run EGL tests only");
    Console.WriteLine("  --vulkan    Run Vulkan tests only");
    Console.WriteLine("  --both      Run both EGL and Vulkan tests (default)");
    Console.WriteLine("  --logic     Run only pure logic tests (no GPU)");
    Console.WriteLine("  -v          Verbose output (show passing tests)");
    Console.WriteLine("  -h          Show this help");
}
