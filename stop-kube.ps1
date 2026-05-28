param(
    [switch]$Full,
    [switch]$Destroy
)

$ns = "temperaturaapp"

Write-Host ""
Write-Host "=========================================" -ForegroundColor Yellow
Write-Host "  TemperaturaVS --- Stop Minikube" -ForegroundColor Yellow
Write-Host "=========================================" -ForegroundColor Yellow
Write-Host ""

# Sempre encerra port-forwards
Write-Host "  Encerrando port-forwards..." -ForegroundColor Cyan
Get-Process kubectl -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Get-Job | Where-Object { $_.State -eq "Running" } | Stop-Job
Write-Host "  [OK] Port-forwards encerrados" -ForegroundColor Green

if ($Destroy) {
    Write-Host "  Destruindo cluster Minikube..." -ForegroundColor Red
    minikube delete
    Write-Host "  [OK] Cluster destruido" -ForegroundColor Green

} elseif ($Full) {
    Write-Host "  Removendo deployments .NET do namespace $ns..." -ForegroundColor Yellow

    $deploys = @("api-weather","api-location","api-person","api-orders","frontend")
    foreach ($d in $deploys) {
        kubectl delete deployment $d -n $ns --ignore-not-found=true 2>&1 | Out-Null
        kubectl delete service $d -n $ns --ignore-not-found=true 2>&1 | Out-Null
        Write-Host "    [removed] $d" -ForegroundColor Gray
    }

    Write-Host "  [OK] APIs e frontend removidos (infra mantida)" -ForegroundColor Green
    Write-Host ""
    Write-Host "  Infraestrutura ainda rodando:" -ForegroundColor DarkCyan
    kubectl get pods -n $ns --no-headers 2>&1 | ForEach-Object {
        Write-Host "    $_" -ForegroundColor Gray
    }

} else {
    Write-Host ""
    Write-Host "  Pods continuam rodando no Minikube." -ForegroundColor DarkCyan
    Write-Host "  Use -Full para remover os deployments .NET" -ForegroundColor DarkCyan
    Write-Host "  Use -Destroy para destruir o cluster" -ForegroundColor DarkCyan
}

Write-Host ""
Write-Host "  Para reiniciar: .\start-kube.ps1" -ForegroundColor DarkCyan
Write-Host ""
