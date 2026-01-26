using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HallApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatStatistics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalConversations = table.Column<int>(type: "integer", nullable: false),
                    OpenConversations = table.Column<int>(type: "integer", nullable: false),
                    ResolvedConversations = table.Column<int>(type: "integer", nullable: false),
                    ClosedConversations = table.Column<int>(type: "integer", nullable: false),
                    AverageResponseTime = table.Column<double>(type: "double precision", nullable: false),
                    AverageResolutionTime = table.Column<double>(type: "double precision", nullable: false),
                    CustomerSatisfactionScore = table.Column<double>(type: "double precision", nullable: false),
                    TotalMessages = table.Column<int>(type: "integer", nullable: false),
                    MessagesFromCustomers = table.Column<int>(type: "integer", nullable: false),
                    MessagesFromAgents = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatStatistics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Halls",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    CommercialRegistration = table.Column<long>(type: "bigint", nullable: false),
                    Vat = table.Column<long>(type: "bigint", nullable: false),
                    BothWeekDays = table.Column<double>(type: "numeric(18,2)", nullable: false),
                    BothWeekEnds = table.Column<double>(type: "numeric(18,2)", nullable: false),
                    MaleWeekDays = table.Column<double>(type: "numeric(18,2)", nullable: false),
                    MaleWeekEnds = table.Column<double>(type: "numeric(18,2)", nullable: false),
                    MaleMin = table.Column<int>(type: "integer", nullable: false),
                    MaleMax = table.Column<int>(type: "integer", nullable: false),
                    MaleActive = table.Column<bool>(type: "boolean", nullable: false),
                    FemaleWeekDays = table.Column<double>(type: "numeric(18,2)", nullable: false),
                    FemaleWeekEnds = table.Column<double>(type: "numeric(18,2)", nullable: false),
                    FemaleMin = table.Column<int>(type: "integer", nullable: false),
                    FemaleMax = table.Column<int>(type: "integer", nullable: false),
                    FemaleActive = table.Column<bool>(type: "boolean", nullable: false),
                    Gender = table.Column<int>(type: "integer", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    WhatsApp = table.Column<string>(type: "text", nullable: true),
                    Logo = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    AverageRating = table.Column<double>(type: "double precision", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsApproved = table.Column<bool>(type: "boolean", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    HasSpecialOffer = table.Column<bool>(type: "boolean", nullable: false),
                    IsFeatured = table.Column<bool>(type: "boolean", nullable: false),
                    IsPremium = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Halls", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirstName = table.Column<string>(type: "text", nullable: true),
                    LastName = table.Column<string>(type: "text", nullable: true),
                    Gender = table.Column<string>(type: "text", nullable: true),
                    DOB = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    RefreshToken = table.Column<string>(type: "text", nullable: true),
                    RefreshTokenExpiryTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VendorTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RequiresHallBooking = table.Column<bool>(type: "boolean", nullable: false),
                    AllowsMultipleBookings = table.Column<bool>(type: "boolean", nullable: false),
                    MaxSimultaneousBookings = table.Column<int>(type: "integer", nullable: false),
                    RequiresTimeSlot = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultDuration = table.Column<int>(type: "integer", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Contact",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    HallID = table.Column<int>(type: "integer", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contact", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Contact_Halls_HallID",
                        column: x => x.HallID,
                        principalTable: "Halls",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HallMedia",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Gender = table.Column<int>(type: "integer", nullable: false),
                    MediaType = table.Column<string>(type: "text", nullable: true),
                    URL = table.Column<string>(type: "text", nullable: true),
                    HallID = table.Column<int>(type: "integer", nullable: false),
                    index = table.Column<int>(type: "integer", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HallMedia", x => x.ID);
                    table.ForeignKey(
                        name: "FK_HallMedia_Halls_HallID",
                        column: x => x.HallID,
                        principalTable: "Halls",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Location",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Altitude = table.Column<double>(type: "double precision", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    City = table.Column<string>(type: "text", nullable: true),
                    State = table.Column<string>(type: "text", nullable: true),
                    Address = table.Column<string>(type: "text", nullable: true),
                    HallID = table.Column<int>(type: "integer", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Location", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Location_Halls_HallID",
                        column: x => x.HallID,
                        principalTable: "Halls",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Package",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Price = table.Column<double>(type: "double precision", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    HallID = table.Column<int>(type: "integer", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Package", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Package_Halls_HallID",
                        column: x => x.HallID,
                        principalTable: "Halls",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Service",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    ArabicName = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Gender = table.Column<int>(type: "integer", nullable: false),
                    HallID = table.Column<int>(type: "integer", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Service", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Service_Halls_HallID",
                        column: x => x.HallID,
                        principalTable: "Halls",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<int>(type: "integer", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleClaims_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NumberOfOrders = table.Column<int>(type: "integer", nullable: false),
                    SelectedAddressId = table.Column<int>(type: "integer", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AppUserId = table.Column<int>(type: "integer", nullable: false),
                    CreditMoney = table.Column<int>(type: "integer", nullable: false),
                    Confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Customers_Users_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HallManagers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AppUserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HallManagers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HallManagers_Users_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AppUserId = table.Column<int>(type: "integer", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserClaims_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_UserLogins_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    RoleId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTokens",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_UserTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VendorManagers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AppUserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorManagers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorManagers_Users_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Vendors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: false),
                    WhatsApp = table.Column<string>(type: "text", nullable: true),
                    Website = table.Column<string>(type: "text", nullable: true),
                    LogoUrl = table.Column<string>(type: "text", nullable: true),
                    CoverImageUrl = table.Column<string>(type: "text", nullable: true),
                    CommercialRegistrationNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    VatNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsApproved = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Rating = table.Column<double>(type: "double precision", nullable: false),
                    ReviewCount = table.Column<int>(type: "integer", nullable: false),
                    VendorTypeId = table.Column<int>(type: "integer", nullable: false),
                    HasSpecialOffer = table.Column<bool>(type: "boolean", nullable: false),
                    IsFeatured = table.Column<bool>(type: "boolean", nullable: false),
                    IsPremium = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vendors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vendors_VendorTypes_VendorTypeId",
                        column: x => x.VendorTypeId,
                        principalTable: "VendorTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Addresses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Street = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    State = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ZipCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsMain = table.Column<bool>(type: "boolean", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Addresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Addresses_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bookings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HallId = table.Column<int>(type: "integer", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    PaymentMethod = table.Column<string>(type: "text", nullable: true),
                    Coupon = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: true),
                    Comments = table.Column<string>(type: "text", nullable: true),
                    VisitDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsVisitCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    IsBookingConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    BookingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EventDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: true),
                    GuestCount = table.Column<int>(type: "integer", nullable: false),
                    GenderPreference = table.Column<int>(type: "integer", nullable: false),
                    HallCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    VendorServicesCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TaxRate = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: true),
                    PaymentStatus = table.Column<string>(type: "text", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bookings_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bookings_Halls_HallId",
                        column: x => x.HallId,
                        principalTable: "Halls",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Favorites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    VendorId = table.Column<int>(type: "integer", nullable: false),
                    HallId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Favorites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Favorites_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Favorites_Halls_HallId",
                        column: x => x.HallId,
                        principalTable: "Halls",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HallHallManager",
                columns: table => new
                {
                    HallsID = table.Column<int>(type: "integer", nullable: false),
                    ManagersId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HallHallManager", x => new { x.HallsID, x.ManagersId });
                    table.ForeignKey(
                        name: "FK_HallHallManager_HallManagers_ManagersId",
                        column: x => x.ManagersId,
                        principalTable: "HallManagers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HallHallManager_Halls_HallsID",
                        column: x => x.HallsID,
                        principalTable: "Halls",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsApproved = table.Column<bool>(type: "boolean", nullable: false),
                    IsFlagged = table.Column<bool>(type: "boolean", nullable: false),
                    RejectionReason = table.Column<string>(type: "text", nullable: true),
                    CustomerId = table.Column<int>(type: "integer", nullable: true),
                    HallId = table.Column<int>(type: "integer", nullable: false),
                    VendorId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reviews_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reviews_Halls_HallId",
                        column: x => x.HallId,
                        principalTable: "Halls",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reviews_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServiceItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ServiceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DiscountedPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VendorId = table.Column<int>(type: "integer", nullable: false),
                    VendorTypeId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceItems_VendorTypes_VendorTypeId",
                        column: x => x.VendorTypeId,
                        principalTable: "VendorTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceItems_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VendorBlockedDates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VendorId = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsRecurring = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorBlockedDates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorBlockedDates_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VendorBusinessHours",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VendorId = table.Column<int>(type: "integer", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    OpenTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    CloseTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    IsClosed = table.Column<bool>(type: "boolean", nullable: false),
                    SpecialNote = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorBusinessHours", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorBusinessHours_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VendorLocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    State = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Country = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Latitude = table.Column<double>(type: "numeric(9,6)", nullable: true),
                    Longitude = table.Column<double>(type: "numeric(9,6)", nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    VendorId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorLocations_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VendorVendorManager",
                columns: table => new
                {
                    ManagersId = table.Column<int>(type: "integer", nullable: false),
                    VendorsId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorVendorManager", x => new { x.ManagersId, x.VendorsId });
                    table.ForeignKey(
                        name: "FK_VendorVendorManager_VendorManagers_ManagersId",
                        column: x => x.ManagersId,
                        principalTable: "VendorManagers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VendorVendorManager_Vendors_VendorsId",
                        column: x => x.VendorsId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BookingPackages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Price = table.Column<double>(type: "double precision", nullable: false),
                    MaxGuests = table.Column<int>(type: "integer", nullable: false),
                    Features = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BookingId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingPackages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingPackages_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChatConversations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BookingId = table.Column<int>(type: "integer", nullable: true),
                    HallId = table.Column<int>(type: "integer", nullable: true),
                    VendorId = table.Column<int>(type: "integer", nullable: true),
                    CustomerId = table.Column<int>(type: "integer", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: false),
                    ConversationType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SupportAgentId = table.Column<int>(type: "integer", nullable: true),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastMessageAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClaimedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CustomerRating = table.Column<int>(type: "integer", nullable: true),
                    CustomerFeedback = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsAutoCloseEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    TotalMessages = table.Column<int>(type: "integer", nullable: false),
                    ResponseTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    ResolutionTime = table.Column<TimeSpan>(type: "interval", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatConversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatConversations_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ChatConversations_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChatConversations_Halls_HallId",
                        column: x => x.HallId,
                        principalTable: "Halls",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ChatConversations_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChatConversations_Users_SupportAgentId",
                        column: x => x.SupportAgentId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ChatConversations_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InvoiceNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    InvoiceType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SupplyDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BookingId = table.Column<int>(type: "integer", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    HallId = table.Column<int>(type: "integer", nullable: true),
                    SellerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SellerVatNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SellerCommercialRegistrationNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SellerAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SellerBuildingNumber = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    SellerStreetName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SellerDistrict = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SellerCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SellerPostalCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    SellerCountryCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    BuyerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BuyerVatNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BuyerAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BuyerCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BuyerPostalCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    BuyerCountryCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    SubtotalBeforeTax = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TaxableAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TaxRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalAmountWithTax = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PaymentMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PaymentStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PaymentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaymentReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Terms = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    QRCode = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    InvoiceHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ZATCA_UUID = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ZATCA_Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ZATCA_SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ZATCA_Response = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsCancelled = table.Column<bool>(type: "boolean", nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PdfPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsPdfGenerated = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoices_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Invoices_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Invoices_Halls_HallId",
                        column: x => x.HallId,
                        principalTable: "Halls",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BookingId = table.Column<int>(type: "integer", nullable: false),
                    CheckoutId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TransactionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PaymentGateway = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PaymentBrand = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StatusDescription = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ResultCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    CardBrand = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Last4Digits = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    CardExpiryDate = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    CardHolder = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    CustomerEmail = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CustomerPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RiskScore = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WebhookPayload = table.Column<string>(type: "nvarchar(MAX)", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsRefunded = table.Column<bool>(type: "boolean", nullable: false),
                    RefundedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RefundAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    RefundReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RefundedBy = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VendorBookings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VendorId = table.Column<int>(type: "integer", nullable: false),
                    BookingId = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    IsPaid = table.Column<bool>(type: "boolean", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaymentMethod = table.Column<string>(type: "text", nullable: true),
                    PaymentStatus = table.Column<string>(type: "text", nullable: true),
                    CancellationReason = table.Column<string>(type: "text", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ServiceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ServiceType = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorBookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorBookings_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VendorBookings_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceItemImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    Alt = table.Column<string>(type: "text", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ServiceItemId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceItemImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceItemImages_ServiceItems_ServiceItemId",
                        column: x => x.ServiceItemId,
                        principalTable: "ServiceItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConversationId = table.Column<int>(type: "integer", nullable: false),
                    SenderId = table.Column<int>(type: "integer", nullable: false),
                    SenderType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    MessageType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AttachmentUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AttachmentName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AttachmentSize = table.Column<long>(type: "bigint", nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsSystemMessage = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMessages_ChatConversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "ChatConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatMessages_Users_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceLineItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InvoiceId = table.Column<int>(type: "integer", nullable: false),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ItemCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SubtotalBeforeTax = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TaxRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TaxCategory = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceLineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceLineItems_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentRefunds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PaymentId = table.Column<int>(type: "integer", nullable: false),
                    RefundAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RefundTransactionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RequestedBy = table.Column<int>(type: "integer", nullable: false),
                    ResponsePayload = table.Column<string>(type: "nvarchar(MAX)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentRefunds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentRefunds_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaymentRefunds_Users_RequestedBy",
                        column: x => x.RequestedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VendorBookingServices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VendorBookingId = table.Column<int>(type: "integer", nullable: false),
                    ServiceItemId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    SpecialInstructions = table.Column<string>(type: "text", nullable: true),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorBookingServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorBookingServices_ServiceItems_ServiceItemId",
                        column: x => x.ServiceItemId,
                        principalTable: "ServiceItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VendorBookingServices_VendorBookings_VendorBookingId",
                        column: x => x.VendorBookingId,
                        principalTable: "VendorBookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_CustomerId",
                table: "Addresses",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingPackages_BookingId",
                table: "BookingPackages",
                column: "BookingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CustomerId",
                table: "Bookings",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_HallId",
                table: "Bookings",
                column: "HallId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatConversations_BookingId",
                table: "ChatConversations",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatConversations_CreatedAt",
                table: "ChatConversations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ChatConversations_CreatedByUserId",
                table: "ChatConversations",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatConversations_CustomerId",
                table: "ChatConversations",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatConversations_HallId",
                table: "ChatConversations",
                column: "HallId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatConversations_Status",
                table: "ChatConversations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ChatConversations_SupportAgentId",
                table: "ChatConversations",
                column: "SupportAgentId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatConversations_VendorId",
                table: "ChatConversations",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ConversationId",
                table: "ChatMessages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_IsRead",
                table: "ChatMessages",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SenderId",
                table: "ChatMessages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SentAt",
                table: "ChatMessages",
                column: "SentAt");

            migrationBuilder.CreateIndex(
                name: "IX_ChatStatistics_Date",
                table: "ChatStatistics",
                column: "Date",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contact_HallID",
                table: "Contact",
                column: "HallID");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_AppUserId",
                table: "Customers",
                column: "AppUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_CustomerId",
                table: "Favorites",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_HallId",
                table: "Favorites",
                column: "HallId");

            migrationBuilder.CreateIndex(
                name: "IX_HallHallManager_ManagersId",
                table: "HallHallManager",
                column: "ManagersId");

            migrationBuilder.CreateIndex(
                name: "IX_HallManagers_AppUserId",
                table: "HallManagers",
                column: "AppUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HallMedia_HallID",
                table: "HallMedia",
                column: "HallID");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLineItems_InvoiceId",
                table: "InvoiceLineItems",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_BookingId",
                table: "Invoices",
                column: "BookingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CustomerId",
                table: "Invoices",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_HallId",
                table: "Invoices",
                column: "HallId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceNumber",
                table: "Invoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_ZATCA_UUID",
                table: "Invoices",
                column: "ZATCA_UUID");

            migrationBuilder.CreateIndex(
                name: "IX_Location_HallID",
                table: "Location",
                column: "HallID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_AppUserId",
                table: "Notifications",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Package_HallID",
                table: "Package",
                column: "HallID");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRefunds_PaymentId",
                table: "PaymentRefunds",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRefunds_RequestedBy",
                table: "PaymentRefunds",
                column: "RequestedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_BookingId",
                table: "Payments",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CheckoutId",
                table: "Payments",
                column: "CheckoutId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CustomerId",
                table: "Payments",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TransactionId",
                table: "Payments",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_CustomerId",
                table: "Reviews",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_HallId",
                table: "Reviews",
                column: "HallId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_VendorId",
                table: "Reviews",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleClaims_RoleId",
                table: "RoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "Roles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Service_HallID",
                table: "Service",
                column: "HallID");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceItemImages_ServiceItemId",
                table: "ServiceItemImages",
                column: "ServiceItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceItems_VendorId",
                table: "ServiceItems",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceItems_VendorTypeId",
                table: "ServiceItems",
                column: "VendorTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_UserClaims_UserId",
                table: "UserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLogins_UserId",
                table: "UserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "Users",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "Users",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendorBlockedDates_VendorId",
                table: "VendorBlockedDates",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorBookings_BookingId",
                table: "VendorBookings",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorBookings_VendorId",
                table: "VendorBookings",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorBookingServices_ServiceItemId",
                table: "VendorBookingServices",
                column: "ServiceItemId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorBookingServices_VendorBookingId",
                table: "VendorBookingServices",
                column: "VendorBookingId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorBusinessHours_VendorId_DayOfWeek",
                table: "VendorBusinessHours",
                columns: new[] { "VendorId", "DayOfWeek" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendorLocations_VendorId",
                table: "VendorLocations",
                column: "VendorId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendorLocations_VendorId_IsPrimary",
                table: "VendorLocations",
                columns: new[] { "VendorId", "IsPrimary" },
                unique: true,
                filter: "[IsPrimary] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_VendorManagers_AppUserId",
                table: "VendorManagers",
                column: "AppUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_VendorTypeId",
                table: "Vendors",
                column: "VendorTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorVendorManager_VendorsId",
                table: "VendorVendorManager",
                column: "VendorsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Addresses");

            migrationBuilder.DropTable(
                name: "BookingPackages");

            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "ChatStatistics");

            migrationBuilder.DropTable(
                name: "Contact");

            migrationBuilder.DropTable(
                name: "Favorites");

            migrationBuilder.DropTable(
                name: "HallHallManager");

            migrationBuilder.DropTable(
                name: "HallMedia");

            migrationBuilder.DropTable(
                name: "InvoiceLineItems");

            migrationBuilder.DropTable(
                name: "Location");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Package");

            migrationBuilder.DropTable(
                name: "PaymentRefunds");

            migrationBuilder.DropTable(
                name: "Reviews");

            migrationBuilder.DropTable(
                name: "RoleClaims");

            migrationBuilder.DropTable(
                name: "Service");

            migrationBuilder.DropTable(
                name: "ServiceItemImages");

            migrationBuilder.DropTable(
                name: "UserClaims");

            migrationBuilder.DropTable(
                name: "UserLogins");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "UserTokens");

            migrationBuilder.DropTable(
                name: "VendorBlockedDates");

            migrationBuilder.DropTable(
                name: "VendorBookingServices");

            migrationBuilder.DropTable(
                name: "VendorBusinessHours");

            migrationBuilder.DropTable(
                name: "VendorLocations");

            migrationBuilder.DropTable(
                name: "VendorVendorManager");

            migrationBuilder.DropTable(
                name: "ChatConversations");

            migrationBuilder.DropTable(
                name: "HallManagers");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "ServiceItems");

            migrationBuilder.DropTable(
                name: "VendorBookings");

            migrationBuilder.DropTable(
                name: "VendorManagers");

            migrationBuilder.DropTable(
                name: "Bookings");

            migrationBuilder.DropTable(
                name: "Vendors");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "Halls");

            migrationBuilder.DropTable(
                name: "VendorTypes");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
