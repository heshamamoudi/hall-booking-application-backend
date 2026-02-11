-- EMERGENCY FIX: Manual PostgreSQL Migration
-- Run this directly in Railway PostgreSQL to bypass EF Core migration issues
-- This creates the tables with correct PostgreSQL types

-- Step 1: Create ChatMessageReadStatuses table (if not exists)
CREATE TABLE IF NOT EXISTS "ChatMessageReadStatuses" (
    "Id" serial PRIMARY KEY,
    "MessageId" int NOT NULL,
    "UserId" int NOT NULL,
    "IsRead" boolean NOT NULL DEFAULT false,
    "ReadAt" timestamp NULL,
    CONSTRAINT "FK_ChatMessageReadStatuses_ChatMessages_MessageId"
        FOREIGN KEY ("MessageId") REFERENCES "ChatMessages" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ChatMessageReadStatuses_Users_UserId"
        FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

-- Step 2: Create indexes
CREATE UNIQUE INDEX IF NOT EXISTS "IX_ChatMessageReadStatuses_MessageId_UserId"
    ON "ChatMessageReadStatuses" ("MessageId", "UserId");
CREATE INDEX IF NOT EXISTS "IX_ChatMessageReadStatuses_UserId_IsRead"
    ON "ChatMessageReadStatuses" ("UserId", "IsRead");

-- Step 3: Create DataProtectionKeys table (if not exists)
CREATE TABLE IF NOT EXISTS "DataProtectionKeys" (
    "Id" serial PRIMARY KEY,
    "FriendlyName" text NULL,
    "Xml" text NULL
);

-- Step 4: Mark migrations as applied (prevents EF Core from trying to run them again)
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260210192956_AddChatMessageReadStatus', '8.0.11')
ON CONFLICT ("MigrationId") DO NOTHING;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260211210624_AddDataProtectionKeys', '8.0.11')
ON CONFLICT ("MigrationId") DO NOTHING;

-- Verification queries
SELECT 'ChatMessageReadStatuses table exists' as status, EXISTS (
    SELECT FROM information_schema.tables
    WHERE table_name = 'ChatMessageReadStatuses'
) as exists;

SELECT 'DataProtectionKeys table exists' as status, EXISTS (
    SELECT FROM information_schema.tables
    WHERE table_name = 'DataProtectionKeys'
) as exists;

SELECT 'Migrations marked as applied' as status, COUNT(*) as count
FROM "__EFMigrationsHistory"
WHERE "MigrationId" IN ('20260210192956_AddChatMessageReadStatus', '20260211210624_AddDataProtectionKeys');
