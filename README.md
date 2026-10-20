# PromoFilterBot

A Telegram bot where anyone registers what they want to buy and the highest price they would pay. The system reads
the promotion channels the admin registered and, as soon as an offer is posted, sends it in private to the people
whose alerts match it.

- **Front door:** the official bot (created in Telegram itself, with @BotFather). Commands for those who like typing,
  buttons for everyone else.
- **Eyes of the system:** the admin's own Telegram account, used only to read the public channels the admin
  registered. It never lists or reads the chats of the account and never sends anything.
- **Multi-user from the start:** a user is just their chat and their active alerts; the notification goes only to the
  owner of the alert.
- **The channels don't lose:** the bot copies the whole post (formatting, coupon, links with the channel's affiliate
  code) under the name of the channel, which opens the original post.

## Table of Contents

- [How it works](#how-it-works)
- [Project structure](#project-structure)
- [Using the bot](#using-the-bot)
- [Getting started](#getting-started)
- [Deploy on a VM](#deploy-on-a-vm)
- [Database and migrations](#database-and-migrations)
- [Configuration](#configuration)
- [Tests](#tests)
- [Design notes](#design-notes)

## How it works

Two Telegram accounts with separate roles: the reader account (yours) only reads the channels, and the official bot is
the only one that talks to the users. Between them, the matcher decides who must be told.

```text
+----------------------+  reads +----------------------+ publishes +-------------------------+
| Promotion channels   |------->| Reader account       |---------->| Matcher                 |
| public, registered   |        | WTelegramClient      |           | normalizes, terms, price|
| by the admin         |        | only reads           |           | product, store, coupon  |
+----------------------+        +----------------------+           +-------------------------+
                                                                      ^                 |
                                +----------------------+  queries     |                 | queues
                                | PostgreSQL           |<------------>+                 |
                                | users, alerts,       |   and saves                    |
                                | channels, messages,  |                                |
                                | notifications        |                                v
+----------------------+  chat  +----------------------+   sends   +-------------------------+
| Users                |<------>| Official bot         |<----------| Notifier                |
| talk only to the bot |        | Telegram.Bot         |           | at once, copy of the    |
|                      |        | long polling         |           | post, ~30/s, 1/s a chat |
+----------------------+        +----------------------+           +-------------------------+
```

Life of an offer:

1. A registered channel posts a promotion.
2. The reader account receives the update and publishes it to an in-memory queue (`IOfferQueue`), with the text and
   its formatting as HTML.
3. The matcher normalizes the text and reads the price, the product, the store and the coupon. It saves the post and
   finds the alerts it matches through an in-memory inverted index.
4. For each owner (once per user, even with several matching alerts), a notification is saved and queued — unless the
   user already got the same offer today. An offer is product × store × price × coupon: the same product at another
   price, with another coupon or in another store is a new offer, sent at once.
5. The notifier sends it right away: a copy of the whole post under the name of the channel, with buttons to open the
   offer and the post, turn the alert off or say it was not what the user wanted. It keeps sending until the user
   turns the alert off (or pauses).

A single process (a .NET 10 Worker Service) hosts everything; the reader only publishes to the queue, so a second
reader account (or a separate container) can be added later without touching the rest.

## Project structure

```text
PromoFilterBot.sln
├── src/
│   ├── PromoFilterBot.Domain          // entities + pure logic: normalizer, alert syntax, price, coupon, store, product key, index, matcher, HTML of the copy
│   ├── PromoFilterBot.Infrastructure  // EF Core + Npgsql: DbContext, migrations, Interfaces → Services → Repositories
│   ├── PromoFilterBot.Reader          // WTelegramClient: login, updates, joining (muted and archived) the channels, FloodWait
│   ├── PromoFilterBot.Bot             // Telegram.Bot: handlers (commands, buttons, guided flow), notifier
│   └── PromoFilterBot.Host            // Program.cs (composition root), appsettings, matcher/monitoring/cleanup workers
├── tests/
│   ├── PromoFilterBot.Domain.Tests    // xUnit: normalization, syntax, price, coupon, store, product key, index, matching, HTML
│   └── PromoFilterBot.Tests           // xUnit + Moq: services, handlers, router, formatter, rate limiter
├── scripts/                           // backup.sh / restore.sh
├── docker-compose.yml
└── Dockerfile
```

| Library | Use |
| --- | --- |
| WTelegramClient | reader account (MTProto) |
| Telegram.Bot | official bot, long polling |
| EF Core + Npgsql | database and migrations |
| Serilog | structured logs on the console and in rolling files |
| xUnit + Moq | tests |

Long polling instead of a webhook: the bot asks Telegram for updates, so the VM needs no open port, domain or HTTPS
certificate.

## Using the bot

Every command works typed in one line or, without arguments, in guided mode with inline buttons.

| Command | Typed | Guided |
| --- | --- | --- |
| /start | Welcome, privacy notice, main menu | New alert · My alerts · Pause · Help |
| /adicionar | `/adicionar rtx 4060 max 2000` | What to look for → highest price → summary (Save · Fix) |
| /alertas | Numbered list | Each alert with 🔕/🔔 turn off or on · ✏️ edit · 🗑 delete |
| /remover | `/remover 3` | List with a delete button per alert |
| /pausar | `/pausar 8h` (`30m`, `2 dias`, `amanhã`, `retomar`) | 1h · 8h · Until tomorrow · Resume |
| /ajuda | Search examples | Same |
| /apagarconta | Asks for confirmation | Delete everything · Cancel |
| /cancelar | Leaves the guided flow | — |

**Admin** (only `ADMIN_CHAT_ID`, the only one who registers channels): `/admin` (numbers, reader account status,
channels) · `/admin adicionar @canal1 @canal2` (several at once, or the Add channels button) · `/admin remover @canal` ·
`/admin canais`.

**The notification:** the name of the channel (a link to the original post), the whole post with its formatting —
bold, the coupon code, the links hidden behind words — and the alert that matched. Buttons: Open offer (the link the
channel posted) · See in the channel · Turn alert off · Not what I wanted (feedback, with a tip to refine the alert).

**Search syntax:**

| User types | Means |
| --- | --- |
| `rtx 4060` | contains "rtx" AND "4060", in any order |
| `"air fryer"` | the exact phrase |
| `ssd 1tb -sata` | contains ssd and 1tb, but not sata |

Case, accents, emojis and styled letters (𝗥𝗧𝗫) don't matter; `rtx4060` and `rtx 4060` are the same.

## Getting started

### Prerequisites

- [.NET SDK 10](https://dotnet.microsoft.com/download) and Docker.
- **The bot**, created inside Telegram: open a chat with [@BotFather](https://t.me/BotFather), send `/newbot`, pick a
  name and a username ending in `bot`, and copy the token it answers with.
- **The reader account** is your own Telegram account. Because it is a user account (MTProto), not a bot, it needs an
  `api_id` and an `api_hash`: log in at [my.telegram.org](https://my.telegram.org) with your number, open *API
  development tools* and create an app. This is the only step outside the Telegram app.
- Your chat id (send a message to [@userinfobot](https://t.me/userinfobot)) to be the admin.

### What the reader does with your account

- Reads only the channels you registered; the updates of any other chat are dropped on arrival, never read, logged or
  stored. It never lists your chats (no `GetAllDialogs`): it loads only the registered channels.
- Never sends a message, never marks anything as read, never changes your status.
- Joins a registered channel only when you are not in it yet, slowly (one every 15–25 min, at most 10 a day), and
  then mutes and archives it so it does not ring on your phone. Channels you already follow are left as they are.
- The session file in the `session` volume is worth a login of your account: keep it private. You can end it at any
  time in Telegram → Settings → Devices; the reader then stops and tells the admin, without asking Telegram for a new
  code.

### Secrets

```sh
cp .env.example .env   # fill in BOT_TOKEN, ADMIN_CHAT_ID, TELEGRAM_*, POSTGRES_PASSWORD
```

The `.env` and the session file of the reader are secrets: they are git-ignored and never go into the image.

### Running locally

Everything in containers (the recommended way, the same as in production):

```sh
docker compose build
docker compose run --rm app --login   # first login of the reader: type the code Telegram sends to your app
docker compose up -d
docker compose logs -f app
```

Or the app with `dotnet run`, against a local Postgres:

```sh
docker run -d --name promofilterbot-pg -p 5432:5432 \
  -e POSTGRES_USER=promofilterbot -e POSTGRES_PASSWORD=promofilterbot -e POSTGRES_DB=promofilterbot postgres:17
```

and a `src/PromoFilterBot.Host/appsettings.Development.json` (git-ignored) with your settings:

```json
{
    "ConnectionStrings": { "PromoFilterBotPostgres": "Host=localhost;Port=5432;Database=promofilterbot;Username=promofilterbot;Password=promofilterbot" },
    "General": { "AdminChatId": 123456789 },
    "Bot": { "Token": "123456:ABC..." },
    "Reader": { "ApiId": 12345, "ApiHash": "...", "PhoneNumber": "+5511999999999" }
}
```

```sh
dotnet run --project src/PromoFilterBot.Host -- --login   # once
dotnet run --project src/PromoFilterBot.Host
```

Set `Reader:Enabled` to `false` to work on the bot without the reader account.

## Deploy on a VM

Two containers (app and PostgreSQL) via docker compose, on the same free AWS EC2 as .ei, with no inbound port: the bot
uses long polling and the database is only reachable inside the network of the compose. The image is multi-arch
(`linux/amd64` for the t2/t3.micro, `linux/arm64` for Graviton). Each container has a memory cap (app 400 MB, database
256 MB) and the logs of Docker rotate, because the machine is small and shared.

### Once, on the VM

```sh
sudo mkdir -p /opt/promofilterbot && sudo chown $USER /opt/promofilterbot
```

From your machine, send the `.env` (filled in from `.env.example`):

```sh
scp -i <key>.pem .env ubuntu@<vm>:/opt/promofilterbot/.env
```

With 1 GB of RAM shared with .ei, check `free -h` and add swap if there is none:

```sh
sudo fallocate -l 2G /swapfile && sudo chmod 600 /swapfile && sudo mkswap /swapfile && sudo swapon /swapfile
echo '/swapfile none swap sw 0 0' | sudo tee -a /etc/fstab
```

### Once, on GitHub

In the repository, Settings → Secrets and variables → Actions (or in the `production` environment), the same values
as .ei:

| Secret | Value |
| --- | --- |
| `SSH_HOST` | public IP (or DNS) of the EC2 |
| `SSH_USER` | `ubuntu` (Ubuntu) or `ec2-user` (Amazon Linux) |
| `SSH_PRIVATE_KEY` | content of the `.pem` key |
| `SSH_PORT` | optional, 22 |
| `DEPLOY_DIR` | optional, `/opt/promofilterbot` |

### Every deploy

A push to `main` or `develop` runs `.github/workflows/deploy.yml` (also by hand: Actions → Deploy → Run workflow):

1. builds and tests the solution;
2. publishes `ghcr.io/joaolubaw/promofilterbot:sha-<commit>` (and `latest`, from `main`);
3. copies `docker-compose.yml` and `scripts/` to the VM, writes the image in the `.env` and runs `docker compose up -d`.
   The EF migrations are applied when the app starts.

### After the first deploy: the login of the reader

```sh
ssh -t -i <key>.pem ubuntu@<vm>
cd /opt/promofilterbot
docker compose run --rm app --login   # the code Telegram sends to your app (and your 2FA password, if any)
docker compose restart app
```

Until then the bot works, and tells the admin the reader needs a login. If the session is ever ended (Telegram →
Settings → Devices), the reader turns itself off (retrying would make Telegram send a new code each time) and tells
the admin: run the login again and restart the app.

Then register the channels in the chat with the bot: `/admin adicionar @canal1 @canal2 ...`. The channels you already
follow start being read within minutes; the others are joined little by little.

**Backup:** `scripts/backup.sh` dumps the database (`pg_dump`), keeps the last 7 days and, when `RCLONE_REMOTE` is set,
copies it out of the VM (Object Storage, Google Drive...). Schedule it with cron on the host:

```text
0 4 * * * cd /opt/promofilterbot && ./scripts/backup.sh >> backups/backup.log 2>&1
```

`scripts/restore.sh backups/<file>.sql.gz` restores one.

**Monitoring:** the bot messages the admin when the reader account needs a login, stays disconnected, gets a long
FloodWait, or when no post arrives from the channels for 6 hours (heartbeat). Logs go to the console and to
`logs/log-*.txt` (daily rotation, 14 days) in the `logs` volume.

## Database and migrations

Unlike rast-core, where the schema is hand-written SQL, here **the schema is generated by EF Core migrations** from the
model in `src/PromoFilterBot.Infrastructure/Models` + the entities of `PromoFilterBot.Domain/Models`. Tables and columns
keep the Rast convention: snake_case, every column prefixed by its table (`alert_max_price`), plus the `row_*` columns
of `BaseEntity`.

| Table | Main columns | Purpose |
| --- | --- | --- |
| `user` | chat id (unique), name, paused until, is active | Who uses the bot; the pause of /pausar |
| `alert` | user, original text, normalized terms, excluded terms, max price (null = no limit), is active | What each user wants |
| `channel` | telegram id (unique), access hash, username (unique), title, status (Approved · Active · Blocked) | Sources the admin registered |
| `channel_message` | channel, telegram message id (unique per channel), text, HTML, link, price, product key, store, coupon | Offers read; the copy sent and debugging (30 days) |
| `notification` | user, alert, message, product key, store, price, coupon, offer date, status, sent at, feedback | History; **unique** (user, product, store, price, coupon, day) |
| `conversation_state` | user, step, data (jsonb) | Step of the guided flow the user (or the admin) is in |

`/apagarconta` deletes the user row, and the database cascades delete everything of theirs.

```sh
dotnet tool restore
dotnet ef migrations add <Name> -p src/PromoFilterBot.Infrastructure -s src/PromoFilterBot.Infrastructure
```

The migrations need no database to be created. To apply them by hand instead of on startup, set
`Database:ApplyMigrationsOnStartup=false` and run `dotnet ef database update` with
`ConnectionStrings__PromoFilterBotPostgres` set.

## Configuration

Everything is in `appsettings.json` and can be overridden by environment variables (`Section__Key`). The business
limits are adjustable without a new build.

| Rule | Default | Setting |
| --- | --- | --- |
| Bot messages in total | ~30 per second | `Bot:GlobalMessagesPerSecond` (Telegram) |
| Bot messages to the same chat | ~1 per second | `Bot:PerChatIntervalMilliseconds` (Telegram) |
| Channels per reader account | 500 | `Reader:MaxChannels` (Telegram, without Premium) |
| Channels joined per day / interval | 10 / every 15–25 min | `Reader:MaxJoinsPerDay`, `JoinIntervalMinutes`, `JoinJitterMinutes` |
| Channel you already follow | read within ~1 min, no join | `Reader:ResolveIntervalSeconds` |
| Channels joined by the reader | muted and archived | `Reader:MuteJoinedChannels`, `ArchiveJoinedChannels` |
| Active alerts per user | 10 | `Limits:MaxActiveAlertsPerUser` |
| Same offer to the same user | once a day; another price, coupon or store is sent again | unique index of `notification` |
| Retention of the posts read | 30 days | `Limits:MessageRetentionDays` |
| Offer without a readable price, alert with a limit | sent, flagged | `Matching:NotifyWhenPriceUnknown` |
| Follow short links (amzn.to...) to identify product and store | on, at most 3 s | `Matching:ResolveShortLinks`, `ShortLinkHosts`, `ShortLinkTimeoutSeconds` |

Check Telegram's numbers in the [bot FAQ](https://core.telegram.org/bots/faq) before adjusting them.

## Tests

```sh
dotnet test
```

`PromoFilterBot.Domain.Tests` covers the heart of the system (normalization, search syntax, price, coupon and store
extraction, product key, inverted index, matching, the HTML of the copy); `PromoFilterBot.Tests` covers the services
(every expected failure and the success path), the handlers and the router, the notification format and the rate
limiter. CI runs them on every push and pull request (`.github/workflows/ci.yml`).

## Design notes

- **Copy, not forward:** the Bot API only forwards from chats the bot is in, and a bot cannot join a channel it does
  not run. So the reader keeps the post with its formatting (entities → HTML) and the bot sends a copy, under the
  name of the channel. If Telegram refuses the formatting, the plain text goes instead.
- **What tells two offers apart:** product × store × price × coupon, per day, enforced by a unique index
  (`NULLS NOT DISTINCT`, so an offer without a price counts too). The same offer posted by ten channels reaches the
  user once; a lower (or higher) price, another coupon or another store is sent at once. No hourly cap, no summary.
- **Price:** the value after "por" wins ("de R$ X por R$ Y"), otherwise the lowest one; installments ("10x de"),
  coupons, cashback, shipping thresholds and "R$ 50 OFF" are left out.
- **Coupon:** the code on the line of "cupom"/"código" (or on the next line): a word in capitals or a mix of letters
  and numbers, without the words of the offer around it (OFF, FRETE, BLACK FRIDAY...).
- **Store:** from the host of the links (Amazon, Mercado Livre, Shopee, Magalu, KaBuM, AliExpress and others; short
  links are followed), otherwise the store named in the text.
- **Product key:** Amazon ASIN, Mercado Livre MLB, Shopee, Magalu, KaBuM and AliExpress ids from the link (the link
  sent is always the one the channel posted); otherwise a hash of the normalized text.
- **Matching performance:** each alert is filed under its rarest word; an offer is only compared with the alerts whose
  anchor word it contains. The index is rebuilt when an alert changes and every 10 minutes.
- **Safety of the reader account:** see [What the reader does with your account](#what-the-reader-does-with-your-account);
  it also waits on every FloodWait and keeps an update state, so a restart catches up on what was posted meanwhile.
- **Privacy (LGPD):** only the chat id, the display name and the alerts are kept; /start says so and /apagarconta
  removes everything.
- **Out of scope:** monitoring prices straight in the stores, price history and a web panel (phase 4 of the roadmap).
