using System;
using System.Collections.Generic;

namespace SimpleEchoBot.Model
{
    [Serializable]
    public class IssueModel
    {
        public string Title { get; set; }

        public string Body { get; set; }

        public string Labels { get; set; }

        public List<object> BosyList { get; set; }
    }
}