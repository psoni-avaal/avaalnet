# QuickBasket

QuickBasket is an ASP.NET Core MVC grocery storefront backed by PostgreSQL and EF Core. The customer store, session cart, demo checkout, product details, and protected inventory management all live in this project.

## Requirements

- .NET 8 SDK
- PostgreSQL
- `psql`

## Database setup and migration

Keep the existing PostgreSQL database and product rows. The migration is additive and idempotent; it adds grocery metadata and order-item tables and inserts sample groceries only when their names are absent.

For an existing database, provide the current connection through .NET User Secrets in Development or the `ConnectionStrings__DefaultConnection` environment variable in other environments. Do not put credentials in `appsettings.json` or commit them.

Apply the migration using the existing Development connection stored in User Secrets:

```bash
connection=$(dotnet user-secrets list | sed -n 's/^ConnectionStrings:DefaultConnection = //p')
field() { printf '%s' "$connection" | tr ';' '\n' | awk -F= -v key="$1" '{gsub(/^[[:space:]]+|[[:space:]]+$/, "", $1); if ($1 == key) print $2}'; }
PGPASSWORD="$(field Password)" psql -v ON_ERROR_STOP=1 \
	-h "$(field Host)" -p "$(field Port)" -U "$(field Username)" \
	-d "$(field Database)" -f Database/migrations/001_quickbasket.sql
unset connection
```

The migration uses existing `public.categories` and `public.products.category_id` where present. It creates the category table/foreign key safely if a fresh database does not have them yet. It does not drop tables or reset identity values.

## Local connection secret

Store the current connection string locally (replace the placeholder outside source control):

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<your existing PostgreSQL connection string>"
```

The current workspace already has the connection in User Secrets. For non-Development environments, set `ConnectionStrings__DefaultConnection` through the host's secret manager.

## Run

```bash
dotnet restore
dotnet build
ASPNETCORE_ENVIRONMENT=Development dotnet watch run --urls http://127.0.0.1:5080
```

Open <http://127.0.0.1:5080>.

## Store administration

New registrations are customer accounts. To grant an existing account inventory access, run this statement in PostgreSQL after replacing the email:

```sql
UPDATE public.store_users
SET is_admin = true
WHERE lower(email) = lower('<account email>');
```

Inventory management is at `/manage/products`. Product-write API endpoints require the `StoreAdmin` role; product reads remain public.

## Checkout

The cart is stored in ASP.NET Core session and re-priced from PostgreSQL on the server. Checkout creates an order header and item snapshots in one transaction and reduces stock atomically. Checkout is a demo order flow; no online payment is processed.

## Image sources

The initial grocery catalog uses Unsplash image URLs. Product cards and detail pages show an illustration fallback when a product image cannot load. Admins may upload JPG, PNG, or WEBP images up to 2 MB.
