/**
 * sync.js -- Sync Spendy (TrueLayer) SQLite transactions to Actual Budget
 *
 * Usage:
 *   node sync.js              # live sync
 *   node sync.js --dry-run    # preview without writing to Actual
 *   node sync.js --days 90    # how many days back to sync (default: 90)
 *
 * Required env vars (or edit CONFIG below):
 *   ACTUAL_SERVER_URL   e.g. http://localhost:5006
 *   ACTUAL_PASSWORD     your Actual server password
 *   ACTUAL_BUDGET_ID    Settings > Show advanced settings > Sync ID
 *   SPENDY_DB_PATH      path to Spendy-Sqlite.db
 *
 * Optional:
 *   ACTUAL_ENCRYPTION_PASSWORD   if end-to-end encryption is enabled
 *   ACTUAL_DATA_DIR              local cache dir (default: ./actual-data)
 */

process.loadEnvFile();

import * as api from '@actual-app/api';
import Database from 'better-sqlite3';
import fs from 'fs';

// ---------------------------------------------------------------------------
// Configuration -- override via env vars or edit directly
// ---------------------------------------------------------------------------

const CONFIG = {
  actualServerUrl: process.env.ACTUAL_SERVER_URL || 'http://localhost:5006',
  actualPassword: process.env.ACTUAL_PASSWORD || 'your-password-here',
  actualBudgetId: process.env.ACTUAL_BUDGET_ID || 'your-sync-id-here',
  actualEncryptionPassword: process.env.ACTUAL_ENCRYPTION_PASSWORD || null,
  actualDataDir: process.env.ACTUAL_DATA_DIR || './actual-data',
  spendyDbPath: process.env.SPENDY_DB_PATH || './Spendy-Sqlite.db',
};

// ---------------------------------------------------------------------------
// CLI flags
// ---------------------------------------------------------------------------

const args = process.argv.slice(2);
const DRY_RUN = args.includes('--dry-run');
const daysIdx = args.indexOf('--days');
const DAYS_BACK = daysIdx !== -1 ? parseInt(args[daysIdx + 1], 10) : 90;

// ---------------------------------------------------------------------------
// SQLite helpers -- Spendy stores each entity as a JSON blob in the Data column
// ---------------------------------------------------------------------------

function readSpendyTable(dbPath, tableName) {
  if (!fs.existsSync(dbPath)) {
    throw new Error(`Spendy DB not found at: ${dbPath}`);
  }
  const db = new Database(dbPath, { readonly: true });
  try {
    const tables = db.prepare(
      `SELECT name FROM sqlite_master WHERE type='table' AND name=?`
    ).all(tableName);
    if (tables.length === 0) {
      console.warn(`  Table "${tableName}" does not exist in the DB -- skipping.`);
      return [];
    }
    const rows = db.prepare(`SELECT Data FROM "${tableName}"`).all();
    return rows.map(r => JSON.parse(r.Data));
  } finally {
    db.close();
  }
}

// ---------------------------------------------------------------------------
// Amount conversion
//
// Spendy stores amounts as decimals (e.g. -12.50).
// Actual stores amounts as integers in pence (e.g. -1250).
//
// Sign convention by account type:
//   Current/savings: TrueLayer sends debits negative, credits positive -- matches Actual directly.
//   Credit cards:    TrueLayer sends purchases as POSITIVE (balance owed increases)
//                   and repayments as NEGATIVE. Actual expects the opposite.
//                   We flip the sign for credit card accounts.
// ---------------------------------------------------------------------------

function toActualAmount(decimalAmount, flipSign = false) {
  const pence = Math.round(parseFloat(decimalAmount) * 100);
  return flipSign ? -pence : pence;
}

// ---------------------------------------------------------------------------
// Transfer detection
//
// TrueLayer classifies transfers via TransactionClassification (array) and/or
// TransactionType. We detect these so they can be flagged in Actual.
//
// Full linking of paired transfers (e.g. current -> credit card payment) is not
// attempted automatically because it requires matching amounts across accounts
// on the same date -- too fragile without manual confirmation. Instead we set
// the payee to "Transfer" and add a note so you can match them in the Actual UI.
// ---------------------------------------------------------------------------

const TRANSFER_CLASSIFICATIONS = new Set(['Transfer', 'Transfers']);
const TRANSFER_TYPES = new Set([
  'TRANSFER', 'FPS_CREDIT', 'FPS_DEBIT', 'BACS_CREDIT', 'BACS_DEBIT',
]);

