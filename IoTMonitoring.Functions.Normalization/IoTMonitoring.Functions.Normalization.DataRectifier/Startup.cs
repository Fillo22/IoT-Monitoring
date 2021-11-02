using IoTMonitoring.Functions.Normalization.DataRectifier.Bll.Interfaces;
using IoTMonitoring.Functions.Normalization.DataRectifier.Bll.Logic;
using IoTMonitoring.Functions.Normalization.DataRectifier.Bll.Options;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[assembly: FunctionsStartup(typeof(IoTMonitoring.Functions.Normalization.DataRectifier.Startup))]


namespace IoTMonitoring.Functions.Normalization.DataRectifier
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddOptions<EventHubServiceOptions>()
                .Configure<IConfiguration>((settings, configuration) =>
                {
                    configuration.GetSection("EventHubService").Bind(settings);
                });

            builder.Services.AddSingleton<INormalizationService, NormalizationService>();
            builder.Services.AddSingleton<IStreamingService, EventHubService>();
        }
    }
}
