# EchoBook

Reading first. Everything else is secondary.

## What's built so far (Milestones 1–14 — feature-complete)

- Project structure: Controllers / Services / Repositories / ViewModels, clean DI
- Full database schema (RecoveryKeys, Books, Chapters, Bookmarks, ReadingProgress, AudioCache, Settings)
- Recovery Key system (passwordless, no accounts) — generation, validation, cookie-based session
- EPUB upload with real parsing (title, author, cover, chapter/TOC extraction) via VersOne.Epub
- Library grid (cover, title, delete) with black placeholder covers when no cover exists
- Three-column Reader UI (chapters/bookmarks sidebar, center reader, settings sidebar)
- Custom line-count-driven pagination (CSS multi-column layout measured/paged in JS — not epub.js's
  built-in pagination), fully repaginating when lines-per-page or typography settings change
- Keyboard shortcuts: Space/→ next page, ← previous page, ↑/↓ lines per page, B bookmark, Esc hide UI, F fullscreen
- Reader settings (dark mode, language, font, font size, line height, letter spacing, AI voice, reading
  speed) — applied live and persisted per recovery key
- Reading progress auto-saved and restored (book, chapter, page, lines-per-page, voice, speed)
- Bookmarks: add from the reader, list/jump/delete from the left sidebar
- Distraction-free auto-hide of both sidebars (and cursor) after ~2.5s of inactivity
- **AI text-to-speech** via a native .NET WebSocket client speaking Microsoft Edge's free "Read Aloud"
  protocol directly (no Python sidecar, no API key) — synthesizes the current page's text sentence by
  sentence, caches every clip on disk keyed by a text+voice+speed hash (`AudioCache` table), and plays
  them back to back, auto-continuing into the next page and the next chapter at the end of the book
- Sentence-level highlighting that tracks whichever chunk is currently being narrated
- Minimal audio controls (Previous / Play-Pause / Next / Speed) — no progress bar, no timestamps, per spec
- **UI polish**: favicon, page fade-ins, visible keyboard focus states, a drag-and-drop upload
  zone with a parsing spinner, a first-load overlay on the reader (no flash of unpaginated
  content), click-to-copy on the recovery key badge, and a real 404 page (not just the generic
  error template)
- **Deployment config**: multi-stage `Dockerfile`, `docker-compose.yml` for local container
  testing against Postgres, and a `render.yaml` Blueprint for one-click deploy to Render's free
  tier — see "Deployment" below

**Known simplifications:**
- Chapter HTML is extracted and sanitized server-side (via VersOne.Epub) rather than rendered through
  epub.js in the browser. Embedded per-book stylesheets are intentionally dropped in favor of
  EchoBook's own consistent reader typography — the same choice Kindle and Apple Books make.
