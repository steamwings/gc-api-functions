using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json; // TODO Replace with System.Text.Json I think you just need this: 
using System.Net.Http;
using Functions.Authentication;
using System.Web;
using Common.Extensions;

namespace Functions.Utility
{
    public static class ValidateAddress
    {
        static readonly HttpClient client = new HttpClient();

        [FunctionName(nameof(ValidateAddress))]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] 
            HttpRequest req,
            ILogger log)
        {
            log.LogInformation($"Processing address validation request...");
            
            if (!AuthenticationHelper.Authorize(log, req.Headers, out var errorResponse))
                return errorResponse;

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

            if (log.NullWarning(new { theater, street, city }, out string nullNames))
            {
                return new BadRequestObjectResult($"Missing parameter(s) {nullNames}");
            }

            string searchUrl = "https://www.google.com/maps/search/" + HttpUtility.UrlEncode(theater);
            var mapsResponse = await client.GetAsync(searchUrl);
            string responseBody = await mapsResponse.Content.ReadAsStringAsync();

            if (!mapsResponse.IsSuccessStatusCode)
            {
                log.LogError($"Call to maps failed\nURL:[{searchUrl}]" +
                    $"\nMAPS RESPONSE:[Code:{mapsResponse.StatusCode},Reason:{mapsResponse.ReasonPhrase},Content:{mapsResponse.Content}]" +
                    $"\nINCOMING REQUEST HEADERS:[{req.Headers}]\nBODY:[{requestBody}]");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            var expected = $"{theater}, {street}, {city}" + (string.IsNullOrEmpty(state) ? "" : $", {state}");
            bool result = responseBody.Contains(expected);
            log.LogInformation($"Theater '{theater}' IS {(result ? "" : "NOT")} located near " +
                $"{street}, {city}{(String.IsNullOrEmpty(state) ? "" : (", " + state))}");
            return new OkObjectResult($"{result}");
     
        }
    }
}
