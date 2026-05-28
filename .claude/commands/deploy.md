# deploy — Rebuild e redeploy de um serviço específico

Reconstrói a imagem Docker de um serviço .NET ou Angular e atualiza o deployment no Minikube.

## Uso

`/deploy <serviço>`

Onde `<serviço>` pode ser: `api-weather`, `api-location`, `api-person`, `api-orders`, `frontend`, `all`

## Passos

1. Validar que o argumento é um serviço válido
2. Garantir contexto Docker no Minikube: `& minikube -p minikube docker-env --shell powershell | Invoke-Expression`
3. Build da imagem:
   - Para APIs .NET: `docker build -t <serviço>:local ./<serviço>`
   - Para frontend Angular: `docker build -t frontend:local ./frontend`
4. Forçar rollout: `kubectl rollout restart deployment/<serviço> -n temperaturaapp`
5. Aguardar: `kubectl rollout status deployment/<serviço> -n temperaturaapp`
6. Se `all`: repetir para api-weather, api-location, api-person, api-orders, frontend

Serviços disponíveis: api-weather, api-location, api-person, api-orders, frontend.
Se não existir, listar com `kubectl get deployments -n temperaturaapp`.
