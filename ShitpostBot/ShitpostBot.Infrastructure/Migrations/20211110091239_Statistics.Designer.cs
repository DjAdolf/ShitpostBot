﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ShitpostBot.Infrastructure;

#nullable disable

namespace ShitpostBot.Infrastructure.Migrations
{
    [DbContext(typeof(ShitpostBotDbContext))]
    [Migration("20211110091239_Statistics")]
    partial class Statistics
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.0-rc.2.21480.5")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("ShitpostBot.Domain.Post", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"), 1L, 1);

                    b.Property<decimal>("ChatChannelId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<decimal>("ChatGuildId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<decimal>("ChatMessageId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTimeOffset?>("EvaluatedOn")
                        .HasColumnType("datetimeoffset");

                    b.Property<DateTimeOffset>("PostedOn")
                        .HasColumnType("datetimeoffset");

                    b.Property<decimal>("PosterId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<DateTimeOffset>("TrackedOn")
                        .HasColumnType("datetimeoffset");

                    b.Property<int>("Type")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("PostedOn");

                    b.HasIndex("PosterId");

                    b.ToTable("Post");

                    b.HasDiscriminator<int>("Type");
                });

            modelBuilder.Entity("ShitpostBot.Domain.ImagePost", b =>
                {
                    b.HasBaseType("ShitpostBot.Domain.Post");

                    b.HasDiscriminator().HasValue(0);
                });

            modelBuilder.Entity("ShitpostBot.Domain.LinkPost", b =>
                {
                    b.HasBaseType("ShitpostBot.Domain.Post");

                    b.HasDiscriminator().HasValue(1);
                });

            modelBuilder.Entity("ShitpostBot.Domain.Post", b =>
                {
                    b.OwnsOne("ShitpostBot.Domain.PostStatistics", "Statistics", b1 =>
                        {
                            b1.Property<long>("PostId")
                                .HasColumnType("bigint");

                            b1.Property<bool>("Placeholder")
                                .HasColumnType("bit");

                            b1.HasKey("PostId");

                            b1.ToTable("Post");

                            b1.WithOwner()
                                .HasForeignKey("PostId");

                            b1.OwnsOne("ShitpostBot.Domain.PostStatisticsMostSimilarTo", "MostSimilarTo", b2 =>
                                {
                                    b2.Property<long>("PostStatisticsPostId")
                                        .HasColumnType("bigint");

                                    b2.Property<long>("SimilarToPostId")
                                        .HasColumnType("bigint");

                                    b2.Property<decimal>("Similarity")
                                        .HasColumnType("decimal(19,17)");

                                    b2.HasKey("PostStatisticsPostId");

                                    b2.ToTable("Post");

                                    b2.WithOwner()
                                        .HasForeignKey("PostStatisticsPostId");
                                });

                            b1.Navigation("MostSimilarTo");
                        });

                    b.Navigation("Statistics");
                });
#pragma warning restore 612, 618
        }
    }
}
