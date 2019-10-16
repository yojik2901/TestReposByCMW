using System;

namespace Comindware.Solution.Git.ViewModel
{
    public class Configuration
    {
        public string Repository { get; set; }
        public string Branch { get; set; }
    }

    public class ConfigurationWithSolution : Configuration
    {
        public string Solution { get; set; }
    }

    public class GitCommit
    {
        public string Commiter { get; set; }
        public DateTime DateTime { get; set; }
        public string Description { get; set; }
        public string ID { get; set; }
        public string Tag { get; set; }
    }

    public class GitCommitter
    {
        public string User { get; set; }
        public string Password { get; set; }

        public string UserName { get; set; }
        public string Email { get; set; }
    }

    public class GitConfiguration : ConfigurationWithSolution
    {
        public GitConfiguration(GitPushPullConfiguration configuration)
        {
            Branch = configuration?.Branch;
            Repository = configuration?.Repository;
            Solution = configuration?.Solution;
            Message = configuration?.Message;
        }
        public GitConfiguration(GitSolutionConfiguration configuration = null)
        {
            Branch = configuration?.Branch;
            Repository = configuration?.Repository;
        }
        public string URL { get; set; }
        public string Message { get; set; }
    }

    public class GitPushPullConfiguration : ConfigurationWithSolution
    {
        public string Message { get; set; }
    }

    public class GitMergeConfiguration : ConfigurationWithSolution
    {
        public string MasterBranch { get; set; }
        public bool FromMaster { get; set; }
    }

    public class GitSolutionConfiguration : Configuration
    {
        public string Message { get; set; }
        public string NewNameBranch { get; set; }
        public GitCommitter UserCredentials { get; set; }
    }
}
