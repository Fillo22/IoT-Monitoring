using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTMonitoring.Functions.Normalization.DataRectifier.Bll.Interfaces
{
    public interface INormalizationService
    {
        JObject Normalize(JObject input);
    }
}
