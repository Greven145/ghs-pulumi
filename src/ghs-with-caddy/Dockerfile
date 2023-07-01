FROM gloomhavensecretariat/ghs-server:latest as ghs
FROM caddy:latest
ENV HOST_NAME=ghs-server.net

COPY entrypoint.sh /work/
COPY --from=ghs /root/ghs-server*.jar /root/
RUN apk --no-cache add openjdk11 --repository=http://dl-cdn.alpinelinux.org/alpine/edge/community &&\
    chmod +x /work/entrypoint.sh
WORKDIR /work
ENTRYPOINT ./entrypoint.sh
VOLUME /root/.ghs
