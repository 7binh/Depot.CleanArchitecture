/**
 * Vietnamese text for the error codes the API sends in the ProblemDetails `title`.
 * The API `detail` is English, so the screens show this text instead.
 */
export const ERROR_MESSAGES: Readonly<Record<string, string>> = {
  'Auth.InvalidCredentials': 'Email hoặc mật khẩu không đúng.',
  'Auth.InvalidRefreshToken': 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.',

  'Code.Empty': 'Mã không được để trống.',
  'Code.InvalidLength': 'Mã không đúng độ dài quy định.',
  'Code.TooShort': 'Mã quá ngắn.',
  'Code.TooLong': 'Mã quá dài.',
  'Name.Empty': 'Tên không được để trống.',
  'Name.TooLong': 'Tên quá dài.',
  'TaxCode.Empty': 'Mã số thuế không được để trống.',
  'TaxCode.Invalid': 'Mã số thuế phải gồm đúng 10 hoặc 13 chữ số.',

  'LineOperator.CodeExists': 'Mã hãng khai thác này đã tồn tại.',
  'LineOperator.NotFound': 'Không tìm thấy hãng khai thác.',
  'ContainerType.CodeExists': 'Mã loại container này đã tồn tại.',
  'ContainerType.NotFound': 'Không tìm thấy loại container.',
  'Customer.TaxCodeExists': 'Mã số thuế này đã có khách hàng sử dụng.',
  'Customer.NotFound': 'Không tìm thấy khách hàng.',
};
