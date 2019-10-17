using System;
using Comindware.Solution.Git.ViewModel;

namespace TestMergeFirstMethod
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Enter number merge method (1 or 2)");
            var method = Console.ReadLine();
            if(method.Contains("1"))
            {
                MergeOne();
            }

            else if (method.Contains("2"))
            {
                MethodTwo();
            }
        }

        private static void MethodTwo()
        {
            Console.Write("\nEnter repository = ");
            //string repos = @"C:\Users\sorlov\repos";
            string repos = Console.ReadLine();

            Console.Write("\nEnter URL = ");
            //string URL = @"https://aoleynikov@gitrepo.comindware.com/scm/demo/transfer.git";
            string URL = Console.ReadLine();

            Console.Write("\nEnter source branch = ");
            //string sourceBranch = "NewBrunchByOrlov3";
            string sourceBranch = Console.ReadLine();

            Console.Write("\nEnter source solution name = ");
            //string sourceSolution = "Sol2";
            string sourceSolution = Console.ReadLine();

            Console.Write("\nEnter target branch = ");
            string targetBranch = Console.ReadLine();
            //string targetBranch = "Solution2Branch";

            Console.Write("\nEnter target solution name = ");
            string targetSolution = Console.ReadLine();
            //string targetSolution = "systemSolution";

            Console.Write("\nEnter Email = ");
            var Email = Console.ReadLine();
            Console.Write("\nEnter Password = ");
            var Password = Console.ReadLine();
            Console.Write("\nEnter User = ");
            var User = Console.ReadLine();
            Console.Write("\nEnter UserName = ");
            var UserName = Console.ReadLine();

            //Console.Write("\nEnter repository = ");
            GitCommitter gitCommitter = new GitCommitter()
            {
                Email = Email,
                Password = Password,
                User = User,
                UserName = UserName
            };

            Console.Write("\nEnter commit message = ");
            string commitMessage = Console.ReadLine();

            var first = MergeMethodTwo.FirstPart(repos, URL, sourceSolution, targetSolution, gitCommitter, sourceBranch, targetBranch, commitMessage);
            if (first)
            {
                Console.WriteLine("true");
            }
            else
            {
                Console.WriteLine("Нажмите кномпку как решите конфликты");
                Console.ReadLine();
                Console.ReadLine();
                MergeMethodTwo.SecondPart(repos, gitCommitter, targetBranch, sourceBranch, commitMessage, URL);
            }
        }

        public static void MergeOne()
        {
            Console.Write("\nEnter repository = ");
            //string repos = @"C:\Users\sorlov\repos";
            string repos = Console.ReadLine();

            Console.Write("\nEnter URL = ");
            //string URL = @"https://aoleynikov@gitrepo.comindware.com/scm/demo/transfer.git";
            string URL = Console.ReadLine();

            Console.Write("\nEnter source branch = ");
            //string sourceBranch = "NewBrunchByOrlov3";
            string sourceBranch = Console.ReadLine();

            Console.Write("\nEnter source solution name = ");
            //string sourceSolution = "Sol2";
            string sourceSolution = Console.ReadLine();

            Console.Write("\nEnter target branch = ");
            string targetBranch = Console.ReadLine();
            //string targetBranch = "Solution2Branch";

            Console.Write("\nEnter target solution name = ");
            string targetSolution = Console.ReadLine();
            //string targetSolution = "systemSolution";

            Console.Write("\nEnter Email = ");
            var Email = Console.ReadLine();
            Console.Write("\nEnter Password = ");
            var Password = Console.ReadLine();
            Console.Write("\nEnter User = ");
            var User = Console.ReadLine();
            Console.Write("\nEnter UserName = ");
            var UserName = Console.ReadLine();

            //Console.Write("\nEnter repository = ");
            GitCommitter gitCommitter = new GitCommitter()
            {
                Email = Email,
                Password = Password,
                User = User,
                UserName = UserName
            };

            Console.Write("\nEnter commit message = ");
            string commitMessage = Console.ReadLine();

            var first = MergeMethodOne.FirstPart(repos, URL, sourceSolution, targetSolution, gitCommitter, sourceBranch, targetBranch, commitMessage);
            if (first)
            {
                Console.WriteLine("true");
            }
            else
            {
                Console.WriteLine("Нажмите кномпку как решите конфликты");
                Console.ReadLine();
                Console.ReadLine();
                MergeMethodOne.SecondPart(repos, gitCommitter, targetBranch);
            }

        }
    }
}
