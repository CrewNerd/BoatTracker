using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace BoatTracker.Bot.DataObjects
{
    public class BotUserEntity : TableEntity
    {
        public BotUserEntity(string clubId, long userId)
        {
            this.PartitionKey = clubId;
            this.RowKey = userId.ToString();
        }

        public BotUserEntity() { }

        public string ToId { get; set; }

        public string ToName { get; set; }

        public string FromId { get; set; }

        public string FromName { get; set; }

        public string ServiceUrl { get; set; }

        public string ChannelId { get; set; }

        public string ConversationId { get; set; }

        public string ClubId
        {
            get
            {
                return this.PartitionKey;
            }
        }

        public long UserId
        {
            get
            {
                return long.Parse(this.RowKey);
            }
        }
    }
}