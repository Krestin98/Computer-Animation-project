// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CoreBot.DBClass;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;

namespace CoreBot.Dialogs
{
    public class GetAdviceDialog : CancelAndHelpDialog
    {
        public GetAdviceDialog(EmailVerifier emailVerifier)
            : base(nameof(GetAdviceDialog))
        {
            AddDialog(AddDialog(new TextPrompt(nameof(TextPrompt))));
            AddDialog(emailVerifier);
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                EmailVerifierStepAsync,
                CategoryStepAsync,
                GetAdviceStepAsync,
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

            List<string> cancelDialogCategories = new List<string> { "none", "neither", "nothing" };
            if (MainDialog.userDetails.Category == null || AdviceManager.CategoryList.IndexOf(MainDialog.userDetails.Category.ToLower()) == -1)
            {
                //quick reply
                var actions = new List<CardAction>();
                foreach (string category in AdviceManager.CategoryList)
                    actions.Add(new CardAction() { Title = category, Type = ActionTypes.ImBack, Value = category });

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
            else if (cancelDialogCategories.IndexOf(MainDialog.userDetails.Category.ToLower()) != -1)
            {
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }

             return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> GetAdviceStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            MainDialog.userDetails.Category = (string)stepContext.Result;

            if (MainDialog.userDetails.Category == null)
            {
                return await stepContext.ReplaceDialogAsync(InitialDialogId, null, cancellationToken);
            }
            else if (AdviceManager.CategoryList.Contains(MainDialog.userDetails.Category))
            {
                var result = AdviceManager.GetAdvice(MainDialog.userDetails.Category, MainDialog.userDetails.CurrentEmail );
                var reply = MessageFactory.Text(result, result, InputHints.IgnoringInput);
                await stepContext.Context.SendActivityAsync(reply, cancellationToken);
                return await stepContext.NextAsync(true, cancellationToken);
            }
            else
            {
                // todo handle unexpected values
                return await stepContext.ReplaceDialogAsync(InitialDialogId, null, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                MainDialog.userDetails.FinalPrompt = true;
            }

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
    }
}