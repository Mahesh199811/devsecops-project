# DevOps Practice - Day 3

This Day 3 practice covers integrating security scanning into the Jenkins pipeline:
- **SonarQube** for static code analysis (SAST)
- **Trivy** for container vulnerability scanning

## Practice Goal

By the end of this setup, the flow should work like this:

```text
Push code to GitHub → Jenkins pipeline starts 
  → Code Analysis (SonarQube)
  → Build Docker Image
  → Vulnerability Scan (Trivy)
  → Test Application
  → Push to Registries
```

Tools configured in this practice:

- SonarQube for source code security and quality analysis
- Trivy for container image vulnerability detection
- Jenkins plugins for SonarQube integration

## Quick Validation Checklist

Use this checklist after setup:

```bash
sonar-scanner --version
trivy --version
docker ps | grep sonarqube
curl http://localhost:9000
```

Expected result:

- SonarQube is running on port `9000`
- Trivy is installed on the Jenkins server
- Jenkins has SonarQube plugin installed
- The pipeline runs security stages without failures

## 1. Install and Configure SonarQube

### Option A: Install SonarQube on Jenkins Server

Install Java (SonarQube requirement):

```bash
sudo apt update
sudo apt install -y openjdk-17-jdk
java -version
```

Download and install SonarQube:

```bash
sudo useradd sonarqube || true
cd /opt
sudo wget https://binaries.sonarsource.com/Distribution/sonarqube/sonarqube-9.9.1.69595.zip
sudo unzip sonarqube-9.9.1.69595.zip
sudo mv sonarqube-9.9.1.69595 sonarqube
sudo chown -R sonarqube:sonarqube /opt/sonarqube
```

Start SonarQube:

```bash
sudo su - sonarqube
cd /opt/sonarqube/bin/linux-x86-64
./sonar.sh start
exit
```

Access SonarQube:

```
http://localhost:9000
```

Default credentials: `admin` / `admin`

### Option B: Run SonarQube in Docker

```bash
docker run -d \
  --name sonarqube \
  -p 9000:9000 \
  -e SONAR_JDBC_URL=jdbc:postgresql://db:5432/sonarqube \
  -e SONAR_JDBC_USERNAME=sonarqube \
  -e SONAR_JDBC_PASSWORD=sonarqube \
  sonarqube:latest
```

## 2. Create SonarQube Project and Token

1. Log in to SonarQube (`http://localhost:9000`)
2. Go to **Administration → Security → Users** (for token generation)
3. Create a project:
   - **Project Key**: `devsecops-project`
   - **Project Name**: `DevSecOps Project`
4. Generate an authentication token:
   - Go to your user profile → **Security → Generate Tokens**
   - Token name: `jenkins-sonarqube`
   - Copy the token

## 3. Install SonarQube Scanner on Jenkins Server

Download sonar-scanner:

```bash
cd /opt
sudo wget https://binaries.sonarsource.com/Distribution/sonar-scanner-cli/sonar-scanner-cli-4.8.0.2856-linux.zip
sudo unzip sonar-scanner-cli-4.8.0.2856-linux.zip
sudo mv sonar-scanner-4.8.0.2856-linux sonar-scanner
sudo chown -R jenkins:jenkins /opt/sonar-scanner
```

Verify installation:

```bash
/opt/sonar-scanner/bin/sonar-scanner --version
```

## 4. Configure Jenkins Credentials for SonarQube

Add SonarQube token to Jenkins:

1. Go to **Manage Jenkins → Manage Credentials**
2. Click **Add Credentials** (or under Jenkins store → Global credentials)
3. Select **Secret text**
4. Paste your SonarQube token in the **Secret** field
5. Set **ID**: `sonarqube-token`
6. Click **Create**

Install Jenkins SonarQube Plugin:

1. Go to **Manage Jenkins → Manage Plugins → Available**
2. Search for `SonarQube Scanner`
3. Install it and restart Jenkins

Configure SonarQube Server in Jenkins:

1. Go to **Manage Jenkins → Configure System**
2. Scroll to **SonarQube servers**
3. Click **Add SonarQube**:
   - **Name**: `SonarQube`
   - **Server URL**: `http://localhost:9000`
   - **Server authentication token**: Select `sonarqube-token`
4. Click **Save**

## 5. Install Trivy on Jenkins Server

Trivy is a container vulnerability scanner that will be auto-installed by the pipeline if not present. However, you can pre-install it:

```bash
sudo apt-get update
sudo apt-get install -y wget apt-transport-https gnupg lsb-release

wget -qO - https://aquasecurity.github.io/trivy-repo/deb/public.key | sudo apt-key add -
echo "deb https://aquasecurity.github.io/trivy-repo/deb $(lsb_release -sc) main" | sudo tee /etc/apt/sources.list.d/trivy.list

sudo apt-get update
sudo apt-get install -y trivy
```

