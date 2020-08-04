#addin nuget:?package=Newtonsoft.Json&version=12.0.3

using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Threading.Tasks;

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var serveDocs = Argument<bool>("serveDocs", false);

var isAppveyor = EnvironmentVariable<bool>("APPVEYOR", false);
var projectRoot = Directory(EnvironmentVariable<string>("APPVEYOR_BUILD_FOLDER", ".."));
var isReleasePublication = isAppveyor && EnvironmentVariable("APPVEYOR_REPO_BRANCH") == "master" && EnvironmentVariable<bool>("APPVEYOR_REPO_TAG", false);
var canPublishDocs = isAppveyor && EnvironmentVariable<string>("APPVEYOR_REPO_NAME", "none").StartsWith("fireflycons/");
var docFxConfig = projectRoot + File("docfx/docfx.json");
var testResultsDir = Directory(EnvironmentVariable<string>("APPVEYOR_BUILD_FOLDER", "..")) + Directory("test-output");

FilePath mainProjectFile;
FilePath nugetPackagePath;
Version buildVersion;
DirectoryPath docFxSite;

var solutionFile = File("../Firefly.PSCloudFormation.sln");

Task("Init")
    .Does(() => {

        buildVersion = GetBuildVersion();

        foreach(var projectFile in GetFiles(projectRoot + File("**/*.csproj")))
        {
            var project = XElement.Load(projectFile.ToString());

            var packElem = project.Elements("PropertyGroup").Descendants("CopyLocalLockFileAssemblies").FirstOrDefault();

            if (packElem != null)
            {
                mainProjectFile = MakeAbsolute(File(projectFile.ToString()));
            }
        }

        if (mainProjectFile == null)
        {
            throw new CakeException("Unable to locate main project file (i.e. the one that will create the nuget package)");
        }
    });

Task("SetAssemblyProperties")
    .WithCriteria(IsRunningOnWindows())
    .Does(() => {

        // Only manipulate the nuget/assembly properties on Windows as it's this run that publishes to nuget.
        // Other platform builds to run tests only
        var project = XElement.Load(mainProjectFile.ToString());

        var propertyGroups = project.Elements("PropertyGroup").ToList();

        Information($"Package version: {buildVersion}");
        Information($"Assembly version: {buildVersion.ToString(2)}.0.0");
        Information($"File version: {buildVersion.ToString(3)}.0");

        SetProjectProperty(propertyGroups, "Version", buildVersion.ToString());
        SetProjectProperty(propertyGroups, "AssemblyVersion", $"{buildVersion.ToString(2)}.0.0");
        SetProjectProperty(propertyGroups, "FileVersion", $"{buildVersion.ToString(3)}.0");

        project.Save(mainProjectFile.ToString());
    });

Task("BuildProject")
    .Does(() => {

        DotNetCoreBuild(solutionFile, new DotNetCoreBuildSettings
        {
            Configuration = configuration
        });
    });

Task("TestProject")
    .WithCriteria(IsRunningOnWindows())
    .Does(() => {

        try
        {
            DotNetCoreTest(solutionFile, new DotNetCoreTestSettings
            {
                Configuration = configuration,
                NoBuild = true,
                Logger = "trx",
                ResultsDirectory = testResultsDir
            });
        }
        finally
        {
            UploadTestResults();
        }
    });

Task("Build")
    .IsDependentOn("Init")
    .IsDependentOn("SetAssemblyProperties")
    .IsDependentOn("BuildProject");

Task("Test")
    .IsDependentOn("Init")
    .IsDependentOn("TestProject");

Task("Default")
    .IsDependentOn("Build")
    .IsDependentOn("Test");

RunTarget(target);

#region Helper Functions

class AppveyorArtifactRequest
{
    public AppveyorArtifactRequest(FilePath artifact)
    {
        this.Path = artifact.ToString();
        this.FileName = artifact.GetFilename().ToString();
    }

    [JsonProperty("path")]
    public string Path { get; }

    [JsonProperty("fileName")]
    public string FileName { get; }

    [JsonProperty("name")]
    public string Name { get; } = null;

    [JsonProperty("type")]
    public string Type { get; } = "NuGetPackage";
}

class AppveyorArtifactResponse
{
    [JsonProperty("uploadUrl")]
    public string UploadUrl { get; set; }

    [JsonProperty("storageType")]
    public string StorageType { get; set; }

    [JsonIgnore]
    public bool IsAzureOrGoogle
    {
        get
        {
            return this.StorageType == "Azure" || this.UploadUrl.Contains("google");
        }
    }
}

