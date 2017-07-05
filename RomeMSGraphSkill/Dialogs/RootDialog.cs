using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System.Diagnostics;
using AuthBot;
using AuthBot.Dialogs;
using System.Threading;
using AuthBot.Models;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace RomeMSGraphSkill.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as Microsoft.Bot.Connector.Activity;
            activity.Text = activity.Text ?? string.Empty;

            await EnsureAuthentication(context, activity);

            //// check if the user said reset
            //if (activity.Text.ToLowerInvariant().StartsWith("reset"))
            //{
            //    // ask the user to confirm if they want to reset the counter
            //    var options = new PromptOptions<string>(prompt: "Are you sure you want to reset the count?",
            //        retry: "Didn't get that!", speak: "Do you want me to reset the counter?",
            //        retrySpeak: "You can say yes or no!",
            //        options: PromptDialog.PromptConfirm.Options,
            //        promptStyler: new PromptStyler());

            //    PromptDialog.Confirm(context, AfterResetAsync, options);

            //}
            //else


        }

        #region AuthBot
        private async Task EnsureAuthentication(IDialogContext context, Microsoft.Bot.Connector.Activity activity)
        {
            string token = null;
            if (activity.ChannelId.Equals("cortana", StringComparison.InvariantCultureIgnoreCase))
            {
                token = await context.GetAccessToken(string.Empty);
            }
            else
            {
                token = await context.GetAccessToken(AuthSettings.Scopes);
            }

            if (string.IsNullOrEmpty(token))
            {
                if (activity.ChannelId.Equals("cortana", StringComparison.InvariantCultureIgnoreCase))
                {
                    //Cortana channel uses Connected Service for authentication, it has to be authorized to use cortana, we should not get here...
                    throw new InvalidOperationException("Cortana channel has to be used with Connected Service Account");
                }
                else
                {
                    try
                    {
                        // Force user to sign in
                        await context.Forward(new AzureAuthDialog(AuthSettings.Scopes), this.ResumeAfterAuth, context.Activity, CancellationToken.None);
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }
            else
            {
                // calculate something for us to return
                int length = activity.Text.Length;

                // say reply to the user
                await context.SayAsync($"You sent {activity.Text} which was {length} characters", $" You said {activity.Text}", new MessageOptions() { InputHint = InputHints.AcceptingInput });
                context.Wait(MessageReceivedAsync);

                //await MessageReceivedAsync(context, Awaitable.FromItem<Microsoft.Bot.Connector.Activity>(activity));
                //await base.MessageReceived(context, Awaitable.FromItem<Microsoft.Bot.Connector.Activity>(activity));
            }
        }
        private async Task ResumeAfterAuth(IDialogContext context, IAwaitable<string> result)
        {
            var message = await result;
            await context.PostAsync(message);
            // Process again
            context.Wait(MessageReceivedAsync);
        }
        #endregion

    }
}