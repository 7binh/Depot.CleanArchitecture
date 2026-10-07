# Dev - Developer - New

## Tổng quan các lĩnh vực

Các phần kiến thức căn bản bắt buộc các bạn tự học.

| Lĩnh vực                                                                                                                                                                                                                                                                           | Nội dung tự học                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                              | Tham khảo                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                |
| ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Kiến thức chuyên môn căn bản**<br><br>- Standard code (code convention)<br>- Kiến thức về lập trình hướng đối tượng OOP<br>- Nguyên lý lập trình SOLID<br>- Git & Quản lý source code<br>- SQL Cơ bản<br>- Bảo mật cơ bản<br>- Design Pattern<br>- Angular<br>- Docker & Jenkins | **Kết quả:** hiểu và áp dụng được, sẵn sàng áp dụng cho dự án bên dưới.<br><br>- Code convention và cách sử dụng công cụ linting (ví dụ: StyleCop, SonarLint).<br>- OOP cơ bản (class, object, inheritance, polymorphism).<br>- Nguyên lý SOLID và áp dụng thực tế qua các bài tập nhỏ.<br>&nbsp;&nbsp;- **Single Responsibility Principle (SRP):** Mỗi class chỉ nên có một lý do để thay đổi.<br>&nbsp;&nbsp;- **Open/Closed Principle (OCP):** Mở rộng chức năng mà không sửa đổi mã nguồn.<br>&nbsp;&nbsp;- **Liskov Substitution Principle (LSP):** Các đối tượng kiểu class con có thể thay thế hoàn toàn class cha mà không làm sai lệch tính đúng đắn của chương trình.<br>&nbsp;&nbsp;- **Interface Segregation Principle (ISP):** Nên chia nhỏ các interface thành nhiều interface chuyên biệt thay vì dùng một interface lớn chung chung.<br>&nbsp;&nbsp;- **Dependency Inversion Principle (DIP):** Các module cấp cao không nên phụ thuộc vào module cấp thấp; cả hai nên phụ thuộc vào abstraction (interface/abstract class).<br>- Biết cách sử dụng Git (branching, merging, pull request, pre-commit hooks).<br>- Biết cách sử dụng SQL cơ bản và ứng dụng.<br>&nbsp;&nbsp;- Câu lệnh SQL: SELECT, INSERT, UPDATE, DELETE, JOIN.<br>&nbsp;&nbsp;- Thiết kế bảng và quan hệ: Primary Key, Foreign Key.<br>&nbsp;&nbsp;- Truy vấn nâng cao: GROUP BY, HAVING, Window Functions.<br>- Secure coding for Developers with OWASP.<br>- Generic, Delegate/Action/Predicate/Func, Lambda, LINQ, Dependency Injection.<br>- Factory, Singleton, 3-Tier Architecture.<br>- CLI, Component, Service, gọi API.<br>- Lệnh cơ bản Docker/Docker Compose; tổng quan Jenkins, tạo Pipeline. | - C# Coding Standards and Naming Conventions<br>- C# identifier naming rules and conventions<br>- C# Coding Conventions<br>- C# OOP<br>- Object-Oriented Programming Concepts<br>- https://www.scholarhat.com/tutorial/designpatterns/solid-design-principles-explained<br>- https://medium.com/backticks-tildes/the-s-o-l-i-d-principles-in-pictures-b34ce2f1e898<br>- https://www.c-sharpcorner.com/UploadFile/yusufkaratoprak/difference-between-loose-coupling-and-tight-coupling/<br>- https://stackoverflow.com/questions/39946/coupling-and-cohesion<br>- https://enlabsoftware.com/development/how-to-apply-solid-principles-with-practical-examples-in-c-sharp.html<br>- https://funix.edu.vn/chia-se-kien-thuc/huong-dan-co-ban-ve-cach-thao-tac-voi-bang-trong-csdl-sql/ (Tham khảo full SQL Series)<br>- https://owasp.org/www-project-top-ten/<br>- Hướng dẫn thao tác với bảng trong CSDL SQL (full series)<br>- Các lệnh cơ bản thao tác với SQL<br>- Thực hành SQL online<br>- OWASP Top 10<br>- Lập trình C# cơ bản<br>- Tự học lập trình C#<br>- LINQ Samples<br>- Generic trong C#<br>- Dependency Injection trong ASP.NET Core<br>- Design Patterns bằng C# (ví dụ)<br>- Catalog đầy đủ các Design Pattern<br>- Unit of Work & Generic Repository Pattern<br>- Angular Official Documentation<br>- Tài liệu Docker, Jenkins thực tập |

---

# Dự án: Quản lý DEPOT container

## Nội dung áp dụng

