# Azure Key Vault Console Example

This example is a manual live Azure-backed specimen for the Varlock `.NET` bridge. It keeps the same `builder.Configuration.AddVarlock()` entry point as the baseline console example, but resolves sensitive values from Azure Key Vault through the `@varlock/azure-key-vault-plugin`.

Inside this repository, the schema points directly at the checked-in Azure plugin package so the specimen can prove the bridge without requiring a separate example-local JavaScript install.

## What It Shows

- standard `IConfiguration` access still works when the backing values come from Azure Key Vault
- local development can use `az login` and the Azure CLI credential path that the plugin already supports
- the example reads the gitignored `.env.local` and seeds `AZURE_KEY_VAULT_URL` into process env before `AddVarlock()`, while the actual secret values stay in Key Vault
- the same schema can later move from local Azure CLI auth to Managed Identity without changing the app-facing keys

## Provision a throwaway proof vault

Run the setup script from the repository root:

```bash
bun run scripts/provision-dotnet-azure-key-vault-example.ts \
  --subscription <your-subscription-id> \
  --location <your-azure-region>
```

The script:

- creates a new resource group and Key Vault
- grants the signed-in Azure user secret `get`, `list`, and `set` permissions
- seeds `database-url` and `stripe-secret-key`
- writes `examples/dotnet-console-azure-key-vault/.env.local`

## Run the example

```bash
dotnet run --project examples/dotnet-console-azure-key-vault
```

Expected output should show the vault URL, the public base URL, and `loaded = true` for both Key Vault-backed secrets.

## Cleanup

The setup script prints the delete command for the resource group it created. The normal cleanup shape is:

```bash
az group delete --name <resource-group> --yes --no-wait
```

## Notes

- This specimen is intentionally manual and is not part of `bun run proof:dotnet`.
- The checked-in `.env` file is only a placeholder. `.env.local` supplies the real vault URL.
- If you want the broader Azure, AWS, and GCP comparison, see `packages/varlock-website/src/content/docs/integrations/dotnet/cloud-providers.mdx`.