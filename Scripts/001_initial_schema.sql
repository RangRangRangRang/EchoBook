-- EchoBook initial schema (PostgreSQL)
-- This mirrors what `dotnet ef database update` will create from the EF Core model.
-- You do NOT need to run this manually if you use EF Core migrations (recommended) -
-- it's provided per spec for reference / manual setup on hosts without the dotnet-ef tool.

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

CREATE TABLE "RecoveryKeys" (
    "Id" uuid PRIMARY KEY,
    "Code" varchar(20) NOT NULL,
    "CreatedAtUtc" timestamp NOT NULL,
    "LastAccessedAtUtc" timestamp NOT NULL
);
CREATE UNIQUE INDEX "IX_RecoveryKeys_Code" ON "RecoveryKeys" ("Code");

CREATE TABLE "Books" (
    "Id" uuid PRIMARY KEY,
    "RecoveryKeyId" uuid NOT NULL REFERENCES "RecoveryKeys"("Id") ON DELETE CASCADE,
    "Title" varchar(500) NOT NULL,
    "Author" varchar(500) NULL,
    "EpubFilePath" varchar(1000) NOT NULL,
    "CoverImagePath" varchar(1000) NULL,
    "FileSizeBytes" bigint NOT NULL,
    "UploadedAtUtc" timestamp NOT NULL
);
CREATE INDEX "IX_Books_RecoveryKeyId" ON "Books" ("RecoveryKeyId");

CREATE TABLE "Chapters" (
    "Id" uuid PRIMARY KEY,
    "BookId" uuid NOT NULL REFERENCES "Books"("Id") ON DELETE CASCADE,
    "Order" integer NOT NULL,
    "Title" varchar(500) NOT NULL,
    "EpubItemHref" varchar(1000) NOT NULL
);
CREATE INDEX "IX_Chapters_BookId_Order" ON "Chapters" ("BookId", "Order");

CREATE TABLE "Bookmarks" (
    "Id" uuid PRIMARY KEY,
    "BookId" uuid NOT NULL REFERENCES "Books"("Id") ON DELETE CASCADE,
    "ChapterId" uuid NOT NULL REFERENCES "Chapters"("Id") ON DELETE RESTRICT,
    "PageNumber" integer NOT NULL,
    "LinesPerPage" integer NOT NULL,
    "PreviewText" varchar(300) NULL,
    "CreatedAtUtc" timestamp NOT NULL
);

CREATE TABLE "ReadingProgresses" (
    "BookId" uuid PRIMARY KEY REFERENCES "Books"("Id") ON DELETE CASCADE,
    "CurrentChapterId" uuid NULL,
    "CurrentPage" integer NOT NULL,
    "CurrentScrollOffset" integer NOT NULL,
    "LinesPerPage" integer NOT NULL DEFAULT 25,
    "SelectedVoice" varchar(100) NULL,
    "ReadingSpeed" double precision NOT NULL DEFAULT 1.0,
    "UpdatedAtUtc" timestamp NOT NULL
);

CREATE TABLE "AudioCaches" (
    "Id" uuid PRIMARY KEY,
    "ChunkHash" varchar(64) NOT NULL,
    "Voice" varchar(100) NOT NULL,
    "Speed" double precision NOT NULL,
    "AudioFilePath" varchar(1000) NOT NULL,
    "CreatedAtUtc" timestamp NOT NULL
);
CREATE UNIQUE INDEX "IX_AudioCaches_Hash_Voice_Speed" ON "AudioCaches" ("ChunkHash", "Voice", "Speed");

CREATE TABLE "Settings" (
    "RecoveryKeyId" uuid PRIMARY KEY REFERENCES "RecoveryKeys"("Id") ON DELETE CASCADE,
    "DarkMode" boolean NOT NULL DEFAULT true,
    "Language" varchar(20) NOT NULL DEFAULT 'en',
    "Font" varchar(100) NOT NULL DEFAULT 'Georgia, serif',
    "FontSize" integer NOT NULL DEFAULT 18,
    "LineHeight" double precision NOT NULL DEFAULT 1.6,
    "LetterSpacing" double precision NOT NULL DEFAULT 0.0,
    "AiVoice" varchar(100) NULL,
    "ReadingSpeed" double precision NOT NULL DEFAULT 1.0,
    "LinesPerPage" integer NOT NULL DEFAULT 25
);
