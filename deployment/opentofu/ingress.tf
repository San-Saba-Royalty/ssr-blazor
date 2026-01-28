resource "kubernetes_ingress_v1" "ssr_ingress" {
  depends_on = [kubernetes_namespace_v1.ssr]
  metadata {
    name      = "ssr-ingress"
    namespace = kubernetes_namespace_v1.ssr.metadata[0].name
    annotations = {
      "kubernetes.io/ingress.class"                 = "nginx"
      "nginx.ingress.kubernetes.io/ssl-redirect"    = "true"
      "nginx.ingress.kubernetes.io/proxy-body-size" = "50m" # Useful for file uploads
    }
  }

  spec {
    tls {
      hosts       = ["ssr.prometheusags.ai"]
      secret_name = "wildcard-prometheusags-ai-tls" # Your preconfigured secret
    }

    rule {
      host = "ssr.prometheusags.ai"
      http {
        path {
          path      = "/"
          path_type = "Prefix"
          backend {
            service {
              name = kubernetes_service_v1.ssr_service.metadata[0].name
              port {
                number = 80
              }
            }
          }
        }
      }
    }
  }
}
