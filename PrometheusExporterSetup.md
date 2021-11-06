# Prometheus node exporter
All modules must emit metrics using the Prometheus data model.
Prometheus is an open source metrics database and monitoring system, it collects and stores its metrics as time series data.
Many systems do not have Prometheus formatted. For example a Raspberry Pi running Raspbian does not have a Prometheus metrics endpoint. This is where the node exporter comes in. The node exporter is an agent that exposes your host’s metrics in the Prometheus format.

## Node Exporter Setup
Download the release of node exporter specific for the architecture of your device on [projects releases page](https://github.com/prometheus/node_exporter/releases) on Github.

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