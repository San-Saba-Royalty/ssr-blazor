variable "gcp_project_id" {
  description = "The GCP project ID"
  type        = string
}

variable "gcp_region" {
  description = "The GCP region"
  type        = string
  default     = "us-central1"
}

variable "gke_cluster_name" {
  description = "The name of the GKE cluster"
  type        = string
}

variable "gke_cluster_location" {
  description = "The location/zone of the GKE cluster"
  type        = string
}

variable "tls_cert_data" {
  description = "Base64 encoded TLS certificate data"
  type        = string
  sensitive   = true
}

variable "tls_key_data" {
  description = "Base64 encoded TLS private key data"
  type        = string
  sensitive   = true
}