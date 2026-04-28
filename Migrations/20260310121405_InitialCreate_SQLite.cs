// Project comment: 20260310121405_InitialCreate_SQLite.cs is part of the IT Form application source code.

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace IT_Form.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate_SQLite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Department = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Signature = table.Column<string>(type: "TEXT", nullable: true),
                    LockoutCount = table.Column<int>(type: "INTEGER", nullable: false),
                    MustChangePassword = table.Column<bool>(type: "INTEGER", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: true),
                    SecurityStamp = table.Column<string>(type: "TEXT", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumber = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EntityName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Action = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PerformedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    ActorName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Details = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Changes = table.Column<string>(type: "TEXT", nullable: true),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CatalogRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RequestDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedByEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    EmployeeName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Department = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Branch = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    IsHardwareRequest = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsSoftwareRequest = table.Column<bool>(type: "INTEGER", nullable: false),
                    HardwareRequirements = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    SoftwareRequirements = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Justification = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    HODEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    HODSignature = table.Column<string>(type: "TEXT", nullable: true),
                    HODSignedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ITAdminSignature = table.Column<string>(type: "TEXT", nullable: true),
                    ITAdminSignedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EmployeeSignature = table.Column<string>(type: "TEXT", nullable: true),
                    EmployeeSignedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Lot = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ITAssets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AssetTag = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SerialNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    AssetType = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Model = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Location = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Company = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    PurchaseDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    WarrantyExpiry = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RAM = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Storage = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    WindowsVersion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    DDRType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    CPU = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    LastSeen = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ITAssets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OnboardingRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RequestDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    EmployeeID = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Designation = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Department = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Branch = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DeskNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    JoinDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EmailAddress = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ContactNumber = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    HODEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    OtherGroup = table.Column<bool>(type: "INTEGER", nullable: false),
                    OtherGroupDetails = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    RequesterSignature = table.Column<string>(type: "TEXT", nullable: true),
                    RequesterSignedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ITAdminSignature = table.Column<string>(type: "TEXT", nullable: true),
                    ITAdminSignedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EmployeeSignature = table.Column<string>(type: "TEXT", nullable: true),
                    EmployeeSignedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AccessToken = table.Column<Guid>(type: "TEXT", nullable: false),
                    RejectionReason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    RejectedByUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CheckPhysicalClean = table.Column<bool>(type: "INTEGER", nullable: false),
                    CheckO365Setup = table.Column<bool>(type: "INTEGER", nullable: false),
                    CheckWindowsLogin = table.Column<bool>(type: "INTEGER", nullable: false),
                    CheckEmailSetup = table.Column<bool>(type: "INTEGER", nullable: false),
                    CheckSpecificSoftware = table.Column<bool>(type: "INTEGER", nullable: false),
                    CheckGeneralSoftware = table.Column<bool>(type: "INTEGER", nullable: false),
                    CheckAbcGroup = table.Column<bool>(type: "INTEGER", nullable: false),
                    CheckEmailMailGroup = table.Column<bool>(type: "INTEGER", nullable: false),
                    CheckDomainJoin = table.Column<bool>(type: "INTEGER", nullable: false),
                    CheckSharedDrive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CheckWiFiSetup = table.Column<bool>(type: "INTEGER", nullable: false),
                    CheckPrinterSetup = table.Column<bool>(type: "INTEGER", nullable: false),
                    CheckScanFolder = table.Column<bool>(type: "INTEGER", nullable: false),
                    CheckVPNSetup = table.Column<bool>(type: "INTEGER", nullable: false),
                    ChecklistRemarks = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsPdpaConsented = table.Column<bool>(type: "INTEGER", nullable: false),
                    PdpaConsentedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnboardingRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OtpRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 6, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsUsed = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtpRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportingManagers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Department = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportingManagers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServerFolders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FolderName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerFolders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SlaSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RequestType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TargetDays = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlaSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StandardSoftware",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SoftwareName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsMandatory = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StandardSoftware", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderKey = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeviceVerificationTokens",
                columns: table => new
                {
                    Token = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EmployeeId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    IsVerified = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceVerificationTokens", x => x.Token);
                    table.ForeignKey(
                        name: "FK_DeviceVerificationTokens_AspNetUsers_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserCompanies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    CompanyName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCompanies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserCompanies_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTitles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTitles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserTitles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CompanyId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Departments_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EmailGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GroupName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    GroupEmail = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CompanyId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailGroups_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SharedMailboxes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MailboxName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    MailboxAddress = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CompanyId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharedMailboxes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SharedMailboxes_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ITAssetId = table.Column<int>(type: "INTEGER", nullable: false),
                    ServiceDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ServiceProvider = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Cost = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceLogs_ITAssets_ITAssetId",
                        column: x => x.ITAssetId,
                        principalTable: "ITAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeTransfers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OnboardingRequestId = table.Column<int>(type: "INTEGER", nullable: true),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EmployeeID = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    NewEmployeeID = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    NewEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NewContactNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    FromDepartment = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ToDepartment = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    FromBranch = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ToBranch = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    EffectiveDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    HODEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    CheckADUpdate = table.Column<bool>(type: "INTEGER", nullable: false),
                    CheckEmailSignatureUpdate = table.Column<bool>(type: "INTEGER", nullable: false),
                    CheckFolderPermissionsUpdate = table.Column<bool>(type: "INTEGER", nullable: false),
                    CheckSoftwareUpdate = table.Column<bool>(type: "INTEGER", nullable: false),
                    AssetsReturned = table.Column<bool>(type: "INTEGER", nullable: false),
                    OtherGroup = table.Column<bool>(type: "INTEGER", nullable: false),
                    OtherGroupDetails = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    AssetsReturnedRemarks = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ChecklistRemarks = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    HRSignature = table.Column<string>(type: "TEXT", nullable: true),
                    HRSignedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    HODSignature = table.Column<string>(type: "TEXT", nullable: true),
                    HODSignedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ITAdminSignature = table.Column<string>(type: "TEXT", nullable: true),
                    ITAdminSignedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EmployeeSignature = table.Column<string>(type: "TEXT", nullable: true),
                    EmployeeSignedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AccessToken = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeTransfers_OnboardingRequests_OnboardingRequestId",
                        column: x => x.OnboardingRequestId,
                        principalTable: "OnboardingRequests",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OffboardingRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RequestDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    OnboardingRequestId = table.Column<int>(type: "INTEGER", nullable: true),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EmployeeID = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Department = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Branch = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ProjectCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    LastDayOfWork = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EmailAddress = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    HODEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    AccountDisabledAD = table.Column<bool>(type: "INTEGER", nullable: false),
                    EmailForwarded = table.Column<bool>(type: "INTEGER", nullable: false),
                    ForwardToEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    M365LicenseRevoked = table.Column<bool>(type: "INTEGER", nullable: false),
                    SoftwareLicensesRevoked = table.Column<bool>(type: "INTEGER", nullable: false),
                    LaptopReturned = table.Column<bool>(type: "INTEGER", nullable: false),
                    AccessoriesReturned = table.Column<bool>(type: "INTEGER", nullable: false),
                    HardwareNotes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    HardwareDamageImagePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    MissingItemFines = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    FinesRemarks = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    HRSignature = table.Column<string>(type: "TEXT", nullable: true),
                    HRSignedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    HODSignature = table.Column<string>(type: "TEXT", nullable: true),
                    HODSignedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ITAdminSignature = table.Column<string>(type: "TEXT", nullable: true),
                    ITAdminSignedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EmployeeSignature = table.Column<string>(type: "TEXT", nullable: true),
                    EmployeeSignedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AccessToken = table.Column<Guid>(type: "TEXT", nullable: false),
                    ResignationLetterPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    RejectionReason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    RejectedByUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ClearedLaptop = table.Column<bool>(type: "INTEGER", nullable: false),
                    ClearedMobile = table.Column<bool>(type: "INTEGER", nullable: false),
                    ClearedKeys = table.Column<bool>(type: "INTEGER", nullable: false),
                    ClearedOtherAssets = table.Column<bool>(type: "INTEGER", nullable: false),
                    ClearanceRemarks = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CheckPhysicalClean = table.Column<bool>(type: "INTEGER", nullable: false),
                    CheckO365Deactivate = table.Column<bool>(type: "INTEGER", nullable: false),
                    CheckWindowsDisable = table.Column<bool>(type: "INTEGER", nullable: false),
                    CheckEmailSignOut = table.Column<bool>(type: "INTEGER", nullable: false),
                    CheckSpecificSoftwareDeactivate = table.Column<bool>(type: "INTEGER", nullable: false),
                    CheckGeneralSoftwareDeactivate = table.Column<bool>(type: "INTEGER", nullable: false),
                    CheckAbcGroupRemove = table.Column<bool>(type: "INTEGER", nullable: false),
                    CheckEmailMailGroupRemove = table.Column<bool>(type: "INTEGER", nullable: false),
                    CheckDomainDisjoin = table.Column<bool>(type: "INTEGER", nullable: false),
                    CheckSharedDriveRemove = table.Column<bool>(type: "INTEGER", nullable: false),
                    CheckWiFiDisable = table.Column<bool>(type: "INTEGER", nullable: false),
                    CheckPrinterRemove = table.Column<bool>(type: "INTEGER", nullable: false),
                    CheckScanFolderRemove = table.Column<bool>(type: "INTEGER", nullable: false),
                    CheckVPNDisable = table.Column<bool>(type: "INTEGER", nullable: false),
                    ChecklistRemarks = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    DataBackupDone = table.Column<bool>(type: "INTEGER", nullable: false),
                    EmailBackupDone = table.Column<bool>(type: "INTEGER", nullable: false),
                    EmailForwardingDone = table.Column<bool>(type: "INTEGER", nullable: false),
                    EmailForwardName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    LicenseRemovalDone = table.Column<bool>(type: "INTEGER", nullable: false),
                    FollowUpRemarks = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsPdpaConsented = table.Column<bool>(type: "INTEGER", nullable: false),
                    PdpaConsentedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OffboardingRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OffboardingRequests_OnboardingRequests_OnboardingRequestId",
                        column: x => x.OnboardingRequestId,
                        principalTable: "OnboardingRequests",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SoftwareLicenses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SoftwareName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TotalLicenses = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    StandardSoftwareId = table.Column<int>(type: "INTEGER", nullable: false),
                    SystemMappingKey = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    IsSubscription = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoftwareLicenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SoftwareLicenses_StandardSoftware_StandardSoftwareId",
                        column: x => x.StandardSoftwareId,
                        principalTable: "StandardSoftware",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "FolderPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OnboardingRequestId = table.Column<int>(type: "INTEGER", nullable: true),
                    EmployeeTransferId = table.Column<int>(type: "INTEGER", nullable: true),
                    FolderName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CanRead = table.Column<bool>(type: "INTEGER", nullable: false),
                    CanModify = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FolderPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FolderPermissions_EmployeeTransfers_EmployeeTransferId",
                        column: x => x.EmployeeTransferId,
                        principalTable: "EmployeeTransfers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FolderPermissions_OnboardingRequests_OnboardingRequestId",
                        column: x => x.OnboardingRequestId,
                        principalTable: "OnboardingRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IssuedAssets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OnboardingRequestId = table.Column<int>(type: "INTEGER", nullable: true),
                    EmployeeTransferId = table.Column<int>(type: "INTEGER", nullable: true),
                    ITAssetId = table.Column<int>(type: "INTEGER", nullable: true),
                    AssetType = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    SerialNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    AssetTag = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Qty = table.Column<int>(type: "INTEGER", nullable: true),
                    BrandModel = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssuedAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IssuedAssets_EmployeeTransfers_EmployeeTransferId",
                        column: x => x.EmployeeTransferId,
                        principalTable: "EmployeeTransfers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IssuedAssets_ITAssets_ITAssetId",
                        column: x => x.ITAssetId,
                        principalTable: "ITAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_IssuedAssets_OnboardingRequests_OnboardingRequestId",
                        column: x => x.OnboardingRequestId,
                        principalTable: "OnboardingRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RequestEmailGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OnboardingRequestId = table.Column<int>(type: "INTEGER", nullable: true),
                    EmployeeTransferId = table.Column<int>(type: "INTEGER", nullable: true),
                    EmailGroupId = table.Column<int>(type: "INTEGER", nullable: false),
                    GroupName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    GroupEmail = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestEmailGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestEmailGroups_EmailGroups_EmailGroupId",
                        column: x => x.EmailGroupId,
                        principalTable: "EmailGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequestEmailGroups_EmployeeTransfers_EmployeeTransferId",
                        column: x => x.EmployeeTransferId,
                        principalTable: "EmployeeTransfers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequestEmailGroups_OnboardingRequests_OnboardingRequestId",
                        column: x => x.OnboardingRequestId,
                        principalTable: "OnboardingRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RequestSharedMailboxes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OnboardingRequestId = table.Column<int>(type: "INTEGER", nullable: true),
                    EmployeeTransferId = table.Column<int>(type: "INTEGER", nullable: true),
                    SharedMailboxId = table.Column<int>(type: "INTEGER", nullable: false),
                    MailboxName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    MailboxAddress = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestSharedMailboxes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestSharedMailboxes_EmployeeTransfers_EmployeeTransferId",
                        column: x => x.EmployeeTransferId,
                        principalTable: "EmployeeTransfers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequestSharedMailboxes_OnboardingRequests_OnboardingRequestId",
                        column: x => x.OnboardingRequestId,
                        principalTable: "OnboardingRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequestSharedMailboxes_SharedMailboxes_SharedMailboxId",
                        column: x => x.SharedMailboxId,
                        principalTable: "SharedMailboxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RequestSoftware",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OnboardingRequestId = table.Column<int>(type: "INTEGER", nullable: true),
                    EmployeeTransferId = table.Column<int>(type: "INTEGER", nullable: true),
                    StandardSoftwareId = table.Column<int>(type: "INTEGER", nullable: false),
                    SoftwareName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestSoftware", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestSoftware_EmployeeTransfers_EmployeeTransferId",
                        column: x => x.EmployeeTransferId,
                        principalTable: "EmployeeTransfers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequestSoftware_OnboardingRequests_OnboardingRequestId",
                        column: x => x.OnboardingRequestId,
                        principalTable: "OnboardingRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequestSoftware_StandardSoftware_StandardSoftwareId",
                        column: x => x.StandardSoftwareId,
                        principalTable: "StandardSoftware",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SigningTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Token = table.Column<string>(type: "TEXT", nullable: false),
                    OnboardingRequestId = table.Column<int>(type: "INTEGER", nullable: true),
                    OffboardingRequestId = table.Column<int>(type: "INTEGER", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsUsed = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SigningTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SigningTokens_OffboardingRequests_OffboardingRequestId",
                        column: x => x.OffboardingRequestId,
                        principalTable: "OffboardingRequests",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SigningTokens_OnboardingRequests_OnboardingRequestId",
                        column: x => x.OnboardingRequestId,
                        principalTable: "OnboardingRequests",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "IssuedSoftware",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OnboardingRequestId = table.Column<int>(type: "INTEGER", nullable: true),
                    EmployeeTransferId = table.Column<int>(type: "INTEGER", nullable: true),
                    SoftwareLicenseId = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    SoftwareName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssuedSoftware", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IssuedSoftware_EmployeeTransfers_EmployeeTransferId",
                        column: x => x.EmployeeTransferId,
                        principalTable: "EmployeeTransfers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IssuedSoftware_OnboardingRequests_OnboardingRequestId",
                        column: x => x.OnboardingRequestId,
                        principalTable: "OnboardingRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IssuedSoftware_SoftwareLicenses_SoftwareLicenseId",
                        column: x => x.SoftwareLicenseId,
                        principalTable: "SoftwareLicenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SoftwareKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SoftwareLicenseId = table.Column<int>(type: "INTEGER", nullable: false),
                    KeyValue = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    IsAssigned = table.Column<bool>(type: "INTEGER", nullable: false),
                    OnboardingRequestId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoftwareKeys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SoftwareKeys_OnboardingRequests_OnboardingRequestId",
                        column: x => x.OnboardingRequestId,
                        principalTable: "OnboardingRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SoftwareKeys_SoftwareLicenses_SoftwareLicenseId",
                        column: x => x.SoftwareLicenseId,
                        principalTable: "SoftwareLicenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "SlaSettings",
                columns: new[] { "Id", "RequestType", "TargetDays", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "Onboarding", 3, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, "Offboarding", 3, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, "Catalog", 5, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, "Transfer", 3, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_CompanyId",
                table: "Departments",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceVerificationTokens_EmployeeId",
                table: "DeviceVerificationTokens",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailGroups_CompanyId",
                table: "EmailGroups",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTransfers_OnboardingRequestId",
                table: "EmployeeTransfers",
                column: "OnboardingRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_FolderPermissions_EmployeeTransferId",
                table: "FolderPermissions",
                column: "EmployeeTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_FolderPermissions_OnboardingRequestId",
                table: "FolderPermissions",
                column: "OnboardingRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_IssuedAssets_EmployeeTransferId",
                table: "IssuedAssets",
                column: "EmployeeTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_IssuedAssets_ITAssetId",
                table: "IssuedAssets",
                column: "ITAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_IssuedAssets_OnboardingRequestId",
                table: "IssuedAssets",
                column: "OnboardingRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_IssuedSoftware_EmployeeTransferId",
                table: "IssuedSoftware",
                column: "EmployeeTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_IssuedSoftware_OnboardingRequestId",
                table: "IssuedSoftware",
                column: "OnboardingRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_IssuedSoftware_SoftwareLicenseId",
                table: "IssuedSoftware",
                column: "SoftwareLicenseId");

            migrationBuilder.CreateIndex(
                name: "IX_ITAssets_AssetTag",
                table: "ITAssets",
                column: "AssetTag",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceLogs_ITAssetId",
                table: "MaintenanceLogs",
                column: "ITAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_OffboardingRequests_OnboardingRequestId",
                table: "OffboardingRequests",
                column: "OnboardingRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_OtpRecords_Email_Code",
                table: "OtpRecords",
                columns: new[] { "Email", "Code" });

            migrationBuilder.CreateIndex(
                name: "IX_RequestEmailGroups_EmailGroupId",
                table: "RequestEmailGroups",
                column: "EmailGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestEmailGroups_EmployeeTransferId",
                table: "RequestEmailGroups",
                column: "EmployeeTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestEmailGroups_OnboardingRequestId",
                table: "RequestEmailGroups",
                column: "OnboardingRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestSharedMailboxes_EmployeeTransferId",
                table: "RequestSharedMailboxes",
                column: "EmployeeTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestSharedMailboxes_OnboardingRequestId",
                table: "RequestSharedMailboxes",
                column: "OnboardingRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestSharedMailboxes_SharedMailboxId",
                table: "RequestSharedMailboxes",
                column: "SharedMailboxId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestSoftware_EmployeeTransferId",
                table: "RequestSoftware",
                column: "EmployeeTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestSoftware_OnboardingRequestId",
                table: "RequestSoftware",
                column: "OnboardingRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestSoftware_StandardSoftwareId",
                table: "RequestSoftware",
                column: "StandardSoftwareId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedMailboxes_CompanyId",
                table: "SharedMailboxes",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_SigningTokens_OffboardingRequestId",
                table: "SigningTokens",
                column: "OffboardingRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_SigningTokens_OnboardingRequestId",
                table: "SigningTokens",
                column: "OnboardingRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareKeys_OnboardingRequestId",
                table: "SoftwareKeys",
                column: "OnboardingRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareKeys_SoftwareLicenseId",
                table: "SoftwareKeys",
                column: "SoftwareLicenseId");

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareLicenses_StandardSoftwareId",
                table: "SoftwareLicenses",
                column: "StandardSoftwareId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCompanies_UserId_CompanyName",
                table: "UserCompanies",
                columns: new[] { "UserId", "CompanyName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserTitles_UserId_Title",
                table: "UserTitles",
                columns: new[] { "UserId", "Title" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "CatalogRequests");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "DeviceVerificationTokens");

            migrationBuilder.DropTable(
                name: "FolderPermissions");

            migrationBuilder.DropTable(
                name: "IssuedAssets");

            migrationBuilder.DropTable(
                name: "IssuedSoftware");

            migrationBuilder.DropTable(
                name: "MaintenanceLogs");

            migrationBuilder.DropTable(
                name: "OtpRecords");

            migrationBuilder.DropTable(
                name: "ReportingManagers");

            migrationBuilder.DropTable(
                name: "RequestEmailGroups");

            migrationBuilder.DropTable(
                name: "RequestSharedMailboxes");

            migrationBuilder.DropTable(
                name: "RequestSoftware");

            migrationBuilder.DropTable(
                name: "ServerFolders");

            migrationBuilder.DropTable(
                name: "SigningTokens");

            migrationBuilder.DropTable(
                name: "SlaSettings");

            migrationBuilder.DropTable(
                name: "SoftwareKeys");

            migrationBuilder.DropTable(
                name: "UserCompanies");

            migrationBuilder.DropTable(
                name: "UserTitles");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "ITAssets");

            migrationBuilder.DropTable(
                name: "EmailGroups");

            migrationBuilder.DropTable(
                name: "SharedMailboxes");

            migrationBuilder.DropTable(
                name: "EmployeeTransfers");

            migrationBuilder.DropTable(
                name: "OffboardingRequests");

            migrationBuilder.DropTable(
                name: "SoftwareLicenses");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Companies");

            migrationBuilder.DropTable(
                name: "OnboardingRequests");

            migrationBuilder.DropTable(
                name: "StandardSoftware");
        }
    }
}
