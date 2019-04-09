using BasicBot.Dialogs;
using BasicBot.Dialogs.Shoes;
using BasicBot.Model;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace BasicBot.Dialogs.Shoes
{
    /// <summary>
    /// Demonstrates the following concepts:
    /// - Use a subclass of ComponentDialog to implement a multi-turn conversation
    /// - Use a Waterflow dialog to model multi-turn conversation flow
    /// - Use custom prompts to validate user input
    /// - Store conversation and user state.
    /// </summary>
    public class ShoesDialog : ComponentDialog
    {
        // User state for greeting dialog
        private const string ProductStateProperty = "productState";

        // Dialog IDs
        private const string ProfileDialog = "profileDialog";
        private const string CategoriePrompt = "categoriePrompt";
        private const string PricePrompt = "pricePrompt";
        private const string ShoesListPrompt = "shoesListPrompt";
        private ProductState productState;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShoesDialog"/> class.
        /// </summary>
        /// <param name="botServices">Connected services used in processing.</param>
        /// <param name="botState">The <see cref="UserState"/> for storing properties at user-scope.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> that enables logging and tracing.</param>
        public ShoesDialog(IStatePropertyAccessor<ProductState> userProfileStateAccessor, ILoggerFactory loggerFactory)
            : base(nameof(ShoesDialog))
        {
            UserProfileAccessor = userProfileStateAccessor ?? throw new ArgumentNullException(nameof(userProfileStateAccessor));

            // Add control flow dialogs
            var waterfallSteps = new WaterfallStep[]
            {
                    InitializeStateStepAsync,
                    ShoesCategoriePromptAsync,
                    ShoesPricePromptAsync,
                    ShoesSuggestionPromptAsync,
                    GoodByeUser,
            };
            AddDialog(new WaterfallDialog(ProfileDialog, waterfallSteps));
            AddDialog(new TextPrompt(CategoriePrompt));
            AddDialog(new TextPrompt(PricePrompt));
            AddDialog(new TextPrompt(ShoesListPrompt));
            }

        public IStatePropertyAccessor<ProductState> UserProfileAccessor { get; }

        private async Task<DialogTurnResult> InitializeStateStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var productState = await UserProfileAccessor.GetAsync(stepContext.Context, () => null);
            if (productState == null)
            {
                var productStateOpt = stepContext.Options as ProductState;
                if (productStateOpt != null)
                {
                    await UserProfileAccessor.SetAsync(stepContext.Context, productStateOpt);
                }
                else
                {
                    await UserProfileAccessor.SetAsync(stepContext.Context, new ProductState());
                }
            }

            return await stepContext.NextAsync();
        }

        private async Task<DialogTurnResult> ShoesCategoriePromptAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //var dc = stepContext.Context.Activity;
            var productState = await UserProfileAccessor.GetAsync(stepContext.Context);

            if (productState.Categorie != null)
            {
                return await stepContext.NextAsync();
            }
            else
            {
                PromptOptions opts = CardShoesCategorie();
                return await stepContext.PromptAsync(CategoriePrompt, opts);
            }
        }

        private PromptOptions CardShoesCategorie()
        {
            var card = new HeroCard()
            {
                Buttons = new List<CardAction>()
                   {
                     new CardAction(ActionTypes.ImBack, title: "Sneakers", value: "Sneakers"),
                     new CardAction(ActionTypes.ImBack, title: "Loafers", value: "Loafers"),
                     new CardAction(ActionTypes.ImBack, title: "Boots", value: "Boots"),
                    },
            };
            var opts = new PromptOptions
            {
                Prompt = new Activity
                {
                    Text = $"Great what Kind of shoes?",
                    Type = ActivityTypes.Message,
                    Attachments = new List<Attachment>
                    {
                      card.ToAttachment(),
                    },
                },
            };
            return opts;
        }

        private async Task<DialogTurnResult> ShoesPricePromptAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var productState = await UserProfileAccessor.GetAsync(stepContext.Context);
            var dc = stepContext.Context.Activity;
            var lowerCaseCategorie =productState.Categorie;
            //if (dc.Text.Equals("Sneakers") || dc.Text.Equals("Loafers") || dc.Text.Equals("Boots"))
            //{
            //    lowerCaseCategorie = dc.Text;
            //}

            if (string.IsNullOrWhiteSpace(productState.Categorie))
            {
                productState.Categorie = char.ToUpper(lowerCaseCategorie[0]) + lowerCaseCategorie.Substring(1);
                await UserProfileAccessor.SetAsync(stepContext.Context, productState);
            }

            if (productState.PriceMin == 0 && productState.PriceMax == 0)
            {
                return await stepContext.PromptAsync(PricePrompt, CardServices.CardPrice());
            }
            else
            {
                return await stepContext.NextAsync();
            }
        }

        private async Task<DialogTurnResult> ShoesSuggestionPromptAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            productState = await UserProfileAccessor.GetAsync(stepContext.Context);
            var ShoesList = await ShoesSuggestionListAsync();
            //var dc = stepContext.Context.Activity;
            //var lowerCasePrice = stepContext.Result as string ;

            //if (string.IsNullOrWhiteSpace(productState.Categorie) && lowerCasePrice != null)
            //{
            //    productState.PriceMin = Convert.ToDouble(lowerCasePrice);
            //    await UserProfileAccessor.SetAsync(stepContext.Context, productState);
            //}

            var opts = new PromptOptions
            {
                Prompt = new Activity
                {
                    Text =ShoesList.Count==0? $"could not find anything" : $"What do you think of these?",
                    Type = ActivityTypes.Message,
                    Attachments =ShoesList,
                    AttachmentLayout = AttachmentLayoutTypes.Carousel,
                },
            };
            return await stepContext.PromptAsync(ShoesListPrompt, opts);
        }

        private async Task<DialogTurnResult> GoodByeUser(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var context = stepContext.Context;

            // Display their profile information and end dialog.
            await context.SendActivityAsync($"Good by!");
            return await stepContext.EndDialogAsync();
        }

        private async Task<List<Attachment>> ShoesSuggestionListAsync()
        {
            //var products = new List<Product> {
            //    new Product("adidas ", 220, "https://images-na.ssl-images-amazon.com/images/I/812Y0qDRtsL._AC_SR201,266_.jpg", "Sneakers"),
            //    new Product("PUMA ", 20, "https://images-na.ssl-images-amazon.com/images/I/71XRCCS3igL._AC_SR201,266_.jpg", "Sneakers"),
            //    new Product("NIKE ", 590, "https://images-na.ssl-images-amazon.com/images/I/41JtpXekzNL._AC_UL260_SR200,260_.jpg", "Sneakers"),
            //    new Product("adidas22 ", 30, "https://images-na.ssl-images-amazon.com/images/I/812Y0qDRtsL._AC_SR201,266_.jpg", "Sneakers"),
            //    new Product("adidas ", 560, "https://images-na.ssl-images-amazon.com/images/I/61XnSvWaj7L._AC_SR201,266_.jpg", "Sneakers"),

            //};
            var products = await ApiServices.GetProductByCategorie(productState);
            List<Attachment> attachments = new List<Attachment>();
            foreach (var p in products)
            {
                attachments.Add(CardProduct(p).ToAttachment());

                //if (productState.PriceMin != 0 && productState.PriceMax != 0)
                //{
                //    if (p.Price > productState.PriceMin && p.Price < productState.PriceMax)
                //    {
                //        attachments.Add(CardProduct(p).ToAttachment());
                //    }
                //}
                //else
                //{
                //    if (productState.PriceMax == 0)
                //    {
                //        if (p.Price > productState.PriceMin)
                //        {
                //            attachments.Add(CardProduct(p).ToAttachment());
                //        }
                //    }
                //    else
                //    {
                //        if (p.Price < productState.PriceMax)
                //        {
                //            attachments.Add(CardProduct(p).ToAttachment());
                //        }
                //    }
                //}
            }

            return attachments;
        }

        private HeroCard CardProduct(Product product)
        {
            return new HeroCard()
            {
                Title = product.Name,
                Subtitle = product.Price.ToString(),
                Text = product.Name,
                Images = new List<CardImage>
                    {
                       new CardImage(product.Image),
                    },
                Buttons = new List<CardAction>()
                   {
                       new CardAction(ActionTypes.ImBack, title: "Buy this item", value: "Buy"),
                       new CardAction(ActionTypes.ImBack, title: "See more like this", value: "More"),
                       new CardAction(ActionTypes.ImBack, title: "Ask a question", value: "Question"),
                    },
            };
        }
    }
}
