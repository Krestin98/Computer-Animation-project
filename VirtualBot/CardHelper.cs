using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using System.IO;

namespace CoreBot
{
    public static class CardHelper
    {
        // Creates a hero or an adaptive card using a path to the JSON
        public static Attachment CreateAttachmentCard(string cardResourcePath, bool hero) 
        {
            using (var reader = new StreamReader(cardResourcePath))
            {
                var adaptiveCard = reader.ReadToEnd();
                if (hero)
                {
                    return new Attachment()
                    {
                        ContentType = "application/vnd.microsoft.card.hero",
                        /*ContentType = "application/vnd.microsoft.card.adaptive",*/
                        Content = JsonConvert.DeserializeObject(adaptiveCard),
                    };
                }
                else {
                    return new Attachment()
                    {
                        ContentType = "application/vnd.microsoft.card.adaptive",
                        Content = JsonConvert.DeserializeObject(adaptiveCard),
                    };
                }
                }
        }
    }
}
