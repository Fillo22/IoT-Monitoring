# IoT-Monitoring
[TOC]

## Description
### Requirements
## Architecture
### Schema 
1a) Vengono utilizzati due server Opc Ua: Opc Ua Simulator genera dati da due "siti" diversi e Opc Ua Reader (modulo fornito da Microsoft's Industrial IoT team) legge i dati dal simulator e li scrive in IoT Edge (via edgeHub)
1b) Il modulo Temperature Simulator genera periodicamente dati di temperatura, pressione e umidità
2) I moduli di Node Red collezionano i dati generati dai simulatori ed effettuano un arricchimento
    2a) I dati arricchiti vengono salvati su InfluxDB e utilizzati da Grafana per la creazione delle dashboard
    2b) Gli stessi dati vengo inviati a IoT Hub attraverso EdgeHub
3) L'Azure Function viene triggerata ad ogni messaggio ricevuto da IoT Hub, effettua una normalizzazione dei dati e li invia a Event Hub con DeviceId come partition key
4) Azure Data Explorer acquisisce i data da Event Hub, sono presenti tre tabelle:
    - RawTelemetry: in cui vengono mappati i campi del json
    - AssetTelemetry: in cui vengono inseriti i dati di telemetria relativi a Opc Ua Simulator
    - TemperatureTelemetry: in cui vengono inseirti i dati di telemetria relativi a Temperature Simulator
Sulla base di queste tabelle sono stata create delle dashboard online
5) Il modulo Metrics Collector è un modulo IoT Edge fornito da Microsoft che raccoglie le metriche del carico di lavoro dei moduli e le invia a Log Analytics


