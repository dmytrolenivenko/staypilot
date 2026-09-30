/**
 * The locale this bundle was built for: 'en-GB' or 'pt-PT'.
 *
 * $localize.locale is only set by a localized build (ng build, or ng serve -c pt). A plain
 * `ng serve` falls back to the source locale, not the browser's, so numbers read the same as prod.
 */
export const APP_LOCALE: string = $localize.locale || 'en-GB';

export const IS_PORTUGUESE = APP_LOCALE.startsWith('pt');

/**
 * The same page in the other language. A full page load, not a router link: each language is
 * its own build, served from its own base path (/ and /pt/).
 */
export function otherLanguageUrl(routerUrl: string): string {
  return IS_PORTUGUESE ? routerUrl : `/pt${routerUrl}`;
}
