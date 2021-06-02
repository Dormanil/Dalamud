using System.Collections.Generic;
using System.IO;
using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.MSBuild;

[CheckBuildProjectConfigurations]
[UnsetVisualStudioEnvironmentVariables]
public class DalamudBuild : NukeBuild
{
    /// Support plugins are available for:
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main() => Execute<DalamudBuild>(x => x.Publish);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution] Solution Solution;
    [GitRepository] GitRepository GitRepository;

    AbsolutePath DalamudProjectDir => RootDirectory / "Dalamud";
    AbsolutePath DalamudProjectFile => DalamudProjectDir / "Dalamud.csproj";
    AbsolutePath DalamudProjectOutDir => DalamudProjectDir / "bin" / Configuration;

    AbsolutePath BootProjectDir => RootDirectory / "Dalamud.Boot";
    AbsolutePath BootProjectFile => BootProjectDir / "Dalamud.Boot.vcxproj";
    AbsolutePath BootProjectOutDir => BootProjectDir / "bin" / Configuration;

    AbsolutePath InjectorProjectDir => RootDirectory / "Dalamud.Injector";
    AbsolutePath InjectorProjectFile => InjectorProjectDir / "Dalamud.Injector.csproj";
    AbsolutePath InjectorProjectOutDir => InjectorProjectDir / "bin" / Configuration;

    AbsolutePath TestProjectDir => RootDirectory / "Dalamud.Test";
    AbsolutePath TestProjectFile => TestProjectDir / "Dalamud.Test.csproj";

    AbsolutePath ArtifactsDirectory => RootDirectory / "bin" / Configuration;

    private static AbsolutePath LibraryDirectory => RootDirectory / "lib";

    private static Dictionary<string, string> EnvironmentVariables => new(EnvironmentInfo.Variables);

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetTasks.DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target CompileDalamud => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetTasks.DotNetBuild(s => s
                .SetProjectFile(DalamudProjectFile)
                .SetConfiguration(Configuration)
                .EnableNoRestore());
        });

    Target CompileBoot => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            MSBuildTasks.MSBuild(s => s
                .SetTargetPath(BootProjectFile)
                .SetConfiguration(Configuration));
        });

    Target CompileInjector => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetTasks.DotNetBuild(s => s
                .SetProjectFile(InjectorProjectFile)
                .SetConfiguration(Configuration)
                .EnableNoRestore());
        });

    Target Compile => _ => _
        .DependsOn(CompileDalamud)
        .DependsOn(CompileBoot)
        .DependsOn(CompileInjector);

    Target PublishDalamud => _ => _
        .DependsOn(CompileDalamud)
        .Executes(() =>
        {
            FileSystemTasks.CopyDirectoryRecursively(DalamudProjectOutDir, ArtifactsDirectory, DirectoryExistsPolicy.Merge, FileExistsPolicy.Overwrite);
        });

    Target PublishBoot => _ => _
        .DependsOn(CompileBoot)
        .Executes(() =>
        {
            FileSystemTasks.CopyDirectoryRecursively(BootProjectOutDir, ArtifactsDirectory, DirectoryExistsPolicy.Merge, FileExistsPolicy.Overwrite);
        });

    Target PublishInjector => _ => _
        .DependsOn(CompileInjector)
        .Executes(() =>
        {
            DotNetTasks.DotNetPublish(s => s
                .SetProject(InjectorProjectFile)
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .EnableSelfContained()
                .EnablePublishSingleFile()
                .EnablePublishTrimmed()
                .SetOutput(ArtifactsDirectory));
        });

    Target Publish => _ => _
        .DependsOn(Compile)
        .DependsOn(Test)
        .DependsOn(PublishDalamud)
        .DependsOn(PublishBoot)
        .DependsOn(PublishInjector);

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTasks.DotNetTest(s => s
                .SetProjectFile(TestProjectFile)
                .SetConfiguration(Configuration)
                .EnableNoRestore());
        });

    Target Clean => _ => _
        .Executes(() =>
        {
            DotNetTasks.DotNetClean(s => s
                .SetProject(DalamudProjectFile)
                .SetConfiguration(Configuration));

            MSBuildTasks.MSBuild(s => s
                .SetProjectFile(BootProjectFile)
                .SetConfiguration(Configuration)
                .SetTargets("Clean"));

            DotNetTasks.DotNetClean(s => s
                .SetProject(InjectorProjectFile)
                .SetConfiguration(Configuration));

            FileSystemTasks.DeleteDirectory(ArtifactsDirectory);
            Directory.CreateDirectory(ArtifactsDirectory);
        });
}
