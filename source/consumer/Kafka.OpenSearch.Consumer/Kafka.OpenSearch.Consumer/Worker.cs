using System.Text.Json;
using Confluent.Kafka;
using Kafka.OpenSearch.Consumer.Models;
using OpenSearch.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

namespace Kafka.OpenSearch.Consumer;

public class Worker : BackgroundService
{
    private readonly IConfiguration _configuration;

    public Worker(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opensearchUrl = _configuration["OpenSearch:Url"];

        var openSearchClient = CreateOpenSearchClient(opensearchUrl);

        var wikimediaIndexName = _configuration["OpenSearch:Index"];

        var indexCreated = await CreateOpenSearchIndex(openSearchClient, wikimediaIndexName, stoppingToken);

        if (!indexCreated)
        {
            Console.WriteLine($"Failed to create index [{wikimediaIndexName}] in open search.");
            return;
        }

        var bootstrapServer = _configuration["Kafka:BootstrapServers"];
        var groupId = _configuration["Kafka:GroupId"];
        var topic = _configuration["Kafka:Topic"];

        var consumer = CreateKafkaConsumer(bootstrapServer, groupId, topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                Console.WriteLine("Polling..");

                var consumeResult = consumer.Consume(3000);

                if (consumeResult == null)
                {
                    Console.WriteLine("Nothing to process..");
                    continue;
                }

                Console.WriteLine(consumeResult.Offset.Value);

                WikimediatDetails wikimediaDetails = null;

                try
                {
                    Console.WriteLine($"Deserializing: {consumeResult.Message.Value}");
                    wikimediaDetails = JsonSerializer.Deserialize<WikimediatDetails>(consumeResult.Message.Value, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch (JsonException)
                {
                    Console.WriteLine($"Bad json recieved. Skipping. \n {consumeResult.Message.Value}");
                    continue;
                }

                var indexRequest = new IndexRequest<WikimediatDetails>(wikimediaDetails, wikimediaIndexName, wikimediaDetails.Meta.Id);

                var response = await openSearchClient.IndexAsync<WikimediatDetails>(indexRequest, stoppingToken);

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
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        consumer.Close();
    }

    private IConsumer<Ignore, string> CreateKafkaConsumer(string bootstrapServers, string groupId, string topic)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true,
            EnableAutoOffsetStore = false
        };

        var consumer = new ConsumerBuilder<Ignore, string>(config).Build();

        consumer.Subscribe(topic);

        return consumer;
    }

    private async Task<bool> CreateOpenSearchIndex(IOpenSearchClient openSearchClient, string indexName, CancellationToken cancellationToken)
    {
        var wikiMediaIndex = await openSearchClient.Indices.ExistsAsync(indexName, ct: cancellationToken);

        if (wikiMediaIndex == null || !wikiMediaIndex.Exists)
        {
            var response = await openSearchClient.Indices.CreateAsync(indexName, ct: cancellationToken);

            return response.IsValid;
        }

        return wikiMediaIndex.Exists;
    }

    private IOpenSearchClient CreateOpenSearchClient(string url)
    {
        return new OpenSearchClient(new Uri(url));
    }
}