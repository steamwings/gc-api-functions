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
using Functions.Authentication;
using Functions.Extensions;

namespace Functions
{
    public static class ValidateAddress
    {
        static readonly HttpClient client = new HttpClient();

        [FunctionName("ValidateAddress")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] 
            HttpRequest req,
            ILogger log)
        {
            log.LogInformation($"Processing address validation request...");
            
            if (!AuthenticationHelper.Authorize(log, req.Headers, out var errorResponse))
            {
                return errorResponse;
            }

            // Try getting fields from query string
            string theater = req.Query?.GetQueryValue("theater");
            string street = req.Query?.GetQueryValue("street");
            string city = req.Query?.GetQueryValue("city");
            string state = req.Query?.GetQueryValue("state");

            // Also get the request body and try getting fields if they're null
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            theater ??= data?.theater;
            street ??= data?.street;
            city ??= data?.city;
            state ??= data?.state; //TODO This probably isn't needed

            // Obtuse null check
            string nullName = 0 switch
            {
                _ when theater is null => nameof(theater),
                _ when street is null => nameof(street),
                _ when city is null => nameof(city),
                _ => "none"
            };

            if (nullName != "none")
            {
                log.LogDebug($"Field {nullName} is missing.");
                return new BadRequestObjectResult($"{nullName} missing. Theater, street, and city must appear in query string or JSON request body.");
            }

            // Generate a search URL
            string searchUrl = "https://www.google.com/maps/search/";
            searchUrl += street + ',' + city;
            if (!string.IsNullOrEmpty(state)) 
                searchUrl += ',' + state;
            searchUrl = searchUrl.Replace(' ', '+') + "/";

            HttpResponseMessage mapsResponse = await client.GetAsync(searchUrl);
            string responseBody = await mapsResponse.Content.ReadAsStringAsync();

            if (!mapsResponse.IsSuccessStatusCode)
            {
                log.LogError($"Call to maps failed\nURL:[{searchUrl}]" +
                    $"\nMAPS RESPONSE:[Code:{mapsResponse.StatusCode},Reason:{mapsResponse.ReasonPhrase},Content:{mapsResponse.Content}]" +
                    $"\nINCOMING REQUEST HEADERS:[{req.Headers}]\nBODY:[{requestBody}]");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            bool result = responseBody.Contains(theater);
            log.LogInformation($"Theater '{theater}' IS {(result ? "" : "NOT")} located near " +
                $"{street}, {city}{(String.IsNullOrEmpty(state) ? "" : (", " + state))}");
            return new OkObjectResult($"{result}");
     
        }
    }
}