function isTransfer(spendyTx) {
  const classifications = spendyTx.TransactionClassification ?? [];
  if (classifications.some(c => TRANSFER_CLASSIFICATIONS.has(c))) return true;
  if (TRANSFER_TYPES.has(spendyTx.TransactionType)) return true;
  if (TRANSFER_TYPES.has(spendyTx.TransactionCategory)) return true;
  return false;
}

// ---------------------------------------------------------------------------
// Date helpers
// ---------------------------------------------------------------------------

function toISODate(timestamp) {
  return new Date(timestamp).toISOString().split('T')[0];
}

function cutoffDate(daysBack) {
  const d = new Date();
  d.setDate(d.getDate() - daysBack);
  return d;
}

// ---------------------------------------------------------------------------
// Map a Spendy Transaction to an Actual transaction object
// ---------------------------------------------------------------------------

function mapTransaction(spendyTx, actualAccountId, isCreditCard) {
  const transfer = isTransfer(spendyTx);

  const payeeName = transfer
    ? 'Transfer'
    : (spendyTx.MerchantName || spendyTx.Description || 'Unknown').slice(0, 100);

  const noteParts = [
    spendyTx.Description,
    spendyTx.TransactionCategory,
    spendyTx.TransactionType,
    spendyTx.TransactionClassification?.join(', '),
  ].filter(Boolean);

  return {
    account: actualAccountId,
    date: toISODate(spendyTx.Timestamp),
    amount: toActualAmount(spendyTx.Amount, isCreditCard),
    payee_name: payeeName,
    imported_payee: spendyTx.Description?.slice(0, 100),
    imported_id: spendyTx.TransactionId,
    notes: noteParts.join(' | ').slice(0, 500) || null,
    cleared: true,
  };
}

// ---------------------------------------------------------------------------
// Account matching -- match Spendy accounts to Actual accounts by display name
// ---------------------------------------------------------------------------

function buildAccountMap(spendyAccounts, spendyCards, actualAccounts) {
  // Map: Spendy AccountId -> { actualId, isCreditCard }
  // Anything from the Card table is treated as a credit card (signs flipped).
  const cardAccountIds = new Set(spendyCards.map(c => c.AccountId));
  const map = {};

  for (const actual of actualAccounts) {
    const normActual = actual.name.trim().toLowerCase();

    for (const spendy of [...spendyAccounts, ...spendyCards]) {
      const normSpendy = (spendy.CustomDisplayName || spendy.DisplayName || '')
        .trim()
        .toLowerCase();

      if (normSpendy && normSpendy === normActual) {
        const isCreditCard = cardAccountIds.has(spendy.AccountId);
        map[spendy.AccountId] = { actualId: actual.id, isCreditCard };
        const tag = isCreditCard ? ' [credit card -- signs will be flipped]' : '';
        console.log(`  Matched: "${spendy.CustomDisplayName || spendy.DisplayName}" -> Actual "${actual.name}"${tag}`);
      }
    }
  }

  return map;
}

// ---------------------------------------------------------------------------
// Main
// ---------------------------------------------------------------------------

