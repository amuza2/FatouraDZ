using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using FatouraDZ.Models;

namespace FatouraDZ.Database;

public class AppDbContext : DbContext
{
    public DbSet<Business> Businesses { get; set; } = null!;
    public DbSet<Facture> Factures { get; set; } = null!;
    public DbSet<LigneFacture> LignesFacture { get; set; } = null!;
    public DbSet<Configuration> Configurations { get; set; } = null!;

    private readonly string _dbPath;

    public AppDbContext()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FatouraDZ"
        );
        Directory.CreateDirectory(appDataPath);
        _dbPath = Path.Combine(appDataPath, "fatouradz.db");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source={_dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Business>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nom).IsRequired();
            entity.Property(e => e.TypeEntreprise).IsRequired();
            entity.Property(e => e.Adresse).IsRequired();
            entity.Property(e => e.Ville).IsRequired();
            entity.Property(e => e.Wilaya).IsRequired();
            entity.Property(e => e.Telephone).IsRequired();
            entity.HasMany(e => e.Factures)
                  .WithOne(f => f.Business)
                  .HasForeignKey(f => f.BusinessId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Facture>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.NumeroFacture).IsUnique();
            entity.Property(e => e.NumeroFacture).IsRequired();
            entity.Property(e => e.ClientNom).IsRequired();
            entity.Property(e => e.ClientAdresse).IsRequired();
            entity.Property(e => e.ClientTelephone).IsRequired();
            entity.Property(e => e.MontantEnLettres).IsRequired();
            entity.HasMany(e => e.Lignes)
                  .WithOne(l => l.Facture)
                  .HasForeignKey(l => l.FactureId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LigneFacture>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Designation).IsRequired();
        });

        modelBuilder.Entity<Configuration>(entity =>
        {
            entity.HasKey(e => e.Cle);
        });
    }
}
