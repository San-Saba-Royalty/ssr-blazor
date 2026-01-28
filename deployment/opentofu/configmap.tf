resource "kubernetes_config_map_v1" "app_settings" {
  depends_on = [kubernetes_namespace_v1.ssr]
  metadata {
    name      = "ssr-config"
    namespace = kubernetes_namespace_v1.ssr.metadata[0].name
  }

  data = {
    "appsettings.Production.json" = jsonencode({
      ConnectionStrings = {
        SanSabaConnection = "Server=ssr.database.windows.net;Database=ssr-db;User Id=babyice;Password=J0n@th0nJ@m3s;TrustServerCertificate=True;"
      }
      AzureStorage = {
        ConnectionString = ""
        ContainerNames = {
          Acquisitions     = "acquisitions"
          LetterAgreements = "letter-agreements"
        }
      }
      AzureFileShare = {
        AccountName             = "sansaba"
        AccountKey              = "mrAQfxTnHNuy//OF6CUUKaNTswcqsRZ32m/CR7rm/o9UkbDYrL8eRXTqqmBb0f0kFgWIBrAsSn8e+AStJJRMUw=="
        FileShareName           = "document-templates"
        GeneratedDocumentsShare = "generated-documents"
      }
      Logging = {
        LogLevel = {
          Default                = "Information"
          "Microsoft.AspNetCore" = "Warning"
        }
      }
      AllowedHosts = "*"
    })
  }
}
