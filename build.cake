//////////////////////////////////////////////////////////////////////
// TOOLS
//////////////////////////////////////////////////////////////////////
#tool "nuget:?package=GitVersion.CommandLine&version=4.0.0-beta0007"
#addin "nuget:?package=Newtonsoft.Json&version=9.0.1"
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var forceCiBuild = Argument("forceCiBuild", false);

///////////////////////////////////////////////////////////////////////////////
// GLOBAL VARIABLES
///////////////////////////////////////////////////////////////////////////////
var artifactsDir = "./artifacts/";
var projectToPackage = "./src/NanoPack";
string originalProjectJsonVersion = null;
var isContinuousIntegrationBuild = !BuildSystem.IsLocalBuild || forceCiBuild;

var gitVersionInfo = GitVersion(new GitVersionSettings {
    OutputType = GitVersionOutput.Json
});

if(BuildSystem.IsRunningOnTeamCity)
    BuildSystem.TeamCity.SetBuildNumber(gitVersionInfo.NuGetVersion);

if(BuildSystem.IsRunningOnAppVeyor)
    BuildSystem.AppVeyor.UpdateBuildVersion(gitVersionInfo.NuGetVersion);

var nugetVersion = gitVersionInfo.NuGetVersion;

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////
Setup(context =>
{
    Information("Building NanoPack v{0}", nugetVersion);
});

Teardown(context =>
{
    Information("Finished running tasks.");
});

//////////////////////////////////////////////////////////////////////
//  PRIVATE TASKS
//////////////////////////////////////////////////////////////////////

Task("__Default")
    .IsDependentOn("__Clean")
    .IsDependentOn("__Restore")
    .IsDependentOn("__UpdateProjectJsonVersion")
    .IsDependentOn("__Build")
    .IsDependentOn("__Pack")
    .IsDependentOn("__ResetProjectJsonVersion")  
    .IsDependentOn("__Publish");

Task("__Clean")
    .Does(() =>
{
    CleanDirectory(artifactsDir);
    CleanDirectories("./src/**/bin");
    CleanDirectories("./src/**/obj");
});

Task("__Restore")
    .Does(() => DotNetCoreRestore());

Task("__Build")
    .Does(() =>
{
    DotNetCoreBuild("**/project.json", new DotNetCoreBuildSettings
    {
        Configuration = configuration
    });
});

Task("__UpdateProjectJsonVersion")
    .Does(() =>
{
    var projectToPackagePackageJson = $"{projectToPackage}/project.json";
    var json = JsonConvert.DeserializeObject<JObject>(System.IO.File.ReadAllText(projectToPackagePackageJson));
    originalProjectJsonVersion = (string)json["version"];
    Information("Updating {0} version -> {1}", projectToPackagePackageJson, nugetVersion);
    json["version"] = nugetVersion;
    System.IO.File.WriteAllText(projectToPackagePackageJson, JsonConvert.SerializeObject(json, Formatting.Indented));
});

Task("__ResetProjectJsonVersion")
    .Does(() =>
    {
        if(originalProjectJsonVersion == null) return;

        var projectToPackagePackageJson = $"{projectToPackage}/project.json";
        var json = JsonConvert.DeserializeObject<JObject>(System.IO.File.ReadAllText(projectToPackagePackageJson));
        json["version"] = originalProjectJsonVersion;
        System.IO.File.WriteAllText(projectToPackagePackageJson, JsonConvert.SerializeObject(json, Formatting.Indented));
    });

Task("__Pack")
    .Does(() =>
{
    DotNetCorePack(projectToPackage, new DotNetCorePackSettings
    {
        Configuration = configuration,
        OutputDirectory = artifactsDir,
        NoBuild = true
    });

    DeleteFiles(artifactsDir + "*symbols*");
});

Task("__Publish")
    .WithCriteria(isContinuousIntegrationBuild && !forceCiBuild)
    .Does(() =>
{
    var isPullRequest = !String.IsNullOrEmpty(EnvironmentVariable("APPVEYOR_PULL_REQUEST_NUMBER"));
    var isMasterBranch = EnvironmentVariable("APPVEYOR_REPO_BRANCH") == "master" && !isPullRequest;
    var shouldPushToMyGet = !BuildSystem.IsLocalBuild;
    var shouldPushToNuGet = !BuildSystem.IsLocalBuild && isMasterBranch;

    if (shouldPushToMyGet)
    {
        NuGetPush(artifactsDir + "NanoPack." + nugetVersion + ".nupkg", new NuGetPushSettings {
            Source = "https://octopus.myget.org/F/octopus-dependencies/api/v3/index.json",
            ApiKey = EnvironmentVariable("MyGetApiKey")
        });
    }

    // if (shouldPushToNuGet)
    // {
    //     NuGetPush(artifactsDir + "NanoPack." + nugetVersion + ".nupkg", new NuGetPushSettings {
    //         Source = "https://www.nuget.org/api/v2/package",
    //         ApiKey = EnvironmentVariable("NuGetApiKey")
    //     });
    //     NuGetPush(artifactsDir + "NanoPack." + nugetVersion + ".symbols.nupkg", new NuGetPushSettings {
    //         Source = "https://www.nuget.org/api/v2/package",
    //         ApiKey = EnvironmentVariable("NuGetApiKey")
    //     });
    // }
});

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////
Task("Default")
    .IsDependentOn("__Default");

Task("Clean")
    .IsDependentOn("__Clean");

Task("Restore")
    .IsDependentOn("__Restore");

Task("Build")
    .IsDependentOn("__Build");

Task("Pack")
    .IsDependentOn("__Clean")
    .IsDependentOn("__Restore")
    .IsDependentOn("__UpdateProjectJsonVersion")
    .IsDependentOn("__Build")
    .IsDependentOn("__Pack")
    .IsDependentOn("__ResetProjectJsonVersion");

Task("Publish")
    .IsDependentOn("Pack")
    .IsDependentOn("__Publish");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////
RunTarget(target);
