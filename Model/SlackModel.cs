using Microsoft.Bot.Sample.SimpleEchoBot;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SimpleEchoBot.Model
{
    [JsonObject("slack")]
    public class SlackModel<T>
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("channel")]
        public string Channel { get; set; }

        [JsonProperty("response_type")]
        public string Response_type { get; set; }
        
        [JsonProperty("attachments")]
        public List<AttachmentsModel<T>> Attachments { get; set; }

        public SlackModel()
        {
            Channel = "C7QS4K92Q";
            Response_type = "in_channel";
        }
    }
}