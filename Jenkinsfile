pipeline {
    agent any

    environment {
        IMAGE_NAME = 'flask-app'
        DOCKERHUB_REPO = 'maheshgadhave82/flask-app'
    }

    stages {
        stage('Checkout') {
            steps {
                checkout scm
            }
        }
        stage('Build Docker Image') {
            steps {
                sh 'docker build -t ${IMAGE_NAME} ./app'
            }
        }
        stage('Run Container') {
            steps {
                sh 'docker rm -f flask-app-container || true'
                sh 'docker run -d --name flask-app-container -p 5001:5001 ${IMAGE_NAME}'
            }
        }
        stage('Push Image to Docker Hub') {
            steps {
                withCredentials([usernamePassword(
                    credentialsId: 'dockerhub-credentials',
                    usernameVariable: 'DOCKERHUB_USER',
                    passwordVariable: 'DOCKERHUB_PASS'
                )]) {
                    sh '''
                        echo "$DOCKERHUB_PASS" | docker login -u "$DOCKERHUB_USER" --password-stdin
                        docker tag ${IMAGE_NAME} ${DOCKERHUB_REPO}:latest
                        docker push ${DOCKERHUB_REPO}:latest
                        docker logout
                    '''
                }
            }
        }
    }