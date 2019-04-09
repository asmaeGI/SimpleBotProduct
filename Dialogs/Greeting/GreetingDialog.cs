// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BasicBot.CogService;
using BasicBot.Dialogs;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;


namespace Microsoft.BotBuilderSamples
{
    /// <summary>
    /// Demonstrates the following concepts:
    /// - Use a subclass of ComponentDialog to implement a multi-turn conversation
    /// - Use a Waterflow dialog to model multi-turn conversation flow
    /// - Use custom prompts to validate user input
    /// - Store conversation and user state.
    /// </summary>
    public class GreetingDialog : ComponentDialog
    {
        // User state for greeting dialog
        private const string GreetingStateProperty = "greetingState";
        private const string NameValue = "greetingName";

        // Prompts names
        private const string NamePrompt = "namePrompt";
        private const string CategoriePrompt = "categoriePrompt";

        // Minimum length requirements for city and name
        private const int NameLengthMinValue = 3;

        // Dialog IDs
        private const string ProfileDialog = "profileDialog";
        /// <summary>
        /// Initializes a new instance of the <see cref="GreetingDialog"/> class.
        /// </summary>
        /// <param name="botServices">Connected services used in processing.</param>
        /// <param name="botState">The <see cref="UserState"/> for storing properties at user-scope.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> that enables logging and tracing.</param>
        public GreetingDialog(IStatePropertyAccessor<GreetingState> userProfileStateAccessor, ILoggerFactory loggerFactory)
            : base(nameof(GreetingDialog))
        {
            UserProfileAccessor = userProfileStateAccessor ?? throw new ArgumentNullException(nameof(userProfileStateAccessor));

            // Add control flow dialogs
            var waterfallSteps = new WaterfallStep[]
            {
                    InitializeStateStepAsync,
                    PromptForNameStepAsync,
                    PromptForCategorieAsync,
            };
            AddDialog(new WaterfallDialog(ProfileDialog, waterfallSteps));
            AddDialog(new TextPrompt(NamePrompt, ValidateName));
            AddDialog(new TextPrompt(CategoriePrompt));

        }


        public IStatePropertyAccessor<GreetingState> UserProfileAccessor { get; }

        private async Task<DialogTurnResult> InitializeStateStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var greetingState = await UserProfileAccessor.GetAsync(stepContext.Context, () => null);
            if (greetingState == null)
            {
                var greetingStateOpt = stepContext.Options as GreetingState;
                if (greetingStateOpt != null)
                {
                    await UserProfileAccessor.SetAsync(stepContext.Context, greetingStateOpt);
                }
                else
                {
                    await UserProfileAccessor.SetAsync(stepContext.Context, new GreetingState());
                }
            }

            return await stepContext.NextAsync();
        }

        private async Task<DialogTurnResult> PromptForNameStepAsync(
                                                WaterfallStepContext stepContext,
                                                CancellationToken cancellationToken)
        {
            var greetingState = await UserProfileAccessor.GetAsync(stepContext.Context);

            if (string.IsNullOrWhiteSpace(greetingState.Name))
            {
                await SpeechService.TextToSpeechAsync("Hi, What is your name?");

                // prompt for name, if missing
                var opts = new PromptOptions
                {
                    Prompt = new Activity
                    {
                        Type = ActivityTypes.Message,
                        Text = "What is your name?",
                    },
                };
                SpeechService.PlaySound();
                return await stepContext.PromptAsync(NamePrompt, opts);
            }
            else
            {
                return await stepContext.NextAsync();
            }
        }



        private async Task<DialogTurnResult> PromptForCategorieAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var greetingState = await UserProfileAccessor.GetAsync(stepContext.Context);
            var lowerCaseName = stepContext.Result as string;
            if (string.IsNullOrWhiteSpace(greetingState.Name) && lowerCaseName != null)
            {
                // Capitalize and set name.
                greetingState.Name = char.ToUpper(lowerCaseName[0]) + lowerCaseName.Substring(1);
                await UserProfileAccessor.SetAsync(stepContext.Context, greetingState);
            }
            PromptOptions opts =await CardOptions(greetingState);
            return await stepContext.PromptAsync(CategoriePrompt, opts);
        }

        private static async Task<PromptOptions> CardOptions(GreetingState greetingState)
        {
            List<CardAction> listCategories = await Categories();
            var card = new HeroCard()
            {
                //Buttons = new List<CardAction>()
                //{
                //    //new CardAction(ActionTypes.ImBack, title: "Clothing", value: "Clothing"),
                //    //new CardAction(ActionTypes.ImBack, title: "Shoes", value: "Shoes"),
                //    //new CardAction(ActionTypes.ImBack, title: "Accessories", value: "Accessories"),
                //},
                Buttons = listCategories,
            };
            var opts = new PromptOptions
            {

                Prompt = new Activity
                {
                    Text = $"Hello {greetingState.Name}, what are you looking for today?",
                    Type = ActivityTypes.Message,
                    Attachments = new List<Attachment>
                    {
                        card.ToAttachment(),
                    },
                },
            };
            return opts;
        }

        private static async Task<List<CardAction>> Categories()
        {
            var categories = await ApiServices.GetAllCategories();
            List<CardAction> categoriesCard = new List<CardAction>();
            foreach (var categorie in categories)
            {
                CardAction card = new CardAction(ActionTypes.ImBack, title: categorie.Name, value: categorie.Name);
                categoriesCard.Add(card);
            }

            return categoriesCard;
        }

        /// <summary>
        /// Validator function to verify if the user name meets required constraints.
        /// </summary>
        /// <param name="promptContext">Context for this prompt.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> that represents the work queued to execute.</returns>
        private async Task<bool> ValidateName(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            // Validate that the user entered a minimum length for their name.
            var value = promptContext.Recognized.Value?.Trim() ?? string.Empty;
            if (value.Length >= NameLengthMinValue)
            {
                promptContext.Recognized.Value = value;
                return true;
            }
            else
            {
                await promptContext.Context.SendActivityAsync($"Names needs to be at least `{NameLengthMinValue}` characters long.");
                return false;
            }
        }

    }
}
