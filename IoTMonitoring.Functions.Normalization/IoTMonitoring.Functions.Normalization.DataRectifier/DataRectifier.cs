using IoTHubTrigger = Microsoft.Azure.WebJobs.EventHubTriggerAttribute;

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System.Text;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using IoTMonitoring.Functions.Normalization.DataRectifier.Bll.Interfaces;
using Newtonsoft.Json.Linq;

namespace IoTMonitoring.Functions.Normalization.DataRectifier
{
    public class DataRectifier
    {
        private readonly IStreamingService _StreamingService;
        private readonly INormalizationService _NormalizationService;
        private readonly ILogger<DataRectifier> _Logger;
        private List<JObject> _OutputMessages = new();

        public DataRectifier(IStreamingService streamingService, INormalizationService normalizationService, ILogger<DataRectifier> logger)
        {
            _StreamingService = streamingService;
            _NormalizationService = normalizationService;
            _Logger = logger;
        }

        [FunctionName("DataRectifier")]
        public async Task Run(
            [IoTHubTrigger("%IoTHubName%", Connection = "IoTHubConnectionString", ConsumerGroup = "%IoTHubConsumerGroup%")] EventData[] messages)
        {
            
            foreach (var item in messages)
            {
                string partiotionKey = item.SystemProperties["iothub-connection-device-id"].ToString();
                if (Encoding.UTF8.GetString(item.EventBody).StartsWith("["))
                {
                    var messageArray = JArray.Parse(Encoding.UTF8.GetString(item.EventBody)).ToObject<List<JObject>>();
                    foreach (JObject message in messageArray)
                    {
                        await _StreamingService.SendMessageAsync(partiotionKey, InsertStandardInfo(message, partiotionKey));
                        _Logger.LogInformation($"Message sent with pk: {partiotionKey}");
                    }
                    return;
                }
                await _StreamingService.SendMessageAsync(partiotionKey, InsertStandardInfo(JObject.Parse(Encoding.UTF8.GetString(item.EventBody)), item.SystemProperties["iothub-connection-device-id"].ToString()));
                _Logger.LogInformation($"Message sent with pk: {partiotionKey}");
            }
        }

        private JObject InsertStandardInfo(JObject input, string partitionKey)
        {
            input.Add("pk", partitionKey);
            input.Add("sn", partitionKey);
            input.Add("mt", "tel");
            input = _NormalizationService.Normalize(input);

            return input;
        }
    }
}