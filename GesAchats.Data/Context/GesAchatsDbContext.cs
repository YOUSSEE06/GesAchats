using Microsoft.EntityFrameworkCore;
using GesAchats.Core.Entities;

namespace GesAchats.Data.Context;

public class GesAchatsDbContext : DbContext
{
    public GesAchatsDbContext(DbContextOptions<GesAchatsDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Quotation> Quotations => Set<Quotation>();
    public DbSet<QuotationDetail> QuotationDetails => Set<QuotationDetail>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderDetail> PurchaseOrderDetails => Set<PurchaseOrderDetail>();
    public DbSet<DeliveryNote> DeliveryNotes => Set<DeliveryNote>();
    public DbSet<DeliveryNoteDetail> DeliveryNoteDetails => Set<DeliveryNoteDetail>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceDetail> InvoiceDetails => Set<InvoiceDetail>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Need> Needs => Set<Need>();
    public DbSet<NeedDetail> NeedDetails => Set<NeedDetail>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // Cette configuration sera normalement faite via l'injection de dépendances dans le projet WPF
            // Mais on peut mettre une valeur par défaut pour les outils de design (migrations)
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=gesachatsdb;Username=postgres;Password=medpos2025");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuration des relations et contraintes spécifiques (Fluent API)

        // Roles
        modelBuilder.Entity<Role>()
            .HasIndex(r => r.Code)
            .IsUnique();

        // Users
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Login)
            .IsUnique();

        // Products
        modelBuilder.Entity<Product>()
            .Property(p => p.CurrentStock)
            .HasPrecision(18, 2);
        
        modelBuilder.Entity<Product>()
            .Property(p => p.MinimumStock)
            .HasPrecision(18, 2);

        // Quotations
        modelBuilder.Entity<Quotation>()
            .HasIndex(q => q.ReferenceNumber)
            .IsUnique();

        modelBuilder.Entity<QuotationDetail>()
            .Property(q => q.Quantity)
            .HasPrecision(18, 2);

        // PurchaseOrders
        modelBuilder.Entity<PurchaseOrder>()
            .HasIndex(p => p.OrderNumber)
            .IsUnique();

        modelBuilder.Entity<PurchaseOrderDetail>()
            .Property(p => p.Quantity)
            .HasPrecision(18, 2);

        // DeliveryNotes
        modelBuilder.Entity<DeliveryNote>().ToTable("bons_livraison");
        modelBuilder.Entity<DeliveryNote>().Property(d => d.Id).HasColumnName("id");
        modelBuilder.Entity<DeliveryNote>().Property(d => d.DeliveryNumber).HasColumnName("numero_bl").HasMaxLength(50).IsRequired();
        modelBuilder.Entity<DeliveryNote>().Property(d => d.ReceptionDate).HasColumnName("date_reception").IsRequired();
        modelBuilder.Entity<DeliveryNote>().Property(d => d.SupplierId).HasColumnName("fournisseur_id");
        modelBuilder.Entity<DeliveryNote>().Property(d => d.PurchaseOrderId).HasColumnName("bc_id");
        modelBuilder.Entity<DeliveryNote>().Property(d => d.FilePath).HasColumnName("fichier_pdf");
        modelBuilder.Entity<DeliveryNote>().Property(d => d.Observations).HasColumnName("observations");

        modelBuilder.Entity<DeliveryNote>()
            .HasIndex(d => d.DeliveryNumber)
            .IsUnique();

        modelBuilder.Entity<DeliveryNote>()
            .Property(d => d.ReceivedQuantity)
            .HasPrecision(18, 2);

        // Relation BL -> BC
        modelBuilder.Entity<DeliveryNote>()
            .HasOne(d => d.PurchaseOrder)
            .WithMany()
            .HasForeignKey(d => d.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Restrict); 

