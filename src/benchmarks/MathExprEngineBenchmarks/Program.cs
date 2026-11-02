using BenchmarkDotNet.Running;
using System;
using System.Linq;
using System.Reflection;

namespace MathExprEngineBenchmarks
{
    public static class Program
    {
        private const string EngineProjectPathKey = "EngineProjectPath";

        public static int Main(string[] args)
        {
            ExportEngineProjectPath();

            var summaries = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args).ToArray();
            var failed = summaries.Any(summary =>
                summary.HasCriticalValidationErrors || summary.Reports.Any(report => !report.Success));

            return failed ? 1 : 0;
        }

        private static void ExportEngineProjectPath()
        {
            var enginePath = typeof(Program).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(attribute => attribute.Key == EngineProjectPathKey)?.Value;

            if (!string.IsNullOrEmpty(enginePath))
                Environment.SetEnvironmentVariable(EngineProjectPathKey, enginePath);
        }
    }
}
