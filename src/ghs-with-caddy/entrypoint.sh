GHSFILE=/root/.ghs/application.properties
if [ ! -f "$GHSFILE" ]; then
    echo -e "\
server.port=8888\n\
ghs-server.lastestClientOnStartup=true" > $GHSFILE
fi

CADDYFILE=/etc/caddy/Caddyfile
echo -e "\
http://${HOST_NAME} {\n\
	redir https://{host}{uri}\n\
}\n\
https://${HOST_NAME} {\n\
	reverse_proxy /* 127.0.0.1:8888\n\
}" > $CADDYFILE

java -jar /root/ghs-server-*.jar -Djava.awt.headless=true &
caddy run --config /etc/caddy/Caddyfile --adapter caddyfile