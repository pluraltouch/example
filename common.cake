//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Defining folder(s)
var sourceFolder = Directory("./source");
class RepositoryInfo {
    public string CloneUrl {get; set; }
    public string GitUrl {get; set; }
}

public void CleanTask(string outputFolder, string configuration)
{
    CleanDirectory(outputFolder + configuration);
    CleanDirectories(repositoryFolder + "/**/bin/" + configuration);
    CleanDirectories(repositoryFolder + "/**/obj/" + configuration);    

}

public string GetRepositoryFolder(string cloneUrl)
{
    var split = cloneUrl.Replace(".git", "").Split('/', '\\');
    return split.LastOrDefault()?? "created-"+ DateTime.Now.ToString("{yyyy-MM-dd-HH-mm-ss-fff}");
}

