using Newtonsoft.Json;
using Microsoft.Azure.Cosmos.Table;
using System;

namespace CoreBot.DBClass
{
    
    public class UserEntity : TableEntity
    {
        public UserEntity() { }

        public UserEntity(string email)
        {
            PartitionKey = "UserEntity";
            RowKey = email;
        }

        public UserEntity(string email, bool verified, string name)
        {
            PartitionKey = "UserEntity";
            RowKey = email;
            Verified = verified;
            Name = name;
        }

        public bool Verified { get; set; }
        public string Name { get; set; }
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class SubscriptionEntity : TableEntity
    {
        public SubscriptionEntity() { }

        public SubscriptionEntity(string id)
        {
            PartitionKey = "SubscriptionEntity";
            RowKey = id;
        }

        public SubscriptionEntity(string id, string email, string recipientId, string category, int frequency, DateTime nextRequest, string subscriptionType)
        {
            PartitionKey = "SubscriptionEntity";
            RowKey = id;
            Email = email;
            RecipientId = recipientId;
            Category = category;
            Frequency = frequency;
            NextRequest = nextRequest;
            SubscriptionType = subscriptionType;
        }

        public string Email { get; set; }
        public string RecipientId { get; set; }
        public string Category { get; set; }
        public int Frequency { get; set; }
        public DateTime NextRequest { get; set; }
        public string SubscriptionType { get; set; }
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class AdviceEntity : TableEntity
    {
        public AdviceEntity()
        {
            PartitionKey = "AdviceEntity";
        }

        public AdviceEntity(string id)
        {
            PartitionKey = "AdviceEntity";
            RowKey = id;
        }

        public AdviceEntity(string id, string category, string value)
        {
            PartitionKey = "AdviceEntity";
            RowKey = id;
            Category = category;
            Value = value;
        }

        public string Category { get; set; }
        public string Value { get; set; }
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class NewAdviceEntity : TableEntity
    {
        public NewAdviceEntity() { }

        public NewAdviceEntity(string id)
        {
            PartitionKey = "NewAdviceEntity";
            RowKey = id;
        }

        public NewAdviceEntity(string id, string category, string value)
        {
            PartitionKey = "NewAdviceEntity";
            RowKey = id;
            Category = category;
            Value = value;
        }

        public string Category { get; set; }
        public string Value { get; set; }
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class AdviceConnection : TableEntity
    {
        public AdviceConnection() { }

        public AdviceConnection(string userId, string adviceId)
        {
            PartitionKey = userId;
            RowKey = adviceId;
        }
        public AdviceConnection(string userId, string adviceId, string category)
        {
            PartitionKey = userId;
            RowKey = adviceId;
            Category = category;
        }
        public string Category { get; set; }
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class AdviceComparer : System.Collections.Generic.IEqualityComparer<AdviceEntity>
    {
        public bool Equals(AdviceEntity x, AdviceEntity y) => (x.RowKey == y.RowKey);
        public int GetHashCode(AdviceEntity obj) => obj.RowKey.GetHashCode();
    }
}