1. Áp dụng **Standard code**.
2. Áp dụng và hiểu rõ tình huống áp dụng **OOP, SOLID**.
3. **C# cơ bản - nâng cao**
   - Generic, Partial, ExtensionMethod.
   - Delegate, Action, Predicate, Func.
   - Lambda expression, Linq.
   - Dependency Injection.
4. **ASP.NET Core**
   - ServiceCollection.
   - Configuration.
   - Middleware.
   - Controllers.
5. **ORM:** Entity Framework, Dapper, Protocol Buffers.
6. **Design pattern:** factory, singleton, 3-Tier Architecture.
7. **Cơ sở dữ liệu quan hệ:** Kiến thức về Database - CSDL (type/query/function/procedure/trigger/index).

### Tham khảo

- https://xuanthulab.net/lap-trinh-c-co-ban/
- https://tuhocict.com/huong-dan-tu-hoc-lap-trinh-c-sharp/
- https://linqsamples.com/
- https://toidicodedao.com/2015/03/05/series-c-hay-ho-generic-la-cai-thu-chi-chi/
- https://tedu.com.vn/lap-trinh-aspnet-core/co-che-dependency-injection-trong-aspnet-core-256.html
- https://github.com/nguyenphuc22/Design-Patterns
- https://refactoring.guru/design-patterns/catalog
- https://funix.edu.vn/chia-se-kien-thuc/huong-dan-co-ban-ve-cach-thao-tac-voi-bang-trong-csdl-sql/ (Tham khảo full SQL Series)
- https://funix.edu.vn/chia-se-kien-thuc/cac-lenh-co-ban-thao-tac-voi-sql/
- https://sqlfiddle.com/sql-server/online-compiler (CSDL Online)

## Phân tích & thiết kế

Xây dựng mô hình dữ liệu cho ứng dụng trên.

## SQL

Thống kê container đang tồn tại bãi theo từng hãng khai thác, theo thời gian tồn:

- 0-10 ngày.
- >= 10 ngày (tồn lâu).

## Thiết kế giao diện

Thiết kế giao diện (mockup hoặc prototype dùng figma, axure,...) cho các màn hình dự định thực hiện của ứng dụng.

## Xây dựng/lập trình ứng dụng

- Xây dựng API cho chức quản lý nhập/xuất container.
- Viết Unit test cho API với Coverage >= 80%.
- Xây dựng màn hình cho phép người dùng quản lý nhập/xuất container.
- Thống kê sản lượng xuất nhập của từng hãng (line operator) theo ngày.
- Thống kê container đang tồn tại bãi theo từng hãng, theo thời gian tồn (0-10 ngày, và >= 10 ngày (tồn lâu)).

---

# Lựa chọn công nghệ Backend

Thực tập sinh chọn chọn dự án dưới đây tùy theo mức độ nền tảng hiện tại, trao đổi với mentor trước khi bắt đầu dự án:

- **Lựa chọn 2 - .NET 10, Clean Architecture (theo mẫu Aspire).**  
  Dùng repo tham khảo **TechSpherex.CleanArchitecture** (Clean Architecture 4 layer, CQRS thủ công, Minimal API, .NET Aspire, HybridCache). Phù hợp nếu đã có nền tảng OOP/SOLID vững, muốn tiếp cận công nghệ mới ngay.

---

# Checklist kỹ thuật bắt buộc

Dùng để mentor và thực tập sinh tự đánh giá trước khi tham gia team dự án thực tế - tất cả các mục dưới đây là **bắt buộc tối thiểu**, không đạt thì chưa chuyển vào team.

| Nhóm           | Kỹ thuật bắt buộc                                                  |
| -------------- | ------------------------------------------------------------------ |
| **Nền tảng**   | OOP (4 tính chất), SOLID (ít nhất S và D), Git (branch/merge/PR)   |
| **Backend**    | DI, Middleware, 3-Tier Architecture, EF Core CRUD, LINQ cơ bản     |
| **API**        | Xây REST API, Swagger, test bằng Postman, Unit test cơ bản         |
| **Database**   | SQL JOIN, Primary/Foreign Key, viết được câu truy vấn thống kê     |
| **Frontend**   | Angular CLI, Component, Service, gọi API và hiển thị dữ liệu       |
| **Triển khai** | Docker cơ bản (Dockerfile, docker-compose), không hardcode secrets |
| **Bảo mật**    | Nhận biết OWASP Top 10 ở mức khái niệm                             |
| **Dự án**      | Dự án Depot chạy được end-to-end (FE + BE + DB), có README         |

## Không bắt buộc ở mốc 1 tháng

Bổ sung sau khi vào team:

- O/L/I của SOLID.
- Design Pattern nâng cao.
- Clean Architecture 4 layer đầy đủ.
- Dapper.
- gRPC.
- Jenkins/CI-CD.
- Nghiệp vụ chuyên sâu hệ thống Cảng.
