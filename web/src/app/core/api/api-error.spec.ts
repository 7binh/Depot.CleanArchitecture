import { HttpErrorResponse } from '@angular/common/http';
import { toMessage } from './api-error';

describe('toMessage', () => {
  it('reports a network failure when the status is 0', () => {
    const error = new HttpErrorResponse({ status: 0 });
    expect(toMessage(error)).toContain('Không kết nối được máy chủ');
  });

  it('uses the detail sent by the API', () => {
    const error = new HttpErrorResponse({
      status: 401,
      error: { detail: 'Email hoặc mật khẩu không đúng.' },
    });
    expect(toMessage(error)).toBe('Email hoặc mật khẩu không đúng.');
  });

  it('falls back when the API sends no detail', () => {
    const error = new HttpErrorResponse({ status: 500, error: null });
    expect(toMessage(error)).toBe('Có lỗi xảy ra. Vui lòng thử lại.');
  });

  it('falls back for values that are not HTTP errors', () => {
    expect(toMessage(new Error('boom'))).toBe('Có lỗi xảy ra. Vui lòng thử lại.');
  });
});
