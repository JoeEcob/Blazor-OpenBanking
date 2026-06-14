# Spendy → Actual Budget Sync

Reads transactions from the Spendy SQLite database (populated via TrueLayer) and imports them into Actual Budget using the `@actual-app/api` package.

## Prerequisites

- Node.js 18+
- A running Actual Budget server
- Your Spendy SQLite DB (`Spendy-Sqlite.db`)

## Setup

```bash
npm install
```

## Configuration

Set these environment variables (or edit `CONFIG` at the top of `sync.js`):

| Variable | Description |
|---|---|
| `ACTUAL_SERVER_URL` | URL of your Actual server, e.g. `http://localhost:5006` |
| `ACTUAL_PASSWORD` | Your Actual server password |
| `ACTUAL_BUDGET_ID` | Sync ID from Settings → Show advanced settings → Sync ID |
| `SPENDY_DB_PATH` | Path to `Spendy-Sqlite.db` |
| `ACTUAL_ENCRYPTION_PASSWORD` | Only needed if E2E encryption is enabled |
| `ACTUAL_DATA_DIR` | Local cache dir for Actual (default: `./actual-data`) |

Example using a `.env` file with `dotenv` or just exporting directly:

```bash
export ACTUAL_SERVER_URL=http://localhost:5006
export ACTUAL_PASSWORD=hunter2
export ACTUAL_BUDGET_ID=1cfdbb80-6274-49bf-b0c2-737235a4c81f
export SPENDY_DB_PATH=/path/to/Spendy-Sqlite.db
```

## Usage

```bash
# Dry run — preview what would be synced
node sync.js --dry-run

# Live sync (last 90 days, default)
node sync.js

# Sync last 30 days
node sync.js --days 30

# Sync a full year
node sync.js --days 365
```

## Account matching

The script matches Spendy accounts to Actual accounts by **display name** (case-insensitive). So if your Spendy account is named "Monzo Current Account", your Actual account must also be named "Monzo Current Account".

If auto-matching fails, you'll see a warning listing the unmapped accounts. You can add manual overrides near the bottom of `sync.js`:

```js
const ACCOUNT_OVERRIDES = {
  'spendy-account-id': 'actual-account-uuid',
};
```

## How it works

1. Opens the SQLite DB and reads `Account`, `Card`, and `Transaction` tables.
2. Filters transactions to the requested date window.
3. Connects to your Actual server and loads the budget.
4. Matches Spendy accounts → Actual accounts by display name.
5. Calls `importTransactions` for each account — this deduplicates by `imported_id` (the TrueLayer transaction ID), so re-running is safe.
6. Syncs the budget back to the server.

## Running on a schedule

To sync automatically, add a cron job:

```cron
# Sync every morning at 7am
0 7 * * * cd /path/to/spendy-to-actual && node sync.js >> /var/log/spendy-sync.log 2>&1
```
