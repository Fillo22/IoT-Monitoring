# Azure Data Explorer
Azure Data Explorer is a fast, fully managed data analytics service for real-time analysis on large volumes of data streaming from applications, websites, IoT devices, and more. You can use Azure Data Explorer to collect, store, and analyze diverse data to improve products, enhance customer experiences, monitor devices, and boost operations.

## Data Ingestion
Data ingestion is the process used to load data records from one or more sources to import data into a table in Azure Data Explorer. Once ingested, the data becomes available for query.
Azure Data Explorer supports several ingestion methods, each with its own target scenarios. These methods include ingestion tools, connectors and plugins to diverse services, managed pipelines, programmatic ingestion using SDKs, and direct access to ingestion.
In this sample we use the Azure Pipeline for Event Hub, a pipeline that transfers events from services to Azure Data Explorer.

## Azure Data Explorer Setup
### Create table
Create a new empty table (all scripts are available in the following [folder](https://github.com/Fillo22/IoT-Monitoring/tree/main/Kusto))
```kql
.create table RawTelemetry ( 
PartitionKey:string,
SerialNumber:string, 
MessageType:string,
Date:datetime, 
Data:dynamic, 
Message:dynamic  )
```
### Data mappings
Create an ingestion mapping associated to the format json and the table created previously
```kql
.create-or-alter table RawTelemetry ingestion json mapping "RawTelemetryMapping"
'['
'    { "column" : "PartitionKey", "datatype" : "string", "Properties":{"Path":"$.pk"}},'
'    { "column" : "SerialNumber", "datatype" : "string", "Properties":{"Path":"$.sn"}},'
'    { "column" : "MessageType", "datatype" : "string", "Properties":{"Path":"$.mt"}},'
'    { "column" : "Date", "datatype" : "datetime", "Properties":{"Path":"$.date"}},'
'    { "column" : "Data", "datatype" : "dynamic", "Properties":{"Path":"$.data"}},'
'    { "column" : "Message", "datatype" : "dynamic", "Properties":{"Path":"$"}}'
']'
```
### Configure streaming ingestion
Enable streaming ingestion on RawTelemetry table.
Streaming ingestion is useful for loading data when you need low latency between ingestion and query
```kql
.alter table RawTelemetry policy streamingingestion enable
```
### Update policy
Define two functions to separate asset metrics and telemetry from raw data
```kql
.create function
 with (docstring = 'used to separate asset metrics from raw data', folder = 'UpdatePolicyFunctions')
 ExtractAssetsMetrics()  
{
    RawTelemetry
    | where tostring(Data.measurement) == 'DeviceData'
    | summarize arg_max(ingestion_time(), *)
    | extend ItemCountGood = toint(Data.fields.ITEM_COUNT_GOOD), ItemCountBad = toint(Data.fields.ITEM_COUNT_BAD), Source = tostring(Data.tags.Source)
    | project-keep PartitionKey, SerialNumber, MessageType, Date, ItemCountGood, ItemCountBad,Source, Message
    | project-reorder PartitionKey, SerialNumber, MessageType, Date, ItemCountGood, ItemCountBad,Source, Message
}


.create function
 with (docstring = 'used to separate telemetry from raw data', folder = 'UpdatePolicyFunctions')
 ExtractTemperature()  
{
    RawTelemetry
    | where tostring(Data.measurement) == 'TemperatureSensor'
    | summarize arg_max(ingestion_time(), *)
    | extend MachineTemperature = todecimal(Data.fields.MachineTemperature), MachinePressure = todecimal(Data.fields.MachinePressure), 
        AmbientTemperature = todecimal(Data.fields.AmbientTemperature), AmbientHumidity = toint(Data.fields.AmbientHumidity)
    | project-keep PartitionKey, SerialNumber, MessageType, Date, MachineTemperature, MachinePressure, AmbientTemperature, AmbientHumidity, Message
    | project-reorder PartitionKey, SerialNumber, MessageType, Date, MachineTemperature, MachinePressure, AmbientTemperature, AmbientHumidity, Message
}
```
Create the two tables
```kql
.create table AssetTelemetry (PartitionKey: string, SerialNumber: string, MessageType: string, Date: datetime, ItemCountGood: int, ItemCountBad: int, Source: string, Message:dynamic)

.create table TemperatureTelemetry (PartitionKey: string, SerialNumber: string, MessageType: string, Date: datetime, MachineTemperature: decimal,
    MachinePressure: decimal, AmbientTemperature: decimal, AmbientHumidity: int, Message:dynamic)
```
Finally add the update policy command.
The update policy instructs Azure Data Explorer to automatically append data to a target table whenever new data is inserted into the source table, based on a transformation query that runs on the data inserted into the source table.
```kql
.alter table  AssetTelemetry policy update 
@'[{ "IsEnabled": true, "Source": "RawTelemetry", "Query": "ExtractAssetsMetrics()", "IsTransactional": true, "PropagateIngestionProperties": false}]'

.alter table TemperatureTelemetry policy update 
@'[{ "IsEnabled": true, "Source": "RawTelemetry", "Query": "ExtractTemperature()", "IsTransactional": true, "PropagateIngestionProperties": false}]'

```