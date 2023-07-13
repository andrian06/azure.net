using CustomerInfoCosmosDb.Common.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace CustomerInfoCosmosDb.Functions
{
    public static class UpdateCustomer
    {
        [FunctionName("UpdateCustomer")]
        [OpenApiOperation(operationId: "Run", tags: new[] { "Update customer" })]
        [OpenApiParameter(name: "customerId", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The customer ID")]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(Customer), Description = "The updated customer information")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "customers/{customerId}")] HttpRequest req,
            string customerId,
            [CosmosDB(databaseName: "%DatabaseName%", containerName: "%CollectionNameAOW%", Connection = "CosmosDBConnection",
                Id = "{customerId}", PartitionKey = "{customerId}")] Customer customer,
            [CosmosDB(databaseName: "%DatabaseName%", containerName: "%CollectionNameAOW%", Connection = "CosmosDBConnection")]
            IAsyncCollector<Customer> appendOnlyWrites,
            ILogger log)
        {
            try
            {
                // Basic input validation
                if (string.IsNullOrEmpty(customerId))
                {
                    return new BadRequestObjectResult("Invalid customer ID");
                }

                // Check if the customer exists
                if (customer == null)
                {
                    return new NotFoundResult();
                }

                // Read the JSON request body
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                // Deserialize the JSON to Customer object
                var updatedCustomer = JsonConvert.DeserializeObject<Customer>(requestBody);

                // Update the customer data
                customer.FirstName = updatedCustomer.FirstName;
                customer.LastName = updatedCustomer.LastName;
                customer.BirthdayInEpoch = updatedCustomer.BirthdayInEpoch;
                customer.Email = updatedCustomer.Email;

                // Save the updated customer data to the append-only-writes container
                await appendOnlyWrites.AddAsync(customer);

                return new OkObjectResult("Customer updated successfully");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "An error occurred while updating the customer");
                return new StatusCodeResult((int)HttpStatusCode.InternalServerError);
            }
        }
    }
}
