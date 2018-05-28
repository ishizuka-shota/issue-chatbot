using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Sample.SimpleEchoBot;
using Octokit;
using SimpleEchoBot.Model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleEchoBot.Dialogs
{
    [Serializable]
    public class BaseDialog : IDialog<object>
    {

        #region 初期処理(使わない)
        /// <summary>
        /// 初期処理(使わない)
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        Task IDialog<object>.StartAsync(IDialogContext context)
        {
            throw new NotImplementedException();
        }
        #endregion


        #region 【変数】基本操作用変数
        /// <summary>
        /// 基本操作用変数
        /// </summary>
        GitHubDialog gitHubDialog = new GitHubDialog();
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
            using (WebClient webClient = SlackApi.SlackApi.CreateHeader_Post())
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
                context.Wait(gitHubDialog.MessageReceivedAsync);
            }
            //ユーザ情報不正時の処理
            catch (AuthorizationException)
            {
                //ユーザ情報が不正というメッセージを出す
                await context.PostAsync("不正なユーザ情報です");
                //メッセージ送信待機
                context.Wait(gitHubDialog.MessageReceivedAsync);
            }
            //それ以外の例外処理
            catch (Exception)
            {
                //ユーザ情報が不正というメッセージを出す
                await context.PostAsync("不正な処理が発生しました");
                //メッセージ送信待機
                context.Wait(gitHubDialog.MessageReceivedAsync);
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
}