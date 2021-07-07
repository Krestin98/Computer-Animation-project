// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CoreBot.DBClass;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace CoreBot.Dialogs
{
    public class GiveAdviceDialog : CancelAndHelpDialog
    {
        private static Activity reply;
        private static PromptOptions promptOptions;

        public GiveAdviceDialog(EmailVerifier emailVerifier)
            : base(nameof(GiveAdviceDialog))
        {
            AddDialog(AddDialog(new TextPrompt("GetCategory")));
            AddDialog(AddDialog(new TextPrompt("GetAdvice")));
            AddDialog(emailVerifier);
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                EmailVerifierStepAsync,
                CategoryStepAsync,
                AdviceCategoryAsync,
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

        private async Task<DialogTurnResult> CategoryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (!MainDialog.userDetails.EmailVerified)
            {
                MainDialog.userDetails.FinalPrompt = false;
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }

            if (MainDialog.userDetails.Category == null || AdviceManager.CategoryList.Contains(MainDialog.userDetails.Category))
            {
                var actions = new List<CardAction>();
                foreach (string category in AdviceManager.CategoryList)
                    actions.Add(new CardAction() { Title = category, Type = ActionTypes.ImBack, Value = category });

                reply = MessageFactory.Text("Here are some categories I know or you can suggest a category not listed.");
                reply.SuggestedActions = new SuggestedActions()
                {
                    Actions = actions
                };
                reply.InputHint = InputHints.ExpectingInput;

                promptOptions = new PromptOptions()
                {
                    Prompt = reply
                };

                // prompt for categories to select. (Use Quick reply)
                return await stepContext.PromptAsync("GetCategory", promptOptions, cancellationToken);
            }

             return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> AdviceCategoryAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            MainDialog.userDetails.Category = ((string)stepContext.Result).Trim();

            var messageText = $"What advice would you like to give for {MainDialog.userDetails.Category}?";

            var adviceCategoryAsyncPromptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
            return await stepContext.PromptAsync("GetAdvice", new PromptOptions { Prompt = adviceCategoryAsyncPromptMessage }, cancellationToken);

        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var miscSynonyms = new List<string> { "misc", "rand", "idk", "none", "neit", "gene" }; // we could use luis phrase list here (todo)
            MainDialog.userDetails.AdviceString = ((string)stepContext.Result).Trim();
            bool isSuccessful = false;
            if (MainDialog.userDetails.AdviceString != null || MainDialog.userDetails.AdviceString != "")
            {
                if (MainDialog.userDetails.Category is null || MainDialog.userDetails.Category == "")     // no category given, store advice.
                {
                    MainDialog.userDetails.Category = "General";
                }
                else if (MainDialog.userDetails.Category.Length >= 4)
                {
                    if (miscSynonyms.Contains(MainDialog.userDetails.Category.ToLower().Substring(0, 4)))
                    {
                        MainDialog.userDetails.Category = "General";
                    }
                }
                isSuccessful = AdviceManager.AddAdvice(MainDialog.userDetails.Category, MainDialog.userDetails.AdviceString, MainDialog.userDetails.CurrentEmail);
            }

            if (isSuccessful)
            {
#if DEBUG
                await stepContext.Context.SendActivityAsync($"Successfully added advice");
#endif //DEBUG
                if (!AdviceManager.CategoryList.Contains(MainDialog.userDetails.Category))
                {
#if DEBUG
                    await stepContext.Context.SendActivityAsync($"Category doesn't exist");
#endif //DEBUG
                    var formattedString = DBManager.AddCategoryToList(MainDialog.userDetails.Category);
                    reply.SuggestedActions.Actions.Add(new CardAction() { Title = formattedString, Type = ActionTypes.ImBack, Value = formattedString });
                }
            }
            else 
            {
#if DEBUG
                await stepContext.Context.SendActivityAsync($"Failed to added advice");
#endif //DEBUG
            }
#if DEBUG
            await stepContext.Context.SendActivityAsync($"Category: {MainDialog.userDetails.Category}\nAdvice: {MainDialog.userDetails.AdviceString}");
#endif //DEBUG
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
    }
}