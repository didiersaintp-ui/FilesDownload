variable "resource_group_name" {
  description = "Name of the resource group"
  type        = string
}

variable "location" {
  description = "Azure region"
  type        = string
  default     = "eastus"
}

variable "storage_account_name" {
  description = "Name of the storage account (must be globally unique, 3-24 lowercase alphanumeric)"
  type        = string
}

variable "account_tier" {
  description = "Storage account tier"
  type        = string
  default     = "Standard"
}

variable "replication_type" {
  description = "Storage account replication type"
  type        = string
  default     = "LRS"
}

variable "container_name" {
  description = "Name of the blob container"
  type        = string
  default     = "device-files"
}

variable "allowed_subnet_ids" {
  description = "List of subnet IDs allowed to access the storage account"
  type        = list(string)
  default     = []
}

variable "private_endpoint_subnet_id" {
  description = "Subnet ID for the private endpoint"
  type        = string
}

variable "vnet_id" {
  description = "Virtual network ID for private DNS zone link"
  type        = string
}

variable "aks_kubelet_identity_object_id" {
  description = "AKS kubelet managed identity object ID (for role assignment)"
  type        = string
  default     = null
}

variable "tags" {
  description = "Tags to apply to resources"
  type        = map(string)
  default     = {}
}
