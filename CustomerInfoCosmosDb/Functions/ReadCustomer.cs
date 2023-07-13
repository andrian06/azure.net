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

namespace CustomerInfoCosmosDb.Functions
{
    public static class ReadCustomer
    {
        [FunctionName("ReadCustomer")]
        [OpenApiOperation(operationId: "Run", tags: new[] { "Read customer" })]
        [OpenApiParameter(name: "customerId", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The customer ID")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(Customer), Description = "The OK response")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = "Customer not found")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "customers/{customerId}")] HttpRequest req,
            string customerId,
            [CosmosDB(databaseName: "%DatabaseName%", containerName: "%CollectionNameAOW%", Connection = "CosmosDBConnection", Id = "{customerId}", PartitionKey = "{customerId}")]
            Customer customer,
            ILogger log)
        {
            try
            {
                if (customer == null)
                {
                    return new NotFoundResult();
                }

                return new OkObjectResult(customer);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "An error occurred while reading the customer");
                return new StatusCodeResult((int)HttpStatusCode.InternalServerError);
            }
        }
    }
}
