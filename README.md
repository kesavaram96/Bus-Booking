<<<<<<< HEAD
# 🚌 BusBooking

> A modern Sri Lankan Bus Booking Platform built with **ASP.NET Core 10**, **Clean Architecture**, and a **Modular Monolith** design.

**Backend:** ✅ Complete
**Frontend:** 🚧 React + TypeScript — Coming Next

---

## ✨ Features

* 🔐 JWT Authentication & Refresh Tokens
* 👥 Role-Based Authorization
* 🚌 Bus Management
* 💺 Seat Management
* 🛣️ Route & Stop Management
* 📅 Trip Management
* 🔎 Trip Search
* 🔒 Redis Seat Locking
* 🎫 Segment-Based Seat Availability
* 📋 Customer & Guest Booking
* 💳 Payment Processing
* 🎟️ Digital Tickets & QR Codes
* ❌ Booking & Trip Cancellation
* 💰 Refund Processing
* 📧 Email Notifications
* 📊 Reports
* 📝 Audit Logging
* 🛡️ Security & Rate Limiting
* ❤️ Health Checks
* 🧪 Unit & Integration Testing
* 🐳 Docker Support

---

## 🏗️ Architecture

The backend follows **Clean Architecture** with a **Modular Monolith** approach.

### Project Structure

```text
BusBooking
│
├── src
│   ├── BusBooking.API
│   ├── BusBooking.Application
│   ├── BusBooking.Domain
│   └── BusBooking.Infrastructure
│
├── tests
│   ├── BusBooking.UnitTests
│   └── BusBooking.IntegrationTests
│
├── docker-compose.yml
├── docker-compose.prod.yml
├── .env.example
└── BusBooking.sln
```

### Architecture Flow

```text
API
 ↓
Application
 ↓
Domain
 ↑
Infrastructure
```

---

## 🛠️ Tech Stack

* **Backend:** ASP.NET Core 10 Web API
* **Architecture:** Clean Architecture + Modular Monolith
* **Database:** SQL Server
* **ORM:** Entity Framework Core
* **Authentication:** ASP.NET Core Identity + JWT
* **CQRS:** MediatR
* **Validation:** FluentValidation
* **Mapping:** Mapster
* **Caching & Locking:** Redis
* **Logging:** Serilog
* **Background Jobs:** Hangfire
* **QR Codes:** QRCoder
* **API Documentation:** Swagger / OpenAPI
* **Testing:** xUnit, Moq, FluentAssertions
* **Containerization:** Docker

---

## 🔄 Booking Flow

```text
Search Trip
     ↓
Select Seats
     ↓
Lock Seats in Redis
     ↓
Enter Passenger Details
     ↓
Create Booking
     ↓
Process Payment
     ↓
Confirm Booking
     ↓
Generate Ticket
     ↓
Generate QR Code
```

---

## 🔐 Security

The API includes:

* JWT access tokens
* Refresh token rotation
* Role-based authorization
* Request validation
* Rate limiting
* Security headers
* Correlation IDs
* Audit logging
* Sensitive data protection
* Secure QR ticket verification

---

## 🧪 Testing

The project includes:

* Unit tests
* Integration tests
* Authentication tests
* Booking tests
* Seat-locking tests
* Payment tests
* Cancellation & refund tests
* QR verification tests
* Security tests
* Health-check tests

Run all tests:

```bash
dotnet test BusBooking.sln
```

---

## 🚀 Getting Started

### Prerequisites

Make sure you have:

* .NET 10 SDK
* SQL Server
* Redis
* Docker *(optional)*

### Clone the Repository

```bash
git clone <repository-url>
cd BusBooking
```

### Configure JWT

```bash
dotnet user-secrets set \
  "Jwt:Secret" \
  "<your-long-random-secret>" \
  --project src/BusBooking.API
```

### Restore & Build

```bash
dotnet restore BusBooking.sln
dotnet build BusBooking.sln
```

### Apply Database Migrations

```bash
dotnet ef database update \
  --project src/BusBooking.Infrastructure \
  --startup-project src/BusBooking.API
```

### Run the API

```bash
dotnet run --project src/BusBooking.API
```

Open Swagger:

```text
/swagger
```

Health check:

```text
/api/health
```

---

## 🐳 Docker

Create your environment file:

```bash
cp .env.example .env
```

Then run:

```bash
docker compose up --build
```

For production:

```bash
docker compose \
  -f docker-compose.yml \
  -f docker-compose.prod.yml \
  up --build
```

---

## 📊 Development Status

### Backend

