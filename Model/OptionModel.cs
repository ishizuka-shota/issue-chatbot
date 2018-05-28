using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SimpleEchoBot.Model
{
    [JsonObject("options")]
    public class OptionModel
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

        public OptionModel(string text, string value)
        {
            Text = text;
            Value = value;
        }
    }
}