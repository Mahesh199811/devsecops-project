pipeline {
    agent any

    environment {
        IMAGE_NAME = 'dotnet-app'
        IMAGE_TAG = "${BUILD_NUMBER}"
        DOCKERHUB_REPO = 'maheshgadhave82/dotnet-app'
        AWS_ACCOUNT_ID = '659093653742'
        AWS_REGION = 'ap-south-1'
        ECR_REPO = 'dotnet-app'
    }

    stages {
        stage('Checkout') {
            steps {
                checkout scm
            }
        }
        stage('Build Docker Image') {
            steps {
                sh 'docker build -t ${IMAGE_NAME}:${IMAGE_TAG} -t ${IMAGE_NAME}:latest ./app'
            }
        }
        stage('Run Container') {
            steps {
                sh 'docker rm -f dotnet-app-container || true'
                sh 'docker run -d --name dotnet-app-container -p 5001:5001 ${IMAGE_NAME}:${IMAGE_TAG}'
            }
        }
        stage('Test Application') {
            steps {
                sh '''
                    echo "Waiting for .NET app to start..."
                    sleep 5
                    echo "Testing application health..."
                    curl -f http://localhost:5001/health || exit 1
                    echo "Application is running successfully!"
                '''
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
                        docker tag ${IMAGE_NAME}:${IMAGE_TAG} ${DOCKERHUB_REPO}:${IMAGE_TAG}
                        docker tag ${IMAGE_NAME}:${IMAGE_TAG} ${DOCKERHUB_REPO}:latest
                        docker push ${DOCKERHUB_REPO}:${IMAGE_TAG}
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
                        ECR_IMAGE=${AWS_ECR_REGISTRY}/${ECR_REPO}:${IMAGE_TAG}
                        ECR_LATEST_IMAGE=${AWS_ECR_REGISTRY}/${ECR_REPO}:latest

                        aws ecr get-login-password --region ${AWS_REGION} | docker login --username AWS --password-stdin ${AWS_ECR_REGISTRY}

                        docker tag ${IMAGE_NAME}:latest ${ECR_IMAGE}
                        docker tag ${IMAGE_NAME}:latest ${ECR_LATEST_IMAGE}
                        docker push ${ECR_IMAGE}
                        docker push ${ECR_LATEST_IMAGE}

                        docker logout ${AWS_ECR_REGISTRY}
                    '''
                }
            }
        }
    }

    post {
        success {
            echo "Pipeline completed successfully. Image tag pushed: ${IMAGE_TAG}"
        }
        failure {
            echo "Pipeline failed. Check the stage logs for the error."
        }
        always {
            sh '''
                docker rm -f dotnet-app-container || true
                docker logout || true
                docker logout ${AWS_ACCOUNT_ID}.dkr.ecr.${AWS_REGION}.amazonaws.com || true
                docker image prune -f || true
            '''
        }
    }
}
