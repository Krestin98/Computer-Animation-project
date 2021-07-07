using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Net;

namespace CoreBot.DBClass
{
    public static class AccountManager
    {
        private static readonly DBManager manager = new DBManager();
        private static readonly string apiKey = "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";
        private static readonly SendGridClient client = new SendGridClient(apiKey);
        private static readonly EmailAddress sender = new EmailAddress("no-reply@capstonebot.com", "Capstone Bot");
        /*        private static readonly string URI = $"{Appsettings.GetAppSettings().MessagingEndPoint}";*/
        private static readonly string URI = $"{Appsettings.GetAppSettings().NGROKEndPoint}";
        private static readonly string verifyTemplateId = "d-432bf4f7402e40258c717b7ad591566b";
        private static readonly string adviceTemplateId = "d-0822a74f4b0f4f16b2a395a48c24ac2d";

        //  Remove User Subscription by the db rowkey
        public static async void  RemoveSubscriptionById( string subscriptionId)
        {
            try
            {
                SubscriptionEntity result = await manager.GetItemAsync<SubscriptionEntity>("SubscriptionEntity",subscriptionId);
                Console.WriteLine("Info: found {0}", result.ToString());
                await manager.RemoveItemAsync<SubscriptionEntity>(result);
                Console.WriteLine("Info: removed {0}",result.ToString());
            }
            catch(Exception e)
            {
                Console.WriteLine("Error: failure to remove {0} due to {1}", subscriptionId, e.Message);
            }
        }

        // Verify User Email Exists by the db rowkey
        public static async void VerifyUserEmailById( string userId)
        {
            try
            {
                UserEntity result = await manager.GetItemAsync<UserEntity>("UserEntity", userId);
                Console.WriteLine("Info: found {0}", result.ToString());
                result.Verified = true;
                await manager.AddItemAsync<UserEntity>(result);
                Console.WriteLine("Info: merged {0}", result.ToString());
            }
            catch(Exception e)
            {
                Console.WriteLine("Error: failure to verify {0} due to {1}", userId, e.Message);
            }
        }

        // Send the user a SendGrid email for email verification
        public static async void SendUserVerificationEmailById( string userId )
        {
            UserEntity receiver = await manager.GetItemAsync<UserEntity>("UserEntity", userId);
            var to = new EmailAddress(receiver.RowKey, receiver.Name);
            var templateData =
                $@"{{
                    'firstname':'{receiver.Name}',
                    'Sender_Name':'Capstone Advice Bot',
                    'Sender_Address':'4202 E Fowler Ave',
                    'Sender_City':'Tampa',
                    'Sender_State':'FL',
                    'Sender_Zip':'33620',
                    'verify':'{URI}/api/events/Verify/{userId}',
                    'unsubscribe':'{URI}/api/events/Unsubscribe/{userId}'
                }}";
            var jsonPayload = JsonConvert.DeserializeObject<Object>(templateData);
            var msg = MailHelper.CreateSingleTemplateEmail(sender, to, verifyTemplateId, jsonPayload);
            var response = await client.SendEmailAsync(msg);
            Console.WriteLine("Info: {0}", response.ToString());
        }

        // Send the user a SendGrid email for an advice item based on their subscription
        private static async void SendAdviceEmail( SubscriptionEntity subscription)
        {
            try
            {
                UserEntity reciever = await manager.GetItemAsync<UserEntity>("UserEntity", subscription.Email);
                var to = new EmailAddress(subscription.Email, reciever.Name);

                var advice = AdviceManager.GetAdvice(subscription.Category, reciever.RowKey);

                var templateData =
                   $@"{{
                    'firstname':'{reciever.Name}',
                    'category':'{subscription.Category}',
                    'adviceItem':'{advice}',
                    'Sender_Name':'Capstone Advice Bot',
                    'Sender_Address':'4202 E Fowler Ave',
                    'Sender_City':'Tampa',
                    'Sender_State':'FL',
                    'Sender_Zip':'33620',
                    'unsubscribe':'{URI}/api/events/Unsubscribe/{subscription.RowKey}'
                }}";
                var jsonPayload = JsonConvert.DeserializeObject<Object>(templateData);

                var msg = MailHelper.CreateSingleTemplateEmail(sender, to, adviceTemplateId, jsonPayload);
                var response = await client.SendEmailAsync(msg);
                Console.WriteLine("Info: {0}", response.ToString());
            }
            catch(Exception e)
            {
                Console.WriteLine("Error: sending failed {0}", e.Message);
            }
        }

        // Send out an advice item to all Subscriptions due via their channel
        public static async void SendSubscriptionRequests()
        {
            while (true)
            {
                try
                {
                    Microsoft.Azure.Cosmos.Table.CloudTable table = DBManager.entityTable;
                    IQueryable<SubscriptionEntity> query = table.CreateQuery<SubscriptionEntity>()
                        .Where(x => x.PartitionKey.Equals("SubscriptionEntity") && x.NextRequest.CompareTo(DateTime.Now) < 0);
                    foreach (var subscription in query)
                    {
                        if (subscription.SubscriptionType == "Email" || subscription.SubscriptionType == "Both")
                        {
                            SendAdviceEmail(subscription);
                            subscription.NextRequest = DateTime.Now.AddDays(subscription.Frequency);
                            await manager.AddItemAsync(subscription);
                        }

                        if (subscription.SubscriptionType == "Channel" || subscription.SubscriptionType == "Both") 
                        {
                            WebClient webClient = new WebClient();
                            webClient.QueryString.Add("message", AdviceManager.GetAdvice(subscription.Category, subscription.Email));
                            webClient.QueryString.Add("id", subscription.RecipientId);
                            webClient.QueryString.Add("subId", subscription.RowKey);
                            string result = webClient.DownloadString($"{Appsettings.GetAppSettings().NGROKEndPoint}/api/notify/query");
                            if (subscription.SubscriptionType != "Both")
                            {
                                subscription.NextRequest = DateTime.Now.AddDays(subscription.Frequency); 
                            }
                            await manager.AddItemAsync(subscription);
                        }
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine("Error: failed to send subscriptions: {0}", e.Message);
                }
                await Task.Delay(TimeSpan.FromDays(1));
            }
        }
    }
}
