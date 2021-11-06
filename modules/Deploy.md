
# Deploying Edge modules

- [Deploying Edge modules](#deploying-edge-modules)
    - [Adding the Edge modules](#adding-the-edge-modules)
      - [edgetoinfluxdb](#edgetoinfluxdb)
      - [Grafana](#grafana)
      - [InfluxDB](#influxdb)
      - [OpcPublisher](#opcpublisher)
      - [OpcSimulator](#opcsimulator)
      - [Prometheus](#prometheus)
      - [Simulated Temperature Sensor](#simulated-temperature-sensor)
      - [Azure Monitor](#azure-monitor)
    - [Adding the routes](#adding-the-routes)
    - [Apply the changes](#apply-the-changes)

All seven module images should now be in a container registry. Instances of these module images can now be deployed to an Edge machine using IoT Hub.

Navigate to the desired IoT Hub instance in the Azure portal and select "IoT Edge". All registered Edge devices should be visible. Click on the desired Edge device and click "Set Modules." In the "Container Registry Credentials", put the name, address, user name and password of the registry container used when [building the Edge module images](#building-edge-module-images).

### Adding the Edge modules

#### edgetoinfluxdb

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

#### Grafana

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

#### InfluxDB

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

#### OpcPublisher

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

#### OpcSimulator

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

#### Prometheus

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

#### Simulated Temperature Sensor

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

#### Azure Monitor

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

### Apply the changes

Click the "Review + Create" button and then select the "Create" button. This will start the deployment. Assuming all goes well the modules will be running after several minutes. The "IoT Edge Runtime Response" should be "200 -- Ok" and the module runtime status "running".