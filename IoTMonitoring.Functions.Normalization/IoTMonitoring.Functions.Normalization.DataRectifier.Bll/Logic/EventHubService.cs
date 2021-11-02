using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using IoTMonitoring.Functions.Normalization.DataRectifier.Bll.Interfaces;
using IoTMonitoring.Functions.Normalization.DataRectifier.Bll.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTMonitoring.Functions.Normalization.DataRectifier.Bll.Logic
{
    public class EventHubService : IStreamingService
    {
        private readonly EventHubServiceOptions _Options;
        private readonly ILogger<EventHubService> _Logger;
        private EventHubProducerClient _ProducerClient;
        private Dictionary<string, EventDataBatch> _EventBatches = new();

        public EventHubService(IOptions<EventHubServiceOptions> options, ILogger<EventHubService> logger)
        {
            _Options = options.Value;
            _Logger = logger;
            Init();
        }

        public async Task SendMessageAsync(string partitionKey, JObject message)
        {
            if(!_EventBatches.Where(x => x.Key == partitionKey).Any())
            {
                _EventBatches.Add(partitionKey, await _ProducerClient.CreateBatchAsync(new CreateBatchOptions() { PartitionKey = partitionKey }));
            }

            if (!_EventBatches.Where(x => x.Key == partitionKey).FirstOrDefault().Value.TryAdd(new EventData(message.ToString())))
            {
                _Logger.LogError("can't add message to EventBatch");
            };

            foreach (var keyValuePair in _EventBatches.Where(x => x.Value.Count >= _Options.BatchLimit))
            {
                await _ProducerClient.SendAsync(keyValuePair.Value);
                _EventBatches.Remove(keyValuePair.Key);
            }
            
        }

        private void Init()
        {
            _ProducerClient = new EventHubProducerClient(_Options.ConnectionString, _Options.EventHubName); 
        }

        //@TODO: add checkpoint at 10 messages
    }
}
