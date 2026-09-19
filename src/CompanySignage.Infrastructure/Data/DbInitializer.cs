using BCrypt.Net;
using CompanySignage.Domain.Entities;
using CompanySignage.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CompanySignage.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(SignageDbContext context, string adminPassword)
    {
        await context.Database.EnsureCreatedAsync();

        // EnsureCreated mevcut bir veritabanında yeni kolon eklemez. Eski kurulumları
        // veri kaybı olmadan yeni ekran ölçüsü ve görüntüleme ayarlarına yükselt.
        if (context.Database.IsSqlServer())
        {
            await context.Database.ExecuteSqlRawAsync("""
                IF COL_LENGTH('dbo.Screens', 'ScreenWidth') IS NULL
                    ALTER TABLE dbo.Screens ADD ScreenWidth int NOT NULL
                        CONSTRAINT DF_Screens_ScreenWidth DEFAULT 1920;
                IF COL_LENGTH('dbo.Screens', 'ScreenHeight') IS NULL
                    ALTER TABLE dbo.Screens ADD ScreenHeight int NOT NULL
                        CONSTRAINT DF_Screens_ScreenHeight DEFAULT 1080;
                IF COL_LENGTH('dbo.Screens', 'Orientation') IS NULL
                    ALTER TABLE dbo.Screens ADD Orientation nvarchar(20) NOT NULL
                        CONSTRAINT DF_Screens_Orientation DEFAULT N'Landscape';
                IF COL_LENGTH('dbo.Screens', 'DisplayMode') IS NULL
                    ALTER TABLE dbo.Screens ADD DisplayMode nvarchar(20) NOT NULL
                        CONSTRAINT DF_Screens_DisplayMode DEFAULT N'Fill';

                UPDATE dbo.Screens
                SET DisplayMode = N'Fill'
                WHERE DisplayMode <> N'Fill';

                -- Medya URL'leri cihazdan bağımsız olmalı. Eski localhost kayıtlarını da
                -- mevcut Player sürümlerinin indirebileceği göreli biçime getir.
                IF OBJECT_ID('dbo.MediaFiles', 'U') IS NOT NULL
                    UPDATE dbo.MediaFiles
                    SET FileUrl = N'/uploads/' + StoredFileName
                    WHERE StoredFileName IS NOT NULL
                      AND FileUrl <> N'/uploads/' + StoredFileName;
                """);
        }

        if (!await context.Users.AnyAsync())
        {
            var admin = new User
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword, workFactor: 12),
                FullName = "Sistem Yöneticisi",
                Role = UserRole.Admin,
                IsActive = true
            };
            context.Users.Add(admin);

            await context.SaveChangesAsync();
        }
    }
}
