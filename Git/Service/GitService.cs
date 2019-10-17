using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

using Comindware.Solution.Git.ViewModel;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;

namespace Comindware.Solution.Git.Service
{
    public class GitService
    {
        private const string GitFolder = ".git";
        private const string Origin = "origin";
        private const string Undefined = "undefined";
        private const char CanonicalNameSeparator = '/';

        public string GetLocalRepository(string repository, string branch, string userId)
        {
            if (string.IsNullOrEmpty(repository))
            {
                throw new ArgumentNullException("LocalRepositoryPath");
            }

            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentNullException("UserId");
            }

            var path = Path.Combine(repository, userId, string.IsNullOrEmpty(branch)
                ? string.Concat(Undefined)
                : string.Concat(branch));

            if (branch == string.Empty)
            {
                branch = null;
            }

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }

        public string GetLocalRepository(GitConfiguration gitConfiguration, string userId)
        {
            return GetLocalRepository(gitConfiguration.Repository, gitConfiguration.Branch, userId);
        }

        public void LoadRepository(GitConfiguration gitConfiguration, GitCommitter gitCredentials, string userId, bool cleanAll = false, bool isNewBranch = false, bool isListBranch = false)
        {
            var path = this.GetLocalRepository(gitConfiguration, userId);
            if (Directory.Exists(path))
            {
                var gitFolder = Directory.GetDirectories(path, GitFolder, SearchOption.TopDirectoryOnly);

                if (!cleanAll && gitFolder?.Length > 0)
                {
                    return;
                }
            }

            this.Clone(gitConfiguration, gitCredentials, userId, cleanAll, isNewBranch, isListBranch);
        }

        public void Clone(GitConfiguration gitConfiguration, GitCommitter gitCredentials, string userId, bool cleanAll = false, bool isNewBranch = false, bool isListBranch = false)
        {
            var localRepository = this.GetLocalRepository(gitConfiguration, userId);
            if (cleanAll)
            {
                this.DeleteFolder(localRepository);
            }

            if (!Directory.Exists(localRepository))
            {
                Directory.CreateDirectory(localRepository);
            }
            if (isNewBranch  && gitConfiguration.Branch == string.Empty)
            {
                gitConfiguration.Branch = null;
            }
            var co = this.GetCloneOptions(gitConfiguration, gitCredentials);
            
            Repository.Clone(gitConfiguration.URL, localRepository, co);

            if(!isListBranch)
            {
                //SolutionValidate(localRepository);
            }
        }

        public void AddRemoteBranch(GitConfiguration gitConfiguration, GitCommitter gitCredentials, string sourceBranch, string targetBranch, string commitMessage, string userId)
        {
            this.LoadRepository(gitConfiguration, gitCredentials, userId, true, true);
            using (var repo = new Repository(GetLocalRepository(gitConfiguration.Repository, sourceBranch, userId)))
            {
                var trackedBranch = !repo.Branches.Any() 
                    ? repo.Branches.Add(targetBranch, repo.Commit(commitMessage, GetCommiter(gitCredentials), GetCommiter(gitCredentials))) 
                    : repo.CreateBranch(targetBranch);

                repo.Network.Push(repo.Network.Remotes[Origin], trackedBranch.CanonicalName, this.GetPushOptions(gitCredentials));
            }
        }

        public void Checkout(GitConfiguration gitConfiguration, string userId)
        {
            using (var repo = new Repository(this.GetLocalRepository(gitConfiguration, userId)))
            {
                var branchRef = this.GetBranchRef(repo, gitConfiguration.Branch);
                Commands.Checkout(repo, branchRef);
            }
        }

