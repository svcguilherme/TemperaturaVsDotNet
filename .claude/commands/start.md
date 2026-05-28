# start — Inicializar ambiente Minikube completo (.NET 8 + Angular 19)

Usa o script `start-kube.ps1` para inicializar tudo automaticamente.

## Forma mais simples

```powershell
.\start-kube.ps1               # Usa imagens em cache + aplica manifestos + port-forwards
.\start-kube.ps1 -Build        # Força rebuild de todas as imagens antes de subir
.\start-kube.ps1 -NoBuild      # Só aplica manifestos e abre port-forwards
```

## O que o script faz automaticamente

1. Verifica se Minikube está rodando (inicia se necessário)
2. Configura Docker context para o Minikube: `& minikube -p minikube docker-env | Invoke-Expression`
3. Build das 5 imagens (só se não existirem ou se -Build): api-weather, api-location, api-person, api-orders, frontend
4. Aplica todos os manifestos k8s (namespace, configmaps, volumes, deployments, services)
5. Aguarda RabbitMQ ficar pronto (dependência crítica dos outros serviços)
6. Aguarda todos os deployments .NET ficarem prontos
7. Abre portforward.ps1 em nova janela

## URLs após start (portas deslocadas do Node.js/React)

- Frontend Angular: http://localhost:4200
- api-weather: http://localhost:5001/swagger
- api-location: http://localhost:5002/swagger
- api-person: http://localhost:5003/swagger
- api-orders: http://localhost:5004/swagger
- RabbitMQ UI: http://localhost:15673 (guest/guest)
- Prometheus: http://localhost:9091

## Para parar

```powershell
.\stop-kube.ps1           # Encerra port-forwards
.\stop-kube.ps1 -Full     # Remove deployments .NET (mantém infra)
.\stop-kube.ps1 -Destroy  # Destrói cluster inteiro
```

## Diagnóstico de falhas

```powershell
kubectl get pods -n temperaturaapp
kubectl describe pod <nome-do-pod> -n temperaturaapp
kubectl logs <nome-do-pod> -n temperaturaapp
```
