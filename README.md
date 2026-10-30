# Order Management API

REST API untuk mengelola pesanan pada perusahaan distribusi. Aplikasi ini mendukung pembuatan pesanan, pengecekan stok, pengelolaan status pesanan, pembatalan pesanan, serta pencatatan transaksi menggunakan PostgreSQL.

## Teknologi

- C# dan ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- FluentValidation
- Serilog
- xUnit
- REST API

## Fitur

- Membuat order dengan satu atau beberapa produk.
- Mengurangi stok produk secara atomik ketika order dibuat.
- Menolak order jika stok tidak mencukupi.
- Mencegah duplikasi order menggunakan `Idempotency-Key`.
- Mengambil detail order berdasarkan ID.
- Menampilkan daftar order dengan filter status, customer, rentang tanggal, dan pagination.
- Mengubah status order sesuai aturan bisnis.
- Membatalkan order dan mengembalikan stok.
- Optimistic concurrency menggunakan kolom `Version`.
- Logging dan correlation ID untuk membantu penelusuran request.
- Integration test untuk skenario concurrency.

## Konfigurasi Database

Buat database PostgreSQL:

```sql
CREATE DATABASE order_management;
```

Atur connection string pada `appsettings.json` di project API:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=order_management;Username=postgres;Password=YOUR_PASSWORD"
  }
}
```

Ganti `YOUR_PASSWORD` dengan password PostgreSQL lokal.

## Menjalankan Migration

Dari folder utama solution, jalankan:

```bash
dotnet ef database update \
  --project OrderManagement.Infrastructure \
  --startup-project OrderManagement.Api
```

## Menjalankan Aplikasi

Jalankan perintah:

```bash
dotnet run --project OrderManagement.Api
```

## API Endpoints

| Method | Endpoint | Deskripsi |
|---|---|---|
| POST | `/api/orders` | Membuat order |
| GET | `/api/orders/{id}` | Mengambil detail order |
| GET | `/api/orders` | Mengambil daftar order |
| PUT | `/api/orders/{id}/status` | Mengubah status order |
| POST | `/api/orders/{id}/cancel` | Membatalkan order |

### Create Order

```http
POST /api/orders
Idempotency-Key: ORDER-001
Content-Type: application/json
```

```json
{
  "customerId": "22222222-2222-2222-2222-222222222222",
  "shippingAddress": "Jakarta",
  "items": [
    {
      "productId": "11111111-1111-1111-1111-111111111111",
      "quantity": 2
    }
  ]
}
```

### Get Order

```http
GET /api/orders/{id}
```

### Get Order List

```http
GET /api/orders?status=Pending&customerId=22222222-2222-2222-2222-222222222222&page=1&pageSize=10
```

Filter yang tersedia:

- `status`
- `customerId`
- `startDate`
- `endDate`
- `page`
- `pageSize`

### Update Order Status

```http
PUT /api/orders/{id}/status
Content-Type: application/json
```

```json
{
  "status": 1,
  "version": 1
}
```

### Cancel Order

```http
POST /api/orders/{id}/cancel
Content-Type: application/json
```

```json
{
  "version": 1
}
```

Pembatalan hanya diperbolehkan untuk order berstatus `Pending` atau `Confirmed`.

## Error Handling

HTTP status :

| Status | Keterangan |
|---|---|
| 400 | Request tidak valid |
| 404 | Data tidak ditemukan |
| 409 | Conflict atau concurrency conflict |
| 422 | Jika digunakan untuk validasi business rule |
| 500 | Internal server error |

## Running Tests

Jalankan seluruh test:

```bash
dotnet test
```

```bash
dotnet test OrderManagement.IntegrationTests
```

Skenario integration test:

- Dua request membuat order dengan stok yang terbatas.
- Dua request menggunakan idempotency key yang sama.
- Dua admin membatalkan order yang sama.
- Update status dan cancel dilakukan bersamaan.
