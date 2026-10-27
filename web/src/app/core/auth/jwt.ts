const EMAIL_CLAIMS = [
  'email',
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress',
];

function decodeBase64Url(part: string): string {
  const base64 = part.replaceAll('-', '+').replaceAll('_', '/');
  return atob(base64.padEnd(Math.ceil(base64.length / 4) * 4, '='));
}

/** Reads the email claim from a JWT without verifying it; the API does the verifying. */
export function emailFromToken(token: string): string | null {
  try {
    const payload = JSON.parse(decodeBase64Url(token.split('.')[1] ?? '')) as Record<
      string,
      unknown
    >;
    const email = EMAIL_CLAIMS.map((claim) => payload[claim]).find(
      (value) => typeof value === 'string',
    );
    return (email as string | undefined) ?? null;
  } catch {
    return null;
  }
}
