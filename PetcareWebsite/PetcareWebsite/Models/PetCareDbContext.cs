using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace PetcareWebsite.Models;

public partial class PetCareDbContext : DbContext
{
    public PetCareDbContext()
    {
    }

    public PetCareDbContext(DbContextOptions<PetCareDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<Booking> Bookings { get; set; }

    public virtual DbSet<BookingDetail> BookingDetails { get; set; }

    public virtual DbSet<BookingDetailEmployee> BookingDetailEmployees { get; set; }

    public virtual DbSet<BookingStatus> BookingStatuses { get; set; }

    public virtual DbSet<ContactMessage> ContactMessages { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<DetailStatus> DetailStatuses { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<InventoryTransaction> InventoryTransactions { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    public virtual DbSet<InvoiceStatus> InvoiceStatuses { get; set; }

    public virtual DbSet<MedicalSupply> MedicalSupplies { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<PaymentMethod> PaymentMethods { get; set; }

    public virtual DbSet<Pet> Pets { get; set; }

    public virtual DbSet<PetBreed> PetBreeds { get; set; }

    public virtual DbSet<PetSpecy> PetSpecies { get; set; }

    public virtual DbSet<Promotion> Promotions { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<ServiceCatalog> ServiceCatalogs { get; set; }

    public virtual DbSet<ServiceCategory> ServiceCategories { get; set; }

    public virtual DbSet<ServiceMaterialQuotum> ServiceMaterialQuota { get; set; }

    public virtual DbSet<ServiceReview> ServiceReviews { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.AccountId).HasName("PK__Account__349DA5868A4D3138");

            entity.ToTable("Account");

            entity.HasIndex(e => e.Username, "UQ__Account__536C85E4DDC48A98").IsUnique();

            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.Password).HasMaxLength(255);
            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.Username)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.Role).WithMany(p => p.Accounts)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Account__RoleID__3E52440B");
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.BookingId).HasName("PK__Booking__73951ACDD79CC8C1");

            entity.ToTable("Booking");

            entity.HasIndex(e => e.CustomerId, "IX_Booking_CustomerID");

            entity.HasIndex(e => e.StatusId, "IX_Booking_StatusID");

            entity.HasIndex(e => e.BookingCode, "UQ__Booking__C6E56BD587F8C71C").IsUnique();

            entity.Property(e => e.BookingId).HasColumnName("BookingID");
            entity.Property(e => e.BookingCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.StatusId)
                .HasDefaultValue(1)
                .HasColumnName("StatusID");

            entity.HasOne(d => d.Customer).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Booking__Custome__123EB7A3");

            entity.HasOne(d => d.Status).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Booking__StatusI__1332DBDC");
        });

