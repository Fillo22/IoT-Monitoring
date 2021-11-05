# IoT-Monitoring
[TOC]

## Description
### Requirements
## Architecture
### Schema 
![](media/Diagram%20v.2.6.png)
* **1a** The [OPC-UA Publisher](https://github.com/Azure/iot-edge-opc-publisher) module provided by Microsoft's Industrial IoT team, reads OPC-UA data from the simulator and writes it to IoT Edge (via edgeHub)
* **1b** Temperature Simulator periodically generates fake temperature, pressure and humidity data
* **2** Node-RED modules collects data from OPC Publisher (via edgeHub) and Temperature simulator and writes that data into:
    * **2a** InfluxDB which stores data in time series structure and provides this data to Grafana for dashboards
    * **2b** IoT Hub via EdgeHub
* **3** Azure Function triggered by IoT Hub, performs data normalization and sends them to Event Hub partitioned by DeviceID
* **4** Azure Data Explorer ingest data from Event Hub and map them to RawTelemetry table.
Then the data are divided into two tables:
    * AssetTelemetry: Opc Ua Simulator data
    * TemperatureTelemetry: Temperature Simulator data

    These tables are used for dashboards
* **5** The [metrics-collector module](https://aka.ms/edgemon-metrics-collector) is a Microsoft-supplied IoT Edge module that collects workload module metrics and send them to Log Analytics.

### Reasons for selecting this architecture

The main purpose of this solution is to provide an ability for local operators to view dashboards at the edge regardless of whether the edge device was online or offline. This is a natural scenario that IoT Edge supports. To support dashboarding however, there was a need to also select both a storage component as well as a visualization component. 

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

## Building Edge module images

Before any Edge modules can be deployed, it is necessary to build the module images using the Dockerfiles found in the repository. Once built, the images need to be placed into a container registry.

Start by cloning [the github repository](https://github.com/Fillo22/IoT-Monitoring) to a machine that has docker installed (possibly the IoT Edge device or a local development machine).

```bash
git clone https://github.com/Fillo22/IoT-Monitoring
```

Replace `{registry}` in the commands below with the container registry location (e.g. myregistry.azurecr.io).

```bash
sudo docker login {registry}

cd IoT-Monitoring/modules/edgetoinfluxdb
sudo docker build --tag {registry}/edgetoinfluxdb:1.0 .
sudo docker push {registry}/edgetoinfluxdb:1.0

cd ../grafana
sudo docker build --tag {registry}/grafana:1.0 .
sudo docker push {registry}/grafana:1.0

cd ../influxdb
sudo docker build --tag {registry}/influxdb:1.0 .
sudo docker push {registry}/influxdb:1.0

cd ../opcpublisher
sudo docker build --tag {registry}/opcpublisher:1.0 .
sudo docker push {registry}/opcpublisher:1.0

cd ../opcsimulator
sudo docker build --tag {registry}/opcsimulator:1.0 .
sudo docker push {registry}/opcsimulator:1.0

cd ../temperaturetoinfluxdb
sudo docker build --tag {registry}/temperaturetoinfluxdb:1.0 .
sudo docker push {registry}/temperaturetoinfluxdb:1.0
```
Replace the `{ip-address}` in IoT-Monitoring/modules/prometheus/prometheus.yml with your own IotEdge device ip address.
```
scrape_configs:
  - job_name: "prometheus"
    static_configs:
      - targets: ["{ip-address}:9100"]
```
```bash
cd ../prometheus
sudo docker build --tag {registry}/prometheus:1.0 .
sudo docker push {registry}/prometheus:1.0
```

## Deploying Edge modules

All seven module images should now be in a container registry. Instances of these module images can now be deployed to an Edge machine using IoT Hub.

Navigate to the desired IoT Hub instance in the Azure portal and select "IoT Edge". All registered Edge devices should be visible. Click on the desired Edge device and click "Set Modules." In the "Container Registry Credentials", put the name, address, user name and password of the registry container used when [building the Edge module images](#building-edge-module-images).

### Adding the Edge modules

In the "IoT Edge Modules" section, click the "+ Add" button and select "IoT Edge Module". For "IoT Edge Module Name" enter `"edgetoinfluxdb"` and for "Image URI" enter `"{registry}/edgetoinfluxdb:1.0"`. Be sure to replace `{registry}` with the registry address defined above. Switch to the "Container Create Options" and place the following JSON into the create options field:

```json
{
    "HostConfig": {
        "PortBindings": {
            "1880/tcp": [
                {
                    "HostPort": "1881"
                }
            ]
        }
    }
}
```

Click the "Add" button to complete the creation of the module for it to be deployed. This needs to be repeated for all other six remaining modules plus the two modules provided by Microsoft (Simulated Temperature Sensor and Azure Monitor). The following are the property values for each module.  Note: the variable `{GF_SECURITY_ADMIN_PASSWORD}` represents the admin password that you will use to log into the Grafana dashboards once deployment is complete.

**Module grafana:**

```
IoT Edge Module Name: grafana
Image URI: {registry}/grafana:1.0
Environment Variable:
    Name: GF_SECURITY_ADMIN_PASSWORD
    Value: {password}
Container Create Options:
{
    "HostConfig": {
        "PortBindings": {
            "3000/tcp": [
                {
                    "HostPort": "3000"
                }
            ]
        }
    }
}
```

**Module influxdb:**

```
IoT Edge Module Name: influxdb
Image URI: {registry}/influxdb:1.0
Container Create Options:
{
    "HostConfig": {
        "Binds": [
            "/influxdata:/var/lib/influxdb"
        ],
        "PortBindings": {
            "8086/tcp": [
                {
                    "HostPort": "8086"
                }
            ]
        }
    }
}
```

**Module opcpublisher:**

```
IoT Edge Module Name: opcpublisher
Image URI: {registry}/opcpublisher:1.0
Container Create Options:
{
    "Hostname": "publisher",
    "Cmd": [
        "--pf=/app/pn.json",
        "--aa"
    ]
}
```

**Module opcsimulator:**

```
IoT Edge Module Name: opcsimulator
Image URI: {registry}/opcsimulator:1.0
Container Create Options:
{
    "HostConfig": {
        "PortBindings": {
            "1880/tcp": [
                {
                    "HostPort": "1880"
                }
            ]
        }
    }
}
```

**Module prometheus:**

```
IoT Edge Module Name: prometheus
Image URI: {registry}/prometheus:1.0
```

**Module temperaturetoinfluxdb:**

```
IoT Edge Module Name: temperaturetoinfluxdb
Image URI: {registry}/temperaturetoinfluxdb:1.0
Container Create Options:
{
    "HostConfig": {
        "PortBindings": {
            "1880/tcp": [
                {
                    "HostPort": "1882"
                }
            ]
        }
    }
}
```

**Module SimulatedTemperatureSensor:**

```
IoT Edge Module Name: SimulatedTemperatureSensor
Image URI: mcr.microsoft.com/azureiotedge-simulated-temperature-sensor:1.0
Environment Variable:
    Name: MessageCount
    Value: -1
Module Twin Settings:
{
    "SendData": true,
    "SendInterval": 1
}
```

**Module AzureMonitor:**

```
IoT Edge Module Name: AzureMonitor
Image URI: mcr.microsoft.com/azureiotedge-metrics-collector
Environment Variable:
    Name: ResourceId
    Value: {ResourceID}
    Name: UploadTarget
    Value: AzureMonitor
    Name: LogAnalyticsWorkspaceId
    Value: {LogAnalyticsID}
    Name: LogAnalyticsSharedKey
    Value: {LogAnalyticskey}
    Name: ScrapeFrequencyInSecs
    Value: 10
```
Note: the variable `{ResourceID}` represents resource ID of the IoT hub that the device communicates with, `{LogAnalyticsID}` and `{LogAnalyticskey}` represents Log Analytics workspace ID and key.

The "Set modules" dialog should now look like this:


### Adding the routes

Next, click on the "Routes" tab and add the following routes:

* routeTemperature
```bash
FROM /messages/modules/temperaturetoinfluxdb/* INTO $upstream
```
* RouteOPC
```bash
FROM /messages/modules/edgetoinfluxdb/* INTO $upstream
```
* TempNode
```bash
FROM /messages/modules/SimulatedTemperatureSensor/* INTO BrokeredEndpoint("/modules/temperaturetoinfluxdb/inputs/input1")
```
* OpcNode
```bash
FROM /messages/modules/opcpublisher/* INTO BrokeredEndpoint("/modules/edgetoinfluxdb/inputs/input1")
```

### Deploying modules to devices

Click the "Review + Create" button and then select the "Create" button. This will start the deployment. Assuming all goes well the modules will be running after several minutes. The "IoT Edge Runtime Response" should be "200 -- Ok" and the module runtime status "running".

## Prometheus node exporter
All modules must emit metrics using the Prometheus data model.
Prometheus is an open source metrics database and monitoring system, it collects and stores its metrics as time series data.
Many systems do not have Prometheus formatted. For example a Raspberry Pi running Raspbian does not have a Prometheus metrics endpoint. This is where the node exporter comes in. The node exporter is an agent. It exposes your host’s metrics in the format Prometheus expects.

### Node Exporter Setup
Download the release of node exporter specif for the architecture of your device on [projects releases page](https://github.com/prometheus/node_exporter/releases) on Github.

```bash
wget https://github.com/prometheus/node_exporter/releases/download/v1.2.2/node_exporter-1.2.2.linux-amd64.tar.gz
```
Now unpack the tar file using this command
```bash
tar -xvf node_exporter-1.2.2.linux-amd64.tar.gz
```

Move the binary file of node exporter to /usr/local/bin location
```bash
sudo mv node_exporter-1.2.2.linux-amd64/node_exporter /usr/local/bin/
```

Create a service account for the node_exporter
```bash
sudo useradd -rs /bin/false node_exporter
```

Create a file called node_exporter.service in the /etc/sytemd/system directory
```bash
sudo nano /etc/systemd/system/node_exporter.service
```
Put the following contents into the file:
```
[Unit]
Description=Node Exporter
After=network.target

[Service]
User=node_exporter
Group=node_exporter
Type=simple
ExecStart=/usr/local/bin/node_exporter

[Install]
WantedBy=multi-user.target
```

Now reload the systemd daemon and set the service to always run at boot and start it
```bash
sudo systemctl daemon-reload
sudo systemctl enable node_exporter
sudo systemctl start node_exporter
sudo systemctl status node_exporter
```

node_exporter web server is up on port 9100. Try using curl to view all the server metrics (replace `{ip-address}` with your IotEdge device ip address.)
```bash
curl http://{ip-address}:9100/metrics
```

## View the Grafana dashboard

Verify that the IoT Edge modules are indeed running by viewing the running Grafana dashboard. To do that, replace the `{ip-address}` in the following link with your own IotEdge device ip address and open the URL with a web browser:

```http
http://{ip-address}:3000/
```

Login to Grafana using "admin" as user name and the password specified in the "GF_SECURITY_ADMIN_PASSWORD" environment variable (in grafana module options).

Hover over the dashboard icon in the left side panel and click "Manage." There should be three dashboards under the General folder. Click on the "Node Exporter Full" dashboard to get started. The resulting dashboard should look like below:



> [!NOTE]
> It may take upwards of 10 minutes for all graphs to show correctly since they rely on a history of data.

Feel free to explore the other dashboards available.

## View the Node red flow

Replace the `{ip-address}` in the following link with your own IotEdge device ip address and open the URLs with a web browser:

```http
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
This message are written into InfluxDB and sent to IoT Hub.

