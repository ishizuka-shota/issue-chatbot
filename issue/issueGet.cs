using System.Collections.Generic;
using System.Threading.Tasks;
using Octokit;
using Microsoft.Bot.Builder.Dialogs;

namespace SimpleEchoBot.issue
{
    public class issueGet
    {

        public async Task Get(IDialogContext context, GitHubClient github)
        {
            IReadOnlyList<Issue> issues = await github.Issue.GetAllForRepository("ishizuka-shota", "SampleAPI");
            foreach (Issue issue in issues)
            {
                await context.PostAsync("Number:" + issue.Number);
                await context.PostAsync("Title:" + issue.Title);
                await context.PostAsync("Date:" + issue.CreatedAt);
                await context.PostAsync("Body:" + issue.Body);
                await context.PostAsync("User:", issue.User.Login);
                await context.PostAsync("--------");
            }
        }
    }
}