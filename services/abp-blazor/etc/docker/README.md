# Docker-Compose for Infrastructure Dependencies

This folder includes docker-compose configuration and necessary PowerShell scripts to run depending services to be able to run the solution in local development environment.

> That docker dependencies are also configured in ABP Studio Solution Runner. So, if you use ABP Studio Solution Runner, no need to manually run it.

## Up

Run the `up.ps1` file in a PowerShell terminal to run all the dependencies, so you can locally run and debug your solution.

## Down

Use `down.ps1` to remove all the containers from your computer.

## MinIO

The HCS migration uses `containers/minio.yml` for document object storage.

- S3 API: `http://localhost:9000`
- Console: `http://localhost:9001`
- Container endpoint: `minio:9000`
- Default local credentials: `hcsadmin` / `hcsadminpassword`

Override `MINIO_ROOT_USER` and `MINIO_ROOT_PASSWORD` in the shell or local
environment before starting the compose file. Do not commit production
credentials. The default values are for the local lab only.
