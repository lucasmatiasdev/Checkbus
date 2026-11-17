#!/bin/sh
set -e

# Runs as root at container startup. A Railway Volume mounted on /app/App_Data/storage is
# created root-owned regardless of the chown baked into the image at build time (see
# Dockerfile), so it must be re-chowned here, every start, before dropping privileges to the
# non-root "app" user that ships in the .NET 8+ base images.
chown -R app:app /app/App_Data/storage

exec su app -s /bin/sh -c "exec dotnet Checkbus.ApiService.dll"
