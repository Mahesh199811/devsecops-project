# DevOps Practice - Day 2

This Day 2 practice covers pushing a Docker image from Jenkins to container
registries. The current pipeline pushes the same image to Docker Hub and AWS
ECR.

## Practice Goal

By the end of this setup, the flow should work like this:

```text
Push code to GitHub -> Jenkins builds image -> Jenkins pushes image to registries
```

Tools configured in this practice:

- Docker for building the application image
- Jenkins for CI/CD automation
- Docker Hub and AWS ECR for storing the image
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

## 2. Configure Image and Registry Values in Jenkinsfile

The project `Jenkinsfile` defines the image name, image tag, Docker Hub
repository, and AWS ECR settings in the `environment` block:

```groovy
environment {
    IMAGE_NAME = 'flask-app'
    IMAGE_TAG = "${BUILD_NUMBER}"
    DOCKERHUB_REPO = 'maheshgadhave82/flask-app'
    AWS_ACCOUNT_ID = '659093653742'
    AWS_REGION = 'ap-south-1'
    ECR_REPO = 'flask-app'
}
```

The pipeline tags images with:

```text
flask-app:BUILD_NUMBER
flask-app:latest
```

For Docker Hub, the pipeline pushes:

```text
maheshgadhave82/flask-app:BUILD_NUMBER
maheshgadhave82/flask-app:latest
```

For AWS ECR, the pipeline pushes:

```text
659093653742.dkr.ecr.ap-south-1.amazonaws.com/flask-app:BUILD_NUMBER
659093653742.dkr.ecr.ap-south-1.amazonaws.com/flask-app:latest
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

## 4. Push to Docker Hub

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

Jenkins will push:

```text
maheshgadhave82/flask-app:BUILD_NUMBER
maheshgadhave82/flask-app:latest
```

Verify from your machine or Jenkins server:

```bash
docker pull maheshgadhave82/flask-app:latest
```

## 5. Push to AWS ECR

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
ID: aws-creds
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

Jenkins will push:

```text
659093653742.dkr.ecr.ap-south-1.amazonaws.com/flask-app:BUILD_NUMBER
659093653742.dkr.ecr.ap-south-1.amazonaws.com/flask-app:latest
```

Verify the image in ECR:

```bash
aws ecr describe-images --repository-name flask-app --region ap-south-1
```

## 6. Add Post Actions

The pipeline includes post actions that run after the stages complete:

```groovy
post {
    success {
        echo "Pipeline completed successfully. Image tag pushed: ${IMAGE_TAG}"
    }
    failure {
        echo "Pipeline failed. Check the stage logs for the error."
    }
    always {
        sh '''
            docker rm -f flask-app-container || true
            docker logout || true
            docker logout ${AWS_ACCOUNT_ID}.dkr.ecr.${AWS_REGION}.amazonaws.com || true
            docker image prune -f || true
        '''
    }
}
```

What it does:

- Prints a success message when the image push completes.
- Prints a failure message when any stage fails.
- Removes the test container after every run.
- Logs out of Docker Hub and ECR.
- Cleans dangling Docker images from the Jenkins agent.

## 7. Common Errors

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
Check DOCKERHUB_REPO in Jenkinsfile and dockerhub-credentials in Jenkins.
```

ECR login error:

```text
Unable to locate credentials
```

Fix:

```text
Check aws-creds in Jenkins and confirm the AWS Credentials plugin is installed.
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
