using Newtonsoft.Json;
using SimpleEchoBot.Model.BaseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SimpleEchoBot.Model
{
    [JsonObject("actions")]
    public class MenusActionsModel : BaseActionsModel
    {
        [JsonProperty("options")]
        public List<OptionModel> Options { get; set; }

        public MenusActionsModel(List<string> list)
        {
            Type = "select";
            Options = list.ConvertAll(x => new OptionModel(x,x));
        }
    }
}