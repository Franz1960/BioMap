#!/bin/sh
#
# Build a Docker image to build and run the BioMap app.
#
docker build -t biomap .
docker run -it --rm -p 5010:80 --mount type=bind,source="/var/www/data",target=/var/www/data --name biomap_run biomap