#!/bin/sh
#
# Build a Docker image to build and run the BioMap app.
#
docker container stop biomap_run
docker build -t biomap .
docker run -d --restart unless-stopped  -p 5010:80 -p5011:22 --mount type=bind,source="/var/www/data",target=/var/www/data --name biomap_run biomap
