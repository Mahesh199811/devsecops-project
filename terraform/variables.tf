variable "aws_region" {
  description = "AWS region for infrastructure resources."
  type        = string
  default     = "us-east-1"
}

variable "project_name" {
  description = "Name prefix for project resources."
  type        = string
  default     = "devsecops-project"
}
