# AI ERP Platform - Frontend

A React (Vite) frontend for the AI ERP Platform. It connects to the .NET backend API to
manage companies.

## Features

- **Companies page** – lists all companies in a table (ID, Name) with Add/Edit/Delete actions.
- **Add/Edit Company form** – modal form with validation for creating and updating companies.
- **API service** (`src/services/companyService.js`) – wraps `axios` calls to the backend
  companies endpoints (GET/POST/PUT/DELETE).
- **Navigation header** – app title and links to the available pages.

## Getting Started

### Prerequisites

- Node.js 18+
- The backend API running (defaults to `http://localhost:5079`)

### Setup

```bash
cd frontend
npm install
cp .env.example .env   # adjust VITE_API_URL if your backend runs elsewhere
npm run dev
```

The app will be available at `http://localhost:5173` (Vite's default dev port) and will
communicate with the backend API at the URL configured via `VITE_API_URL`.

### Available Scripts

- `npm run dev` – start the Vite dev server
- `npm run build` – build the production bundle into `dist/`
- `npm run preview` – preview the production build locally
- `npm run lint` – run oxlint

## Configuration

The backend base URL is read from the `VITE_API_URL` environment variable (see
`.env.example`). If not set, it defaults to `http://localhost:5079`.
