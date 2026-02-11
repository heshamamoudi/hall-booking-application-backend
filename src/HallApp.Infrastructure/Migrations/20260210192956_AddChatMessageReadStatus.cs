using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HallApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChatMessageReadStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // BUGFIX: Skip boolean->bit conversions for PostgreSQL as it requires explicit USING clause
            // bit type is SQL Server specific; PostgreSQL uses boolean
            var isPostgreSQL = migrationBuilder.ActiveProvider?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) ?? false;
            var isSqlServer = migrationBuilder.ActiveProvider?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) ?? false;

            migrationBuilder.DropIndex(
                name: "UserNameIndex",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "RoleNameIndex",
                table: "Roles");

            migrationBuilder.AlterColumn<int>(
                name: "VendorsId",
                table: "VendorVendorManager",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "ManagersId",
                table: "VendorVendorManager",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "SortOrder",
                table: "VendorTypes",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "RequiresTimeSlot",
                table: "VendorTypes",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "RequiresHallBooking",
                table: "VendorTypes",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "VendorTypes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            migrationBuilder.AlterColumn<int>(
                name: "MaxSimultaneousBookings",
                table: "VendorTypes",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "VendorTypes",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "VendorTypes",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            migrationBuilder.AlterColumn<int>(
                name: "DefaultDuration",
                table: "VendorTypes",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "AllowsMultipleBookings",
                table: "VendorTypes",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "VendorTypes",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("SqlServer:Identity", "1, 1");

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "WhatsApp",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Website",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            migrationBuilder.AlterColumn<int>(
                name: "VendorTypeId",
                table: "Vendors",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "VatNumber",
                table: "Vendors",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Vendors",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
            }

            migrationBuilder.AlterColumn<int>(
                name: "ReviewCount",
                table: "Vendors",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<double>(
                name: "Rating",
                table: "Vendors",
                type: "float",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision");

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Vendors",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "LogoUrl",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "IsPremium",
                table: "Vendors",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "IsFeatured",
                table: "Vendors",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "IsApproved",
                table: "Vendors",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Vendors",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "HasSpecialOffer",
                table: "Vendors",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Vendors",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Vendors",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "CoverImageUrl",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "CommercialRegistrationNumber",
                table: "Vendors",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "ApprovedAt",
                table: "Vendors",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
            }

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Vendors",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("SqlServer:Identity", "1, 1");

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "VendorManagers",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            migrationBuilder.AlterColumn<int>(
                name: "AppUserId",
                table: "VendorManagers",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "VendorManagers",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<int>(
                name: "VendorId",
                table: "VendorLocations",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "State",
                table: "VendorLocations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "PostalCode",
                table: "VendorLocations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "IsPrimary",
                table: "VendorLocations",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "VendorLocations",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Country",
                table: "VendorLocations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "City",
                table: "VendorLocations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "VendorLocations",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "VendorLocations",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<int>(
                name: "VendorId",
                table: "VendorBusinessHours",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "SpecialNote",
                table: "VendorBusinessHours",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "OpenTime",
                table: "VendorBusinessHours",
                type: "time",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "interval");

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "IsClosed",
                table: "VendorBusinessHours",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            migrationBuilder.AlterColumn<int>(
                name: "DayOfWeek",
                table: "VendorBusinessHours",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "CloseTime",
                table: "VendorBusinessHours",
                type: "time",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "interval");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "VendorBusinessHours",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<int>(
                name: "VendorBookingId",
                table: "VendorBookingServices",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "VendorBookingServices",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "Updated",
                table: "VendorBookingServices",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
            }

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "SpecialInstructions",
                table: "VendorBookingServices",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            migrationBuilder.AlterColumn<int>(
                name: "ServiceItemId",
                table: "VendorBookingServices",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "VendorBookingServices",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "VendorBookingServices",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "VendorBookingServices",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "Created",
                table: "VendorBookingServices",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "VendorBookingServices",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<int>(
                name: "VendorId",
                table: "VendorBookings",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "VendorBookings",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "Updated",
                table: "VendorBookings",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
            }

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "VendorBookings",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "StartTime",
                table: "VendorBookings",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "ServiceType",
                table: "VendorBookings",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "ServiceDate",
                table: "VendorBookings",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "RejectedAt",
                table: "VendorBookings",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
            }

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "PaymentStatus",
                table: "VendorBookings",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "PaymentMethod",
                table: "VendorBookings",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "PaidAt",
                table: "VendorBookings",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
            }

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "VendorBookings",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "IsPaid",
                table: "VendorBookings",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "VendorBookings",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "EndTime",
                table: "VendorBookings",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "VendorBookings",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "Created",
                table: "VendorBookings",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CancelledAt",
                table: "VendorBookings",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
            }

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "CancellationReason",
                table: "VendorBookings",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            migrationBuilder.AlterColumn<int>(
                name: "BookingId",
                table: "VendorBookings",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "ApprovedAt",
                table: "VendorBookings",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
            }

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "VendorBookings",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<int>(
                name: "VendorId",
                table: "VendorBlockedDates",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDate",
                table: "VendorBlockedDates",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "VendorBlockedDates",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "IsRecurring",
                table: "VendorBlockedDates",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDate",
                table: "VendorBlockedDates",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "VendorBlockedDates",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "VendorBlockedDates",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("SqlServer:Identity", "1, 1");

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Value",
                table: "UserTokens",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "UserTokens",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "UserTokens",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "UserTokens",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "UserName",
                table: "Users",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "Updated",
                table: "Users",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "TwoFactorEnabled",
                table: "Users",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "SecurityStamp",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "RefreshTokenExpiryTime",
                table: "Users",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "RefreshToken",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "PhoneNumberConfirmed",
                table: "Users",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "NormalizedUserName",
                table: "Users",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "NormalizedEmail",
                table: "Users",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "LockoutEnd",
                table: "Users",
                type: "datetimeoffset",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);
            }

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "LockoutEnabled",
                table: "Users",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Gender",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "EmailConfirmed",
                table: "Users",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "DOB",
                table: "Users",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "Created",
                table: "Users",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "ConcurrencyStamp",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "Active",
                table: "Users",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            migrationBuilder.AlterColumn<int>(
                name: "AccessFailedCount",
                table: "Users",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Users",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<int>(
                name: "RoleId",
                table: "UserRoles",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "UserRoles",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "UserLogins",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "ProviderDisplayName",
                table: "UserLogins",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "ProviderKey",
                table: "UserLogins",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "UserLogins",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "UserClaims",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "ClaimValue",
                table: "UserClaims",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "ClaimType",
                table: "UserClaims",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "UserClaims",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<int>(
                name: "VendorTypeId",
                table: "ServiceItems",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "VendorId",
                table: "ServiceItems",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "ServiceItems",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
            }

            migrationBuilder.AlterColumn<int>(
                name: "SortOrder",
                table: "ServiceItems",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "ServiceType",
                table: "ServiceItems",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ServiceItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "IsAvailable",
                table: "ServiceItems",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "ServiceItems",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "ServiceItems",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ServiceItems",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "ServiceItems",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "ServiceItems",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<int>(
                name: "SortOrder",
                table: "ServiceItemImages",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "ServiceItemId",
                table: "ServiceItemImages",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "ServiceItemImages",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "ServiceItemImages",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Alt",
                table: "ServiceItemImages",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "ServiceItemImages",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("SqlServer:Identity", "1, 1");

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "Updated",
                table: "Service",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Service",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Service",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            migrationBuilder.AlterColumn<int>(
                name: "HallID",
                table: "Service",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "Gender",
                table: "Service",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "Created",
                table: "Service",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "ArabicName",
                table: "Service",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            migrationBuilder.AlterColumn<int>(
                name: "ID",
                table: "Service",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("SqlServer:Identity", "1, 1");

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "NormalizedName",
                table: "Roles",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Roles",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "ConcurrencyStamp",
                table: "Roles",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Roles",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<int>(
                name: "RoleId",
                table: "RoleClaims",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "ClaimValue",
                table: "RoleClaims",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "ClaimType",
                table: "RoleClaims",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "RoleClaims",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<int>(
                name: "VendorId",
                table: "Reviews",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "Updated",
                table: "Reviews",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "RejectionReason",
                table: "Reviews",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            migrationBuilder.AlterColumn<int>(
                name: "Rating",
                table: "Reviews",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "IsFlagged",
                table: "Reviews",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "IsApproved",
                table: "Reviews",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            migrationBuilder.AlterColumn<int>(
                name: "HallId",
                table: "Reviews",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "CustomerId",
                table: "Reviews",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "Created",
                table: "Reviews",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "Reviews",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Reviews",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("SqlServer:Identity", "1, 1");

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "TransactionId",
                table: "Payments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "StatusDescription",
                table: "Payments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Payments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "RiskScore",
                table: "Payments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "ResultCode",
                table: "Payments",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            migrationBuilder.AlterColumn<int>(
                name: "RefundedBy",
                table: "Payments",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "RefundedAt",
                table: "Payments",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
            }

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "RefundReason",
                table: "Payments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "PaymentGateway",
                table: "Payments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "PaymentBrand",
                table: "Payments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Last4Digits",
                table: "Payments",
                type: "nvarchar(4)",
                maxLength: 4,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(4)",
                oldMaxLength: 4,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "IsRefunded",
                table: "Payments",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "IpAddress",
                table: "Payments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "FailureReason",
                table: "Payments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "FailedAt",
                table: "Payments",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
            }

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "CustomerPhone",
                table: "Payments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            migrationBuilder.AlterColumn<int>(
                name: "CustomerId",
                table: "Payments",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "CustomerEmail",
                table: "Payments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "Payments",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Payments",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CompletedAt",
                table: "Payments",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
            }

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "CheckoutId",
                table: "Payments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "CardHolder",
                table: "Payments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "CardExpiryDate",
                table: "Payments",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(7)",
                oldMaxLength: 7,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "CardBrand",
                table: "Payments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            migrationBuilder.AlterColumn<int>(
                name: "BookingId",
                table: "Payments",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Payments",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("SqlServer:Identity", "1, 1");

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "PaymentRefunds",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            migrationBuilder.AlterColumn<int>(
                name: "RequestedBy",
                table: "PaymentRefunds",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "RefundTransactionId",
                table: "PaymentRefunds",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "PaymentRefunds",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "ProcessedAt",
                table: "PaymentRefunds",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
            }

            migrationBuilder.AlterColumn<int>(
                name: "PaymentId",
                table: "PaymentRefunds",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "PaymentRefunds",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "PaymentRefunds",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("SqlServer:Identity", "1, 1");

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "Updated",
                table: "Package",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDate",
                table: "Package",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            migrationBuilder.AlterColumn<double>(
                name: "Price",
                table: "Package",
                type: "float",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision");

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Package",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Package",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            migrationBuilder.AlterColumn<int>(
                name: "HallID",
                table: "Package",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDate",
                table: "Package",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Package",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "Created",
                table: "Package",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            migrationBuilder.AlterColumn<int>(
                name: "ID",
                table: "Package",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("SqlServer:Identity", "1, 1");

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "Updated",
                table: "Notifications",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
            }

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Notifications",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Notifications",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "ReadAt",
                table: "Notifications",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
            }

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "Notifications",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "IsRead",
                table: "Notifications",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Notifications",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "Created",
                table: "Notifications",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            migrationBuilder.AlterColumn<int>(
                name: "AppUserId",
                table: "Notifications",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Notifications",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("SqlServer:Identity", "1, 1");

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "Updated",
                table: "Location",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "State",
                table: "Location",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            migrationBuilder.AlterColumn<double>(
                name: "Longitude",
                table: "Location",
                type: "float",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AlterColumn<double>(
                name: "Latitude",
                table: "Location",
                type: "float",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AlterColumn<int>(
                name: "HallID",
                table: "Location",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "Created",
                table: "Location",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "City",
                table: "Location",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            migrationBuilder.AlterColumn<double>(
                name: "Altitude",
                table: "Location",
                type: "float",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision");

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Location",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            migrationBuilder.AlterColumn<int>(
                name: "ID",
                table: "Location",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("SqlServer:Identity", "1, 1");

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "ZATCA_UUID",
                table: "Invoices",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "ZATCA_SubmittedAt",
                table: "Invoices",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
            }

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "ZATCA_Status",
                table: "Invoices",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "ZATCA_Response",
                table: "Invoices",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Invoices",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Terms",
                table: "Invoices",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "SupplyDate",
                table: "Invoices",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "SellerVatNumber",
                table: "Invoices",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "SellerStreetName",
                table: "Invoices",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "SellerPostalCode",
                table: "Invoices",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "SellerName",
                table: "Invoices",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "SellerDistrict",
                table: "Invoices",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "SellerCountryCode",
                table: "Invoices",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2)",
                oldMaxLength: 2,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "SellerCommercialRegistrationNumber",
                table: "Invoices",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "SellerCity",
                table: "Invoices",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "SellerBuildingNumber",
                table: "Invoices",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "SellerAddress",
                table: "Invoices",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "QRCode",
                table: "Invoices",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "PdfPath",
                table: "Invoices",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "PaymentStatus",
                table: "Invoices",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "PaymentReference",
                table: "Invoices",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "PaymentMethod",
                table: "Invoices",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "PaymentDate",
                table: "Invoices",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
            }

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "Invoices",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "IsPdfGenerated",
                table: "Invoices",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "IsCancelled",
                table: "Invoices",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "InvoiceType",
                table: "Invoices",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "InvoiceNumber",
                table: "Invoices",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "InvoiceHash",
                table: "Invoices",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "InvoiceDate",
                table: "Invoices",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            migrationBuilder.AlterColumn<int>(
                name: "HallId",
                table: "Invoices",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CustomerId",
                table: "Invoices",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "Invoices",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "Invoices",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Invoices",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CancelledAt",
                table: "Invoices",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
            }

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "CancellationReason",
                table: "Invoices",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "BuyerVatNumber",
                table: "Invoices",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "BuyerPostalCode",
                table: "Invoices",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "BuyerName",
                table: "Invoices",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "BuyerCountryCode",
                table: "Invoices",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2)",
                oldMaxLength: 2,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "BuyerCity",
                table: "Invoices",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "BuyerAddress",
                table: "Invoices",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            migrationBuilder.AlterColumn<int>(
                name: "BookingId",
                table: "Invoices",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Invoices",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("SqlServer:Identity", "1, 1");

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Unit",
                table: "InvoiceLineItems",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "TaxCategory",
                table: "InvoiceLineItems",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            migrationBuilder.AlterColumn<int>(
                name: "LineNumber",
                table: "InvoiceLineItems",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "ItemCode",
                table: "InvoiceLineItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            migrationBuilder.AlterColumn<int>(
                name: "InvoiceId",
                table: "InvoiceLineItems",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "InvoiceLineItems",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "InvoiceLineItems",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("SqlServer:Identity", "1, 1");

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "WhatsApp",
                table: "Halls",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "Updated",
                table: "Halls",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Halls",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Halls",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            migrationBuilder.AlterColumn<int>(
                name: "MaleMin",
                table: "Halls",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "MaleMax",
                table: "Halls",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "MaleActive",
                table: "Halls",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Logo",
                table: "Halls",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "IsPremium",
                table: "Halls",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "IsFeatured",
                table: "Halls",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "IsApproved",
                table: "Halls",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Halls",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "HasSpecialOffer",
                table: "Halls",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            migrationBuilder.AlterColumn<int>(
                name: "Gender",
                table: "Halls",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "FemaleMin",
                table: "Halls",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "FemaleMax",
                table: "Halls",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "FemaleActive",
                table: "Halls",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Halls",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Halls",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "Created",
                table: "Halls",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            migrationBuilder.AlterColumn<double>(
                name: "AverageRating",
                table: "Halls",
                type: "float",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision");

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "ApprovedAt",
                table: "Halls",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
            }

            migrationBuilder.AlterColumn<int>(
                name: "ID",
                table: "Halls",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<int>(
                name: "index",
                table: "HallMedia",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "Updated",
                table: "HallMedia",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "URL",
                table: "HallMedia",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "MediaType",
                table: "HallMedia",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            migrationBuilder.AlterColumn<int>(
                name: "HallID",
                table: "HallMedia",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "Gender",
                table: "HallMedia",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "Created",
                table: "HallMedia",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            migrationBuilder.AlterColumn<int>(
                name: "ID",
                table: "HallMedia",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("SqlServer:Identity", "1, 1");

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "HallManagers",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            migrationBuilder.AlterColumn<int>(
                name: "AppUserId",
                table: "HallManagers",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "HallManagers",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<int>(
                name: "ManagersId",
                table: "HallHallManager",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "HallsID",
                table: "HallHallManager",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "VendorId",
                table: "Favorites",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "HallId",
                table: "Favorites",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "CustomerId",
                table: "Favorites",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Favorites",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Favorites",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("SqlServer:Identity", "1, 1");

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "Updated",
                table: "Customers",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            migrationBuilder.AlterColumn<int>(
                name: "SelectedAddressId",
                table: "Customers",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "NumberOfOrders",
                table: "Customers",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "CreditMoney",
                table: "Customers",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "Created",
                table: "Customers",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "Confirmed",
                table: "Customers",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            migrationBuilder.AlterColumn<int>(
                name: "AppUserId",
                table: "Customers",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "Active",
                table: "Customers",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Customers",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("SqlServer:Identity", "1, 1");

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "Updated",
                table: "Contact",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Contact",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Contact",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            migrationBuilder.AlterColumn<int>(
                name: "HallID",
                table: "Contact",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Contact",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "Created",
                table: "Contact",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            migrationBuilder.AlterColumn<int>(
                name: "ID",
                table: "Contact",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<int>(
                name: "TotalMessages",
                table: "ChatStatistics",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "TotalConversations",
                table: "ChatStatistics",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "ResolvedConversations",
                table: "ChatStatistics",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "OpenConversations",
                table: "ChatStatistics",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "MessagesFromCustomers",
                table: "ChatStatistics",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "MessagesFromAgents",
                table: "ChatStatistics",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "Date",
                table: "ChatStatistics",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            migrationBuilder.AlterColumn<double>(
                name: "CustomerSatisfactionScore",
                table: "ChatStatistics",
                type: "float",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AlterColumn<int>(
                name: "ClosedConversations",
                table: "ChatStatistics",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<double>(
                name: "AverageResponseTime",
                table: "ChatStatistics",
                type: "float",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AlterColumn<double>(
                name: "AverageResolutionTime",
                table: "ChatStatistics",
                type: "float",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "ChatStatistics",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("SqlServer:Identity", "1, 1");

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "SentAt",
                table: "ChatMessages",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "SenderType",
                table: "ChatMessages",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            migrationBuilder.AlterColumn<int>(
                name: "SenderId",
                table: "ChatMessages",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "ReadAt",
                table: "ChatMessages",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
            }

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "MessageType",
                table: "ChatMessages",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "ChatMessages",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "IsSystemMessage",
                table: "ChatMessages",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "IsRead",
                table: "ChatMessages",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "IsDeleted",
                table: "ChatMessages",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "ChatMessages",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
            }

            migrationBuilder.AlterColumn<int>(
                name: "ConversationId",
                table: "ChatMessages",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "AttachmentUrl",
                table: "ChatMessages",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "AttachmentName",
                table: "ChatMessages",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "ChatMessages",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<int>(
                name: "VendorId",
                table: "ChatConversations",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TotalMessages",
                table: "ChatConversations",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "SupportAgentId",
                table: "ChatConversations",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Subject",
                table: "ChatConversations",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ChatConversations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "ResponseTime",
                table: "ChatConversations",
                type: "time",
                nullable: true,
                oldClrType: typeof(TimeSpan),
                oldType: "interval",
                oldNullable: true);

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "ResolvedAt",
                table: "ChatConversations",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
            }

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "ResolutionTime",
                table: "ChatConversations",
                type: "time",
                nullable: true,
                oldClrType: typeof(TimeSpan),
                oldType: "interval",
                oldNullable: true);

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Priority",
                table: "ChatConversations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "LastMessageAt",
                table: "ChatConversations",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
            }

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "IsAutoCloseEnabled",
                table: "ChatConversations",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            migrationBuilder.AlterColumn<int>(
                name: "HallId",
                table: "ChatConversations",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CustomerRating",
                table: "ChatConversations",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CustomerId",
                table: "ChatConversations",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "CustomerFeedback",
                table: "ChatConversations",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            migrationBuilder.AlterColumn<int>(
                name: "CreatedByUserId",
                table: "ChatConversations",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "ChatConversations",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "ConversationType",
                table: "ChatConversations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "ClosedAt",
                table: "ChatConversations",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
            }

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "ClaimedAt",
                table: "ChatConversations",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
            }

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "ChatConversations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            migrationBuilder.AlterColumn<int>(
                name: "BookingId",
                table: "ChatConversations",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "ChatConversations",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("SqlServer:Identity", "1, 1");

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "VisitDate",
                table: "Bookings",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Bookings",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "Updated",
                table: "Bookings",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "StartTime",
                table: "Bookings",
                type: "time",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "interval");

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "PaymentStatus",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "PaymentMethod",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "PaidAt",
                table: "Bookings",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
            }

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "IsVisitCompleted",
                table: "Bookings",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "IsBookingConfirmed",
                table: "Bookings",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            migrationBuilder.AlterColumn<int>(
                name: "HallId",
                table: "Bookings",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "GuestCount",
                table: "Bookings",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "GenderPreference",
                table: "Bookings",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "EventType",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "EventDate",
                table: "Bookings",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "EndTime",
                table: "Bookings",
                type: "time",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "interval");

            migrationBuilder.AlterColumn<int>(
                name: "CustomerId",
                table: "Bookings",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Bookings",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "Created",
                table: "Bookings",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Coupon",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Comments",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "BookingDate",
                table: "Bookings",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Bookings",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("SqlServer:Identity", "1, 1");

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "Updated",
                table: "BookingPackages",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            migrationBuilder.AlterColumn<double>(
                name: "Price",
                table: "BookingPackages",
                type: "float",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision");

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "BookingPackages",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            migrationBuilder.AlterColumn<int>(
                name: "MaxGuests",
                table: "BookingPackages",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "BookingPackages",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Features",
                table: "BookingPackages",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "BookingPackages",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            if (isSqlServer)
            {
            migrationBuilder.AlterColumn<DateTime>(
                name: "Created",
                table: "BookingPackages",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            }

            migrationBuilder.AlterColumn<int>(
                name: "BookingId",
                table: "BookingPackages",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "BookingPackages",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("SqlServer:Identity", "1, 1");

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "ZipCode",
                table: "Addresses",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "Street",
                table: "Addresses",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "State",
                table: "Addresses",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<bool>(
                name: "IsMain",
                table: "Addresses",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
            }
            // else: Skip for PostgreSQL - keep boolean type

            migrationBuilder.AlterColumn<int>(
                name: "CustomerId",
                table: "Addresses",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            
            if (isSqlServer)
            {
                migrationBuilder.AlterColumn<string>(
                name: "City",
                table: "Addresses",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);
            }
            // else: Skip for PostgreSQL - keep character varying/text type

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Addresses",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("SqlServer:Identity", "1, 1");

            // BUGFIX: Create table with provider-specific types
            if (isSqlServer)
            {
                migrationBuilder.CreateTable(
                    name: "ChatMessageReadStatuses",
                    columns: table => new
                    {
                        Id = table.Column<int>(type: "int", nullable: false)
                            .Annotation("SqlServer:Identity", "1, 1"),
                        MessageId = table.Column<int>(type: "int", nullable: false),
                        UserId = table.Column<int>(type: "int", nullable: false),
                        IsRead = table.Column<bool>(type: "bit", nullable: false),
                        ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_ChatMessageReadStatuses", x => x.Id);
                        table.ForeignKey(
                            name: "FK_ChatMessageReadStatuses_ChatMessages_MessageId",
                            column: x => x.MessageId,
                            principalTable: "ChatMessages",
                            principalColumn: "Id",
                            onDelete: ReferentialAction.Cascade);
                        table.ForeignKey(
                            name: "FK_ChatMessageReadStatuses_Users_UserId",
                            column: x => x.UserId,
                            principalTable: "Users",
                            principalColumn: "Id",
                            onDelete: ReferentialAction.Cascade);
                    });
            }
            else
            {
                // PostgreSQL: Let EF Core use provider-native types (serial, boolean, timestamp)
                migrationBuilder.CreateTable(
                    name: "ChatMessageReadStatuses",
                    columns: table => new
                    {
                        Id = table.Column<int>(nullable: false)
                            .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                        MessageId = table.Column<int>(nullable: false),
                        UserId = table.Column<int>(nullable: false),
                        IsRead = table.Column<bool>(nullable: false),
                        ReadAt = table.Column<DateTime>(nullable: true)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_ChatMessageReadStatuses", x => x.Id);
                        table.ForeignKey(
                            name: "FK_ChatMessageReadStatuses_ChatMessages_MessageId",
                            column: x => x.MessageId,
                            principalTable: "ChatMessages",
                            principalColumn: "Id",
                            onDelete: ReferentialAction.Cascade);
                        table.ForeignKey(
                            name: "FK_ChatMessageReadStatuses_Users_UserId",
                            column: x => x.UserId,
                            principalTable: "Users",
                            principalColumn: "Id",
                            onDelete: ReferentialAction.Cascade);
                    });
            }

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "Users",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "Roles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessageReadStatuses_MessageId_UserId",
                table: "ChatMessageReadStatuses",
                columns: new[] { "MessageId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessageReadStatuses_UserId_IsRead",
                table: "ChatMessageReadStatuses",
                columns: new[] { "UserId", "IsRead" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatMessageReadStatuses");

            migrationBuilder.DropIndex(
                name: "UserNameIndex",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "RoleNameIndex",
                table: "Roles");

            migrationBuilder.AlterColumn<int>(
                name: "VendorsId",
                table: "VendorVendorManager",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "ManagersId",
                table: "VendorVendorManager",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "SortOrder",
                table: "VendorTypes",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<bool>(
                name: "RequiresTimeSlot",
                table: "VendorTypes",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "RequiresHallBooking",
                table: "VendorTypes",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "VendorTypes",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<int>(
                name: "MaxSimultaneousBookings",
                table: "VendorTypes",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "VendorTypes",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "VendorTypes",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "DefaultDuration",
                table: "VendorTypes",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "AllowsMultipleBookings",
                table: "VendorTypes",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "VendorTypes",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<string>(
                name: "WhatsApp",
                table: "Vendors",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Website",
                table: "Vendors",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "VendorTypeId",
                table: "Vendors",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "VatNumber",
                table: "Vendors",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Vendors",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ReviewCount",
                table: "Vendors",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<double>(
                name: "Rating",
                table: "Vendors",
                type: "double precision",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Vendors",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Vendors",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "LogoUrl",
                table: "Vendors",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsPremium",
                table: "Vendors",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "IsFeatured",
                table: "Vendors",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "IsApproved",
                table: "Vendors",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Vendors",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "HasSpecialOffer",
                table: "Vendors",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Vendors",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Vendors",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Vendors",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "CoverImageUrl",
                table: "Vendors",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CommercialRegistrationNumber",
                table: "Vendors",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ApprovedAt",
                table: "Vendors",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Vendors",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "VendorManagers",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<int>(
                name: "AppUserId",
                table: "VendorManagers",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "VendorManagers",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<int>(
                name: "VendorId",
                table: "VendorLocations",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "State",
                table: "VendorLocations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PostalCode",
                table: "VendorLocations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsPrimary",
                table: "VendorLocations",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "VendorLocations",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<string>(
                name: "Country",
                table: "VendorLocations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "City",
                table: "VendorLocations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "VendorLocations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "VendorLocations",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<int>(
                name: "VendorId",
                table: "VendorBusinessHours",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "SpecialNote",
                table: "VendorBusinessHours",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "OpenTime",
                table: "VendorBusinessHours",
                type: "interval",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "time");

            migrationBuilder.AlterColumn<bool>(
                name: "IsClosed",
                table: "VendorBusinessHours",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<int>(
                name: "DayOfWeek",
                table: "VendorBusinessHours",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "CloseTime",
                table: "VendorBusinessHours",
                type: "interval",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "time");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "VendorBusinessHours",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<int>(
                name: "VendorBookingId",
                table: "VendorBookingServices",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "VendorBookingServices",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Updated",
                table: "VendorBookingServices",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SpecialInstructions",
                table: "VendorBookingServices",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ServiceItemId",
                table: "VendorBookingServices",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "VendorBookingServices",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "VendorBookingServices",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "VendorBookingServices",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Created",
                table: "VendorBookingServices",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "VendorBookingServices",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<int>(
                name: "VendorId",
                table: "VendorBookings",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "VendorBookings",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Updated",
                table: "VendorBookings",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "VendorBookings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartTime",
                table: "VendorBookings",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "ServiceType",
                table: "VendorBookings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ServiceDate",
                table: "VendorBookings",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "RejectedAt",
                table: "VendorBookings",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentStatus",
                table: "VendorBookings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentMethod",
                table: "VendorBookings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "PaidAt",
                table: "VendorBookings",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "VendorBookings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsPaid",
                table: "VendorBookings",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "VendorBookings",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndTime",
                table: "VendorBookings",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "VendorBookings",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Created",
                table: "VendorBookings",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CancelledAt",
                table: "VendorBookings",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CancellationReason",
                table: "VendorBookings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "BookingId",
                table: "VendorBookings",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ApprovedAt",
                table: "VendorBookings",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "VendorBookings",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<int>(
                name: "VendorId",
                table: "VendorBlockedDates",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDate",
                table: "VendorBlockedDates",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "VendorBlockedDates",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsRecurring",
                table: "VendorBlockedDates",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDate",
                table: "VendorBlockedDates",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "VendorBlockedDates",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "VendorBlockedDates",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<string>(
                name: "Value",
                table: "UserTokens",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "UserTokens",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "UserTokens",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "UserTokens",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "UserName",
                table: "Users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "Updated",
                table: "Users",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<bool>(
                name: "TwoFactorEnabled",
                table: "Users",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<string>(
                name: "SecurityStamp",
                table: "Users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "RefreshTokenExpiryTime",
                table: "Users",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "RefreshToken",
                table: "Users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "PhoneNumberConfirmed",
                table: "Users",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "Users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedUserName",
                table: "Users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedEmail",
                table: "Users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "LockoutEnd",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "LockoutEnabled",
                table: "Users",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "Users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Gender",
                table: "Users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "Users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "EmailConfirmed",
                table: "Users",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DOB",
                table: "Users",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Created",
                table: "Users",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "ConcurrencyStamp",
                table: "Users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "Active",
                table: "Users",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<int>(
                name: "AccessFailedCount",
                table: "Users",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Users",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<int>(
                name: "RoleId",
                table: "UserRoles",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "UserRoles",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "UserLogins",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "ProviderDisplayName",
                table: "UserLogins",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ProviderKey",
                table: "UserLogins",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "UserLogins",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "UserClaims",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "ClaimValue",
                table: "UserClaims",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ClaimType",
                table: "UserClaims",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "UserClaims",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<int>(
                name: "VendorTypeId",
                table: "ServiceItems",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "VendorId",
                table: "ServiceItems",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "ServiceItems",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SortOrder",
                table: "ServiceItems",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "ServiceType",
                table: "ServiceItems",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ServiceItems",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<bool>(
                name: "IsAvailable",
                table: "ServiceItems",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "ServiceItems",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "ServiceItems",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ServiceItems",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "ServiceItems",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "ServiceItems",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<int>(
                name: "SortOrder",
                table: "ServiceItemImages",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "ServiceItemId",
                table: "ServiceItemImages",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "ServiceItemImages",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "ServiceItemImages",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "Alt",
                table: "ServiceItemImages",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "ServiceItemImages",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Updated",
                table: "Service",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Service",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Service",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<int>(
                name: "HallID",
                table: "Service",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "Gender",
                table: "Service",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Created",
                table: "Service",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "ArabicName",
                table: "Service",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ID",
                table: "Service",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedName",
                table: "Roles",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Roles",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ConcurrencyStamp",
                table: "Roles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Roles",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<int>(
                name: "RoleId",
                table: "RoleClaims",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "ClaimValue",
                table: "RoleClaims",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ClaimType",
                table: "RoleClaims",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "RoleClaims",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<int>(
                name: "VendorId",
                table: "Reviews",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "Updated",
                table: "Reviews",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "RejectionReason",
                table: "Reviews",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Rating",
                table: "Reviews",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<bool>(
                name: "IsFlagged",
                table: "Reviews",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "IsApproved",
                table: "Reviews",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<int>(
                name: "HallId",
                table: "Reviews",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "CustomerId",
                table: "Reviews",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "Created",
                table: "Reviews",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "Reviews",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Reviews",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<string>(
                name: "TransactionId",
                table: "Payments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "StatusDescription",
                table: "Payments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Payments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "RiskScore",
                table: "Payments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ResultCode",
                table: "Payments",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "RefundedBy",
                table: "Payments",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "RefundedAt",
                table: "Payments",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RefundReason",
                table: "Payments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentGateway",
                table: "Payments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentBrand",
                table: "Payments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Last4Digits",
                table: "Payments",
                type: "character varying(4)",
                maxLength: 4,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(4)",
                oldMaxLength: 4,
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsRefunded",
                table: "Payments",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<string>(
                name: "IpAddress",
                table: "Payments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FailureReason",
                table: "Payments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "FailedAt",
                table: "Payments",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerPhone",
                table: "Payments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CustomerId",
                table: "Payments",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "CustomerEmail",
                table: "Payments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "Payments",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(3)",
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Payments",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CompletedAt",
                table: "Payments",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CheckoutId",
                table: "Payments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "CardHolder",
                table: "Payments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CardExpiryDate",
                table: "Payments",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(7)",
                oldMaxLength: 7,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CardBrand",
                table: "Payments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "BookingId",
                table: "Payments",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Payments",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "PaymentRefunds",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<int>(
                name: "RequestedBy",
                table: "PaymentRefunds",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "RefundTransactionId",
                table: "PaymentRefunds",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "PaymentRefunds",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ProcessedAt",
                table: "PaymentRefunds",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PaymentId",
                table: "PaymentRefunds",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "PaymentRefunds",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "PaymentRefunds",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Updated",
                table: "Package",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDate",
                table: "Package",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<double>(
                name: "Price",
                table: "Package",
                type: "double precision",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Package",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Package",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<int>(
                name: "HallID",
                table: "Package",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDate",
                table: "Package",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Package",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "Created",
                table: "Package",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<int>(
                name: "ID",
                table: "Package",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Updated",
                table: "Notifications",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Notifications",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Notifications",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ReadAt",
                table: "Notifications",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "Notifications",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<bool>(
                name: "IsRead",
                table: "Notifications",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Notifications",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Created",
                table: "Notifications",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<int>(
                name: "AppUserId",
                table: "Notifications",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Notifications",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Updated",
                table: "Location",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "State",
                table: "Location",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Longitude",
                table: "Location",
                type: "double precision",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<double>(
                name: "Latitude",
                table: "Location",
                type: "double precision",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<int>(
                name: "HallID",
                table: "Location",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Created",
                table: "Location",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "City",
                table: "Location",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Altitude",
                table: "Location",
                type: "double precision",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Location",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ID",
                table: "Location",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<string>(
                name: "ZATCA_UUID",
                table: "Invoices",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ZATCA_SubmittedAt",
                table: "Invoices",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ZATCA_Status",
                table: "Invoices",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ZATCA_Response",
                table: "Invoices",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Invoices",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "Terms",
                table: "Invoices",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "SupplyDate",
                table: "Invoices",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "SellerVatNumber",
                table: "Invoices",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "SellerStreetName",
                table: "Invoices",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SellerPostalCode",
                table: "Invoices",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SellerName",
                table: "Invoices",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "SellerDistrict",
                table: "Invoices",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SellerCountryCode",
                table: "Invoices",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2)",
                oldMaxLength: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SellerCommercialRegistrationNumber",
                table: "Invoices",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "SellerCity",
                table: "Invoices",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SellerBuildingNumber",
                table: "Invoices",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SellerAddress",
                table: "Invoices",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "QRCode",
                table: "Invoices",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PdfPath",
                table: "Invoices",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentStatus",
                table: "Invoices",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentReference",
                table: "Invoices",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentMethod",
                table: "Invoices",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<DateTime>(
                name: "PaymentDate",
                table: "Invoices",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "Invoices",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsPdfGenerated",
                table: "Invoices",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "IsCancelled",
                table: "Invoices",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<string>(
                name: "InvoiceType",
                table: "Invoices",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "InvoiceNumber",
                table: "Invoices",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "InvoiceHash",
                table: "Invoices",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "InvoiceDate",
                table: "Invoices",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<int>(
                name: "HallId",
                table: "Invoices",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CustomerId",
                table: "Invoices",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "Invoices",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(3)",
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "Invoices",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Invoices",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CancelledAt",
                table: "Invoices",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CancellationReason",
                table: "Invoices",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BuyerVatNumber",
                table: "Invoices",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BuyerPostalCode",
                table: "Invoices",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BuyerName",
                table: "Invoices",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "BuyerCountryCode",
                table: "Invoices",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2)",
                oldMaxLength: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BuyerCity",
                table: "Invoices",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BuyerAddress",
                table: "Invoices",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "BookingId",
                table: "Invoices",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Invoices",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<string>(
                name: "Unit",
                table: "InvoiceLineItems",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TaxCategory",
                table: "InvoiceLineItems",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "LineNumber",
                table: "InvoiceLineItems",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "ItemCode",
                table: "InvoiceLineItems",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "InvoiceId",
                table: "InvoiceLineItems",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "InvoiceLineItems",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "InvoiceLineItems",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<string>(
                name: "WhatsApp",
                table: "Halls",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "Updated",
                table: "Halls",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Halls",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Halls",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "MaleMin",
                table: "Halls",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "MaleMax",
                table: "Halls",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<bool>(
                name: "MaleActive",
                table: "Halls",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<string>(
                name: "Logo",
                table: "Halls",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsPremium",
                table: "Halls",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "IsFeatured",
                table: "Halls",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "IsApproved",
                table: "Halls",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Halls",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "HasSpecialOffer",
                table: "Halls",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<int>(
                name: "Gender",
                table: "Halls",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "FemaleMin",
                table: "Halls",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "FemaleMax",
                table: "Halls",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<bool>(
                name: "FemaleActive",
                table: "Halls",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Halls",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Halls",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "Created",
                table: "Halls",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<double>(
                name: "AverageRating",
                table: "Halls",
                type: "double precision",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ApprovedAt",
                table: "Halls",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ID",
                table: "Halls",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<int>(
                name: "index",
                table: "HallMedia",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Updated",
                table: "HallMedia",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "URL",
                table: "HallMedia",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MediaType",
                table: "HallMedia",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "HallID",
                table: "HallMedia",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "Gender",
                table: "HallMedia",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Created",
                table: "HallMedia",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<int>(
                name: "ID",
                table: "HallMedia",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "HallManagers",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<int>(
                name: "AppUserId",
                table: "HallManagers",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "HallManagers",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<int>(
                name: "ManagersId",
                table: "HallHallManager",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "HallsID",
                table: "HallHallManager",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "VendorId",
                table: "Favorites",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "HallId",
                table: "Favorites",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "CustomerId",
                table: "Favorites",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Favorites",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Favorites",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Updated",
                table: "Customers",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<int>(
                name: "SelectedAddressId",
                table: "Customers",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "NumberOfOrders",
                table: "Customers",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "CreditMoney",
                table: "Customers",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Created",
                table: "Customers",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<bool>(
                name: "Confirmed",
                table: "Customers",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<int>(
                name: "AppUserId",
                table: "Customers",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<bool>(
                name: "Active",
                table: "Customers",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Customers",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Updated",
                table: "Contact",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Contact",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Contact",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "HallID",
                table: "Contact",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Contact",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "Created",
                table: "Contact",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<int>(
                name: "ID",
                table: "Contact",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<int>(
                name: "TotalMessages",
                table: "ChatStatistics",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "TotalConversations",
                table: "ChatStatistics",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "ResolvedConversations",
                table: "ChatStatistics",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "OpenConversations",
                table: "ChatStatistics",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "MessagesFromCustomers",
                table: "ChatStatistics",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "MessagesFromAgents",
                table: "ChatStatistics",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Date",
                table: "ChatStatistics",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<double>(
                name: "CustomerSatisfactionScore",
                table: "ChatStatistics",
                type: "double precision",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<int>(
                name: "ClosedConversations",
                table: "ChatStatistics",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<double>(
                name: "AverageResponseTime",
                table: "ChatStatistics",
                type: "double precision",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<double>(
                name: "AverageResolutionTime",
                table: "ChatStatistics",
                type: "double precision",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "ChatStatistics",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SentAt",
                table: "ChatMessages",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "SenderType",
                table: "ChatMessages",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<int>(
                name: "SenderId",
                table: "ChatMessages",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ReadAt",
                table: "ChatMessages",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MessageType",
                table: "ChatMessages",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "ChatMessages",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<bool>(
                name: "IsSystemMessage",
                table: "ChatMessages",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "IsRead",
                table: "ChatMessages",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "IsDeleted",
                table: "ChatMessages",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                table: "ChatMessages",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ConversationId",
                table: "ChatMessages",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "AttachmentUrl",
                table: "ChatMessages",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AttachmentName",
                table: "ChatMessages",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "ChatMessages",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<int>(
                name: "VendorId",
                table: "ChatConversations",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TotalMessages",
                table: "ChatConversations",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "SupportAgentId",
                table: "ChatConversations",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Subject",
                table: "ChatConversations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ChatConversations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "ResponseTime",
                table: "ChatConversations",
                type: "interval",
                nullable: true,
                oldClrType: typeof(TimeSpan),
                oldType: "time",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ResolvedAt",
                table: "ChatConversations",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "ResolutionTime",
                table: "ChatConversations",
                type: "interval",
                nullable: true,
                oldClrType: typeof(TimeSpan),
                oldType: "time",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Priority",
                table: "ChatConversations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastMessageAt",
                table: "ChatConversations",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsAutoCloseEnabled",
                table: "ChatConversations",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<int>(
                name: "HallId",
                table: "ChatConversations",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CustomerRating",
                table: "ChatConversations",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CustomerId",
                table: "ChatConversations",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerFeedback",
                table: "ChatConversations",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CreatedByUserId",
                table: "ChatConversations",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "ChatConversations",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "ConversationType",
                table: "ChatConversations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ClosedAt",
                table: "ChatConversations",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ClaimedAt",
                table: "ChatConversations",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "ChatConversations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<int>(
                name: "BookingId",
                table: "ChatConversations",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "ChatConversations",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<DateTime>(
                name: "VisitDate",
                table: "Bookings",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Bookings",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Updated",
                table: "Bookings",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Bookings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "StartTime",
                table: "Bookings",
                type: "interval",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "time");

            migrationBuilder.AlterColumn<string>(
                name: "PaymentStatus",
                table: "Bookings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentMethod",
                table: "Bookings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "PaidAt",
                table: "Bookings",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsVisitCompleted",
                table: "Bookings",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "IsBookingConfirmed",
                table: "Bookings",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<int>(
                name: "HallId",
                table: "Bookings",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "GuestCount",
                table: "Bookings",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "GenderPreference",
                table: "Bookings",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "EventType",
                table: "Bookings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "EventDate",
                table: "Bookings",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "EndTime",
                table: "Bookings",
                type: "interval",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "time");

            migrationBuilder.AlterColumn<int>(
                name: "CustomerId",
                table: "Bookings",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "Bookings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Bookings",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Created",
                table: "Bookings",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "Coupon",
                table: "Bookings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Comments",
                table: "Bookings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "BookingDate",
                table: "Bookings",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Bookings",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Updated",
                table: "BookingPackages",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<double>(
                name: "Price",
                table: "BookingPackages",
                type: "double precision",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "BookingPackages",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "MaxGuests",
                table: "BookingPackages",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "BookingPackages",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<string>(
                name: "Features",
                table: "BookingPackages",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "BookingPackages",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "Created",
                table: "BookingPackages",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<int>(
                name: "BookingId",
                table: "BookingPackages",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "BookingPackages",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<string>(
                name: "ZipCode",
                table: "Addresses",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Street",
                table: "Addresses",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "State",
                table: "Addresses",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<bool>(
                name: "IsMain",
                table: "Addresses",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<int>(
                name: "CustomerId",
                table: "Addresses",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "City",
                table: "Addresses",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Addresses",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "Users",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "Roles",
                column: "NormalizedName",
                unique: true);
        }
    }
}
