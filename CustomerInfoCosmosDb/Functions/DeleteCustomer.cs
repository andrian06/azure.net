using CustomerInfoCosmosDb.Common.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Net;
using System.Threading.Tasks;

namespace CustomerInfoCosmosDb.Functions
{
    public static class DeleteCustomer
    {
        [FunctionName("DeleteCustomer")]
        [OpenApiOperation(operationId: "Run", tags: new[] { "Delete customer" })]
        [OpenApiParameter(name: "customerId", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The customer ID")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "customers/{customerId}")] HttpRequest req,
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

                // Mark the customer as deleted (soft delete)
                customer.FirstName = null;
                customer.LastName = null;
                customer.BirthdayInEpoch = 0;
                customer.Email = null;

                // Save the updated customer data (deletion) to the append-only-writes container
                await appendOnlyWrites.AddAsync(customer);

                return new OkObjectResult("Customer deleted successfully");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "An error occurred while deleting the customer");
                return new StatusCodeResult((int)HttpStatusCode.InternalServerError);
            }
        }
    }
}
