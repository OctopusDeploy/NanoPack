//////////////////////////////////////////////////////////////////////
// TOOLS
//////////////////////////////////////////////////////////////////////
#tool "nuget:?package=GitVersion.CommandLine&version=4.0.0-beta0011"
#addin "Cake.FileHelpers"

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

///////////////////////////////////////////////////////////////////////////////
// GLOBAL VARIABLES
///////////////////////////////////////////////////////////////////////////////
var artifactsDir = "./artifacts/";
GitVersion gitVersionInfo;
string nugetVersion;

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////
Setup(context =>
{
    gitVersionInfo = GitVersion(new GitVersionSettings {
        OutputType = GitVersionOutput.Json
    });

    if(BuildSystem.IsRunningOnTeamCity)
        BuildSystem.TeamCity.SetBuildNumber(gitVersionInfo.NuGetVersion);

    nugetVersion = gitVersionInfo.NuGetVersion;

    Information("Building NanoPack v{0}", nugetVersion);
    Information("Informational Version {0}", gitVersionInfo.InformationalVersion);
});

Teardown(context =>
{
    Information("Finished running tasks.");
});


//////////////////////////////////////////////////////////////////////
//  PRIVATE TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() => {
		CleanDirectory(artifactsDir);
		CleanDirectories("./source/**/bin");
		CleanDirectories("./source/**/obj");
		CleanDirectories("./source/**/TestResults");
	});

Task("Restore")
    .IsDependentOn("Clean")
    .Does(() => {
		DotNetCoreRestore(".");
    });


Task("Build")
    .IsDependentOn("Restore")
    .IsDependentOn("Clean")
    .Does(() => {
		 DotNetCoreBuild(".", new DotNetCoreBuildSettings
		{
			Configuration = configuration,
			ArgumentCustomization = args => args.Append($"/p:Version={nugetVersion}")
		});
	});

Task("Test")
    .IsDependentOn("Build")
    .Does(() => {
        DotNetCoreTest("./src/NanoPack.Tests/NanoPack.Tests.csproj", new DotNetCoreTestSettings
        {
            Configuration = configuration,
            NoBuild = true,
            ArgumentCustomization = args => args.Append("-l trx")
        });
	});

Task("Pack")
    .IsDependentOn("Build")
    .Does(() =>
{
    DotNetCorePack("./src/NanoPack/NanoPack.csproj", new DotNetCorePackSettings
    {
        Configuration = configuration,
        OutputDirectory = artifactsDir,
        NoBuild = true,
        ArgumentCustomization = args => args.Append($"/p:Version={nugetVersion}")
    });
});

Task("Publish")
    .IsDependentOn("Pack")
    .Does(() =>
{
    if (!BuildSystem.IsLocalBuild)
    {
        NuGetPush(artifactsDir + "NanoPack." + nugetVersion + ".nupkg", new NuGetPushSettings {
            Source = "https://octopus.myget.org/F/octopus-dependencies/api/v3/index.json",
            ApiKey = EnvironmentVariable("MyGetApiKey")
        });
    }
});

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Publish");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////
RunTarget(target);
