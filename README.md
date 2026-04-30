# NutriNET

A simple, free mobile app for tracking your daily nutrition and discovering recipes. Built as a learning project to explore full-stack mobile development with .NET.

## Architecture

Four projects, one-way dependency chain: `Maui` → `Api` → `Services` → `Data`.

| Project | Role | Key Packages |
|---|---|---|
| `NutriNET.Data` | Entities, migrations, DB context | `Microsoft.EntityFrameworkCore`, `MySql.EntityFrameworkCore` |
| `NutriNET.Services` | All business logic | — |
| `NutriNET.Api` | ASP.NET Core REST API | `JwtBearer`, `SixLabors.ImageSharp`, `Resend` |
| `NutriNET.Maui` | Mobile frontend (Android 6+, iOS 15+) | `CommunityToolkit.Mvvm`, `Syncfusion.Maui.Toolkit`, `ZXing.Net.Maui`, `Microcharts.Maui` |
| `NutriNET.Tests` | Unit tests | `NUnit`, `EF Core InMemory` |

## Requirements

- .NET 10 SDK with the MAUI workload
- MySQL 8+
- A [Resend](https://resend.com) API key

## Features

- Log meals and track daily calories, protein, carbs, and fat
- Browse and search a community food database — or request a new entry
- Create, publish, rate and comment on recipes
- Follow other users and build saved recipe lists
- Scan barcodes to instantly log packaged foods
- Apply to become a moderator and help maintain the database
