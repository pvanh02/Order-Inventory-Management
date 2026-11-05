# Order & Inventory API

REST API ASP.NET Core cho một cửa hàng. Hiện có nền tảng bốn tầng, đăng nhập JWT, quản trị tài khoản, CRUD danh mục/sản phẩm và sổ tồn kho. Khách hàng, đơn hàng và báo cáo còn thuộc các phase tiếp theo.

- [COMET System Design](docs/system-design.md): requirement/UC/BR, quyết định Identity đã chốt và thiết kế Proposed/TBD còn lại.

Tests, scripts hỗ trợ, kế hoạch và hướng dẫn làm việc nội bộ được giữ riêng trên máy, không nằm trong repository này. Solution chỉ chứa các project chạy ứng dụng và migrator.

## Chuẩn bị

Cần .NET SDK **10.0.403** theo [global.json](global.json) để build bằng SDK; Docker Desktop với engine **Linux/amd64** và Docker Compose để chạy container. Phiên bản NuGet nằm trong [Directory.Packages.props](Directory.Packages.props); SQL Server và container images được pin trong [compose.yaml](compose.yaml) và [Dockerfile](Dockerfile).

Chạy các lệnh từ repository root với SDK đúng phiên bản có trong PATH:

```powershell
dotnet --version
dotnet tool restore
dotnet restore --locked-mode --disable-parallel -m:1
dotnet build --no-restore -m:1
```

## Cấu hình và chạy local

Tạo `.env` từ [.env.example](.env.example) nếu chưa có:

```powershell
if (-not (Test-Path .env)) { Copy-Item .env.example .env }
```

Chỉnh `.env` riêng trên máy: thay các placeholder bằng username Admin/Staff khác nhau, mật khẩu ngẫu nhiên dài 12–128 ký tự và đáp ứng độ phức tạp SQL Server cho `SQL_SA_PASSWORD`. `JWT_SIGNING_KEY_BASE64` phải là Base64 của ít nhất 32 byte ngẫu nhiên mật mã. Có thể tạo key riêng trong PowerShell bằng `[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(64))`. Giữ bí mật các giá trị này; `.env` đã bị ignore. Các giá trị cấu hình khác có mặc định trong file mẫu.

Sau khi hoàn tất cấu hình:

```powershell
docker compose config --quiet
docker compose up --build -d --wait
```

