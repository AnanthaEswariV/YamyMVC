
namespace YamyProject.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<TblCompany> TblCompanys { get;set;}
        public DbSet<TblWarehouse> tbl_warehouse { get; set; }

    }
}
