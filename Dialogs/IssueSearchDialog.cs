using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Sample.SimpleEchoBot;
using Octokit;
using SimpleEchoBot.RedisCache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleEchoBot.Dialogs
{
    [Serializable]
    public class IssueSearchDialog : BaseDialog, IDialog<object>
    {
        #region 【リスト】issue検索リスト
        /// <summary>
        /// issue検索リスト
        /// </summary>
        private static List<string> searchList = new List<string> { "番号",
                                                                    "作成ユーザ",
                                                                    "タイトル",
                                                                    "ラベル",
                                                                    "キャンセル"};
        #endregion

        #region 【変数】issue検索方法
        /// <summary>
        /// 検索方法
        /// </summary>
        private string search;
        #endregion

        #region 【変数】基本操作用変数
        /// <summary>
        /// 基本操作用変数
        /// </summary>
        GitHubDialog dialog = new GitHubDialog();
        #endregion

        string repository = RedisCacheOperation.Connection.StringGet(GitHubDialog.channelId);


        #region issue検索1（検索方法選択）
        /// <summary>
        /// 検索方法選択
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task StartAsync(IDialogContext context)
        {
            //検索方法を検索方法リストから選択させる
            PromptDialog.Choice(context, this.IssueSearch, searchList, "検索方法を選んでください");
        }
        #endregion

        #region issue検索2（検索方法）
        /// <summary>
        /// issue検索
        /// </summary>
        /// <param name="context"></param>
        /// <param name="argument"></param>
        /// <returns></returns>
        public async Task IssueSearch(IDialogContext context, IAwaitable<string> argument)
        {
            //入力メッセージをメンバ変数[search]に格納
            var message = await argument;
            search = message;

            //操作キャンセル確認メソッド
            await dialog.TryCatch(context, search, async () =>
            {
                //検索内容を入力するよう要求するメッセージをbot送信
                await context.PostAsync("検索内容を入力してください");

                //issue検索処理待ち
                context.Wait(IssueSearch);
            });
        }
        #endregion issue検索

        #region issue検索3（検索内容）
        /// <summary>
        /// issue検索
        /// </summary>
        /// <returns></returns>
        public async Task IssueSearch(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            //入力メッセージを格納
            var message = await argument;
            var text = message.Text;

            //検索issue格納用リスト
            List<Issue> issueListTemp = new List<Issue>();

            //issue一覧表示の引数用リスト
            IReadOnlyList<Issue> issueList = null;

            //操作キャンセル確認メソッド
            
                #region 操作一覧選択
                switch (search)
                {
                    case "番号":
                        {
                            //現在のリポジトリからopen済みissueを全取得
                            IReadOnlyList<Issue> issues = await GitHubDialog.github.Issue.GetAllForRepository(repository.Split('/')[0], repository.Split('/')[1]);

                            //入力されたメッセージを数値化
                            int num = Convert.ToInt32(text);

                            //入力されたメッセージの数値と同じissueを取得
                            issueListTemp = issues.ToList().Where(x => x.Number == num) as List<Issue>;

                            //issue一覧表示をするためにリストの形式をreadonlyに変化
                            issueList = issueListTemp;

                            break;
                        }

                    case "作成ユーザ":
                        {
                            //issue検索用変数を用意し、メンバ変数「作成者」に入力メッセージを格納する
                            var recently = new RepositoryIssueRequest
                            {
                                Creator = text
                            };

                            //現在のリポジトリからissue検索用変数に添ったissueを全取得
                            issueList = await GitHubDialog.github.Issue.GetAllForRepository(repository.Split('/')[0], repository.Split('/')[1], recently);

                            break;
                        }
                    case "タイトル":
                        {
                            //現在のリポジトリからopen済みissueを全取得
                            IReadOnlyList<Issue> issues = await GitHubDialog.github.Issue.GetAllForRepository(repository.Split('/')[0], repository.Split('/')[1]);

                            //入力メッセージがタイトルと部分一致するissueを取得
                            issueListTemp = issues.ToList().Where(x => x.Title.Contains(text)) as List<Issue>;

                            //issue一覧表示をするためにリストの形式をreadonlyに変化
                            issueList = issueListTemp;

                            break;
                        }
                    case "ラベル":
                        {
                            List<string> labels = new List<string> { text };
                            issueList = await GitHubDialog.convinient.GetIssuesForLabel(labels);

                            break;
                        }
                }
                #endregion

                //issueの一覧を表示する旨をメッセージでbot送信
                await context.PostAsync("issueの一覧を表示します");

                //issue一覧表示処理待ち
                await IssueDisplay(context, issueList);

                //issue検索操作を終了する
                context.Done<object>(context);

           



        }
        #endregion issue検索

    }
}