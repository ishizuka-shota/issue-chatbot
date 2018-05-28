using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Web;
using Octokit;
using Octokit.Internal;

namespace SimpleEchoBot.issue
{
    public class issueCreate
    {

        public async Task Create(GitHubClient github, string title, string body) {
            var newIssue = new NewIssue(title)
            {
                Body = body
            };

            await github.Issue.Create("ishizuka-shota", "SampleAPI", newIssue);
        }
    }
}