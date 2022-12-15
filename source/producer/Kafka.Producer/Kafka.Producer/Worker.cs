using System;
using Confluent.Kafka;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

namespace Kafka.Producer
{
	public class Worker : BackgroundService
	{
        private static IProducer<Null, string> _producer;
        private readonly IConfiguration _configuration;

        public Worker(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _producer = CreateWikiMediatProducer(_configuration["Kafka:BootstrapServers"]);

            var wikiMediaEventStreamHandler = new WikiMediaEventStreamHandler(PublishToKafka);

            await wikiMediaEventStreamHandler.StartAsync(stoppingToken);
        }

        private async Task PublishToKafka(string message)
        {
            var deliveryResult = await _producer.ProduceAsync(_configuration["Kafka:Topic"], new Message<Null, string>() { Value = message }, CancellationToken.None);

            Console.WriteLine($"Delivery status: {deliveryResult.Status}");
        }

        private IProducer<Null, string> CreateWikiMediatProducer(string bootstrapServers)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = bootstrapServers,
                Acks = Acks.All,
                EnableIdempotence = true,
                MessageSendMaxRetries = int.MaxValue,
                CompressionType = CompressionType.Snappy,
                LingerMs = 20,
                BatchSize = 32 * 1024
            };

            return new ProducerBuilder<Null, string>(config)
                .SetLogHandler((_, message) => Console.WriteLine($"Kafka log: {message}"))
                .SetErrorHandler((_, error) => Console.WriteLine($"Kafka error: {JsonSerializer.Serialize(error)}"))
                .Build();
        }
    }
}

