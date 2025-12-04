variable "subscription_id" {
  description = "Azure Subscription ID"
  type        = string
}

variable "project_name" {
  description = "Project name used for resource naming"
  type        = string
  default     = "allowancetracker"
}

variable "environment" {
  description = "Environment name (dev, staging, prod)"
  type        = string
  default     = "dev"

  validation {
    condition     = contains(["dev", "staging", "prod"], var.environment)
    error_message = "Environment must be dev, staging, or prod"
  }
}

variable "location" {
  description = "Azure region for resources"
  type        = string
  default     = "East US"
}

variable "app_service_plan_sku" {
  description = "SKU for App Service Plan"
  type        = string
  default     = "Y1" # Consumption plan (serverless)

  validation {
    condition     = contains(["Y1", "EP1", "EP2", "EP3"], var.app_service_plan_sku)
    error_message = "App Service Plan SKU must be Y1 (consumption), EP1, EP2, or EP3 (elastic premium)"
  }
}

variable "database_sku" {
  description = "SKU for Azure SQL Database"
  type        = string
  default     = "Basic" # Cheapest option for dev/test

  validation {
    condition     = contains(["Basic", "S0", "S1", "S2", "P1", "P2"], var.database_sku)
    error_message = "Database SKU must be valid Azure SQL tier"
  }
}

variable "database_max_size_gb" {
  description = "Maximum database size in GB"
  type        = number
  default     = 2
}

variable "sql_admin_username" {
  description = "SQL Server administrator username"
  type        = string
  sensitive   = true
}

variable "sql_admin_password" {
  description = "SQL Server administrator password"
  type        = string
  sensitive   = true

  validation {
    condition     = length(var.sql_admin_password) >= 8
    error_message = "SQL admin password must be at least 8 characters"
  }
}

variable "jwt_secret_key" {
  description = "JWT secret key for token generation"
  type        = string
  sensitive   = true

  validation {
    condition     = length(var.jwt_secret_key) >= 32
    error_message = "JWT secret key must be at least 32 characters"
  }
}

variable "sendgrid_api_key" {
  description = "SendGrid API key for email"
  type        = string
  sensitive   = true
  default     = ""
}

variable "sendgrid_from_email" {
  description = "SendGrid sender email address"
  type        = string
  default     = "noreply@allowancetracker.com"
}

variable "sendgrid_from_name" {
  description = "SendGrid sender display name"
  type        = string
  default     = "Allowance Tracker"
}

variable "cors_allowed_origins" {
  description = "CORS allowed origins for Functions"
  type        = list(string)
  default     = ["http://localhost:5173", "http://localhost:3000"]
}
