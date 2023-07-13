using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Messaging.EventGrid;
using CustomerInfoCosmosDb.Common.Model;
using Microsoft.Azure.Documents;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CustomerInfoCosmosDb.Functions
{
    public static class CustomerInfoEventPublisher
    {
        [FunctionName("CustomerInfoEventPublisher")]
        public static async Task RunAsync(
            [CosmosDBTrigger(
                databaseName: "%DatabaseName%",
                containerName: "%CollectionNameAOW%",
                Connection = "CosmosDBConnection",
                LeaseContainerName = "leases",
                CreateLeaseContainerIfNotExists = true)] IReadOnlyList<Document> documents,
            [CosmosDB(
                databaseName: "%DatabaseName%",
                containerName: "%CollectionNameMV%",
                Connection = "CosmosDBConnection")]
            IAsyncCollector<Customer> materializedViewCollection,
            ILogger log,
            ExecutionContext context) // Add ExecutionContext parameter
        {
            try
            {
                if (documents != null && documents.Count > 0)
                {
                    // Load configuration from local.settings.json
                    var config = new ConfigurationBuilder()
                        .SetBasePath(context.FunctionAppDirectory)
                        .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                        .AddEnvironmentVariables()
                        .Build();

                    // Get the value of topicEndpoint from configuration
                    string topicEndpoint = config["topicEndpoint"];

                    foreach (var document in documents)
                    {
                        var customer = JsonConvert.DeserializeObject<Customer>(document.ToString());
                        await materializedViewCollection.AddAsync(customer);
                        log.LogInformation($"Inserted customer id: {customer.Id}");

                        // Creating client to publish events to event grid topic
                        EventGridPublisherClient client = new EventGridPublisherClient(new Uri(topicEndpoint), new DefaultAzureCredential());
                        // Creating a sample event with Subject, Eventtype, dataVersion, and data
                        EventGridEvent egEvent = new EventGridEvent("Customer Transaction", "CustomerInfoEventSub", "1.0", customer);
                        // Send the event
                        await client.SendEventAsync(egEvent);
                        log.LogInformation($"Processed customer id: {customer.Id}");
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error publishing customer changes to Event Grid");
            }
        }
    }
}
