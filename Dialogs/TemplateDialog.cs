using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Sample.SimpleEchoBot;
using Microsoft.WindowsAzure.Storage.Table;
using SimpleEchoBot.AzureTableStorage;
using SimpleEchoBot.Model;
using System.Configuration;
using System.Linq;
using SimpleEchoBot.RedisCache;

namespace SimpleEchoBot.Dialogs
{
    [Serializable]
    public class TemplateDialog : BaseDialog, IDialog<object>
    {
        private EntityOperation<TemplateEntity> entityOperation = new EntityOperation<TemplateEntity>();

        #region 【変数】ストレージ操作変数
        /// <summary>
        /// ストレージ操作変数
        /// </summary>
        private static StorageOperation storageOperation = new StorageOperation();
        #endregion

        #region 【変数】選択ラベル
        /// <summary>
        /// 選択ラベル
        /// </summary>
        private string label = string.Empty;
        #endregion


        #region テンプレート作成・編集（作成・編集選択）
        /// <summary>
        /// 開始メソッド
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task StartAsync(IDialogContext context)
        {
            PromptDialog.Choice(context, TemplateMethod, new List<string> { "作成", "編集" }, "リストから選んでください");
        }
        #endregion

        #region テンプレート作成・編集（ラベル入力）
        /// <summary>
        /// テンプレート作成・編集(作成か編集か)
        /// </summary>
        /// <param name="context"></param>
        /// <param name="argument"></param>
        /// <returns></returns>
        public async Task TemplateMethod(IDialogContext context, IAwaitable<string> argument)
        {
            var way = await argument;

            //現在チャンネルのリポジトリ取得
            string repository = RedisCacheOperation.Connection.StringGet(GitHubDialog.channelId);

            //対象リポジトリのラベルをすべて取得
            var labelList = await GitHubDialog.github.Issue.Labels.GetAllForRepository(repository.Split('/')[0], repository.Split('/')[1]);

            //ラベルリストからラベル名のリストを生成
            var labelNameList_project = labelList.ToList().ConvertAll(x => x.Name);

            //RowKeyがlabelのEntityを取得するクエリ
            TableQuery<TemplateEntity> query = new TableQuery<TemplateEntity>().Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, "label"));

            //Entityリストから
            List<string> labelNameList_table = StorageOperation.GetTableIfNotExistsCreate(string.Empty).ExecuteQuery(query).ToList().ConvertAll(x => x.PartitionKey);

            switch (way)
            {
                case "作成":
                    {
                        var labelNameList = labelNameList_project.Except(labelNameList_table).ToList();
                        

                        if(labelNameList.Count != 0)
                        {
                            labelNameList.Add("キャンセルする");
                            PromptDialog.Choice(context, this.TemplateCreateInput, labelNameList, "作成したいテンプレートのラベルを選択してください");
                        }                         
                        else
                        {
                            await context.PostAsync("テンプレートが未作成のラベルはありません");
                            context.Done<object>(context);
                        }
                        break;
                    }
                case "編集":
                    {
                        
                        if (labelNameList_table.Count != 0)
                        {
                            labelNameList_table.Add("キャンセルする");
                            PromptDialog.Choice(context, this.TemplateEditInput, labelNameList_table, "編集したいラベルを選択してください");
                        }
                        else
                        {
                            await context.PostAsync("テンプレートが作成済のラベルはありません");
                            context.Done<object>(context);
                        }
                        break;

                        
                    }
            }
        }
        #endregion

        #region テンプレート作成（テンプレート入力）
        /// <summary>
        /// テンプレート作成（テンプレート入力）
        /// </summary>
        /// <param name="context"></param>
        /// <param name="argument"></param>
        /// <returns></returns>
        public async Task TemplateCreateInput(IDialogContext context, IAwaitable<string> argument)
        {
            label = await argument;
            
            await TryCatch(context, label, async () =>
            {
                await context.PostAsync("テンプレートを入力してください");
                context.Wait(TemplateCreate);
            });
        }
        #endregion

        #region テンプレート編集（テンプレート入力）
        /// <summary>
        /// テンプレート編集（テンプレート入力）
        /// </summary>
        /// <param name="context"></param>
        /// <param name="argument"></param>
        /// <returns></returns>
        public async Task TemplateEditInput(IDialogContext context, IAwaitable<string> argument)
        {
            label = await argument;

            await TryCatch(context, label, async () =>
            {
                await context.PostAsync("テンプレートを入力してください");
                context.Wait(TemplateEdit);
            });
        }
        #endregion

        #region テンプレート作成（作成本体）
        /// <summary>
        /// テンプレート作成（作成本体）
        /// </summary>
        /// <param name="context"></param>
        /// <param name="argument"></param>
        /// <returns></returns>
        public async Task TemplateCreate(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument;
            var temp = message.Text;

            //キャンセル操作が行われたら
            await TryCatch(context, temp, async () =>
            {
                //リストから指定したラベルを基にテンプレートエンティティを作成
                TemplateEntity templateEntity = new TemplateEntity(label, temp);

                //エンティティ追加実行
                TableResult insertResult = entityOperation.InsertEntityResult(templateEntity, string.Empty);

                if (insertResult != null) await context.PostAsync("テンプレートを作成しました");
                else                      await context.PostAsync("テンプレートの作成に失敗しました");
            });
            
            context.Done<object>(context);
        }
        #endregion

        #region テンプレート編集（編集本体）
        /// <summary>
        /// テンプレート編集（編集本体）
        /// </summary>
        /// <param name="context"></param>
        /// <param name="argument"></param>
        /// <returns></returns>
        public async Task TemplateEdit(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument;
            var temp = message.Text;

            //キャンセル操作が行われたら
            await TryCatch(context, temp, async () =>
            {
                //リストから指定したラベルを基にテンプレートエンティティを作成
                TemplateEntity template = new TemplateEntity(label, temp);

                //Eitityで上書きする
                TemplateEntity entity = entityOperation.UpdateEntityResult(template, string.Empty).Result as TemplateEntity;

                //上書き後の返り値がnullかどうか
                if (entity != null) await context.PostAsync("テンプレートを編集しました");   
                else                await context.PostAsync("テンプレートの編集に失敗しました");  
            });
            

            context.Done<object>(context);
        }
        #endregion
    }


}