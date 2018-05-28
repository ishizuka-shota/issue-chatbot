using Newtonsoft.Json;
using SimpleEchoBot.Model.BaseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SimpleEchoBot.Model
{
    [JsonObject("actions")]
    public class ButtonActionsModel : BaseActionsModel
    {
        [JsonProperty("options")]
        public string Value { get; set; }

        public ButtonActionsModel(String value)
        {
            Type = "button";
            Value = value;
        }
    }
}