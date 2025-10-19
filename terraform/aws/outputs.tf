output "api_gateway_url" {
  description = "API Gateway URL"
  value       = aws_apigatewayv2_api.main.api_endpoint
}

output "rds_endpoint" {
  description = "RDS PostgreSQL endpoint"
  value       = aws_db_instance.main.endpoint
}

output "rds_database_name" {
  description = "RDS database name"
  value       = aws_db_instance.main.db_name
}

output "lambda_function_name" {
  description = "Lambda function name (example)"
  value       = aws_lambda_function.register_parent.function_name
}

output "lambda_role_arn" {
  description = "IAM role ARN for Lambda functions"
  value       = aws_iam_role.lambda_exec.arn
}

output "vpc_id" {
  description = "VPC ID"
  value       = aws_vpc.main.id
}

output "connection_string" {
  description = "Database connection string"
  value       = "Host=${aws_db_instance.main.endpoint};Database=${aws_db_instance.main.db_name};Username=${var.db_username};Password=${var.db_password}"
  sensitive   = true
}
