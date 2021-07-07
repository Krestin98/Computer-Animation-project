// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.PersonalityChat.Core;
using Microsoft.Bot.Schema;

namespace CoreBot.Dialogs
{
    public class PersonaDialog : CancelAndHelpDialog
    {
        public PersonaDialog()
            : base(nameof(PersonaDialog))
        {
            AddDialog(AddDialog(new TextPrompt(nameof(TextPrompt))));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                PersonaStepAsync,
                ConfirmPersonaStepAsync
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> PersonaStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (MainDialog.userDetails.PersonaInput == null)
            {
                //quick reply
                var reply = MessageFactory.Text("I only have a few personalities to choose from...");
                reply.SuggestedActions = new SuggestedActions()
                {
                    Actions = new List<CardAction>()
                            {
                                new CardAction(){ Title = "Professional", Type=ActionTypes.ImBack, Value="Professional" },
                                new CardAction(){ Title = "Funny", Type=ActionTypes.ImBack, Value="Funny" },
                                new CardAction(){ Title = "Friendly", Type=ActionTypes.ImBack, Value="Friendly" }
                            }
                };
                reply.InputHint = InputHints.ExpectingInput;

                // prompt for personalities to select. (Use Quick reply)
                return await stepContext.PromptAsync(nameof(TextPrompt),
                    new PromptOptions
                    {
                        Prompt = reply,
                        Choices = ChoiceFactory.ToChoices(Personality.Personas)
                    }, cancellationToken);
            }

             return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> ConfirmPersonaStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((string)stepContext.Result != null) 
            { 
                MainDialog.userDetails.PersonaInput = (string)stepContext.Result;
            }

            if (MainDialog.userDetails.PersonaInput == null || MainDialog.userDetails.PersonaInput.Length < Personality.PERSONA_STRING_LENGTH)
            {
                MainDialog.userDetails.PersonaInput = null;
                return await stepContext.ReplaceDialogAsync(InitialDialogId, null, cancellationToken);
            }
            else
            {
                var userInput = MainDialog.userDetails.PersonaInput.Substring(0, Personality.PERSONA_STRING_LENGTH)?.ToLower();
                var persona = Personality.Personas.IndexOf(userInput);
                switch (persona)
                {
                    case (int)Personality.Personalities.Friendly:
                    case (int)Personality.Personalities.Kind:
                    case (int)Personality.Personalities.Happy:
                        Personality.ChangePersona(PersonalityChatPersona.Friendly, MainDialog.userDetails.CurrentEmail);
                        break;

                    case (int)Personality.Personalities.Professional:
                    case (int)Personality.Personalities.Serious:
                        Personality.ChangePersona(PersonalityChatPersona.Professional, MainDialog.userDetails.CurrentEmail);
                        break;

                    case (int)Personality.Personalities.Humorous:
                    case (int)Personality.Personalities.Witty:
                    case (int)Personality.Personalities.Funny:
                        Personality.ChangePersona(PersonalityChatPersona.Humorous, MainDialog.userDetails.CurrentEmail);
                        break;

                    default:
                        // prompt for personalities to select. (Use Quick reply)
                        MainDialog.userDetails.PersonaInput = null;
                        return await stepContext.ReplaceDialogAsync(InitialDialogId, null, cancellationToken);
                }
                await stepContext.Context.SendActivityAsync($"I'll be {Personality.CurrentPersonality().ToString().ToLower()} now.");
                MainDialog.userDetails.FinalPrompt = false;
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
        }
    }
}