# teardown — Destruir ambiente do cluster

Usa o script `stop-kube.ps1` para parar/destruir o ambiente.

## Forma mais simples

```powershell
.\stop-kube.ps1           # Encerra port-forwards (pods continuam rodando)
.\stop-kube.ps1 -Full     # Remove deployments .NET (mantém infra: RabbitMQ, Redis, MongoDB)
.\stop-kube.ps1 -Destroy  # Destrói cluster Minikube completamente
```

## Níveis de destruição

`/teardown` — Encerra port-forwards: `.\stop-kube.ps1`
`/teardown --full` — Remove APIs/frontend do namespace: `.\stop-kube.ps1 -Full`
`/teardown --destroy` — Destrói cluster: `.\stop-kube.ps1 -Destroy`

## Alternativa manual

```powershell
# Remover só o namespace
kubectl delete namespace temperaturaapp

# Ou parar tudo sem deletar
minikube stop
```

## Aviso

Dados no SQLite (PVC) serão perdidos ao deletar o namespace/PVC. MongoDB usa Atlas (nuvem) — dados persistem.
Para recriar: `.\start-kube.ps1`
