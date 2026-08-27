# Item Finder

[![CI](https://github.com/DenysTudovshi/Eaton-Test-Assigment/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/DenysTudovshi/Eaton-Test-Assigment/actions/workflows/ci.yml)

A console application that reads a data file describing where items are located,
lists the items it finds, and prints step-by-step directions to whichever item you
pick.

```
Available items:

[1] - Coffee Mug
[2] - Cookies
[3] - Milk
[4] - Mobile Phone
[5] - Pencils

What item would you like to search for?
4

Walk to the end of the hall.
Turn right.
Go through the door at the end of the hall.
Look on top of the desk.

Press Enter to continue...
```

Items are listed alphabetically. Type an item's number and press Enter to see its
directions; press Enter again to go back to the list, and `q` at the selection
prompt quits.

A companion [Web API](#web-api) serves the same item directory over HTTP and lets
an administrator download, replace, or delete the data file.

## Web API

| Method | Route | Auth | Description |
|---|---|---|---|
| GET | `/api/v1/items?search=&name=&fields=&page=&pageSize=` | — | Items with their directions, alphabetical; case-insensitive substring search and paging; repeated `name` params fetch several items' directions in one request; `fields=name` returns just the names |
| GET | `/api/v1/items/{name}` | — | One item by exact, case-insensitive name; 404 if absent |
| POST | `/api/v1/identity/register` · `/login` | — | Account registration and login — the entire identity surface; login returns the bearer token |
| GET | `/api/v1/data-file` | Admin | Download the current data file |
| PUT | `/api/v1/data-file` | Admin | Upload a replacement (multipart `file` field); validated against the grammar before it replaces anything |
| DELETE | `/api/v1/data-file` | Admin | Remove the data file; idempotent |

### Authentication

Anyone may register and log in, but the data-file endpoints require the `Admin`
role, held only by the admin account seeded at startup — registration never
grants it. Log in for a bearer token; in Swagger UI, paste the `accessToken`
into the **Authorize** dialog:

```
curl -X POST http://localhost:5054/api/v1/identity/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"ChangeMe!123"}'
# → {"tokenType":"Bearer","accessToken":"...", "expiresIn":3600, ...}
```

Register and login are the only identity endpoints; tokens expire after an hour.
Logins are rate-limited and accounts lock temporarily after repeated failures.

## The data file

Directions and items form a hierarchy drawn with text characters:

```
+ Walk to the end of the hall.
├──+ Turn left.
|  └──+ Go through the first door on the right.
|     ├──+ Open the cabinet on the left.
|     |  └── Item: Cookies
```

- A line starting with `+ ` is a root direction; nested lines add `|  ` or three
  spaces per level before a `├──`/`└──` branch.
- `+ ` after the branch marks a further direction; ` Item: ` marks an item.
- Items are always leaves, and each item name is unique.

Invalid files — blank lines, skipped levels, entries nested under an item,
duplicate item names, or lines that fit no known shape — are rejected with a
message naming the offending line. A missing or unreadable file gets a clear
message too, and the app exits with a non-zero code.

Three sample files live in [`data/`](data/): the small one above, a nine-item
building layout ([`Data-medium.txt`](data/Data-medium.txt)), and a 22-item site
spanning three buildings with nesting six levels deep
([`Data-complex.txt`](data/Data-complex.txt)).

## Architecture

```
ItemFinder.ConsoleApp ──┐
                        ├──► ItemFinder.Application ◄── ItemFinder.Infrastructure
ItemFinder.Api ─────────┘             │
                                      ▼
                             ItemFinder.Domain
```

| Project | Responsibility |
|---|---|
| `ItemFinder.Domain` | The direction tree: direction nodes, item leaves, and traversal that pairs every item with its chain of directions. No dependencies. |
| `ItemFinder.Application` | Use cases over the tree — the alphabetical item list and per-item direction lookup — plus the parser contract (`IDataFileParser`) and its result types. |
| `ItemFinder.Infrastructure` | The data file parser: grammar, structural validation, friendly line-numbered errors, file access. |
| `ItemFinder.ConsoleApp` | The interactive flow and composition root. All console I/O sits behind an `IConsole` abstraction with the real adapter in one class. |
| `ItemFinder.Api` | The HTTP presentation layer: minimal-API endpoints, Swagger, request validation (MediatR pipeline + FluentValidation), and ASP.NET Core Identity with users and roles in SQLite via EF Core. Identity and EF live only here — the core stays auth-free. |

The solution follows Clean Architecture: dependencies point inward only, and the
rule is enforced through project references. The console app and the Web API are
peer presentation layers over the same `Application` core — each can evolve or be
replaced without touching the other or the domain logic, and a further front-end
(desktop, say) would be one more project, not a rewrite.

Design notes:

- Parsing problems are values, not exceptions: the parser returns either the tree
  or a list of errors with line numbers, and only the console boundary decides
  how to present them. A last-resort handler turns anything unexpected into a
  single friendly message rather than a stack trace.
- Item names are trimmed, and the parser accepts both LF and CRLF line endings
  and an optional UTF-8 byte-order mark, so files behave the same across
  platforms and editors.
- Each layer is unit-tested against its boundary; the parser is additionally
  tested against both sample files and a fixture per validation rule.

## Run in Docker

Requires Docker (Docker Desktop on Windows/macOS).

CI publishes ready-made images to GitHub Container Registry on every green build
of `main`, so both apps also run without cloning the repo:

```
docker run -it --rm ghcr.io/denystudovshi/item-finder:latest
```

```
docker run -p 5054:8080 -e ITEMFINDER_ADMIN_EMAIL=admin@example.com -e ITEMFINDER_ADMIN_PASSWORD='ChangeMe!123' ghcr.io/denystudovshi/item-finder-api:latest
```

The sections below build the same images from source.

### Console app

```
docker build -t item-finder .
docker run -it --rm item-finder
```

Use `-it` so the app can read your input.

To run against your own data file, mount it over the bundled default — the app
then picks it up automatically. `{file path}` is the file on your machine,
absolute or relative to the directory you run the command from; the rest is
literal (`/app/Data.txt` is where the app looks inside the container, `ro`
mounts your file read-only):

```
docker run -it --rm -v "{file path}:/app/Data.txt:ro" item-finder
```

### Web API

Put the admin credentials in a `.env` file next to `docker-compose.yml`
(git-ignored, loaded by Compose automatically):

```
ITEMFINDER_ADMIN_EMAIL=admin@example.com
ITEMFINDER_ADMIN_PASSWORD=ChangeMe!123
```

then:

```
docker compose up --build
```

Swagger UI: http://localhost:5054/swagger. A named volume keeps the state across
container recreation: the data file (deletions stick too), the user database, and
the token signing keys, so existing logins keep working after a rebuild.
`docker compose down` stops the API and keeps that state; `docker compose down -v`
resets everything. The image can also be built standalone:
`docker build -f Dockerfile.api -t item-finder-api .` (the console `Dockerfile`
is separate and unchanged).

## Run locally

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

### Console app

```
dotnet run --project src/ItemFinder.ConsoleApp
```

The bundled `Data.txt` is used by default. Point it at another file with an
argument (`-- {file path}`, absolute or relative) or the `ITEMFINDER_DATA_FILE`
environment variable; the argument wins when both are given.

### Web API

```
dotnet user-secrets set ITEMFINDER_ADMIN_EMAIL admin@example.com --project src/ItemFinder.Api
dotnet user-secrets set ITEMFINDER_ADMIN_PASSWORD 'ChangeMe!123' --project src/ItemFinder.Api
dotnet run --project src/ItemFinder.Api
```

Swagger UI: http://localhost:5054/swagger. The two secrets seed the admin account
on first start and are optional — without them the API still serves the public
item endpoints, but nobody can manage the data file.

### Tests

```
dotnet test
```

## Pre-release notes

Before deploying to production:

- The container speaks plain HTTP; terminate TLS in front of it.
- The login rate limiter keys on the direct peer address. Behind a proxy or load
  balancer, configure forwarded headers first or every client shares one bucket.
- Run without the volume and tokens, users, and data-file changes all reset with
  the container.
