pipeline {
    agent any

    environment {
        IMAGE_NAME = 'flask-app'
    }

    stages {
        stage('Checkout') {
            steps {
                git branch: 'main', url: 'https://github.com/Mahesh199811/devsecops-project.git'
            }
        }
       stage('Build Docker Image') {
            steps {
                sh 'docker build -t ${IMAGE_NAME} ./app'
            }
        }
       stage('Run Container'){
            steps {
                sh 'docker rm -f flask-app-container || true'
                sh 'docker run -d --name flask-app-container -p 5001:5001 ${IMAGE_NAME}'
            }
        }
    }
}
