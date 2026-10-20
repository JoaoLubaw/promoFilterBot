# PromoFilterBot

Bot do Telegram em .NET 10 (Worker Service) onde cada usuário cadastra o que quer comprar e o preço máximo. A **conta leitora** (a conta pessoal do admin, via WTelegramClient/MTProto) só lê os canais públicos que o admin cadastrou, um **matcher** encontra os alertas que batem e o **bot oficial** (Telegram.Bot, long polling) manda no privado, na hora, uma cópia do post (formatação, cupom e links) só para o dono de cada alerta. O usuário só tem o cadastro do chat e os alertas; canal é coisa do admin. Um único processo hospeda tudo; o leitor só publica numa fila em memória, então dá para separar o leitor em outro container ou somar uma segunda conta leitora depois.

O padrão de código segue o do **rast-core** (`E:\Codes\VBL\Projects\Rast\rast\rast-core`), com uma diferença deliberada: **o banco é gerado pelas migrations do EF Core**, não por SQL manual.

## Arquitetura

```
src/
  PromoFilterBot.Domain          → Models/ (entidades + enums), Common/ (lógica pura), Dto/ — SEM dependências
  PromoFilterBot.Infrastructure  → Models/ (DbContext), Migrations/, Interfaces/, Services/, Repositories/, Dto/, Options/, Common/
  PromoFilterBot.Reader          → conta leitora: sessão, updates, entrada nos canais (BackgroundServices/, Services/, Interfaces/)
  PromoFilterBot.Bot             → bot: Handlers/ (o "Controllers" do rast), Services/ (messenger, rate limiter), BackgroundServices/
  PromoFilterBot.Host            → Program.cs (composition root), appsettings.json, workers do matcher/monitoramento/limpeza
tests/
  PromoFilterBot.Domain.Tests    → lógica pura (xUnit)
  PromoFilterBot.Tests           → UnitTests/Services, UnitTests/Handlers, UnitTests/Common (xUnit + Moq)
```

Fluxo padrão (o mesmo do rast-core): `Handler : BotHandlerBase` → `IXxxService` → `IXxxRepository : BaseRepository` → `PromoFilterBotDbContext`. A Infrastructure concentra Interfaces/Services/Repositories, como o rast-core; o Domain só tem o que não depende de nada (normalização, sintaxe, preço, chave do produto, índice invertido, matcher) e é onde ficam as regras centrais do matching.

Fluxo de uma oferta: `ReaderUpdateHandler` → `IOfferQueue` → `MatcherWorker` → `IMatchingService` (salva a mensagem e as notificações numa transação do `IUnitOfWork`; enfileira os ids em `INotificationQueue` só depois do commit) → `NotifierWorker` → `INotificationService.PrepareDispatchAsync` (envia ou pula, se o usuário ou o alerta pausou) → `IBotMessenger`. Sem limite por hora nem resumo: cada oferta sai na hora.

Regras que não podem regredir:

- **Oferta = produto × loja × preço × cupom, por dia.** O índice único de `notification` (`NULLS NOT DISTINCT`) garante uma vez por dia; outro preço, cupom ou loja é oferta nova e sai de novo. `MatchingService` consulta as mesmas colunas antes de salvar.
- **Cópia, não encaminhamento:** a Bot API só encaminha de chats em que o bot está, e o bot não entra em canal de terceiros. O leitor guarda o post como HTML (`TelegramHtml.FromEntities`) e o bot copia; se o Telegram recusar o HTML, vai o texto puro.
- **A conta leitora é pessoal:** nunca listar os diálogos (`Messages_GetAllDialogs`), nunca ler, logar ou guardar update que não seja de canal monitorado, nunca enviar mensagem nem marcar como lida. Só entra, silencia e arquiva os canais cadastrados.

## Convenções (as do rast-core)

