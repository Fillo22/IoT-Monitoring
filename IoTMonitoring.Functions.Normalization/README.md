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

```

### Assets:

```json

```

What the function does is to convert the data to the following format, basically adding a common header and insert all the content inside a specific property called "data":
```json

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