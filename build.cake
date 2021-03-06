
// Load external file and tools
#tool "nuget:?package=GitVersion.CommandLine"

//Read script parameter
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Debug");

//Declare variable
GitVersion versionInfo;
var NugetRestoreUrl = "http://infrahost01:8081/repository/nuget-group//";
var NuGetPublishUrl = NugetRestoreUrl;
var nuGetApiKey = "8a4908df-3b3d-3511-9d2f-ccd543552a48";

//All .csproj from the solution. Used to build the projects
var csProjPathArray =  GetFiles("./**/*.csproj");
//.csproj to version. The GitVersion will be set in the csproj
var csProjToVersionPathArray =  GetFiles("./src/**/*.csproj");
//csproj of tests
var csProjToTestPathArray =  GetFiles("./tests/**/*.csproj");


Setup(ctx =>
    {
    // Executed BEFORE the first task.
    Information("Running tasks...");
    });


Teardown(ctx =>{
        // Executed AFTER the last task.
        Information("Finished running tasks.");
    });

//Comment step who are not needed
Task("Default")
    .IsDependentOn("Build")
    .IsDependentOn("ExecuteTests")
    .IsDependentOn("VersionCsProj")
    .IsDependentOn("PackageNuget")
    .IsDependentOn("PublishNuget")
    .Does(() =>{
         Information("Running Build...");    
    });

Task("Build")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .DoesForEach(csProjPathArray, (file) => { 

        DotNetCoreBuild(
            file.FullPath,
            new DotNetCoreBuildSettings{
                Configuration = configuration,
                ArgumentCustomization = args => args.Append("--no-restore --no-dependencies")               
            }
        );
    });

Task("ExecuteTests")
     .DoesForEach(csProjToTestPathArray, (file) => { 
        DotNetCoreTest(
            file.FullPath,
            new DotNetCoreTestSettings{
                Configuration = configuration,
                ArgumentCustomization = args => args.Append("--no-build --no-restore --verbosity normal ")               
            }
        );
    });

Task("VersionCsProj")
    .DoesForEach(csProjToVersionPathArray, (file) => { 

    versionInfo = GitVersion(
        new GitVersionSettings {
            RepositoryPath = ".",
            UpdateAssemblyInfo = true                
        }
    );   

    Information("Versioning " +file.FullPath + " with version " +versionInfo.FullSemVer);
    
    //Force adding commit number to package version if it is a Feature Branch
    var packageVersion =  versionInfo.NuGetVersionV2.Remove(versionInfo.NuGetVersionV2.Length -4) + versionInfo.CommitsSinceVersionSourcePadded;
    Information("Package version : " +packageVersion);

    XmlPoke(file.FullPath, "/Project/PropertyGroup/Version", versionInfo.FullSemVer.ToString());
    XmlPoke(file.FullPath, "/Project/PropertyGroup/FileVersion", versionInfo.AssemblySemVer.ToString());
    XmlPoke(file.FullPath, "/Project/PropertyGroup/PackageVersion", packageVersion.ToString());
    
    }).OnError(err => {      
        throw err;
    });
    //add this to .csproj and do a first commit before building
    // <Product>AppName</Product>   
    // <FileVersion>0.1.0.0</FileVersion>
    // <Version>0.1.0</Version>  
    // <PackageVersion>00000</PackageVersion> 


Task("Restore")   
    .IsDependentOn("Clean")
     .Does(() => { 
        Information("Restoring packages using .sln file");  
        DotNetCoreRestore(".", new DotNetCoreRestoreSettings {
            Sources = new[] { NugetRestoreUrl }
        });
     }).OnError(err => {      
        throw err;
    });;

Task("Clean")
    .DoesForEach(csProjPathArray, (file) => {  
        // Clean .dll 
        DotNetCoreClean(file.FullPath);
        //clean .nupkg
        CleanDirectory("./artifacts/");
    });

Task("PackageNuget")
     .DoesForEach(csProjPathArray, (file) => {
   
        DotNetCorePack(file.FullPath,new DotNetCorePackSettings{
           Configuration = configuration,
           OutputDirectory = "./artifacts/",
           NoBuild = true,
           ArgumentCustomization = args => args.Append("--no-restore ") 
                     
        });

    }).DeferOnError();

Task("PublishNuget")
    .IsDependentOn("PackageNuget")
    .Does(() => 
    {
        var files = GetFiles("./artifacts/*.nupkg");
        foreach (var file in files)
        {
            NuGetPush(file.FullPath, new NuGetPushSettings{
               ApiKey = "8a4908df-3b3d-3511-9d2f-ccd543552a48",
               Source = NuGetPublishUrl
            });
        }
       
    }).OnError(err => {      
        throw err;
    });




RunTarget(target);