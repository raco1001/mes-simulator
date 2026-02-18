# Frontend - build stage
FROM node:24-alpine AS build
WORKDIR /app

# Build-time API URL (브라우저가 접근할 백엔드 주소)
ARG VITE_API_BASE_URL=http://localhost:5000
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL

COPY package.json ./
RUN npm install

COPY . .
RUN npm run build

# Frontend - serve stage (nginx)
FROM nginx:alpine AS runtime
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
