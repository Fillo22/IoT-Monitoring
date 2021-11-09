# Data rectifier

**Description:** This function is used to rectify the data.

- [Data rectifier](#data-rectifier)
  - [Description](#description)
    - [Temperature:](#temperature)
    - [Assets:](#assets)
  - [Requirements](#requirements)
  - [Solution structure](#solution-structure)
  - [Project functionalities](#project-functionalities)

## Description

Data coming from the sensors are formatted in a way that is not suitable for the rest of the system.
For this reason, we need to align the data to a common format, so that it can be used by the rest of applications or stored in a database.

By default, the data coming from the sensors are formatted as follows:

### Temperature:

```json
{
  "body": [
    {
      "measurement": "TemperatureSensor",
      "fields": {
        "MachineTemperature": 28.685982673631973,
        "MachinePressure": 1.8756182792745286,
        "AmbientTemperature": 20.933404935260025,
        "AmbientHumidity": 26
      },
      "tags": {
        "Source": "TemperatureSensor"
      },
      "timestamp": 1636381653890000000
    }
  ],
  "enqueuedTime": "Mon Nov 08 2021 15:27:55 GMT+0100 (Central European Standard Time)",
  "properties": {}
}

```

### Assets:

```json
{
  "body": [
    {
      "measurement": "DeviceData",
      "fields": {
        "ITEM_COUNT_GOOD": 110
      },
      "tags": {
        "Source": "urn:e2b3e0043117:OPC-Site-01"
      },
      "timestamp": 1636451185855000000
    },
    {
      "measurement": "DeviceData",
      "fields": {
        "ITEM_COUNT_BAD": 10
      },
      "tags": {
        "Source": "urn:e2b3e0043117:OPC-Site-01"
      },
      "timestamp": 1636451185855000000
    },
    {
      "measurement": "DeviceData",
      "fields": {
        "ITEM_COUNT_GOOD": 101
      },
      "tags": {
        "Source": "urn:e2b3e0043117:OPC-Site-01"
      },
      "timestamp": 1636451190858000000
    },
    {
      "measurement": "DeviceData",
      "fields": {
        "ITEM_COUNT_BAD": 2
      },
      "tags": {
        "Source": "urn:e2b3e0043117:OPC-Site-01"
      },
      "timestamp": 1636451190858000000
    }
  ],
  "enqueuedTime": "Tue Nov 09 2021 10:46:35 GMT+0100 (Central European Standard Time)"
}
```

What the function does is to convert the data to the following format, basically adding a common header and insert all the content inside a specific property called "data":
```json
{
  "date": "2021-11-08T18:33:46.3763615Z",
  "data": {
    "machine": {
      "temperature": 24.35758269303831,
      "pressure": 1.3825094207258835
    },
    "ambient": {
      "temperature": 21.175382869632628,
      "humidity": 24
    },
    "timeCreated": "2021-11-02T08:17:42.9444227Z"
  },
  "pk": "Berry",
  "sn": "Berry",
  "mt": "tel"
}
```

## Requirements

* Runtime: **.NET 6.0**
* OS: Windows
* Pricing: Serverless (Consumption)

[!NOTE]: Configuration on local.settings.json is in a flat-nested format, in order to be able to apply option pattern. In order to form a correct hierarchy, we must divide the individual properties through the use of a colon if we use Windows as an operating system, otherwise they must be divided using the double underscore. **DO NOT** use the dot as a separator, or expand properties as objects, as it will not work.

## Solution structure

The solution structure is composed by the following elements:
* **IoTMonitoring.Functions.Normalization.DataRectifier**: This is the main project where function is defined.
* **IoTMonitoring.Functions.Normalization.DataRectifier.Bll**: This is the business logic layer. It's a project made only of classes that can be copied and plugged into any other project and reuse the logic. 

## Project functionalities

The core project is made of two services (plus Azure Function method) that basically manage the message coming from IoT Hub, add all the required fields, change the structure and then send the message to Event Hub in batches.

Data directed to Event Hub is sent on different partitions and each partition is a specific serial number, that we retrieve from system properties of the incoming message.

Data is sent to Event Hub in batches and batch limit is configurable, then, every minute, the system will send all the messages that are currently in memory in order to prevent data loss in case of a failure and to enable also low rate device messages to be forwarded and processed.