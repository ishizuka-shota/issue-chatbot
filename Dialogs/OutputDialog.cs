using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Sample.SimpleEchoBot;
using Octokit;
using SimpleEchoBot.AzureTableStorage;
using SimpleEchoBot.Model;
using SimpleEchoBot.RedisCache;

namespace SimpleEchoBot.Dialogs
{
    [Serializable]
    public class OutputDialog : BaseDialog, IDialog<object>
    {
        private EntityOperation<TemplateEntity> entityOperation = new EntityOperation<TemplateEntity>();

        #region【定数】slackAPI用基本URL
        /// <summary>
        /// slackAPI用基本URL
        /// </summary>
        private static string baseUrl = "https://slack.com/api/";
        #endregion

        /// <summary>
        /// 現在チャンネルのリポジトリ
        /// </summary>
        string repository = RedisCacheOperation.Connection.StringGet(GitHubDialog.channelId);


        #region 出力1（出力形式選択）
        /// <summary>
        /// 出力形式選択
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task StartAsync(IDialogContext context)
        {
            //出力形式を出力形式リストから選択させる
            PromptDialog.Choice(context, this.ChoiceOutput, new List<string> { "issueリスト", "project" }, "出力する物を選んでください");
        }
        #endregion

        #region 出力2（出力形式毎の処理）
        /// <summary>
        /// 出力形式選択
        /// </summary>
        /// <param name="context"></param>
        /// <param name="argument"></param>
        /// <returns></returns>
        public async Task ChoiceOutput(IDialogContext context, IAwaitable<string> argument)
        {
            var text = await argument;

            switch (text)
            {
                case "issueリスト":
                    {
                        //それぞれのラベルのissueのリストをリストで保管
                        List<IReadOnlyList<Issue>> issueListForLabels = new List<IReadOnlyList<Issue>>();

                        //現在のリポジトリからラベルのリストを取得
                        var labelList = await GitHubDialog.github.Issue.Labels.GetAllForRepository(repository.Split('/')[0], repository.Split('/')[1]);

                        //ラベル毎の処理
                        foreach (var label in labelList)
                        {
                            //ステータス関係なくissueを取得するリクエスト
                            var recently = new RepositoryIssueRequest
                            {
                                State = ItemStateFilter.All
                            };
                            //取得するラベルを設定
                            recently.Labels.Add(label.Name);

                            //issueを取得
                            IReadOnlyList<Issue> issueList = await GitHubDialog.github.Issue.GetAllForRepository(repository.Split('/')[0], repository.Split('/')[1], recently);

                            //取得したissueリストをリストに格納
                            issueListForLabels.Add(issueList);
                        }
                        //issue出力処理待ち
                        await OutputIssue(context, issueListForLabels, labelList.ToList().ConvertAll(x => x.Name));
                        break;
                    }
                case "project":
                    {
                        //現在のリポジトリからプロジェクトをすべて取得する
                        var pro = await GitHubDialog.github.Repository.Project.GetAllForRepository(repository.Split('/')[0], repository.Split('/')[1]);

                        //プロジェクトリストからプロジェクト名リストを生成
                        List<string> proNameList = pro.ToList().ConvertAll(x => x.Name);

                        //プロジェクト出力処理待ち
                        await OutputProject(context, pro);
                        break;
                    }
            }

            //出力操作を終了する
            context.Done<object>(context);

        }
        #endregion

