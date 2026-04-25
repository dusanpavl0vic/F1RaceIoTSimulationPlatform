FROM node:20-alpine

WORKDIR /app

COPY ./telemetry-database-service/package.json ./
RUN npm install --omit=dev

COPY ./telemetry-database-service/src ./src

ENV NODE_ENV=production
ENV SERVICE_PORT=8090

EXPOSE 8090

CMD ["node", "src/index.js"]
