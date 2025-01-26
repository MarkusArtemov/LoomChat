﻿// <auto-generated />
using System;
using De.Hsfl.LoomChat.Chat.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace De.Hsfl.LoomChat.Chat.Migrations
{
    [DbContext(typeof(ChatDbContext))]
    partial class ChatDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("De.Hsfl.LoomChat.Common.Models.Channel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<bool>("IsDmChannel")
                        .HasColumnType("boolean");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.HasKey("Id");

                    b.ToTable("Channels", (string)null);
                });

            modelBuilder.Entity("De.Hsfl.LoomChat.Common.Models.ChannelMember", b =>
                {
                    b.Property<int>("ChannelId")
                        .HasColumnType("integer");

                    b.Property<int>("UserId")
                        .HasColumnType("integer");

                    b.Property<bool>("IsArchived")
                        .HasColumnType("boolean");

                    b.Property<string>("Role")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("ChannelId", "UserId");

                    b.ToTable("ChannelMembers", (string)null);
                });

            modelBuilder.Entity("De.Hsfl.LoomChat.Common.Models.ChatMessage", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("ChannelId")
                        .HasColumnType("integer");

                    b.Property<string>("MessageType")
                        .IsRequired()
                        .HasMaxLength(5)
                        .HasColumnType("character varying(5)");

                    b.Property<int>("SenderUserId")
                        .HasColumnType("integer");

                    b.Property<DateTime>("SentAt")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.HasIndex("ChannelId");

                    b.ToTable("ChatMessages", (string)null);

                    b.HasDiscriminator<string>("MessageType").HasValue("text");

                    b.UseTphMappingStrategy();
                });

            modelBuilder.Entity("De.Hsfl.LoomChat.Common.Models.Poll", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("ChannelId")
                        .HasColumnType("integer");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("CreatedByUserId")
                        .HasColumnType("integer");

                    b.Property<bool>("IsClosed")
                        .HasColumnType("boolean");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.HasKey("Id");

                    b.HasIndex("ChannelId");

                    b.ToTable("Polls", (string)null);
                });

            modelBuilder.Entity("De.Hsfl.LoomChat.Common.Models.PollOption", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("OptionText")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<int>("PollId")
                        .HasColumnType("integer");

                    b.Property<int>("Votes")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("PollId");

                    b.ToTable("PollOptions", (string)null);
                });

            modelBuilder.Entity("De.Hsfl.LoomChat.Common.Models.PollMessage", b =>
                {
                    b.HasBaseType("De.Hsfl.LoomChat.Common.Models.ChatMessage");

                    b.Property<int>("PollId")
                        .HasColumnType("integer");

                    b.HasIndex("PollId");

                    b.HasDiscriminator().HasValue("poll");
                });

            modelBuilder.Entity("De.Hsfl.LoomChat.Common.Models.TextMessage", b =>
                {
                    b.HasBaseType("De.Hsfl.LoomChat.Common.Models.ChatMessage");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasMaxLength(1000)
                        .HasColumnType("character varying(1000)");

                    b.HasDiscriminator().HasValue("text");
                });

            modelBuilder.Entity("De.Hsfl.LoomChat.Common.Models.ChannelMember", b =>
                {
                    b.HasOne("De.Hsfl.LoomChat.Common.Models.Channel", "Channel")
                        .WithMany("ChannelMembers")
                        .HasForeignKey("ChannelId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Channel");
                });

            modelBuilder.Entity("De.Hsfl.LoomChat.Common.Models.ChatMessage", b =>
                {
                    b.HasOne("De.Hsfl.LoomChat.Common.Models.Channel", "Channel")
                        .WithMany("ChatMessages")
                        .HasForeignKey("ChannelId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Channel");
                });

            modelBuilder.Entity("De.Hsfl.LoomChat.Common.Models.Poll", b =>
                {
                    b.HasOne("De.Hsfl.LoomChat.Common.Models.Channel", "Channel")
                        .WithMany("Polls")
                        .HasForeignKey("ChannelId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Channel");
                });

            modelBuilder.Entity("De.Hsfl.LoomChat.Common.Models.PollOption", b =>
                {
                    b.HasOne("De.Hsfl.LoomChat.Common.Models.Poll", "Poll")
                        .WithMany("Options")
                        .HasForeignKey("PollId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Poll");
                });

            modelBuilder.Entity("De.Hsfl.LoomChat.Common.Models.PollMessage", b =>
                {
                    b.HasOne("De.Hsfl.LoomChat.Common.Models.Poll", "Poll")
                        .WithMany()
                        .HasForeignKey("PollId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Poll");
                });

            modelBuilder.Entity("De.Hsfl.LoomChat.Common.Models.Channel", b =>
                {
                    b.Navigation("ChannelMembers");

                    b.Navigation("ChatMessages");

                    b.Navigation("Polls");
                });

            modelBuilder.Entity("De.Hsfl.LoomChat.Common.Models.Poll", b =>
                {
                    b.Navigation("Options");
                });
#pragma warning restore 612, 618
        }
    }
}
