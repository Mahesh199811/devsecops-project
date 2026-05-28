# DevOps Practice - Day 2

This Day 2 practice covers pushing a Docker image from Jenkins to a container
registry. You can use either Docker Hub or AWS ECR.

## Practice Goal

By the end of this setup, the flow should work like this:

```text
Push code to GitHub -> Jenkins builds image -> Jenkins pushes image to registry
```

Tools configured in this practice:

- Docker for building the application image
- Jenkins for CI/CD automation
- Docker Hub or AWS ECR for storing the image
- Jenkins credentials for securely storing registry secrets

## Quick Validation Checklist

Use this checklist before running the pipeline:

```bash
docker --version
docker ps
aws --version
```

Expected result:

- Docker is installed and usable by the `jenkins` user.
- Jenkins can build the Docker image.
- Registry credentials are stored in Jenkins, not in Git.
- For AWS ECR, AWS CLI is installed on the Jenkins server.

## 1. Confirm Jenkins Can Use Docker

The `jenkins` user must be in the Docker group:

```bash
sudo usermod -aG docker jenkins
sudo systemctl restart jenkins
```

After Jenkins restarts, run the pipeline again or test from the Jenkins server:

```bash
docker ps
```

If Docker still fails from Jenkins, restart the server or reconnect the Jenkins
agent so the new group membership is active.

## 2. Add Registry Parameters to Jenkinsfile

The project `Jenkinsfile` supports both Docker Hub and AWS ECR using build
parameters:

```groovy
parameters {
    choice(
        name: 'REGISTRY_TYPE',
        choices: ['dockerhub', 'ecr'],
        description: 'Container registry to push the image to.'
    )
    string(
        name: 'DOCKERHUB_REPOSITORY',
        defaultValue: 'your-dockerhub-username/flask-app',
        description: 'Docker Hub repository, for example username/flask-app.'
    )
    string(
        name: 'ECR_REGISTRY',
        defaultValue: '123456789012.dkr.ecr.ap-south-1.amazonaws.com',
        description: 'AWS ECR registry URL.'
    )
    string(
        name: 'ECR_REPOSITORY',
        defaultValue: 'flask-app',
        description: 'AWS ECR repository name.'
    )
    string(
        name: 'AWS_REGION',
        defaultValue: 'ap-south-1',
        description: 'AWS region for ECR.'
    )
}
```

The pipeline tags images with:

```text
BUILD_NUMBER
latest
```

## 3. Install Required Jenkins Plugins

Install these plugins from Jenkins:

```text
Manage Jenkins -> Plugins -> Available plugins
```

Required plugins:

```text
Docker Pipeline
AWS Credentials
```

Restart Jenkins if the plugin installation asks for it.

## 4. Option A: Push to Docker Hub

Create a Docker Hub access token:

```text
Docker Hub -> Account Settings -> Personal access tokens -> Generate new token
```

Add the token in Jenkins:

```text
Manage Jenkins -> Credentials -> System -> Global credentials -> Add Credentials
Kind: Username with password
ID: dockerhub-credentials
Username: YOUR_DOCKERHUB_USERNAME
Password: YOUR_DOCKERHUB_ACCESS_TOKEN
```

Run the Jenkins pipeline with these parameters:

```text
REGISTRY_TYPE: dockerhub
DOCKERHUB_REPOSITORY: YOUR_DOCKERHUB_USERNAME/flask-app
```

Jenkins will push:

```text
YOUR_DOCKERHUB_USERNAME/flask-app:BUILD_NUMBER
YOUR_DOCKERHUB_USERNAME/flask-app:latest
```

Verify from your machine or Jenkins server:

```bash
docker pull YOUR_DOCKERHUB_USERNAME/flask-app:latest
```

## 5. Option B: Push to AWS ECR

Install AWS CLI on the Jenkins server:

```bash
sudo apt update
sudo apt install -y unzip
curl "https://awscli.amazonaws.com/awscli-exe-linux-x86_64.zip" -o "awscliv2.zip"
unzip awscliv2.zip
sudo ./aws/install
aws --version
```

Create the ECR repository:

```bash
aws ecr create-repository --repository-name flask-app --region ap-south-1
```

Add AWS credentials in Jenkins:

```text
Manage Jenkins -> Credentials -> System -> Global credentials -> Add Credentials
Kind: AWS Credentials
ID: aws-credentials
Access key ID: YOUR_AWS_ACCESS_KEY
Secret access key: YOUR_AWS_SECRET_KEY
```

The AWS IAM user or role needs permission for:

```text
ecr:GetAuthorizationToken
ecr:BatchCheckLayerAvailability
ecr:InitiateLayerUpload
ecr:UploadLayerPart
ecr:CompleteLayerUpload
ecr:PutImage
```

Run the Jenkins pipeline with these parameters:

```text
REGISTRY_TYPE: ecr
ECR_REGISTRY: AWS_ACCOUNT_ID.dkr.ecr.ap-south-1.amazonaws.com
ECR_REPOSITORY: flask-app
AWS_REGION: ap-south-1
```

Jenkins will push:

```text
AWS_ACCOUNT_ID.dkr.ecr.ap-south-1.amazonaws.com/flask-app:BUILD_NUMBER
AWS_ACCOUNT_ID.dkr.ecr.ap-south-1.amazonaws.com/flask-app:latest
```

Verify the image in ECR:

```bash
aws ecr describe-images --repository-name flask-app --region ap-south-1
```

## 6. Common Errors

Docker permission error:

```text
permission denied while trying to connect to the Docker daemon socket
```

Fix:

```bash
sudo usermod -aG docker jenkins
sudo systemctl restart jenkins
```

Docker Hub authentication error:

```text
denied: requested access to the resource is denied
```

Fix:

```text
Check DOCKERHUB_REPOSITORY and dockerhub-credentials.
```

ECR login error:

```text
Unable to locate credentials
```

Fix:

```text
Check aws-credentials in Jenkins and confirm the AWS Credentials plugin is installed.
```

## Final Flow

```text
Developer pushes code to GitHub
        |
        v
Jenkins receives the webhook
        |
        v
Jenkins builds the Docker image
        |
        v
Jenkins logs in to Docker Hub or AWS ECR
        |
        v
Jenkins pushes BUILD_NUMBER and latest image tags
```
