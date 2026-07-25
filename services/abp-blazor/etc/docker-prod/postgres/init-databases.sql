-- Created on first postgres volume init only (docker-entrypoint-initdb.d).
-- User comes from POSTGRES_USER; this script creates ABP microservice databases.

SELECT 'CREATE DATABASE hanhchinhso_Identity'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'hanhchinhso_Identity')\gexec

SELECT 'CREATE DATABASE hanhchinhso_Administration'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'hanhchinhso_Administration')\gexec

SELECT 'CREATE DATABASE hanhchinhso_BlobStoring'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'hanhchinhso_BlobStoring')\gexec

SELECT 'CREATE DATABASE hanhchinhso_AuditLogging'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'hanhchinhso_AuditLogging')\gexec

SELECT 'CREATE DATABASE hanhchinhso_Gdpr'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'hanhchinhso_Gdpr')\gexec

SELECT 'CREATE DATABASE hanhchinhso_Language'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'hanhchinhso_Language')\gexec

SELECT 'CREATE DATABASE hanhchinhso_AIManagement'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'hanhchinhso_AIManagement')\gexec

SELECT 'CREATE DATABASE hanhchinhso_Organization'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'hanhchinhso_Organization')\gexec

SELECT 'CREATE DATABASE hanhchinhso_Workflow'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'hanhchinhso_Workflow')\gexec
