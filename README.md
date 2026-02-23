# MultiThreading — multi-threaded website downloader

a .NET console application that recursively downloads web resources with configurable concurrency, depth limits, `robots.txt` support and saves files into a local directory structure.

Where to find sources: [MultiThreading/Program.cs](MultiThreading/Program.cs#L1-L200)

Main features:

- Recursive download of pages and assets (scripts, images, CSS).
- Depth limit for recursion (`MaxDepth`).
- Concurrency limit for worker tasks (`MaxConcurrency`).
- Respects `robots.txt` with caching of rules.
- Host restriction modes: `domain`, `subDomains`, `any`.
- Saves content into `output` directory organized by host.

Requirements

- .NET SDK 10 (project targets `net10.0`)

## Build and tests

Build the solution:

```bash
dotnet build
```

Run unit tests:

```bash
dotnet test
```

## Running the application

Example invocation:

```bash
dotnet run --project MultiThreading -- "google.com,github.com" 2 10
```

__Arguments__ (in order passed to `args`):

- `<rootUrl>` — comma-separated list of URLs to crawl (required).
- `[maxDepth]` — maximum crawl depth (defaults from config when omitted).
- `[maxConcurrency]` — number of concurrent worker tasks.
- `[downloadMode]` — host matching mode: `domain`, `subDomains`, `any`.
- `[maxAllowedLinks]` — maximum links allowed in the queue.

Example:

```bash
dotnet run --project MultiThreading -- "example.com" 3 15
```

If required arguments are missing or invalid the program prints a usage helper (see [MultiThreading/Program.cs](MultiThreading/Program.cs#L1-L200)).

### Configuration

Application settings are read from `appsettings.json` in the `MultiThreading` folder (see [MultiThreading/appsettings.json](MultiThreading/appsettings.json#L1-L200)).
Key settings (class `ApplicationConfiguration`): [MultiThreading/Configuration/ApplicationConfiguration.cs](MultiThreading/Configuration/ApplicationConfiguration.cs#L1-L200)

- `OutputDir` — output directory for saved files (default `output`).
- `MaxDepth` — maximum recursion depth.
- `MaxConcurrency` — number of worker tasks.
- `MaxLinks` — maximum queue size for links.
- `DownloadMode` — host-matching mode (`domain`, `subDomains`, `any`).

Robots.txt settings (class `RobotsConfiguration`): [MultiThreading/Configuration/RobotsConfiguration.cs](MultiThreading/Configuration/RobotsConfiguration.cs#L1-L50)

- `UserAgent` — user agent used when fetching `robots.txt`.

Key components

- `IWebDownloader` / `WebDownloader` — main crawler/downloader that manages visited URLs, a bounded channel and a pool of worker tasks. See [MultiThreading/Service/WebDownloader.cs](MultiThreading/Service/WebDownloader.cs#L1-L200).
- `IRobotsTxtService` / `RobotsTxtService` — fetches and parses `robots.txt`, with caching. See [MultiThreading/Service/RobotsTxtService.cs](MultiThreading/Service/RobotsTxtService.cs#L1-L200).
- `IRobotsTxtParser` — parser interface for `robots.txt` content.

### Output

Downloaded files are stored under `output/<host>/...`. HTML pages are saved as index files or with `.html` extension; binary assets are saved with extensions inferred from `Content-Type` where possible.

### Shutdown and cancellation

The application handles `CTRL+C` (cancellation) and stops workers gracefully.

### Development and tests

Unit tests live in `MultiThreading.UnitTests` (run with `dotnet test`).