        public void Push(GitConfiguration gitConfiguration, GitCommitter gitCredentials, string userId, string commitMessage = null, string tag = null)
        {
            using (var repo = new Repository(this.GetLocalRepository(gitConfiguration, userId)))
            {
                Commands.Pull(repo, this.GetCommiter(gitCredentials), this.GetPullOptions(gitCredentials));
                Commands.Stage(repo, "*");

                var commit = repo.Commit(commitMessage, GetCommiter(gitCredentials), GetCommiter(gitCredentials));
                repo.Network.Push(repo.Network.Remotes[Origin], GetBranchRef(repo, gitConfiguration.Branch), this.GetPushOptions(gitCredentials));

                if (string.IsNullOrEmpty(tag))
                {
                    return;
                }

                tag = $"{gitConfiguration.Branch}_{tag}";

                var canonicalNameTag = repo.ApplyTag(tag, commit.Id.Sha, GetCommiter(gitCredentials), tag).CanonicalName;
                repo.Network.Push(repo.Network.Remotes[Origin], canonicalNameTag, this.GetPushOptions(gitCredentials));
            }
        }

        public bool Test(GitConfiguration gitConfiguration, GitCommitter gitCredentials, string userId)
        {
            this.LoadRepository(gitConfiguration, gitCredentials, userId, true);

            GetBranches(gitConfiguration, gitCredentials, userId);

            return true;
        }

        public IList<string> GetBranches(GitConfiguration gitConfiguration, GitCommitter gitCredentials, string userId)
        {
            this.LoadRepository(gitConfiguration, gitCredentials, userId, true, isListBranch:true);

            var list = new List<string>();
            using (var repo = new Repository(this.GetLocalRepository(gitConfiguration, userId)))
            {
                foreach (var b in repo.Branches)
                {
                    var name = b.CanonicalName.Split(CanonicalNameSeparator).Last();
                    if (!list.Contains(name))
                    {
                        list.Add(name);
                    }
                }
            }

            return list;
        }

        public List<GitCommit> GetCommits(GitConfiguration gitConfiguration, GitCommitter gitCredentials, string userId)
        {
            this.LoadRepository(gitConfiguration, gitCredentials, userId, true);

            var commits = new List<GitCommit>();
            using (var repo = new Repository(GetLocalRepository(gitConfiguration, userId)))
            {
                var commitIdToTagLookup = this.CreateCommitIdToTagLookup(repo);

                var bref = GetBranchRef(repo, gitConfiguration.Branch);
                foreach (var b in repo.Branches)
                {
                    if (b.CanonicalName != bref)
                    {
                        continue;
                    }

                    foreach (var commit in b.Commits)
                    {
                        var tag = string.Empty;
                        if (commitIdToTagLookup.Contains(commit.Id))
                        {
                            tag = commitIdToTagLookup[commit.Id]?.First().FriendlyName;
                        }

                        commits.Add(new GitCommit()
                        {
                            Description = commit.Message,
                            Commiter = commit.Committer.Name,
                            DateTime = commit.Committer.When.DateTime,
                            ID = commit.Id.ToString(),
                            Tag = tag.Replace($"{gitConfiguration.Branch}_", "")
                        });
                    }
                }
            }

            return commits;
        }

        public List<string> Merge(GitConfiguration gitConfiguration, GitCommitter gitCredentials, string targetBranchName, string userId, CheckoutFileConflictStrategy strategy = CheckoutFileConflictStrategy.Merge)
        {
            var path = this.GetLocalRepository(gitConfiguration, userId);
            using (var repo = new Repository(path))
            {
                var branch = repo.Branches[targetBranchName];
                if (branch == null)
                {
                    throw new Exception("Branch is busy");
                }
                var opts = new MergeOptions { FileConflictStrategy = strategy };

                repo.Merge(branch, new Signature(gitCredentials.UserName, gitCredentials.Email, DateTime.Now), opts);

                if (repo.Index.Conflicts.Any())
                {
                    return repo.Index.Conflicts
                        .Select(x => x.Ours.Path)
                        .ToList();
                }

                repo.Network.Push(repo.Network.Remotes[Origin], GetBranchRef(repo, Path.GetFileName(path)), GetPushOptions(gitCredentials));
                return null;
            }
        }

