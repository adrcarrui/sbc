# 🛡️ Simulator Backup Control

![Status](https://img.shields.io/badge/status-active%20development-blue)
![Backend](https://img.shields.io/badge/backend-.NET%209-512BD4)
![Frontend](https://img.shields.io/badge/frontend-React%20%2B%20TypeScript-61DAFB)
![Database](https://img.shields.io/badge/database-PostgreSQL-336791)
![Backup](https://img.shields.io/badge/integration-UrBackup-2E7D32)

**Simulator Backup Control**, or **SBC**, is an internal backup monitoring and control application designed to centralize the status of simulator-related protected systems, UrBackup clients, manual backup requests, alerts, and backup validation workflows.

SBC integrates with **UrBackup** to monitor automatic file and image backups, while also supporting **manual backup requests** for systems that are not fully covered by UrBackup.

---

## 📌 Overview

SBC provides a single control panel to answer questions such as:

* 🖥️ Which systems are protected?
* 🔗 Which systems are integrated with UrBackup?
* 🟢 Which systems are online?
* 🔴 Which systems are offline?
* 💾 Which backups are successful?
* ⚠️ Which backups have issues?
* ❌ Which clients were removed from UrBackup?
* 🚨 Which alerts are currently open?
* 📝 Which systems require manual backup handling?
* ✅ Which manual backups have been completed and validated?

The application is designed for environments where multiple simulator PCs or support systems need to be tracked, audited, and reviewed from a central interface.

---

## ✨ Main Features

### 🔄 UrBackup Monitoring

SBC connects to the UrBackup server API and keeps protected systems synchronized.

It tracks:

* 🧾 UrBackup client ID
* 🖥️ Client name
* 🧬 Client version
* 🪟 Operating system
* 🟢 Online/offline status
* 👀 Last seen timestamp
* 📁 Last file backup
* 🧱 Last image backup
* ⚠️ Backup issues
* ❌ Removed clients

---

### 🖥️ Protected Systems

SBC maintains a database of protected systems.

It supports:

* 🔗 Systems integrated with UrBackup
* 📝 Systems handled manually
* 🧩 Simulator assignments
* 🧠 Backup capability tracking
* 📡 UrBackup synchronization status

Tracked metadata includes:

* Hostname
* IP address
* Operating system
* Simulator assignment
* Backup capability
* UrBackup client link
* Online/offline status
* Removed-from-UrBackup state

---

### 📊 Backup Dashboard

The frontend provides dedicated pages for each operational area:

* 🏠 **Overview**
* 🖥️ **Systems**
* 🔍 **System Detail**
* 🚨 **Alerts**
* 💾 **Backups**
* 🔄 **UrBackup**
* 📝 **Manual Requests**

System detail pages are available at:

```txt
/systems/:id
```

---

### 🚨 Alerts

SBC automatically creates and tracks alerts for relevant backup conditions.

Typical alert cases include:

* 🔌 UrBackup server unreachable
* ❌ Protected system removed from UrBackup
* ⚠️ Backup issues detected
* 🕳️ No successful backup available
* 📝 Manual validation required

Alerts can be reviewed and resolved from the frontend.

---

### 📝 Manual Backup Requests

For systems not fully handled by UrBackup, SBC supports a manual backup workflow:

```txt
Pending → InProgress → Completed → Validated
```

Manual backup requests include:

* 🖥️ Protected system
* 🙋 Requester
* 👷 Assignee
* 🧾 Reason
* 🔖 Change reference
* 💾 Backup path
* 📝 Completion notes
* ✅ Validation notes
* 👤 Validator

---

## 🧰 Tech Stack

### 🧠 Backend

* ⚙️ **.NET 9**
* 🌐 **ASP.NET Core Web API**
* 🗃️ **Entity Framework Core**
* 🐘 **PostgreSQL**
* 🧱 **Clean Architecture**
* ⏱️ **Background Services**
* 🔄 **UrBackup API Integration**

### 🎨 Frontend

* ⚛️ **React**
* 🟦 **TypeScript**
* ⚡ **Vite**
* 🧭 **React Router**
* 📡 **Axios**
* 🎛️ **Reusable components**
* 🎨 **Structured App.css**

### 🗄️ Database

* 🐘 **PostgreSQL**

---

## 🗂️ Project Structure

```txt
simulator-backup-control/
├── backend/
│   ├── Sbc.Api/
│   │   ├── Controllers/
│   │   ├── BackgroundServices/
│   │   └── Program.cs
│   │
│   ├── Sbc.Application/
│   │   ├── Integrations/
│   │   └── Services/
│   │
│   ├── Sbc.Domain/
│   │   ├── Entities/
│   │   └── Enums/
│   │
│   ├── Sbc.Infrastructure/
│   │   ├── Persistence/
│   │   └── Integrations/
│   │
│   └── Sbc.Tests/
│
├── frontend/
│   ├── src/
│   │   ├── api/
│   │   ├── components/
│   │   ├── layout/
│   │   ├── pages/
│   │   ├── types/
│   │   ├── utils/
│   │   ├── App.tsx
│   │   └── App.css
│   │
│   ├── package.json
│   └── vite.config.ts
│
└── README.md
```

---

## 🧱 Backend Architecture

The backend follows a Clean Architecture style:

```txt
Sbc.Domain
    Core entities, enums, and domain rules.

Sbc.Application
    Application interfaces, use cases, service contracts, DTOs.

Sbc.Infrastructure
    EF Core persistence, database context, UrBackup integration.

Sbc.Api
    Controllers, background workers, dependency injection, HTTP endpoints.
```

This keeps business logic separated from infrastructure and API concerns.

A shocking concept: not putting everything in one controller and praying.

---

## 🎨 Frontend Architecture

The frontend is organized as a routed React application.

```txt
src/
├── api/
│   └── sbcApi.ts
│
├── components/
│   ├── BooleanStatus.tsx
│   ├── DetailItem.tsx
│   ├── EmptyState.tsx
│   ├── StatusBadge.tsx
│   └── SummaryCard.tsx
│
├── layout/
│   ├── AppLayout.tsx
│   └── Sidebar.tsx
│
├── pages/
│   ├── OverviewPage.tsx
│   ├── SystemsPage.tsx
│   ├── SystemDetailPage.tsx
│   ├── AlertsPage.tsx
│   ├── BackupsPage.tsx
│   ├── UrBackupPage.tsx
│   └── ManualRequestsPage.tsx
│
├── types/
│   └── dashboard.ts
│
└── utils/
    └── formatters.ts
```

Navigation is handled with **React Router**.

Main routes:

```txt
/
 /systems
 /systems/:id
 /alerts
 /backups
 /manual-requests
 /urbackup
```

---

## ✅ Requirements

Before running the project, make sure the following tools are installed:

* ⚙️ .NET 9 SDK
* 🟢 Node.js
* 📦 npm
* 🐘 PostgreSQL
* 💾 UrBackup Server
* 🖥️ UrBackup Client installed on protected systems

---

## ⚙️ Backend Configuration

The backend expects a PostgreSQL connection string named:

```txt
SbcDb
```

Example `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "SbcDb": "Host=localhost;Port=5432;Database=sbc;Username=postgres;Password=postgres"
  },
  "UrBackup": {
    "BaseUrl": "http://localhost:55414",
    "ApiPath": "/x",
    "Username": "",
    "Password": "",
    "TimeoutSeconds": 10,
    "HealthCheckIntervalSeconds": 60,
    "EnableClientSyncWorker": true,
    "ClientSyncIntervalSeconds": 300
  }
}
```

---

## 🚀 Running the Backend

From the project root:

```powershell
cd C:\Users\adrian\SIA\projects\simulator-backup-control

dotnet build .\SimulatorBackupControl.sln

dotnet run --project .\backend\Sbc.Api
```

By default, the API should be available at:

```txt
http://localhost:5169
```

---

## 🧪 Running Backend Tests

```powershell
dotnet test
```

---

## ⚡ Running the Frontend

From the frontend folder:

```powershell
cd C:\Users\adrian\SIA\projects\simulator-backup-control\frontend

npm install

npm run dev
```

The frontend should be available at:

```txt
http://localhost:5173
```

The Vite proxy forwards `/api` requests to the backend:

```ts
server: {
  proxy: {
    '/api': {
      target: 'http://localhost:5169',
      changeOrigin: true,
    },
  },
}
```

---

## 🔌 Useful API Endpoints

### 🔄 UrBackup

```http
GET  /api/urbackup/status
GET  /api/urbackup/raw-status
POST /api/urbackup/sync-clients
```

### 📊 Dashboard

```http
GET /api/dashboard/summary
GET /api/dashboard/urbackup-systems
GET /api/dashboard/attention-systems
GET /api/dashboard/latest-backups
GET /api/dashboard/recent-backup-events
```

### 🖥️ Protected Systems

```http
GET /api/protected-systems
GET /api/protected-systems/backup-capabilities
GET /api/protected-systems/{id}/urbackup-detail
```

### 🚨 Alerts

```http
GET  /api/alerts
GET  /api/alerts/open
POST /api/alerts/{id}/resolve
```

### 📝 Manual Backup Requests

```http
GET  /api/manual-backup-requests
POST /api/manual-backup-requests
PUT  /api/manual-backup-requests/{id}/start
PUT  /api/manual-backup-requests/{id}/complete
PUT  /api/manual-backup-requests/{id}/validate
```

---

## 🔄 UrBackup Client Synchronization

SBC synchronizes clients from UrBackup in two ways.

### ⏱️ Automatic Sync

A background worker periodically calls UrBackup and updates SBC records.

Configured with:

```json
"EnableClientSyncWorker": true,
"ClientSyncIntervalSeconds": 300
```

### 🖱️ Manual Sync

The frontend includes a manual sync action in the **UrBackup** page.

This calls:

```http
POST /api/urbackup/sync-clients
```

The sync process can:

* ➕ Create new protected systems
* 🔁 Update existing systems
* ♻️ Restore systems previously marked as removed
* ❌ Mark missing UrBackup clients as removed

---

## 💾 Backup Status Logic

SBC tracks backup state using values such as:

```txt
Success
Failed
PendingValidation
NoBackupJob
RemovedFromUrBackup
WithIssues
NoSuccessfulBackup
```

Systems requiring attention are highlighted in the dashboard and system tables.

Typical attention cases include:

* ❌ Backup failure
* ⚠️ Backup issues reported by UrBackup
* 🕳️ No successful backup available
* 🚫 Client removed from UrBackup
* 📝 Manual validation pending

---

## 📝 Manual Backup Workflow

Manual backups are used for systems that cannot be fully monitored through UrBackup.

```txt
Pending
   ↓
InProgress
   ↓
Completed
   ↓
Validated
```

### 🕓 Pending

A manual backup request has been created but not started.

### 🔧 InProgress

The backup task has been started.

### 📦 Completed

The backup has been completed and evidence has been registered.

### ✅ Validated

The backup has been reviewed and accepted.

---

## 🧭 Frontend Pages

### 🏠 Overview

Global summary of systems, alerts, backup health and systems requiring attention.

### 🖥️ Systems

List of protected systems with filters, UrBackup status and backup status.

### 🔍 System Detail

Detailed view of a protected system, including:

* Inventory information
* UrBackup integration
* Latest backup status
* Open alerts
* Recent alerts
* Recent events

### 🚨 Alerts

Open and resolved alerts, with filtering and resolve actions.

### 💾 Backups

Latest file and image backup status per protected system.

### 🔄 UrBackup

UrBackup server health, raw client status and manual synchronization.

### 📝 Manual Requests

Manual backup request creation and lifecycle management.

---

## 🧪 Development Commands

### ⚙️ Backend

```powershell
dotnet build .\SimulatorBackupControl.sln
dotnet run --project .\backend\Sbc.Api
dotnet test
```

### 🎨 Frontend

```powershell
cd frontend

npm install
npm run dev
npm run build
```

---

## 🏁 Suggested Development Workflow

1. 🐘 Start PostgreSQL.
2. 💾 Start UrBackup Server.
3. ⚙️ Run the backend API.
4. 🎨 Run the frontend.
5. 🌐 Open the frontend:

```txt
http://localhost:5173
```

6. 🔄 Verify UrBackup status from the UrBackup page.
7. 🖱️ Run manual sync if needed.
8. 🖥️ Review systems.
9. 🚨 Review alerts.
10. 💾 Review backup status.
11. 📝 Manage manual backup requests.

---

## 🧩 Current Capabilities

* 🔄 UrBackup health monitoring
* 🧾 UrBackup raw status inspection
* ⏱️ Automatic client synchronization
* 🖱️ Manual client synchronization
* 🖥️ Protected system listing
* 🔍 System detail page
* 📊 Backup summary dashboard
* 💾 Backup status overview
* 🚨 Alert listing and resolution
* 📝 Manual backup request workflow
* 📱 Responsive sidebar-based frontend

---

## 🛣️ Planned Improvements

Possible future improvements include:

* 🔐 Authentication and user roles
* 🧾 Better audit trail for user actions
* 🔍 Advanced filtering and sorting in all tables
* 📄 Pagination for large environments
* 📎 Manual backup evidence attachments
* 🗃️ Backup retention policy visibility
* ♻️ Restore workflow tracking
* 📧 Email or Teams notifications
* 🚨 More granular alert severity rules
* 📤 Export to CSV or PDF
* 🧪 Integration tests for UrBackup sync
* 🧭 End-to-end frontend tests

---

## 🧱 Git Commit Example

```txt
feat: redesign frontend and add backup monitoring workflows

- Add routed frontend layout with sidebar navigation
- Add Overview, Systems, Alerts, Backups, UrBackup and Manual Requests pages
- Add system detail route
- Add reusable frontend components
- Add UrBackup sync status and manual sync UI
- Add manual backup request lifecycle UI
- Refactor CSS with reusable variables and grouped sections
```

---

## 🔒 License

Internal project.

Usage and distribution are restricted to the organization maintaining SBC.

---

## 📍 Project Status

SBC is currently under active development.

The current version focuses on:

* 🔄 UrBackup integration
* 💾 Backup monitoring
* 🚨 Alert tracking
* 🖥️ Protected system visibility
* 📝 Manual backup workflows

Additional hardening, authentication and operational features should be added before production deployment.