        // DeliveryNoteDetails
        modelBuilder.Entity<DeliveryNoteDetail>().ToTable("bl_details");
        modelBuilder.Entity<DeliveryNoteDetail>().Property(d => d.Id).HasColumnName("id");
        modelBuilder.Entity<DeliveryNoteDetail>().Property(d => d.DeliveryNoteId).HasColumnName("bl_id");
        modelBuilder.Entity<DeliveryNoteDetail>().Property(d => d.ProductId).HasColumnName("produit_id");
        modelBuilder.Entity<DeliveryNoteDetail>().Property(d => d.QuantityOrdered).HasColumnName("quantite_commandee").HasPrecision(18, 2);
        modelBuilder.Entity<DeliveryNoteDetail>().Property(d => d.QuantityReceived).HasColumnName("quantite_livree").HasPrecision(18, 2);
        modelBuilder.Entity<DeliveryNoteDetail>().Property(d => d.UnitPriceHT).HasColumnName("prix_ht").HasPrecision(18, 2);
        modelBuilder.Entity<DeliveryNoteDetail>().Property(d => d.UnitPriceTTC).HasColumnName("prix_ttc").HasPrecision(18, 2);
        modelBuilder.Entity<DeliveryNoteDetail>().Property(d => d.Total).HasColumnName("total").HasPrecision(18, 2);
        modelBuilder.Entity<DeliveryNoteDetail>().Property(d => d.IsValidated).HasColumnName("valide");

        // Cascade Delete bl_details when bons_livraison is deleted
        modelBuilder.Entity<DeliveryNoteDetail>()
            .HasOne(d => d.DeliveryNote)
            .WithMany(n => n.Details)
            .HasForeignKey(d => d.DeliveryNoteId)
            .OnDelete(DeleteBehavior.Cascade);

        // Invoices
        modelBuilder.Entity<Invoice>().ToTable("factures");
        modelBuilder.Entity<Invoice>().Property(i => i.Id).HasColumnName("id");
        modelBuilder.Entity<Invoice>().Property(i => i.InvoiceNumber).HasColumnName("numero_facture").HasMaxLength(50).IsRequired();
        modelBuilder.Entity<Invoice>().Property(i => i.ExternalInvoiceNumber).HasColumnName("numero_facture_fournisseur").HasMaxLength(100);
        modelBuilder.Entity<Invoice>().Property(i => i.SupplierId).HasColumnName("fournisseur_id");
        modelBuilder.Entity<Invoice>().Property(i => i.PurchaseOrderId).HasColumnName("bc_id");
        modelBuilder.Entity<Invoice>().Property(i => i.DeliveryNoteId).HasColumnName("bl_id");
        modelBuilder.Entity<Invoice>().Property(i => i.InvoiceDate).HasColumnName("date_facture").IsRequired();
        modelBuilder.Entity<Invoice>().Property(i => i.ReceptionDate).HasColumnName("date_reception").IsRequired();
        modelBuilder.Entity<Invoice>().Property(i => i.DueDate).HasColumnName("date_echeance");
        modelBuilder.Entity<Invoice>().Property(i => i.AmountHT).HasColumnName("montant_ht").HasPrecision(18, 2);
        modelBuilder.Entity<Invoice>().Property(i => i.TaxRate).HasColumnName("taux_tva").HasPrecision(18, 2);
        modelBuilder.Entity<Invoice>().Property(i => i.TaxAmount).HasColumnName("montant_tva").HasPrecision(18, 2);
        modelBuilder.Entity<Invoice>().Property(i => i.AmountTTC).HasColumnName("montant_ttc").HasPrecision(18, 2);
        modelBuilder.Entity<Invoice>().Property(i => i.Status).HasColumnName("statut").HasMaxLength(50);
        modelBuilder.Entity<Invoice>().Property(i => i.ConformityStatus).HasColumnName("conformite").HasMaxLength(50);
        modelBuilder.Entity<Invoice>().Property(i => i.ConformityJustification).HasColumnName("justification_conformite");
        modelBuilder.Entity<Invoice>().Property(i => i.Observations).HasColumnName("observations");
        modelBuilder.Entity<Invoice>().Property(i => i.CreatedById).HasColumnName("cree_par");
        modelBuilder.Entity<Invoice>().Property(i => i.CreatedAt).HasColumnName("date_creation");
        modelBuilder.Entity<Invoice>().Property(i => i.UpdatedAt).HasColumnName("date_maj");

