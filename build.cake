var target = Argument("target", "Default");

var solutionFile = "./Pose.sln";
var nuspecFileName = "Poser";

Task("Build")
  .Does(() =>
{
  var buildSettings = new DotNetCoreBuildSettings
  {
    Configuration = "Release",
    Verbosity = DotNetCoreVerbosity.Minimal
  };
  
  DotNetCoreBuild(solutionFile, buildSettings);

});

Task("Test")
  .Does(() =>
{
  var settings = new DotNetCoreTestSettings
  {
    Verbosity = DotNetCoreVerbosity.Minimal
  };

  DotNetCoreTest(solutionFile, settings);
})
;

Task("Default")
  .IsDependentOn("Build")
  .IsDependentOn("Test")
;

Task("Pack")
  //.IsDependentOn("Build")
  //.IsDependentOn("Test")
  .Does(() =>
{
  Pack("Pose", new [] { "netstandard2.0" });
})
;

RunTarget(target);

public void Pack(string projectName, string[] targets) 
{
  var buildSettings = new DotNetCoreMSBuildSettings()
    .WithProperty("NuspecFile", $"../../nuget/{nuspecFileName}.nuspec")
    .WithProperty("NuspecBasePath", "bin/Release");
  var settings = new DotNetCorePackSettings
  {
    MSBuildSettings = buildSettings,
    Verbosity = DotNetCoreVerbosity.Minimal,
    Configuration = "Release",
    IncludeSource = true,
    IncludeSymbols = true,
    OutputDirectory = "./nuget"
  };
  
  DotNetCorePack($"./src/{projectName}/{projectName}.csproj", settings);
}
