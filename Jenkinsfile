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
        
        // SonarQube
        SONARQUBE_SERVER = 'http://sonarqube:9000'  // Use service name for Docker network
        SONARQUBE_PROJECT_KEY = 'HealthSyncWebAPI'
        SONARQUBE_PROJECT_NAME = 'HealthSync Web API'
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

        stage('SonarQube Analysis') {
            steps {
                script {
                    echo "========== STAGE: SonarQube Analysis =========="
                    withSonarQubeEnv('SonarQube') {
                        sh """
                            dotnet tool install --global dotnet-sonarscanner --version 5.14.0 || true
                            export PATH="\\\$PATH:/root/.dotnet/tools"
                            
                            dotnet sonarscanner begin \\
                              /k:"\\\${SONARQUBE_PROJECT_KEY}" \\
                              /n:"\\\${SONARQUBE_PROJECT_NAME}" \\
                              /v:"\\\${BUILD_NUMBER}" \\
                              /d:sonar.login="\\\${SONAR_AUTH_TOKEN}" \\
                              /d:sonar.host.url="\\\${SONAR_HOST_URL}" \\
                              /d:sonar.cs.opencover.reportsPaths="test-results/**/coverage.opencover.xml" \\
                              /d:sonar.exclusions="**/Migrations/**,**/*.Tests/**,**/*.Test/**" \\
                              /d:sonar.qualitygate.wait=true \\
                              /d:sonar.qualitygate.timeout=300
                            
                            dotnet build HealthSyncWebAPI.sln -c Release
                            
                            find . -name "*.Tests.csproj" -type f | while read testproj; do
                                echo "Running tests for SonarQube: \\\$testproj"
                                dotnet test "\\\$testproj" -c Release --no-build \\
                                  --collect:"XPlat Code Coverage" \\
                                  --results-directory ./test-results \\
                                  --logger "junit;LogFileName=test-results.xml" || true
                            done
                            
                            dotnet sonarscanner end /d:sonar.login="\\\${SONAR_AUTH_TOKEN}"
                        """
                }
            }
        }



        stage('Run Unit Tests') {
            steps {
                script {
                    echo "========== STAGE: Run Unit Tests =========="
                    sh """
if [ ! -d "test-results" ] || [ -z "\\\$(ls -A test-results/*.xml 2>/dev/null)" ]; then
    echo "Running tests (not run by SonarQube)..."
    find . -name "*.Tests.csproj" -type f | while read testproj; do
        echo "Running tests: \\\$testproj"
        dotnet test "\\\$testproj" -c Release --no-build --verbosity normal \\
            --collect:"XPlat Code Coverage" \\
            --results-directory ./test-results \\
            --logger "junit;LogFileName=test-results.xml" || true
    done
else
    echo "Tests already run by SonarQube stage, skipping..."
fi
"""
                    echo "✓ Unit tests completed"
                }
            }
            post {
                always {
                    script {
                        echo "Publishing test results..."
                        // Publish JUnit test results
                        junit 'test-results/**/*.xml'
                        
                        echo "Generating coverage reports..."
                        // Install ReportGenerator if not available
                        sh '''
                            dotnet tool install -g dotnet-reportgenerator-globaltool --version 5.1.26 || true
                            export PATH="$PATH:/root/.dotnet/tools"
                            
                            # Generate HTML coverage reports
                            reportgenerator \
                                -reports:"test-results/*/coverage.cobertura.xml" \
                                -targetdir:"test-results/coverage-report" \
                                -reporttypes:Html
                        '''
                        
                        // Publish HTML coverage report
                        publishHTML([
                            allowMissing: true,
                            alwaysLinkToLastBuild: true,
                            keepAll: true,
                            reportDir: 'test-results/coverage-report',
                            reportFiles: 'index.html',
                            reportName: 'Coverage Report',
                            reportTitles: 'Code Coverage'
                        ])
                    }
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
                        withCredentials([file(credentialsId: 'dev-env-file-healthsync', variable: 'ENV_FILE_PATH')]) {
                            sh '''
                                echo "Deploying development stack..."
                                # Download docker-compose standalone
                                curl -SL https://github.com/docker/compose/releases/download/v2.29.0/docker-compose-linux-x86_64 -o ./docker-compose
                                chmod +x ./docker-compose
                                # Copy env file
                                cp $ENV_FILE_PATH .env.dev
                                # Stop and remove existing containers
                                ./docker-compose -f docker-compose.yml --env-file .env.dev down --remove-orphans --volumes || true
                                docker rm -f healthsync-db healthsync-minio healthsync-nginx healthsync-api-1 healthsync-api-2 || true
                                # Use standalone docker-compose
                                ./docker-compose -f docker-compose.yml --env-file .env.dev up -d --remove-orphans
                                sleep 10
                                ./docker-compose ps
                            '''
                        }
                    } else if (params.ENVIRONMENT == 'prod') {
                        withCredentials([file(credentialsId: 'dev-prod-file-healthsync', variable: 'ENV_FILE_PATH')]) {
                            sh '''
                                echo "Deploying production stack..."
                                # Download docker-compose standalone
                                curl -SL https://github.com/docker/compose/releases/download/v2.29.0/docker-compose-linux-x86_64 -o ./docker-compose
                                chmod +x ./docker-compose
                                # Copy env file
                                cp $ENV_FILE_PATH .env.prod
                                # Stop and remove existing containers
                                ./docker-compose -f docker-compose.prod.yml --env-file .env.prod down --remove-orphans --volumes || true
                                docker rm -f healthsync-db healthsync-minio healthsync-nginx healthsync-api-1 healthsync-api-2 || true
                                # Use standalone docker-compose
                                ./docker-compose -f docker-compose.prod.yml --env-file .env.prod up -d --remove-orphans
                                sleep 10
                                ./docker-compose -f docker-compose.prod.yml ps
                            '''
                        }
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
}