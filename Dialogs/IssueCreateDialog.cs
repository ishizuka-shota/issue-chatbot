using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Sample.SimpleEchoBot;
using Octokit;
using SimpleEchoBot.AzureTableStorage;
using SimpleEchoBot.Model;
using SimpleEchoBot.RedisCache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleEchoBot.Dialogs
{
    [Serializable]
    public class IssueCreateDialog : BaseDialog, IDialog<object>
    {
        private EntityOperation<TemplateEntity> entityOperation_Template = new EntityOperation<TemplateEntity>();

        #region 【変数】issueの情報
        /// <summary>
        /// issue（task）の情報
        /// </summary>
        public IssueModel issueModel = new IssueModel();
        #endregion

        #region 【変数】issue作成時のissueID
        /// <summary>
        /// issue作成時のissueID
        /// </summary>
        private int targetIssueId = 0;
        #endregion

        #region 【変数】基本操作用変数
        /// <summary>
        /// 基本操作用変数
        /// </summary>
        GitHubDialog dialog = new GitHubDialog();
        #endregion

        #region【変数】現在チャンネルのリポジトリ
        /// <summary>
        /// 現在チャンネルのリポジトリ
        /// </summary>
        string repository = RedisCacheOperation.Connection.StringGet(GitHubDialog.channelId);
        #endregion

        #region issue作成1（ラベル選択）
        /// <summary>
        /// ラベル選択
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task StartAsync(IDialogContext context)
        {
            await TryCatch(context, "", async () =>
            {
                //現在のリポジトリからラベルのリストを取得
                var labelList = await GitHubDialog.github.Issue.Labels.GetAllForRepository(repository.Split('/')[0], repository.Split('/')[1]);

                //ラベルのリストからラベルの名前を各々取得
                List<string> labelNameList = labelList.ToList().ConvertAll(x => x.Name);

                //戻るボタン
                labelNameList.Add("キャンセルする");

                var labelMenu = CreateMenu("ラベルを一覧の中から選んでください", labelNameList);

                // メッセージ送信SlackApiを叩き、ラベルメニューを表示
                PostMessageAtSlackApi(labelMenu);

                //選択したラベルでテンプレート表示
                context.Wait(TemplateDisplay);
            });
        }
        #endregion

        #region issue作成2（テンプレート表示）
        /// <summary>
        /// テンプレート表示
        /// </summary>
        /// <param name="context"></param>
        /// <param name="argument"></param>
        /// <returns></returns>
        public async Task TemplateDisplay(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument;

            issueModel.Labels = message.Text;

            //操作キャンセル確認メソッド
            await dialog.TryCatch(context, issueModel.Labels, async () =>
            {
                //引数のラベルでEntityを検索する
                TemplateEntity entity = entityOperation_Template.RetrieveEntityResult(issueModel.Labels, "label", string.Empty).Result as TemplateEntity;

                //ラベルのテンプレートが未設定だったら
                if (entity == null)
                {
                    //テンプレートが未設定の旨をメッセージで表示
                    await context.PostAsync("テンプレートが未設定です。テンプレートを設定してください");

                    //issue作成操作を終了する
                    context.Done<object>(context);
                }
                else
                {
                    var input = "タイトル" + Environment.NewLine + entity.Template;

                    await context.PostAsync("下記のテンプレートを用いて本文を作成してください(タイトルのみも可)");
                    await context.PostAsync(ConvertToSlackFormat(input, "```"));

                    context.Wait(IssueCreate);
                }
            });

        }
        #endregion

        #region issue作成3（issueの中身入力）
        /// <summary>
        /// issue作成（ガイド）
        /// </summary>
        /// <param name="context"></param>
        /// <param name="argument"></param>
        /// <returns></returns>
        public async Task IssueCreate(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            //送信されたメッセージをissueモデルのBodyに格納
            var message = await argument;

            //Bodyを改行文字で分割し、リストに格納する
            List<string> templateList = new List<string>();
            templateList.AddRange(message.Text.Split('\n'));

            issueModel.Title = templateList[0];
            templateList.RemoveAt(0);
            issueModel.Body = string.Join("\n", templateList);

            
            //操作キャンセル確認メソッド
            await dialog.TryCatch(context, message.Text, async () =>
            {
                //issue作成処理待ち
                await IssueCreate(context);

                //issue作成タスク処理
                await context.PostAsync("issueの作成が完了しました");

                //作成したissueをプロジェクトに追加するかどうか尋ねる
                PromptDialog.Choice(context, this.TargetProject, new List<string> { "はい", "いいえ" }, "projectに追加しますか？");
            });
        }
        #endregion issue作成（ガイド）

        #region issue作成4（作成本体）
        /// <summary>
        /// issue作成本体
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task IssueCreate(IDialogContext context)
        {
            //issue作成
            var newIssue = new NewIssue(issueModel.Title)
            {
                Body = issueModel.Body
            };

            //ラベル追加
            if (!string.IsNullOrEmpty(issueModel.Labels))
            {
                foreach (string str in issueModel.Labels.Split(' '))
                {
                    newIssue.Labels.Add(str);
                }
            }
            //ユーザとリポジトリを用いてissueを登録
            var iss = await GitHubDialog.github.Issue.Create(repository.Split('/')[0], repository.Split('/')[1], newIssue);

            //作成したissueIDを格納
            targetIssueId = iss.Id;

            //作成したissueをリスト化
            var issue = new List<Issue>();
            issue.Add(iss);

            //issue表示処理待ち
            await IssueDisplay(context, issue);
        }
        #endregion issue作成

        #region issue作成5（プロジェクトへ追加するかどうか）
        /// <summary>
        /// projectに追加するかどうか
        /// </summary>
        /// <param name="context"></param>
        /// <param name="argument"></param>
        /// <returns></returns>
        public async Task TargetProject(IDialogContext context, IAwaitable<string> argument)
        {
            //送信されたメッセージを取得する
            var message = await argument;

            #region 【条件式】作成したissueをプロジェクトに追加するかどうか
            if (message.Equals("はい"))
            {
                //現在のリポジトリのプロジェクトをすべて取得する
                var pro = await GitHubDialog.github.Repository.Project.GetAllForRepository(repository.Split('/')[0], repository.Split('/')[1]);

                //プロジェクト名を格納する為のリスト
                List<string> proNameList = pro.ToList().ConvertAll(x => x.Name);

                //追加するプロジェクトをプロジェクト名リストから選択させる
                PromptDialog.Choice(context, ConfigProject, proNameList, "追加するprojectを選んでください");
            }
            else if (message.Equals("いいえ"))
            {
                //プロジェクトへの追加がキャンセルされた旨のメッセージをbot送信する
                await context.PostAsync("projectへの追加をキャンセルしました");
                //issue作成操作を終了する
                context.Done<object>(context);
            }
            #endregion
        }
        #endregion

        #region issue作成6（プロジェクト指定）
        /// <summary>
        /// project指定
        /// </summary>
        /// <param name="context"></param>
        /// <param name="argument"></param>
        /// <returns></returns>
        public async Task ConfigProject(IDialogContext context, IAwaitable<string> argument)
        {
            //入力されたメッセージをプロジェクト名として格納
            var projectName = await argument;

            //プロジェクトを再び取得する
            var proList = await GitHubDialog.github.Repository.Project.GetAllForRepository(repository.Split('/')[0], repository.Split('/')[1]);

            //入力されたプロジェクト名と名前が一致するものを取得
            Project pro = proList.ToList().First(x => x.Name.Equals(projectName));

            //プロジェクト追加処理待ち
            await IssueEntry(context, pro);

            //プロジェクトに追加された旨をメッセージでbot送信する
            await context.PostAsync("projectへの追加が完了しました");

            //issue作成操作を終了する
            context.Done<object>(context);
        }
        #endregion

        #region issue作成7（プロジェクトへ追加）
        /// <summary>
        /// projectへ追加
        /// </summary>
        /// <param name="context"></param>
        /// <param name="project"></param>
        /// <returns></returns>
        public async Task IssueEntry(IDialogContext context, Project project)
        {
            //対象プロジェクトのIDからカラムのリストを取得する
            var columnList = await GitHubDialog.github.Repository.Project.Column.GetAll(project.Id);

            //対象issueのIDからプロジェクトのカードを生成する
            NewProjectCard card = new NewProjectCard(targetIssueId, ProjectCardContentType.Issue);

            //カラムの1番目に生成したプロジェクトカードを挿入する
            var newCard = await GitHubDialog.github.Repository.Project.Card.Create(columnList[0].Id, card);
        }
        #endregion
    }
}