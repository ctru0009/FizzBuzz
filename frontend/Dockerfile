FROM node:23-alpine AS builder

WORKDIR /app

COPY package*.json ./

RUN npm install

COPY . .

RUN ["npm", "run", "build"]

FROM node:23-alpine

# Set the working directory
WORKDIR /app

# Copy only the necessary files from the build stage
COPY --from=builder /app/package*.json ./
COPY --from=builder /app/.next ./.next
COPY --from=builder /app/public ./public
COPY --from=builder /app/next.config.ts ./
# Install production dependencies only
RUN npm install --production

# Expose port 3000
EXPOSE 3000

# Start the Next.js application
CMD ["npm", "start"]
