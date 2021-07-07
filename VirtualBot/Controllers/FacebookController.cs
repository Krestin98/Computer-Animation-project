using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters.Facebook;

namespace CoreBot.Controllers
{
    [Route("api/facebook")]
    [ApiController]
    public class FacebookController : ControllerBase
    {
        private readonly FacebookAdapter _adapter;
        private readonly IBot _bot;

        public FacebookController(FacebookAdapter adapter, IBot bot)
        {
            _adapter = adapter;
            _bot = bot;
            
        }

        [HttpPost]
        [HttpGet]
        public async Task PostAsync()
        {
            // Delegate the processing of the HTTP POST to the adapter.
            // The adapter will invoke the bot.
            await _adapter.ProcessAsync(Request, Response, _bot);
        }
    }
}
