using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;
using Microsoft.Graph;
using Microsoft.Extensions.Options;
using System.IO;
using Newtonsoft.Json;
using System.Linq;
using MyProject.Shared.ViewModels;
using MyProject.Settings;

namespace MyProject.Function
{
    
    public class CheckUserExistence
    {
        private readonly AdminConfiguration adminSettings;

        public CheckUserExistence(IOptions<AdminConfiguration> adminSettings)
        {
            this.adminSettings = adminSettings.Value;
        }

        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [FunctionName("CheckUserExistence")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var adUser = JsonConvert.DeserializeObject<AdUserViewModel>(requestBody);
            log.LogInformation("Request body: " + requestBody);
            // If input data is null, show block page
            if (adUser == null)
            {
                return new OkObjectResult(new ResponseContent("ShowBlockPage", "There was a problem with your request."));
            }

            string tenantId = adminSettings.TenantId;
            string applicationId = adminSettings.ApplicationId;
            string clientSecret = adminSettings.ClientSecret;

            // If some configuration is missing, show block page
            if (string.IsNullOrEmpty(tenantId) ||
                string.IsNullOrEmpty(applicationId) ||
                string.IsNullOrEmpty(clientSecret))
            {
                return new OkObjectResult(new ResponseContent("ShowBlockPage", "There was a problem with your request."));
            }

            // If crawl handle claim not found, show block page
            if (string.IsNullOrEmpty(adUser.Crawlhandle))
            {
                return new BadRequestObjectResult(new ResponseContent("ShowBlockPage", requestBody));
            }

            // Initialize the client credential auth provider
            IConfidentialClientApplication confidentialClientApplication = ConfidentialClientApplicationBuilder
                .Create(applicationId)
                .WithTenantId(tenantId)
                .WithClientSecret(clientSecret)
                .Build();
            ClientCredentialProvider authProvider = new ClientCredentialProvider(confidentialClientApplication);

            // Set up the Microsoft Graph service client with client credentials
            GraphServiceClient graphClient = new GraphServiceClient(authProvider);

            // Grab the custom attribute name of the Crawlhandle user attribute in B2C app
            const string crawlhandle = "Crawlhandle";
            B2cCustomAttributeHelper helper = new B2cCustomAttributeHelper(adminSettings.ExtensionsAppId);
            string crawlhandleAttributeName = helper.GetCompleteAttributeName(crawlhandle);
            string userhandle = adUser.Crawlhandle.Trim().ToLower();
            try
            {
                // Check for existing users with the same crawlhandle
                var result = await graphClient.Users
                    .Request()
                    .Select($"id")
                    .Filter($"{crawlhandleAttributeName} eq '{userhandle}'")
                    .GetAsync();

                if (result.Count > 0)
                {
                    return new BadRequestObjectResult(new ResponseContent("ValidationError", "A user with this name handle already exists.", "400"));
                }
            }
            catch (Exception e)
            {
                log.LogError("Error executing MS Graph request: ", e);
                return new OkObjectResult(new ResponseContent("ShowBlockPage", "There was a problem with your request."));
            }

            // If all is OK, return 200 OK - Continue message
            return new OkObjectResult(new ResponseContent("Continue"));
        }
        
    }
    public class B2cCustomAttributeHelper
    {
        internal readonly string _b2cExtensionAppClientId;

        internal B2cCustomAttributeHelper(string b2cExtensionAppClientId)
        {
            _b2cExtensionAppClientId = b2cExtensionAppClientId.Replace("-", "");
        }

        internal string GetCompleteAttributeName(string attributeName)
        {
            if (string.IsNullOrWhiteSpace(attributeName))
            {
                throw new System.ArgumentException("Parameter cannot be null", nameof(attributeName));
            }

            return $"extension_{_b2cExtensionAppClientId}_{attributeName}";
        }
    }
}
