import { HttpErrorResponse } from '@angular/common/http';

// What to tell the user when a request fails.
//
// Two different things arrive with a 400. Our own services answer with
// { errors: [{ errorCode, errorMessage }] }, and ASP.NET's own model validation answers with
// { errors: { fieldName: ["..."] } }. Both mean the server read the request and refused it,
// so saying "check the API is running" is both wrong and unhelpful - it is running, it just
// told us which field was bad, and that message is the only thing that helps.
//
// Only a status of 0 (nothing answered at all) or a 5xx deserves the "is it running" line.
export function apiErrorMessage(error: HttpErrorResponse, fallback: string): string {
  const body = error?.error;

  // Our shape: a list of errors, each with its own message.
  if (Array.isArray(body?.errors)) {
    const detail = body.errors
      .map((e: { errorMessage?: string }) => e?.errorMessage)
      .filter(Boolean)
      .join(' ');

    if (detail) {
      return detail;
    }
  }

  // ASP.NET's shape: field name -> the things wrong with it.
  if (body?.errors && typeof body.errors === 'object') {
    const detail = Object.values(body.errors as Record<string, string[]>)
      .flat()
      .filter(Boolean)
      .join(' ');

    if (detail) {
      return detail;
    }
  }

  if (error?.status === 0 || error?.status >= 500) {
    return fallback + ' Check the API is running.';
  }

  return fallback;
}
