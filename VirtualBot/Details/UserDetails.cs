// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.AI.Luis;

namespace CoreBot.Details
{
    public class UserDetails
    {
        // User Dialog Inputs
        public string Category { get; set; }
        public string AdviceString { get; set; }
        public string InputEmail { get; set; }
        public DateTimeSpec FrequencyInput { get; set; }
        public int Frequency { get; set; }
        public string PersonaInput { get; set; }
        public string SubscriptionType { get; set; }


        //need to be stored in memorystorage, persistent data
        public string CurrentEmail { get; set; }
        public bool EmailVerified { get; set; }
        public string MemberId { get; set; }
        //misc
        public bool FinalPrompt { get; set; } = true;

        // Reset Inputs
        public void ResetInputs() 
        {
            Category = AdviceString = InputEmail = PersonaInput = SubscriptionType = null;
            FrequencyInput = null;
            Frequency = 0;
            FinalPrompt = true;
        }

        // Soft Reset all of User Details object
        public void ResetUserDetails() 
        {
            ResetInputs();
            EmailVerified = false;
            CurrentEmail = null;
        }
    }

}