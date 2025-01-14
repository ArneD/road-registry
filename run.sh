#!/usr/bin/env bash
set -e

docker-compose --project-name road-registry-tools \
    -f ./docker/compose/tools.yml \
    -f ./docker/compose/mssql.yml \
    up -d \
    --remove-orphans

docker-compose --project-name road-registry \
    -f ./docker/compose/backoffice.yml \
    -f ./docker/compose/municipality.yml \
    -f ./docker/compose/streetname.yml \
    -f ./docker/compose/legacy.yml \
    up -d \
    --remove-orphans

docker compose --project-name road-registry-hosts \
    -f ./docker/compose/projectionhost.yml \
    up -d \
    --remove-orphans
