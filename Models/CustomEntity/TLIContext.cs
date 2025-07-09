using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using AddonProcurement.Models.CustomEntity.Models;
using WolfApprove.Model;
using WolfApprove.Model.Helper;

namespace AddonProcurement.Models.CustomEntity
{
    public class TLIContext : WolfApproveModel
    {
        public virtual DbSet<ViewBudgetTransaction> ViewBudgetTransactions { get; set; }
        public virtual DbSet<BudgetTransaction> BudgetTransactions { get; set; }
        public virtual DbSet<BudgetTracking> BudgetTrackings { get; set; }
        public virtual DbSet<BudgetTrackingSummary> BudgetTrackingSummaries { get; set; }
        public virtual DbSet<BudgetTrackingGLSummary> BudgetTrackingGLSummaries { get; set; }
        public virtual DbSet<BudgetTrackingIOSummary> BudgetTrackingIOSummaries { get; set; }
        public virtual DbSet<ZSYNCBUDGET> ZSYNCBUDGETs { get; set; }
        public virtual DbSet<RelationBudget> RelationBudgets { get; set; }
        public virtual DbSet<ZSYNCCC> ZSYNCCCs { get; set; }
        public virtual DbSet<ZSYNCGL> ZSYNCGLs { get; set; }
        public virtual DbSet<ZSYNCIO> ZSYNCIOs { get; set; }
        public virtual DbSet<MSTAsset> MSTAssets { get; set; }



        public TLIContext(string Connection) : base(Connection)
        {
            Database.SetInitializer<TLIContext>(null);
        }

        public TLIContext() : base("name=WolfContext")
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<RelationBudget>()
                .HasKey(x => x.Id)
                .ToTable("ViewRelationBudget");

            modelBuilder.Entity<BudgetTracking>()
                .HasKey(x => x.Id)
                .ToTable("vw_BudgetTracking");

            modelBuilder.Entity<BudgetTrackingSummary>()
                .HasKey(x => x.Id)
                .ToTable("vw_BudgetTrackingSummary");

            modelBuilder.Entity<BudgetTrackingGLSummary>()
                .HasKey(x => x.Id)
                .ToTable("vw_BudgetTrackingGLSummary");

            modelBuilder.Entity<BudgetTrackingIOSummary>()
                .HasKey(x => x.Id)
                .ToTable("vw_BudgetTrackingIOSummary");

            modelBuilder.Entity<BudgetTransaction>()
                .HasKey(x => x.Id)
                .ToTable("BudgetTransaction");

            modelBuilder.Entity<ViewBudgetTransaction>()
                .HasKey(x => x.Id)
                .ToTable("ViewBudgetTransection");

            modelBuilder.Entity<ZSYNCBUDGET>()
                .HasKey(x => x.Id)
                .ToTable("ZSYNCBUDGET");

            modelBuilder.Entity<ZSYNCCC>()
                .HasKey(x => x.Id)
                .ToTable("ZSYNCCC");

            modelBuilder.Entity<ZSYNCGL>()
                .HasKey(x => x.Id)
                .ToTable("ZSYNCGL");

            modelBuilder.Entity<ZSYNCIO>()
                .HasKey(x => x.Id)
                .ToTable("ZSYNCIO");
            modelBuilder.Entity<MSTAsset>()
             .HasKey(x => x.Id)
             .ToTable("MSTAsset");
        }

        public static TLIContext OpenConnection(string strConnection)
        {
            return new TLIContext(new AESCipher().function.Decrypt(strConnection, DecryptBase64: true));
        }
    }
}