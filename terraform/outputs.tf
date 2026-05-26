output "artifact_bucket_name" {
  description = "Name of the artifact bucket."
  value       = aws_s3_bucket.artifacts.bucket
}
