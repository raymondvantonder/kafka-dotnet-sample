using System.Text;
using System.Text.Json;
using Confluent.Kafka;

namespace Kafka.Producer;

public class WikiMediaEventStreamHandler
{
    private readonly Func<string, Task> _onMessageReceivedAction;
    private static string _dataField = "data: ";

    public WikiMediaEventStreamHandler(Func<string, Task> onMessageReceivedAction)
    {
        _onMessageReceivedAction = onMessageReceivedAction;
    }

    public async Task StartAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var response = await client.GetStreamAsync("https://stream.wikimedia.org/v2/stream/recentchange");

                    var reader = new StreamReader(response);

                    while (!reader.EndOfStream || !stoppingToken.IsCancellationRequested)
                    {
                        var line = await reader.ReadLineAsync();

                        if (!string.IsNullOrEmpty(line) && line.StartsWith(_dataField))
                        {
                            var jsonString = GetJsonData(line);

                            Console.WriteLine($"Sending data: {jsonString}");
                            await _onMessageReceivedAction(jsonString);

                            Console.WriteLine("----------------------------------------");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.WriteLine("----------------------------------------");
                Console.WriteLine("Restarting stream in 3 seconds");
                await Task.Delay(3000, stoppingToken);
            }
        }
    }

    private string GetJsonData(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return string.Empty;

        return message.Remove(0, _dataField.Length);
    }
}