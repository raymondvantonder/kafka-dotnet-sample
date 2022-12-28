# kafka-dotnet-sample
Simple Kafka producer and consumer using the confluent dotnet client.

## Installation
Use homebrew to install docker, java, kafka and zookeeper on your machine. 

Mac OS Example:
```bash
brew install java
brew install kafka
brew install zookeeper
brew install docker
```

## Running

Use the following commands to start up zookeeper first then kafka.

```bash
opt/homebrew/bin/zookeeper-server-start /opt/homebrew/etc/zookeeper/zoo.cfg

/opt/homebrew/bin/kafka-server-start /opt/homebrew/etc/kafka/server.properties
```

Navigate to the docker directory in source and run the following command to start up OpenSearch

```bash
docker compose up
```

