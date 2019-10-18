﻿using System;
using System.IO;
using LibGit2Sharp;
using Solution.Git.Service;
using Solution.Git.ViewModel;

namespace TestMergeFirstMethod
{
    public static class MergeMethodOne
    {
        private const string userId = "account.1";
        private static GitService _gitService = new GitService();
        private static string tempBranch;

        public static bool FirstPart(string repos, string URL, string sourceSolution, string targetSolution, GitCommitter gitCommitter, string sourceBranch, string targetBranch, string commitMessage)
        {
            tempBranch = $"tempBranch{new Random().Next(0, 200000000)}";
            _gitService.AddRemoteBranch(new GitConfiguration()
            {
                Branch = sourceBranch,
                Message = commitMessage,
                Repository = repos,
                Solution = sourceSolution,
                URL = URL
            }, gitCommitter, sourceBranch, tempBranch, commitMessage, userId);
            _gitService.Clone(new GitConfiguration()
            {
                Branch = tempBranch,
                Message = commitMessage,
                Repository = repos,
                URL = URL
            }, gitCommitter, userId,true);

            Directory.Move(Path.Combine(_gitService.GetLocalRepository(repos, tempBranch, userId), $"{sourceSolution}"),
                Path.Combine(_gitService.GetLocalRepository(repos, tempBranch, userId), $"{targetSolution}"));

            _gitService.Push(new GitConfiguration()
            {
                Branch = tempBranch,
                Message = commitMessage,
                Repository = repos,
                URL = URL
            }, gitCommitter, userId, commitMessage);

            var mergeResult = _gitService.Merge(new GitConfiguration
            {
                Branch = tempBranch,
                Message = commitMessage,
                Repository = repos,
                URL = URL
            }, gitCommitter, targetBranch, userId);

            if (mergeResult != null)
            {
                return false;
            }

            SecondPart(repos, gitCommitter, targetBranch);
            return true;
        }

        public static void SecondPart(string repos, GitCommitter gitCommitter, string targetBranch)
        {
            using (var repository = new Repository(_gitService.GetLocalRepository(repos, "undefined", userId)))
            {
                var remote = repository.Network.Remotes["origin"];
                var options = new PushOptions();
                var credentials =  _gitService.GetPushOptions(gitCommitter);
                options.CredentialsProvider = credentials.CredentialsProvider;
                var pushRefSpec = $"+:refs/heads/{tempBranch}";
                repository.Network.Push(remote, pushRefSpec,options);
            }
        }
    }
}