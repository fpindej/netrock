# Favicons and SEO Fundamentals

**Date**: 2026-03-14
**Scope**: Update favicon assets to brand colors and add SEO fundamentals (Open Graph, Twitter Cards, sitemap, canonical URLs, robots.txt)

## Summary

Updated all 6 favicon files to match the brand palette (cyan on dark background, consistent with netrock.dev). Added baseline SEO fundamentals to the frontend template: Open Graph and Twitter Card meta tags, canonical URLs, a dynamic sitemap.xml endpoint, and a server-generated robots.txt with crawler restrictions for auth-protected routes. Added a Frontend SEO checklist to the before-you-ship docs.

## Changes Made

| File | Change | Reason |
|------|--------|--------|
| `src/frontend/static/favicon-*.png` | Updated 16x16, 32x32 favicon PNGs | Match brand colors (cyan on dark) |
| `src/frontend/static/favicon.ico` | Updated ICO with new brand colors | Match brand colors |
| `src/frontend/static/apple-touch-icon.png` | Updated 180x180 touch icon | Match brand colors |
| `src/frontend/static/android-chrome-*.png` | Updated 192x192 and 512x512 chrome icons | Match brand colors |
| `src/frontend/static/site.webmanifest` | Changed `theme_color` and `background_color` from `#ffffff` to `#1b1917` | Match the dark mode warm charcoal background |
| `src/frontend/static/robots.txt` | Deleted static file | Replaced with server route for dynamic origin |
| `src/frontend/src/routes/robots.txt/+server.ts` | New server endpoint generating robots.txt | Allows absolute Sitemap URL using request origin; restricts `/api/`, `/admin/`, and auth-protected paths |
| `src/frontend/src/routes/sitemap.xml/+server.ts` | New server endpoint generating sitemap XML | Extensible public routes array; uses request origin for `<loc>` elements |
| `src/frontend/src/routes/+layout.svelte` | Added OG, Twitter Card, and canonical meta tags | Baseline SEO for all pages using existing i18n values |
| `docs/before-you-ship.md` | Added Frontend SEO checklist section | Guide template users to customize meta tags, OG image, sitemap routes, and webmanifest |

## Decisions & Reasoning

### Server-generated robots.txt and sitemap.xml instead of static files

- **Choice**: SvelteKit server endpoints (`+server.ts`) instead of static files in `static/`
- **Alternatives considered**: Static files with placeholder URLs; relative Sitemap URL in robots.txt
- **Reasoning**: The Sitemap directive in robots.txt requires an absolute URL per RFC 9309. A static file cannot know the deployment domain at build time. Server routes read `url.origin` from the request, producing correct absolute URLs automatically regardless of deployment target.

### OG defaults in root layout only (no per-page overrides)

- **Choice**: Set OG/Twitter meta tags in the root layout using `app_name` and `meta_description` i18n keys
- **Alternatives considered**: Adding per-page OG overrides to all 17 page files; creating a reusable SEO component
- **Reasoning**: This is a template. The app pages are behind auth (crawlers won't reach them), and the public auth pages (login, register) are not content pages users want ranked. The root layout defaults are a correct baseline. Users add per-page overrides as they build public marketing pages. The before-you-ship checklist guides them.

### Webmanifest colors set to dark theme

- **Choice**: `#1b1917` (warm charcoal) for both `theme_color` and `background_color`
- **Alternatives considered**: Keep `#ffffff`; use cyan brand accent; use different light/dark values
- **Reasoning**: The favicons use cyan on dark background. The template's default dark mode uses HSL `24 8% 10%` which converts to approximately `#1b1917`. Using the dark background for the manifest matches the favicon aesthetic and provides a cohesive PWA experience.

## Follow-Up Items

- [ ] Users should add an `og:image` meta tag with a 1200x630px preview image for social sharing (noted in before-you-ship checklist)
- [ ] Users should update `name`/`short_name` in `site.webmanifest` and i18n `app_name`/`meta_description` keys for their product
