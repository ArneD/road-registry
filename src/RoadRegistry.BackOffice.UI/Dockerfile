FROM nginx:stable-alpine as production-stage

LABEL maintainer "Digitaal Vlaanderen <digitaal.vlaanderen@vlaanderen.be>"

ARG build_number
RUN echo $build_number
ENV API_VERSION=$build_number

COPY default.conf.template /etc/nginx/conf.d/default.conf.template
COPY /dist /usr/share/nginx/html
COPY init.sh ./init.sh

RUN chmod +x ./init.sh

EXPOSE 10007

ENTRYPOINT ["./init.sh"]