# ECommerce API - ASP.NET Core 8 (.NET 8)

> **Backend RESTful API hoàn chỉnh theo kiến trúc Clean Architecture**, phục vụ phỏng vấn tuyển dụng vị trí .NET Backend Developer.

---

## 🌟 Tổng Quan Dự Án

Dự án mô phỏng hệ thống backend thương mại điện tử (E-Commerce) với đầy đủ nghiệp vụ:
- Quản lý danh mục sản phẩm (Categories)
- Quản lý sản phẩm (Products) kèm phân trang, tìm kiếm, lọc theo giá, danh mục, sắp xếp
- Xác thực & Phân quyền người dùng (JWT Authentication & Role-Based Authorization: `Admin`, `Customer`, `Manager`)
- Quản lý đơn hàng (Orders) với xử lý nghiệp vụ trừ tồn kho tự động, khôi phục tồn kho khi hủy đơn, và kiểm soát luồng trạng thái đơn hàng (State Machine).
- Lưu trữ dữ liệu In-Memory (sử dụng `ConcurrentDictionary` thread-safe) - **không cần cài đặt SQL Server hay database ngoại vi nào, clone về là chạy ngay!**

---

## 📐 Kiến Trúc Clean Architecture

```text
ECommerceAPI/
│
├── ECommerceAPI.Domain/                 # [Domain Layer] Core entities & abstractions
│   ├── Common/                          # BaseEntity (Id, CreatedAt, UpdatedAt, IsDeleted)
│   ├── Entities/                        # Category, Product, User, Order, OrderItem
│   ├── Enums/                           # OrderStatus, ProductStatus, UserRole
│   └── Interfaces/                      # IRepository<T>, IUnitOfWork, ISpecificRepositories
│
├── ECommerceAPI.Application/            # [Application Layer] Business logic & use cases
│   ├── Common/                          # ApiResponse<T>, PagedResult<T>, PaginationParams
│   ├── DTOs/                            # Request & Response Data Transfer Objects
│   ├── Interfaces/                      # IAuthService, IProductService, ICategoryService, IOrderService, IJwtService
│   ├── Services/                        # Business Service Implementations (Auth, Product, Category, Order)
│   └── DependencyInjection/             # ApplicationDI extension method
│
├── ECommerceAPI.Infrastructure/         # [Infrastructure Layer] Data access & external services
│   ├── Repositories/                    # InMemoryRepository<T>, InMemoryUnitOfWork, Specific Repositories
│   ├── Services/                        # JwtService (HMAC-SHA256, Claims)
│   └── DependencyInjection/             # InfrastructureDI & DataSeeder (Seed categories, products, users)
│
├── ECommerceAPI.API/                    # [Presentation Layer] HTTP Endpoints & Host
│   ├── Controllers/                     # AuthController, ProductsController, CategoriesController, OrdersController
│   ├── Middleware/                      # GlobalExceptionMiddleware (Error handling thống nhất)
│   ├── appsettings.json                 # JWT & application configuration
│   └── Program.cs                       # Middleware pipeline, DI setup, Swagger with JWT UI
│
├── test-requests.http                   # File test request sẵn cho VS Code / Visual Studio
├── ECommerceAPI.postman_collection.json # File import trực tiếp vào Postman
└── README.md
```

### Nguyên tắc thiết kế (Design Principles & Patterns)
1. **Clean Architecture (Onion/Hexagonal)**: Dependency hướng vào trong (`API` -> `Infrastructure` -> `Application` -> `Domain`). Domain không phụ thuộc vào bất kỳ thư viện ngoài nào.
2. **Repository Pattern & Unit of Work**: Tách biệt logic truy xuất dữ liệu khỏi business logic, cho phép hoán đổi In-Memory sang EF Core / SQL Server chỉ bằng cách đổi Infrastructure layer mà không sửa 1 dòng code Application/Domain nào.
3. **ApiResponse Wrapper**: Toàn bộ endpoint trả về format chuẩn `{ success: bool, message: string, data: T, errors: [] }`.
4. **Soft Delete**: Xóa sản phẩm và danh mục bằng cờ `IsDeleted` thay vì xóa vật lý khỏi bộ nhớ.
5. **Thread-Safe In-Memory Store**: Sử dụng `ConcurrentDictionary<Guid, T>` và `Interlocked.Increment` để đảm bảo an toàn đa luồng.

