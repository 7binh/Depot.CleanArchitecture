import { HttpErrorResponse } from '@angular/common/http';

const NETWORK_MESSAGE = 'Không kết nối được máy chủ. Kiểm tra API đã chạy chưa.';
const FALLBACK_MESSAGE = 'Có lỗi xảy ra. Vui lòng thử lại.';

interface ProblemDetails {
  detail?: string;
}

export function toMessage(error: unknown): string {
  if (!(error instanceof HttpErrorResponse)) {
    return FALLBACK_MESSAGE;
  }
  if (error.status === 0) {
    return NETWORK_MESSAGE;
  }
  const problem = error.error as ProblemDetails | null;
  return problem?.detail ?? FALLBACK_MESSAGE;
}
