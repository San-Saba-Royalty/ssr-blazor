
resource "kubernetes_namespace_v1" "ssr" {
  metadata {
    name = "ssr"
  }
}

# TLS Secret for wildcard certificate
resource "kubernetes_secret_v1" "wildcard_tls" {
  depends_on = [kubernetes_namespace_v1.ssr]

  metadata {
    name      = "wildcard-prometheusags-ai-tls"
    namespace = kubernetes_namespace_v1.ssr.metadata[0].name
  }

  type = "kubernetes.io/tls"

  data = {
    "tls.crt" = var.tls_cert_data
    "tls.key" = var.tls_key_data
  }
}

