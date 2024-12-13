//add a using directive to ArchUnitNET.Fluent.ArchRuleDefinition to easily define ArchRules
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace NukeTest.ArchitectureTests
{
    using System.Linq;
    using System.Text.RegularExpressions;
    using ArchUnitNET.Domain.Extensions;
    using ArchUnitNET.xUnit;
    using NukeTest.ArchitectureTests;
    using Xunit;

    public class ModuleIsolationTests : ArchitectureTestBase
    {
        [Fact]
        public void ModulesShouldBeCalledByAnotherModule()
        {
            var regex = new Regex(@"^(NukeTest\.Modules\.[^\.\s]+)");

            foreach (
                var archNamespace in NukeTestArchitecture.Namespaces.Where(n =>
                    n.NameStartsWith("NukeTest.Modules.")
                )
            )
            {
                if (archNamespace == null)
                    continue;

                // This extracts the modules root namespace
                var moduleNamespace = regex.Match(archNamespace.FullName).Value;
                if (string.IsNullOrWhiteSpace(moduleNamespace))
                    continue;

                var classes = Classes().That().ResideInNamespace(moduleNamespace);
                var methods = MethodMembers().That().AreDeclaredIn(classes);
                var rule = methods
                    .Should()
                    .NotBeCalledBy(Classes().That().DoNotResideInNamespace(moduleNamespace))
                    .WithoutRequiringPositiveResults();
                rule.Check(NukeTestArchitecture);
            }
        }
    }
}