- **Erro previsto não lança exception.** Todo caminho esperado (usuário não cadastrado, dado inválido, limite, entidade inexistente) retorna `ServiceResult<T>(ResultCode.XXX, null)`. `throw` só para o imprevisto — o `ExecuteAsync` do `BotHandlerBase` captura e responde um erro tratado. Faixas do `ResultCode` (`Infrastructure/Common/ServiceResult.cs`): `10xx` permissão, `20xx` validação, `30xx` entidade, `80xx` sistema. Código novo = valor no enum **e** mensagem em pt-BR em `Bot/Common/ResultMessages.cs`.
- **Tudo atrás de interface** em `Interfaces/`; ao mudar um método, mude a interface primeiro. Handler nunca fala com repository; regra e permissão (dono do alerta, admin) vivem no **service**; o repository só lê/escreve.
- **DI 100% manual em `Program.cs`**, nos blocos `/* Repositories */`, `/* Services */`, `/* Shared state */`, `/* Reader */`, `/* Bot */`, `/* Handlers */`, `/* Hosted services */`. Classe nova sem `Add...` quebra no boot (em Development o host valida o grafo de DI ao subir). Singleton só sem `DbContext`; o resto é scoped (um escopo por update do bot / por oferta do matcher).
- **Entidade herda `BaseEntity`** (colunas `row_*`), com `[Table("snake_case")]` e `[Column("<tabela>_<campo>")]` — o nome da coluna começa pelo da tabela (`alert_max_price`), exceto as `row_*` e as FKs (`user_id`). `[DataContract]`/`[DataMember]` como no rast. Relações, chaves compostas, índices e `OnDelete` ficam no `OnModelCreating`.
- **Banner `/* File: ... */`** no topo de todo `.cs` (copie de um vizinho e ajuste `File`/`Project`/datas; `Project` = nome da pasta, ou do projeto para arquivos na raiz). As migrations geradas pelo EF não têm banner.
- **Estilo:** CRLF, 4 espaços, chaves sempre e na mesma linha (`.editorconfig`). Tipos explícitos no código (`User? user = ...`); `var` à vontade nos testes. Comentários e logs em inglês; textos do bot em pt-BR (`Bot/Common/BotTexts.cs`, sempre com `Html.Escape` no que vem de usuário/canal).
- **Logs via Serilog** (`ILogger<T>`), nunca `Console.WriteLine` (exceção: o login interativo do leitor). Service loga o motivo de cada retorno de erro (`LogWarning`).
- **Callback data ≤ 64 bytes:** só códigos curtos (`a:del:42`) via `Callbacks.Build`; o resto vem do banco.

## Banco de dados e migrations (diferente do rast)

- PostgreSQL via EF Core + Npgsql. **O schema é o das migrations do EF** em `src/PromoFilterBot.Infrastructure/Migrations`, aplicadas no startup (`Database:ApplyMigrationsOnStartup`).
- Ao mudar uma entidade ou o `OnModelCreating`, gere a migration (não escreva SQL à mão):

```sh
dotnet tool restore
dotnet ef migrations add <NomeEmPascalCase> -p src/PromoFilterBot.Infrastructure -s src/PromoFilterBot.Infrastructure
dotnet ef migrations has-pending-model-changes -p src/PromoFilterBot.Infrastructure -s src/PromoFilterBot.Infrastructure
```

- O `PromoFilterBotDbContextFactory` permite gerar migrations sem banco. `/apagarconta` apaga a linha do `user` e o cascade do banco leva o resto — mantenha `OnDelete(Cascade)` em tudo que pertence ao usuário.

## Como rodar

```sh
cp .env.example .env                    # BOT_TOKEN, ADMIN_CHAT_ID, TELEGRAM_*, POSTGRES_PASSWORD
docker compose run --rm app --login     # primeiro login da conta leitora (interativo)
docker compose up -d
```

Deploy: push em `main` ou `develop` roda `.github/workflows/deploy.yml` (testa → publica a imagem no GHCR → copia o compose e os scripts e sobe por SSH em `/opt/promofilterbot` na VM, onde fica o `.env`). Mesmo padrão do .ei, na mesma máquina.

Para desenvolver só o bot: `Reader:Enabled=false`. Com `dotnet run`, use um Postgres local e um `appsettings.Development.json` (git-ignored) — ver README.

## Testes

```sh
dotnet test
```

- Regra pura → `PromoFilterBot.Domain.Tests`. Service → `UnitTests/Services/XxxServiceTest.cs` mockando **cada** dependência do construtor (Moq), um teste por early-return + o sucesso, asserindo em `result.Code`. Handler → `UnitTests/Handlers` herdando `HandlerTestBase` (captura o que foi enviado/editado).
- Repositories, DbContext, workers e o cliente do Telegram são `[ExcludeFromCodeCoverage]`.

## Gates antes de concluir

```sh
dotnet build PromoFilterBot.sln         # sem erros nem warnings
dotnet format PromoFilterBot.sln        # e commitar o resultado
dotnet test PromoFilterBot.sln          # relatar total e falhas
```

## Commits

- Incrementais, cada um buildável e com os testes passando. Nunca commitar estado que não compila.
- Nunca commitar `.env`, `appsettings.*.json`, `session/` (o arquivo de sessão vale como login da conta leitora) nem backups.
