FROM node:22-alpine AS base
WORKDIR /app
ENV PNPM_HOME="/pnpm"
ENV PATH="$PNPM_HOME:$PATH"
RUN corepack enable
COPY package.json pnpm-lock.yaml .npmrc ./

FROM base AS prod-deps
RUN --mount=type=cache,id=pnpm,target=/pnpm/store pnpm install --prod --frozen-lockfile

FROM base AS builder
RUN --mount=type=cache,id=pnpm,target=/pnpm/store pnpm install --frozen-lockfile
COPY . .

# SvelteKit $env/static/public vars are inlined at build time.
# Pass via: docker build --build-arg PUBLIC_TURNSTILE_SITE_KEY=...
ARG PUBLIC_TURNSTILE_SITE_KEY
ENV PUBLIC_TURNSTILE_SITE_KEY=${PUBLIC_TURNSTILE_SITE_KEY}

RUN pnpm run build

FROM node:22-alpine
WORKDIR /app
COPY --from=prod-deps /app/node_modules node_modules/
COPY --from=builder /app/build build/
COPY package.json .
EXPOSE 3000
ENV NODE_ENV=production
ENV PORT=3000
CMD [ "node", "build" ]