        modelBuilder.Entity<BookingDetail>(entity =>
        {
            entity.HasKey(e => e.BookingDetailId).HasName("PK__BookingD__8136D47AFFB35368");

            entity.ToTable("BookingDetail", tb =>
                {
                    tb.HasTrigger("trg_HandleStockOnStatusChange");
                    tb.HasTrigger("trg_ValidatePetOwnership");
                });

            entity.HasIndex(e => e.BookingId, "IX_BookingDetail_BookingID");

            entity.HasIndex(e => e.PetId, "IX_BookingDetail_PetID");

            entity.HasIndex(e => e.ServiceId, "IX_BookingDetail_ServiceID");

            entity.HasIndex(e => e.StatusId, "IX_BookingDetail_StatusID");

            entity.HasIndex(e => new { e.BookingId, e.PetId, e.ServiceId }, "UQ_BookingDetail").IsUnique();

            entity.Property(e => e.BookingDetailId).HasColumnName("BookingDetailID");
            entity.Property(e => e.ActualPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.BookingId).HasColumnName("BookingID");
            entity.Property(e => e.PetId).HasColumnName("PetID");
            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");
            entity.Property(e => e.StartTime);
            entity.Property(e => e.EndTime);
            entity.Property(e => e.StatusId)
                .HasDefaultValue(1)
                .HasColumnName("StatusID");

            entity.HasOne(d => d.Booking).WithMany(p => p.BookingDetails)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__BookingDe__Booki__19DFD96B");

            entity.HasOne(d => d.Pet).WithMany(p => p.BookingDetails)
                .HasForeignKey(d => d.PetId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__BookingDe__PetID__1AD3FDA4");

            entity.HasOne(d => d.Service).WithMany(p => p.BookingDetails)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__BookingDe__Servi__1BC821DD");

            entity.HasOne(d => d.Status).WithMany(p => p.BookingDetails)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__BookingDe__Statu__1CBC4616");
        });

        modelBuilder.Entity<BookingDetailEmployee>(entity =>
        {
            entity.HasKey(e => new { e.BookingDetailId, e.EmployeeId }).HasName("PK__BookingD__869BD0854FCB4878");

            entity.ToTable("BookingDetail_Employee");

            entity.Property(e => e.BookingDetailId).HasColumnName("BookingDetailID");
            entity.Property(e => e.EmployeeId).HasColumnName("EmployeeID");
            entity.Property(e => e.AssignedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.BookingDetail).WithMany(p => p.BookingDetailEmployees)
                .HasForeignKey(d => d.BookingDetailId)
                .HasConstraintName("FK__BookingDe__Booki__208CD6FA");

            entity.HasOne(d => d.Employee).WithMany(p => p.BookingDetailEmployees)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__BookingDe__Emplo__2180FB33");
        });

        modelBuilder.Entity<BookingStatus>(entity =>
        {
            entity.HasKey(e => e.StatusId).HasName("PK__BookingS__C8EE2043957A3D44");

            entity.ToTable("BookingStatus");

            entity.HasIndex(e => e.StatusName, "UQ__BookingS__05E7698AE781A1C3").IsUnique();

            entity.Property(e => e.StatusId)
                .ValueGeneratedNever()
                .HasColumnName("StatusID");
            entity.Property(e => e.StatusName)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<ContactMessage>(entity =>
        {
            entity.HasKey(e => e.ContactMessageId);

            entity.ToTable("ContactMessage");

            entity.HasIndex(e => e.CustomerId, "IX_ContactMessage_CustomerID");

            entity.Property(e => e.ContactMessageId).HasColumnName("ContactMessageID");
            entity.Property(e => e.AdminNote).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Message).HasMaxLength(1000);
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("New");
            entity.Property(e => e.Topic).HasMaxLength(100);

            entity.HasOne(d => d.Customer).WithMany(p => p.ContactMessages)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ContactMessage_Customer");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.CustomerId).HasName("PK__Customer__A4AE64B8D2A5F7F9");

            entity.ToTable("Customer");

            entity.HasIndex(e => e.AccountId, "UX_Customer_AccountID_NotNull")
                .IsUnique()
                .HasFilter("([AccountID] IS NOT NULL)");

            entity.HasIndex(e => e.PhoneNumber, "UQ__Customer__85FB4E38A5219B9D").IsUnique();

            entity.HasIndex(e => e.Email, "UX_Customer_Email")
                .IsUnique()
                .HasFilter("([Email] IS NOT NULL)");

            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(15)
                .IsUnicode(false);

            entity.HasOne(d => d.Account).WithOne(p => p.Customer)
                .HasForeignKey<Customer>(d => d.AccountId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__Customer__Accoun__59FA5E80");
        });

        modelBuilder.Entity<DetailStatus>(entity =>
        {
            entity.HasKey(e => e.StatusId).HasName("PK__DetailSt__C8EE20439E264D1E");

            entity.ToTable("DetailStatus");

            entity.HasIndex(e => e.StatusName, "UQ__DetailSt__05E7698A7E46E85F").IsUnique();

            entity.Property(e => e.StatusId)
                .ValueGeneratedNever()
                .HasColumnName("StatusID");
            entity.Property(e => e.StatusName)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmployeeId).HasName("PK__Employee__7AD04FF10DEB0976");

            entity.ToTable("Employee");

            entity.HasIndex(e => e.AccountId, "UQ__Employee__349DA5877CB4E180").IsUnique();

            entity.HasIndex(e => e.PhoneNumber, "UQ__Employee__85FB4E38C7E7226B").IsUnique();

            entity.Property(e => e.EmployeeId).HasColumnName("EmployeeID");
            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.RoleId).HasColumnName("RoleID");

            entity.HasOne(d => d.Account).WithOne(p => p.Employee)
                .HasForeignKey<Employee>(d => d.AccountId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__Employee__Accoun__68487DD7");

            entity.HasOne(d => d.Role).WithMany(p => p.Employees)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Employee__RoleID__693CA210");
        });

        modelBuilder.Entity<InventoryTransaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId).HasName("PK__Inventor__55433A4B74F3D725");

            entity.ToTable("InventoryTransaction");

            entity.HasIndex(e => e.SupplyId, "IX_InventoryTransaction_SupplyID");

            entity.Property(e => e.TransactionId).HasColumnName("TransactionID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.EmployeeId).HasColumnName("EmployeeID");
            entity.Property(e => e.Note).HasMaxLength(255);
            entity.Property(e => e.ReferenceId).HasColumnName("ReferenceID");
            entity.Property(e => e.SupplyId).HasColumnName("SupplyID");
            entity.Property(e => e.TransactionType)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Employee).WithMany(p => p.InventoryTransactions)
                .HasForeignKey(d => d.EmployeeId)
                .HasConstraintName("FK__Inventory__Emplo__02084FDA");

            entity.HasOne(d => d.Supply).WithMany(p => p.InventoryTransactions)
                .HasForeignKey(d => d.SupplyId)
                .HasConstraintName("FK__Inventory__Suppl__01142BA1");
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.InvoiceId).HasName("PK__Invoice__D796AAD535C91F44");

