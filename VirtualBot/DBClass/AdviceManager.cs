using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreBot.DBClass
{
    public class AdviceManager
    {
        private static readonly DBManager manager = new DBManager();
        private static readonly Random random = new Random();

        public static List<string> CategoryList { get { return DBManager.Categories; } }

        //  Get a Unique Advice Item via category and relating to a userId
        public static string GetAdvice( string category, string userId)
        {
            //todo respect userId
            Microsoft.Azure.Cosmos.Table.CloudTable entityTable = DBManager.entityTable;
            Microsoft.Azure.Cosmos.Table.CloudTable relationTable = DBManager.relationTable;

            IEnumerable<AdviceEntity> allAdvice = entityTable.CreateQuery<AdviceEntity>()
                .Where(x => x.PartitionKey.Equals("AdviceEntity") && x.Category.Equals(category));

            if ( allAdvice.Any() )
            {
                IEnumerable<AdviceEntity> unseenAdvice = allAdvice.Except(
                        relationTable.CreateQuery<AdviceConnection>()
                            .Where(x => x.PartitionKey.Equals(userId) && x.Category.Equals(category))
                            .Select(x => new AdviceEntity() { RowKey = x.RowKey }),
                        new AdviceComparer()
                    ).AsEnumerable();

                AdviceEntity result;
                if ( unseenAdvice.Any() )
                {
                    result = unseenAdvice.ElementAt(random.Next(unseenAdvice.Count()));
                }
                else
                {
                    result = allAdvice.ElementAt(random.Next(allAdvice.Count()));
                    _ = manager.RemoveRelationsAsync(userId, category);
                }
                _ = manager.AddRelationAsync(new AdviceConnection(userId, result.RowKey, category));
                return result.Value;
            }
            else
            {
                return "no results";
            }
        }

        //  Add an advice item to the database to a particular category
        public static bool AddAdvice( string category, string value, string userId)
        {
            try
            {
                string id;
                do
                {
                    id = random.Next().ToString();
                }
                while (manager.TestAdviceIdExists(userId + "-" + id));
                NewAdviceEntity advice = new NewAdviceEntity(userId + "-" + id, category, value);
                _ = manager.AddItemAsync(advice);
                return true;
            }
            catch { return false; }
        }

        //  Add a subcription entity to the database
        public static bool AddSubscription( string email, string recipientId, string category, int frequency, string subscriptionType)
        {
            try
            {
                string id;
                do
                {
                    id = random.Next().ToString();
                }
                while (manager.TestSubscriptionIdExists(id));
                SubscriptionEntity subscription = new SubscriptionEntity(id, email, recipientId, category, frequency, DateTime.Now, subscriptionType);
                _ = manager.AddItemAsync(subscription);
                return true;
            }
            catch { return false; }
        }

        //  Check if the user is verified via email in the database
        public static bool IsUserVerified(string email)
        {
            try
            {
                Microsoft.Azure.Cosmos.Table.CloudTable table = DBManager.entityTable;
                return table.CreateQuery<UserEntity>()
                        .Where(x => x.PartitionKey.Equals("UserEntity") && x.RowKey.Equals(email.ToLower()) && x.Verified)
                        .AsEnumerable().Any();                
            }
            catch { return false; }
        }

        //  Add a User entity or profile pertaining to the email address.
        public static bool AddUser(string email)
        {
            try
            {
                email = email.ToLower();
                if (!manager.TestUserExists(email))
                {
                    _ = manager.AddItemAsync(new UserEntity(email));
                }
                return true;
            }
            catch { return false; }
        }
    }
}