Verify Trivy installation:

```bash
trivy --version
```

## 6. Pipeline Stages Explanation

### SonarQube Code Analysis Stage

```groovy
stage('SonarQube Code Analysis') {
    steps {
        withSonarQubeEnv('SonarQube') {
            sh '''
                /opt/sonar-scanner/bin/sonar-scanner \
                    -Dsonar.projectKey=devsecops-project \
                    -Dsonar.sources=app/src \
                    -Dsonar.host.url=${SONARQUBE_URL} \
                    -Dsonar.login=${SONARQUBE_TOKEN} \
                    -Dsonar.python.version=3.12
            '''
        }
    }
}
```

This stage:
- Scans Python source code in `app/src`
- Uploads results to SonarQube
- Detects code smells, bugs, and security issues
- Can fail the build if quality gates are not met

### Trivy Container Scan Stage

```groovy
stage('Trivy Container Scan') {
    steps {
        sh '''
            trivy image --exit-code 0 --severity HIGH,CRITICAL \
                --format json --output trivy-report.json ${IMAGE_NAME}:${IMAGE_TAG}
            
            trivy image --severity HIGH,CRITICAL ${IMAGE_NAME}:${IMAGE_TAG}
        '''
    }
}
```

This stage:
- Scans the Docker image for vulnerabilities
- Generates a JSON report (`trivy-report.json`)
- Reports HIGH and CRITICAL severity vulnerabilities
- Uses `--exit-code 0` to report but not fail (change to `1` to enforce strict policy)

## 7. Running the Pipeline

Push code to trigger the pipeline:

```bash
git add .
git commit -m "Add security scanning stages"
git push origin main
```

Monitor the pipeline in Jenkins:

1. Go to **Dashboard → Your Pipeline**
2. Click the latest build
3. View logs for each stage
4. Check SonarQube results at `http://localhost:9000`
5. Review Trivy reports in Jenkins logs or `trivy-report.json`

## 8. Viewing Results

### SonarQube Dashboard

Visit `http://localhost:9000`:
- Project overview with quality metrics
- Issues grouped by severity
- Code coverage and duplication stats
- Security hotspots and vulnerabilities

### Trivy Report

View the generated report:

```bash
cat trivy-report.json | jq '.' | head -50
```

Or check the Jenkins build logs for the Trivy output.

## 9. Next Steps (Optional Enhancements)

### Enforce Quality Gates

Modify Jenkinsfile to fail on SonarQube issues:

```groovy
withSonarQubeEnv('SonarQube') {
    sh '/opt/sonar-scanner/bin/sonar-scanner ...'
    timeout(time: 1, unit: 'MINUTES') {
        waitForQualityGate abortPipeline: true
    }
}
```

### Make Trivy Fail on HIGH/CRITICAL Vulnerabilities

Change `--exit-code 0` to `--exit-code 1` to stop pipeline:

```bash
trivy image --exit-code 1 --severity HIGH,CRITICAL ${IMAGE_NAME}:${IMAGE_TAG}
```

### Archive Security Reports

Add to `post` section:

```groovy
always {
    archiveArtifacts artifacts: 'trivy-report.json', allowEmptyArchive: true
}
```

### Slack Notifications

Notify on security findings:

```groovy
post {
    always {
        script {
            sh 'echo "Security scan completed" | curl -X POST -d @- $SLACK_WEBHOOK'
        }
    }
}
```

## Troubleshooting

### SonarQube Connection Error

**Problem**: `ERROR: Unable to execute SonarScanner`

**Solution**:
- Verify SonarQube is running: `curl http://localhost:9000`
- Check sonar-scanner path: `/opt/sonar-scanner/bin/sonar-scanner --version`
- Verify Jenkins user can access it: `sudo -u jenkins /opt/sonar-scanner/bin/sonar-scanner --version`

### Trivy Installation Fails

**Problem**: `trivy: command not found`

**Solution**:
- The pipeline auto-installs Trivy with `apt-get`
- For manual install, check: `which trivy`
- Verify installation: `trivy --version`

### Pipeline Hangs on Code Analysis

**Problem**: SonarQube analysis takes too long

**Solution**:
- Add timeout: `timeout(time: 5, unit: 'MINUTES') { ... }`
- Check SonarQube server resources
- Review SonarQube logs: `tail -f /opt/sonarqube/logs/sonarqube.log`

## Summary

You now have a DevSecOps pipeline with:

- ✅ Static code analysis (SonarQube)
- ✅ Container vulnerability scanning (Trivy)
- ✅ Automated security gates
- ✅ Compliance reporting

Next step: Deploy to Kubernetes with ArgoCD for GitOps continuous deployment!