        #region 出力3（プロジェクトのExcel出力）
        /// <summary>
        /// project出力
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task OutputProject(IDialogContext context, IReadOnlyList<Project> projectList)
        {

            string filePath = @"D:\home\Excel\" + repository.Split('/')[1] + "(project).xlsx";

            await context.PostAsync("project「" + repository.Split('/')[1] + "(project)」を出力しています...");

            using (var book = new XLWorkbook(XLEventTracking.Disabled))
            {
                foreach (var project in projectList)
                {
                    var proColumnList = await GitHubDialog.github.Repository.Project.Column.GetAll(project.Id);

                    // エクセルファイルに書き込み
                    var ws = book.AddWorksheet(project.Name);

                    int row = 2;
                    int col = 2;

                    //ラベル別の列幅とフォントサイズの指定
                    ws = GitHubDialog.convinient.ColumnWidthAndFontSize(ws, "task");

                    ws.Cell(row, col).Value = "No";
                    col++;
                    ws.Cell(row, col).Value = "タイプ";
                    col++;
                    ws.Cell(row, col).Value = "内容";
                    col++;
                    ws.Cell(row, col).Value = "詳細";
                    col++;
                    ws.Cell(row, col).Value = "完了判断";
                    col++;
                    ws.Cell(row, col).Value = "備考";
                    col++;
                    ws.Cell(row, col).Value = "担当者";
                    col++;
                    ws.Cell(row, col).Value = "着手日";
                    col++;
                    ws.Cell(row, col).Value = "完了日";
                    col++;
                    ws.Cell(row, col).Value = "実施状況";

                    IXLCell headerCell = ws.Cell(row, 2);
                    IXLRange ExcelRange = ws.Range(ws.Cell(row, 2), ws.Cell(row, col));

                    // ExcelRange.Interior.ColorIndex = 15; // カラーパレットの色を指定する場合
                    ExcelRange.Style.Fill.BackgroundColor = XLColor.Olivine; // オリーブ色

                    row++;
                    col = 2;

                    foreach (var column in proColumnList)
                    {
                        var proCardList = await GitHubDialog.github.Repository.Project.Card.GetAll(column.Id);

                        foreach (var card in proCardList)
                        {
                            if (!string.IsNullOrEmpty(card.Note))
                            {
                                continue;
                            }

                            //取得したカードのコンテンツURLの末尾の数字を取得
                            int issueNumber = int.Parse(card.ContentUrl.Substring(card.ContentUrl.LastIndexOf('/') + 1));


                            //コンテンツURLの末尾の数字を用いてissueを取得
                            Issue issue = await GitHubDialog.github.Issue.Get(repository.Split('/')[0], repository.Split('/')[1], issueNumber);

                            ws.Cell(row, col).Value = issue.Number;
                            col++;
                            ws.Cell(row, col).Value = string.Join("", issue.Labels.ToList().ConvertAll(x => x.Name));
                            col++;
                            ws.Cell(row, col).Value = issue.Title;
                            col++;
                            string[] temp = issue.Body.Split(new string[] { "##" }, StringSplitOptions.RemoveEmptyEntries);
                            int index = temp[0].IndexOf('\n');
                            ws.Cell(row, col).Value = temp[0].Substring(index + 1);
                            col++;
                            index = temp[1].IndexOf('\n');
                            ws.Cell(row, col).Value = temp[1].Substring(index + 1);
                            col++;
                            index = temp[2].IndexOf('\n');
                            ws.Cell(row, col).Value = temp[2].Substring(index + 1);
                            col++;
                            if (issue.Assignees.Count != 0)
                            {
                                ws.Cell(row, col).Value = issue.Assignees[0].Login;
                            }
                            col++;
                            ws.Cell(row, col).Value = issue.CreatedAt.DateTime.ToString();
                            col++;
                            if (issue.UpdatedAt.HasValue)
                            {
                                ws.Cell(row, col).Value = ((DateTimeOffset)issue.UpdatedAt).DateTime.ToString();
                            }
                            col++;
                            ws.Cell(row, col).Value = column.Name;

                            if (proCardList.Last().Equals(card))
                            {
                                IXLRange range = ws.Range(headerCell, ws.Cell(row, col));
                                range.Style
                                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                                    .Border.SetInsideBorder(XLBorderStyleValues.Hair)
                                    .Border.SetLeftBorder(XLBorderStyleValues.Thin)
                                    .Border.SetRightBorder(XLBorderStyleValues.Thin);
                            }
                            row++;
                            col = 2;
                        }
                    }
                    //検索用フィルターをつける
                    ws.RangeUsed().SetAutoFilter();
                }
                book.SaveAs(filePath);
            }

            string url = baseUrl + "files.upload?channels=" + GitHubDialog.channelName;

            //
            Encoding.UTF8.GetString(fileUpload(url, filePath));

            await context.PostAsync("出力が完了しました");
            //context.Wait(MessageReceivedAsync);
        }
        #endregion project出力

