-- Enables the pgvector extension on first container start.
-- Runs automatically via Docker's /docker-entrypoint-initdb.d/ mechanism.
CREATE EXTENSION IF NOT EXISTS vector;
