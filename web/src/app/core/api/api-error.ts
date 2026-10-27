import { HttpErrorResponse } from '@angular/common/http';
import { ERROR_MESSAGES } from './error-messages';

const NETWORK_MESSAGE = 'Không kết nối được máy chủ. Kiểm tra API đã chạy chưa.';
const FALLBACK_MESSAGE = 'Có lỗi xảy ra. Vui lòng thử lại.';

interface ProblemDetails {
  title?: string;
}

/** The error code the API put in the ProblemDetails `title`, or null when there is none. */
export function errorCode(error: unknown): string | null {
  if (!(error instanceof HttpErrorResponse)) {
    return null;
  }
  const problem = error.error as ProblemDetails | null;
  return problem?.title ?? null;
}

export function toMessage(error: unknown): string {
  if (error instanceof HttpErrorResponse && error.status === 0) {
    return NETWORK_MESSAGE;
  }
  const code = errorCode(error);
  return (code && ERROR_MESSAGES[code]) ?? FALLBACK_MESSAGE;
}
