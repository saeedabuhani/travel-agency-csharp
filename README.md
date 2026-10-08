# Travel Agency Management System

A full-stack travel agency web application built with **C#, ASP.NET Core MVC (.NET 8)** and **SQL Server (LocalDB)**.
Customers browse and book travel packages; administrators manage packages, users, bookings, reviews and reports.

## Table of contents

1. [Features](#features)
2. [Tech stack](#tech-stack)
3. [Project structure](#project-structure)
4. [Data model](#data-model)
5. [Roles and access control](#roles-and-access-control)
6. [Getting started](#getting-started)
7. [Notes](#notes)

## Features

### For customers
- **Account system** with registration, login/logout and a "blocked account" page (ASP.NET Core Identity; passwords need 8+ characters, upper and lower case, a digit and a symbol; unique email).
- **Browse travel packages** with search, filters (country, package type, price range, start-date range, discounted only) and sorting. Package details show nights, dates, price, discount, age limit and available rooms.
- **Shopping cart**: add packages, remove them, clear the cart and check out several packages at once.
- **Bookings**: reserve a package, pay, view *My Bookings* and cancel. A booking is `Reserved`, `Paid` or `Cancelled`.
- **Payment flow** with a payment form and validation (a simulated payment, no real gateway).
- **Waiting list**: when a package is full, join the queue. When a room frees up, the next user in line is notified and gets a limited time to confirm or decline before the offer moves on.
- **Age limits and booking windows**: packages can restrict bookings by user age (based on date of birth) and by admin-defined booking time frames and cancellation periods.
- **Itinerary**: download a booking itinerary as a PDF and share it with a friend by email.
- **Notifications**: in-app notification center (unread counter, mark as read, delete, clear read) plus email notifications.
- **Trip reminders**: a background service checks hourly and reminds users of upcoming trips (next 30 days).
- **Reviews**: rate and comment on a package, and leave a general website review.

### For administrators
- **Dashboard and analytics** with key figures and charts.
- **Packages (CRUD)**: create, edit, delete, show/hide, add rooms, and set discounts with start/end dates.
- **Users**: list users, promote or demote admins, block/unblock (with a reason) and delete users.
- **Bookings**: view all bookings.
- **Waiting list management**: view queues, notify waiting users, remove entries.
- **Booking time frames**: create, edit and delete time frames and cancellation periods per package.
- **Reviews**: view package and website reviews.
- **Reports**: export to **PDF** (iText7 and QuestPDF, including a version with charts) and **Excel** (ClosedXML).

## Tech stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core MVC, .NET 8, Razor views |
| Language | C# 12 (nullable reference types enabled) |
| Database | SQL Server LocalDB via Entity Framework Core 8 (Code First, migrations) |
| Auth | ASP.NET Core Identity with roles |
| Reports | QuestPDF, iText7, ClosedXML |
| Frontend | Razor, Bootstrap, jQuery, custom CSS |
| Background work | `IHostedService` (trip reminders) |

## Project structure

```
Travel_Agency.sln
Travel_Agency/
├── Controllers/   Account, Admin, AdminReports, AdminUsers, Bookings, Home,
│                  Notifications, Reviews, ShoppingCart, TravelPackages
├── Models/        AppUser, TravelPackage, Booking, BookingStatus, BookingTimeFrame,
│                  WaitingList, ShoppingCart, Notification, Review, WebsiteReview, ...
├── ViewModels/    Login, Register, Payment, TravelPackageCard
├── Views/         Razor views for every controller + shared layout
├── Services/      WaitingListService, NotificationService, PdfService,
│                  TripReminderService, UserRegistrationHandler
├── Data/          TravelAgencyDbContext, SeedData
├── Migrations/    EF Core migrations
├── wwwroot/       static files (css, js, libraries)
└── Program.cs     service registration, middleware, seeding
```

## Data model

- **AppUser**: extends `IdentityUser` with full name, date of birth and blocked status.
- **TravelPackage**: title, destination, country, type, price, optional discount (with dates), nights, start/end date, available rooms, age limit, image and visibility.
- **Booking**: user, package, date, status, payment time and whether it came from the waiting list.
- **WaitingList**: queue entry per user and package, with position, notification status and an expiry for the offer.
- **ShoppingCart**: items a user intends to book.
- **BookingTimeFrame**: allowed booking and cancellation windows for a package.
- **Notification**: typed in-app messages with read/unread status.
- **Review** and **WebsiteReview**: ratings and comments.

## Roles and access control

| Role | Can do |
|---|---|
| Visitor | Browse packages and view the home page |
| User | Book, pay, cancel, use the cart, waiting list, notifications and reviews |
| Admin | Everything above plus the admin area (`[Authorize(Roles = "Admin")]`) |

On first run, `SeedData` creates the Admin role, an admin account, a few demo users and a set of sample packages (see `Data/SeedData.cs`). These accounts are for local demo use only.

## Getting started

**Prerequisites:** [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0), Visual Studio 2022 (or the `dotnet` CLI) and SQL Server LocalDB (installed with Visual Studio).

```bash
git clone https://github.com/saeedabuhani/travel-agency-csharp.git
cd travel-agency-csharp/Travel_Agency
dotnet restore
dotnet ef database update      # creates the TravelAgencyDB LocalDB database
dotnet run
```

Then open the HTTPS URL printed in the console. The connection string is in `appsettings.json`:

```
Server=(localdb)\mssqllocaldb;Database=TravelAgencyDB;Trusted_Connection=True;
```

With Visual Studio, open `Travel_Agency.sln` and press **F5**.

## Notes

- Payment is simulated for demonstration purposes.
- Email notifications use SMTP. Configure your own credentials before relying on email features.
- Built as an academic project at Sami Shamoon College of Engineering.

## Author

**Saeed Abuhani**: [github.com/saeedabuhani](https://github.com/saeedabuhani)