**22 / 22 Phases Completed ✅**

| Phase                | Status |
| -------------------- | ------ |
| Solution Setup       | ✅      |
| Domain & Database    | ✅      |
| Authentication       | ✅      |
| Bus Management       | ✅      |
| Seat Layout          | ✅      |
| Routes & Stops       | ✅      |
| Trip Management      | ✅      |
| Customer Management  | ✅      |
| Trip Search          | ✅      |
| Trip Seats           | ✅      |
| Redis Seat Locking   | ✅      |
| Booking              | ✅      |
| Segment Availability | ✅      |
| Payment              | ✅      |
| Ticket & QR          | ✅      |
| Passenger Register   | ✅      |
| Cancellation         | ✅      |
| Notifications        | ✅      |
| Reports              | ✅      |
| Audit & Security     | ✅      |
| Testing              | ✅      |
| Docker & Deployment  | ✅      |

### Frontend

**React + TypeScript — Coming Next 🚧**

Planned modules:

* Authentication
* Trip Search
* Seat Selection
* Passenger Details
* Payment
* Ticket
* Customer Account
* Admin Dashboard
* Bus Management
* Route Management
* Trip Management
* Booking Management
* Reports

---

## 🎯 Project Goal

BusBooking is designed specifically for the **Sri Lankan bus transportation industry**.

The platform is designed to support:

* Multiple buses
* Multiple routes
* Multiple trips
* Real-time seat locking
* Partial route bookings
* Guest bookings
* Registered customers
* Staff-assisted bookings
* Online payments
* Digital ticket verification
* Operational reporting

---

## 🗺️ Roadmap

### Backend

**100% Complete ✅**

### React Frontend

**0% — Next Phase 🚧**

The next major milestone is building the React + TypeScript frontend and connecting it with the completed backend API.

---

## 📄 License

Add your preferred license here.

---

## ⭐ Support

If you find this project useful, consider giving the repository a ⭐ star.
=======
🚌 BusBooking

A modern Sri Lankan Bus Booking Platform backend built with ASP.NET Core 10, following Clean Architecture and a Modular Monolith approach.

The backend provides everything needed for bus operators and passengers — from route and trip management to seat locking, booking, payments, tickets, notifications, reporting, and security.

🚧 Backend: Complete
🎨 React Frontend: Planned / Next Phase

✨ Features
🔐 JWT Authentication & Refresh Tokens
👥 Role-Based Authorization
🚌 Bus Management
💺 Seat Layout & Seat Management
🛣️ Route & Stop Management
📅 Trip Management & Status Lifecycle
🔎 Public Trip Search
🔒 Redis-Based Atomic Seat Locking
🎫 Segment-Based Seat Availability
📋 Customer, Guest & Staff Booking
💳 Payment Processing
🎟️ Digital Tickets with QR Codes
👤 Passenger Manifest
❌ Booking & Trip Cancellation
💰 Automatic Refund Handling
📧 Background Email Notifications
📊 Reporting APIs
📝 Audit Logging
🛡️ Security Headers & Rate Limiting
❤️ Health Checks
🧪 Unit & Integration Testing
🐳 Docker & Docker Compose Support
🏗️ Architecture
                    ┌─────────────────┐
                    │   React Client  │
                    │    (Planned)    │
                    └────────┬────────┘
                             │
                             ▼
                    ┌─────────────────┐
                    │ ASP.NET Core API│
                    └────────┬────────┘
                             │
              ┌──────────────┼──────────────┐
              ▼              ▼              ▼
       ┌────────────┐ ┌────────────┐ ┌────────────┐
       │ SQL Server │ │   Redis    │ │ Hangfire   │
       │   Database │ │ Seat Locks │ │ Background │
       └────────────┘ └────────────┘ │    Jobs    │
                                     └────────────┘
Clean Architecture
src/
├── BusBooking.API
├── BusBooking.Application
├── BusBooking.Domain
└── BusBooking.Infrastructure

tests/
├── BusBooking.UnitTests
└── BusBooking.IntegrationTests

Dependency flow:

Domain
   ↑
Application
   ↑
Infrastructure
   ↑
API
🛠️ Tech Stack
Category	Technology
Backend	ASP.NET Core 10 Web API
Architecture	Clean Architecture + Modular Monolith
Database	SQL Server
ORM	Entity Framework Core
Authentication	ASP.NET Core Identity + JWT
CQRS	MediatR
Validation	FluentValidation
Mapping	Mapster
Caching / Locking	Redis
Logging	Serilog
Background Jobs	Hangfire
QR Codes	QRCoder
API Documentation	Swagger / OpenAPI
Testing	xUnit, Moq, FluentAssertions
Containerization	Docker
🔄 Booking Flow

