﻿using Microsoft.EntityFrameworkCore;
using Sample.Infrastructure.Entities;

namespace Sample.Infrastructure.EntityFramework;

public class MessageDbContext(DbContextOptions<MessageDbContext> options) : DbContext(options)
{
    public DbSet<MessageRecord> MessageRecords { get; set; }
    public DbSet<OutboxEvent> OutboxEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MessageRecord>().HasKey(m => m.Id);
        modelBuilder.Entity<OutboxEvent>().HasKey(oe => oe.Id);
    }
}