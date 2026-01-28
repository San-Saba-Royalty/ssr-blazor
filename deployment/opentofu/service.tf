resource "kubernetes_service_v1" "ssr_service" {
  depends_on = [kubernetes_namespace_v1.ssr]
  metadata {
    name      = "ssr-service"
    namespace = kubernetes_namespace_v1.ssr.metadata[0].name
  }
  spec {
    selector = {
      app = "ssr-app"
    }
    port {
      port        = 8080
      target_port = 8080
    }
    type = "ClusterIP"
  }
}
