using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Bson;

namespace CaseStudy.Models
{
    public class DatabaseContext : DbContext
    {
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Address> Addresses { get; set; } = null!;
        public DbSet<Account> Accounts { get; set; } = null!;
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<Rating> Ratings { get; set; } = null!;
         public DbSet<CropType> CropTypes { get;set;}= null!;
        public DbSet<CropDetail>  CropDetails { get;set;}= null!;
        public DbSet<Invoice> Invoices { get; set; } = null!;
        public DbSet<Admin> Admins { get; set; } = null!;
        public DbSet<ExceptionError> ExceptionErrors { get; set; } = null!;
        public DbSet<Subscription> Subscriptions { get; set; } = null!;
    
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"data source=.\sqlexpress;database=CaseStudyBackend;integrated security=SSPI");
            base.OnConfiguring(optionsBuilder);
        } 
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<Role>().HasData(
            //        new Role
            //        {
            //            RoleId = 1,
            //            RoleName = "Farmer"
            //        },
            //         new Role
            //         {
            //             RoleId = 2,
            //             RoleName = "Dealer"
            //         }
            //    );

            //modelBuilder.Entity<CropType>().HasData(
            //       new CropType
            //       {
            //           CropTypeId = 1,
            //           TypeName = "Fruit"
            //       },
            //        new CropType
            //        {
            //            CropTypeId = 2,
            //            TypeName = "Vegetable"
            //        },
            //        new CropType
            //        {
            //            CropTypeId = 3,
            //            TypeName = "Grain"
            //        }
            //   );

       

            modelBuilder.Entity<Invoice>(
                entity =>
                entity.HasOne(f => f.Farmer)
                .WithMany(e => e.FarmerInvoices)
                .HasPrincipalKey(k => k.UserId)
                .HasForeignKey(k => k.FarmerId)
                .OnDelete(DeleteBehavior.ClientSetNull));

            modelBuilder.Entity<Invoice>(
                entity =>
                entity.HasOne(f => f.Dealer)
                .WithMany(e => e.DealerInvoices)
                .HasPrincipalKey(k => k.UserId)
                .HasForeignKey(k => k.DealerId)
                .OnDelete(DeleteBehavior.ClientSetNull));
        }
    }
}
