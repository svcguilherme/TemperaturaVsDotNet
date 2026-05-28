# portforward — Iniciar port-forwards para todos os serviços

Executa o portforward.ps1 para mapear as portas do cluster Minikube para localhost.
Portas deslocadas para não colidir com projeto Node.js/React existente (que usa 3000-3004).

## Uso

`/portforward` — Inicia todos os forwards
`/portforward stop` — Para todos os port-forwards
`/portforward <serviço>` — Forward de um serviço específico

## Mapeamento de portas (deslocado do Node.js/React)

| Serviço       | Local    | Descrição                         |
|---------------|----------|-----------------------------------|
| frontend      | :4200    | Angular app (React usa :3000)     |
| api-weather   | :5001    | ASP.NET Core + Swagger            |
| api-location  | :5002    | ASP.NET Core + Swagger            |
| api-person    | :5003    | ASP.NET Core + Swagger            |
| api-orders    | :5004    | ASP.NET Core + Swagger            |
| rabbitmq-ui   | :15673   | RabbitMQ Management UI            |
| prometheus    | :9091    | Métricas                          |
| otel-http     | :4319    | OpenTelemetry HTTP endpoint       |

## Comandos

```powershell
# Todos os serviços
.\portforward.ps1

# Parar tudo
.\portforward.ps1 -Stop

# Apenas um serviço
.\portforward.ps1 -Service orders
```

## Passos que o Claude deve executar

1. Verificar se Minikube está rodando: `minikube status`
2. Se o usuário pediu /portforward stop: `.\portforward.ps1 -Stop`
3. Caso contrário: `.\portforward.ps1` (ou com -Service <nome> se específico)
4. Exibir as URLs disponíveis para o usuário
