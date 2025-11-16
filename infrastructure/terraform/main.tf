# Main Terraform configuration to deploy the complete infrastructure
# This orchestrates all modules: networking, AKS, and storage

terraform {
  required_version = ">= 1.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
  }
}

provider "azurerm" {
  features {}
}

# Networking module (VNet, subnets, Log Analytics)
module "networking" {
  source = "./networking"

  resource_group_name = var.network_resource_group_name
  location            = var.location
  vnet_name           = var.vnet_name
  vnet_address_space  = var.vnet_address_space
  log_analytics_name  = var.log_analytics_name

  tags = var.tags
}

# AKS module
module "aks" {
  source = "./aks"

  resource_group_name        = var.aks_resource_group_name
  location                   = var.location
  cluster_name               = var.cluster_name
  dns_prefix                 = var.dns_prefix
  node_count                 = var.node_count
  vm_size                    = var.vm_size
  vnet_subnet_id             = module.networking.aks_subnet_id
  log_analytics_workspace_id = module.networking.log_analytics_workspace_id
  enable_auto_scaling        = var.enable_auto_scaling
  min_count                  = var.min_count
  max_count                  = var.max_count

  tags = var.tags

  depends_on = [module.networking]
}

# Azure Blob Storage module (for Solution A)
module "storage" {
  source = "./storage"

  resource_group_name              = var.storage_resource_group_name
  location                         = var.location
  storage_account_name             = var.storage_account_name
  container_name                   = var.container_name
  allowed_subnet_ids               = [module.networking.aks_subnet_id]
  private_endpoint_subnet_id       = module.networking.private_endpoints_subnet_id
  vnet_id                          = module.networking.vnet_id
  aks_kubelet_identity_object_id   = module.aks.kubelet_identity_object_id

  tags = var.tags

  depends_on = [module.networking, module.aks]
}
