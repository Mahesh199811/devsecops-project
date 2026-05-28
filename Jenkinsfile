pipeline {
    agent any

    environment {
        IMAGE_NAME = 'flask-app'
        DOCKERHUB_REPO = 'maheshgadhave82/flask-app'
        AWS_ACCOUNT_ID = '659093653742'
        AWS_REGION = 'ap-south-1'
        ECR_REPO = 'flask-app'
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
        stage('Push Image to AWS ECR') {
            steps {
                withCredentials([[$class: 'AmazonWebServicesCredentialsBinding',
                credentialsId: 'aws-creds',
                accessKeyVariable: 'AWS_ACCESS_KEY_ID',
                secretKeyVariable: 'AWS_SECRET_ACCESS_KEY'
                ]]) {
                    sh '''
                        AWS_ECR_REGISTRY=${AWS_ACCOUNT_ID}.dkr.ecr.${AWS_REGION}.amazonaws.com
                        ECR_IMAGE=${AWS_ECR_REGISTRY}/${ECR_REPO}:latest

                        aws ecr get-login-password --region ${AWS_REGION} | docker login --username AWS --password-stdin ${AWS_ECR_REGISTRY}

                        docker tag ${IMAGE_NAME}:latest ${ECR_IMAGE}
                        docker push ${ECR_IMAGE}

                        docker logout ${AWS_ECR_REGISTRY}
                    '''
                }
            }
        }
    }
}