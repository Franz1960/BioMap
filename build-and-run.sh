#!/bin/sh
#
# Build a Docker image to build and run the BioMap app.
#
docker ps -aq -f name=biomap_run | (xargs docker stop || true) | (xargs docker rm || true) && (docker rmi biomap || true)
docker build -t biomap .
docker run -d --restart unless-stopped  -p 5010:80 -p5011:22 --mount type=bind,source="/var/www/data",target=/var/www/data --name biomap_run biomap