        #region 出力4（issueのExcel出力）
        /// <summary>
        /// project出力
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task OutputIssue(IDialogContext context, List<IReadOnlyList<Issue>> issueListForLabels, List<string> labelNameList)
        {
            //try
            //{
            //出力するExcelファイルの一時保存パス
            string filePath = @"D:\home\Excel\" + repository.Split('/')[1] + "(issue).xlsx";

            //出力する旨をbotで表示
            await context.PostAsync("project「" + repository.Split('/')[1] + "(issue)」を出力しています...");

            //ワークブックに書き込む
            using (var book = new XLWorkbook(XLEventTracking.Disabled))
            {
                //ラベル名前リストのインデックス用変数
                int i = 0;

                //ラベル別issueリストの各issueリストに対する処理
                foreach (var issueList in issueListForLabels)
                {
                    //ワークシートを用意
                    IXLWorksheet ws = null;

                    //issueが一つでもあったらエクセルファイルに書き込み
                    if (issueList != null) { ws = book.AddWorksheet(labelNameList[i]); }
                    //issueがひとつもなかったら次のループ処理に移る
                    else{ continue; }

                    //初期位置を2行目2列目に定める
                    int row = 2;
                    int col = 2;

                    //テンプレート大見出し格納リスト
                    List<string> HeadingList = new List<string>();

                    //ラベルのEntityを取得
                    TemplateEntity entity = entityOperation.RetrieveEntityResult(labelNameList[i], "label", string.Empty).Result as TemplateEntity;
                    if (entity != null) 
                    {
                        //改行毎に区切る
                        string[] templateColumn = entity.Template.Split('\n');
                    
                        foreach (var tempCol in templateColumn)
                        {
                            int index = tempCol.IndexOf("##");
                            if (index != -1)
                            {
                                HeadingList.Add(tempCol.Substring(index + 3));
                            }
                        }
                    }

                  
                    //ラベル別の列幅とフォントサイズの指定
                    ws = GitHubDialog.convinient.ColumnWidthAndFontSize(ws, "task");

                    row++;
                    ws.Cell(row, col).Value = "No";
                    col++;
                    ws.Cell(row, col).Value = "タイプ";
                    col++;
                    ws.Cell(row, col).Value = "タイトル";
                    col++;
                    foreach (string header in HeadingList)
                    {
                        ws.Cell(row, col).Value = header;
                        col++;
                    }
                    ws.Cell(row, col).Value = "担当者";
                    col++;
                    ws.Cell(row, col).Value = "着手日";
                    col++;
                    ws.Cell(row, col).Value = "完了日";

                    //ヘッダーのセルを範囲指定
                    IXLCell headerCell = ws.Cell(row, 2);
                    IXLRange ExcelRange = ws.Range(ws.Cell(row, 2), ws.Cell(row, col));

                    //指定範囲の背景色をオリーブに変更
                    ExcelRange.Style.Fill.BackgroundColor = XLColor.Olivine;

                    //指定行を1行下げ、列を2列目に戻す
                    row++;
                    col = 2;

                    //issueリストの各issueに対する処理
                    foreach (var issue in issueList)
                    {
                        //issueの番号を指定セルに格納
                        ws.Cell(row, col).Value = issue.Number;
                        col++;
                        //issueのラベル名を指定セルに格納
                        ws.Cell(row, col).Value = string.Join("", issue.Labels.ToList().ConvertAll(x => x.Name));
                        col++;
                        //issueのタイトルを指定セルに格納
                        ws.Cell(row, col).Value = issue.Title;
                        col++;

                        //本文の見出し部分を除き、リストで格納
                        List<string> tempList = new List<string>();
                        tempList.AddRange(issue.Body.Split(new string[] { "##" }, StringSplitOptions.RemoveEmptyEntries));

                        //本文（見出し除く）を見出しごとにセルに格納
                        for (int c = 0; c < tempList.Count; c++)
                        {
                            int index = tempList[c].IndexOf('\n');
                            ws.Cell(row, col).Value = tempList[c].Substring(index + 1);
                            col++;
                        }

                        //issueにアサインしている人がひとりでもいたら
                        if (issue.Assignees.Count != 0)
                        {
                            //アサインしているアカウント名を指定セルに格納
                            ws.Cell(row, col).Value = issue.Assignees[0].Login;
                        }
                        col++;
                        //issueが作成された日付を指定セルに格納
                        ws.Cell(row, col).Value = issue.CreatedAt.DateTime.ToString();
                        col++;
                        //issueが更新されたことがあったら
                        if (issue.UpdatedAt.HasValue)
                        {
                            //issueの更新日付を指定セルに格納
                            ws.Cell(row, col).Value = ((DateTimeOffset)issue.UpdatedAt).DateTime.ToString();
                        }
                        //現在issueがcloseされていたら
                        if (issue.ClosedAt.HasValue)
                        {
                            //指定セルの行の背景色を灰色にする
                            IXLRange range = ws.Range(ws.Cell(row, 2), ws.Cell(row, col));
                            range.Style.Fill.BackgroundColor = XLColor.Gray; // 灰色
                        }
                        //リストの最後尾だったら
                        if (issueList.Last().Equals(issue))
                        {
                            //表形式にするため罫線をつける
                            IXLRange range = ws.Range(headerCell, ws.Cell(row, col));
                            range.Style
                                .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                                .Border.SetInsideBorder(XLBorderStyleValues.Hair)
                                .Border.SetLeftBorder(XLBorderStyleValues.Thin)
                                .Border.SetRightBorder(XLBorderStyleValues.Thin);
                        }
                        //次の行に行く
                        row++;
                        col = 2;
                    }
                    //検索用フィルターをつける
                    ws.RangeUsed().SetAutoFilter();
                    row++;
                    i++;
                }
                //引数のパスにExcelファイルを保存する
                book.SaveAs(filePath);
            }

            //ファイル送信用slackAPIのURl
            string url = baseUrl + "files.upload?channels=" +GitHubDialog.channelName;

            //指定パスのファイルでファイル送信APIを送る
            Encoding.UTF8.GetString(fileUpload(url, filePath));

            //出力完了のメッセージを送信する
            await context.PostAsync("出力が完了しました");
            //context.Wait(MessageReceivedAsync);
            //}
            //catch (Exception e)
            //{
            //    await context.PostAsync(e.Message);
            //    context.Wait(MessageReceivedAsync);
            //}
        }
        #endregion project出力
    }
}