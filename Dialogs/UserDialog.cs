using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Sample.SimpleEchoBot;
using Octokit;

namespace SimpleEchoBot.Dialogs
{
    [Serializable]
    public class UserDialog : BaseDialog, IDialog<object>
    {
        #region ユーザ選択
        /// <summary>
        /// ユーザ選択
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task StartAsync(IDialogContext context)
        {
            try
            {
                //変更可能なユーザリスト生成
                var user = await GitHubDialog.github.User.Current();
                await context.PostAsync(user.HtmlUrl);
            }
            catch (AuthorizationException)
            {
                await context.PostAsync("ログインしていません。");
            }
          
            //ユーザ変更操作を終了する
            context.Done<object>(context);

        }
        #endregion

        #region ユーザ変更
        /// <summary>
        /// ユーザ変更
        /// </summary>
        /// <param name="context"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task UserSet(IDialogContext context, string name)
        {
            await TryCatch(context, name, async () =>
            {
                //入力がなされていなかったら
                if (name.Equals("")) await context.PostAsync("ユーザ名が未入力です");

                //AppSettingsにて、入力された値のKeyからValueを取得
                var value = ConfigurationManager.AppSettings[name];

                //AppSettingsにて、Key[accessToken]のValueを取得したValueで上書き
                ConfigurationManager.AppSettings.Set("accessToken", value);

                //ユーザの変更を知らせるメッセージをbot送信
                await context.PostAsync("ユーザの変更が完了しました");
            });
           
        }
        #endregion ユーザ変更

        #region ユーザ変更（ガイド）
        /// <summary>
        /// ユーザ変更(ガイド)
        /// </summary>
        /// <param name="context"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task UserSet(IDialogContext context, IAwaitable<string> argument)
        {
            //入力メッセージを取得
            var name = await argument;

            //操作キャンセル確認メソッド
            await TryCatch(context, name, async () =>
            {
                GitHubDialog dialog = new GitHubDialog();

                //AppSettingsにて、入力された値のKeyからValueを取得
                var value = ConfigurationManager.AppSettings[name];

                //AppSettingsにて、Key[accessToken]のValueを取得したValueで上書き
                ConfigurationManager.AppSettings.Set("accessToken", value);

                //ユーザの変更を知らせるメッセージをbot送信
                await context.PostAsync("ユーザの変更が完了しました");

                //ユーザ変更操作を終了する
                context.Done<object>(context);
            });
        }
        #endregion ユーザ変更     
    }
}