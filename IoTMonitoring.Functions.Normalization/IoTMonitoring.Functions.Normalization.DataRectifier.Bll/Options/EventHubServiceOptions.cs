using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTMonitoring.Functions.Normalization.DataRectifier.Bll.Options
{
    public class EventHubServiceOptions
    {

        public string ConnectionString { get; set; }
        public string EventHubName { get; set;}
        public int BatchLimit { get; set; }

        public EventHubServiceOptions()
        {

        }
    }
}
