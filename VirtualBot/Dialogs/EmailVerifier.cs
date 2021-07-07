// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// This Dialog verifies the email address given by the user, if not verified the dialog callback will end.
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CoreBot.DBClass;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace CoreBot.Dialogs
{
    public class EmailVerifier : CancelAndHelpDialog
    {
        private readonly USFVirtualAssistantRecognizer _luisRecognizer;

        public EmailVerifier(USFVirtualAssistantRecognizer luisRecognizer)
            : base(nameof(EmailVerifier))
        {
            _luisRecognizer = luisRecognizer;
            AddDialog(AddDialog(new TextPrompt(nameof(TextPrompt))));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                EmailStepAsync,
                CheckVerifiedStepAsync
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }
        
        private bool IsUSFEmail(string email)
        {
            string emailDomain;
            List<string> USF_EMAIL_DOMAIN;
            try
            {
                emailDomain = email.Trim().ToLower().Split('@')[1];
                USF_EMAIL_DOMAIN = new List<string> { $"mail.usf.edu", $"usf.edu" };
            }
            catch 
            {
                return false;
            }

            if (USF_EMAIL_DOMAIN.IndexOf(emailDomain) != -1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private async Task<DialogTurnResult> EmailStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (MainDialog.userDetails.InputEmail is null || !IsUSFEmail(MainDialog.userDetails.InputEmail))
            {
                var reply = MessageFactory.Text("What is your usf email (@mail.usf.edu)? (type 'stop' to stop this form)");
                reply.InputHint = InputHints.ExpectingInput;

                // prompt for email
                return await stepContext.PromptAsync(nameof(TextPrompt),
                    new PromptOptions
                    {
                        Prompt = reply
                    }, cancellationToken);
            }

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> CheckVerifiedStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((string)stepContext.Result != null) { MainDialog.userDetails.InputEmail = (string)stepContext.Result; }

            if (MainDialog.userDetails.InputEmail == null) 
            { 
                MainDialog.userDetails.InputEmail = (string)stepContext.Result;
            }

            if (MainDialog.userDetails.InputEmail is null || !IsUSFEmail(MainDialog.userDetails.InputEmail))
            {
                return await stepContext.ReplaceDialogAsync(InitialDialogId, null, cancellationToken);
            }

            if (!AdviceManager.AddUser(MainDialog.userDetails.InputEmail))
            {
                var result = $"I'm sorry, I wasn't able to add your email.";
                var reply = MessageFactory.Text(result, result, InputHints.IgnoringInput);
                MainDialog.userDetails.EmailVerified = false;
                await stepContext.Context.SendActivityAsync(reply, cancellationToken);
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }

            if (!AdviceManager.IsUserVerified(MainDialog.userDetails.InputEmail.ToLower()))
            {
                var reply = MessageFactory.Text($"Your email isn't verified. Click the verification link I've sent to {MainDialog.userDetails.InputEmail} | From: no-reply@capstonebot.com via sendgrid.net ");
                reply.InputHint = InputHints.ExpectingInput;
                AccountManager.SendUserVerificationEmailById(MainDialog.userDetails.InputEmail);    //send verification email
                MainDialog.userDetails.EmailVerified = false;
                await stepContext.Context.SendActivityAsync(reply, cancellationToken);
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }

            MainDialog.userDetails.EmailVerified = true;
            MainDialog.userDetails.CurrentEmail = MainDialog.userDetails.InputEmail;
            MainDialog.userDetails.InputEmail = null;

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
    }
}