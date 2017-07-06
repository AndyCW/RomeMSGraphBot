using Microsoft.Bot.Connector;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Web;

namespace RomeMSGraphSkill.Helpers
{
    public static class ActivityExtension
    {
        public static string AuthorizationToken(this Microsoft.Bot.Connector.Activity activity)
        {
            if (activity.ChannelId.Equals("cortana", StringComparison.InvariantCultureIgnoreCase))
            {
                //Cortana channel uses Connected Service for authentication
                var msg = activity as IMessageActivity;

                    var tokenEntity = msg.AsMessageActivity().Entities.Where(e => e.Type.Equals("AuthorizationToken")).SingleOrDefault();
                if (tokenEntity == null)
                {
                    return string.Empty;
                }
                else
                {
                    var token = tokenEntity?.Properties.Value<string>("token");
                    return token;
                }
            }
            else
            {
                return string.Empty;
            }
        }


        public static Tuple<double, double> GetLocation(this Microsoft.Bot.Connector.Activity activity)
        {
            var userInfo = activity.AsMessageActivity().Entities.Where(e => e.Type.Equals("UserInfo")).SingleOrDefault();
            if (userInfo == null)
            {
                return null;
            }
            dynamic location = userInfo.Properties.Value<JObject>("Location");

            return new Tuple<double, double>(
                        (double)location.Hub.Latitude,
                        (double)location.Hub.Longitude);
        }
    }
}