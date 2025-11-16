variable "resource_group_name" {
  description = "Name of the resource group"
  type        = string
}

variable "location" {
  description = "Azure region"
  type        = string
  default     = "eastus"
}

variable "cluster_name" {
  description = "Name of the AKS cluster"
  type        = string
}

variable "dns_prefix" {
  description = "DNS prefix for the cluster"
  type        = string
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

variable "os_disk_size_gb" {
  description = "OS disk size in GB"
  type        = number
  default     = 100
}

variable "vnet_subnet_id" {
  description = "Subnet ID for the AKS nodes"
  type        = string
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

variable "log_analytics_workspace_id" {
  description = "Log Analytics workspace ID for monitoring"
  type        = string
}

variable "acr_id" {
  description = "Azure Container Registry ID (optional)"
  type        = string
  default     = null
}

variable "write_kubeconfig" {
  description = "Write kubeconfig to file"
  type        = bool
  default     = false
}

variable "tags" {
  description = "Tags to apply to resources"
  type        = map(string)
  default     = {}
}
