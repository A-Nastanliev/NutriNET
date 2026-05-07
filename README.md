# NutriNET

A free, open-source mobile app for tracking daily nutrition and discovering recipes — built as a learning project exploring full-stack mobile development with .NET.

## Features

- Log meals and track daily calories, protein, carbs and fat
- Browse and search a community food database
- Scan barcodes to instantly log packaged foods
- Create, publish, rate, and comment on recipes
- Share recipes via deep links — tapping the link opens directly in the app
- Follow other users and build saved recipe lists
- Android home screen widgets for calories, protein, carbs, fat and a daily summary
- Full localization support with English and Bulgarian

## Roles

### User
- Request foods to be added (up to 3 pending at a time)
- Apply to become a moderator

### Moderator
- Accept or decline food requests
- Add and edit foods
- Apply temporary comment restrictions to users
- View restriction history

### Admin
- Everything moderators can do
- Delete foods (if unused)
- Delete users
- End restrictions early
- Accept or decline moderator requests
- Demote moderators

## Architecture

Four projects arranged in a one-way dependency chain: `Maui` → `Api` → `Services` → `Data`.

| Project | Role | Key packages |
|---|---|---|
| `NutriNET.Data` | Entities, migrations, DB context | `EntityFrameworkCore`, `MySql.EntityFrameworkCore` |
| `NutriNET.Services` | Business logic | — |
| `NutriNET.Api` | ASP.NET Core REST API | `JwtBearer`, `ImageSharp`, `Resend` |
| `NutriNET.Maui` | Mobile frontend | `CommunityToolkit.Mvvm`, `Syncfusion.Maui.Toolkit`, `ZXing.Net.Maui`, `Microcharts.Maui` |
| `NutriNET.Tests` | Unit tests | `NUnit`, `EF Core InMemory` |

## Configuration

### API — appsettings.json

The API applies migrations automatically on startup. Fill in all fields before running:

```json
{
  "Jwt": {
    "Secret": "",
    "Issuer": "https://api.nutrinet.com",
    "Audience": "NutriNet-api"
  },
  "Share": {
    "Secret": ""
  },
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "App": {
    "BaseUrl": ""
  },
  "Resend": {
    "ApiKey": "",
    "SenderEmail": "onboarding@resend.dev",
    "SenderName": "NutriNET"
  }
}
```

### MAUI — MauiProgram.cs

Set the API base URL in the two `AddHttpClient` calls:

```csharp
client.BaseAddress = new Uri("https://your-api-url");
```

### Android deep linking — MainActivity.cs

Set the API domain in the intent filter so recipe share links open the app correctly:

```csharp
DataHost = "your-api-domain.com"
```