param(
    [switch]$Stop,
    [string]$Service = ""
)

$ns = "temperaturaapp"

$forwards = @(
    @{ Name = "frontend";     Local = 6201;  Remote = 80;    Svc = "frontend"       },
    @{ Name = "api-weather";  Local = 7001;  Remote = 8080;  Svc = "api-weather"    },
    @{ Name = "api-location"; Local = 7002;  Remote = 8080;  Svc = "api-location"   },
    @{ Name = "api-person";   Local = 7003;  Remote = 8080;  Svc = "api-person"     },
    @{ Name = "api-orders";   Local = 7004;  Remote = 8080;  Svc = "api-orders"     },
    @{ Name = "rabbitmq-ui";  Local = 25673; Remote = 15672; Svc = "rabbitmq"       },
    @{ Name = "prometheus";   Local = 9991;  Remote = 9090;  Svc = "prometheus"     },
    @{ Name = "grafana";      Local = 3200;  Remote = 3000;  Svc = "grafana"        },
    @{ Name = "otel-http";    Local = 4419;  Remote = 4318;  Svc = "otel-collector" }
)

if ($Stop) {
    Write-Host "Encerrando port-forwards..." -ForegroundColor Yellow
    Get-Process kubectl -ErrorAction SilentlyContinue | Stop-Process -Force
    Write-Host "Pronto." -ForegroundColor Green
    exit 0
}

if ($Service -ne "") {
    $forwards = $forwards | Where-Object { $_.Name -like "*$Service*" }
    if ($forwards.Count -eq 0) {
        Write-Host "Servico '$Service' nao encontrado." -ForegroundColor Red
        exit 1
    }
}

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  TemperaturaVS (.NET) --- Port Forward" -ForegroundColor Cyan
Write-Host "  (portas deslocadas: nao bate com Node.js)" -ForegroundColor DarkCyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

$jobs = @()

foreach ($fwd in $forwards) {
    $cmd = "kubectl port-forward svc/$($fwd.Svc) $($fwd.Local):$($fwd.Remote) -n $ns"
    Write-Host "  [$($fwd.Name)] localhost:$($fwd.Local) -> $($fwd.Svc):$($fwd.Remote)" -ForegroundColor Green
    $job = Start-Job -ScriptBlock { param($c) Invoke-Expression $c } -ArgumentList $cmd
    $jobs += $job
}

Write-Host ""
Write-Host "URLs (nao colidem com Node.js/React em :3000-3004):" -ForegroundColor Cyan
Write-Host "  Frontend Angular   -> http://localhost:6201" -ForegroundColor White
Write-Host "  api-weather (.NET) -> http://localhost:7001/swagger" -ForegroundColor White
Write-Host "  api-location(.NET) -> http://localhost:7002/swagger" -ForegroundColor White
Write-Host "  api-person  (.NET) -> http://localhost:7003/swagger" -ForegroundColor White
Write-Host "  api-orders  (.NET) -> http://localhost:7004/swagger" -ForegroundColor White
Write-Host "  RabbitMQ UI        -> http://localhost:25673  (guest/guest)" -ForegroundColor White
Write-Host "  Prometheus         -> http://localhost:9991" -ForegroundColor White
Write-Host "  Grafana            -> http://localhost:3200  (admin/admin123)" -ForegroundColor White
Write-Host ""
Write-Host "Pressione CTRL+C para encerrar todos os port-forwards." -ForegroundColor Yellow
Write-Host ""

try {
    while ($true) {
        Start-Sleep -Seconds 5
        foreach ($job in $jobs) {
            if ($job.State -eq "Failed") {
                Write-Host "  [WARN] Job $($job.Id) falhou, reiniciando..." -ForegroundColor Yellow
            }
        }
    }
}
finally {
    Write-Host "Encerrando port-forwards..." -ForegroundColor Yellow
    $jobs | Stop-Job
    $jobs | Remove-Job
    Get-Process kubectl -ErrorAction SilentlyContinue | Stop-Process -Force
    Write-Host "Pronto." -ForegroundColor Green
}
