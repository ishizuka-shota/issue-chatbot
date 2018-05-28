using Microsoft.WindowsAzure.Storage.Table;

namespace SimpleEchoBot.Model
{
    public class UserEntity : TableEntity
    {
        public UserEntity(string userId, string userName, string accessToken)
        {
            this.PartitionKey = userId;
            this.RowKey = userName;
            this.AccessToken = accessToken;
        }

        public UserEntity() { }

        public string AccessToken { get; set; }
    }
}