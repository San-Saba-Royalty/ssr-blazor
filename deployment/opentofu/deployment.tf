resource "kubernetes_deployment_v1" "ssr_app" {
  depends_on = [kubernetes_namespace_v1.ssr]
  metadata {
    name      = "ssr-deployment"
    namespace = kubernetes_namespace_v1.ssr.metadata[0].name
    labels    = { app = "ssr-app" }
  }

  spec {
    replicas = 2
    selector {
      match_labels = { app = "ssr-app" }
    }

    template {
      metadata {
        labels = { app = "ssr-app" }
      }

      spec {
        container {
          name  = "ssr-container"
          image = "tribehealth/ssrblazor:latest"

          port { container_port = 8080 }

          env {
            name  = "ASPNETCORE_ENVIRONMENT"
            value = "Production"
          }

          volume_mount {
            name       = "config-volume"
            mount_path = "/app/appsettings.Production.json"
            sub_path   = "appsettings.Production.json"
            read_only  = true
          }
        }

        volume {
          name = "config-volume"
          config_map {
            name = kubernetes_config_map_v1.app_settings.metadata[0].name
          }
        }
      }
    }
  }
}
