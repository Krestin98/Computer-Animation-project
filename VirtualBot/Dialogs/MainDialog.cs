// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using CoreBot.CognitiveModels;
using System.Linq;
using CoreBot.Details;
using CoreBot.QnA;

namespace CoreBot.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        public static readonly string DEFAULT_QNA = "No good match found in KB.".ToLower();
        public static UserDetails userDetails = new UserDetails();
        private const char GIVE_ADVICE_DIALOG_ECC = '2';   // IDK WHY BUT THE GIVE_ADVICE_DIALOG ID HAS 2 AT THE END
        private readonly USFVirtualAssistantRecognizer _luisRecognizer;
        protected readonly ILogger Logger;
        // Dependency injection uses this constructor to instantiate MainDialog
        public MainDialog(USFVirtualAssistantRecognizer luisRecognizer, 
            PersonaDialog personaDialog, GetAdviceDialog getAdviceDialog, GiveAdviceDialog giveAdviceDialog, RecurringAdviceDialog recurringAdviceDialog,
            ILogger<MainDialog> logger)
            : base(nameof(MainDialog))
        {
            _luisRecognizer = luisRecognizer;
            Logger = logger;
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(personaDialog);
            AddDialog(getAdviceDialog);
            AddDialog(giveAdviceDialog);
            AddDialog(recurringAdviceDialog);
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                IntroStepAsync,
                ActStepAsync,
                FinalStepAsync,
            }));
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (!_luisRecognizer.IsConfigured)
            {
#if DEBUG
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("NOTE: LUIS is not configured. To enable all capabilities, add 'LuisAppId', 'LuisAPIKey' and 'LuisAPIHostName' to the appsettings.json file.", inputHint: InputHints.IgnoringInput), cancellationToken);
#endif //DEBUG
                return await stepContext.NextAsync(null, cancellationToken);
            }

            // Use the text provided in FinalStepAsync or the default if it is the first time.
            var messageText = stepContext.Options?.ToString() ?? "What can I help you with today?";
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (!_luisRecognizer.IsConfigured)
            {
                // Placeholder, perhaps give a random advice.
/*                return await stepContext.BeginDialogAsync(nameof(LUISMissingDialog), cancellationToken);*/
            }

            userDetails.MemberId = stepContext.Context.Activity.From.Id;
            // Instantiate the knowledge base
            QnAMakerService KB_1_QnAService = new QnAMakerService();

            // Call LUIS and gather any potential details. (Note the TurnContext has the response to the prompt.)
            var luisResult = await _luisRecognizer.RecognizeAsync<USFVirtualAssistantLUIS>(stepContext.Context, cancellationToken);
            var intent = luisResult.TopIntent().intent;
#if DEBUG
            var PlaceHolderMessageText = $"Intent was {intent} with a score of {luisResult.TopIntent().score}.";
            var PlaceHolderMessage = MessageFactory.Text(PlaceHolderMessageText, PlaceHolderMessageText, InputHints.IgnoringInput);