        public void SolutionValidate(string repository)
        {
            var files = Directory.GetFiles(repository, "*", SearchOption.TopDirectoryOnly);
            if (files.Length != 1 || !files[0].Contains("metadata.json"))
            {
                throw new Exception("Incorrect repository");
            }

            var directory = Directory.EnumerateDirectories(repository, "*", SearchOption.TopDirectoryOnly).Where(d => !d.ToLower().EndsWith(".git")).ToList();
            if (directory.Count != 1 || Directory.GetFiles(directory[0], "*", SearchOption.TopDirectoryOnly).Length != 1)
            {
                throw new Exception("Incorrect repository");
            }

            var directories = Directory.GetDirectories(directory[0], "*", SearchOption.TopDirectoryOnly);
            if (directories == null)
            {
                throw new Exception("Incorrect repository");
            }

            var incorrectFiles = Directory.EnumerateFiles(repository, "*.json*", SearchOption.AllDirectories).Where(f => !f.ToLower().EndsWith(".json")).ToList();
            if (incorrectFiles.Any())
            {
                throw new Exception("Incorrect repository");
            }
        }

        private ILookup<ObjectId, Tag> CreateCommitIdToTagLookup(Repository repo)
        {
            var commitIdToTagLookup =
                repo.Tags
                .Select(tag => new { Commit = tag.PeeledTarget as Commit, Tag = tag })
                .Where(x => x.Commit != null)
                .ToLookup(x => x.Commit.Id, x => x.Tag);

            return commitIdToTagLookup;
        }

        private Signature GetCommiter(GitCommitter gitCredentials)
        {
            return new Signature(gitCredentials.UserName, gitCredentials.Email, DateTime.Now);
        }

        private UsernamePasswordCredentials GetCreds(GitCommitter gitCredentials)
        {
            return new UsernamePasswordCredentials
            {
                Username = gitCredentials.User,
                Password = gitCredentials.Password
            };
        }

        private CloneOptions GetCloneOptions(GitConfiguration gitConfiguration, GitCommitter gitCredentials)
        {
            return new CloneOptions()
            {
                BranchName = gitConfiguration.Branch,
                CredentialsProvider = (_url, _user, _cred) => this.GetCreds(gitCredentials)
            };
        }

        private PullOptions GetPullOptions(GitCommitter gitCredentials)
        {
            return new PullOptions()
            {
                FetchOptions = new FetchOptions()
                {
                    CredentialsProvider = new CredentialsHandler( (a, b, c) => this.GetCreds(gitCredentials))
                },
                MergeOptions = new MergeOptions()
            };
        }

        public PushOptions GetPushOptions(GitCommitter gitCredentials)
        {
            return new PushOptions()
            {
                CredentialsProvider = (_url, _user, _cred) => this.GetCreds(gitCredentials)
            };
        }

        public string GetBranchRef(Repository repo, string branch)
        {
            foreach (var val in repo.Branches)
            {
                if (val.FriendlyName.EndsWith(branch) && !val.FriendlyName.StartsWith(Origin, true, System.Globalization.CultureInfo.CurrentCulture))
                {
                    return val.CanonicalName;
                }
            }

            return null;
        }

        private void DeleteFolder(string targetDirectory)
        {
            if (!Directory.Exists(targetDirectory))
            {
                return;
            }

            var files = System.IO.Directory.GetFiles(targetDirectory);
            foreach (var file in files)
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception)
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }
            }

            var folders = System.IO.Directory.GetDirectories(targetDirectory);
            foreach (var folder in folders)
            {
                DeleteFolder(folder);
                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder, true); 
                }
            }

            folders = System.IO.Directory.GetDirectories(targetDirectory);
            if (folders?.Count() == 0)
            {
                System.IO.Directory.Delete(targetDirectory);
            }
        }
    }
}
