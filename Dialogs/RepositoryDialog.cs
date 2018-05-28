using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Sample.SimpleEchoBot;
using Microsoft.WindowsAzure.Storage.Table;
using SimpleEchoBot.AzureTableStorage;
using SimpleEchoBot.Model;
using SimpleEchoBot.RedisCache;

namespace SimpleEchoBot.Dialogs
{
    [Serializable]
    public class RepositoryDialog : BaseDialog, IDialog<object>
    {
        #region リポジトリ名入力
        /// <summary>
        /// リポジトリ名入力
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task StartAsync(IDialogContext context)
        {
            //ユーザ名/リポジトリ名を入力するようメッセージをbot送信する
            await context.PostAsync("ユーザ名/リポジトリ名を入力してください");

            //リポジトリ変更待ち
            context.Wait(RepositorySet);
        }
        #endregion

        #region repository変更
        /// <summary>
        /// repository変更
        /// </summary>
        /// <param name="context"></param>
        /// <param name="repositoryUser"></param>
        /// <param name="repositoryName"></param>
        /// <returns></returns>
        public async Task RepositorySet(IDialogContext context, string repository)
        {
            RedisCacheOperation.Connection.StringSet(GitHubDialog.channelId, repository);

            //リポジトリの変更が完了した旨をメッセージでbot送信
            await context.PostAsync("リポジトリの変更が完了しました");

            //リポジトリへのリンクをハイパーリンクとして生成し、bot送信
            await context.PostAsync("<" + "http://github.com/" + repository + "|" + repository + ">");

            //リポジトリ変更操作を終了する
            context.Done<object>(context);
        }
        #endregion

        #region repository変更（ガイド）
        /// <summary>
        /// repository変更(ガイド)
        /// </summary>
        /// <param name="context"></param>
        /// <param name="argument"></param>
        /// <returns></returns>
        public async Task RepositorySet(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            //入力メッセージを格納
            var message = await argument;

            //'/'で文字列分割し、リストに格納
            string repository = message.Text;

            RedisCacheOperation.Connection.StringSet(GitHubDialog.channelId, repository);

            //操作キャンセル確認メソッド
            await TryCatch(context, message.Text, async () =>
            {
                //リポジトリの変更が完了した旨をメッセージでbot送信
                await context.PostAsync("リポジトリの変更が完了しました");

                //リポジトリへのリンクをハイパーリンクとして生成し、bot送信
                await context.PostAsync("<" + "http://github.com/" + repository + "|" + repository + ">");

                //リポジトリ変更操作を終了する
                context.Done<object>(context);
            });

        }
        #endregion repository変更
    }
}