#endif //DEBUG
            IActivity response;
            string ssml;

            userDetails.Category = luisResult.Entities.AdviceCategory?[0];
            userDetails.InputEmail = luisResult.Entities.email?[0];
            userDetails.FrequencyInput = luisResult.Entities.datetime?[0];
            userDetails.PersonaInput = luisResult.Entities.PersonalityType?[0];

            // Unknown intent, QNA & Chit Chat
            if (luisResult.TopIntent().score >= 0.5)
            {
                //luis Intent dialog switch
                switch (intent)
                {
                    // Display Help Menu
                    case USFVirtualAssistantLUIS.Intent.GeneralHelpMenu:
                        ssml = "General Help Menu: Get Advice, Give Advice, Get Recurring Advice, Get Course Details, Get Computer Science & Engineering Department Details";

                        switch (stepContext.Context.Activity.ChannelId)
                        {
                            case ChannelIDs.Facebook:
                                Attachment fbAttachment = CardHelper.CreateAttachmentCard("Cards/generalHelpMenuFBCard.json", true);
                                response = MessageFactory.Attachment(fbAttachment, ssml: ssml);
                                break;

                            default:
                                Attachment emulatorAttachment = CardHelper.CreateAttachmentCard("Cards/generalHelpMenuCard.json", false);
                                response = MessageFactory.Attachment(emulatorAttachment, ssml: ssml);
                                break;
                        }

                        await stepContext.Context.SendActivityAsync(response, cancellationToken);
                        break;

                    // Give the user advice pertaining to a category
                    case USFVirtualAssistantLUIS.Intent.UserGetsAdvice:
                        return await stepContext.BeginDialogAsync(nameof(GetAdviceDialog), null, cancellationToken);
                    
                    // Add Advice to the bot, the user provides advice to the bot
                    case USFVirtualAssistantLUIS.Intent.UserGivesAdvice:
                        return await stepContext.BeginDialogAsync(nameof(GiveAdviceDialog)+GIVE_ADVICE_DIALOG_ECC, null, cancellationToken);
                    
                    // User subscribes to advice dialog
                    case USFVirtualAssistantLUIS.Intent.UserGetsRecurringAdvice:
                        return await stepContext.BeginDialogAsync(nameof(RecurringAdviceDialog), null, cancellationToken);

                    // User changes bot personality
                    case USFVirtualAssistantLUIS.Intent.PersonalityToggle:
                        if (userDetails.PersonaInput is null || userDetails.PersonaInput.Length < Personality.PERSONA_STRING_LENGTH)
                        {
                            userDetails.PersonaInput = null;
                        }
                        return await stepContext.BeginDialogAsync(nameof(PersonaDialog), null, cancellationToken);

                    // Intent not implemented, QNA & Chit Chat
                    default:
                        var qnaMakerAnswer = await KB_1_QnAService.GetAnswer(luisResult.Text, true);

                        if (qnaMakerAnswer.ToLower() == DEFAULT_QNA || KB_1_QnAService.topAnswerScore < 60.0) // (04/10) JDM: Setting QnA Confidence Score Threshold ( || condition added)
                        {
                            var _PersonalityChatResults = Personality.GetPersonalityChatService().QueryServiceAsync(luisResult.Text).Result;
                            var _PersonalityChatOutput = _PersonalityChatResults?.ScenarioList?.FirstOrDefault()?.Responses?.FirstOrDefault() ?? "";
                            if (_PersonalityChatOutput.ToLower() == DEFAULT_QNA)
                            {
                                _PersonalityChatOutput = "I'm sorry, I didn't understand.";
                            }
                            userDetails.FinalPrompt = false;
                            await stepContext.Context.SendActivityAsync(
#if DEBUG
                            $"{Personality.CurrentPersonality().ToString()}: " +
#endif //DEBUG
                            $"{_PersonalityChatOutput}");

                        }
                        else
                        {
                            userDetails.FinalPrompt = false;
                            await stepContext.Context.SendActivityAsync($"{qnaMakerAnswer}");
                        }
                        userDetails.FinalPrompt = false;
                        break;
                    }
            }
            // Unknown or no intent, QNA & Chit Chat
            else
            {
                var qnaMakerAnswer = await KB_1_QnAService.GetAnswer(luisResult.Text, true);

                if (qnaMakerAnswer.ToLower() == DEFAULT_QNA || KB_1_QnAService.topAnswerScore < 60.0) // (04/10) JDM: Setting QnA Confidence Score Threshold ( || condition added)
                {
                    var PersonalityChatResults = Personality.GetPersonalityChatService().QueryServiceAsync(luisResult.Text).Result;
                    var PersonalityChatOutput = PersonalityChatResults?.ScenarioList?.FirstOrDefault()?.Responses?.FirstOrDefault() ?? "";
                    if (PersonalityChatOutput.ToLower() == DEFAULT_QNA) 
                    {
                        PersonalityChatOutput = "I'm sorry, I didn't understand.";
                    }
                    userDetails.FinalPrompt = false;
                    await stepContext.Context.SendActivityAsync(
#if DEBUG
                            $"{Personality.CurrentPersonality().ToString()}: " +
#endif //DEBUG
                            $"{PersonalityChatOutput}");

                }
                else
                {
                    userDetails.FinalPrompt = false;
                    await stepContext.Context.SendActivityAsync($"{qnaMakerAnswer}");
                }
                userDetails.FinalPrompt = false;
                return await stepContext.NextAsync(null, cancellationToken);
            }
            userDetails.FinalPrompt = false;
            return await stepContext.NextAsync(null, cancellationToken);
        }

        //  Finish Up dialog
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Check if user dialog inputs need to be reset when dialog finishes
            if (stepContext.Result is null && userDetails.FinalPrompt) 
            {
                userDetails.ResetInputs();
                // Restart the main dialog with a different message the second time around
                var promptMessage = "What else can I do for you?";
                return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
            }
            else 
            {
                userDetails.ResetInputs();
                // Restart the main dialog
                var promptMessage = "";
                return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
            }
        }
    }
}
