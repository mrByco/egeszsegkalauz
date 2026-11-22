# syntax=docker/dockerfile:1
FROM node:24-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .

ARG API_URL=http://localhost:5130
ARG PRODUCTION=true
RUN printf "export const environment = {\n  production: %s,\n  backendUrl: '%s',\n};\n" \
      "$PRODUCTION" "$API_URL" > src/environments/environment.ts

RUN npm run build

FROM nginx:alpine
COPY --from=build /app/dist/client/browser /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
