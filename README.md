# Item Finder

[![CI](https://github.com/DenysTudovshi/Eaton-Test-Assigment/actions/workflows/ci.yml/badge.svg)](https://github.com/DenysTudovshi/Eaton-Test-Assigment/actions/workflows/ci.yml)

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

## Quick start

Requires the .NET 8 SDK.

```
dotnet run --project src/ItemFinder.ConsoleApp
```

The bundled `Data.txt` is used by default. To use another file, pass its path:

```
dotnet run --project src/ItemFinder.ConsoleApp -- path/to/MyData.txt
```

The `ITEMFINDER_DATA_FILE` environment variable also sets the path; a CLI
argument wins over it when both are given.

Run the tests:

```
dotnet test
```

## Docker

```
docker build -t item-finder .
docker run -it --rm item-finder
```

Use `-it` so the app can read your input. To run against a different data file,
mount it and pass its path:

```
docker run -it --rm -v "$(pwd)/data:/data:ro" item-finder /data/Data-medium.txt
```

or point the environment variable at it:

```
docker run -it --rm -v "$(pwd)/data:/data:ro" -e ITEMFINDER_DATA_FILE=/data/Data-medium.txt item-finder
```

## Web API

`ItemFinder.Api` is a minimal API over the same core, with interactive
documentation via Swagger UI:

```
dotnet run --project src/ItemFinder.Api
```

then open http://localhost:5054/swagger.

| Method | Route | Auth | Description |
|---|---|---|---|
| GET | `/api/v1/items?search=&name=&fields=&page=&pageSize=` | — | Items with their directions, alphabetical; case-insensitive substring search and paging; repeated `name` params fetch several items' directions in one request; `fields=name` returns just the names |
| GET | `/api/v1/items/{name}` | — | One item by exact, case-insensitive name; 404 if absent |
| POST | `/api/v1/identity/register` · `/login` | — | Account registration and login — the entire identity surface; login returns the bearer token |
| GET | `/api/v1/data-file` | Admin | Download the current data file |
| PUT | `/api/v1/data-file` | Admin | Upload a replacement (multipart `file` field); 201 on first upload, 200 on replace |
| DELETE | `/api/v1/data-file` | Admin | Remove the data file; idempotent 204 |

Errors are RFC 7807 problem details throughout: `401` without a token, `403`
authenticated without the role, `400` with per-field messages for invalid input.

Two conveniences on the item list mirror how the console app is used:

```
curl "http://localhost:5054/api/v1/items?fields=name"
# the console-style suggestion list - names only, alphabetical

curl "http://localhost:5054/api/v1/items?name=Coffee%20Mug&name=Pencils"
# directions for several items in one request; a name that matches nothing
# simply contributes nothing (use /api/v1/items/{name} for a per-name 404)
```

The `name` filter is exact (case-insensitive) and mutually exclusive with
`search`; both combine with `fields=name` and paging.

### Authentication and the admin account

Anyone may register and log in, but the data-file endpoints require the `Admin`
role, which only the account seeded at startup holds — registration never grants
it. Configure the admin before starting, via user secrets:

```
dotnet user-secrets set ITEMFINDER_ADMIN_EMAIL admin@example.com --project src/ItemFinder.Api
dotnet user-secrets set ITEMFINDER_ADMIN_PASSWORD 'ChangeMe!123' --project src/ItemFinder.Api
```

or the environment variables of the same names. Without them the API still serves
the public endpoints and logs a warning; the data-file endpoints just have no one
who can use them.

```
curl -X POST http://localhost:5054/api/v1/identity/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"ChangeMe!123"}'
# → {"tokenType":"Bearer","accessToken":"...", "expiresIn":3600, ...}

curl -X PUT http://localhost:5054/api/v1/data-file \
  -H "Authorization: Bearer <accessToken>" \
  -F "file=@data/Data-medium.txt;type=text/plain"
# → {"itemCount":9}
```

In Swagger UI, paste the `accessToken` into the **Authorize** dialog to call the
protected endpoints from the browser.

Register and login are deliberately the only identity endpoints — there is no
refresh, password reset, or profile management. Tokens expire after an hour;
log in again to get a new one.

### Upload validation

An uploaded file is checked against the same grammar the console app uses
*before* anything is stored. A file that fails gets `422` with every error's
kind, 1-based line number, and message — and the previous file stays in place
untouched. Only `.txt` files up to 1 MB are accepted (`400` otherwise, checked
before parsing). An accepted upload is parsed once, atomically swapped in, and
visible on `GET /api/v1/items` immediately. After a delete, the item list is
empty and `GET /api/v1/data-file` returns 404 until the next upload.

Login endpoints are rate-limited (10 requests per minute per client address) and
accounts lock temporarily after repeated failed passwords.

### Web API in Docker

```
ITEMFINDER_ADMIN_EMAIL=admin@example.com \
ITEMFINDER_ADMIN_PASSWORD='ChangeMe!123' \
docker compose up --build
```

serves the API on http://localhost:5054/swagger. A named volume keeps the state
across container recreation: the uploaded data file (a deletion sticks too — the
store stays empty until the next upload), the user database, and the token
signing keys, so existing logins keep working after the container is rebuilt.
The image can also be built directly:
`docker build -f Dockerfile.api -t item-finder-api .` (the console `Dockerfile`
is separate and unchanged).

Before deploying to production:

- The container speaks plain HTTP; terminate TLS in front of it.
- The login rate limiter keys on the direct peer address. Behind a proxy or load
  balancer, configure forwarded headers first or every client shares one bucket.
- Run without the volume and tokens, users, and data-file changes all reset with
  the container.

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

Two sample files live in [`data/`](data/): the small one above and a nine-item
building layout ([`Data-medium.txt`](data/Data-medium.txt)).

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

Dependencies point inward only, enforced through project references — which is
exactly how the Web API was added: a second presentation project over the same
`Application` layer, with the console app left untouched.

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
