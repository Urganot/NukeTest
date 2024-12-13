using ArchUnitNET.Loader;

namespace NukeTest.ArchitectureTests
{
    using ArchUnitNET.Domain;
    using Assembly = System.Reflection.Assembly;

    public class ArchitectureTestBase
    {
        protected static Architecture NukeTestArchitecture { get; private set; }

        static ArchitectureTestBase()
        {
            var archLoader = new ArchLoader();

            foreach (var assembliesBaseDir in GetAssembliesBaseDirs())
            {
                if (!Directory.Exists(assembliesBaseDir))
                    throw new Exception($"Given directory {assembliesBaseDir} does not exist! ");

                var NukeTestLibraries = Directory.GetFiles(
                    assembliesBaseDir,
                    "NukeTest*.dll",
                    SearchOption.AllDirectories
                );

                var libraryAssemblies = NukeTestLibraries
                    .Select(Assembly.LoadFile)
                    .Where(IsNoResourcesAssembly())
                    .DistinctBy(assembly => assembly.FullName)
                    .ToArray();

                archLoader
                    .LoadFilteredDirectory(
                        assembliesBaseDir,
                        "NukeTest*.exe",
                        SearchOption.AllDirectories
                    )
                    .LoadAssemblies(libraryAssemblies);
            }

            NukeTestArchitecture = archLoader.Build();
        }

        private static Func<Assembly, bool> IsNoResourcesAssembly()
        {
            return assembly => !assembly.GetName().Name!.EndsWith(".resources");
        }

        private static List<string> GetAssembliesBaseDirs()
        {
#if DEBUG

            var baseDir = AppDomain.CurrentDomain.BaseDirectory.Substring(
                0,
                AppDomain.CurrentDomain.BaseDirectory.IndexOf(@"NukeTest.ArchitectureTests")
            );
            var path = Path.Combine(baseDir, "NukeTest", "bin", "Debug");

            return new List<string> { path };
#else
            var pipelineType = Environment.GetEnvironmentVariable("PipelineType");
            var isLocalReleaseBuild = string.IsNullOrEmpty(pipelineType);

            if (isLocalReleaseBuild)
            {
                return new List<string>
                {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "bin"),
                    Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "..",
                        "..",
                        "NukeTest",
                        "bin",
                        "Release"
                    ),
                };
            }

            switch (pipelineType)
            {
                case "BranchAndPullRequestPipeline":
                    return new List<string>
                    {
                        Path.Combine(
                            AppDomain.CurrentDomain.BaseDirectory,
                            "..",
                            "..",
                            "bin",
                            "Release_temp"
                        ),
                        Path.Combine(
                            AppDomain.CurrentDomain.BaseDirectory,
                            "..",
                            "..",
                            "NukeTest",
                            "bin",
                            "Release"
                        ),
                    };
                case "NightlyPipeline":
                {
                    const string AssembliesDirEnvVariableName = "ArchUnitAssembliesDir";
                    var assembliesDir = Environment.GetEnvironmentVariable(
                        AssembliesDirEnvVariableName
                    );
                    var envVariableIsMissing = string.IsNullOrEmpty(assembliesDir);

                    return envVariableIsMissing
                        ? throw new Exception(
                            $"Env variable '{AssembliesDirEnvVariableName}' must be set for "
                                + $"pipeline type '{pipelineType}'"
                        )
                        : new List<string> { assembliesDir };
                }
                default:
                    throw new Exception($"'{pipelineType}' is an unknown pipeline type");
            }
#endif
        }
    }
}
