﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using WifiLocator.Infrastructure;

#nullable disable

namespace WifiLocator.Infrastructure.Migrations
{
    [DbContext(typeof(WifiLocatorDbContext))]
    [Migration("20250412115532_ChannelAdded")]
    partial class ChannelAdded
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.2")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("WifiLocator.Infrastructure.Entities.AddressEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("City")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Country")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("PostalCode")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Region")
                        .HasColumnType("text");

                    b.Property<string>("Road")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("AddressEntity");
                });

            modelBuilder.Entity("WifiLocator.Infrastructure.Entities.LocationEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<double>("Accuracy")
                        .HasColumnType("double precision");

                    b.Property<int>("Altitude")
                        .HasColumnType("integer");

                    b.Property<string>("EncryptionValue")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("FrequencyMHz")
                        .HasColumnType("integer");

                    b.Property<double>("Latitude")
                        .HasColumnType("double precision");

                    b.Property<double>("Longitude")
                        .HasColumnType("double precision");

                    b.Property<DateTime>("Seen")
                        .HasColumnType("timestamp with time zone");

                    b.Property<double>("SignaldBm")
                        .HasColumnType("double precision");

                    b.Property<bool>("UsedForApproximation")
                        .HasColumnType("boolean");

                    b.Property<Guid>("WifiId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("WifiId");

                    b.ToTable("LocationEntity");
                });

            modelBuilder.Entity("WifiLocator.Infrastructure.Entities.WifiEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid?>("AddressId")
                        .HasColumnType("uuid");

                    b.Property<double?>("ApproximatedLatitude")
                        .HasColumnType("double precision");

                    b.Property<double?>("ApproximatedLongitude")
                        .HasColumnType("double precision");

                    b.Property<string>("Bssid")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("Channel")
                        .HasColumnType("integer");

                    b.Property<string>("Encryption")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Ssid")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<double?>("UncertaintyRadius")
                        .HasColumnType("double precision");

                    b.HasKey("Id");

                    b.HasIndex("AddressId");

                    b.ToTable("WifiEntity");
                });

            modelBuilder.Entity("WifiLocator.Infrastructure.Entities.LocationEntity", b =>
                {
                    b.HasOne("WifiLocator.Infrastructure.Entities.WifiEntity", "Wifi")
                        .WithMany("Locations")
                        .HasForeignKey("WifiId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Wifi");
                });

            modelBuilder.Entity("WifiLocator.Infrastructure.Entities.WifiEntity", b =>
                {
                    b.HasOne("WifiLocator.Infrastructure.Entities.AddressEntity", "Address")
                        .WithMany()
                        .HasForeignKey("AddressId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Address");
                });

            modelBuilder.Entity("WifiLocator.Infrastructure.Entities.WifiEntity", b =>
                {
                    b.Navigation("Locations");
                });
#pragma warning restore 612, 618
        }
    }
}
