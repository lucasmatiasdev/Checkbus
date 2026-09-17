# Development Seed Data

> **Development-only.** These credentials exist only in a locally seeded Development database.
> They are never created in Staging or Production — the seeder that inserts them is gated on
> `IHostEnvironment.IsDevelopment()` with no configuration flag able to re-enable it elsewhere.

## How it works

On every application startup in the `Development` environment, `Checkbus.Presentation/Program.cs`:

1. Applies any pending EF Core migrations (`Database.MigrateAsync()`).
2. Runs `DevelopmentDataSeeder.SeedAsync()`, which checks whether any `Organization` row already
   exists. If one does, seeding is skipped and logged as such. If the database is empty, it inserts
   the full seed graph below in a single `SaveChangesAsync` call.

Because of that guard, seeding only ever runs once per database. To re-seed, you would need an
empty `Organizations` table.

## Seeded organizations

| Organization | CUIT |
|---|---|
| Checkbus Norte | 20-11111111-1 |
| Checkbus Sur | 20-22222222-2 |

## Seeded users

All seeded users share the same password: `Checkbus.Dev!2026`

| Email | Organization | Role | Active | Notes |
|---|---|---|---|---|
| `admin@seed-norte.test` | Checkbus Norte | Administrator | Yes | |
| `operator@seed-norte.test` | Checkbus Norte | Operator | **No** | Seeded inactive on purpose, to exercise the account-inactive login rejection path. |
| `admin@seed-sur.test` | Checkbus Sur | Administrator | Yes | |

Every seeded user has `MustChangePassword = false`, so logging in does not force a password-change
redirect.

## Global profile

A tenant-agnostic "Global Support" profile is seeded with `OrganizationId = null`. It is visible
across every tenant context (not tied to Norte or Sur), which is what lets a developer verify the
optional-tenant query filter behavior described in the spec's "Tenant Isolation Coverage"
scenario.

## Verifying tenant isolation manually

1. Log in as `admin@seed-norte.test` and confirm you only see Checkbus Norte's data.
2. Log in as `admin@seed-sur.test` and confirm you only see Checkbus Sur's data.
3. Confirm both sessions can see the "Global Support" profile.
4. Attempt to log in as `operator@seed-norte.test` and confirm authentication rejects it (inactive
   account).
