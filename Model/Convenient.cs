using ClosedXML.Excel;
using Microsoft.Bot.Sample.SimpleEchoBot;
using Octokit;
using SimpleEchoBot.RedisCache;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleEchoBot.Model
{
    public class Convenient
    {
        #region 【メソッド】一致したラベルのissueを取得
        /// <summary>
        /// 一致したラベル名のissueを取得
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public async Task<IReadOnlyList<Issue>> GetIssuesForLabel(List<string> labels)
        {
            var recently = new RepositoryIssueRequest();
            foreach (var label in labels)
            {
                recently.Labels.Add(label);
            }

            string repository = RedisCacheOperation.Connection.StringGet(GitHubDialog.channelId);

            IReadOnlyList<Issue> issues = await GitHubDialog.github.Issue.GetAllForRepository(repository.Split('/')[0], repository.Split('/')[1], recently);

            return issues;
        }
        #endregion

        #region 【メソッド】issueテンプレート
        /// <summary>
        /// Bodyがないテンプレート
        /// </summary>
        /// <param name="temp"></param>
        /// <returns></returns>
        public string SetTemplateNoBody(List<string> temp, List<string> bodyList)
        {
            string template = string.Empty;
            for (int i = 0; i < temp.Count; i++)
            {
                template += "## " + temp[i] + Environment.NewLine + bodyList[i] + Environment.NewLine;
                //IssueModel.elementList[i] = temp[i];
                //IssueModel.bodyList[i] = bodyList[i];
            }
            
            return template;
        }
        #endregion

        public IXLWorksheet ColumnWidthAndFontSize(IXLWorksheet ws, string label)
        {
            switch (label)
            {
                case "task":
                    {
                        ws.Columns("A").Width = 3;
                        //No列の幅
                        ws.Columns("B").Width = 3;
                        //タイプ列の幅
                        ws.Columns("C").Width = 5;
                        //内容、詳細列の幅
                        ws.Columns("D:E").Width = 64;
                        //完了判断列の幅
                        ws.Columns("F").Width = 30;
                        //備考列の幅
                        ws.Columns("G").Width = 40;
                        //担当者、日付列の幅
                        ws.Columns("H:J").Width = 15;

                        //文字サイズ指定
                        ws.Columns("A:K").Style.Font.FontSize = 9;
                        break;
                    }
            }

            return ws;

        }
    }
}