using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AddonProcurement.Helpers;

namespace AddonProcurement.Extention
{
    public static class CoreAPI
    {
        public static string CallTokenAPI<T>(T requestData)
        {
            string respones = null;
            int maxRetries = 3;
            string apiUrl = System.Web.Configuration.WebConfigurationManager.AppSettings["BaseGetTokenUrl"];
            string grant_type = System.Web.Configuration.WebConfigurationManager.AppSettings["grant_type"];
            string client_id = System.Web.Configuration.WebConfigurationManager.AppSettings["client_id"];
            string client_secret = System.Web.Configuration.WebConfigurationManager.AppSettings["client_secret"];
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    Console.WriteLine($"Start Call API {apiUrl}: " + DateTime.Now);
                    HttpClient client = new HttpClient();
                    client.Timeout = TimeSpan.FromMinutes(30);
                    var Content = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("grant_type", grant_type),
                        new KeyValuePair<string, string>("client_id", client_id),
                         new KeyValuePair<string, string>("client_secret", client_secret),
                    });
                    Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");
                    var response = client.PostAsync(apiUrl, Content);
                    response.Wait();
                    var result = response.Result.Content.ReadAsStringAsync().Result;
                    WriteLogFile.LogAddon($"Call API:{apiUrl} SUCCESS");
                    Console.WriteLine($"Call API:{apiUrl} SUCCESS");
                    respones = result;
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to connect to the database (Attempt {attempt}/{maxRetries}): {ex.Message}");
                    WriteLogFile.LogAddon($"Failed to connect to the database (Attempt {attempt}/{maxRetries}): {ex.Message}");
                    if (attempt < maxRetries)
                    {
                        Console.WriteLine("Retrying...");
                        WriteLogFile.LogAddon("Retrying...");
                    }
                    else
                    {
                        Console.WriteLine("Maximum retry attempts reached.");
                        WriteLogFile.LogAddon("Maximum retry attempts reached.");
                        WriteLogFile.LogAddon(ex.ToString());
                    }
                }
            }
            return respones;

        }
    }
}
