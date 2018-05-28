using Microsoft.Bot.Sample.SimpleEchoBot;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using SimpleEchoBot.Model;
using SimpleEchoBot.RedisCache;
using System;
using System.Configuration;
using System.Text.RegularExpressions;

namespace SimpleEchoBot.AzureTableStorage
{
    public class StorageOperation
    {
        #region Azure Storage Table接続
        /// <summary>
        /// Azure Storage Table接続
        /// </summary>
        public static CloudTable GetTableIfNotExistsCreate(string tableName)
        {
            string repository = RedisCacheOperation.Connection.StringGet(GitHubDialog.channelId);

            //Azure Storageアカウント認証
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["TableStorage"].ConnectionString);

            //アカウントからテーブルクライアントを取得
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            //テーブル名が空だったら
            if (String.IsNullOrEmpty(tableName))
            {
                //repository名から半角英数字を抜き出し、先頭にTをつけたものをテーブル名とする
                var regular = new Regex(@"[^0-9a-zA-Z]");
                tableName = "T" + regular.Replace(repository.Split('/')[1], "");
            }

            //テーブルクライアントからテーブルを選択
            CloudTable table = tableClient.GetTableReference(tableName);

            //テーブルがなかったらテーブル作成
            table.CreateIfNotExists();

            return table;

        }
        #endregion
    }
}