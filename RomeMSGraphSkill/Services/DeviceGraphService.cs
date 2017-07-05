using Microsoft.IdentityModel.Protocols;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace RomeMSGraphSkill.Services
{
    public class DeviceGraphService
    {
        public async Task<Tuple<bool, List<UserDevice>>> GetDevicesAsync(string authAccessToken)
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