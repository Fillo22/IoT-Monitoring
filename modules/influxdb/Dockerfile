# start with standard public influx image
FROM influxdb:1.8.10

# copy in our database initialization script
# creates telemetry database sets retention policy
COPY initdb.iql /docker-entrypoint-initdb.d/init.iql

# copy in our config
COPY influxdb.conf /etc/influxdb/influxdb.conf

# launch influx daemon
CMD ["influxd"]
