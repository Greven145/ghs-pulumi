ARG GHS_VERSION=latest
FROM gloomhavensecretariat/ghs-server:$GHS_VERSION as ghs
FROM caddy:latest
ENV HOST_NAME=ghs-server.net

COPY entrypoint.sh /work/
COPY --from=ghs /root/ghs-server*.jar /root/
RUN apk --no-cache add openjdk17 --repository=http://dl-cdn.alpinelinux.org/alpine/edge/community &&\
    chmod +x /work/entrypoint.sh
WORKDIR /work
ENTRYPOINT ./entrypoint.sh
VOLUME /root/.ghs
