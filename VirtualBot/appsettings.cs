using Newtonsoft.Json;
using System.IO;

namespace CoreBot
{
    // A way to reference appsettings.json for critical configuration information.
    public class Appsettings
    {
        public static Appsettings GetAppSettings(){ return JsonConvert.DeserializeObject<Appsettings>(File.ReadAllText(Directory.GetCurrentDirectory() + "/appsettings.json"));}
        public string LuisAPIHostName { get; set; }
        public string LuisAPIKey { get; set; }
        public string LuisAppId { get; set; }
        public string MicrosoftAppId { get; set; }
        public string MicrosoftAppPassword { get; set; }
        public string ScmType { get; set; }
        public string MessagingEndPoint { get; set; }
        public string NGROKEndPoint { get; set; }
        public string FacebookAppSecret { get; set; }
        public string FacebookAccessToken { get; set; }
        public string FacebookVerifyToken { get; set; }
    }
}