---

## 🚀 Hướng Dẫn Cài Đặt & Chạy

### Yêu cầu môi trường
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Cách chạy
Mở terminal tại thư mục chứa source:
```bash
cd ECommerceAPI
dotnet run --project ECommerceAPI.API
```

Sau khi chạy, API sẽ lắng nghe tại:
- **Swagger UI**: [http://localhost:5000](http://localhost:5000) (hoặc `http://localhost:5000/swagger`)
- **Swagger JSON**: [http://localhost:5000/swagger/v1/swagger.json](http://localhost:5000/swagger/v1/swagger.json)

---

## 👥 Tài Khoản Test Được Seed Sẵn

Khi khởi động, hệ thống tự động nạp sẵn dữ liệu demo:

| Role | Email | Password | Quyền hạn |
|---|---|---|---|
| **Admin** | `admin@ecommerce.com` | `Admin@123` | Toàn quyền: Thêm/Sửa/Xóa Product, Category, xem tất cả Order, đổi trạng thái Order |
| **Customer** | `customer@example.com` | `Customer@123` | Xem catalog, Đặt hàng, Xem lịch sử đơn hàng của mình, Hủy đơn của mình |

---

## 🔑 Thử Nghiệm API

### Cách 1: Qua Swagger UI
1. Mở [http://localhost:5000](http://localhost:5000)
2. Gọi endpoint `POST /api/auth/login` với tài khoản Admin hoặc Customer.
3. Copy chuỗi `accessToken` trong response.
4. Bấm nút **Authorize 🔓** ở góc trên bên phải Swagger, nhập:
   ```text
   Bearer <token_cua_ban>
   ```
5. Thử nghiệm tất cả các endpoint có icon ổ khóa.

### Cách 2: Qua VS Code / Visual Studio (.http file)
Mở file [test-requests.http](file:///d:/.net/.net/ECommerceAPI/test-requests.http) và bấm **Send Request** trực tiếp trên từng request. Token sẽ tự động được lưu và gán vào các request tiếp theo!

### Cách 3: Qua Postman
Import file [ECommerceAPI.postman_collection.json](file:///d:/.net/.net/ECommerceAPI/ECommerceAPI.postman_collection.json) vào Postman.

---

## 📋 Danh Sách Endpoints

### 1. Authentication (`/api/auth`)
| Method | Endpoint | Quyền | Mô tả |
|---|---|---|---|
| `POST` | `/api/auth/register` | Public | Đăng ký tài khoản khách hàng mới |
| `POST` | `/api/auth/login` | Public | Đăng nhập lấy JWT Bearer Token |
| `GET` | `/api/auth/me` | Authenticated | Lấy thông tin tài khoản hiện tại |

### 2. Categories (`/api/categories`)
| Method | Endpoint | Quyền | Mô tả |
|---|---|---|---|
| `GET` | `/api/categories` | Public | Danh sách danh mục (kèm số lượng sản phẩm) |
| `GET` | `/api/categories/{id}` | Public | Chi tiết danh mục |
| `POST` | `/api/categories` | Admin, Manager | Tạo danh mục mới |
| `PUT` | `/api/categories/{id}` | Admin, Manager | Cập nhật danh mục |
| `DELETE` | `/api/categories/{id}` | Admin | Xóa danh mục (chặn nếu còn sản phẩm hoạt động) |

### 3. Products (`/api/products`)
| Method | Endpoint | Quyền | Mô tả |
|---|---|---|---|
| `GET` | `/api/products` | Public | Danh sách sản phẩm (**Phân trang**, tìm kiếm, lọc min/max price, category, sắp xếp) |
| `GET` | `/api/products/{id}` | Public | Chi tiết sản phẩm |
| `POST` | `/api/products` | Admin, Manager | Thêm sản phẩm mới (kiểm tra trùng SKU) |
| `PUT` | `/api/products/{id}` | Admin, Manager | Cập nhật sản phẩm |
| `DELETE` | `/api/products/{id}` | Admin | Xóa mềm sản phẩm (Soft delete) |

#### Query Parameters hỗ trợ cho `/api/products`:
- `page`: Trang hiện tại (mặc định: 1)
- `pageSize`: Số phần tử trên trang (mặc định: 10, tối đa 100)
- `keyword`: Tìm theo tên, mô tả hoặc mã SKU
- `categoryId`: Lọc theo danh mục
- `minPrice` / `maxPrice`: Lọc khoảng giá
- `status`: Lọc theo trạng thái (`Active`, `Inactive`, `OutOfStock`, `Discontinued`)
- `sortBy`: Cột cần sắp xếp (`name`, `price`, `stock`, hoặc mặc định `createdAt`)
- `sortDesc`: `true` nếu muốn sắp xếp giảm dần, `false` tăng dần

### 4. Orders (`/api/orders`)
| Method | Endpoint | Quyền | Mô tả |
|---|---|---|---|
| `POST` | `/api/orders` | Customer | Đặt hàng (kiểm tra tồn kho, trừ tồn kho, tự sinh mã `ORD-YYYYMMDD-XXXXX`) |
| `GET` | `/api/orders/my-orders` | Customer | Xem lịch sử đơn hàng của bản thân |
| `GET` | `/api/orders/{id}` | Owner / Admin | Xem chi tiết đơn hàng (khách chỉ xem được đơn của mình) |
| `GET` | `/api/orders` | Admin, Manager | Xem tất cả đơn hàng (có phân trang & lọc theo trạng thái, ngày) |
| `PATCH` | `/api/orders/{id}/status` | Admin, Manager | Cập nhật trạng thái đơn (tuân theo State Machine) |
| `POST` | `/api/orders/{id}/cancel` | Owner / Admin | Hủy đơn (**tự động hoàn trả tồn kho** nếu đơn đang Pending/Confirmed) |

---

## 🎯 Điểm Sáng Trả Lời Phỏng Vấn (Cheat Sheet)

Khi nhà tuyển dụng hỏi về dự án, đây là những điểm kỹ thuật quan trọng bạn có thể tự tin trình bày:

### 1. Tại sao dùng Clean Architecture thay vì gom chung 1 project?
- **Separation of Concerns (Phân tách trách nhiệm)**: Domain chứa các thực thể và business rules cốt lõi; Application chứa use cases; Infrastructure lo việc kết nối storage/third-party; API chỉ làm nhiệm vụ tiếp nhận HTTP request.
- **Dễ dàng mở rộng & Kiểm thử (Testability)**: Do các layer giao tiếp qua Interface, ta có thể dễ dàng viết Unit Test (mock `IRepository`, `IUnitOfWork`) mà không cần database thật.

### 2. Nếu chuyển từ In-Memory sang SQL Server / PostgreSQL thì làm thế nào?
- Chỉ cần tạo một class `ApplicationDbContext : DbContext` trong `Infrastructure`, triển khai `IRepository<T>` bằng EF Core `DbSet<T>`, và sửa dòng cấu hình `AddInfrastructureServices()` trong `Program.cs`.
- Toàn bộ `Application` layer và `API Controllers` không bị thay đổi bất kỳ dòng code nào.

### 3. Nghiệp vụ Order được xử lý như thế nào?
- **Kiểm tra tồn kho**: Trước khi tạo đơn, hệ thống duyệt qua từng món hàng, kiểm tra `StockQuantity >= requestedQuantity`. Nếu đủ mới trừ tồn kho.
- **Khôi phục tồn kho**: Khi người dùng hoặc admin hủy đơn (`/cancel`), hệ thống hoàn lại đúng số lượng vào kho và kích hoạt lại sản phẩm nếu trước đó bị `OutOfStock`.
- **State Machine đơn hàng**: Chặn các chuyển đổi trạng thái phi logic (ví dụ không thể chuyển thẳng từ `Pending` sang `Delivered` mà phải qua `Confirmed` -> `Processing` -> `Shipped` -> `Delivered`).

### 4. Quản lý lỗi tập trung (Global Exception Handling)
- Triển khai custom middleware `GlobalExceptionMiddleware` trong pipeline để bắt mọi ngoại lệ chưa được xử lý.
- Đảm bảo client luôn nhận được format JSON nhất quán và không làm lộ thông tin nhạy cảm của server (stack trace) ra bên ngoài.
