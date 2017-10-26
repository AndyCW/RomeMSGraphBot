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
using RomeMSGraphSkill.Helpers;
using RomeMSGraphSkill.Services;
using System.Collections.Generic;
using System.Text;

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

        }

        #region AuthBot
        private async Task EnsureAuthentication(IDialogContext context, Microsoft.Bot.Connector.Activity activity)
        {
            string token = null;
            if (activity.ChannelId.Equals("cortana", StringComparison.InvariantCultureIgnoreCase))
            {
                // Call extension method to extract token from cortana Connected Service
                token = activity.AuthorizationToken();
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
                await context.SayAsync($"Discovering your devices...", $" Discovering your devices...", new MessageOptions() { InputHint = InputHints.IgnoringInput });

                var devicesResponse = await new DeviceGraphService().GetDevicesAsync(context, token);
                if (devicesResponse.Item1)
                {
                    List<UserDevice> userDevices = new List<UserDevice>();
                    foreach (var item in devicesResponse.Item2)
                    {
                        // Exclude devices that are not "online" and that are "CortanaSdk" (currently only Invoke devices)
                        if (item.Status.ToLower() == "online" && item.Platform != "CortanaSdk")
                        {
                            userDevices.Add(item);
                        }
                    }
                    context.ConversationData.SetValue<List<UserDevice>>("Devices", userDevices);
                    context.ConversationData.SetValue<string>("AuthToken", token);

                    // say reply to the user
                    await context.SayAsync($"I found {devicesResponse.Item2.Count} devices, {userDevices.Count} of which are online", $" I found {devicesResponse.Item2.Count} devices, {userDevices.Count} of which are online", new MessageOptions() { InputHint = InputHints.IgnoringInput });

                    // Array of strings for the PromptDialog.Choice buttons - though note these are not spoken, just shown on channels with UI
                    var descriptions = new List<string>();
                    // Define the device list choices, plus synonyms for each choice 
                    var choices = new Dictionary<string, IReadOnlyList<string>>();

                    int counter = 1;
                    foreach (var userDevice in userDevices)
                    {
                        descriptions.Add($"{counter}. {userDevice.Name}");
                        choices.Add(counter.ToString(), new List<string> { userDevice.Name, userDevice.Name.ToLowerInvariant() });
                        counter++;
                    }

                    // Additional speech for devices with no screen TODO
                    if (activity.ChannelId.Equals("cortana", StringComparison.InvariantCultureIgnoreCase))
                    {
                        counter = 1;
                        StringBuilder devicesPrompt = new StringBuilder();
                        foreach (var userDevice in userDevices)
                        {
                            devicesPrompt.Append($"{counter}, {userDevice.Kind}, {userDevice.Model}. ");
                            counter++;
                        }
                        await context.SayAsync(devicesPrompt.ToString(), devicesPrompt.ToString(), new MessageOptions() { InputHint = InputHints.IgnoringInput });
                    }

                    var promptOptions = new PromptOptionsWithSynonyms<string>(
                        prompt: "Choose one on which to launch the website", // prompt is not spoken
                        choices: choices,
                        descriptions: descriptions,
                        speak: ($"Which one do you want to launch the website on?"));

                    PromptDialog.Choice(context, DeviceChoiceReceivedAsync, promptOptions);

                }
                else
                {
                    // say reply to the user
                    await context.SayAsync($"I couldn't get your devices", $" I couldn't get your devices", new MessageOptions() { InputHint = InputHints.IgnoringInput });
                    context.Wait(MessageReceivedAsync);
                }
            }
        }

        private async Task DeviceChoiceReceivedAsync(IDialogContext context, IAwaitable<string> result)
        {
            int choiceIndex = 0;
            int.TryParse(await result, out choiceIndex);

            List<UserDevice> deviceList;
            if (context.ConversationData.TryGetValue<List<UserDevice>>("Devices", out deviceList))
            {
                var device = deviceList[choiceIndex - 1];

                await context.SayAsync($"You chose {device.Name}", $" You chose {device.Name}", new MessageOptions() { InputHint = InputHints.IgnoringInput });

                await context.SayAsync($"Launching Rome website on {device.Name}", $" Launching Rome website on {device.Name}", new MessageOptions() { InputHint = InputHints.AcceptingInput });

                string authToken;
                context.ConversationData.TryGetValue<string>("AuthToken", out authToken);
                await new DeviceGraphService().CommandDeviceUriAsync(authToken, device.id);
            }


            context.Wait(MessageReceivedAsync);
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