            entity.ToTable("Invoice");

            entity.HasIndex(e => e.PromotionId, "IX_Invoice_PromotionID");

            entity.HasIndex(e => e.StatusId, "IX_Invoice_StatusID");

            entity.HasIndex(e => e.InvoiceCode, "UQ__Invoice__0D9D7FF327C41FC7").IsUnique();

            entity.HasIndex(e => e.BookingId, "UQ__Invoice__73951ACCBDDB00D7").IsUnique();

            entity.Property(e => e.InvoiceId).HasColumnName("InvoiceID");
            entity.Property(e => e.BookingId).HasColumnName("BookingID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.DiscountAmount)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.InvoiceCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.PaidAmount)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PromotionId).HasColumnName("PromotionID");
            entity.Property(e => e.StatusId)
                .HasDefaultValue(1)
                .HasColumnName("StatusID");
            entity.Property(e => e.TotalAmount)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Booking).WithOne(p => p.Invoice)
                .HasForeignKey<Invoice>(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Invoice__Booking__3587F3E0");

            entity.HasOne(d => d.Promotion).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.PromotionId)
                .HasConstraintName("FK__Invoice__Promoti__37703C52");

            entity.HasOne(d => d.Status).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Invoice__StatusI__367C1819");
        });

        modelBuilder.Entity<InvoiceStatus>(entity =>
        {
            entity.HasKey(e => e.StatusId).HasName("PK__InvoiceS__C8EE204379F7098C");

            entity.ToTable("InvoiceStatus");

            entity.HasIndex(e => e.StatusName, "UQ__InvoiceS__05E7698ABB24C079").IsUnique();

            entity.Property(e => e.StatusId)
                .ValueGeneratedNever()
                .HasColumnName("StatusID");
            entity.Property(e => e.StatusName)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<MedicalSupply>(entity =>
        {
            entity.HasKey(e => e.SupplyId).HasName("PK__MedicalS__7CDD6C8E0A6DD44D");

            entity.ToTable("MedicalSupply");

            entity.HasIndex(e => e.ExpiryDate, "IX_MedicalSupply_Expiry");

            entity.Property(e => e.SupplyId).HasColumnName("SupplyID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.MinStockLevel).HasDefaultValue(5);
            entity.Property(e => e.StockQuantity).HasDefaultValue(0);
            entity.Property(e => e.SupplyName).HasMaxLength(100);
            entity.Property(e => e.Unit).HasMaxLength(20);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payment__9B556A58F516494F");

            entity.ToTable("Payment", tb => tb.HasTrigger("trg_StrictUpdateInvoicePayment"));

            entity.HasIndex(e => e.InvoiceId, "IX_Payment_InvoiceID");

            entity.HasIndex(e => e.MethodId, "IX_Payment_MethodID");

            entity.Property(e => e.PaymentId).HasColumnName("PaymentID");
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.InvoiceId).HasColumnName("InvoiceID");
            entity.Property(e => e.MethodId).HasColumnName("MethodID");
            entity.Property(e => e.Note).HasMaxLength(255);
            entity.Property(e => e.PaymentDate).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Invoice).WithMany(p => p.Payments)
                .HasForeignKey(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Payment__Invoice__3C34F16F");

            entity.HasOne(d => d.Method).WithMany(p => p.Payments)
                .HasForeignKey(d => d.MethodId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Payment__MethodI__3D2915A8");
        });

        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            entity.HasKey(e => e.MethodId).HasName("PK__PaymentM__FC681FB1C7F9F876");

            entity.ToTable("PaymentMethod");

            entity.HasIndex(e => e.MethodName, "UQ__PaymentM__218CFB17387CA75D").IsUnique();

            entity.Property(e => e.MethodId)
                .ValueGeneratedNever()
                .HasColumnName("MethodID");
            entity.Property(e => e.MethodName)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Pet>(entity =>
        {
            entity.HasKey(e => e.PetId).HasName("PK__Pet__48E53802A863E40B");

            entity.ToTable("Pet");

            entity.HasIndex(e => e.CustomerId, "IX_Pet_CustomerID");

            entity.Property(e => e.PetId).HasColumnName("PetID");
            entity.Property(e => e.BreedId).HasColumnName("BreedID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.SpeciesId).HasColumnName("SpeciesID");
            entity.Property(e => e.Weight).HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.Customer).WithMany(p => p.Pets)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Pet__CustomerID__5FB337D6");

            entity.HasOne(d => d.Species).WithMany(p => p.Pets)
                .HasForeignKey(d => d.SpeciesId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Pet__SpeciesID__60A75C0F");

            entity.HasOne(d => d.PetBreed).WithMany(p => p.Pets)
                .HasPrincipalKey(p => new { p.BreedId, p.SpeciesId })
                .HasForeignKey(d => new { d.BreedId, d.SpeciesId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Pet_Breed_Species");
        });

        modelBuilder.Entity<PetBreed>(entity =>
        {
            entity.HasKey(e => e.BreedId).HasName("PK__PetBreed__D1E9AEBD9324166A");

            entity.ToTable("PetBreed");

            entity.HasIndex(e => new { e.BreedId, e.SpeciesId }, "UQ_Breed_Species").IsUnique();

            entity.Property(e => e.BreedId).HasColumnName("BreedID");
            entity.Property(e => e.BreedName).HasMaxLength(50);
            entity.Property(e => e.SpeciesId).HasColumnName("SpeciesID");

            entity.HasOne(d => d.Species).WithMany(p => p.PetBreeds)
                .HasForeignKey(d => d.SpeciesId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PetBreed__Specie__534D60F1");
        });

        modelBuilder.Entity<PetSpecy>(entity =>
        {
            entity.HasKey(e => e.SpeciesId).HasName("PK__PetSpeci__A938047F5FF79249");

            entity.HasIndex(e => e.SpeciesName, "UQ__PetSpeci__304D4C0D4624907A").IsUnique();

            entity.Property(e => e.SpeciesId).HasColumnName("SpeciesID");
            entity.Property(e => e.SpeciesName).HasMaxLength(50);
        });

        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.HasKey(e => e.PromotionId).HasName("PK__Promotio__52C42F2FCCC2AB3F");

            entity.ToTable("Promotion");

            entity.HasIndex(e => e.PromoCode, "UQ__Promotio__32DBED35CE010B4A").IsUnique();

            entity.Property(e => e.PromotionId).HasColumnName("PromotionID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.DiscountType)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.DiscountValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.MaxDiscount)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MinOrderValue)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PromoCode)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Role__8AFACE3A5E4AE2B0");

            entity.ToTable("Role");

            entity.HasIndex(e => e.RoleName, "UQ__Role__8A2B61600ECD876D").IsUnique();

            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<ServiceCatalog>(entity =>
        {
            entity.HasKey(e => e.ServiceId).HasName("PK__ServiceC__C51BB0EAAC0E7B7A");

            entity.ToTable("ServiceCatalog");

            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");
            entity.Property(e => e.BasePrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.MaxCapacity).HasDefaultValue(3);
            entity.Property(e => e.ServiceName).HasMaxLength(100);

            entity.HasOne(d => d.Category).WithMany(p => p.ServiceCatalogs)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ServiceCa__Categ__70DDC3D8");
        });

        modelBuilder.Entity<ServiceCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__ServiceC__19093A2BFDECD303");

            entity.ToTable("ServiceCategory");

            entity.HasIndex(e => e.CategoryName, "UQ__ServiceC__8517B2E05B13AE1D").IsUnique();

            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CategoryName).HasMaxLength(50);
        });

        modelBuilder.Entity<ServiceMaterialQuotum>(entity =>
        {
            entity.HasKey(e => new { e.ServiceId, e.SupplyId }).HasName("PK__ServiceM__32D666226D12F7F6");

            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");
            entity.Property(e => e.SupplyId).HasColumnName("SupplyID");
            entity.Property(e => e.QuantityUsed).HasDefaultValue(1);

            entity.HasOne(d => d.Service).WithMany(p => p.ServiceMaterialQuota)
                .HasForeignKey(d => d.ServiceId)
                .HasConstraintName("FK__ServiceMa__Servi__7B5B524B");

            entity.HasOne(d => d.Supply).WithMany(p => p.ServiceMaterialQuota)
                .HasForeignKey(d => d.SupplyId)
                .HasConstraintName("FK__ServiceMa__Suppl__7C4F7684");
        });

        modelBuilder.Entity<ServiceReview>(entity =>
        {
            entity.HasKey(e => e.ReviewId).HasName("PK__ServiceR__74BC79AEE7AF4C0A");

            entity.ToTable("ServiceReview");

            entity.HasIndex(e => e.BookingDetailId, "UQ__ServiceR__8136D47B7F107B1D").IsUnique();

            entity.Property(e => e.ReviewId).HasColumnName("ReviewID");
            entity.Property(e => e.BookingDetailId).HasColumnName("BookingDetailID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.IsVisible).HasDefaultValue(true);
            entity.Property(e => e.ReviewTags).HasMaxLength(255);

            entity.HasOne(d => d.BookingDetail).WithOne(p => p.ServiceReview)
                .HasForeignKey<ServiceReview>(d => d.BookingDetailId)
                .HasConstraintName("FK__ServiceRe__Booki__282DF8C2");

            entity.HasOne(d => d.Customer).WithMany(p => p.ServiceReviews)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ServiceRe__Custo__29221CFB");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
