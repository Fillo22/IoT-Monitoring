# IoT-Monitoring

- [IoT-Monitoring](#iot-monitoring)
  - [Description](#description)
    - [Requirements](#requirements)
  - [Architecture](#architecture)
    - [Schema](#schema)
    - [Reasons for selecting this architecture](#reasons-for-selecting-this-architecture)
      - [Storage component](#storage-component)
      - [Visualization component](#visualization-component)
      - [Integration component](#integration-component)
  - [Build IoT Edge modules](#build-iot-edge-modules)
  - [Prometheus Node Exporter installation](#prometheus-node-exporter-installation)
  - [View the Grafana dashboard](#view-the-grafana-dashboard)
  - [View the Node red flow](#view-the-node-red-flow)

## Description

### Requirements
## Architecture
### Schema 
![](media/Diagram%20v.2.6.png)
1. Data generation
    (A) The [OPC-UA Publisher](https://github.com/Azure/iot-edge-opc-publisher) module provided by Microsoft's Industrial IoT team, reads OPC-UA data from the simulator and publish it to IoT Edge (via edgeHub)
    (B) Temperature Simulator periodically generates fake temperature, pressure and humidity data
2. Node-RED modules collects data from OPC Publisher (via edgeHub) and Temperature simulator and:
    (A) writes them into InfluxDB which stores data in time series structure and provides it to Grafana for dashboarding
    (B) Sends them to IoT Hub via EdgeHub
3. Azure Function IoT Hub triggered, performs data normalization and sends messages to Event Hub partitioned by DeviceID
4. Azure Data Explorer ingest data from Event Hub and maps them to RawTelemetry table.
   
    Then the data are divided into two tables used later for dashboarding and data analysis:
    * AssetTelemetry: Opc Ua Simulator data
    * TemperatureTelemetry: Temperature Simulator data

5. The [metrics-collector module](https://aka.ms/edgemon-metrics-collector) is a Microsoft-supplied IoT Edge module that collects workload module metrics and send them to Log Analytics.

### Reasons for selecting this architecture

The main purpose of this solution is to provide an ability for local operators to view dashboards at the edge regardless of device connectivity. To support dashboarding however, there was a need to also select both a storage component as well as a visualization component. 

#### Storage component

Several storage solutions were reviewed and the team selected InfluxDB for the following reasons:

* Influx DB is a time series DB and as such is a natural fit for telemetry data from devices
* Open-source with a large community following
* Supports plugin to Grafana
* Node-RED libraries for easy integration
* Quick time to value and can be deployed as a Docker container
* Ranked #1 for time series DBs according to [DB-Engines](https://db-engines.com/en/system/InfluxDB)

Although InfluxDB was chosen to support storage, other DBs were considered and could potentially be used as well. For example, [Graphite](http://graphiteapp.org/), [Prometheus](https://prometheus.io) and [Elasticsearch](https://www.elastic.co/de/) were also considered. [Azure Time Series Insights](https://azure.microsoft.com/en-us/services/time-series-insights) was also considered but at the time of this activity was not yet available on Azure IoT Edge.

#### Visualization component

Several visualization solutions were reviewed and the team selected Grafana for the following reasons:

* Open-source with a large community following
* This particular use case covers metric analysis vs log analysis
* Flexibility with support for a wide array of plugins to different DBs and other supporting tools
* Allows you to share dashboards across an organization
* Quick time to value and can be deployed as a Docker container

Although Grafana was chosen to support visualization and dashboarding, other tools were considered and could potentially be used as well. For example, [Kibana](https://www.elastic.co/kibana) may be a better fit for visualization and analyzing of log files and is a natural fit if working with Elasticsearch. [Chronograf](https://www.influxdata.com/time-series-platform/chronograf) was considered but was limited to InfluxDB as a data source. [PowerBI Report Server](https://powerbi.microsoft.com/en-us/report-server/) was also investigated, but lack of support for being able to containerize the PowerBI Report Server meant it could not be used directly with Azure IoT Edge. Additionally, PowerBI Report Server does not support the real-time "live" dashboarding required for this solution.

#### Integration component

Node-RED was chosen as the tool to ease integration between IoT Edge and InfluxDB. Although the integration component could be written in several programming languages and containerized, Node-RED was selected for the following reasons:

* Open-source with a large community following
* Readily available [nodes](https://flows.nodered.org/node/node-red-contrib-azure-iot-edge-kpm) for tapping into IoT Edge message routes
* Readily available [nodes](https://flows.nodered.org/node/node-red-contrib-influxdb) for integrating and inserting data into InfluxDB as well as many other DBs
* Large library of nodes to integrate with other tools and platforms
* Easy flow-based programming allows manipulation and massaging of messages before inserted into a DB.
* Can be deployed as a Docker container

## Build IoT Edge modules

Refer to the Modules build instructions [here](fillo22/IoT-Monitoring/modules/Build.md)

## Prometheus Node Exporter installation

Refer to the [Prometheus Node Exporter](fillo22/IoT-Monitoring/../../PrometheusExporterSetup.md) for installation instructions.

## View the Grafana dashboard

Verify that the IoT Edge modules are indeed running by viewing the running Grafana dashboard. To do that, replace the `{ip-address}` in the following link with your own IotEdge device ip address and open the URL with a web browser:

```
http://{ip-address}:3000/
```

Login to Grafana using "admin" as user name and the password specified in the "GF_SECURITY_ADMIN_PASSWORD" environment variable (in grafana module options).

Hover over the dashboard icon in the left side panel and click "Manage." There should be three dashboards under the General folder. Click on the "Node Exporter Full" dashboard to get started. The resulting dashboard should look like below:



> [!NOTE]
> It may take upwards of 10 minutes for all graphs to show correctly since they rely on a history of data.

Feel free to explore the other dashboards available.

## View the Node red flow

Replace the `{ip-address}` in the following link with your own IotEdge device ip address and open the URLs with a web browser:

```
http://{ip-address}:1881/
http://{ip-address}:1882/
```

Login to Node red using "admin" as user name and "mypassword" as password.

The flow relative to port 1881 corresponds to the module edgetoinfluxdb and port 1882 corresponds to the module temperaturetoinfluxdb.
These modules collect data (respectively from edgeHub message bus and the Temperature Simulator) and enrich them. The output of the Enricher functions are the following:

* edgetoinfluxdb: an array of 4 elements
```json
[
	{
		"measurement": "DeviceData",
		"fields": {
			"ITEM_COUNT_GOOD": 83
		},
		"tags": {
			"Source": "urn:c740f8169c10:OPC-Site-02"
		},
		"timestamp": 1636123023558000000
	},
	{
		"measurement": "DeviceData",
		"fields": {
			"ITEM_COUNT_BAD": 1
		},
		"tags": {
			"Source": "urn:c740f8169c10:OPC-Site-02"
		},
		"timestamp": 1636123023558000000
	},
	{
		"measurement": "DeviceData",
		"fields": {
			"ITEM_COUNT_GOOD": 120
		},
		"tags": {
			"Source": "urn:c740f8169c10:OPC-Site-02"
		},
		"timestamp": 1636123028561000000
	},
	{
		"measurement": "DeviceData",
		"fields": {
			"ITEM_COUNT_BAD": 2
		},
		"tags": {
			"Source": "urn:c740f8169c10:OPC-Site-02"
		},
		"timestamp": 1636123028562000000
	}
]
```
* temperaturetoinfluxdb:
```json
{
	"payload": [
		{
			"measurement": "TemperatureSensor",
			"fields": {
				"MachineTemperature": 133.34067587407498,
				"MachinePressure": 13.798304846413604,
				"AmbientTemperature": 20.518524645836338,
				"AmbientHumidity": 25
			},
			"tags": {
				"Source": "TemperatureSensor"
			},
			"timestamp": 1636124573209000000
		}
	],
	"topic": "input",
	"input": "input1",
	"_msgid": "4540a94f3230ec88"
}
```
These messages are written into InfluxDB and sent to IoT Hub.

