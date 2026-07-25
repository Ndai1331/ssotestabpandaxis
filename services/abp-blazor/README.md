# hanhchinhso

## About this solution

This is a startup template to create microservice based solutions. All the fundamental ABP modules are already installed and configured. Check the [Microservice Solution Template](https://abp.io/docs/latest/solution-templates/microservice) documentation for more info.

### Pre-requirements

* [.NET10.0+ SDK](https://dotnet.microsoft.com/download/dotnet)
* [Node v18 or 20](https://nodejs.org/en)
* [Docker](https://www.docker.com/) for running infrastructure dependencies
* [Redis](https://redis.io/) for distributed caching

### Solution structure

This is a microservice solution that consists of the following components:

#### Applications (`apps/`)

* `auth-server`: Authentication server using OpenIddict for OAuth 2.0 / OpenID Connect.
* `blazor`: Blazor application for the main UI.

#### Gateways (`gateways/`)

The solution uses the Backend for Frontend (BFF) pattern with dedicated gateways:

* `web`: Web gateway for the main web application.

#### Services (`services/`)

* `administration`: Permissions, settings, and features management service.
* `audit-logging`: Audit logging service for tracking system activities.
* `gdpr`: GDPR compliance service for data protection.
* `identity`: User and role management service.
* `text-template-management`: Text template management service.

## Running locally (CLI — recommended)

One-command Aspire AppHost (no ABP Studio required):

```bash
./aspire/run.sh          # light profile (core SSO + Blazor)
./aspire/run.sh full     # all microservices + Elsa Studio
```

Details, ports, and Keycloak notes: [aspire/README.md](./aspire/README.md).

ABP Studio Solution Runner (`Default.abprun.json`) still works for GUI workflows.

## Before Running the Solution

### Generate Signing-Certificate for AuthServer 

#### Installing mkcert
This guide will be using `mkcert` for creating self-signed certificates. If it is not installed in your system, follow the [installation guide](https://github.com/FiloSottile/mkcert#installation) to install mkcert.

Then use the command to create root (local) certificate authority for your certificates:
```powershell
mkcert -install
```

#### Generate Signing-Certificate

Navigate to `/apps/auth-server/hanhchinhso.AuthServer` folder and run:

```bash
dotnet dev-certs https -v -ep ./openiddict.pfx -p 5ffe2f0a-090d-41f0-9d0b-07859aacfaea
```

to generate pfx file for signing tokens by AuthServer.

> This should be done by every developer.

### Install Client-Side Libraries

Run the following command in this folder:

````bash
abp install-libs
````




### Running on a Kubernetes Cluster Environment

To run the application(s) on your Kubernetes cluster environment, follow these steps:

- Navigate to the [/etc/helm](./etc/helm) directory within your terminal or command prompt.
- Execute the [create-tls-secrets.ps1](./etc/helm/create-tls-secrets.ps1) PowerShell command.
- Open the Kubernetes menu in ABP Studio, then within the Helm tab:
  - Build Docker Images: In the `Charts` tree context menu, click on the `Build Docker Image(s)` option. This will start the process of building your Docker images.
  - Install Charts: After the Docker images have been built, you can install your Helm charts. To do this, go to `Charts->Commands` in the context menu and click on `Install Chart(s)`.

Also, make sure to review the [pre-requirements](./etc/helm/README.md#Pre-requirements) before proceeding.

> This should be done by every developer.

### Deploying the application

Deploying an ABP application follows the same process as deploying any .NET or ASP.NET Core application. However, there are important considerations to keep in mind. For detailed guidance, refer to ABP's [deployment documentation](https://abp.io/docs/latest/deployment/distributed-microservice).

**BD production runbook (Ubuntu 24+, Docker+Nginx hoặc Kubernetes/Helm):**  
[`docs/runbooks/deploy-abp-production.md`](../../docs/runbooks/deploy-abp-production.md)

**Compose + Nginx templates:** [`etc/docker-prod/`](./etc/docker-prod/) (`.env.example`, `docker-compose.prod.yml`, nginx sample).

### Additional resources

#### Internal Resources

You can find detailed setup and configuration guide(s) for your solution below:

* [Docker-Compose for Infrastructure Dependencies](./etc/docker/README.md)
* [Local Kubernetes Guide](./etc/helm/README.md)

#### External Resources

You can see the following resources to learn more about your solution and the ABP Framework:

* [Microservice Development Tutorial](https://abp.io/docs/latest/tutorials/microservice)
* [Microservice Solution Template](https://abp.io/docs/latest/solution-templates/microservice)
