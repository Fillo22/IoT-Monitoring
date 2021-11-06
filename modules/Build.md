# Building Edge module images

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

## Prometheus configuration
Replace the `{ip-address}` in IoT-Monitoring/modules/prometheus/prometheus.yml with your own IotEdge device ip address. This is the IP address of the IoT Edge device where the Prometheus Node exporter is running.

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