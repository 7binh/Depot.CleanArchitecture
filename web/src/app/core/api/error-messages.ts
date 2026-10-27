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

  'Number.Empty': 'Số container không được để trống.',
  'ContainerNumber.InvalidFormat':
    'Số container gồm 3 chữ cái hãng, 1 chữ cái U, J hoặc Z và 7 chữ số.',
  'ContainerNumber.InvalidCheckDigit': 'Số container sai chữ số kiểm tra (ISO 6346).',
  'ContainerNumber.OwnerMismatch': 'Ba chữ cái đầu không khớp mã hãng khai thác.',
  'IsoCode.Empty': 'Mã ISO không được để trống.',
  'IsoCode.Invalid': 'Mã ISO gồm đúng 4 chữ cái hoặc chữ số.',
  'SizeFeet.Invalid': 'Cỡ container chỉ có thể là 20 hoặc 40 feet.',
  'MaxWeightKg.Invalid': 'Tải trọng tối đa phải lớn hơn 0.',
  'TareWeightKg.Invalid': 'Trọng lượng vỏ phải lớn hơn 0.',
  'TareWeightKg.ExceedsMax': 'Trọng lượng vỏ không được lớn hơn tải trọng tối đa.',
  'ManufactureDate.InFuture': 'Ngày sản xuất không được ở tương lai.',
  'Container.NumberExists': 'Số container này đã tồn tại.',
  'Container.OperatorNotFound': 'Ba chữ cái đầu không thuộc hãng khai thác nào.',
  'Container.TypeNotFound': 'Không tìm thấy loại container đã chọn.',
  'Container.NotFound': 'Không tìm thấy container.',

  'LineOperator.CodeExists': 'Mã hãng khai thác này đã tồn tại.',
  'LineOperator.NotFound': 'Không tìm thấy hãng khai thác.',
  'ContainerType.CodeExists': 'Mã loại container này đã tồn tại.',
  'ContainerType.NotFound': 'Không tìm thấy loại container.',
  'Customer.TaxCodeExists': 'Mã số thuế này đã có khách hàng sử dụng.',
  'Customer.NotFound': 'Không tìm thấy khách hàng.',
};
