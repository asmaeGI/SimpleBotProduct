using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BasicBot.Dialogs.Clothing
{
    public class ClothingDialog:ComponentDialog
    {
        private const string ProfileDialog = "profileDialog";

        public ClothingDialog(IStatePropertyAccessor<ClothingState> userProfileStateAccessor, ILoggerFactory loggerFactory)
            : base(nameof(ClothingDialog))
        {
            UserProfileAccessor = userProfileStateAccessor ?? throw new ArgumentNullException(nameof(userProfileStateAccessor));

            // Add control flow dialogs
            var waterfallSteps = new WaterfallStep[]
            {
                    InitializeStateStepAsync,
                    GoodByeUser,
            };
            AddDialog(new WaterfallDialog(ProfileDialog, waterfallSteps));
        }

        public IStatePropertyAccessor<ClothingState> UserProfileAccessor { get; }

        private async Task<DialogTurnResult> InitializeStateStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var clothingState = await UserProfileAccessor.GetAsync(stepContext.Context, () => null);
            //if (clothingState == null)
            //{
            //    var clothingStateOpt = stepContext.Options as ClothingState;
            //    if (clothingStateOpt != null)
            //    {
            //        await UserProfileAccessor.SetAsync(stepContext.Context, clothingStateOpt);
            //    }
            //    else
            //    {
            //        await UserProfileAccessor.SetAsync(stepContext.Context, new ClothingState());
            //    }
            //}

            return await stepContext.NextAsync();
        }

        private async Task<DialogTurnResult> GoodByeUser(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var context = stepContext.Context;

            // Display their profile information and end dialog.
            await context.SendActivityAsync($"Can I help you with something else ?");
            return await stepContext.EndDialogAsync();
        }
    }
}
