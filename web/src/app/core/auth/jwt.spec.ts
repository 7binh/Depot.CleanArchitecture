import { emailFromToken } from './jwt';

function tokenWith(payload: object): string {
  const body = btoa(JSON.stringify(payload)).replaceAll('+', '-').replaceAll('/', '_');
  return `header.${body.replaceAll('=', '')}.signature`;
}

describe('emailFromToken', () => {
  it('reads the short email claim', () => {
    expect(emailFromToken(tokenWith({ email: 'a@b.vn' }))).toBe('a@b.vn');
  });

  it('reads the long .NET email claim', () => {
    const claim = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress';
    expect(emailFromToken(tokenWith({ [claim]: 'c@d.vn' }))).toBe('c@d.vn');
  });

  it('returns null when there is no email claim', () => {
    expect(emailFromToken(tokenWith({ sub: '1' }))).toBeNull();
  });

  it('returns null for a malformed token', () => {
    expect(emailFromToken('not-a-token')).toBeNull();
  });
});
