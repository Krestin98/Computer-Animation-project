// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CoreBot.CognitiveModels;
using CoreBot.DBClass;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;

namespace CoreBot.Dialogs
{
    public class RecurringAdviceDialog : CancelAndHelpDialog
    {
        private readonly USFVirtualAssistantRecognizer _luisRecognizer;

        public RecurringAdviceDialog(USFVirtualAssistantRecognizer luisRecognizer, EmailVerifier emailVerifier)
            : base(nameof(RecurringAdviceDialog))
        {
            _luisRecognizer = luisRecognizer;
            AddDialog(AddDialog(new TextPrompt(nameof(TextPrompt))));
            AddDialog(emailVerifier);
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                EmailVerifierStepAsync,
                FrequencyStepAsync,
                CategoryStepAsync,
                InstantMessageStepAsync,
                FinalStepAsync
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> EmailVerifierStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (
                (MainDialog.userDetails.CurrentEmail == null)
                || (MainDialog.userDetails.InputEmail != null && (MainDialog.userDetails.InputEmail != MainDialog.userDetails.CurrentEmail))
                )
            {
                return await stepContext.BeginDialogAsync(nameof(EmailVerifier), null, cancellationToken);
            }
            
            if (MainDialog.userDetails.EmailVerified) 
            {
                return await stepContext.NextAsync(null, cancellationToken);
            }

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        private bool IsValidTimeSpan(DateTimeSpec frequencyInput)
        {
            var dwm = new List<char>{ 'd', 'w', 'm' };
            string temp = frequencyInput.Expressions[0].ToString().ToLower();
            char dwmType = temp[temp.Length-1];
            if (dwm.IndexOf(dwmType) != -1) { return true; }
            else { return false; }
        }

        private int FrequencyInDays(DateTimeSpec frequencyInput)
        {
            const int DAYS_IN_WEEK = 7;
            const int DAYS_IN_MONTH = 30;
            var dwm = new List<char> { 'd', 'w', 'm' };
            var temp = frequencyInput.Expressions[0].ToString().ToLower();
            var dwmType = temp[temp.Length - 1];
            var num = int.Parse(temp.TrimStart(temp[0]).TrimEnd(dwmType));
            
            if (dwm.IndexOf(dwmType) != -1) 
            {
                switch (dwmType)
                {
                    //day
                    case 'd':
                        return num;
                    //week
                    case 'w':
                        return num * DAYS_IN_WEEK;
                    //month
                    case 'm':
                        return num * DAYS_IN_MONTH;
                    default:
                        return -1;
                }
            }
            else { return -1; }
        }

        private async Task<DialogTurnResult> FrequencyStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (!MainDialog.userDetails.EmailVerified) 
            {
                MainDialog.userDetails.FinalPrompt = false;
                return await stepContext.EndDialogAsync(null, cancellationToken); 
            }

            List<string> qReply = new List<string> { "every day", "every week", "every month" };
            
            if (MainDialog.userDetails.FrequencyInput == null)
            {
                //quick reply
                var reply = MessageFactory.Text("Would you like advice every day, week, month?");
                reply.SuggestedActions = new SuggestedActions()
                {
                    Actions = new List<CardAction>()
                            {
                                new CardAction(){ Title = qReply[0], Type=ActionTypes.ImBack, Value = qReply[0] },
                                new CardAction(){ Title = qReply[1], Type=ActionTypes.ImBack, Value = qReply[1] },
                                new CardAction(){ Title = qReply[2], Type=ActionTypes.ImBack, Value = qReply[2] }
                            }
                };
                reply.InputHint = InputHints.ExpectingInput;

                // prompt for frequency
                return await stepContext.PromptAsync(nameof(TextPrompt),
                    new PromptOptions
                    {
                        Prompt = reply
                    }, cancellationToken);
            }

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> CategoryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var luisResult = await _luisRecognizer.RecognizeAsync<USFVirtualAssistantLUIS>(stepContext.Context, cancellationToken);
            if (MainDialog.userDetails.FrequencyInput is null)
            {
                MainDialog.userDetails.FrequencyInput = luisResult.Entities.datetime?[0];
                return await stepContext.ReplaceDialogAsync(InitialDialogId, null, cancellationToken);
            }
            else if (IsValidTimeSpan(MainDialog.userDetails.FrequencyInput))
            {
                MainDialog.userDetails.Frequency = FrequencyInDays(MainDialog.userDetails.FrequencyInput);
            }
            else 
            {
                return await stepContext.ReplaceDialogAsync(InitialDialogId, null, cancellationToken);
            }

            List<string> cancelDialogCategories = new List<string> { "none", "neither", "nothing" };
            if (MainDialog.userDetails.Category == null || AdviceManager.CategoryList.Contains(MainDialog.userDetails.Category))
            {
                var actions = new List<CardAction>();
                foreach (string category in AdviceManager.CategoryList)
                    actions.Add(new CardAction() { Title = category, Type = ActionTypes.ImBack, Value = category });
                //quick reply
                var reply = MessageFactory.Text("Here are some advice categories you can choose from");
                reply.SuggestedActions = new SuggestedActions()
                {
                    Actions = actions
                };
                reply.InputHint = InputHints.ExpectingInput;

                // prompt for categories to select. (Use Quick reply)
                return await stepContext.PromptAsync(nameof(TextPrompt),
                    new PromptOptions
                    {
                        Prompt = reply,
                        Choices = ChoiceFactory.ToChoices(AdviceManager.CategoryList)
                    }, cancellationToken);
            }
            else if (cancelDialogCategories.Contains(MainDialog.userDetails.Category))
            {
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> InstantMessageStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            MainDialog.userDetails.Category = (string)stepContext.Result;
            string channelId = char.ToUpper(stepContext.Context.Activity.ChannelId[0]) + stepContext.Context.Activity.ChannelId.Substring(1);
            var reply = MessageFactory.Text($"What medium would you like to subscribe via Email, {channelId}, or both?");
            reply.SuggestedActions = new SuggestedActions()
            {
                Actions = new List<CardAction>()
                        {
                            new CardAction(){ Title = "Email", Type=ActionTypes.ImBack, Value="Email" },
                            new CardAction(){ Title = channelId, Type=ActionTypes.ImBack, Value=channelId},
                            new CardAction(){ Title = "Both", Type=ActionTypes.ImBack, Value="Both" }
                        }
            };
            reply.InputHint = InputHints.ExpectingInput;

            // prompt for personalities to select. (Use Quick reply)
            return await stepContext.PromptAsync(nameof(TextPrompt),
                new PromptOptions
                {
                    Prompt = reply,
                    Choices = ChoiceFactory.ToChoices(new List<string> { "Email", channelId, "Both" })
                }, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            MainDialog.userDetails.SubscriptionType = (string)stepContext.Result;

            if ((string)stepContext.Result == (char.ToUpper(stepContext.Context.Activity.ChannelId[0]) + stepContext.Context.Activity.ChannelId.Substring(1))) 
            {
                MainDialog.userDetails.SubscriptionType = "Channel";
            } 
            else 
            { 
                MainDialog.userDetails.SubscriptionType = (string)stepContext.Result; 
            }

            //generate subscription
            AdviceManager.AddSubscription(MainDialog.userDetails.CurrentEmail, MainDialog.userDetails.MemberId, MainDialog.userDetails.Category, MainDialog.userDetails.Frequency, MainDialog.userDetails.SubscriptionType);
#if DEBUG
            var result = $"Subscription Request: To: {MainDialog.userDetails.CurrentEmail} | Every {MainDialog.userDetails.Frequency} Days | Category: {MainDialog.userDetails.Category} | Subscription Type: {MainDialog.userDetails.SubscriptionType}";
            var reply = MessageFactory.Text(result, result, InputHints.IgnoringInput);
            await stepContext.Context.SendActivityAsync(reply, cancellationToken);
#endif //DEBUG
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
    }
}