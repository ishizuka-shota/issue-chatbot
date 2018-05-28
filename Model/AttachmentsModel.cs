using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SimpleEchoBot.Model
{
    [JsonObject("attachments")]
    public class AttachmentsModel<T>
    {
        [JsonProperty("fallback")]
        public string Fallback { get; set; }

        [JsonProperty("attachment_type")]
        public string Attachment_type { get; set; }

        [JsonProperty("callback_id")]
        public string Callback_id { get; set; }

        [JsonProperty("actions")]
        public List<T> Actions { get; set; }

        public AttachmentsModel()
        {
            Fallback = "This is slack api";
            Attachment_type = "default";
        }

    }
}