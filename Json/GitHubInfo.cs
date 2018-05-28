using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace SimpleEchoBot.Json
{
    [JsonObject("issues")]
    public class GitHubInfo
    {
        /// <summary>
        /// issuesのタイトル
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// issuesの本文
        /// </summary>
        [JsonProperty("body")]
        public string Body { get; set; }

        /// <summary>
        /// 認証用
        /// </summary>
        [JsonProperty("assignee")]
        public string Assignee { get; set; }

        /// <summary>
        /// マイルストーン
        /// </summary>
        [JsonProperty("milestone")]
        public int Milestone { get; set; }
         
        /// <summary>
        /// ラベル
        /// </summary>
        [JsonProperty("labels")]
        public List<string> Labels { get; set; }
    }
}