using Microsoft.Bot.Builder.PersonalityChat.Core;
using System;
using System.Collections.Generic;

namespace CoreBot
{
    public static class Personality
    {
        public const int PERSONA_STRING_LENGTH = 4;

        public enum Personalities
        { 
            Friendly,
            Kind,
            Happy,
            Professional,
            Serious,
            Humorous,
            Witty,
            Funny
        }

        public static List<string> Personas { get; } = new List<string> { "frie", "kind", "happ", "prof", "seri", "humo", "witt", "funn" };
        static PersonalityChatOptions PersonalityChatOptions = new PersonalityChatOptions("60885a4c-cbc6-4ff6-98ea-392a01870e92", PersonalityChatPersona.Professional);
        static PersonalityChatService PersonalityChatService = new PersonalityChatService(PersonalityChatOptions);

        public static void ChangePersona(PersonalityChatPersona _persona, string currentEmail)
        {
            PersonalityChatOptions = new PersonalityChatOptions("60885a4c-cbc6-4ff6-98ea-392a01870e92", _persona);
            PersonalityChatService = new PersonalityChatService(PersonalityChatOptions);
            if (currentEmail != null) 
            {
                //store personality to UserEntity
            }
        }

        public static PersonalityChatPersona CurrentPersonality()
        {
            return PersonalityChatOptions.BotPersona;
        }

        public static PersonalityChatService GetPersonalityChatService()
        {
            return PersonalityChatService;
        }
    }
}
