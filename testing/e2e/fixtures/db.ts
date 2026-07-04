/**
 * Thin Postgres helper for specs that need to compute ground truth or set up
 * fixtures directly in the DB (the `E2E_PG_*` values from .env.e2e.example).
 *
 * Most specs verify DB state via the Postgres MCP in the review loop; a few
 * data-correctness specs (e.g. reports) need an in-test connection to compute
 * expected values and to arrange preconditions that have no UI/API path.
 */
import { Client } from 'pg';

export function pgClient(): Client {
  return new Client({
    host: process.env.E2E_PG_HOST || 'localhost',
    port: Number(process.env.E2E_PG_PORT || 5435),
    database: process.env.E2E_PG_DB || 'AttendanceDb',
    user: process.env.E2E_PG_USER || 'postgres',
    password: process.env.E2E_PG_PASSWORD || 'postgres'
  });
}

/** Run a query and return the single scalar of the first row (or null). */
export async function scalar<T = string | number>(
  db: Client,
  sql: string,
  params: unknown[] = []
): Promise<T | null> {
  const r = await db.query(sql, params);
  if (!r.rows.length) return null;
  return Object.values(r.rows[0])[0] as T;
}
