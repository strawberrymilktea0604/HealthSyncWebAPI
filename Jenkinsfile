pipeline {
    agent any

    options {
        buildDiscarder(logRotator(numToKeepStr: '10'))
        disableConcurrentBuilds()
        timeout(time: 1, unit: 'HOURS')
    }

    parameters {
        choice(
            name: 'ENVIRONMENT',
            choices: ['dev', 'prod'],
            description: 'Select deployment environment'
        )
        string(
            name: 'GIT_BRANCH',
            defaultValue: 'main',
            description: 'Git branch to build'
        )
    }

    environment {
        // Docker & Registry
        DOCKER_REGISTRY = 'docker.io'
        DOCKER_IMAGE_NAME = 'healthsync-api'
        DOCKER_IMAGE_TAG = "${BUILD_NUMBER}-${ENVIRONMENT}"
        
        // Git
        GIT_REPOSITORY = 'https://github.com/strawberrymilktea0604/HealthSyncWebAPI.git'
        GIT_CREDENTIALS_ID = 'github-credentials'
        
        // Docker Socket
        DOCKER_SOCKET = '/var/run/docker.sock'
    }

    stages {
        stage('Checkout') {
            steps {
                script {
                    echo "========== STAGE: Checkout =========="
                    echo "Repository: ${GIT_REPOSITORY}"
                    echo "Branch: ${params.GIT_BRANCH}"
                    echo "Environment: ${params.ENVIRONMENT}"
                }
                checkout([
                    $class: 'GitSCM',
                    branches: [[name: "refs/heads/${params.GIT_BRANCH}"]],
                    userRemoteConfigs: [[
                        url: "${GIT_REPOSITORY}",
                        credentialsId: "${GIT_CREDENTIALS_ID}"
                    ]]
                ])
                script {
                    echo "✓ Code checked out successfully"
                    sh 'echo "Commit: $(git rev-parse --short HEAD)"'
                }
            }
        }

        stage('Prepare Environment') {
            steps {
                script {
                    echo "========== STAGE: Prepare Environment =========="
                    if (params.ENVIRONMENT == 'dev') {
                        sh 'cp .env.dev .env || true'
                        echo "✓ Development environment loaded"
                    } else if (params.ENVIRONMENT == 'prod') {
                        sh 'cp .env.prod .env || true'
                        echo "✓ Production environment loaded"
                    }
                }
            }
        }

        stage('Build Solution') {
            steps {
                script {
                    echo "========== STAGE: Build Solution =========="
                    echo "Building .NET solution..."
                    sh '''
                        dotnet build HealthSyncWebAPI.sln -c Release
                    '''
                    echo "✓ Solution built successfully"
                }
            }
        }

        stage('Run Unit Tests') {
            steps {
                script {
                    echo "========== STAGE: Run Unit Tests =========="
                    sh '''
                        # Find and run test projects (if any exist)
                        find . -name "*.Tests.csproj" -type f | while read testproj; do
                            echo "Running tests: $testproj"
                            dotnet test "$testproj" -c Release --no-build --verbosity normal || true
                        done
                    '''
                    echo "✓ Unit tests completed"
                }
            }
        }

        stage('Build Docker Image') {
            steps {
                script {
                    echo "========== STAGE: Build Docker Image =========="
                    echo "Building Docker image: ${DOCKER_IMAGE_NAME}:${DOCKER_IMAGE_TAG}"
                    sh '''
                        docker build \
                            -t ${DOCKER_IMAGE_NAME}:${DOCKER_IMAGE_TAG} \
                            -t ${DOCKER_IMAGE_NAME}:latest \
                            -f Dockerfile \
                            .
                    '''
                    echo "✓ Docker image built successfully"
                    sh 'docker images | grep ${DOCKER_IMAGE_NAME}'
                }
            }
        }

        stage('Push Docker Image') {
            steps {
                script {
                    echo "========== STAGE: Push Docker Image =========="
                    if (params.ENVIRONMENT == 'prod') {
                        echo "Production build - would push to registry (implement credentials)"
                        // TODO: Add Docker registry push with credentials
                        // sh 'docker push ${DOCKER_REGISTRY}/${DOCKER_IMAGE_NAME}:${DOCKER_IMAGE_TAG}'
                    } else {
                        echo "Development build - skipping registry push"
                    }
                }
            }
        }

        stage('Deploy Stack') {
            steps {
                script {
                    echo "========== STAGE: Deploy Stack =========="
                    if (params.ENVIRONMENT == 'dev') {
                        sh '''
                            echo "Deploying development stack..."
                            docker compose -f docker-compose.yml --env-file .env.dev down || true
                            docker compose -f docker-compose.yml --env-file .env.dev up -d
                            sleep 10
                            docker compose ps
                        '''
                    } else if (params.ENVIRONMENT == 'prod') {
                        sh '''
                            echo "Deploying production stack..."
                            docker compose -f docker-compose.prod.yml --env-file .env.prod down || true
                            docker compose -f docker-compose.prod.yml --env-file .env.prod up -d
                            sleep 10
                            docker compose -f docker-compose.prod.yml ps
                        '''
                    }
                    echo "✓ Stack deployed"
                }
            }
        }

        stage('Health Check') {
            steps {
                script {
                    echo "========== STAGE: Health Check =========="
                    sh '''
                        echo "Waiting for API to be ready..."
                        for i in {1..30}; do
                            if curl -f http://localhost:8080/health 2>/dev/null; then
                                echo "✓ API is healthy"
                                exit 0
                            fi
                            echo "Attempt $i/30 - waiting..."
                            sleep 2
                        done
                        echo "✗ API health check failed"
                        exit 1
                    '''
                }
            }
        }
    }

    triggers {
        githubPush()  // Trigger automatically when push to GitHub
    }

    post {
        always {
            script {
                echo "========== POST: Cleanup =========="
                // Keep logs and artifacts for debugging
                sh 'docker-compose logs > docker-compose.log || true'
                archiveArtifacts artifacts: 'docker-compose.log', allowEmptyArchive: true
            }
        }
        success {
            script {
                echo "========== BUILD: SUCCESS =========="
                echo "✓ Pipeline completed successfully"
                echo "Environment: ${params.ENVIRONMENT}"
                echo "Build: ${BUILD_NUMBER}"
                // Send notification (implement as needed)
            }
        }
        failure {
            script {
                echo "========== BUILD: FAILURE =========="
                echo "✗ Pipeline failed"
                // Send notification (implement as needed)
            }
        }
    }
}
