# Attendance.Web

React + TypeScript web client for AnujHRMS.

## Local development

From the repository root:

```powershell
cd src/Attendance.Web
npm install
npm run dev
```

Open http://localhost:5173 while the Attendance.API is running on http://localhost:5048.

The API base URL can be overridden with:

```
VITE_API_BASE_URL=http://server:5048
```

The first screen provides the Phase 1 master-data workflow:
- Organization
- Branch
- Department
- Employee

Authentication, attendance, leave, notifications and device monitoring will be added on top of the same API.