class AppVeyorArtifactFinalisation
{
    public AppVeyorArtifactFinalisation(FilePath artifact)
    {
        this.FileName = artifact.GetFilename().ToString();
        this.Size = new System.IO.FileInfo(artifact.ToString()).Length;
    }

    [JsonProperty("fileName")]
    public string FileName { get; }

    [JsonProperty("size")]
    public long Size { get; }
}

async Task UploadAppveyorArtifact(FilePath artifact)
{
    if (!FileExists(artifact))
    {
        throw new FileNotFoundException(artifact.ToString());
    }

    var appveyorApi = new UriBuilder(EnvironmentVariableStrict("APPVEYOR_API_URL"));

    appveyorApi.Path = "/api/artifacts";

    using (var client = new HttpClient())
    {
        var artifactRequest = new AppveyorArtifactRequest(artifact);

        // Request upload URL
        var uploadDetails = JsonConvert.DeserializeObject<AppveyorArtifactResponse>(
            (
                await client.PostAsync(appveyorApi.Uri, new StringContent(
                    JsonConvert.SerializeObject(artifactRequest),
                    Encoding.UTF8,
                    "application/json"
                    )
                )
            )
            .Content
            .ReadAsStringAsync()
            .Result
        );

        Information($"Uploading Appveyor artifact '{artifactRequest.FileName}' to {uploadDetails.StorageType}");

        if (uploadDetails.IsAzureOrGoogle)
        {
            // PUT to cloud storage
            using (var data = new StreamContent(System.IO.File.OpenRead(artifact.ToString())))
            {
                await client.PutAsync(uploadDetails.UploadUrl, data);
            }

            // Finalise request with Appveyor
            await client.PutAsync(appveyorApi.Uri, new StringContent(
                    JsonConvert.SerializeObject(new AppVeyorArtifactFinalisation(artifact)),
                    Encoding.UTF8,
                    "application/json"
                    )
                );
        }
        else
        {
            // direct to Appveyor
            using (var wc = new WebClient())
            {
                wc.UploadFile(uploadDetails.UploadUrl, artifact.ToString());
            }
        }
    }
}

void UploadTestResults()
{
    if (!isAppveyor)
    {
        return;
    }

    using (var wc = new WebClient())
    {
        foreach(var result in GetFiles(testResultsDir + File("*.trx")))
        {
            wc.UploadFile($"https://ci.appveyor.com/api/testresults/mstest/{EnvironmentVariableStrict("APPVEYOR_JOB_ID")}", result.ToString());
        }
    }
}

void SetProjectProperty(IEnumerable<XElement>properties, string propertyName, string propertyValue)
{
    var elem = properties.Descendants(propertyName).FirstOrDefault();

    if (elem != null)
    {
        elem.Value = propertyValue;
    }
    else
    {
        properties.First().Add(new XElement(propertyName, propertyValue));
    }
}

string EnvironmentVariableStrict(string name)
{
    var val = EnvironmentVariable(name);

    if (string.IsNullOrEmpty(val))
    {
        throw new CakeException($"Required environment variable '{name}' not set.");
    }

    return val;
}

Version GetBuildVersion()
{
    var ver = GetBuildVersionInner();

    System.IO.File.WriteAllText(File("module.ver"), ver.ToString());

    return ver;
}

Version GetBuildVersionInner()
{
    var tag = EnvironmentVariable("APPVEYOR_REPO_TAG_NAME");

    if (tag == null)
    {
        var appveyorVersion = EnvironmentVariable("APPVEYOR_BUILD_NUMBER");

        if (appveyorVersion != null)
        {
            // Development version.
            return new Version($"0.0.{appveyorVersion}");
        }

        // Generate a version for private package manager.
        var localBuildVer = File("build.ver");

        Version newVer;

        if (FileExists(localBuildVer))
        {
            var ver = new Version(System.IO.File.ReadAllText(localBuildVer));
            newVer = new Version(ver.Major, ver.Minor, ver.Build + 1);
        }
        else
        {
            newVer = new Version("0.0.1");
        }

        System.IO.File.WriteAllText(localBuildVer, newVer.ToString());
        return newVer;
    }

    // Tagged, i.e. release build.
    var m = Regex.Match(tag, @"^v(?<version>\d+\.\d+\.\d+)$", RegexOptions.IgnoreCase);

    if (m.Success)
    {
        return new Version(m.Groups["version"].Value);
    }

    throw new CakeException($"Cannot determine version from tag: {tag}");
}

#endregion