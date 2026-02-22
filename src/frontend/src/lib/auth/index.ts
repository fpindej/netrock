export { getUser, logout } from './auth';
export type { GetUserResult } from './auth';
export { createAuthMiddleware } from './middleware';

/**
 * Must match the backend CookieNames.RefreshToken constant.
 *
 * The `__Secure-` prefix requires HTTPS per the cookie spec. Modern browsers
 * (Chrome, Firefox) accept it on `localhost` as a development exception, but
 * this is not spec-guaranteed. If the cookie is absent in local dev, the guard
 * degrades gracefully — users get a clean `/login` instead of "session expired."
 */
export const REFRESH_TOKEN_COOKIE = '__Secure-REFRESH-TOKEN';
