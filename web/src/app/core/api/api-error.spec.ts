import { HttpErrorResponse } from '@angular/common/http';
import { errorCode, toMessage } from './api-error';

function problem(status: number, title: string): HttpErrorResponse {
  return new HttpErrorResponse({ status, error: { title, detail: 'English detail' } });
}

describe('toMessage', () => {
  it('reports a network failure when the status is 0', () => {
    const error = new HttpErrorResponse({ status: 0 });
    expect(toMessage(error)).toContain('Không kết nối được máy chủ');
  });

  it('translates a known error code instead of showing the English detail', () => {
    expect(toMessage(problem(401, 'Auth.InvalidCredentials'))).toBe(
      'Email hoặc mật khẩu không đúng.',
    );
  });

  it('falls back for an error code that has no translation', () => {
    expect(toMessage(problem(500, 'Unknown.Code'))).toBe('Có lỗi xảy ra. Vui lòng thử lại.');
  });

  it('falls back when the API sends no body', () => {
    const error = new HttpErrorResponse({ status: 500, error: null });
    expect(toMessage(error)).toBe('Có lỗi xảy ra. Vui lòng thử lại.');
  });

  it('falls back for values that are not HTTP errors', () => {
    expect(toMessage(new Error('boom'))).toBe('Có lỗi xảy ra. Vui lòng thử lại.');
  });
});

describe('errorCode', () => {
  it('returns the ProblemDetails title', () => {
    expect(errorCode(problem(409, 'Customer.TaxCodeExists'))).toBe('Customer.TaxCodeExists');
  });

  it('returns null when there is no code', () => {
    expect(errorCode(new Error('boom'))).toBeNull();
    expect(errorCode(new HttpErrorResponse({ status: 500 }))).toBeNull();
  });
});
