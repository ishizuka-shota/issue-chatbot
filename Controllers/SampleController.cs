using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace SimpleEchoBot.Controllers
{
    public class SampleController : ApiController
    {
        // GET api/<controller>
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<controller>/5
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<controller>
        public void Post([FromBody]string value)
        {
            var t = this.Request.Properties.Values;
        }

        // PUT api/<controller>/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/<controller>/5
        public void Delete(int id)
        {
        }
    }

    /// <summary>
    /// Slack Slash Command Parse Class
    /// </summary>
    public class ParseResult
    {
        public string Token { get; set; }
        public string TeamId { get; set; }
        public string TeamDomain { get; set; }
        public string ChannelId { get; set; }
        public string ChannelName { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Command { get; set; }
        public string Text { get; set; }
        public string ResponseUrl { get; set; }

        public ParseResult(string[] splitArray)
        {
            Token = Parse(splitArray[0]);
            TeamId = Parse(splitArray[1]);
            TeamDomain = Parse(splitArray[2]);
            ChannelId = Parse(splitArray[3]);
            ChannelName = Parse(splitArray[4]);
            UserId = Parse(splitArray[5]);
            UserName = Parse(splitArray[6]);
            Command = Parse(splitArray[7]);
            Text = Parse(splitArray[8]);
            ResponseUrl = Parse(splitArray[9]);
        }

        private string Parse(string txt)
        {
            return txt.Split('=').Last();
        }
    }
}