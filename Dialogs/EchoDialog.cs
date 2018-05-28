using System;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;

namespace Microsoft.Bot.Sample.SimpleEchoBot
{
    [Serializable]
    public class EchoDialog : IDialog<object>
    {

        public async Task StartAsync(IDialogContext context)
        {
            await context.PostAsync("ファイル入力を受け付けました");
            context.Wait(MessageReceivedAsync);
        }

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
            
            if (String.Compare(message.Text, "call", true) == 0)
            {
                
                await context.PostAsync("一覧の中から操作を選んでください");
               // PromptDialog.Choice(context, this.CallDialog, this.mainMenuList, "What do you want to do?");
                var text = "1. issue一覧表示\n" +
                           "2. issue登録\n" +
                           "3. リポジトリ設定\n" +
                           "4. ユーザ設定\n";
                await context.PostAsync(ConvertToSlackFormat(text, "```"));
                context.Wait(Choice);
            }
            if (String.Compare(message.Text, "text", true) == 0) {
                await context.PostAsync("一覧の中から操作を選んでください");
            }
            if (String.Compare(message.Text, "await", true) == 0) {
                await context.PostAsync("一覧の中から操作を選んでください");
            }
                   

            #region switch文
            //条件式 : 1行目が～なら
            //switch (row1)
            //{
            //    case "create issue" :
            //        {
            //            //issue登録メソッド呼び出し
            //            await IssueCreate(context, github, row2, row3);
            //            break;
            //        }
                 
            //    case "get issues" :
            //        {
            //            //issue一覧表示メソッド呼び出し
            //            await IssueGet(context, github);
            //            break;
            //        }
            //    case "set repository" :
            //        {
            //            //リポジトリ変更メソッド呼び出し
            //            await RepositorySet(context, row2.Split('/')[0], row2.Split('/')[1]);
            //            break;
            //        }
            //    case "set user" :
            //        {
            //            //認証本体
            //            await UserSet(context, row2);
            //            break;
            //        }
            //    case "search issue" :
            //        {
                        
            //            break;
            //        }
            //    case "call" :
            //        {
            //            await context.PostAsync("一覧の中から操作を選んでください");
            //            var text = "1. issue一覧表示\n" +
            //                       "2. issue登録\n" +
            //                       "3. リポジトリ設定\n" +
            //                       "4. ユーザ設定\n";
            //            await context.PostAsync(ConvertToSlackFormat(text, "```"));
            //            context.Wait(Choice);
            //            break;
            //        }

            //}    
            #endregion

            //context.Wait(MessageReceivedAsync);
        }
        #endregion 入力に対して応答を返す


        #region slack形式コンバーター
        /// <summary>
        /// 入力文字列の改行をslackで機能するように調整する
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public string ConvertToSlackFormat(string text, string format)
        {
            switch (format)
            {
                case ">>>":
                    {
                        return "&gt;&gt;&gt;\n" + text;
                    }
                case "```":
                    {
                        return "```\n" + text + "\n```";
                    }
                default:
                    {
                        return text;
                    }
                    
            }
        }
        #endregion slack形式コンバーター

        public virtual async Task Choice(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            //入力文字列取得
            var message = await argument;
            string text = message.Text;

            if (String.Compare(text, "1", true) == 0)
            {
                await context.PostAsync("1");
                context.Done<object>(null);
            }
            if (String.Compare(text, "2", true) == 0)
            {
                await context.PostAsync("2");
                context.Done<object>(null);
            }
            if (String.Compare(text, "3", true) == 0)
            {
                await context.PostAsync("3");
                context.Done<object>(null);
            }
            if (String.Compare(text, "4", true) == 0)
            {
                await context.PostAsync("4");
                context.Done<object>(null);

            }
           
        }
    }
}