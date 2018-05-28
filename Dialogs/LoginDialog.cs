using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Sample.SimpleEchoBot;
using Octokit;
using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Web.Security;

namespace SimpleEchoBot.Dialogs
{
    [Serializable]
    public class LoginDialog : IDialog<object>
    {
        public static string csrf = string.Empty;

        public async Task StartAsync(IDialogContext context)
        {

            csrf = Membership.GeneratePassword(20, 0);

            var request = new OauthLoginRequest(ConfigurationManager.AppSettings["client_id"])
            {
                Scopes = { "repo", "user" },
                State = csrf
            };

            String uri = GitHubDialog.github.Oauth.GetGitHubLoginUrl(request).ToString();

            await context.PostAsync(uri);

            //using (WebClient webClient = SlackApi.SlackApi.CreateHeader_Post())
            //{
            //    var slack = new SlackModel<ButtonActionsModel>()
            //    {
            //        Text = "GitHubへのログインを行ってください",
            //        Attachments = new List<AttachmentsModel<ButtonActionsModel>>()
            //            {
            //                new AttachmentsModel<ButtonActionsModel>()
            //                {
            //                    Callback_id = "menu",
            //                    Actions = new List<ButtonActionsModel>()
            //                    {
            //                        new ButtonActionsModel(uri)
            //                        {
            //                            Name = "GitHubLogin",
            //                            Text = "ログイン",
            //                            Value = uri
            //                        }
            //                    }
            //                }
            //            }

            //    };
            //    string json = Newtonsoft.Json.JsonConvert.SerializeObject(slack);

            //    string response = webClient.UploadString(new Uri("https://slack.com/api/chat.postMessage"), json);
            //}

            context.Done<object>(context);

        }
    }
}