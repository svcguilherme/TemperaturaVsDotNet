param(
    [switch]$Build,
    [switch]$NoBuild
)

$base = "f:\devestudo\Examples_Git_GSVC_Portfolio\TemperaturaVsDotNet"
$ns   = "temperaturaapp"

function Write-Step([string]$msg) {
    Write-Host ""
    Write-Host "  >> $msg" -ForegroundColor Cyan
}

function Write-OK([string]$msg) {
    Write-Host "  [OK] $msg" -ForegroundColor Green
}

function Write-Fail([string]$msg) {
    Write-Host "  [ERRO] $msg" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "  TemperaturaVS --- Start Minikube (.NET)" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

# 1. Verificar Minikube
Write-Step "Verificando Minikube..."
$mkStatus = minikube status 2>&1
if ($mkStatus -notmatch "Running") {
    Write-Step "Iniciando Minikube..."
    minikube start --memory=6144 --cpus=4 --driver=docker
    if ($LASTEXITCODE -ne 0) { Write-Fail "Falha ao iniciar Minikube" }
    minikube addons enable ingress 2>&1 | Out-Null
    minikube addons enable metrics-server 2>&1 | Out-Null
}
Write-OK "Minikube rodando"

# 2. Configurar contexto Docker do Minikube
Write-Step "Configurando Docker context do Minikube..."
& minikube -p minikube docker-env --shell powershell | Invoke-Expression
Write-OK "Docker context = Minikube"

# 3. Build das imagens (se solicitado ou se nao existirem)
if (-not $NoBuild) {
    $services = @("api-weather","api-location","api-person","api-orders","frontend")
    foreach ($svc in $services) {
        $imgExists = docker images --format "{{.Repository}}:{{.Tag}}" | Select-String "^${svc}:local$"
        if ($Build -or -not $imgExists) {
            Write-Step "Build ${svc}:local ..."
            docker build -t "${svc}:local" "$base\$svc" 2>&1 | Select-String "(DONE|ERROR)" | Select-Object -Last 2
            if ($LASTEXITCODE -ne 0) { Write-Fail "Falha no build de $svc" }
            Write-OK "${svc}:local"
        } else {
            Write-OK "${svc}:local (cache OK)"
        }
    }
}

# 4. Aplicar manifestos Kubernetes
Write-Step "Aplicando manifestos Kubernetes..."
kubectl apply -f "$base\k8s\namespace.yaml" 2>&1 | Out-Null
kubectl apply -f "$base\k8s\configmaps\otel-config.yaml" -n $ns 2>&1 | Out-Null
kubectl apply -f "$base\k8s\volumes\sqlite-pvc.yaml" -n $ns 2>&1 | Out-Null
kubectl apply -f "$base\k8s\deployments\" -n $ns 2>&1 | Out-Null
kubectl apply -f "$base\k8s\services\services.yaml" -n $ns 2>&1 | Out-Null
Write-OK "Manifestos aplicados"

# 5. Aguardar RabbitMQ (dependencia critica)
Write-Step "Aguardando RabbitMQ ficar pronto..."
kubectl wait --for=condition=ready pod -l app=rabbitmq -n $ns --timeout=120s 2>&1 | Out-Null
Write-OK "RabbitMQ pronto"

# 6. Aguardar todos os pods
Write-Step "Aguardando todos os pods..."
kubectl rollout status deployment/api-weather  -n $ns --timeout=120s 2>&1 | Out-Null
kubectl rollout status deployment/api-location -n $ns --timeout=120s 2>&1 | Out-Null
kubectl rollout status deployment/api-person   -n $ns --timeout=120s 2>&1 | Out-Null
kubectl rollout status deployment/api-orders   -n $ns --timeout=120s 2>&1 | Out-Null
kubectl rollout status deployment/frontend     -n $ns --timeout=120s 2>&1 | Out-Null
Write-OK "Todos os pods prontos"

# 7. Status final
Write-Step "Status dos pods:"
kubectl get pods -n $ns --no-headers 2>&1 | ForEach-Object {
    if ($_ -match "Running") {
        Write-Host "    [UP]   $_" -ForegroundColor Green
    } else {
        Write-Host "    [WAIT] $_" -ForegroundColor Yellow
    }
}

# 8. Abrir port-forwards em nova janela
Write-Step "Abrindo port-forwards..."
$pfScript = "$base\portforward.ps1"
Start-Process powershell -ArgumentList "-NoExit -File `"$pfScript`"" -WindowStyle Normal

Write-Host ""
Write-Host "=========================================" -ForegroundColor Green
Write-Host "  AMBIENTE PRONTO!" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Green
Write-Host ""
Write-Host "  Frontend Angular  -> http://localhost:4200" -ForegroundColor White
Write-Host "  api-weather Swag  -> http://localhost:5001/swagger" -ForegroundColor White
Write-Host "  api-location Swag -> http://localhost:5002/swagger" -ForegroundColor White
Write-Host "  api-person Swag   -> http://localhost:5003/swagger" -ForegroundColor White
Write-Host "  api-orders Swag   -> http://localhost:5004/swagger" -ForegroundColor White
Write-Host "  RabbitMQ UI       -> http://localhost:15673 (guest/guest)" -ForegroundColor White
Write-Host "  Prometheus        -> http://localhost:9091" -ForegroundColor White
Write-Host ""
Write-Host "  Para parar: .\stop-kube.ps1" -ForegroundColor DarkCyan
Write-Host ""
