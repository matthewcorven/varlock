# Reload Console Example

Demonstrates `ReloadOnChange` — automatic configuration reload when `.env` files change.

## What It Shows

- `source.ReloadOnChange = true` opt-in on `AddVarlock()`
- `ChangeToken.OnChange()` callback fires on `.env` edits
- Varlock re-resolves the schema and updates `IConfiguration` automatically
- 300ms debounce prevents rapid-fire reloads during editor saves

## When to Use This Pattern

Long-running console apps, background workers, or services that need to pick up configuration changes without restarting.

## Run

```bash
dotnet run
```

Then edit `.env` (e.g., change `MAX_RETRIES=5`) and save — you'll see the reload message.
