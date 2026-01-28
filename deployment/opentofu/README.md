# SSR Blazor Kubernetes Deployment

This directory contains OpenTofu/Terraform configuration for deploying the SSR Blazor application to a Google Kubernetes Engine (GKE) cluster.

## Prerequisites

- [OpenTofu](https://opentofu.org/) or [Terraform](https://terraform.io/) installed
- [Google Cloud CLI](https://cloud.google.com/sdk/docs/install) installed and configured
- [kubectl](https://kubernetes.io/docs/tasks/tools/) installed
- Access to a GKE cluster
- TLS certificate for your domain (wildcard certificate for `*.prometheusags.ai`)

## Configuration

### 1. Set up variables

Copy the example variables file and configure it:

```bash
cp terraform.tfvars.example terraform.tfvars
```

Edit `terraform.tfvars` with your specific values:

- `gcp_project_id`: Your Google Cloud project ID
- `gke_cluster_name`: Name of your GKE cluster
- `gke_cluster_location`: Zone (for zonal cluster) or region (for regional cluster)
- `tls_cert_data`: Base64 encoded TLS certificate
- `tls_key_data`: Base64 encoded TLS private key

### 2. Prepare TLS certificate

To encode your certificate and key files to base64:

```bash
# For certificate
cat your-wildcard-cert.crt | base64 -w 0

# For private key
cat your-wildcard-cert.key | base64 -w 0
```

Add the resulting base64 strings to your `terraform.tfvars` file.

### 3. Authentication

Ensure you're authenticated with Google Cloud:

```bash
gcloud auth login
gcloud config set project YOUR-PROJECT-ID
```

Get cluster credentials:

```bash
gcloud container clusters get-credentials CLUSTER-NAME \
  --location=CLUSTER-LOCATION \
  --project=PROJECT-ID
```

## Deployment

### Initialize and deploy

```bash
# Initialize OpenTofu/Terraform
tofu init  # or terraform init

# Validate configuration
tofu validate  # or terraform validate

# Plan changes
tofu plan  # or terraform plan

# Apply changes
tofu apply  # or terraform apply
```

## Resources Created

This configuration creates the following Kubernetes resources:

- **Namespace**: `ssr` - Isolates the application resources
- **TLS Secret**: `wildcard-prometheusags-ai-tls` - Stores the wildcard SSL certificate
- **ConfigMap**: Application configuration from `appsettings.Production.json`
- **Deployment**: `ssr-deployment` - Runs 2 replicas of the SSR Blazor application
- **Service**: `ssr-service` - ClusterIP service on port 80
- **Ingress**: `ssr-ingress` - NGINX ingress with TLS termination for `ssr.prometheusags.ai`

## CI/CD with GitHub Actions

The repository includes a GitHub Actions workflow (`.github/workflows/deploy.yml`) that:

1. **Builds** the Docker image and pushes to Docker Hub
2. **Deploys** using OpenTofu to the GKE cluster
3. **Updates** the deployment with the new image
4. **Verifies** the deployment health

### Required GitHub Secrets

Configure these secrets in your GitHub repository:

| Secret | Description |
|--------|-------------|
| `DOCKER_HUB_USERNAME` | Docker Hub username |
| `DOCKER_HUB_ACCESS_TOKEN` | Docker Hub access token |
| `GCP_PROJECT_ID` | Google Cloud project ID |
| `GCP_SERVICE_ACCOUNT_KEY` | Service account JSON key for GCP |
| `GKE_CLUSTER_NAME` | Name of the GKE cluster |
| `GKE_CLUSTER_LOCATION` | Location of the GKE cluster |
| `TLS_CERT_DATA` | Base64 encoded TLS certificate |
| `TLS_KEY_DATA` | Base64 encoded TLS private key |

### Service Account Permissions

The GCP service account used by GitHub Actions needs these IAM roles:

- `roles/container.clusterAdmin` - Manage GKE clusters
- `roles/container.developer` - Deploy to GKE
- `roles/storage.objectViewer` - Access container images

## Troubleshooting

### Common Issues

1. **Authentication errors**: Ensure you're logged in to Google Cloud and have the correct project selected
2. **Cluster not found**: Verify the cluster name and location are correct
3. **TLS certificate issues**: Check that your certificate is valid and properly base64 encoded
4. **Image pull errors**: Ensure the Docker image exists and is accessible

### Useful Commands

```bash
# Check cluster info
kubectl cluster-info

# View resources in the ssr namespace
kubectl get all -n ssr

# Check ingress status
kubectl describe ingress ssr-ingress -n ssr

# View application logs
kubectl logs -f deployment/ssr-deployment -n ssr

# Test connectivity
kubectl port-forward service/ssr-service 8080:80 -n ssr
```

### Rollback

If you need to rollback a deployment:

```bash
# Rollback to previous version
kubectl rollout undo deployment/ssr-deployment -n ssr

# Check rollout status
kubectl rollout status deployment/ssr-deployment -n ssr
```

## Security Considerations

- TLS certificates and private keys are stored as Kubernetes secrets
- Service account follows least-privilege principle
- Sensitive variables are marked as `sensitive` in Terraform
- GitHub secrets are used for CI/CD credentials
- HTTPS redirect is enforced by the ingress controller

## Architecture

```
Internet → NGINX Ingress (TLS termination) → Service → Deployment Pods
                                                     ↓
                                              ConfigMap (app config)
```

The application runs in a dedicated namespace with proper resource isolation and TLS encryption for external traffic.