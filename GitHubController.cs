using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Octokit;
using System.Windows.Forms;
using System.Net;

namespace AutoGitHub
{
    //placed this to see if the auto update works
    public class GitHubController
    {
        Credentials tokenAuth = new Credentials(CredentialManager.GetGitToken());
        GitHubClient client;

        public GitHubController()
        {
            client = new GitHubClient(new ProductHeaderValue("JP_OctoClient")) { Credentials = tokenAuth };
        }

        public async Task<string> GetName(long repoID)
        {
            var repo = await client.Repository.Get(repoID);
            return repo.FullName;
        }

        public async Task<bool> CheckRepo(string repoName)
        {
            var repos = await client.Repository.GetAllForCurrent();
            foreach(var repo in repos)
            {
                if (repo.Name == repoName)
                    return true;
            }
            return false;
        }

        private async Task<long> GetRepo(string name)
        {
            var repos = await client.Repository.GetAllForCurrent();
            foreach(var repo in repos)
            {
                if (repo.Name == name)
                    return repo.Id;
            }
            return -1L;
        }

        private async Task<NewTree> PopulateTree(Commit latestCommit, string mainFP, long targetRepo)
        {
            //This function will create a tree based off of the Main file path's folder and file structure
            //Create our variables, our NewTree is based off our last commit
            var nt = new NewTree { BaseTree = latestCommit.Tree.Sha };
            var currentDir = new DirectoryInfo(mainFP);
            
            //Start our Queue with our Main File Path as our initial directory
            var toCheck = new List<DirectoryInfo>() { currentDir };
            var toAdd = new List<FileInfo>();

            //We need to use a Queue to make sure we don't miss any files
            while (toCheck.Count > 0)
            {
                //Pop first folder
                var targetFolder = toCheck[0];
                toCheck.RemoveAt(0);

                //Check for new folders and add them to que
                foreach (var folder in targetFolder.GetDirectories())
                    toCheck.Add(folder);
                
                //Check for all files in said folders and add to our toAdd que
                foreach (var file in targetFolder.GetFiles())
                {
                    if (!file.Name.Contains("-Omit")) //this is done for files that contain sensitive info
                    {
                        toAdd.Add(file);
                    }
                }
            }

            //Once complete, make the tree by turning every file into a tree item
            foreach(var file in toAdd)
            {
                
                //Blob settings for TEXT based files.
                var blob = new NewBlob { Encoding = EncodingType.Utf8, Content = File.ReadAllText(file.FullName) };
                var blobRef = await client.Git.Blob.Create(targetRepo, blob);

                //Prepare a string so we can create a proper folder system by taking the file's full path and subtracting our MainFP
                var preGitFileName = file.FullName.Replace(mainFP, "");
                var gitFileName = preGitFileName.Replace(@"\", @"/").TrimStart('/');

                //Add our file to the tree
                nt.Tree.Add(new NewTreeItem { Path = gitFileName, Mode = "100644", Type = TreeType.Blob, Sha = blobRef.Sha });
            }
            
            return nt;
        }

        private async void PushCommit(long targetRepo, string mainFP, string commitComment)
        {
            //Get branch of repo
            var headMasterRef = "heads/master";
            //Get correct branch from github and store it
            var masterRef = await client.Git.Reference.Get(targetRepo, headMasterRef);
            //Get the latest commit to base our Tree off of
            var latestCommit = await client.Git.Commit.Get(targetRepo, masterRef.Object.Sha);

            //Populate a new tree based off the current tree in github
            var tree = await PopulateTree(latestCommit, mainFP, targetRepo);
            //Create the new tree including the new additions
            var newTree = await client.Git.Tree.Create(targetRepo, tree);
            //Create a new Commit along with a commit comment. We send the SHA of our new tree and our reference/branch
            var newCommit = new NewCommit(commitComment, newTree.Sha, masterRef.Object.Sha);
            //Create the commit in Git
            var commit = await client.Git.Commit.Create(targetRepo, newCommit);
            //Send the update to our targeted repo, targeted branch, and our commit which includes the tree
            await client.Git.Reference.Update(targetRepo, headMasterRef, new ReferenceUpdate(commit.Sha));
        }

        public async void HandleRepo(string name, string mainFP, TextBox outputBox)
        {
            await CheckRepo(name);
            if (!await CheckRepo(name))
            {
                var description = Prompts.ShowDialog_TextBox("Enter a Description");
                description = description == "" ? "No Description" : description; //if "" returned, then set description to "No Description"
                await client.Repository.Create(CreateRepo(name, description, await GetGitIgnoreTemplates()));
                outputBox.Text = string.Format("Repo \"{0}\" Created",name);
            }

            var targetRepo = await GetRepo(name);
            if (targetRepo != -1)
            {
                Console.WriteLine("Found Repo");
                var commitComment = Prompts.ShowDialog_TextBox("Enter a Commit Comment");
                commitComment = commitComment == "" ? "No Commit Comment" : commitComment; //if "" returned, then set commitComment to "No Commit Comment"
                PushCommit(targetRepo, mainFP, commitComment);
                outputBox.Text = string.Format("Update to repo \"{0}\" successful", name);
            }
        }

        public async void EditDesc(string name, string description)
        {
            if (!await CheckRepo(name))
                return;
            var update = new RepositoryUpdate(name) { Description = description };
            await client.Repository.Edit(await GetRepo(name), update);
        }

        public async Task<string> GetGitIgnoreTemplates()
        {
            //Prefix: I originally created code to pull the file's raw url, save the content, and make a custom ignore template.
            //        That isn't required as the API allows you to just pass the name of the template and it will take care of it
            //        Unfortunately i didn't catch that the first time around otherwise i wouldn't have done that but at least
            //        now i know how to pull any file from github via c#

            //Get the repo that contains the templates
            var ignoreRepoID = await GetRepo("gitignore");
            var repoContent = await client.Repository.Content.GetAllContents(ignoreRepoID);

            //populate first entry into list so we have an escape/no template selection
            var nameList = new List<string>() { "No Ignore Template" };

            //iterate and find all files ending in .gitignore
            foreach (var entry in repoContent)
            {
                if (entry.Path.EndsWith(".gitignore"))
                {
                    //Add those files to the list while removing the .gitignore for later
                    nameList.Add(entry.Name.Replace(".gitignore",""));
                }
            }
            //send prompt and get response of user, if no template needed, return the value below
            var response = Prompts.ShowDialog_ComboBox(nameList, "Ignore Templates");
            if (response == "No Ignore Template" || response == "")
                return "";
            //return the name of the template we want
            return response;
        }

        private NewRepository CreateRepo(string name, string description, string gitIgnoreName = "")
        {
            if (gitIgnoreName != "")
            {
                return new NewRepository(name)
                {
                    AutoInit = true,
                    Description = description,
                    Private = false,
                    GitignoreTemplate = gitIgnoreName
                };
            }
            return new NewRepository(name)
            {
                AutoInit = true,
                Description = description,
                Private = false
            };
        }
    }
}
