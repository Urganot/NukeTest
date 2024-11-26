using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Nuke.Common;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.Docker;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.SonarScanner;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.SonarScanner.SonarScannerTasks;

class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode
    public static int Main() => Execute<Build>(x => x.SonarStart);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild
        ? Configuration.Debug
        : Configuration.Release;

    [Solution(GenerateProjects = true, SuppressBuildProjectCheck = true)]
    Solution Solution;

    private bool SonarStartRanSuccessfully = false;

    Target Clean =>
        _ =>
            _.DependentFor(Restore)
                .DependentFor(Lint)
                .Executes(() =>
                {
                    DotNetClean(settings => settings.SetProject(Solution));
                });

    Target Restore =>
        _ =>
            _.DependsOn(Clean)
                .Executes(() =>
                {
                    DotNetRestore(settings => settings.SetProjectFile(Solution).SetForce(true));
                });

    Target ToolRestore =>
        _ =>
            _.Executes(() =>
            {
                DotNetToolRestore(settings => settings); // New settings necessary until bugfix is released
            });

    Target Lint =>
        _ =>
            _.DependentFor(Compile)
                .DependsOn(ToolRestore)
                .Executes(() =>
                {
                    DotNet($"dotnet-csharpier --check {RootDirectory}");
                });

    Target Compile =>
        _ =>
            _.DependsOn(Restore)
                .Executes(() =>
                {
                    Console.WriteLine("Compile");

                    DotNetBuild(settings =>
                        settings
                            .SetProjectFile(Solution)
                            .SetConfiguration(Configuration)
                            .EnableNoRestore()
                    );
                });

    Target SonarStart =>
        _ =>
            _.DependsOn(ToolRestore)
                .DependentFor(Compile)
                .ProceedAfterFailure()
                .Triggers(SonarEnd)
                .Executes(() =>
                {
                    Console.WriteLine(Environment.CurrentDirectory);

                    var configuration = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddUserSecrets<Build>()
                        .Build();

                    SonarScannerBegin(settings =>
                        settings
                            .SetProjectKey(configuration["SonarQube:ProjectKey"])
                            .SetServer(configuration["SonarQube:Server"])
                    );
                    SonarStartRanSuccessfully = true;
                });

    Target SonarEnd =>
        _ =>
            _.DependsOn(ToolRestore)
                .After(Compile)
                .OnlyWhenStatic(() => SonarStartRanSuccessfully)
                .Executes(() =>
                {
                    SonarScannerEnd();
                });

    Target UnitTests =>
        _ =>
            _.DependsOn(Compile)
                .Executes(() =>
                {
                    DotNetTest(settings =>
                        settings
                            .SetProjectFile(Solution)
                            .SetFilter("Category=UnitTests")
                            .EnableNoRestore()
                            .EnableNoBuild()
                    );
                });

    Target ArchitectureTests =>
        _ =>
            _.DependsOn(Compile)
                .Executes(() =>
                {
                    DotNetTest(settings =>
                        settings
                            .SetProjectFile(Solution)
                            .SetFilter("Category=ArchitectureTests")
                            .EnableNoRestore()
                            .EnableNoBuild()
                    );
                });

    Target IntegrationTests =>
        _ =>
            _.DependsOn(Compile)
                .Executes(() =>
                {
                    DotNetTest(settings =>
                        settings
                            .SetProjectFile(Solution)
                            .SetFilter("Category=IntegrationTests")
                            .EnableNoRestore()
                            .EnableNoBuild()
                    );
                });

    Target Sonar => _ => _.Triggers(SonarStart).Triggers(SonarEnd).Executes(() => { });

    Target Test =>
        _ =>
            _.Triggers(ArchitectureTests)
                .Triggers(IntegrationTests)
                .Triggers(UnitTests)
                .Executes(() => { });

    Target TestAndCompile => _ => _.Triggers(Compile).Triggers(Test).Executes(() => { });

    Target GenerateCode =>
        _ =>
            _.Executes(() =>
            {
                DockerTasks.DockerRun(settings =>
                    settings
                        .AddVolume($"{RootDirectory}:/local")
                        .SetImage("openapitools/openapi-generator-cli")
                        .AddArgs(
                            "batch",
                            "/local/CodeGeneration/code-gen-configuration/aspnetcore.yml"
                        )
                );
            });
}
