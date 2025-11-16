variable "location" {
  description = "Azure region for all resources"
  type        = string
  default     = "eastus"
}

variable "tags" {
  description = "Tags to apply to all resources"
  type        = map(string)
  default = {
    Environment = "POC"
    Project     = "DeviceManifest"
  }
}

# Networking variables
variable "network_resource_group_name" {
  description = "Name of the networking resource group"
  type        = string
  default     = "rg-devicemanifest-network"
}

variable "vnet_name" {
  description = "Name of the virtual network"
  type        = string
  default     = "vnet-devicemanifest"
}

variable "vnet_address_space" {
  description = "Address space for the virtual network"
  type        = list(string)
  default     = ["10.0.0.0/16"]
}

variable "log_analytics_name" {
  description = "Name of the Log Analytics workspace"
  type        = string
  default     = "log-devicemanifest"
}

# AKS variables
variable "aks_resource_group_name" {
  description = "Name of the AKS resource group"
  type        = string
  default     = "rg-devicemanifest-aks"
}

variable "cluster_name" {
  description = "Name of the AKS cluster"
  type        = string
  default     = "aks-devicemanifest"
}

variable "dns_prefix" {
  description = "DNS prefix for the AKS cluster"
  type        = string
  default     = "devicemanifest"
}

variable "node_count" {
  description = "Number of nodes in the default node pool"
  type        = number
  default     = 3
}

variable "vm_size" {
  description = "Size of the VMs in the node pool"
  type        = string
  default     = "Standard_D4s_v3"
}

variable "enable_auto_scaling" {
  description = "Enable auto-scaling for the default node pool"
  type        = bool
  default     = true
}

variable "min_count" {
  description = "Minimum number of nodes when auto-scaling is enabled"
  type        = number
  default     = 3
}

variable "max_count" {
  description = "Maximum number of nodes when auto-scaling is enabled"
  type        = number
  default     = 10
}

# Storage variables
variable "storage_resource_group_name" {
  description = "Name of the storage resource group"
  type        = string
  default     = "rg-devicemanifest-storage"
}

variable "storage_account_name" {
  description = "Name of the storage account (must be globally unique, 3-24 lowercase alphanumeric)"
  type        = string
}

variable "container_name" {
  description = "Name of the blob container"
  type        = string
  default     = "device-files"
}
