/*
 * File: PromoFilterBotDbContext.cs
 * Project: Models
 * Create Date: Tuesday, 20th October 2026
 * Author: João Lubawaski (joaolubawaski@gmail.com)
 * -----
 * Last Modified: Tuesday, 20th October 2026 4:30:00 pm
 * Modified by: João Lubawaski (joaolubawaski@gmail.com)
 * -----
 * Copyright (c) 2026 João Lubawaski
 * ----
 * HISTORY:
 * Date      	By	Comments
 * ----------	---	---------------------------------------------------------
 */

using System.Diagnostics.CodeAnalysis;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using PromoFilterBot.Domain.Models;

namespace PromoFilterBot.Infrastructure.Models;

/// <summary>
/// The schema is generated from this model by the EF migrations (Migrations/), applied when the app starts.
/// </summary>
[ExcludeFromCodeCoverage]
public class PromoFilterBotDbContext(DbContextOptions<PromoFilterBotDbContext> options) : DbContext(options) {
    public DbSet<User> Users { get; set; } = null!;

    public DbSet<Alert> Alerts { get; set; } = null!;

    public DbSet<Channel> Channels { get; set; } = null!;

    public DbSet<ChannelMessage> ChannelMessages { get; set; } = null!;

    public DbSet<Notification> Notifications { get; set; } = null!;

    public DbSet<ConversationState> ConversationStates { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("public");

        // Deleting a user (/apagarconta) deletes everything that is theirs in the database itself
        modelBuilder.Entity<User>(entity => {
            entity.HasKey(u => u.UserId);
            entity.HasIndex(u => u.UserTelegramChatId).IsUnique();

            entity.HasMany(u => u.Alerts)
                .WithOne(a => a.User)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Alert>(entity => {
            entity.HasKey(a => a.AlertId);
            entity.HasIndex(a => a.AlertIsActive);
        });

        modelBuilder.Entity<Channel>(entity => {
            entity.HasKey(c => c.ChannelId);
            entity.HasIndex(c => c.ChannelTelegramId).IsUnique();
            entity.HasIndex(c => c.ChannelUsername).IsUnique();
            entity.HasIndex(c => c.ChannelStatus);
        });

        modelBuilder.Entity<ChannelMessage>(entity => {
            entity.HasKey(m => m.ChannelMessageId);
            entity.HasIndex(m => new { m.ChannelId, m.ChannelMessageTelegramId }).IsUnique();
            entity.HasIndex(m => m.ChannelMessageReceivedAt);
            entity.HasIndex(m => m.ChannelMessageProductKey);

            entity.HasOne(m => m.Channel)
                .WithMany()
                .HasForeignKey(m => m.ChannelId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Notification>(entity => {
            entity.HasKey(n => n.NotificationId);
            entity.HasIndex(n => n.NotificationStatus);

            // An offer (product x store x price x coupon) reaches a user once a day; another price, coupon or store is
            // another offer. Nulls not distinct, so an offer without a price is sent once too
            entity.HasIndex(n => new {
                n.UserId,
                n.NotificationProductKey,
                n.NotificationStore,
                n.NotificationPrice,
                n.NotificationCoupon,
                n.NotificationOfferDate
            })
                .HasDatabaseName("IX_notification_user_offer_date")
                .IsUnique()
                .AreNullsDistinct(false);

            entity.HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(n => n.Alert)
                .WithMany()
                .HasForeignKey(n => n.AlertId)
                .OnDelete(DeleteBehavior.Cascade);

            // Deleting the old messages (retention) takes their notifications along
            entity.HasOne(n => n.ChannelMessage)
                .WithMany()
                .HasForeignKey(n => n.ChannelMessageId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ConversationState>(entity => {
            entity.HasKey(s => s.UserId);

            entity.HasOne(s => s.User)
                .WithOne()
                .HasForeignKey<ConversationState>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) {
        base.ConfigureConventions(configurationBuilder);

        configurationBuilder
            .Properties<DateTime>()
            .HaveConversion<UtcDateTimeConverter>();

        configurationBuilder
            .Properties<DateTime?>()
            .HaveConversion<UtcDateTimeConverter>();
    }

    public override int SaveChanges() {
        AddTimestamps();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) {
        AddTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void AddTimestamps() {
        IEnumerable<EntityEntry<BaseEntity>> entities = ChangeTracker.Entries<BaseEntity>()
            .Where(x => x.State == EntityState.Added || x.State == EntityState.Modified);

        DateTime now = DateTime.UtcNow;

        foreach (EntityEntry<BaseEntity> entity in entities) {
            if (entity.State == EntityState.Added) {
                entity.Entity.CreationTime = now;
                entity.Entity.UpdateTime = now;
            }
            else {
                entity.Entity.UpdateTime = now;

                entity.Property(x => x.CreationTime).IsModified = false;
            }
        }
    }
}

public class UtcDateTimeConverter : ValueConverter<DateTime, DateTime> {
    public UtcDateTimeConverter()
        : base(
            v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc)) {
    }
}