FROM node:lts AS build
ARG GHS_VERSION=v0.95.7
RUN mkdir /app
WORKDIR /app
# Take the version passsed in for the server (which might lag behind UI bug fixes) and parse the major and minor version
# to get the latest patch version for the UI.
RUN export MAJORMINOR=$(echo $GHS_VERSION | cut --delimiter='.' --fields=1,2) && \
    export PATCH=$(git -c 'versionsort.suffix=-' ls-remote --tags --exit-code --sort='v:refname' https://github.com/Lurkars/gloomhavensecretariat.git "*${MAJORMINOR}*" | tail --lines=1 | cut --delimiter='/' --fields=3) && \
    git clone -b $PATCH --depth=1 --filter=tree:0 https://github.com/Lurkars/gloomhavensecretariat.git .
RUN --mount=type=cache,target=/app/node_modules npm install
RUN --mount=type=cache,target=/app/node_modules npm run build

FROM scratch AS dist-out
COPY --link --from=build /app/dist/gloomhavensecretariat /