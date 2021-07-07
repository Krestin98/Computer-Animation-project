// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace CoreBot.Controllers
{
    [Route("api/notify/")]
    [ApiController]
    public class NotifyController : ControllerBase
    {
        private readonly IBotFrameworkHttpAdapter _adapter;
        private readonly string _appId;
        private readonly ConcurrentDictionary<string, ConversationReference> _conversationReferences;

        public NotifyController(IBotFrameworkHttpAdapter adapter, IConfiguration configuration, ConcurrentDictionary<string, ConversationReference> conversationReferences)
        {
            _adapter = adapter;
            _conversationReferences = conversationReferences;
            _appId = configuration["MicrosoftAppId"];

            if (string.IsNullOrEmpty(_appId))
            {
                _appId = Guid.NewGuid().ToString(); //if no AppId, use a random Guid
            }
        }

        [HttpGet]
        [Route("")]
        public async Task<IActionResult> Get()
        {
            foreach (var conversationReference in _conversationReferences.Values)
            {
                await ((BotAdapter)_adapter).ContinueConversationAsync(_appId, conversationReference, 
                    async (context, token) => await BotCallback("ALERT: View the latest USF specific coronavirus (COVID-19) updates here https://www.usf.edu/coronavirus/", context, token), 
                    default);
            }

            // Let the caller know proactive messages have been sent
            return new ContentResult()
            {
                Content = "<html><body><h1>Corona Virus Alert has been sent to all users.</h1></body></html>",
                ContentType = "text/html",
                StatusCode = (int)HttpStatusCode.OK,
            };
        }

        [Route("query")]
        public async Task<IActionResult> SendDirect(string message, string id, string subId)
        {
            foreach (var conversationReference in _conversationReferences.Values)
            {
                if (conversationReference.User.Id == id) 
                {
                    message += $" [Unsubscribe]({Appsettings.GetAppSettings().NGROKEndPoint}/api/notify/unsub?subscriptionId={subId})";
                    await ((BotAdapter)_adapter).ContinueConversationAsync(_appId, conversationReference, async (context, token) => await BotCallback(message, context, token), default);
                }
            }

            return new ContentResult()
            {
                Content = "<html><body><h1>Sent!</h1></body></html>",
                ContentType = "text/html",
                StatusCode = (int)HttpStatusCode.OK,
            };
        }

        [Route("unsub")]
        public string Unsubscribe(string subscriptionid)
        {
            Console.WriteLine($"Info: Unsubscribe caught for {subscriptionid}");
            DBClass.AccountManager.RemoveSubscriptionById(subscriptionid);
            return $"Unsubscribed via Channel, subscriptionid: {subscriptionid}";
        }

        private async Task BotCallback(string message, ITurnContext turnContext, CancellationToken token)
        {
            await turnContext.SendActivityAsync(message, cancellationToken: token);
        }
    }
}