Chạy Compose khi Docker engine đã phản hồi bình thường. Mặc định API ở `http://localhost:8080`, SQL ở `127.0.0.1:14333`; có thể đổi port trong cấu hình local. Compose chờ service `sql` healthy, chạy migrator một lần rồi mới khởi động API. Truy cập [Swagger UI](http://localhost:8080/swagger) hoặc [OpenAPI JSON](http://localhost:8080/swagger/v1/swagger.json) trong Development. UI/spec được phép anonymous **chỉ ở Development**; mọi endpoint nghiệp vụ ngoài login cần JWT.

Đọc thông tin đăng nhập Admin/Staff từ `.env` **riêng trên máy** để dùng Swagger/HTTP client. Không đưa mật khẩu, hash hay token vào tài liệu, log, ảnh chụp hoặc báo cáo. `.env.example` chỉ là mẫu cấu hình, không chứa thông tin đăng nhập dùng được.

Nếu chạy API bằng SDK thay vì container, cấp `ConnectionStrings:Database`, `Jwt:*` và `Bootstrap:*` qua biến môi trường hoặc user-secrets ID `order-inventory-local`; SDK không tự đọc file `.env`. Với SQL container mặc định, dùng host `127.0.0.1,14333`. Các field tương ứng được mô tả trong `.env.example` và [cấu hình Infrastructure](src/OrderInventory.Infrastructure/DependencyInjection.cs).

```powershell
docker compose up -d --wait sql
dotnet run --project tools/OrderInventory.DbMigrator
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = 'http://localhost:8080'
dotnet run --project src/OrderInventory.Api
```

API yêu cầu migration đã áp dụng và có Admin active trước khi nhận request. Readiness container dùng CLI riêng trong [HealthProbe](src/OrderInventory.Api/Hosting/HealthProbe.cs); không có anonymous health endpoint. Volume SQL giữ dữ liệu qua restart; README không yêu cầu xóa volume để khôi phục môi trường.

## API hiện có

Contract DTO/validation thuộc [Application Identity](src/OrderInventory.Application/Identity/Contracts.cs) và [IdentityValidation](src/OrderInventory.Application/Identity/IdentityValidation.cs); routing thuộc [AuthController](src/OrderInventory.Api/Controllers/AuthController.cs) và [UsersController](src/OrderInventory.Api/Controllers/UsersController.cs). OpenAPI được tạo từ các owner này qua [Program](src/OrderInventory.Api/Program.cs).

| Method | Route | UC | Quyền / kết quả thành công |
|---|---|---|---|
| POST | `/api/v1/auth/login` | UC-AUTH01 | Username + password; 200 `LoginResult` |
| GET | `/api/v1/users` | UC-USR02 | Admin; 200 danh sách phân trang |
| GET | `/api/v1/users/{id}` | UC-USR03 | Admin; 200 `UserDto` |
| POST | `/api/v1/users` | UC-USR04 | Admin; 201 `UserDto` và Location |
| PATCH | `/api/v1/users/{id}/role` | UC-USR05 | Admin; 200 `UserDto` |
| PATCH | `/api/v1/users/{id}/active` | UC-USR06 | Admin; 200 `UserDto` |
| GET | `/api/v1/categories`, `/api/v1/categories/{id}` | UC-CAT01/02 | Admin/Staff; 200 |
| POST | `/api/v1/categories` | UC-CAT03 | Admin; 201 + Location |
| PUT | `/api/v1/categories/{id}` | UC-CAT04 | Admin; 200 |
| DELETE | `/api/v1/categories/{id}` | UC-CAT05 | Admin; 204, kể cả xóa lặp |
| GET | `/api/v1/products`, `/api/v1/products/{id}` | UC-PRD01/02 | Admin/Staff; 200 |
| POST | `/api/v1/products` | UC-PRD03 | Admin; 201 + Location, tồn 0 |
| PUT | `/api/v1/products/{id}` | UC-PRD04 | Admin; 200, giữ SKU |
| DELETE | `/api/v1/products/{id}` | UC-PRD05 | Admin; 204, kể cả xóa lặp |
| GET | `/api/v1/inventory` | UC-INV01 | Admin/Staff; 200 |
| POST | `/api/v1/inventory/receipts` | UC-INV02 | Admin/Staff; 200 `MovementDto` |
| POST | `/api/v1/inventory/adjustments` | UC-INV03 | Admin; 200 `MovementDto` |
| GET | `/api/v1/inventory/movements` | UC-INV04 | Admin/Staff; 200, gồm lịch sử product đã xóa |

Access JWT có hạn 30 phút, không refresh token. Đổi role hoặc active state thực sự làm token cũ hết hiệu lực ở request kế tiếp; không được hạ quyền hay vô hiệu hóa Admin active cuối cùng. Staff gọi user-management trả 403. Hợp đồng lỗi dùng ProblemDetails với `errorCode`, `traceId`, và `errors` cho validation; xem [ProblemResponses](src/OrderInventory.Api/Errors/ProblemResponses.cs) và các kiểm tra HTTP.

Catalog/kho dùng số lượng nguyên và giá VND nguyên dương; phần lẻ bị từ chối. SKU trim/uppercase, không đổi sau tạo và không tái sử dụng sau xóa mềm. Category còn active product/product còn tồn trả 409 khi xóa. Điều chỉnh nhận signed delta khác 0 và lý do; mỗi thay đổi tồn ghi một movement trong cùng transaction, không tự retry khi mất phản hồi. Các bounds/query allowlist thuộc [CatalogRules](src/OrderInventory.Domain/Catalog/CatalogRules.cs), [CatalogValidation](src/OrderInventory.Application/Catalog/CatalogValidation.cs) và [Inventory contracts](src/OrderInventory.Application/Inventory/Contracts.cs); Swagger mô tả input limits qua [CatalogSchemaFilter](src/OrderInventory.Api/OpenApi/CatalogSchemaFilter.cs).

Ví dụ query sản phẩm: `/api/v1/products?categoryId=1&minPrice=10000&inStock=true&sort=-unitPrice&page=1&pageSize=20`. History hỗ trợ `productId`, `actorId`, `type=Receipt|Adjustment`, `from`/`to` theo khoảng `[from,to)` và sort `id`/`occurredAt` có dấu `-` để đảo chiều. Không nhận actor hoặc tồn hiện tại từ payload catalog.

## Cấu trúc và schema hiện có

[OrderInventory.sln](OrderInventory.sln) gồm API → Application → Domain, Infrastructure triển khai các port của Application; Domain không phụ thuộc tầng khác. [DbMigrator](tools/OrderInventory.DbMigrator/Program.cs) là công cụ migration/seed, không phải tầng nghiệp vụ mới. [BootstrapSeedService](src/OrderInventory.Application/Seeding/BootstrapSeedService.cs) sở hữu transaction cho dữ liệu tài khoản và marker seed.

ERD phản ánh schema hiện có. Mapping thuộc [DbContext](src/OrderInventory.Infrastructure/Persistence/OrderInventoryDbContext.cs) và [configurations](src/OrderInventory.Infrastructure/Persistence/Configurations); lịch sử schema tại [migrations](src/OrderInventory.Infrastructure/Persistence/Migrations).

```mermaid
erDiagram
    Categories ||--o{ Products : categorizes
    Products ||--|| InventoryBalances : has
    Products ||--o{ InventoryMovements : records
    UserAccounts ||--o{ InventoryMovements : performs
    Categories {
        int Id PK
        nvarchar Name
        bit IsDeleted
        rowversion RowVersion
    }
    Products {
        int Id PK
        int CategoryId FK
        nvarchar Sku UK
        nvarchar Name
        decimal UnitPrice "whole VND"
        bit IsDeleted
        rowversion RowVersion
    }
    InventoryBalances {
        int ProductId PK,FK
        int OnHand "nonnegative"
    }
    InventoryMovements {
        bigint Id PK
        int ProductId FK
        int ActorId FK
        int Delta
        int BalanceAfter
        int Type
        nvarchar Reason
        datetimeoffset OccurredAt
    }
    UserAccounts {
        int Id PK
        nvarchar UserName
        nvarchar NormalizedUserName UK
        nvarchar PasswordHash
        int Role
        bit IsActive
        int TokenVersion
        datetimeoffset CreatedAt
        rowversion RowVersion
    }
    SeedHistory {
        int Id PK
        nvarchar SeedVersion "unique with SeedUnitKey"
        nvarchar SeedUnitKey
        datetimeoffset CompletedAt
    }
```

[CatalogSeedService](src/OrderInventory.Application/Seeding/CatalogSeedService.cs) tạo demo category, `DEMO-COFFEE` và `DEMO-TEA` với receipt/history ban đầu dưới cùng transaction với seed marker. Migration/seed chạy lại giữ nguyên dữ liệu đã sửa; không ghi đè tồn hoặc thông tin đăng nhập.

## Kiểm tra

Repository không phân phối test projects. Kiểm tra build bằng các lệnh ở phần chuẩn bị; kiểm tra model/migration bằng:

```powershell
dotnet ef migrations has-pending-model-changes --project src/OrderInventory.Infrastructure --startup-project tools/OrderInventory.DbMigrator
```

Sau khi Compose ready, dùng Swagger kiểm tra đăng nhập Admin/Staff, Admin gọi `GET /api/v1/users` trả 200 và Staff gọi cùng endpoint trả 403. `dotnet test` ở solution này không chạy các suite được giữ riêng trên máy. V1 chỉ chạy local, không gồm frontend, thanh toán online hay multi-tenant.
