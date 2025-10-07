namespace YamyProject.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<TblCompany> TblCompanies { get;set;}
        public DbSet<TblWarehouse> tbl_warehouse { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TblSalesDetail>()
           .HasOne(d => d.Sales)
           .WithMany(p => p.TblSalesDetails)
           .HasForeignKey(d => d.SalesId)
           .HasConstraintName("FK_TblSalesDetail_TblSale");


            //modelBuilder.Entity<TblTransaction>()
            //  .HasOne(t => t.Sale)
            //  .WithMany(s => s.TblTransactions)
            //  .HasForeignKey(t => t.TransactionId);



            modelBuilder.Entity<TblSale>()
           .HasOne(s => s.Customer)
           .WithMany()
           .HasForeignKey(s => s.CustomerId);
        }

    }
}