        modelBuilder.Entity<Invoice>()
            .HasIndex(i => i.InvoiceNumber)
            .IsUnique();

        // InvoiceDetails
        modelBuilder.Entity<InvoiceDetail>().ToTable("facture_details");
        modelBuilder.Entity<InvoiceDetail>().Property(i => i.Id).HasColumnName("id");
        modelBuilder.Entity<InvoiceDetail>().Property(i => i.InvoiceId).HasColumnName("facture_id");
        modelBuilder.Entity<InvoiceDetail>().Property(i => i.ProductId).HasColumnName("produit_id");
        modelBuilder.Entity<InvoiceDetail>().Property(i => i.Quantity).HasColumnName("quantite").HasPrecision(18, 3);
        modelBuilder.Entity<InvoiceDetail>().Property(i => i.UnitPriceHT).HasColumnName("pu_ht").HasPrecision(18, 2);
        modelBuilder.Entity<InvoiceDetail>().Property(i => i.TotalHT).HasColumnName("total_ht").HasPrecision(18, 2);
        modelBuilder.Entity<InvoiceDetail>().Property(i => i.TaxRate).HasColumnName("taux_tva").HasPrecision(18, 2);
        modelBuilder.Entity<InvoiceDetail>().Property(i => i.TotalTTC).HasColumnName("total_ttc").HasPrecision(18, 2);

        modelBuilder.Entity<InvoiceDetail>()
            .HasOne(i => i.Invoice)
            .WithMany(f => f.Details)
            .HasForeignKey(i => i.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Payments
        modelBuilder.Entity<Payment>().ToTable("reglements");
        modelBuilder.Entity<Payment>().Property(p => p.Id).HasColumnName("id");
        modelBuilder.Entity<Payment>().Property(p => p.PaymentNumber).HasColumnName("numero_reglement").HasMaxLength(50).IsRequired();
        modelBuilder.Entity<Payment>().Property(p => p.InvoiceId).HasColumnName("facture_id");
        modelBuilder.Entity<Payment>().Property(p => p.SupplierId).HasColumnName("fournisseur_id");
        modelBuilder.Entity<Payment>().Property(p => p.PaymentDate).HasColumnName("date_paiement").IsRequired();
        modelBuilder.Entity<Payment>().Property(p => p.AmountPaid).HasColumnName("montant").HasPrecision(18, 2);
        modelBuilder.Entity<Payment>().Property(p => p.PaymentMethod).HasColumnName("mode_paiement").HasMaxLength(50);
        modelBuilder.Entity<Payment>().Property(p => p.ReferenceNumber).HasColumnName("reference").HasMaxLength(255);
        modelBuilder.Entity<Payment>().Property(p => p.BankName).HasColumnName("banque").HasMaxLength(100);
        modelBuilder.Entity<Payment>().Property(p => p.ProofFilePath).HasColumnName("fichier_preuve");
        modelBuilder.Entity<Payment>().Property(p => p.FileType).HasColumnName("type_fichier").HasMaxLength(20);
        modelBuilder.Entity<Payment>().Property(p => p.ReceiptFilePath).HasColumnName("fichier_recu");
        modelBuilder.Entity<Payment>().Property(p => p.Observations).HasColumnName("observations");
        modelBuilder.Entity<Payment>().Property(p => p.CreatedById).HasColumnName("cree_par");
        modelBuilder.Entity<Payment>().Property(p => p.CreatedAt).HasColumnName("date_creation");
        modelBuilder.Entity<Payment>().Property(p => p.UpdatedAt).HasColumnName("date_maj");

        modelBuilder.Entity<Payment>()
            .HasIndex(p => p.PaymentNumber)
            .IsUnique();

        // Needs
        modelBuilder.Entity<Need>()
            .Property(n => n.Quantity)
            .HasPrecision(18, 2);

        // AuditLog
        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => a.ActionDate);
    }
}
