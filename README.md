# DevSecOps Project

This repository is a starter DevSecOps project that combines application code,
containerization, CI/CD, infrastructure as code, Kubernetes deployment,
GitOps, monitoring, and security scanning.

The sample application is a small Flask service that exposes a health endpoint.
The surrounding folders show how the service can be built, scanned, deployed,
and monitored using common DevSecOps tooling.

## Project Structure

```text
devsecops-project/
├── app/
│   ├── src/
│   │   └── main.py
│   ├── Dockerfile
│   └── requirements.txt
├── jenkins/
│   └── Jenkinsfile
├── terraform/
│   ├── main.tf
│   ├── variables.tf
│   ├── outputs.tf
│   └── provider.tf
├── kubernetes/
│   ├── deployment.yaml
│   ├── service.yaml
│   ├── ingress.yaml
│   └── namespace.yaml
├── argocd/
│   └── application.yaml
├── monitoring/
│   ├── prometheus.yaml
│   └── grafana-dashboard.json
├── security/
│   ├── trivy-scan.sh
│   └── sonarqube-config/
│       └── sonar-project.properties
└── README.md
```

## Components

- `app/`: Flask application source code, Python dependencies, and Dockerfile.
- `jenkins/`: Jenkins pipeline definition for checkout, dependency install,
  security scanning, and Docker image build.
- `terraform/`: Infrastructure as code configuration for cloud resources.
- `kubernetes/`: Kubernetes manifests for namespace, deployment, service, and ingress.
- `argocd/`: Argo CD application manifest for GitOps-based deployment.
- `monitoring/`: Prometheus scrape configuration and Grafana dashboard placeholder.
- `security/`: Security tooling configuration, including Trivy image scanning and
  SonarQube project settings.

## File Details

### Application

- `app/src/main.py`: Defines the Flask application. It creates a simple web
  service with a `/` endpoint that returns a JSON health response showing the
  service is running.
- `app/requirements.txt`: Lists Python dependencies required by the application.
  Currently it installs Flask.
- `app/Dockerfile`: Builds a Docker image for the Flask app. It starts from a
  Python base image, installs dependencies, copies the app source code, exposes
  port `5001`, and starts `main.py`.

### CI/CD

- `jenkins/Jenkinsfile`: Defines the Jenkins pipeline. It checks out the source
  code, installs Python dependencies, runs the Trivy security scan script, and
  builds the Docker image.

### Infrastructure as Code

- `terraform/provider.tf`: Configures Terraform and the AWS provider version.
  It also sets the AWS region using the `aws_region` variable.
- `terraform/variables.tf`: Defines reusable Terraform input variables such as
  `aws_region` and `project_name`.
- `terraform/main.tf`: Defines the cloud infrastructure resources. Currently it
  creates an S3 bucket that can be used for build artifacts or project storage.
- `terraform/outputs.tf`: Prints useful Terraform output values after apply,
  such as the created artifact bucket name.

### Kubernetes

- `kubernetes/namespace.yaml`: Creates the `devsecops` namespace so application
  resources are grouped separately inside the cluster.
- `kubernetes/deployment.yaml`: Defines how the Flask app runs in Kubernetes.
  It creates two replicas of the container and exposes container port `5001`.
- `kubernetes/service.yaml`: Creates a stable internal Kubernetes service for
  the app. It maps service port `80` to container port `5001`.
- `kubernetes/ingress.yaml`: Defines external HTTP routing for the app using the
  host `devsecops.local` and forwards traffic to the Kubernetes service.

### GitOps

- `argocd/application.yaml`: Defines an Argo CD application. Argo CD uses this
  file to sync the Kubernetes manifests from Git into the Kubernetes cluster.
  Update the `repoURL` value before using it with your real repository.

### Monitoring

- `monitoring/prometheus.yaml`: Defines a Prometheus scrape configuration for
  collecting metrics from the application service.
- `monitoring/grafana-dashboard.json`: Placeholder Grafana dashboard definition.
  It can be extended with panels for application health, latency, error rate,
  CPU, memory, and deployment metrics.

### Security

- `security/trivy-scan.sh`: Runs a Trivy container image vulnerability scan. By
  default it scans `devsecops-project:latest` and fails when high or critical
  vulnerabilities are found.
- `security/sonarqube-config/sonar-project.properties`: Defines SonarQube
  project metadata and tells SonarQube to scan the `app/src` source directory.

## DevSecOps Flow

1. Developer pushes code to the repository.
2. Jenkins runs the CI pipeline.
3. Dependencies are installed and the application is checked.
4. Trivy scans the Docker image for high and critical vulnerabilities.
5. Jenkins builds the Docker image.
6. Kubernetes manifests define how the app runs in the cluster.
7. Argo CD syncs the Kubernetes manifests from Git to the cluster.
8. Prometheus and Grafana provide monitoring visibility.

## Run Locally

Install dependencies:

```bash
pip install -r app/requirements.txt
```

Start the Flask app:

```bash
python app/src/main.py
```

Test the health endpoint:

```bash
curl http://localhost:5001/
```

Expected response:

```json
{
  "service": "devsecops-project",
  "status": "ok"
}
```

## Build Docker Image

```bash
docker build -t devsecops-project:latest app
docker run -p 5001:5001 devsecops-project:latest
```

## Push Docker Image from Jenkins

The Jenkins pipeline can push the built image to either Docker Hub or AWS ECR.
It tags each image with the Jenkins build number and `latest`.

### Docker Hub

Create a Jenkins credential:

```text
Kind: Username with password
ID: dockerhub-credentials
Username: your Docker Hub username
Password: your Docker Hub access token
```

Run the pipeline with:

```text
REGISTRY_TYPE: dockerhub
DOCKERHUB_REPOSITORY: your-dockerhub-username/flask-app
```

### AWS ECR

Install the AWS CLI on the Jenkins agent and make sure the ECR repository exists:

```bash
aws ecr create-repository --repository-name flask-app --region ap-south-1
```

Create a Jenkins credential:

```text
Kind: AWS Credentials
ID: aws-credentials
Access key ID: your AWS access key
Secret access key: your AWS secret key
```

Run the pipeline with:

```text
REGISTRY_TYPE: ecr
ECR_REGISTRY: 123456789012.dkr.ecr.ap-south-1.amazonaws.com
ECR_REPOSITORY: flask-app
AWS_REGION: ap-south-1
```

The Jenkins agent needs Docker, AWS CLI, Docker Pipeline plugin, and AWS
Credentials plugin installed.

## Run Security Scan

Install Trivy first, then run:

```bash
bash security/trivy-scan.sh
```

The script scans `devsecops-project:latest` by default. You can override the
image name with:

```bash
IMAGE_NAME=my-image:tag bash security/trivy-scan.sh
```

## Deploy to Kubernetes

Apply the manifests:

```bash
kubectl apply -f kubernetes/namespace.yaml
kubectl apply -f kubernetes/
```

Check the deployment:

```bash
kubectl get all -n devsecops
```

## GitOps with Argo CD

Update `argocd/application.yaml` with the correct Git repository URL:

```yaml
repoURL: https://github.com/Mahesh199811/devsecops-project.git
```

Then apply it to the Argo CD namespace:

```bash
kubectl apply -f argocd/application.yaml
```

## Notes

- Replace placeholder repository and image names before using this in a real
  environment.
- Add automated tests before promoting this pipeline to production use.
- Store secrets in a secure secret manager or Kubernetes secrets, not in Git.
