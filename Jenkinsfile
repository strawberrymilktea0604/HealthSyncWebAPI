pipeline {
    agent any

    options {
        buildDiscarder(logRotator(numToKeepStr: '10'))
        disableConcurrentBuilds()
        timeout(time: 1, unit: 'HOURS')
    }

    // Best Practice: Triggers nên để ở đầu pipeline
    triggers {
        githubPush()
    }

    parameters {
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
        DOCKER_IMAGE_TAG = "${BUILD_NUMBER}-prod"
        
        // Docker Hub - Đọc từ Jenkins credentials
        DOCKER_HUB_USERNAME = credentials('docker-hub-username')
        DOCKER_HUB_PASSWORD = credentials('docker-hub-password')
        DOCKER_HUB_REPO = credentials('docker-hub-repo') // VD: username/healthsync-api
        
        // Production Server (SSH deployment) - Đọc từ Jenkins credentials
        PROD_SERVER_IP = credentials('prod-server-ip')
        PROD_SERVER_USER = credentials('prod-server-user')
        PROD_DEPLOY_DIR = credentials('prod-deploy-dir')
        SSH_CREDENTIALS_ID = 'prod-ssh-key'
        
        // Git
        GIT_REPOSITORY = 'https://github.com/strawberrymilktea0604/HealthSyncWebAPI.git'
        GIT_CREDENTIALS_ID = 'github-credentials'
        
        // Docker Socket
        DOCKER_SOCKET = '/var/run/docker.sock'
        
        // SonarQube
        SONARQUBE_SERVER = 'http://sonarqube:9000'
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
                    echo "Building for: Production"
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

        stage('Prepare Secrets') {
            steps {
                script {
                    echo "========== STAGE: Prepare Secrets =========="
                    withCredentials([file(credentialsId: 'prod-certs-archive', variable: 'CERTS_PKG')]) {
                        echo "--- Đang thiết lập Certificates ---"
                        
                        // Jenkins lưu file secret ở thư mục tạm, ta copy nó về workspace hiện tại
                        // Đổi tên lại thành certs.tar.gz cho dễ xử lý
                        sh 'cp $CERTS_PKG ./certs.tar.gz'
                        
                        // Giải nén file
                        // Lệnh này sẽ bung thư mục 'certs' ra ngay tại đây
                        sh 'tar -xzf certs.tar.gz'
                        
                        // (Tùy chọn) Xóa file nén đi cho sạch
                        sh 'rm certs.tar.gz'
                        
                        // Kiểm tra xem thư mục đã có chưa (để debug)
                        sh 'ls -la certs/'
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

        stage('SonarQube Begin') {
            steps {
                script {
                    echo "========== STAGE: SonarQube Begin =========="
                    withSonarQubeEnv('SonarQube') {
                        sh """
                            dotnet tool install --global dotnet-sonarscanner --version 5.14.0 || true
                            export PATH="\$PATH:/root/.dotnet/tools"
                            
                            dotnet sonarscanner begin \\
                              /k:"${SONARQUBE_PROJECT_KEY}" \\
                              /n:"${SONARQUBE_PROJECT_NAME}" \\
                              /v:"${BUILD_NUMBER}" \\
                              /d:sonar.login="${SONAR_AUTH_TOKEN}" \\
                              /d:sonar.host.url="${SONAR_HOST_URL}" \\
                              /d:sonar.cs.opencover.reportsPaths="test-results/**/coverage.opencover.xml" \\
                              /d:sonar.exclusions="**/Migrations/**,**/*.Tests/**,**/*.Test/**" \\
                              /d:sonar.qualitygate.wait=true \\
                              /d:sonar.qualitygate.timeout=300
                        """
                    }
                }
            }
        }

        stage('Run Unit Tests') {
            steps {
                script {
                    echo "========== STAGE: Run Unit Tests =========="
                    sh '''
                        # clean previous results
                        rm -rf test-results || true
                        mkdir -p test-results

                        # find test projects (adjust glob if your tests live in different folders)
                        TEST_PROJECTS=$(find . -type f -name '*Tests*.csproj' -o -name '*Test*.csproj' || true)
                        if [ -z "$TEST_PROJECTS" ]; then
                            echo "No test project files found by pattern '*Tests*' or '*Test*' - aborting tests"
                            exit 0
                        fi

                        echo "Found test projects:"
                        echo "$TEST_PROJECTS"

                        # loop over projects -> run dotnet test for each and write a distinct JUnit XML
                        for p in $TEST_PROJECTS; do
                            # derive short name for file
                            pname=$(basename "$p" .csproj | sed 's/[^a-zA-Z0-9._-]/_/g')
                            logfile="test-results/TEST-${pname}.xml"
                            echo "Running tests for [$p] -> $logfile"
                            # Note: NO --no-build to be safe; remove newline escaping if you embed in pipeline groovy string
                            dotnet test "$p" -c Release \
                                --collect:"XPlat Code Coverage;Format=opencover" \
                                --results-directory ./test-results \
                                --logger "junit;LogFileName=${logfile}" || true
                        done

                        echo "List generated test results:"
                        ls -la test-results || true
                    '''
                    echo "✓ Unit tests stage finished (see artifacts/test-results)"
                }
            }
            post {
                always {
                    script {
                        echo "Publishing test results..."
                        junit allowEmptyResults: true, testResults: 'test-results/**/TEST-*.xml'
                        echo "Generating coverage (if any)..."
                        sh '''
                            dotnet tool install -g dotnet-reportgenerator-globaltool --version 5.1.26 || true
                            export PATH="$PATH:/root/.dotnet/tools"
                            reportgenerator -reports:"test-results/**/coverage.opencover.xml" -targetdir:"test-results/coverage-report" -reporttypes:Html || true
                        '''
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

        stage('SonarQube End') {
            steps {
                script {
                    echo "========== STAGE: SonarQube End =========="
                    withSonarQubeEnv('SonarQube') {
                        sh """
                            export PATH="\$PATH:/root/.dotnet/tools"
                            dotnet sonarscanner end /d:sonar.login="${SONAR_AUTH_TOKEN}"
                        """
                    }
                }
            }
        }

        stage('Build Docker Image') {
            steps {
                script {
                    echo "========== STAGE: Build Docker Image =========="
                    echo "Building Docker image: ${DOCKER_IMAGE_NAME}:${DOCKER_IMAGE_TAG}"
                    sh """
                        docker build \\
                            -t ${DOCKER_IMAGE_NAME}:${DOCKER_IMAGE_TAG} \\
                            -t ${DOCKER_IMAGE_NAME}:latest \\
                            -f Dockerfile \\
                            .
                    """
                    echo "✓ Docker image built successfully"
                    sh "docker images | grep ${DOCKER_IMAGE_NAME}"
                }
            }
        }

        stage('Push Docker Image') {
            steps {
                script {
                    echo "========== STAGE: Push Docker Image =========="
                    echo "Pushing image to Docker Hub: ${DOCKER_HUB_REPO}"
                    sh '''
                        # Login to Docker Hub (using single quotes to avoid secret interpolation warning)
                        echo "$DOCKER_HUB_PASSWORD" | docker login -u "$DOCKER_HUB_USERNAME" --password-stdin
                        
                        # Tag image with Docker Hub repository name
                        docker tag ''' + DOCKER_IMAGE_NAME + ':' + DOCKER_IMAGE_TAG + ''' ''' + DOCKER_HUB_REPO + ':' + DOCKER_IMAGE_TAG + '''
                        docker tag ''' + DOCKER_IMAGE_NAME + ''':latest ''' + DOCKER_HUB_REPO + ''':latest
                        
                        # Push to Docker Hub
                        docker push ''' + DOCKER_HUB_REPO + ':' + DOCKER_IMAGE_TAG + '''
                        docker push ''' + DOCKER_HUB_REPO + ''':latest
                        
                        echo "\u2713 Image pushed successfully"
                        
                        # Logout from Docker Hub
                        docker logout
                    '''
                }
            }
        }

        stage('Deploy to Production') {
            steps {
                script {
                    echo "========== STAGE: Deploy to Production =========="
                    withCredentials([
                        file(credentialsId: 'prod-env-file-healthsync', variable: 'ENV_FILE_PATH'),
                        sshUserPrivateKey(credentialsId: SSH_CREDENTIALS_ID, keyFileVariable: 'SSH_KEY', usernameVariable: 'SSH_USER')
                    ]) {
                        sh """
                            # 1. Tạo thư mục deploy (nếu chưa có)
                            ssh -p 2222 -o StrictHostKeyChecking=no -i \$SSH_KEY \$SSH_USER@${PROD_SERVER_IP} \
                                "mkdir -p ${PROD_DEPLOY_DIR}"
                            
                            # 2. QUAN TRỌNG: Xóa file .env.prod cũ trước khi copy đè
                            # Giúp tránh lỗi 'Text file busy' hoặc 'Permission denied' do Docker đang lock file
                            ssh -p 2222 -o StrictHostKeyChecking=no -i \$SSH_KEY \$SSH_USER@${PROD_SERVER_IP} \
                                "rm -f ${PROD_DEPLOY_DIR}/.env.prod"

                            # 3. Copy các file cấu hình
                            scp -P 2222 -o StrictHostKeyChecking=no -i \$SSH_KEY \
                                docker-compose.prod.yml \$SSH_USER@${PROD_SERVER_IP}:${PROD_DEPLOY_DIR}/
                            
                            scp -P 2222 -o StrictHostKeyChecking=no -i \$SSH_KEY \
                                nginx.conf \$SSH_USER@${PROD_SERVER_IP}:${PROD_DEPLOY_DIR}/
                            
                            scp -P 2222 -o StrictHostKeyChecking=no -i \$SSH_KEY \
                                Dockerfile.loophole \$SSH_USER@${PROD_SERVER_IP}:${PROD_DEPLOY_DIR}/
                            
                            # Copy thư mục certs (từ Jenkins workspace sau khi giải nén)
                            scp -P 2222 -r -o StrictHostKeyChecking=no -i \$SSH_KEY \
                                certs \$SSH_USER@${PROD_SERVER_IP}:${PROD_DEPLOY_DIR}/
                            
                            # Bây giờ copy file env sẽ an toàn
                            scp -P 2222 -o StrictHostKeyChecking=no -i \$SSH_KEY \
                                \$ENV_FILE_PATH \$SSH_USER@${PROD_SERVER_IP}:${PROD_DEPLOY_DIR}/.env.prod
                            
                            # 4. Thực hiện Deploy
                            ssh -p 2222 -o StrictHostKeyChecking=no -i \$SSH_KEY \$SSH_USER@${PROD_SERVER_IP} "
                                cd ${PROD_DEPLOY_DIR}
                                
                                # Update image tag
                                sed -i 's|image: healthsync-api:latest|image: ${DOCKER_HUB_REPO}:latest|g' docker-compose.prod.yml
                                
                                # Pull & Redeploy
                                docker compose -f docker-compose.prod.yml --env-file .env.prod pull
                                docker compose -f docker-compose.prod.yml --env-file .env.prod down --remove-orphans || true
                                docker compose -f docker-compose.prod.yml --env-file .env.prod up -d --remove-orphans
                                
                                # Wait check
                                sleep 20
                                docker compose -f docker-compose.prod.yml --env-file .env.prod ps
                                
                                # Cleanup images
                                docker image prune -f
                            "
                        """
                    }
                }
            }
        }

        stage('Health Check') {
            steps {
                script {
                    echo "========== STAGE: Health Check =========="
                    withCredentials([
                        sshUserPrivateKey(credentialsId: SSH_CREDENTIALS_ID, keyFileVariable: 'SSH_KEY', usernameVariable: 'SSH_USER')
                    ]) {
                        sh """
                            echo "Checking Health on Production Server..."
                            
                            # SSH vào server để chạy health check từ bên trong
                            ssh -p 2222 -o StrictHostKeyChecking=no -i \$SSH_KEY \$SSH_USER@${PROD_SERVER_IP} "
                                for i in {1..30}; do
                                    # Curl vào localhost của server prod (container đang chạy ở đó)
                                    if curl -k https://localhost:9443/health 2>/dev/null; then
                                        echo '✓ Production API is healthy (port 9443 HTTPS)'
                                        exit 0
                                    fi
                                    echo 'Attempt \$i/30 - waiting...'
                                    sleep 2
                                done
                                echo '✗ Production API health check failed'
                                exit 1
                            "
                        """
                    }
                }
            }
        }
    }

    post {
        always {
            script {
                echo "========== POST: Cleanup =========="
                sh 'docker-compose logs > docker-compose.log || true'
                archiveArtifacts artifacts: 'docker-compose.log', allowEmptyArchive: true
                cleanWs() // Dọn dẹp workspace để không lộ certs
            }
        }
        success {
            script {
                echo "========== BUILD: SUCCESS =========="
                echo "✓ Production pipeline completed successfully"
                echo "Build: ${BUILD_NUMBER}"
                echo "Docker Image: ${DOCKER_HUB_REPO}:latest"
            }
        }
        failure {
            script {
                echo "========== BUILD: FAILURE =========="
                echo "✗ Pipeline failed"
            }
        }
    }
}