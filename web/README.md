# Allowance Tracker - React Frontend

This is the React frontend for the Allowance Tracker application, built with Vite and TypeScript.

## Tech Stack

- **React 18** with TypeScript
- **Vite** for fast development and building
- **React Router** for routing
- **Axios** for API calls
- **Tailwind CSS v4** for styling

## Prerequisites

- Node.js 18+
- npm 9+
- .NET API running on `https://localhost:7071` (or update `.env` file)

## Getting Started

### Install Dependencies

```bash
npm install
```

### Environment Configuration

Create or update the `.env` file in the root directory:

```env
VITE_API_URL=https://localhost:7071
```

### Development

Run the development server:

```bash
npm run dev
```

The app will be available at `http://localhost:5173`

### Build for Production

```bash
npm run build
```

The built files will be in the `dist` directory.

### Preview Production Build

```bash
npm run preview
```

## Project Structure

```
web/
├── src/
│   ├── components/         # Reusable components
│   │   ├── ChildCard.tsx
│   │   └── ProtectedRoute.tsx
│   ├── contexts/           # React contexts
│   │   └── AuthContext.tsx
│   ├── pages/              # Page components
│   │   ├── Login.tsx
│   │   ├── Register.tsx
│   │   └── Dashboard.tsx
│   ├── services/           # API client services
│   │   └── api.ts
│   ├── types/              # TypeScript type definitions
│   │   └── index.ts
│   ├── App.tsx             # Main app component with routing
│   ├── main.tsx            # App entry point
│   └── index.css           # Global styles with Tailwind
├── .env                    # Environment variables
├── tailwind.config.js      # Tailwind CSS configuration
├── postcss.config.js       # PostCSS configuration
├── tsconfig.json           # TypeScript configuration
└── vite.config.ts          # Vite configuration
```

## Features

### Authentication
- User registration (parent accounts)
- Login with email and password
- JWT token-based authentication
- Automatic token refresh
- Protected routes

### Dashboard
- View all children in the family
- See current balances for each child
- Quick access to child details and transactions

### API Integration
- Axios client with automatic JWT token injection
- Request/response interceptors
- Error handling with automatic logout on 401
- Type-safe API calls

## Available Routes

- `/login` - Login page
- `/register` - Registration page
- `/dashboard` - Main dashboard (protected)
- `/` - Redirects to dashboard

## Testing with .NET API

1. **Start the .NET API:**
   ```bash
   cd /Users/bryce/Dev/personal/allowance/src/AllowanceTracker
   dotnet run
   ```

2. **Start the React dev server:**
   ```bash
   cd /Users/bryce/Dev/personal/allowance/web
   npm run dev
   ```

3. **Test the flow:**
   - Navigate to `http://localhost:5173`
   - Register a new parent account
   - Login with your credentials
   - View the dashboard (will be empty until children are added)

## CORS Configuration

The .NET API is configured to accept requests from:
- `http://localhost:5173` (Vite dev server)
- `http://localhost:3000` (Create React App fallback)

If you change ports, update the CORS configuration in `Program.cs`.

## Next Steps

- Add child creation form
- Implement transaction history
- Add wish list functionality
- Build analytics dashboard
- Implement real-time updates via SignalR (optional)
