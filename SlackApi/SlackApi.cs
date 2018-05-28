using System.Configuration;
using System.Net;
using System.Text;

namespace SimpleEchoBot.SlackApi
{
    public class SlackApi
    {

        public static WebClient CreateHeader_Post()
        {
            WebClient webClient = new WebClient();
            webClient.Headers[HttpRequestHeader.ContentType] = "application/json;charset=UTF-8";
            webClient.Headers[HttpRequestHeader.Accept] = "application/json";
            webClient.Headers[HttpRequestHeader.Authorization] = "Bearer " + ConfigurationManager.AppSettings["botToken"];
            webClient.Encoding = Encoding.UTF8;

            return webClient;
        }

    }
}