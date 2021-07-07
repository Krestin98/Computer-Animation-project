// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters.Facebook;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using CoreBot.Bots;
using CoreBot.Dialogs;
using Microsoft.Extensions.DependencyInjection;

// test of sendgrid capabilities
using System;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Microsoft.Bot.Schema;

// Views directives
/*using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;*/

namespace CoreBot
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            // Create the Bot Framework Adapter with error handling enabled.
            services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();
            
            // Facebook Messenger Adapter
            services.AddSingleton<FacebookAdapter, FacebookAdapterWithErrorHandler>();
            
            // Create the storage we'll be using for User and Conversation state. (Memory is great for testing purposes.)
            services.AddSingleton<IStorage, MemoryStorage>();

            // Create the User state. (Used in this bot's Dialog implementation.)
            services.AddSingleton<UserState>();

            // Create the Conversation state. (Used by the Dialog system itself.)
            services.AddSingleton<ConversationState>();

            // Register LUIS recognizer
            services.AddSingleton<USFVirtualAssistantRecognizer>();

            // Create a global hashset for our ConversationReferences
            services.AddSingleton<ConcurrentDictionary<string, ConversationReference>>();

            // The PersonaDialog that will handle personality change.
            services.AddSingleton<PersonaDialog>();
            
            // Handles email login and verification
            services.AddSingleton<EmailVerifier>();

            // The GetAdviceDialog that will send the user advice based on the category they choose
            services.AddSingleton<GetAdviceDialog>();

            // The GiveAdviceDialog that add advice and category to the DB
            services.AddSingleton<GiveAdviceDialog>();

            // The recurringAdviceDialog that subscribes to categorized advice from the DB
            services.AddSingleton<RecurringAdviceDialog>();

            // The MainDialog that will be run by the bot.
            services.AddSingleton<MainDialog>();

            // Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
            services.AddTransient<IBot, DialogAndWelcomeBot<MainDialog>>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseWebSockets();

            app.UseMvc();
            //this line test the cosmos db database interface, see the DBManager class to edit the test functions
#if DEBUG
            _ = TestDatabase();
#endif //DEBUG
            DBClass.AccountManager.SendSubscriptionRequests();
        }

        // Test email client, please point to a valid address before testing
        static async Task TestDatabase()
        {
            try
            {
                DBClass.DBManager dbt = new DBClass.DBManager();
/*                await dbt.GetStartedDemoAsync();*/
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}", e);
            }
        }
        
    }
}
