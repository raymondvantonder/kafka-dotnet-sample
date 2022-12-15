using System.Text.Json;
using Confluent.Kafka;
using Kafka.OpenSearch.Consumer.Models;
using OpenSearch.Client;
using Microsoft.Extensions.Hosting;

namespace Kafka.OpenSearch.Consumer;

public class Worker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var openSearchClient = CreateOpenSearchClient();

        string wikimediaIndexName = "wikimedia";

        var wikiMediaIndex = await openSearchClient.Indices.ExistsAsync(wikimediaIndexName, ct: stoppingToken);

        if (wikiMediaIndex == null || !wikiMediaIndex.Exists)
        {
            var response = await openSearchClient.Indices.CreateAsync(wikimediaIndexName, ct: stoppingToken);
        }

        var config = new ConsumerConfig
        {
            BootstrapServers = "127.0.0.1:9092",
            GroupId = "test-group",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true,
            EnableAutoOffsetStore = false
        };

        var consumer = new ConsumerBuilder<Ignore, string>(config).Build();

        consumer.Subscribe("wikimedia_data");

        

        while (!stoppingToken.IsCancellationRequested)
        {
            var consumeResult = consumer.Consume(3000);

            if (consumeResult == null)
            {
                Console.WriteLine("Nothing to process..");
                continue;
            }

            Console.WriteLine(consumeResult.Offset.Value);

            WikimediatDetails wikimediatDetails = null;

            try
            {
                wikimediatDetails = JsonSerializer.Deserialize<WikimediatDetails>(consumeResult.Message.Value, new JsonSerializerOptions { PropertyNameCaseInsensitive = true, NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.WriteAsString });
            }
            catch (Exception e)
            {
                Console.WriteLine($"Bad json recieved. Skipping. \n {consumeResult.Message.Value}");
                continue;
            }

            var response = await openSearchClient.IndexAsync<WikimediatDetails>(wikimediatDetails, i => i.Index(wikimediaIndexName), stoppingToken);

            if (!response.IsValid)
            {
                Console.WriteLine("Failed to send data to open search index.");
            }
            else
            {
                Console.WriteLine(response.Id);
            }

            consumer.StoreOffset(consumeResult);
        }

        consumer.Close();
    }

    private static IOpenSearchClient CreateOpenSearchClient()
    {
        return new OpenSearchClient(new Uri("http://localhost:9200"));
    }
}