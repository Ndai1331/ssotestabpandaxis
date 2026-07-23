docker-compose -f containers/elasticsearch.yml down
docker-compose -f containers/grafana.yml down
docker-compose -f containers/kibana.yml down
docker-compose -f containers/prometheus.yml down
docker-compose -f containers/rabbitmq.yml down
docker-compose -f containers/redis.yml down
docker-compose -f containers/ollama.yml down
docker-compose -f containers/pgvector.yml down
docker-compose -f containers/postgresql.yml down
exit $LASTEXITCODE
