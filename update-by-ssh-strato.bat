ssh itools.de -l fh "docker ps -aq -f name=biomap | (xargs docker stop || true) | (xargs docker rm || true) && (docker rmi franz1960/biomap || true) && docker run -d --restart unless-stopped -p 5010:80 -p5011:22 --mount type=bind,source=/var/www/data,target=/var/www/data --name biomap franz1960/biomap:latest"
pause
