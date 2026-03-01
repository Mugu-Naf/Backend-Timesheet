# Timesheet HR App

A complete .NET 8 Web API for managing employee timesheets, leave requests, attendance, and projects with role-based access control.

## 🚀 Features

- **Authentication & Authorization**: JWT-based security with roles: `Admin`, `HR`, `Employee`.
- **Timesheet Management**: Weekly submission, automatic overtime calculation based on flexible rules.
- **Leave Management**: Request workflow (Pending -> Approved/Rejected) with overlap prevention.
- **Attendance**: Daily check-in/out with automatic half-day detection.
- **Project Tracking**: Manage projects and assign them to timesheets.
- **Global Exception Handling**: Centralized middleware for consistent error responses.
- **Unit Testing**: NUnit + Moq covering all business logic in the service layer.

## 🛠️ Technology Stack

- **Backend**: .NET 8 Web API
- **Database**: SQL Server (EF Core)
- **Mapping**: AutoMapper
- **Testing**: NUnit, Moq, InMemory Database
- **Auth**: JWT Bearer, HMACSHA256 Password Hashing

## 📝 Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (Express or LocalDB)
- [Visual Studio 2022](https://visualstudio.microsoft.com/vs/) (or VS Insiders)

## ⚙️ Setup Instructions

1. **Clone the repository**:
   ```bash
   git clone <your-repo-url>
   cd TimeSheet
   ```

2. **Configure Database**:
   Update the `DefaultConnection` in `FirstAPI/appsettings.json` with your SQL Server connection string.

3. **Run Migrations**:
   Open a terminal in the `FirstAPI` folder and run:
   ```bash
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```

4. **Run Application**:
   - Press `F5` in Visual Studio, or
   - Run `dotnet run --project FirstAPI`

## 🧪 How to Test APIs

### 1. Swagger (Recommended)
Once the app is running, navigate to `https://localhost:<port>/swagger`.
- Use the `/api/Auth/register` endpoint to create a user.
- Use `/api/Auth/login` to get a JWT token.
- Click the **Authorize** button at the top and enter: `Bearer <your-token>`.

### 2. Postman
- **Login**: `POST /api/Auth/login`
- **Headers**: Set `Authorization: Bearer <your-token>` for all protected endpoints.
- **Roles**: 
  - `Admin`: Full access to employees and rules.
  - `HR`: Approval workflow and project management.
  - `Employee`: Manage own timesheets, leave, and attendance.

### 3. Running Unit Tests
Navigate to the root directory and run:
```bash
dotnet test
```

## 📂 Project Structure

- `FirstAPI/`: Main Web API project.
- `FirstAPI.Tests/`: NUnit test project.
- `TimeSheet.sln`: Visual Studio solution file.
- `global.json`: SDK version pin.
