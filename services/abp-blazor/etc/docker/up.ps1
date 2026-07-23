docker network create abptestwithsso --label=abptestwithsso
docker-compose -f containers/elasticsearch.yml up -d
docker-compose -f containers/grafana.yml up -d
docker-compose -f containers/kibana.yml up -d
docker-compose -f containers/prometheus.yml up -d
docker-compose -f containers/rabbitmq.yml up -d
docker-compose -f containers/redis.yml up -d
docker-compose -f containers/ollama.yml up -d
docker-compose -f containers/pgvector.yml up -d
docker-compose -f containers/postgresql.yml up -d
exit $LASTEXITCODE