- The edge-tts integration talks to an **undocumented** Microsoft endpoint (the same one the popular
  Python `edge-tts` package and Edge's own Read Aloud feature use). It requires no API key, which is
  exactly why Microsoft could change it without notice. If narration stops working, check
  `Services/EdgeTtsClient.cs` first — the `Sec-MS-GEC` token algorithm or endpoint URL are the most
  likely things to drift.
- "Current visible page" text (for TTS and progress) is derived by measuring which paginated CSS
  column each paragraph/heading currently falls into. It's accurate for normal prose; an element that
  straddles a page boundary is counted as belonging to whichever column its top-left corner lands in.

## Requirements

- .NET 8 SDK
- PostgreSQL (local install, or a free-tier host: Supabase / Neon / Railway / Render)
- EF Core CLI tool: `dotnet tool install --global dotnet-ef` (only needed once)

## Setup

1. **Point the app at your database.** Edit `EchoBook/appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Port=5432;Database=echobook;Username=postgres;Password=yourpassword"
   }
   ```
   (For local dev, prefer `dotnet user-secrets set "ConnectionStrings:DefaultConnection" "..."` instead
   of editing the file, so you don't commit credentials.)

2. **Restore packages and create the initial migration:**
   ```bash
   cd EchoBook
   dotnet restore
   dotnet ef migrations add InitialCreate
   ```
   This generates `Migrations/` from the model in `Data/AppDbContext.cs`. You don't need to run
   `dotnet ef database update` manually — the app calls `db.Database.Migrate()` on startup and will
   create the schema automatically the first time it runs. (A hand-written equivalent schema is also
   included at `Scripts/001_initial_schema.sql` if you'd rather apply it directly with `psql`.)

3. **Run it:**
   ```bash
   dotnet run
   ```
   Then open the URL printed in the console (typically `https://localhost:5001`).

## Deployment

**Before deploying by any method below**, generate and commit the initial migration locally first
(see step 2 under Setup: `dotnet ef migrations add InitialCreate`). `db.Database.Migrate()` on
startup only applies migrations that are already compiled into the app — it can't invent one from
nothing, so an image built before a migration exists will start successfully but leave you with an
empty (non-existent) schema. If you'd rather not deal with the EF CLI at deploy time, apply
`Scripts/001_initial_schema.sql` directly against the target database with `psql` instead and skip
`dotnet ef migrations add` — either approach gets you a working schema.

### Option A — Docker Compose (local container test, or any host that runs Compose)

```bash
docker compose up --build
```

This builds the app image from the `Dockerfile`, starts a local Postgres 16 container, and runs
the app on `http://localhost:8080`. Uploaded epubs/covers and the Postgres data directory are kept
in named Docker volumes (`echobook-uploads`, `echobook-db-data`) so they survive `docker compose
down` / restarts. Migrations are applied automatically on startup, same as `dotnet run`.

### Option B — Render (free tier)

1. Push this repo to GitHub.
2. In Render, choose **New > Blueprint** and point it at the repo — it will read `render.yaml` and
   provision both the `echobook` web service (built from the `Dockerfile`) and a free `echobook-db`
   Postgres instance, wiring `DATABASE_URL` from the database into the web service automatically.
3. Render builds and deploys. `Program.cs` detects `DATABASE_URL` and translates it into the Npgsql
   connection string itself — no manual connection-string configuration needed.

**Free-tier storage caveat:** Render's free web service plan has an ephemeral filesystem — it
doesn't support the persistent disks needed to keep `Uploads/` (epub files + covers) across
restarts and redeploys. `AudioCache/` losing its contents is harmless (it's just a cache, keyed by
text+voice+speed hash — the app regenerates a clip on next play), but losing `Uploads/` means
losing the actual uploaded books. If that's not acceptable, either:
- upgrade the web service to a paid Render plan and uncomment the `disk` block in `render.yaml`, or
- point `Storage:UploadsPath` at an external object store (S3-compatible bucket, etc.) — not wired
  up here, since it wasn't in the original spec, but `FileStorageService` is the one place that
  would need to change.

The Postgres database itself is unaffected by any of this either way — Render's managed Postgres
(even on the free plan) has its own persistent disk.

### Other hosts (Railway, Supabase, Neon, etc.)

The `Dockerfile` and the `DATABASE_URL` auto-detection in `Program.cs` work the same way on
Railway or any other host that (a) can build/run a Docker image and (b) hands out a Postgres
connection as a `DATABASE_URL`-style URI. Set `DATABASE_URL` (or `ConnectionStrings__DefaultConnection`
directly, in standard Npgsql keyword format, if a host gives you one instead) and `PORT` if the
platform requires binding to a specific port; both are read automatically at startup.

## Notes on package versions

`VersOne.Epub` is pinned to `3.3.4` in the `.csproj`. If NuGet reports that version doesn't exist by
the time you build, run `dotnet add package VersOne.Epub` to pull latest and paste me any compiler
errors — the API surface (`EpubReader.ReadBookAsync`, `book.Navigation`, `book.ReadingOrder`) has been
stable across recent 3.x releases, but I can patch fast if something shifted.

## Project layout

```
EchoBook/
  Controllers/     Thin controllers, no business logic
  Services/        Business logic (recovery keys, epub parsing, file storage, book orchestration)
  Repositories/     EF Core data access, one per aggregate
  Models/           EF Core entities
  ViewModels/       Data shaped for views / parsing results
  Data/             AppDbContext
  Views/            Razor views (Bootstrap 5, dark theme)
  wwwroot/          CSS/JS
  Uploads/          Uploaded epubs + extracted covers (gitignored, created at runtime)
  AudioCache/       Generated TTS mp3s (gitignored, created at runtime)
Scripts/
  001_initial_schema.sql   Manual/reference PostgreSQL schema
Dockerfile              Multi-stage build (SDK -> ASP.NET runtime image)
docker-compose.yml       App + Postgres, for local container testing
render.yaml              Render Blueprint (web service + free Postgres)
.dockerignore
```
