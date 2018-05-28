using Microsoft.WindowsAzure.Storage.Table;

namespace SimpleEchoBot.Model
{
    public class TemplateEntity : TableEntity
    {
        public TemplateEntity(string label, string temp)
        {
            this.PartitionKey = label;
            this.RowKey = "label";
            this.Template = temp;
        }

        public TemplateEntity() { }

        public string Template { get; set; }

    }
}