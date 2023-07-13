using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using CustomerInfoCosmosDb.Common.Model;

namespace CustomerInfoCosmosDb.Functions
{
    public static class CreateCustomer
    {
        [FunctionName("CreateCustomer")]
        [OpenApiOperation(operationId: "Run", tags: new[] { "Create customer" })]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(Customer), Description = "The customer information")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "customers")]
            HttpRequest req,
         [CosmosDB(databaseName: "%DatabaseName%", containerName: "%CollectionNameAOW%", Connection = "CosmosDBConnection")]
            IAsyncCollector<Customer> appendOnlyWrites,
            ILogger log)
        {
            try
            {
                // Read the JSON request body
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                // Deserialize the JSON to Customer object
                var customer = JsonConvert.DeserializeObject<Customer>(requestBody);

                // Validate the customer object
                var validationResults = new List<ValidationResult>();
                bool isValid = Validator.TryValidateObject(customer, new ValidationContext(customer), validationResults, true);
                if (!isValid)
                {
                    return new BadRequestObjectResult(validationResults);
                }

                // Save the customer data to the append-only-writes container
                await appendOnlyWrites.AddAsync(customer);

                return new OkObjectResult("Customer created successfully");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "An error occurred while creating the customer");
                return new StatusCodeResult((int)HttpStatusCode.InternalServerError);
            }
        }
    }
}
