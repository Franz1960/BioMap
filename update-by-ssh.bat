ssh itools.de -l fh "cd \"/var/www/docker/BioMap\" && sudo git pull && sudo chmod +x *.sh && sudo ./build-and-run.sh"
REM Mit folgendem Kommando können temporäre Container gelöscht werden:
REM docker rmi $(docker images --filter \"dangling=true\" -q --no-trunc)