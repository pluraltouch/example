// Note #1: Cake.Git: The loaddependencies=true will take effect 
// in case using Cake built in NuGet support and not the external nuget.exe

// Note #2: Cake.Git: We must use older version of Cake.Git because of internal incompatibility with LibGit2Sharp  
// error CS0029: Cannot implicitly convert type 'System.Collections.Generic.List<LibGit2Sharp.Tag>' to 'System.Collections.Generic.List<LibGit2Sharp.Tag>'
#addin nuget:?package=Cake.Git&version=0.16.1&loaddependencies=true

#tool nuget:?package=NUnit.ConsoleRunner&version=3.4.0
//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "default");
var configuration = Argument("configuration", "Debug");
var gitUserName = Argument("git-username", "<username>");
var gitPassword = Argument("git-password", "******");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define directories.
var repositoryFolder = Directory(".");
    
// TODO: Not hardcode outputfolder(s), instead infer from the projects (*.csproj files)
var outputFolder = repositoryFolder + Directory("output");

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("clean")
    .Does(() =>
{
    CleanDirectory(outputFolder.Path + configuration);
    CleanDirectories(repositoryFolder.Path +  "/**/bin/" + configuration);
    CleanDirectories(repositoryFolder.Path + "/**/obj/" + configuration);    
});

Task("git-pull")
    .Does(() =>
{
    // Note: The repo should not have uncommitted changes for this operation to work:
    // Note: Object [develop] must be known in the local git config, so the original clone must clone that branch (too)
    // It turned out that the following lines will silently overwrite local changes, so checnking before:

    if (GitHasUncommitedChanges(repositoryFolder))
    {
        throw new Exception($"Repository '{repositoryFolder}' has uncommitted changes. Please commit befores pulling");
    }
    GitCheckout(repositoryFolder, "develop", new FilePath[0]);
    GitPull(repositoryFolder, "ses.cake.merger", "ses.cake.merger@msesolutions.net.au", gitUserName, gitPassword, "origin");
});

Task("clean-all")
    .IsDependentOn("git-pull")
    .Does(() =>

{
    CleanDirectory(outputFolder.Path);
    CleanDirectories(repositoryFolder.Path +  "/**/bin");
    CleanDirectories(repositoryFolder.Path + "/**/obj");    
    CleanDirectories(repositoryFolder.Path + "/**/packages");    
    CleanDirectories(repositoryFolder.Path + "/lib");        
});

Task("restore-nuget")
    .IsDependentOn("clean-all")
    .Does(() =>
{
    // Get all solutions (usualy the One)
    var solutions = GetFiles(repositoryFolder.Path + "/**/*.sln");

    // Use custom restore settings:
    var restoreSettings = new NuGetRestoreSettings {
        // This is the place to add custom settings:
    };
    
    // Take any special settings in effect * if any *, but not in a hardcoded way (for example the usual SES repositoryPath)
    var configFile = GetFiles(repositoryFolder.Path + "/**/nuget.config").FirstOrDefault();
    if (configFile != null)
    {
        restoreSettings.ConfigFile = configFile;
    }

    // Restore the packages
    NuGetRestore(solutions, restoreSettings);
});

Task("update-nuget")
    .IsDependentOn("restore-nuget")
    .Does(() =>
{
    // Get all solutions (usualy the One)
    var solutions = GetFiles(repositoryFolder.Path + "/**/*.sln");

    // Update the packages
    // Use custom settings:
    var updateSettings = new NuGetUpdateSettings {
        // This is the place to add custom settings:
        Prerelease = false
    };
    NuGetUpdate(solutions, updateSettings);                
});

Task("build")
    .IsDependentOn("update-nuget")
    .Does(() =>
{
     var solutions = GetFiles(repositoryFolder.Path + "/**/*.sln");
     foreach(var solution in solutions)
     {
        MSBuild(solution, settings => settings.SetConfiguration(configuration));
     }
});

Task("run-unit-tests")
    .IsDependentOn("build")
    .Does(() =>
{
    var folders = new []{
        outputFolder.Path + configuration + "/*.Tests.dll",
        outputFolder.Path + configuration + "/*.Test.dll",
        // For non SES conform projects:        
        "./**/bin/" + configuration + "/*.Tests.dll",
        "./**/bin/" + configuration + "/*.Test.dll"
    };

    foreach(var folder in folders)
    {
    NUnit3(folder, new NUnit3Settings {
        NoResults = true
        });

    }
});


Task("git-commit")
    .IsDependentOn("run-unit-tests")
    .Does(() =>
{
        //GitAddAll(repositoryFolder);
        GitUnstageAll(repositoryFolder);
        GitCommit(repositoryFolder, "ses.cake.merger", "ses.cake.merger@msesolutions.net.au", "Commit done by an automated Cake script");
});

Task("git-push")
    .IsDependentOn("git-commit")
    .Does(() =>
{
    GitPush(repositoryFolder, gitUserName, gitPassword);
});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("default")
    .IsDependentOn("git-push");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
