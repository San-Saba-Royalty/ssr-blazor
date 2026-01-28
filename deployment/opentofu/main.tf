
resource "kubernetes_namespace_v1" "ssr" {
  metadata {
    name = "ssr"
  }
}

# TLS certificates are now automatically managed by cert-manager and Let's Encrypt

