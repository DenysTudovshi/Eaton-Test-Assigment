# Item Finder

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

## Quick start

Requires the .NET 8 SDK.

```
dotnet run --project src/ItemFinder.ConsoleApp
```

The bundled `Data.txt` is used by default. To use another file, pass its path:

```
dotnet run --project src/ItemFinder.ConsoleApp -- path/to/MyData.txt
```

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
ItemFinder.ConsoleApp ──► ItemFinder.Application ◄── ItemFinder.Infrastructure
                                   │
                                   ▼
                          ItemFinder.Domain
```

| Project | Responsibility |
|---|---|
| `ItemFinder.Domain` | The direction tree: direction nodes, item leaves, and traversal that pairs every item with its chain of directions. No dependencies. |
| `ItemFinder.Application` | Use cases over the tree — the alphabetical item list and per-item direction lookup — plus the parser contract (`IDataFileParser`) and its result types. |
| `ItemFinder.Infrastructure` | The data file parser: grammar, structural validation, friendly line-numbered errors, file access. |
| `ItemFinder.ConsoleApp` | The interactive flow and composition root. All console I/O sits behind an `IConsole` abstraction with the real adapter in one class. |

Dependencies point inward only, enforced through project references: the console
front-end can be replaced (say, by a web or desktop UI) by adding a new
presentation project over the same `Application` layer, without touching the
core.

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
