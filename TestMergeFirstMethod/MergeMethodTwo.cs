using System;
using System.IO;
using LibGit2Sharp;
using Solution.Git.Service;
using Solution.Git.ViewModel;

namespace TestMergeFirstMethod
{
    public class MergeMethodTwo
    {
        private const string userId = "account.1";
        private static GitService _gitService = new GitService();
        private static string tempBranchTarget;
        private static string tempBranchSource;

        public static bool FirstPart(string repos, string URL, string sourceSolution, string targetSolution, GitCommitter gitCommitter, string sourceBranch, string targetBranch, string commitMessage)
        {
            tempBranchTarget = $"tempBranchTarget{new Random().Next(0, 200000000)}";

            _gitService.AddRemoteBranch(new GitConfiguration()
            {
                Branch = targetBranch,
                Message = commitMessage,
                Repository = repos,
                Solution = sourceSolution,
                URL = URL
            }, gitCommitter, targetBranch, tempBranchTarget, commitMessage, userId);

            _gitService.Clone(new GitConfiguration()
            {
                Branch = tempBranchTarget,
                Message = commitMessage,
                Repository = repos,
                URL = URL
            }, gitCommitter, userId, true);

            Directory.Move(Path.Combine(_gitService.GetLocalRepository(repos, tempBranchTarget, userId), $"{sourceSolution}"),
                Path.Combine(_gitService.GetLocalRepository(repos, tempBranchTarget, userId), $"{targetSolution}"));

            _gitService.Push(new GitConfiguration()
            {
                Branch = tempBranchTarget,
                Message = commitMessage,
                Repository = repos,
                URL = URL
            }, gitCommitter, userId, commitMessage);

            _gitService.AddRemoteBranch(new GitConfiguration
            {
                Branch = sourceBranch,
                Message = commitMessage,
                Repository = repos,
                Solution = sourceSolution,
                URL = URL
            }, gitCommitter, sourceBranch, tempBranchSource, commitMessage, userId);

            var mergeResult = _gitService.Merge(new GitConfiguration
            {
                Branch = tempBranchSource,
                Message = commitMessage,
                Repository = repos,
                URL = URL
            }, gitCommitter, tempBranchTarget, userId);

            if (mergeResult != null)
            {
                return false;
            }

            SecondPart(repos, gitCommitter, targetBranch, sourceBranch, commitMessage, URL);
            return true;
        }

        public static void SecondPart(string repos, GitCommitter gitCommitter, string targetBranch, string sourceBranch, string commitMessage, string URL)
        {
            using (var repository = new Repository(_gitService.GetLocalRepository(repos, "undefined", userId)))
            {
                var remote = repository.Network.Remotes["origin"];
                var options = new PushOptions();
                var credentials = _gitService.GetPushOptions(gitCommitter);
                options.CredentialsProvider = credentials.CredentialsProvider;
                var pushRefSpec = $"+:refs/heads/{tempBranchTarget}";
                repository.Network.Push(remote, pushRefSpec, options);
                
                var mergeResult = _gitService.Merge(new GitConfiguration
                {
                    Branch = sourceBranch,
                    Message = commitMessage,
                    Repository = repos,
                    URL = URL
                }, gitCommitter, tempBranchSource, userId, CheckoutFileConflictStrategy.Ours);

                options = new PushOptions();
                credentials = _gitService.GetPushOptions(gitCommitter);
                options.CredentialsProvider = credentials.CredentialsProvider;
                pushRefSpec = $"+:refs/heads/{tempBranchSource}";
                repository.Network.Push(remote, pushRefSpec, options);
            }
        }
    }
}