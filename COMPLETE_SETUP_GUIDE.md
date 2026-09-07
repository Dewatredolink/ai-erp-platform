# AI-First ERP Platform - Complete Setup Guide

## 📋 Table of Contents
1. [Phase 1: Repository Setup](#phase-1-repository-setup)
2. [Phase 2: Development Environment](#phase-2-development-environment)
3. [Phase 3: Frontend Setup](#phase-3-frontend-setup)
4. [Phase 4: Backend Setup](#phase-4-backend-setup)
5. [Phase 5: Database Setup](#phase-5-database-setup)
6. [Phase 6: Documentation Generation](#phase-6-documentation-generation)
7. [Phase 7: GitHub Project Configuration](#phase-7-github-project-configuration)

---

## Phase 1: Repository Setup

### Step 1.1: Clone the Repository
```bash
# Open terminal/command prompt
cd your-projects-folder
git clone https://github.com/Dewatredolink/ai-erp-platform.git
cd ai-erp-platform
```

### Step 1.2: Verify Repository Structure
```bash
# List folder contents (should show README.md and .gitignore)
ls -la
```

### Step 1.3: Create Main Folder Structure
```bash
# Create core directories
mkdir -p frontend/src frontend/public frontend/components frontend/pages
mkdir -p backend/src backend/api backend/services backend/models
mkdir -p database/migrations database/seeds database/schemas
mkdir -p documentation
mkdir -p devops/docker devops/kubernetes
mkdir -p tests/unit tests/integration tests/e2e
mkdir -p ai-layer/models ai-layer/services
mkdir -p config
mkdir -p scripts
```

**Verification:**
```bash
# Verify all folders created
find . -type d -name "src" -o -name "migrations" -o -name "services"
```

---

## Phase 2: Development Environment Setup

### Step 2.1: Install Node.js and npm
**Windows:**
- Download from https://nodejs.org/ (LTS version)
- Run installer, follow prompts
- Verify: `node --version` and `npm --version`

**Mac:**
```bash
brew install node
```

**Linux:**
```bash
sudo apt-get update
sudo apt-get install nodejs npm
```

### Step 2.2: Install .NET Core SDK
**Windows/Mac/Linux:**
- Download from https://dotnet.microsoft.com/download
- Install .NET 8.0 SDK
- Verify: `dotnet --version`

### Step 2.3: Install Git
**Windows:**
- Download from https://git-scm.com/
- Run installer with defaults

**Mac:**
```bash
brew install git
```

**Linux:**
```bash
sudo apt-get install git
```

### Step 2.4: Install PostgreSQL
**Windows:**
- Download from https://www.postgresql.org/download/windows/
- Install with default settings (remember password for postgres user)
- Default port: 5432

**Mac:**
```bash
brew install postgresql
```

**Linux:**
```bash
sudo apt-get install postgresql postgresql-contrib
```

### Step 2.5: Install Docker (Optional but Recommended)
- Download from https://www.docker.com/products/docker-desktop
- Install and start Docker Desktop
- Verify: `docker --version`

### Step 2.6: Install Visual Studio Code
- Download from https://code.visualstudio.com/
- Install extensions:
  - **ES7+ React/Redux/React-Native snippets**
  - **C# DevKit**
  - **SQL Server (mssql)**
  - **Thunder Client** (API testing)
  - **GitLens**
  - **Docker**

---

## Phase 3: Frontend Setup

### Step 3.1: Initialize React Project
```bash
cd frontend
npx create-react-app . --template typescript
```

### Step 3.2: Install Frontend Dependencies
```bash
# Core dependencies
npm install react-router-dom
npm install axios
npm install zustand  # State management

# UI Components
npm install @mui/material @emotion/react @emotion/styled
npm install @mui/icons-material
npm install react-data-grid

# Forms & Validation
npm install react-hook-form yup

# Charts & Reporting
npm install recharts chart.js react-chartjs-2

# Date & Time
npm install dayjs react-calendar

# Utilities
npm install lodash clsx

# Development
npm install --save-dev @types/react @types/node
npm install --save-dev eslint prettier
```

### Step 3.3: Create Project Structure
```bash
# From frontend directory
mkdir -p src/{components,pages,services,hooks,utils,store,types,layouts}
mkdir -p src/components/{common,forms,tables,charts,dashboard}
mkdir -p src/pages/{sales,purchase,accounting,inventory,pharma}
mkdir -p public/assets/{images,icons,logos}
```

### Step 3.4: Verify Frontend Setup
```bash
npm start
# Browser should open at http://localhost:3000
```

---

## Phase 4: Backend Setup (.NET Core)

### Step 4.1: Create .NET Core Project
```bash
cd backend
dotnet new webapi -n ErpApi --use-program-main
cd ErpApi
```

### Step 4.2: Create Solution Structure
```bash
cd ..
dotnet new sln -n ErpPlatform
dotnet sln ErpPlatform.sln add ErpApi/ErpApi.csproj
dotnet new classlib -n ErpApi.Services
dotnet new classlib -n ErpApi.Models
dotnet new classlib -n ErpApi.Data
dotnet sln ErpPlatform.sln add ErpApi.Services/ErpApi.Services.csproj
dotnet sln ErpPlatform.sln add ErpApi.Models/ErpApi.Models.csproj
dotnet sln ErpPlatform.sln add ErpApi.Data/ErpApi.Data.csproj
```

### Step 4.3: Install Backend NuGet Packages
```bash
# Navigate to ErpApi project
cd ErpApi

# Database
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.EntityFrameworkCore.Tools

# Authentication
dotnet add package Microsoft.IdentityModel.Tokens
dotnet add package System.IdentityModel.Tokens.Jwt

# Configuration
dotnet add package Microsoft.Extensions.Configuration
dotnet add package Microsoft.Extensions.Configuration.Json

# Logging
dotnet add package Serilog
dotnet add package Serilog.AspNetCore

# Validation
dotnet add package FluentValidation

# Mapping
dotnet add package AutoMapper
dotnet add package AutoMapper.Extensions.Microsoft.DependencyInjection

# API Documentation
dotnet add package Swashbuckle.AspNetCore
```

### Step 4.4: Create Backend Folder Structure
```bash
# From backend/ErpApi directory
mkdir -p {Controllers,Services,Models,Data,DTOs,Middleware,Utilities,Configurations}
```

### Step 4.5: Verify Backend Setup
```bash
dotnet restore
dotnet build
```

---

## Phase 5: Database Setup

### Step 5.1: Create PostgreSQL Database
```bash
# Using psql (PostgreSQL command line)
psql -U postgres

# In psql terminal:
CREATE DATABASE erp_platform;
CREATE USER erp_user WITH PASSWORD 'ErpUser@123!';
ALTER ROLE erp_user SET client_encoding TO 'utf8';
ALTER ROLE erp_user SET default_transaction_isolation TO 'read committed';
ALTER ROLE erp_user SET default_transaction_deferrable TO on;
GRANT ALL PRIVILEGES ON DATABASE erp_platform TO erp_user;
\q
```

### Step 5.2: Create Connection String
Create file: `backend/ErpApi/appsettings.json`
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=erp_platform;Username=erp_user;Password=ErpUser@123!"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

### Step 5.3: Create Database Context
Create file: `backend/ErpApi.Data/ApplicationDbContext.cs`
```csharp
using Microsoft.EntityFrameworkCore;

namespace ErpApi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
            : base(options)
        {
        }

        // DbSets will be added for each entity
        // public DbSet<Company> Companies { get; set; }
        // public DbSet<Customer> Customers { get; set; }
    }
}
```

### Step 5.4: Run Initial Migration
```bash
cd backend/ErpApi
dotnet ef migrations add InitialCreate -p ../ErpApi.Data/ErpApi.Data.csproj -s .
dotnet ef database update
```

---

## Phase 6: Documentation Generation

### Step 6.1: Create PRD Document
```bash
# From repository root
touch documentation/01_PRD.md
touch documentation/02_ARCHITECTURE.md
touch documentation/03_DATABASE_SCHEMA.md
touch documentation/04_API_DOCUMENTATION.md
touch documentation/05_SECURITY_ARCHITECTURE.md
touch documentation/06_AI_ARCHITECTURE.md
```

### Step 6.2: Create Configuration Files
```bash
# Environment files
touch .env.development
touch .env.production
touch .env.example

# Docker files
touch Dockerfile
touch docker-compose.yml

# Git files
touch .gitignore
```

---

## Phase 7: GitHub Project Configuration

### Step 7.1: Create GitHub Project Board

1. **Go to Repository:**
   - Navigate to https://github.com/Dewatredolink/ai-erp-platform
   - Click "Projects" tab
   - Click "New project"

2. **Create Project:**
   - Name: "ERP Platform Development Roadmap"
   - Template: "Table" or "Board"
   - Create

3. **Add Columns:**
   - Backlog
   - Ready
   - In Progress
   - In Review
   - Done

### Step 7.2: Create Issues for Each Module

```bash
# Sample issue template - repeat for each module
# Title: [MODULE] Feature Name
# Labels: module-name, priority, type
# Description: Detailed requirements
```

**Modules to create issues for:**
- Company Master & Compliance
- Customer & Supplier Master
- Inventory Management
- Purchase Module
- Sales Module
- Accounting Module
- GST Module
- Pharma Module
- Manufacturing Module
- Banking Module
- CRM Module
- HR & Payroll
- Approval Engine
- Invoice Designer
- AI Layer (Copilot)
- Security & Authentication
- Audit Trail
- Reporting

---

## Quick Start Commands Summary

```bash
# 1. Clone and setup
git clone https://github.com/Dewatredolink/ai-erp-platform.git
cd ai-erp-platform

# 2. Create folders
mkdir -p frontend backend database documentation devops tests

# 3. Frontend
cd frontend
npm install
npm start

# 4. Backend (new terminal)
cd backend
dotnet build
dotnet run

# 5. Database (new terminal)
# PostgreSQL should be running
psql -U postgres -f database/init.sql
```

---

## Next Steps After Setup

1. ✅ Complete this guide (Phase 1-7)
2. 📄 Review and customize documentation templates
3. 🔧 Set up CI/CD pipelines
4. 🗂️ Organize GitHub Issues into milestones
5. 👥 Create team structure and role assignments
6. 🚀 Begin Phase 1 development (Company Master)

---

## Troubleshooting

### Node/npm issues
```bash
# Clear npm cache
npm cache clean --force
# Reinstall
npm install
```

### .NET issues
```bash
# Restore packages
dotnet restore
# Clean build
dotnet clean
dotnet build
```

### PostgreSQL connection issues
```bash
# Test connection
psql -U erp_user -d erp_platform -h localhost
```

### Port conflicts
- Frontend default: 3000
- Backend default: 5000
- PostgreSQL default: 5432

Change in configuration if needed.

---

## Support Resources

- **Node.js:** https://nodejs.org/en/docs/
- **.NET:** https://learn.microsoft.com/en-us/dotnet/
- **React:** https://react.dev/
- **PostgreSQL:** https://www.postgresql.org/docs/
- **Material UI:** https://mui.com/

---

**Status:** Setup Guide Ready for Execution
**Last Updated:** 2026-09-07
