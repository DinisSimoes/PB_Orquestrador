# PB_Orquestrador

O PB_Orquestrador é um microserviço responsável por gerenciar falhas e orquestrar retentativas automáticas de eventos dentro do ecossistema da aplicação.
Ele atua como uma camada de confiabilidade entre os microserviços, garantindo que mensagens que falharam em algum ponto do fluxo (por exemplo, falhas no PB_Clientes, PB_AnaliseCredito ou PB_Cartoes) sejam registradas, monitoradas e reenviadas automaticamente.

## Funcionamento

O orquestrador é composto por dois principais componentes:

**1.  Worker**

Escuta mensagens de falha publicadas pelos outros microserviços (ClienteFalhaEvent, PropostaFalhaEvent, CartaoFalhaEvent).

Armazena cada falha na tabela FailureRecords do banco de dados.

Cada falha contém:

- Tipo da mensagem original (MessageType)
- Payload original serializado (PayloadJson)
- Número de tentativas (AttemptCount)
- Status (Pending, Retrying, Resolved, Failed)
- Horário da próxima tentativa (NextRetryAtUtc)

Além disso, um serviço em segundo plano — o FailureRetryService — é responsável por:

- Buscar registros pendentes cuja NextRetryAtUtc já tenha expirado.
- Reprocessar as mensagens, republicando-as na fila original.
- Aplicar backoff exponencial para espaçar as retentativas (2^attempt * 10s).
- Marcar as falhas como resolvidas ou definitivamente falhas (Failed) após o número máximo de tentativas.

**2.  API REST**

A API expõe endpoints para consultar e gerenciar falhas manualmente.

Endpoints:

| Método	| Rota	| Descrição |
|-|-|-|
| GET | /api/failures	| Lista os registros de falhas (até 100 mais recentes) |	
| POST | /api/failures/{id}/retry	| Reenvia manualmente a mensagem falhada com base no ID |	

O endpoint de retry:

- Localiza o registro no banco.
- Desserializa o payload original.
- Publica novamente o evento na fila correspondente via IPublishEndpoint.
- Atualiza as informações da tentativa no banco.

## Como rodar localmente
Garanta que as migrations dos microserviços foram executadas

 - As instruções estão descritas no README do MS PB_Clientes.

Associe o pacote PB_Common

 - Siga os mesmos passos descritos no README do projeto PB_Clientes para adicionar o pacote local.

Execute a solução

 - Após configurar o ambiente e as dependências, basta rodar a sln normalmente.
