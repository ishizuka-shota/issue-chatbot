using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Sample.SimpleEchoBot;
using Octokit;
using SimpleEchoBot.RedisCache;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleEchoBot.Dialogs
{
    [Serializable]
    public class IssueDisplayDialog : BaseDialog, IDialog<object>
    {

        #region issue表示1（表示形式選択）
        /// <summary>
        /// 表示形式選択
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task StartAsync(IDialogContext context)
        {
            PromptDialog.Choice(context, this.IssueGet, new List<string> { "すべて", 
                                                                           "No,Titleのみ",
                                                                           "キャンセル"}, "表示方法を選んでください");
        }
        #endregion

        #region issue表示2（表示方法）
        /// <summary>
        /// issue表示
        /// </summary>
        /// <param name="context">入力コンテンツ</param>
        /// <returns></returns>
        public async Task IssueGet(IDialogContext context, IAwaitable<string> argument)
        {
            string repository = RedisCacheOperation.Connection.StringGet(GitHubDialog.channelId);

            //押下されたボタンのテキストを取得
            var text = await argument;

            //操作キャンセル確認メソッド
            await TryCatch(context, text, async () =>
            {
                //ユーザとリポジトリを用いてopenしているissueをすべて取得
                IReadOnlyList<Issue> issues = await GitHubDialog.github.Issue.GetAllForRepository(repository.Split('/')[0], repository.Split('/')[1]);

                //botメッセージ表示
                await context.PostAsync("issueの一覧を表示します");

                #region 【条件式】押下されたボタンが～なら
                switch (text)
                {
                    //押下されたボタンが「すべて」
                    case "すべて":
                        {
                            //issueの情報全表示メソッド
                            await IssueDisplay(context, issues);
                            break;
                        }
                    case "No,Titleのみ":
                        {
                            //issueのタイトル、Noのみ表示メソッド
                            await IssueDisplayOnly(context, issues);
                            break;
                        }
                }
                #endregion 【条件式】押下されたボタンが～なら

                //issue表示操作を終了する
                context.Done<object>(context);
            });
        }
        #endregion issue表示

        #region issue表示3-b（No,Titleのみ）
        /// <summary>
        /// issue表示（本体）
        /// </summary>
        /// <param name="context"></param>
        /// <param name="issues"></param>
        /// <returns></returns>
        public async Task IssueDisplayOnly(IDialogContext context, IReadOnlyList<Issue> issues)
        {
            //1メッセージで情報すべてを送信するための変数
            string hyper = string.Empty;

            //各issueの情報を表示
            foreach (Issue issue in issues)
            {
                if (issue.Number < 10) { hyper += "<" + issue.HtmlUrl + "|" + "#  " + issue.Number.ToString() + "      " + issue.Title + ">" + Environment.NewLine; }
                else { hyper += "<" + issue.HtmlUrl + "|" + "# " + issue.Number.ToString() + "      " + issue.Title + ">" + Environment.NewLine; }
            }

            //引用段落記法でbot送信する
            await context.PostAsync(ConvertToSlackFormat(hyper, "```"));
        }
        #endregion issue表示
    }
}