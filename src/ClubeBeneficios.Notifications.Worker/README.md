# ClubeBeneficios.Notifications.Worker

Worker genÃ©rico responsÃ¡vel por processar notificaÃ§Ãµes pendentes da tabela `notification_outbox`.

## Responsabilidade

Este worker nÃ£o conhece regras de negÃ³cio dos mÃ³dulos.

As APIs/procedures dos mÃ³dulos alimentam a `notification_outbox`.
O worker apenas:

1. Libera locks expirados.
2. Busca notificaÃ§Ãµes pendentes.
3. Aplica lock.
4. Renderiza templates.
5. Envia e-mails via SMTP.
6. Marca como enviada, falha ou morta.

## Modos

### Watch

Processa continuamente conforme `PollingIntervalSeconds`.

### Manual

Executa um ciclo de processamento e encerra.

## ConfiguraÃ§Ã£o principal

```json
"NotificationWorker": {
  "Mode": "Watch",
  "PollingIntervalSeconds": 30,
  "BatchSize": 20,
  "LockMinutes": 5,
  "ReleaseExpiredLocksOnStart": true,
  "MaxItemsPerCycle": 100
}
```

## SMTP

```json
"Smtp": {
  "Host": "smtp.seudominio.com",
  "Port": 587,
  "EnableSsl": true,
  "UserName": "notificacoes@seudominio.com",
  "Password": "SENHA_AQUI",
  "FromEmail": "notificacoes@seudominio.com",
  "FromName": "Clube Matilha"
}
```

## ExecuÃ§Ã£o local

```powershell
dotnet run --project .\src\ClubeBeneficios.Notifications.Worker\ClubeBeneficios.Notifications.Worker.csproj
```

## ExecuÃ§Ã£o manual

Altere temporariamente o `NotificationWorker:Mode` para `Manual` no `appsettings.Development.json` e execute o worker.

## Banco de dados

O worker consome as procedures:

- `dbo.usp_notification_release_expired_locks`
- `dbo.usp_notification_claim_batch`
- `dbo.usp_notification_mark_sent`
- `dbo.usp_notification_mark_failed`

Os mÃ³dulos de negÃ³cio sÃ£o responsÃ¡veis por inserir registros em `notification_outbox`.
