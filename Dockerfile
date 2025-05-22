FROM node:18-alpine AS build

WORKDIR /app

# Copy package.json and package-lock.json
COPY package*.json ./

# Install dependencies
RUN npm ci

# Copy the rest of the app
COPY . .

# Build the app
RUN npm run build

# Stage 2: Serve the app with nginx
FROM nginx:alpine

# Copy the build output
COPY --from=build /app/build /usr/share/nginx/html

# Copy nginx configuration
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Create a script to dynamically update env variables at runtime
RUN apk add --no-cache bash
COPY ./env.sh /env.sh
RUN chmod +x /env.sh

# Copy the entrypoint script
COPY ./docker-entrypoint.sh /docker-entrypoint.sh
RUN chmod +x /docker-entrypoint.sh

# Create a default env-config.js file
RUN echo "window.ENV = {};" > /usr/share/nginx/html/env-config.js

# Expose port 80
EXPOSE 80

# Start with our custom entrypoint
CMD ["/docker-entrypoint.sh"] 