async function main() {
  console.log('=== Spendy -> Actual Budget Sync ===');
  if (DRY_RUN) console.log('DRY RUN -- no changes will be written to Actual\n');

  // 1. Read Spendy data
  console.log(`\nReading Spendy DB: ${CONFIG.spendyDbPath}`);
  const spendyAccounts = readSpendyTable(CONFIG.spendyDbPath, 'Account');
  const spendyCards = readSpendyTable(CONFIG.spendyDbPath, 'Card');
  const allTransactions = readSpendyTable(CONFIG.spendyDbPath, 'Transaction');

  console.log(`  Accounts:             ${spendyAccounts.length}`);
  console.log(`  Cards:                ${spendyCards.length}`);
  console.log(`  Transactions (total): ${allTransactions.length}`);

  // 2. Filter to sync window
  const cutoff = cutoffDate(DAYS_BACK);
  const transactions = allTransactions.filter(tx => new Date(tx.Timestamp) >= cutoff);
  console.log(`  Transactions (last ${DAYS_BACK} days): ${transactions.length}`);

  // 3. Connect to Actual
  console.log(`\nConnecting to Actual at ${CONFIG.actualServerUrl} ...`);

  if (!fs.existsSync(CONFIG.actualDataDir)) {
    fs.mkdirSync(CONFIG.actualDataDir, { recursive: true });
  }

  await api.init({
    dataDir: CONFIG.actualDataDir,
    serverURL: CONFIG.actualServerUrl,
    password: CONFIG.actualPassword,
  });

  const downloadOpts = CONFIG.actualEncryptionPassword
    ? { password: CONFIG.actualEncryptionPassword }
    : undefined;

  await api.downloadBudget(CONFIG.actualBudgetId, downloadOpts);
  console.log('  Budget loaded.');

  // 4. Match accounts
  const actualAccounts = await api.getAccounts();
  console.log(`\nActual accounts (${actualAccounts.length}):`);
  actualAccounts.forEach(a => console.log(`  - "${a.name}" type=${a.type} (${a.id})`));

  console.log('\nMatching Spendy accounts to Actual accounts:');
  const accountMap = buildAccountMap(spendyAccounts, spendyCards, actualAccounts);

  // Optional manual overrides -- add entries here if auto-matching fails:
  // accountMap['spendy-account-id'] = { actualId: 'actual-uuid', isCreditCard: false };
  const ACCOUNT_OVERRIDES = {};
  Object.assign(accountMap, ACCOUNT_OVERRIDES);

  const unmapped = [...spendyAccounts, ...spendyCards].filter(a => !accountMap[a.AccountId]);
  if (unmapped.length > 0) {
    console.warn('\n  WARNING: Unmapped Spendy accounts (transactions will be skipped):');
    unmapped.forEach(a =>
      console.warn(`    - "${a.CustomDisplayName || a.DisplayName}" (AccountId: ${a.AccountId})`)
    );
    console.warn('  Fix: ensure Actual account names match your Spendy display names exactly,');
    console.warn('  or add entries to ACCOUNT_OVERRIDES in this script.\n');
  }

  // 5. Group and import
  const byAccount = {};
  for (const tx of transactions) {
    const match = accountMap[tx.AccountId];
    if (!match) continue;
    const key = match.actualId;
    if (!byAccount[key]) byAccount[key] = { ...match, txs: [] };
    byAccount[key].txs.push(tx);
  }

  let totalAdded = 0;
  let totalUpdated = 0;
  const totalSkipped = transactions.length - Object.values(byAccount).reduce((n, b) => n + b.txs.length, 0);

  console.log('\nImporting transactions:');

  for (const [actualAccountId, { isCreditCard, txs }] of Object.entries(byAccount)) {
    const accountName = actualAccounts.find(a => a.id === actualAccountId)?.name ?? actualAccountId;
    const mapped = txs.map(tx => mapTransaction(tx, actualAccountId, isCreditCard));
    const transfers = mapped.filter(tx => tx.payee_name === 'Transfer').length;

    console.log(`  ${accountName}: ${mapped.length} transactions (${transfers} transfers, credit card signs flipped: ${isCreditCard})`);

    if (DRY_RUN) {
      mapped.slice(0, 5).forEach(tx =>
        console.log(`    [dry] ${tx.date}  ${String(tx.payee_name).padEnd(30)}  GBP ${(tx.amount / 100).toFixed(2)}`)
      );
      if (mapped.length > 5) console.log(`    ... and ${mapped.length - 5} more`);
      continue;
    }

    const result = await api.importTransactions(actualAccountId, mapped, {
      defaultCleared: true,
      reimportDeleted: args.includes('--reimport-deleted'),
    });

    totalAdded += result.added?.length ?? 0;
    totalUpdated += result.updated?.length ?? 0;

    if (result.errors?.length) {
      console.error(`    Errors: ${JSON.stringify(result.errors)}`);
    }

    console.log(`    -> added: ${result.added?.length ?? 0}, updated: ${result.updated?.length ?? 0}`);
  }

  // 6. Sync to server
  if (!DRY_RUN) {
    console.log('\nSyncing to server ...');
    await api.sync();
    console.log('  Done.');
  }

  await api.shutdown();

  console.log('\n=== Summary ===');
  console.log(`  Transactions in window:     ${transactions.length}`);
  console.log(`  Skipped (no account match): ${totalSkipped}`);
  if (!DRY_RUN) {
    console.log(`  Added:   ${totalAdded}`);
    console.log(`  Updated: ${totalUpdated}`);
  } else {
    console.log('  (dry run -- nothing written)');
  }
  console.log('\nAll done!');
}

main().catch(err => {
  console.error('\nFatal error:', err.message ?? err);
  process.exit(1);
});
