using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.IdentityModel.Protocols;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace RomeMSGraphSkill.Services
{
    public class DeviceGraphService
    {
        public async Task<Tuple<bool, List<UserDevice>>> GetDevicesAsync(IDialogContext context, string authAccessToken)
        {
            // Accessing the Devices graph can take a while the first time, so run a "working on it" progress 'bar' 
            // alongside to ensure Cortana doesn't die because it doesn't get a response in time
            Task[] tasks = new Task[2];
            tasks[0] = GetDevicesInternalAsync(authAccessToken);
            var cts = new CancellationTokenSource();
            tasks[1] = DoKeepAlive(context, cts.Token);

            await Task.WhenAny(tasks);

            // Cancel the KeepAlive when the first task completes
            cts.Cancel();

            return await (Task<Tuple<bool, List<UserDevice>>>)tasks[0];
        }

        private static async Task<Tuple<bool, List<UserDevice>>> GetDevicesInternalAsync(string authAccessToken)
        {
            string _restUrl = ConfigurationManager.AppSettings["MSGraphDevicesApiUrl"];

            // Use access token to get user info from Live API
            using (var client = new HttpClient())
            {
                // Pass the access_token in the Authorization header
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + authAccessToken);

                try
                {
                    var response = await client.GetAsync(_restUrl);
                    if (!response.IsSuccessStatusCode)
                    {
                        // API call failed
                        Trace.TraceInformation($"[REST API Failed]{response.ReasonPhrase}");
                        return new Tuple<bool, List<UserDevice>>(false, null);
                    }

                    var responseString = await response.Content.ReadAsStringAsync();

                    // Extract useful info from API response 
                    var userDevices = JsonConvert.DeserializeObject<UserDevicesRoot>(responseString);

                    return new Tuple<bool, List<UserDevice>>(true, userDevices.value.ToList());

                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public async Task<bool> CommandDeviceUriAsync(string authAccessToken, string id)
        {
            var restUri = new UriBuilder(ConfigurationManager.AppSettings["MSGraphDevicesApiUrl"]);
            restUri.Path += $"/{id}/commands";

            // Use access token to get user info from Live API
            using (var client = new HttpClient())
            {
                // Pass the access_token in the Authorization header
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + authAccessToken);

                try
                {
                    string postBody = @"{ ""type"": ""LaunchUri"", ""payload"": {""uri"": ""https://github.com/Microsoft/project-rome"" }}";
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var response = await client.PostAsync(restUri.Uri, new StringContent(postBody, Encoding.UTF8, "application/json"));

                    if (!response.IsSuccessStatusCode)
                    {
                        // API call failed
                        Trace.TraceInformation($"[REST API Failed]{response.ReasonPhrase}");
                        return false;
                    }

                    return true;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        private async Task DoKeepAlive(IDialogContext context, CancellationToken ct)
        {
            do
            {
                if (ct.IsCancellationRequested) break;
                await Task.Delay(5000);
                if (ct.IsCancellationRequested) break;

                await context.SayAsync($"Working on it...", $"Working on it", new MessageOptions() { InputHint = InputHints.IgnoringInput });

            } while (true);
        }
    }


    public class UserDevicesRoot
    {
        public string odatacontext { get; set; }
        public UserDevice[] value { get; set; }
    }

    public class UserDevice
    {
        public string id { get; set; }
        public string Name { get; set; }
        public string Manufacturer { get; set; }
        public string Model { get; set; }
        public string Kind { get; set; }
        public string Status { get; set; }
        public string Platform { get; set; }
    }

}