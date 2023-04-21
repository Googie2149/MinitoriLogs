﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MinitoriEx;

#nullable disable

namespace MinitoriEx.Migrations
{
    [DbContext(typeof(MessageContext))]
    [Migration("20220819220250_nullables")]
    partial class nullables
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "6.0.8");

            modelBuilder.Entity("MinitoriEx.UserMessage", b =>
                {
                    b.Property<ulong>("Id")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Attachments")
                        .HasColumnType("TEXT");

                    b.Property<ulong>("ChannelId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Content")
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset?>("DeletedTime")
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset?>("EditedTime")
                        .HasColumnType("TEXT");

                    b.Property<ulong>("GuildId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("UserId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Messages", (string)null);
                });
#pragma warning restore 612, 618
        }
    }
}
