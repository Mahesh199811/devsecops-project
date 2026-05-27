# DevOps Practice - Day 1

This Day 1 practice covers setting up Git, Docker, Jenkins, a Jenkins pipeline, and a GitHub webhook trigger so a build starts automatically after code is pushed.

## Practice Goal

By the end of this setup, the flow should work like this:

```text
Push code to GitHub -> GitHub webhook -> Jenkins pipeline starts automatically
```

Tools configured in this practice:

- Git for source control
- Docker for container support on the server
- Jenkins for CI/CD automation
- GitHub webhook for automatic pipeline trigger

## Quick Validation Checklist

Use this checklist after setup:

```bash
git --version
docker --version
docker ps
sudo systemctl status jenkins
```

Expected result:

- Git is installed and configured with username and email.
- Docker runs without permission errors for the `ubuntu` user.
- Jenkins is running on port `8080`.
- Jenkins built-in node is online.
- A push to GitHub starts the Jenkins pipeline automatically.

## 1. Configure Git on Linux Server

Install Git:

```bash
sudo apt update
sudo apt install -y git
git --version
```

Set Git user details:

```bash
git config --global user.name "Your Name"
git config --global user.email "your-email@example.com"
git config --list
```

For GitHub push access, use SSH:

```bash
ssh-keygen -t ed25519 -C "your-email@example.com"
cat ~/.ssh/id_ed25519.pub
```

Add the public key in GitHub:

```text
GitHub -> Settings -> SSH and GPG keys -> New SSH key
```

Set the repository remote to SSH:

```bash
git remote set-url origin git@github.com:USERNAME/REPO.git
ssh -T git@github.com
```

## 2. Install Docker

Install Docker on Ubuntu:

```bash
sudo apt update
sudo apt install -y ca-certificates curl gnupg

sudo install -m 0755 -d /etc/apt/keyrings

curl -fsSL https://download.docker.com/linux/ubuntu/gpg | \
  sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg

sudo chmod a+r /etc/apt/keyrings/docker.gpg

echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] \
  https://download.docker.com/linux/ubuntu \
  $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | \
  sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

sudo apt update
sudo apt install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
```

Start Docker:

```bash
sudo systemctl enable --now docker
docker --version
```

Allow the `ubuntu` user to run Docker without `sudo`:

```bash
sudo usermod -aG docker ubuntu
newgrp docker
```

Test Docker:

```bash
docker ps
docker run hello-world
```

## 3. Install Jenkins

Install Java:

```bash
sudo apt update
sudo apt install -y fontconfig openjdk-21-jre
java -version
```

Add the Jenkins repository:

```bash
sudo mkdir -p /etc/apt/keyrings

sudo wget -O /etc/apt/keyrings/jenkins-keyring.asc \
  https://pkg.jenkins.io/debian-stable/jenkins.io-2026.key

echo "deb [signed-by=/etc/apt/keyrings/jenkins-keyring.asc] https://pkg.jenkins.io/debian-stable binary/" | \
  sudo tee /etc/apt/sources.list.d/jenkins.list > /dev/null
```

Install and start Jenkins:

```bash
sudo apt update
sudo apt install -y jenkins
sudo systemctl enable --now jenkins
sudo systemctl status jenkins
```

Open Jenkins:

```text
http://YOUR_SERVER_PUBLIC_IP:8080
```

Get the initial admin password:

```bash
sudo cat /var/lib/jenkins/secrets/initialAdminPassword
```

## 4. Fix Jenkins Built-In Node Offline

The built-in node was offline because Jenkins reported low temporary disk space:

```text
Disk space is below threshold of 1.00 GiB on /tmp
```

Check and clean space:

```bash
df -h /tmp
sudo rm -rf /tmp/*
sudo rm -rf /var/tmp/*
sudo apt clean
sudo apt autoremove -y
sudo systemctl restart jenkins
```

Then bring the node online:

```text
Manage Jenkins -> Nodes -> Built-In Node -> Bring this node back online
```

Make sure the built-in node has at least one executor:

```text
Manage Jenkins -> Nodes -> Built-In Node -> Configure
Number of executors: 1
```

## 5. Allow Jenkins to Use Docker

Add the `jenkins` user to the Docker group:

```bash
sudo usermod -aG docker jenkins
sudo systemctl restart jenkins
```

## 6. Create Jenkins Pipeline

In Jenkins:

```text
Dashboard -> New Item -> Pipeline -> OK
```

Use this basic pipeline:

```groovy
pipeline {
    agent any

    stages {
        stage('Build') {
            steps {
                echo 'Build stage running'
            }
        }

        stage('Test') {
            steps {
                echo 'Test stage running'
            }
        }

        stage('Deploy') {
            steps {
                echo 'Deploy stage running'
            }
        }
    }
}
```

Run it manually:

```text
Build Now -> Build Number -> Console Output
```

## 7. Add Jenkinsfile to GitHub Repo

Create a file named `Jenkinsfile`:

```groovy
pipeline {
    agent any

    stages {
        stage('Checkout') {
            steps {
                checkout scm
            }
        }

        stage('Build') {
            steps {
                echo 'Building from GitHub webhook'
            }
        }

        stage('Test') {
            steps {
                echo 'Running tests'
            }
        }

        stage('Deploy') {
            steps {
                echo 'Deploying'
            }
        }
    }
}
```

Commit and push:

```bash
git add Jenkinsfile
git commit -m "Add Jenkins pipeline"
git push origin main
```

## 8. Configure Jenkins Job for GitHub

In the Jenkins pipeline job:

```text
Dashboard -> Your Pipeline Job -> Configure
```

Enable:

```text
GitHub project
```

Set project URL:

```text
https://github.com/USERNAME/REPO
```

Enable build trigger:

```text
GitHub hook trigger for GITScm polling
```

Set pipeline source:

```text
Definition: Pipeline script from SCM
SCM: Git
Repository URL: https://github.com/USERNAME/REPO.git
Branch Specifier: */main
Script Path: Jenkinsfile
```

## 9. Add GitHub Webhook

In GitHub:

```text
Repo -> Settings -> Webhooks -> Add webhook
```

Use:

```text
Payload URL: http://YOUR_SERVER_PUBLIC_IP:8080/github-webhook/
Content type: application/json
Secret: empty for now
Events: Just the push event
Active: checked
```

The webhook URL must end with:

```text
/github-webhook/
```

## 10. Open AWS Security Group

In the EC2 security group inbound rules, allow Jenkins:

```text
Type: Custom TCP
Port: 8080
Source: 0.0.0.0/0
```

For production, restrict the source later.

## 11. Test Automatic Build

Push a test commit:

```bash
git commit --allow-empty -m "test webhook"
git push origin main
```

Jenkins should trigger the build automatically.

Check webhook delivery:

```text
GitHub repo -> Settings -> Webhooks -> Recent Deliveries
```

Status meanings:

```text
200 = Jenkins received webhook
403/404 = wrong URL or Jenkins blocked it
Timeout = AWS security group or firewall issue
```

Check Jenkins logs if needed:

```bash
sudo journalctl -u jenkins -n 100 --no-pager
```

## Final Flow

```text
Developer pushes code to GitHub
        |
        v
GitHub webhook sends event to Jenkins
        |
        v
Jenkins receives /github-webhook/
        |
        v
Jenkins pulls Jenkinsfile from GitHub
        |
        v
Pipeline runs automatically
```
