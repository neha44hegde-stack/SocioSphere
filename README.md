# 🏢 SocioSphere — Housing Society Management System

A complete, role-based ASP.NET Core MVC application designed to digitize housing society operations. 

## 🚀 Features
- **Role-Based Access Control (RBAC):** Separate dashboards for Admin, Resident, and Security.
- **Maintenance Billing:** Auto-calculate bills, track payments, and prevent duplicate billing.
- **Amenity Booking:** Overlap detection and booking rules (max duration, hours).
- **Visitor Management:** Gate entry/exit logs and pre-registered expected visitors.
- **Complaints & Notices:** Raise, assign, resolve complaints and publish society notices.
- **Parking & Vehicles:** Allocate slots and register resident vehicles.

## 🛠️ Technology Stack
- **Backend:** C# / ASP.NET Core MVC (.NET 8)
- **Database:** Entity Framework Core (SQLite for local dev, easily swappable to SQL Server)
- **Authentication:** ASP.NET Core Identity
- **Frontend:** Razor Views, Bootstrap 5, Custom CSS
- **Testing:** xUnit

## ⚙️ How to Run Locally
1. Clone the repository
2. Navigate to the folder: `cd SocioSphere`
3. Run the app: `dotnet run --project SocioSphere.Web`
4. Open `http://localhost:5274`

### Default Development Accounts (Password: `Pass@123`)
- **Admin:** `admin@sociosphere.local`
- **Resident:** `resident@sociosphere.local`
- **Security:** `security@sociosphere.local`

## 📸 Screenshots
*(You can add screenshots here later by dragging and dropping images into this file!)*
