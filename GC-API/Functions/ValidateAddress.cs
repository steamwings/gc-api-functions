using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace Functions
{
    public static class ValidateAddress
    {
        static readonly HttpClient client = new HttpClient();

        private static string QueryGet(IQueryCollection collection, string name)
        {
            return collection.ContainsKey(name) ? 
                System.Net.WebUtility.UrlDecode(collection[name]) 
                : null;
        }

        private static string MapsPrep(string val)
        {
            return val.Replace(' ', '+');
        }

        private static bool ContainsTheater(string src, string val)
        {
            return new Regex(src).IsMatch(val);
        }

        [FunctionName("ValidateAddress")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation($"Processing address validation request...");

            string theater = QueryGet(req.Query, "theater");
            string street = QueryGet(req.Query, "street");
            string city = QueryGet(req.Query, "city");
            string state = QueryGet(req.Query, "state");

            string bodyStr;
            dynamic data = null;
            using (var s = new StreamReader(req.Body))
            {
                data = JsonConvert.DeserializeObject(bodyStr = await s.ReadToEndAsync());
            }

            theater = theater ?? data?.theater;
            street = street ?? data?.street;
            city = city ?? data?.city;
            state = state ?? data?.state; //TODO This probably isn't needed

            if(theater is null || street is null || city is null)
            {
                log.LogDebug("Data is missing.");
                return new BadRequestObjectResult("Theater, street, and city must be in query string or JSON request body.");
            }

            string searchUrl = "https://www.google.com/maps/search/";
            searchUrl += MapsPrep(street) + ',';
            searchUrl += MapsPrep(city);
            if(!String.IsNullOrEmpty(state)) searchUrl += ',' + MapsPrep(state);
            searchUrl += "/";

            HttpResponseMessage mapsResponse = await client.GetAsync(searchUrl);
            string responseBody = await mapsResponse.Content.ReadAsStringAsync();

            if (!mapsResponse.IsSuccessStatusCode)
            {
                log.LogError($"Call to maps failed\nURL:[{searchUrl}]" +
                    $"\nMAPS RESPONSE:[Code:{mapsResponse.StatusCode},Reason:{mapsResponse.ReasonPhrase},Content:{mapsResponse.Content}]" +
                    $"\nINCOMING REQUEST HEADERS:[{req.Headers}]\nBODY:[{bodyStr}]");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            bool result = ContainsTheater(theater, responseBody);
            log.LogInformation($"Theater '{theater}' IS {(result ? "" : "NOT")} located near " +
                $"{street}, {city}{(String.IsNullOrEmpty(state) ? "" : (", " + state))}");
            return new OkObjectResult($"{result}");
     
        }
    }
}