The main business flow is:

Search Trip
     │
     ▼
Select Seats
     │
     ▼
Redis Seat Lock
     │
     ▼
Enter Passenger Details
     │
     ▼
Create Booking
     │
     ▼
Payment
     │
     ▼
Confirm Booking
     │
     ▼
Generate Ticket
     │
     ▼
QR Ticket

The critical backend flow is:

Trip → TripSeat → Redis Lock → Booking → Payment → Ticket

🔐 Security

The API includes:

JWT access tokens
Refresh-token rotation
Hashed refresh tokens
Role-based authorization policies
Request validation
Rate limiting
Security headers
Correlation IDs
Audit logging
Sensitive-data redaction
Secure QR ticket identifiers
No card numbers or CVV stored
🧪 Testing

The project includes both unit and integration tests covering:

Authentication
Authorization
Bus & route management
Trip management
Seat availability
Redis seat locking
Booking
Concurrent booking attempts
Payments
Cancellation & refunds
Ticket generation
QR verification
Notifications
Reports
Security
Health checks

Run the test suite:

dotnet test BusBooking.sln
🚀 Getting Started
Prerequisites
.NET 10 SDK
SQL Server
Redis 6+
Docker (optional)
1. Clone the repository
git clone <repository-url>
cd BusBooking
2. Configure JWT Secret
dotnet user-secrets set \
  "Jwt:Secret" \
  "<your-long-random-secret>" \
  --project src/BusBooking.API
3. Restore & Build
dotnet restore BusBooking.sln
dotnet build BusBooking.sln
4. Apply Database Migrations
dotnet tool install --global dotnet-ef

dotnet ef database update \
  --project src/BusBooking.Infrastructure \
  --startup-project src/BusBooking.API
5. Run the API
dotnet run --project src/BusBooking.API

Swagger:

/swagger

Health:

GET /api/health
GET /health
🐳 Docker

Copy the environment template:

cp .env.example .env

Configure the required values and run:

docker compose up --build

For production-shaped deployment:

docker compose \
  -f docker-compose.yml \
  -f docker-compose.prod.yml \
  up --build
📈 Development Status
Backend

22 / 22 phases completed ✅

01 Solution Setup             ✅
02 Domain & Database          ✅
03 Authentication             ✅
04 Bus Management             ✅
05 Seat Layout                ✅
06 Routes & Stops             ✅
07 Trip Management            ✅
08 Customer Management        ✅
09 Trip Search                ✅
10 Trip Seats                 ✅
11 Redis Seat Locking         ✅
12 Booking                    ✅
13 Segment Availability       ✅
14 Payment                    ✅
15 Ticket & QR                ✅
16 Passenger Register         ✅
17 Cancellation               ✅
18 Notifications              ✅
19 Reports                    ✅
20 Audit & Security           ✅
21 Testing                   ✅
22 Docker & Deployment        ✅
Frontend

React + TypeScript frontend — Coming Next 🚧

Planned modules:

Authentication
Trip Search
Seat Selection
Passenger Details
Payment
Ticket
Customer Account
Admin Dashboard
Bus Management
Route Management
Trip Management
Booking Management
Reports
📁 Project Structure
BusBooking/
│
├── src/
│   ├── BusBooking.API/
│   ├── BusBooking.Application/
│   ├── BusBooking.Domain/
│   └── BusBooking.Infrastructure/
│
├── tests/
│   ├── BusBooking.UnitTests/
│   └── BusBooking.IntegrationTests/
│
├── docker-compose.yml
├── docker-compose.prod.yml
├── .env.example
├── Directory.Build.props
└── BusBooking.sln
🎯 Project Goal

The goal of BusBooking is to provide a scalable and secure booking platform designed specifically for the Sri Lankan bus transportation industry.

The architecture is designed to support:

Multiple buses
Multiple routes
Multiple trips
Real-time seat locking
Partial route bookings
Guest and registered passengers
Staff-assisted bookings
Online payment integration
Digital ticket verification
Operational reporting
📌 Roadmap
Backend
████████████████████ 100%

React Frontend
░░░░░░░░░░░░░░░░░░░░   0%

Next: Build the React + TypeScript frontend and connect it to the completed backend API.

📄 License

Add your preferred license here.

⭐ If you find this project useful

Feel free to star ⭐ the repository and follow the development of the React frontend.
>>>>>>> c011b2870c208d104c56b74782b75d84e0a12689
