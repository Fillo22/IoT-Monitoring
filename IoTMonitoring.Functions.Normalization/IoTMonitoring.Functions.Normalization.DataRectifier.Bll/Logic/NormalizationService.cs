using IoTMonitoring.Functions.Normalization.DataRectifier.Bll.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTMonitoring.Functions.Normalization.DataRectifier.Bll.Logic
{
    public class NormalizationService : INormalizationService
    {
        private readonly ILogger<NormalizationService> _Logger;

        public NormalizationService(ILogger<NormalizationService> logger)
        {
            _Logger = logger;
        }
        public JObject Normalize(JObject input)
        {
            JObject output = new();
            output.Add("date", DateTime.UtcNow.Ticks);
            output.Add("data", input);
            return output;
        }
    }
}
