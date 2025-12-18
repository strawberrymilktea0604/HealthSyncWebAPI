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
        
        // Docker Hub - Sẽ được bind trong withCredentials
        // DOCKER_HUB_REPO sẽ được bind từ credentials
        
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
                        
                        // Copy certs vào thư mục nginx để build image
                        sh 'mkdir -p nginx && cp -r certs nginx/'
                        
                        // (Tùy chọn) Xóa file nén đi cho sạch
                        sh 'rm certs.tar.gz'
                        
                        // Kiểm tra xem thư mục đã có chưa (để debug)
                        sh 'ls -la certs/ && ls -la nginx/certs/'
                    }
                }
            }
        }

        stage('Build Nginx Image') {
            steps {
                script {
                    echo "========== STAGE: Build Nginx Image =========="
                    echo "Building Nginx Docker image with baked config and certs"
                    sh """
                        docker build \\
                            -t healthsync-nginx:${DOCKER_IMAGE_TAG} \\
                            -t healthsync-nginx:latest \\
                            -f Dockerfile.nginx \\
                            .
                    """
                    echo "✓ Nginx Docker image built successfully"
                    sh "docker images | grep healthsync-nginx"
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

        stage('Build & Push Nginx Image') {
            steps {
                script {
                    echo "========== STAGE: Build & Push Nginx Image =========="
                    withCredentials([
                        usernamePassword(credentialsId: 'docker-hub-credentials', usernameVariable: 'DOCKER_HUB_USER', passwordVariable: 'DOCKER_HUB_PASS'),
                        string(credentialsId: 'docker-hub-repo', variable: 'DOCKER_HUB_REPO_VAR')
                    ]) {
                        sh """
                            # Build nginx image
                            docker build -f Dockerfile.nginx -t \${DOCKER_HUB_REPO_VAR}-nginx:${DOCKER_IMAGE_TAG} .
                            docker tag \${DOCKER_HUB_REPO_VAR}-nginx:${DOCKER_IMAGE_TAG} \${DOCKER_HUB_REPO_VAR}-nginx:latest
                            
                            # Login & push
                            echo "\$DOCKER_HUB_PASS" | docker login -u "\$DOCKER_HUB_USER" --password-stdin
                            docker push \${DOCKER_HUB_REPO_VAR}-nginx:${DOCKER_IMAGE_TAG}
                            docker push \${DOCKER_HUB_REPO_VAR}-nginx:latest
                            docker logout
                            
                            echo "\u2713 Nginx image built and pushed successfully"
                        """
                    }
                }
            }
        }

        stage('Push Docker Image') {
            steps {
                script {
                    echo "========== STAGE: Push Docker Image =========="
                    withCredentials([
                        usernamePassword(credentialsId: 'docker-hub-credentials', usernameVariable: 'DOCKER_HUB_USER', passwordVariable: 'DOCKER_HUB_PASS'),
                        string(credentialsId: 'docker-hub-repo', variable: 'DOCKER_HUB_REPO_VAR')
                    ]) {
                        echo "Pushing image to Docker Hub: ${DOCKER_HUB_REPO_VAR}"
                        sh """
                            # Login to Docker Hub
                            echo "\$DOCKER_HUB_PASS" | docker login -u "\$DOCKER_HUB_USER" --password-stdin
                            
                            # Tag and push API image
                            docker tag ${DOCKER_IMAGE_NAME}:${DOCKER_IMAGE_TAG} \${DOCKER_HUB_REPO_VAR}:${DOCKER_IMAGE_TAG}
                            docker tag ${DOCKER_IMAGE_NAME}:latest \${DOCKER_HUB_REPO_VAR}:latest
                            docker push \${DOCKER_HUB_REPO_VAR}:${DOCKER_IMAGE_TAG}
                            docker push \${DOCKER_HUB_REPO_VAR}:latest
                            
                            # Tag and push Nginx image
                            docker tag healthsync-nginx:${DOCKER_IMAGE_TAG} \${DOCKER_HUB_REPO_VAR}-nginx:${DOCKER_IMAGE_TAG}
                            docker tag healthsync-nginx:latest \${DOCKER_HUB_REPO_VAR}-nginx:latest
                            docker push \${DOCKER_HUB_REPO_VAR}-nginx:${DOCKER_IMAGE_TAG}
                            docker push \${DOCKER_HUB_REPO_VAR}-nginx:latest
                            
                            echo "\u2713 Images pushed successfully"
                            
                            # Logout from Docker Hub
                            docker logout
                        """
                    }
                }
            }
        }

        stage('Deploy to Production') {
            steps {
                script {
                    echo "========== STAGE: Deploy to Production =========="
                    withCredentials([
                        file(credentialsId: 'prod-env-file-healthsync', variable: 'ENV_FILE_PATH'),
                        sshUserPrivateKey(credentialsId: SSH_CREDENTIALS_ID, keyFileVariable: 'SSH_KEY', usernameVariable: 'SSH_USER'),
                        string(credentialsId: 'docker-hub-repo', variable: 'DOCKER_HUB_REPO_VAR')
                    ]) {
                        sh """
                            # 1. Tạo thư mục deploy (nếu chưa có)
                            ssh -p 2222 -o StrictHostKeyChecking=no -i \$SSH_KEY \$SSH_USER@${PROD_SERVER_IP} \
                                "mkdir -p ${PROD_DEPLOY_DIR}"
                            
                            # 2. QUAN TRỌNG: Xóa file .env.prod cũ trước khi copy đè
                            # Giúp tránh lỗi 'Text file busy' hoặc 'Permission denied' do Docker đang lock file
                            ssh -p 2222 -o StrictHostKeyChecking=no -i \$SSH_KEY \$SSH_USER@${PROD_SERVER_IP} \
                                "rm -f ${PROD_DEPLOY_DIR}/.env.prod"

                            # 3. Tạo file .env.prod tạm thời từ credential và thêm DOCKER_HUB_REPO
                            cp \$ENV_FILE_PATH .env.prod.tmp
                            echo "" >> .env.prod.tmp
                            echo "# Docker Hub Repository (injected by Jenkins)" >> .env.prod.tmp
                            echo "DOCKER_HUB_REPO=\${DOCKER_HUB_REPO_VAR}" >> .env.prod.tmp
                            
                            # 4. Copy các file cấu hình
                            scp -P 2222 -o StrictHostKeyChecking=no -i \$SSH_KEY \
                                docker-compose.prod.yml \$SSH_USER@${PROD_SERVER_IP}:${PROD_DEPLOY_DIR}/
                            
                            scp -P 2222 -o StrictHostKeyChecking=no -i \$SSH_KEY \
                                Dockerfile.loophole \$SSH_USER@${PROD_SERVER_IP}:${PROD_DEPLOY_DIR}/
                            
                            # Copy file .env.prod đã được bổ sung DOCKER_HUB_REPO
                            scp -P 2222 -o StrictHostKeyChecking=no -i \$SSH_KEY \
                                .env.prod.tmp \$SSH_USER@${PROD_SERVER_IP}:${PROD_DEPLOY_DIR}/.env.prod
                            
                            # Xóa file tạm
                            rm -f .env.prod.tmp
                            
                            # 4. Thực hiện Deploy
                            ssh -p 2222 -o StrictHostKeyChecking=no -i \$SSH_KEY \$SSH_USER@${PROD_SERVER_IP} "
                                cd ${PROD_DEPLOY_DIR}
                                
                                # Update API image tag with actual value (not shell variable)
                                sed -i 's|image: healthsync-api:latest|image: ${DOCKER_HUB_REPO_VAR}:latest|g' docker-compose.prod.yml
                                
                                # Update nginx image tag with actual value (not shell variable)
                                sed -i 's|image: \\\${DOCKER_HUB_REPO:-healthsync}-nginx:latest|image: ${DOCKER_HUB_REPO_VAR}-nginx:latest|g' docker-compose.prod.yml
                                
                                # Verify replacements
                                echo '=== Checking docker-compose.prod.yml images ==='
                                grep 'image:' docker-compose.prod.yml | grep -E '(api|nginx)'
                                
                                # Pull new images
                                docker compose -f docker-compose.prod.yml --env-file .env.prod pull
                                
                                # Graceful shutdown (check if stack exists first)
                                echo '=== Shutting down existing stack ==='
                                if docker compose -f docker-compose.prod.yml --env-file .env.prod ps -q 2>/dev/null | grep -q .; then
                                    echo 'Existing stack found, shutting down...'
                                    docker compose -f docker-compose.prod.yml --env-file .env.prod down --remove-orphans
                                else
                                    echo 'No existing stack found, skipping shutdown'
                                fi
                                
                                # Start new stack
                                echo '=== Starting new stack ==='
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
                        // Tách riêng thành các bước nhỏ, dễ debug
                        def maxRetries = 60
                        def success = false
                        
                        for (int i = 1; i <= maxRetries; i++) {
                            def checkResult = sh(
                                script: """
                                    ssh -p 2222 -o StrictHostKeyChecking=no -i "\$SSH_KEY" "\$SSH_USER@${PROD_SERVER_IP}" '
                                        STATUS=\$(docker inspect --format="{{.State.Health.Status}}" healthsync-nginx-prod 2>/dev/null || echo "not_found")
                                        if [ "\$STATUS" = "healthy" ]; then
                                            HTTP_CODE=\$(curl -k -s -o /dev/null -w "%{http_code}" https://localhost:9443/health)
                                            if [ "\$HTTP_CODE" = "200" ]; then
                                                echo "HEALTHY"
                                                exit 0
                                            fi
                                        fi
                                        echo "STATUS:\$STATUS"
                                        exit 1
                                    '
                                """,
                                returnStatus: true
                            )
                            
                            if (checkResult == 0) {
                                echo "Health check passed on attempt ${i}"
                                success = true
                                break
                            }
                            
                            if (i % 5 == 0) {
                                echo "Attempt ${i}/${maxRetries} - still waiting..."
                            }
                            
                            sleep(5)
                        }
                        
                        if (!success) {
                            echo "Health check failed after ${maxRetries} attempts"
                            
                            // Dump logs để debug
                            sh """
                                ssh -p 2222 -o StrictHostKeyChecking=no -i "\$SSH_KEY" "\$SSH_USER@${PROD_SERVER_IP}" '
                                    echo "=== Container Status ==="
                                    docker ps -a | grep healthsync || true
                                    echo "=== Nginx Logs (last 20) ==="
                                    docker logs --tail 20 healthsync-nginx-prod 2>&1 || true
                                    echo "=== API Logs (last 20) ==="
                                    docker logs --tail 20 healthsync-api-prod 2>&1 || true
                                '
                            """
                            
                            error("Health check timeout!")
                        }
                    }
                }
            }
        }
    }

    post {
        always {
            script {
                echo "========== POST: Cleanup =========="
                cleanWs() // Dọn dẹp workspace
            }
        }
        success {
            script {
                echo "========== BUILD: SUCCESS =========="
                echo "✓ Production pipeline completed successfully"
                echo "Build: ${BUILD_NUMBER}"
                echo ""
                
                // Define hostname variables for easy customization
                def API_HOSTNAME = 'healthsync-api.loophole.site'
                def FILES_HOSTNAME = 'healthsync-files.loophole.site'
                def CONSOLE_HOSTNAME = 'healthsync-console.loophole.site'
                
                echo "========== PRODUCTION URLs =========="
                echo "🌐 API Endpoint (HTTPS): https://${API_HOSTNAME}"
                echo "🗄️  MinIO Storage:       https://${FILES_HOSTNAME}"
                echo "🎛️  MinIO Console:       https://${CONSOLE_HOSTNAME}"
                echo ""
                echo "📊 Health Check:        https://${API_HOSTNAME}/health"
                echo "📖 Swagger UI:          https://${API_HOSTNAME}/swagger"
                echo "======================================"
            }
        }
        failure {
            script {
                echo "========== BUILD: FAILURE =========="
                echo "✗ Pipeline failed! Đang kết nối server để lấy 50 dòng log cuối..."
                
                // PHẢI nạp lại Credentials vì ta đang ở ngoài stage Deploy
                withCredentials([
                    sshUserPrivateKey(credentialsId: SSH_CREDENTIALS_ID, keyFileVariable: 'SSH_KEY', usernameVariable: 'SSH_USER')
                ]) {
                    // Dùng SSH để chạy lệnh logs trên server PROD (chứ không phải trên Jenkins)
                    // Thêm --tail=50 để chỉ lấy 50 dòng cuối, tránh spam log
                    sh """
                        ssh -p 2222 -o StrictHostKeyChecking=no -i \$SSH_KEY \$SSH_USER@${PROD_SERVER_IP} \
                        "cd ${PROD_DEPLOY_DIR} && docker compose -f docker-compose.prod.yml logs --tail=50"
                    """
                }
            }
        }
    }
}