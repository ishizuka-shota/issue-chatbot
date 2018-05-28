using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.WindowsAzure.Storage.Table;
using Octokit;
using SimpleEchoBot.AzureTableStorage;
using SimpleEchoBot.Dialogs;
using SimpleEchoBot.Model;
using SimpleEchoBot.RedisCache;
using SimpleEchoBot.SlackApi;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Sample.SimpleEchoBot
{
    [Serializable]
    public class GitHubDialog : IDialog<object>
    {
        private EntityOperation<TemplateEntity> entityOperation_Template = new EntityOperation<TemplateEntity>();

        #region 【メソッド集】便利メソッド集
        /// <summary>
        /// 便利メソッド集
        /// </summary>
        public static Convenient convinient = new Convenient();
        #endregion

        #region 【変数】アクセストークン認証
        /// <summary>
        /// アクセストークンを用いた認証
        /// </summary>
        public static GitHubClient github = new GitHubClient(new ProductHeaderValue("SimpleEchoBot"));
        #endregion

        #region 【リスト】操作リスト
        /// <summary>
        /// 操作リスト
        /// </summary>
        private static List<string> menuList = new List<string>();
        #endregion

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

        #region 【変数】チャンネル名
        /// <summary>
        /// チャンネル名
        /// </summary>
        public static string channelName = string.Empty;
        #endregion

        #region 【変数】チャンネルID
        /// <summary>
        /// チャンネルID
        /// </summary>
        public static string channelId = string.Empty;
        #endregion

        #region 【変数】issueの情報
        /// <summary>
        /// issue（task）の情報
        /// </summary>
        public IssueModel issueModel = new IssueModel();
        #endregion

        #region【メソッド】操作一覧表示メソッド
        /// <summary>
        /// 操作一覧を表示し、ユーザの入力を待つ
        /// </summary>
        /// <param name="context"></param>
        public void CALL_GUIDE(IDialogContext context)
        {
            string repository = RedisCacheOperation.Connection.StringGet(channelId);
            string text = string.Empty;

            //リポジトリが設定していなかったら
            if (string.IsNullOrEmpty(repository))
            {
                //リポジトリ設定のみを選択できるようにする
                menuList = new List<string> { "リポジトリ設定" };
                text = "リポジトリを設定してください";
            }
            else
            {
                //通常通りの選択肢
                menuList = new List<string> { "issue一覧表示", "issue作成", "リポジトリ設定", "ユーザ設定","issue検索",　"出力",　"テンプレート作成・編集",　"ログイン", "操作終了"};
                text = "リポジトリ : " + "<" + "http://github.com/" + repository + "|" + repository + ">" + Environment.NewLine + "一覧の中から操作を選んでください";
            }

            //ガイドメニュー作成
            var guideMenu = CreateMenu(text, menuList);

            // メッセージ送信SlackAPIを叩き、ガイドメニューを表示
            PostMessageAtSlackApi(guideMenu);

            // ガイド呼び出し
            context.Wait(Guide);

        }
        #endregion

        #region 開始メソッド
        /// <summary>
        /// 開始メソッドだ
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }
        #endregion 開始メソッド

        public static Connector.Activity activity = new Connector.Activity();
  
        #region 入力に対して応答を返す
        /// <summary>
        /// 入力に対して応答を返す
        /// </summary>
        /// <param name="context">入力コンテンツ</param>
        /// <param name="argument">入力文字列</param>
        /// <returns></returns>
        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {

            //入力文字列取得
            var message = await argument;

            await TryCatch(context, message.Text, async () =>
            {

                activity = message as Connector.Activity;

                //チャンネル名を取得
                channelName = message.Conversation.Name;

                //チャンネルIDを取得
                channelId = message.Conversation.Id.Split(':')[2];

                //アクセストークンセット
                SetCredencial(message.From.Id, message.From.Name, github);

                //行数毎の文字列リストを生成
                List<string> rowList = new List<string>();

                //行毎に文字列分割し、リストに格納
                if (!string.IsNullOrEmpty(message.Text))
                {
                    string[] temp = message.Text.Split('\n');
                    rowList.AddRange(temp);
                }

                #region 【条件式】1行目が～なら

                //リポジトリ設定(1行目がset ripository)
                if (String.Compare(rowList[0], "set repository", true) == 0)
                {
                    RepositoryDialog repositoryDialog = new RepositoryDialog();

                    //リポジトリ変更メソッド呼び出し
                    await repositoryDialog.RepositorySet(context, rowList[1]);
                    context.Wait(MessageReceivedAsync);
                }

                //ユーザ設定（1行目がset user）
                if (String.Compare(rowList[0], "set user", true) == 0)
                {
                    //認証本体
                    context.Wait(MessageReceivedAsync);
                }

                //操作一覧（1行目がcall）
                if (String.Compare(rowList[0], "call", true) == 0)
                {
                    //操作一覧を出す
                    CALL_GUIDE(context);
                }

                //issueの簡易作成（1行目がラベル名）
                if (rowList[0].IsAny("bug", "question", "task", "課題", "運用", "保守", "バグ") && !string.IsNullOrEmpty(rowList[1]))
                {
                    //作成完了のメッセージをbot送信する
                    await context.PostAsync("issueの作成が完了しました");

                    //引数のラベルでEntityを検索する
                    TemplateEntity entity = entityOperation_Template.RetrieveEntityResult(rowList[0], "label", string.Empty).Result as TemplateEntity;

                    //ラベルのテンプレートが未設定だったら
                    if (entity == null)
                    {
                        //テンプレートが未設定の旨をメッセージで表示
                        await context.PostAsync("テンプレートが未設定です。テンプレートを設定してください");
                        //入力待機
                        context.Wait(MessageReceivedAsync);
                    }
                    else
                    {
                        //issue作成実行用クラス
                        IssueCreateDialog createDialog = new IssueCreateDialog();

                        //2行目以降、1行ずつでループ処理
                        for (int i = 1; i < rowList.Count; i++)
                        {
                            //指定行の2文字目からタイトルとして格納（1文字目は・のため）
                            issueModel.Title = rowList[i].Substring(1);
                            //ラベル格納
                            issueModel.Labels = rowList[0];
                            //本文格納（本文は入力されないためテンプレートのみ）
                            issueModel.Body = entity.Template;
                            //issue作成
                            await createDialog.IssueCreate(context);
                        }
                        //メッセージ待機状態に戻る
                        context.Wait(MessageReceivedAsync);
                    }


                }
                #endregion 【条件式】1行目が～なら

            });

            
        }
        #endregion 入力に対して応答を返す


        #region 操作ガイド
        /// <summary>
        /// 操作ガイド
        /// </summary>
        /// <param name="context"></param>
        /// <param name="argument"></param>
        /// <returns></returns>
        public async Task Guide(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            //入力メッセージを取得
            var message = await argument;

            var text = message.Text;

            //操作キャンセル確認メソッド
            await TryCatch(context, text, async () =>
            {
                switch (text)
                {
                    case "issue一覧表示":
                        {
                            //issue表示Dialogへ遷移し、処理を行う
                            //処理終了後はResumeAfterへ遷移する
                            context.Call(new IssueDisplayDialog(), ResumeAfter);
                            break;
                        }
                    case "issue作成":
                        {
                            //issue作成Dialogへ遷移し、処理を行う
                            //処理終了後はResumeAfterへ遷移する
                            context.Call(new IssueCreateDialog(), ResumeAfter);
                            break;
                        }
                    case "リポジトリ設定":
                        {
                            //リポジトリ変更Dialogへ遷移し、処理を行う
                            //処理終了後はResumeAfterへ遷移する
                            context.Call(new RepositoryDialog(), ResumeAfter);
                            break;
                        }
                    case "ユーザ設定":
                        {
                            //ユーザ変更Dialogへ遷移し、処理を行う
                            //処理終了後はResumeAfterへ遷移する
                            context.Call(new UserDialog(), ResumeAfter);
                            break;
                        }
                    case "issue検索":
                        {
                            //issue検索Dialogへ遷移し、処理を行う
                            //処理終了後はResumeAfterへ遷移する
                            context.Call(new IssueSearchDialog(), ResumeAfter);
                            break;
                        }
                    #region 一旦なし
                    //case "issueClose":
                    //    {
                    //        await context.PostAsync("closeするissue番号を入力してください");
                    //        context.Wait(IssueClose);
                    //        break;
                    //    }
                    #endregion
                    case "出力":
                        {
                            //出力Dialogへ遷移し、処理を行う
                            //処理終了後はResumeAfterへ遷移する
                            context.Call(new OutputDialog(), ResumeAfter);
                            break;
                        }
                    case "テンプレート作成・編集":
                        {
                            //テンプレート作成・編集Dialogへ遷移し、処理を行う
                            //処理終了後はResumeAfterへ遷移する
                            context.Call(new TemplateDialog(), ResumeAfter);
                            break;
                        }
                    case "ログイン":
                        {
                            //テンプレート作成・編集Dialogへ遷移し、処理を行う
                            //処理終了後はResumeAfterNotGuideへ遷移する
                            context.Call(new LoginDialog(), ResumeAfterNotGuide);
                            break;
                        }
                    case "操作終了":
                        {
                            //操作ガイドを終了する旨のメッセージをbot送信する
                            await context.PostAsync("操作ガイドを終了しました");
                            //メッセージ送信待機
                            context.Wait(MessageReceivedAsync);
                            break;
                        }
                }
            });
        }
        #endregion 操作ガイド


        #region GitHubクライアントにアクセストークンセット
        /// <summary>
        /// GitHubクライアントにアクセストークンセット
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="userName"></param>
        /// <param name="gitHub"></param>
        /// <returns></returns>
        public GitHubClient SetCredencial(string userId, string userName, GitHubClient gitHub)
        {
            // クレデンシャル情報に適当な値を入れ、認証エラーが起きるようにする
            github.Credentials = new Credentials("aaaaaaaaaaaaaa");

            EntityOperation<UserEntity> entityOperation = new EntityOperation<UserEntity>();

            //検索操作を行う変数を生成
            TableOperation retrieveOperation = TableOperation.Retrieve<UserEntity>(userId, userName);

            //RowKeyがlabelのEntityを取得するクエリ
            TableQuery<UserEntity> query = new TableQuery<UserEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, userId));

            //Entityリストから
            var entityList = StorageOperation.GetTableIfNotExistsCreate("user").ExecuteQuery(query);

            // Entityリストが空じゃなかったら
            if(entityList.Count() != 0)
            {
                // Entityリストをリスト形式に直す
                List<UserEntity> userEntityList = entityList.ToList();

                // Entityは一つしか取得されないため、リストの0番目を取得する
                UserEntity userEntity = userEntityList[0];

                // Entityのクレデンシャル情報をセットする
                github.Credentials = new Credentials(userEntity.AccessToken);

            }
            return github;
        }
        #endregion

        
        #region 各Dialog終了後操作
        /// <summary>
        /// 各Dialog終了後操作
        /// </summary>
        /// <param name="context"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private async Task ResumeAfter(IDialogContext context, IAwaitable<object> result)
        {
            //操作一覧へ戻る
            CALL_GUIDE(context);
        }
        #endregion

        #region 各Dialog終了後操作(ガイド不要)
        /// <summary>
        /// 各Dialog終了後操作
        /// </summary>
        /// <param name="context"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private async Task ResumeAfterNotGuide(IDialogContext context, IAwaitable<object> result)
        {
            //メッセージ待機状態に戻る
            context.Wait(MessageReceivedAsync);
        }
        #endregion

        #region 【デリゲート】キャンセルがされうる処理
        /// <summary>
        /// キャンセルがされうる処理
        /// </summary>
        /// <returns></returns>
        public delegate Task Canceler();
        #endregion


        #region メニュー作成(SlackApi用Json)
        /// <summary>
        /// メニュー作成(SlackApi用Json)
        /// </summary>
        /// <param name="menuList"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        protected SlackModel<MenusActionsModel> CreateMenu(String text, List<string> menuList)
        {
            return new SlackModel<MenusActionsModel>()
            {
                Text = text,
                Attachments = new List<AttachmentsModel<MenusActionsModel>>()
                        {
                            new AttachmentsModel<MenusActionsModel>()
                            {
                                Callback_id = "menu",
                                Actions = new List<MenusActionsModel>()
                                {
                                    new MenusActionsModel(menuList)
                                    {
                                        Name = "menuList",
                                        Text = "操作を選択",
                                    }
                                }
                            }
                        }

            };
        }
        #endregion


        #region SlackApiによるメッセージ送信
        /// <summary>
        /// SlackApiによるメッセージ送信
        /// </summary>
        /// <param name="text"></param>
        /// <param name="menuList"></param>
        /// <returns></returns>
        protected string PostMessageAtSlackApi<T>(SlackModel<T> slackModel)
        {
            using (WebClient webClient = SlackApi.CreateHeader_Post())
            {
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(slackModel);

                return webClient.UploadString(new Uri("https://slack.com/api/chat.postMessage"), json);
            }
        }
        #endregion


        #region slack形式コンバーター（完成・変更不要）
        /// <summary>
        /// 入力文字列の改行をslackで機能するように調整する
        /// </summary>
        /// <param name="text"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        protected string ConvertToSlackFormat(string text, string format)
        {
            #region 【条件式】slack表示形式
            switch (format)
            {
                //引用
                case ">>>":
                    {
                        return ">>>" + Environment.NewLine + text;
                    }
                //引用段落記法
                case "```":
                    {
                        return "```" + Environment.NewLine + text + Environment.NewLine + "```";
                    }
                default:
                    {
                        return text;
                    }

            }
            #endregion
        }
        #endregion slack形式コンバーター


        #region issue表示（すべて）
        /// <summary>
        /// issue表示（本体）
        /// </summary>
        /// <param name="context"></param>
        /// <param name="issues"></param>
        /// <returns></returns>
        public async Task IssueDisplay(IDialogContext context, IReadOnlyList<Issue> issues)
        {
            //本来は1メッセージでissue情報の一覧を表示したかった
            string display = string.Empty;

            //各issueの情報を表示
            foreach (Issue issue in issues)
            {               //左揃え10文字
                var text = string.Format("{0,-10}", "No") + ": " + issue.Number + Environment.NewLine +
                            string.Format("{0,-10}", "Date") + ": " + issue.CreatedAt + Environment.NewLine +
                            string.Format("{0,-10}", "User") + ": " + issue.User.Login + Environment.NewLine +
                            string.Format("{0,-10}", "Title") + ": " + issue.Title + Environment.NewLine +
                            string.Format("{0,-10}", "Body") + ": ";

                //本文のインデント揃え
                if (!string.IsNullOrEmpty(issue.Body))
                {
                    int i = 0;
                    foreach (string part in issue.Body.Split('\n'))
                    {
                        if (i == 0) { text += part; }
                        else { text += "            " + part; }
                        text += Environment.NewLine;
                        i++;
                    }
                }

                text += Environment.NewLine +
                        string.Format("{0,-10}", "Labels") + ": " + string.Join(" , ", issue.Labels.ToList().ConvertAll(x => x.Name)) + Environment.NewLine +
                        string.Format("{0,-10}", "HyperLink") + ": " + "<" + issue.HtmlUrl + "|" + "# " + issue.Number + "  " + issue.Title + ">";

                //引用段落記法でbot送信する
                await context.PostAsync(ConvertToSlackFormat(text, "```"));
            }
        }
        #endregion issue表示


        #region 操作キャンセル
        /// <summary>
        /// 操作キャンセル
        /// </summary>
        /// <param name="context"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public async Task TryCatch(IDialogContext context, string text, Canceler cancel)
        {
            //キャンセルを判断する変数
            CancellationTokenSource source = new CancellationTokenSource();

            try
            {
                //キャンセルが入力されたら
                if (text.IsAny("キャンセル", "exit", "キャンセルする"))
                {
                    //キャンセル変数にキャンセルを格納する
                    source.Cancel();
                    //これ以降非同期処理が行われたらOperationCanceledExceptionをthrowするようにする
                    source.Token.ThrowIfCancellationRequested();
                }
                //なにかしらの処理
                await cancel();
            }
            //操作キャンセル時の処理
            catch (OperationCanceledException)
            {
                //操作がキャンセルされたというメッセージを出す
                await context.PostAsync("操作は取り消されました");
                //メッセージ送信待機
                context.Wait(MessageReceivedAsync);
            }
            //ユーザ情報不正時の処理
            catch (AuthorizationException)
            {
                //ユーザ情報が不正というメッセージを出す
                await context.PostAsync("不正なユーザ情報です");
                //メッセージ送信待機
                context.Wait(MessageReceivedAsync);
            }
            //それ以外の例外処理
            catch (Exception)
            {
                //ユーザ情報が不正というメッセージを出す
                await context.PostAsync("不正な処理が発生しました");
                //メッセージ送信待機
                context.Wait(MessageReceivedAsync);
            }
        }
        #endregion 操作キャンセル


        #region slackファイルアップロードAPI
        /// <summary>
        /// slackファイルアップロードAPI
        /// </summary>
        /// <param name="url"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public byte[] fileUpload(string url, string filePath)
        {
            //API送信用ウェブクライアント
            WebClient wc = new WebClient();

            //ヘッダにContent-TypeとWeb.configにある認証トークンを加える
            var token_type = "Bearer";
            var token = ConfigurationManager.AppSettings["botToken"];
            wc.Headers.Add("Authorization", token_type + " " + token);

            //データを送信し、また受信する
            return wc.UploadFile(url, filePath);
        }
        #endregion slackファイルアップロードAPI

    }

    #region string型拡張（完成・今後も拡張の可能性有）
    /// <summary>
    /// string 型の拡張メソッドを管理するクラス
    /// </summary>
    public static partial class StringExtensions
    {
        /// <summary>
        /// 文字列が指定されたいずれかの文字列と等しいかどうかを返す
        /// </summary>
        public static bool IsAny(this string self, params string[] values)
        {
            return values.Any(c => c == self);
        }
    }
    #endregion string型拡張

}