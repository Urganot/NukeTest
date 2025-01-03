namespace NukeTest.ArchitectureTests;

using ArchUnitNET.Loader;
using ArchUnitNET.Domain;
using Assembly = System.Reflection.Assembly;

// Fixture class
public class AssemblyFixture
{
    public AssemblyFixture()
    {
        var archLoader = new ArchLoader();

        var assemblyDir = GetAssembliesBaseDirs().First();

        Console.WriteLine("Loading assemblies...");

        archLoader.LoadFilteredDirectory(assemblyDir, "NukeTest*.dll", SearchOption.AllDirectories);

        ArchitectureTestBase.NukeTestArchitecture = archLoader.Build();
    }

    private static List<string> GetAssembliesBaseDirs()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory.Substring(
            0,
            AppDomain.CurrentDomain.BaseDirectory.IndexOf(@"NukeTest.ArchitectureTests",
                StringComparison.InvariantCulture)
        );
        var path = Path.Combine(baseDir, "NukeTest", "bin", "Debug");

        return new List<string> { path };
    }
}

// Test class using the collection fixture
[CollectionDefinition("Assembly Collection")]
public class AssemblyCollection : ICollectionFixture<AssemblyFixture>;

[Collection("Assembly Collection")]
public class ArchitectureTestBase
{
    public static Architecture NukeTestArchitecture { get; set; }
}