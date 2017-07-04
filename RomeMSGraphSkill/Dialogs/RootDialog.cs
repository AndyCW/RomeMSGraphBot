using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

namespace RomeMSGraphSkill.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        private int count;

        public Task StartAsync(IDialogContext context)
        {
            this.count = 0;
            context.Wait(MessageReceivedAsync);
            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as Activity;
            activity.Text = activity.Text ?? string.Empty;

            // check if the user said reset
            if (activity.Text.ToLowerInvariant().StartsWith("reset"))
            {
                // ask the user to confirm if they want to reset the counter
                var options = new PromptOptions<string>(prompt: "Are you sure you want to reset the count?",
                    retry: "Didn't get that!", speak: "Do you want me to reset the counter?",
                    retrySpeak: "You can say yes or no!",
                    options: PromptDialog.PromptConfirm.Options,
                    promptStyler: new PromptStyler());

                PromptDialog.Confirm(context, AfterResetAsync, options);

            }
            else
            {
                // calculate something for us to return
                int length = activity.Text.Length;

                // increment the counter
                this.count++;

                // say reply to the user
                await context.SayAsync($"{count}: You sent {activity.Text} which was {length} characters", $"{count}: You said {activity.Text}", new MessageOptions() { InputHint = InputHints.AcceptingInput });
                context.Wait(MessageReceivedAsync);
            }

        }

        public async Task AfterResetAsync(IDialogContext context, IAwaitable<bool> argument)
        {
            var confirm = await argument;
            // check if user wants to reset the counter or not
            if (confirm)
            {
                this.count = 1;
                await context.SayAsync("Reset count.", "I reset the counter for you!");
            }
            else
            {
                await context.SayAsync("Did not reset count.", $"Counter is not reset. Current value: {this.count}");
            }
            context.Wait(MessageReceivedAsync);
        }
    }
}