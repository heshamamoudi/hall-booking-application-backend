using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HallApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessFieldsToOrganization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BankAccountNumber",
                table: "Organizations",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BankIban",
                table: "Organizations",
                type: "character varying(34)",
                maxLength: 34,
                nullable: true,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "Organizations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BuildingNumber",
                table: "Organizations",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Organizations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CommercialRegistrationNumber",
                table: "Organizations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "Organizations",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true,
                defaultValue: "SA");

            migrationBuilder.AddColumn<string>(
                name: "District",
                table: "Organizations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Organizations",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "Organizations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LegalName",
                table: "Organizations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Organizations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "Organizations",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StreetName",
                table: "Organizations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Organizations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VatNumber",
                table: "Organizations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedAt",
                table: "Organizations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "Organizations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WhatsApp",
                table: "Organizations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                defaultValue: "");

            // ---------------------------------------------------------------
            // DATA MIGRATION: Copy business info from Vendors to Organizations
            // ---------------------------------------------------------------
            // Strategy: For each VendorManagement organization, pick the first
            // vendor (by lowest Id) that has non-empty values for each field.
            // Vendors already store CR and VAT as strings.
            migrationBuilder.Sql(
                """
                UPDATE "Organizations" o
                SET
                    "CommercialRegistrationNumber" = COALESCE(
                        (SELECT v."CommercialRegistrationNumber"
                         FROM "Vendors" v
                         WHERE v."OrganizationId" = o."Id"
                           AND v."CommercialRegistrationNumber" IS NOT NULL
                           AND v."CommercialRegistrationNumber" <> ''
                         ORDER BY v."Id" ASC
                         LIMIT 1),
                        o."CommercialRegistrationNumber"),
                    "VatNumber" = COALESCE(
                        (SELECT v."VatNumber"
                         FROM "Vendors" v
                         WHERE v."OrganizationId" = o."Id"
                           AND v."VatNumber" IS NOT NULL
                           AND v."VatNumber" <> ''
                         ORDER BY v."Id" ASC
                         LIMIT 1),
                        o."VatNumber"),
                    "Email" = COALESCE(
                        (SELECT v."Email"
                         FROM "Vendors" v
                         WHERE v."OrganizationId" = o."Id"
                           AND v."Email" IS NOT NULL
                           AND v."Email" <> ''
                         ORDER BY v."Id" ASC
                         LIMIT 1),
                        o."Email"),
                    "Phone" = COALESCE(
                        (SELECT v."Phone"
                         FROM "Vendors" v
                         WHERE v."OrganizationId" = o."Id"
                           AND v."Phone" IS NOT NULL
                           AND v."Phone" <> ''
                         ORDER BY v."Id" ASC
                         LIMIT 1),
                        o."Phone"),
                    "WhatsApp" = COALESCE(
                        (SELECT v."WhatsApp"
                         FROM "Vendors" v
                         WHERE v."OrganizationId" = o."Id"
                           AND v."WhatsApp" IS NOT NULL
                           AND v."WhatsApp" <> ''
                         ORDER BY v."Id" ASC
                         LIMIT 1),
                        o."WhatsApp"),
                    "Website" = COALESCE(
                        (SELECT v."Website"
                         FROM "Vendors" v
                         WHERE v."OrganizationId" = o."Id"
                           AND v."Website" IS NOT NULL
                           AND v."Website" <> ''
                         ORDER BY v."Id" ASC
                         LIMIT 1),
                        o."Website"),
                    "UpdatedAt" = NOW()
                WHERE o."Type" = 'VendorManagement'
                  AND EXISTS (
                      SELECT 1 FROM "Vendors" v2
                      WHERE v2."OrganizationId" = o."Id"
                  );
                """);

            // ---------------------------------------------------------------
            // DATA MIGRATION: Copy business info from Halls to Organizations
            // ---------------------------------------------------------------
            // Strategy: For each HallManagement organization, pick the first
            // hall (by lowest ID) that has non-empty/non-zero values.
            // Hall stores CommercialRegistration and Vat as bigint (long),
            // so we cast to text and filter out '0'.
            migrationBuilder.Sql(
                """
                UPDATE "Organizations" o
                SET
                    "CommercialRegistrationNumber" = COALESCE(
                        (SELECT CAST(h."CommercialRegistration" AS TEXT)
                         FROM "Halls" h
                         WHERE h."OrganizationId" = o."Id"
                           AND h."CommercialRegistration" <> 0
                         ORDER BY h."ID" ASC
                         LIMIT 1),
                        o."CommercialRegistrationNumber"),
                    "VatNumber" = COALESCE(
                        (SELECT CAST(h."Vat" AS TEXT)
                         FROM "Halls" h
                         WHERE h."OrganizationId" = o."Id"
                           AND h."Vat" <> 0
                         ORDER BY h."ID" ASC
                         LIMIT 1),
                        o."VatNumber"),
                    "Email" = COALESCE(
                        (SELECT h."Email"
                         FROM "Halls" h
                         WHERE h."OrganizationId" = o."Id"
                           AND h."Email" IS NOT NULL
                           AND h."Email" <> ''
                         ORDER BY h."ID" ASC
                         LIMIT 1),
                        o."Email"),
                    "Phone" = COALESCE(
                        (SELECT h."Phone"
                         FROM "Halls" h
                         WHERE h."OrganizationId" = o."Id"
                           AND h."Phone" IS NOT NULL
                           AND h."Phone" <> ''
                         ORDER BY h."ID" ASC
                         LIMIT 1),
                        o."Phone"),
                    "WhatsApp" = COALESCE(
                        (SELECT h."WhatsApp"
                         FROM "Halls" h
                         WHERE h."OrganizationId" = o."Id"
                           AND h."WhatsApp" IS NOT NULL
                           AND h."WhatsApp" <> ''
                         ORDER BY h."ID" ASC
                         LIMIT 1),
                        o."WhatsApp"),
                    "UpdatedAt" = NOW()
                WHERE o."Type" = 'HallManagement'
                  AND EXISTS (
                      SELECT 1 FROM "Halls" h2
                      WHERE h2."OrganizationId" = o."Id"
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BankAccountNumber",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "BankIban",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "BankName",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "BuildingNumber",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "CommercialRegistrationNumber",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "District",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "LegalName",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "StreetName",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "VatNumber",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "VerifiedAt",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "WhatsApp",
                table: "Organizations");
        }
    }
}
