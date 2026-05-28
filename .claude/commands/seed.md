# seed — Gerenciar seed de pedidos no api-orders

Gerencia os 10.000 pedidos de seed no SQLite do api-orders.

## Uso

`/seed status`   — Exibe estatísticas dos pedidos
`/seed reset`    — Reseta todos para Pending
`/seed process`  — Enfileira todos pendentes no Service Bus (RabbitMQ)

## Como funciona

- O seed de 10.000 pedidos é criado automaticamente na primeira inicialização do api-orders
- Cada pedido tem: OrderNumber (100001-110000), TotalValue (R$10 a R$9.999), Status (Pending)
- Ao processar: os pedidos são enfileirados no Service Bus (RabbitMQ direct exchange)
- O BackgroundService OrderProcessorWorker consome a fila e processa (90% Completed, 10% Failed)
- Cada mudança de status é publicada no Event Hub (RabbitMQ fanout) para audit/analytics/notification

## Endpoints REST

```
GET  http://localhost:3004/api/orders/stats     — Estatísticas
GET  http://localhost:3004/api/orders?page=1    — Listar pedidos
POST http://localhost:3004/api/orders/process   — Iniciar processamento
POST http://localhost:3004/api/orders/reset     — Resetar todos
```

## Passos que o Claude deve executar

1. Verificar se api-orders está respondendo: `curl http://localhost:3004/health`
2. Para status: `curl http://localhost:3004/api/orders/stats`
3. Para reset: `curl -X POST http://localhost:3004/api/orders/reset`
4. Para processar: `curl -X POST http://localhost:3004/api/orders/process`
5. Exibir resultados formatados para o usuário
