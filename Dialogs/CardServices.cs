using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace BasicBot.Dialogs
{
    
    public static class CardServices
    {
        public static PromptOptions CardPrice()
        {
            var card = new HeroCard()
            {
                Buttons = new List<CardAction>()
                   {
                     new CardAction(ActionTypes.ImBack, title: "Under $75", value: "Under $75"),
                     new CardAction(ActionTypes.ImBack, title: "$75 to $250", value: "$75 to $250"),
                     new CardAction(ActionTypes.ImBack, title: "Over $250", value: "Over $250"),
                    },
            };
            var opts = new PromptOptions
            {
                Prompt = new Activity
                {
                    Text = $"Got it. What price range?",
                    Type = ActivityTypes.Message,
                    Attachments = new List<Attachment>
                    {
                      card.ToAttachment(),
                    },
                },
            };
            return opts;
        }
    }
}
