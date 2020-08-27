using Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace Entities.Contexts
{
    public partial class DbdatiContext : DbContext
    {
        public DbdatiContext()
        {
        }

        public DbdatiContext(DbContextOptions<DbdatiContext> options)
            : base(options)
        {
        }

        public virtual DbSet<AnagLivelli> AnagLivelli { get; set; }
        public virtual DbSet<DbaFunzioni> DbaFunzioni { get; set; }
        public virtual DbSet<DbaFunzioniAree> DbaFunzioniAree { get; set; }
        public virtual DbSet<DbaFunzioniCfg> DbaFunzioniCfg { get; set; }
        public virtual DbSet<DbaFunzioniGruppi> DbaFunzioniGruppi { get; set; }
        public virtual DbSet<VMenu> VMenu { get; set; }
        public virtual DbSet<VMenuLivello> VMenuLivello { get; set; }

        public virtual DbSet<BecaView> BecaView { get; set; }
        public virtual DbSet<BecaViewData> BecaViewData { get; set; }
        public virtual DbSet<BecaViewFilterValues> BecaViewFilterValues { get; set; }
        public virtual DbSet<BecaViewFilters> BecaViewFilters { get; set; }
        public virtual DbSet<BecaViewPanels> BecaViewPanels { get; set; }
        public virtual DbSet<BecaPanelFilters> BecaPanelFilters { get; set; }
        public virtual DbSet<BecaFormula> BecaFormula { get; set; }
        public virtual DbSet<BecaFormulaData> BecaFormulaData { get; set; }
        public virtual DbSet<BecaFormulaDataFilters> BecaFormulaDataFilters { get; set; }
        public virtual DbSet<BecaViewTypes> BecaViewTypes { get; set; }
        public virtual DbSet<BecaAggregationTypes> BecaAggregationTypes { get; set; }
        public virtual DbSet<BecaViewFilterUI> BecaViewFilterUI { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AnagLivelli>(entity =>
            {
                entity.HasKey(e => e.IdLivello)
                    .HasName("PK_Anag_Livelli_idLivello");

                entity.ToTable("Anag_Livelli");

                entity.Property(e => e.IdLivello)
                    .HasColumnName("idLivello")
                    .ValueGeneratedNever();

                entity.Property(e => e.DescLivello)
                    .HasMaxLength(500)
                    .IsUnicode(false);

                entity.Property(e => e.DtInsert)
                    .HasColumnName("dtInsert")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.FlagsProfilo)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.FlgCambioPwd).HasColumnName("flgCambioPwd");

                entity.Property(e => e.FlgFiltroFiliale)
                    .IsRequired()
                    .HasColumnName("flgFiltroFiliale")
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.FlgProtected)
                    .HasColumnName("flgProtected")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.HomePage)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('Home.htm')");

                entity.Property(e => e.LoginPage)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('frmLogin.aspx')");
            });

            modelBuilder.Entity<DbaFunzioni>(entity =>
            {
                entity.HasKey(e => e.CodMenuItem)
                    .HasName("PK_dbaFunzioni_CoddbaFunzioni");

                entity.ToTable("_dbaFunzioni");

                entity.Property(e => e.CodMenuItem)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Caption)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.CodMenuMain)
                    .IsRequired()
                    .HasMaxLength(10)
                    .IsUnicode(false);

                entity.Property(e => e.CustomForm)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.DescMenuItem)
                    .HasMaxLength(500)
                    .IsUnicode(false);

                entity.Property(e => e.DetailsForm)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.DtInsert)
                    .HasColumnName("dtInsert")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.Form)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Parameters)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.TableName)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.ViewName)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.HasOne(d => d.CodMenuMainNavigation)
                    .WithMany(p => p.DbaFunzioni)
                    .HasForeignKey(d => d.CodMenuMain)
                    .HasConstraintName("FK__dbaFunzioni__dbaFunzioniGruppi");
            });

            modelBuilder.Entity<DbaFunzioniAree>(entity =>
            {
                entity.HasKey(e => e.CodMenuArea)
                    .HasName("PK_dbaFunzioniAree");

                entity.ToTable("_dbaFunzioniAree");

                entity.Property(e => e.CodMenuArea)
                    .HasMaxLength(10)
                    .IsUnicode(false);

                entity.Property(e => e.DescMenuArea)
                    .IsRequired()
                    .HasMaxLength(500)
                    .IsUnicode(false);

                entity.Property(e => e.DtInsert)
                    .HasColumnName("dtInsert")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.Icona)
                    .HasColumnName("icona")
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<DbaFunzioniCfg>(entity =>
            {
                entity.ToTable("_dbaFunzioniCfg");

                entity.HasIndex(e => new { e.CodMenuMain, e.CodMenuItem, e.SottoGruppo, e.Posizione, e.IdLivello })
                    .HasName("IX__dbaFunzioniCfg");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Caption)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.CodMenuItem)
                    .IsRequired()
                    .HasMaxLength(10)
                    .IsUnicode(false);

                entity.Property(e => e.CodMenuMain)
                    .HasMaxLength(10)
                    .IsUnicode(false);

                entity.Property(e => e.CodModulo)
                    .HasMaxLength(10)
                    .IsUnicode(false);

                entity.Property(e => e.CustomForm)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.DetailsForm)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.DtInsert)
                    .HasColumnName("dtInsert")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.FlAdd).HasColumnName("flAdd");

                entity.Property(e => e.FlDel).HasColumnName("flDel");

                entity.Property(e => e.FlDetail).HasColumnName("flDetail");

                entity.Property(e => e.FlEdit).HasColumnName("flEdit");

                entity.Property(e => e.FlExcel).HasColumnName("flExcel");

                entity.Property(e => e.FlList).HasColumnName("flList");

                entity.Property(e => e.IdLivello).HasColumnName("idLivello");
            });

            modelBuilder.Entity<DbaFunzioniGruppi>(entity =>
            {
                entity.HasKey(e => e.CodMenuMain)
                    .HasName("PK__dbaFunzioniGruppi_Cod_dbaFunzioniGruppi");

                entity.ToTable("_dbaFunzioniGruppi");

                entity.Property(e => e.CodMenuMain)
                    .HasMaxLength(10)
                    .IsUnicode(false);

                entity.Property(e => e.CodMenuArea)
                    .IsRequired()
                    .HasMaxLength(10)
                    .IsUnicode(false);

                entity.Property(e => e.DescMenuMain)
                    .IsRequired()
                    .HasMaxLength(500)
                    .IsUnicode(false);

                entity.Property(e => e.DtInsert)
                    .HasColumnName("dtInsert")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.Icona)
                    .HasColumnName("icona")
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.HasOne(d => d.CodMenuAreaNavigation)
                    .WithMany(p => p.DbaFunzioniGruppi)
                    .HasForeignKey(d => d.CodMenuArea)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__dbaFunzioniGruppi__dbaFunzioniAree");
            });

            modelBuilder.Entity<VMenu>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("__vMenu");

                entity.Property(e => e.Caption)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.CodMenuItem)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.CodMenuMain)
                    .HasMaxLength(10)
                    .IsUnicode(false);

                entity.Property(e => e.DescLivello)
                    .HasMaxLength(500)
                    .IsUnicode(false);

                entity.Property(e => e.DetailsForm)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.FlAdd).HasColumnName("flAdd");

                entity.Property(e => e.FlDel).HasColumnName("flDel");

                entity.Property(e => e.FlDetail).HasColumnName("flDetail");

                entity.Property(e => e.FlEdit).HasColumnName("flEdit");

                entity.Property(e => e.FlExcel).HasColumnName("flExcel");

                entity.Property(e => e.FlList).HasColumnName("flList");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.IdLivello).HasColumnName("idLivello");
            });

            modelBuilder.Entity<VMenuLivello>(entity =>
            {
                entity.HasNoKey();

                entity.ToView("_vMenuLivello");

                entity.Property(e => e.AreaCod)
                    .IsRequired()
                    .HasMaxLength(10)
                    .IsUnicode(false);

                entity.Property(e => e.AreaDesc)
                    .IsRequired()
                    .HasMaxLength(500)
                    .IsUnicode(false);

                entity.Property(e => e.AreaIcona)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Caption)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.DetailsForm)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.FlAdd).HasColumnName("flAdd");

                entity.Property(e => e.FlDel).HasColumnName("flDel");

                entity.Property(e => e.FlDetail).HasColumnName("flDetail");

                entity.Property(e => e.FlEdit).HasColumnName("flEdit");

                entity.Property(e => e.FlExcel).HasColumnName("flExcel");

                entity.Property(e => e.FlList).HasColumnName("flList");

                entity.Property(e => e.GruppoCod)
                    .IsRequired()
                    .HasMaxLength(10)
                    .IsUnicode(false);

                entity.Property(e => e.GruppoDesc)
                    .IsRequired()
                    .HasMaxLength(500)
                    .IsUnicode(false);

                entity.Property(e => e.GruppoIcona)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.IdLivello).HasColumnName("idLivello");

                entity.Property(e => e.MenuCod)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<BecaAggregationTypes>(entity =>
            {
                entity.HasKey(e => e.IdAggregationType);
            });

            modelBuilder.Entity<BecaFormula>(entity =>
            {
                entity.HasKey(e => e.IdFormula);
            });

            modelBuilder.Entity<BecaFormulaData>(entity =>
            {
                entity.HasKey(e => e.IdFormulaData);
                
                entity.HasOne(d => d.IdAggregationTypeNavigation)
                    .WithMany(p => p.BecaFormulaData)
                    .HasForeignKey(d => d.IdAggregationType)
                    .HasConstraintName("FK_BecaFormulaData_BecaAggregationTypes");

                entity.HasOne(d => d.IdFormulaNavigation)
                    .WithMany(p => p.BecaFormulaData)
                    .HasForeignKey(d => d.IdFormula)
                    .HasConstraintName("FK_BecaFormulaData_BecaFormula");
            });

            modelBuilder.Entity<BecaFormulaDataFilters>(entity =>
            {
                entity.ToView("vBecaFormulaDataFilters");
                entity.HasKey(e => new { e.IdFormulaData, e.idBecaFilter });

                entity.HasOne(d => d.IdFormulaDataNavigation)
                    .WithMany(p => p.BecaFormulaDataFilters)
                    .HasForeignKey(d => d.IdFormulaData)
                    .HasConstraintName("FK_BecaFormulaDataFilters_BecaFormulaData");
            });

            modelBuilder.Entity<BecaPanelFilters>(entity =>
            {
                entity.ToView("vBecaPanelFilters");
                entity.HasKey(e => new { e.idBecaViewPanel, e.idBecaFilter });

                entity.HasOne(d => d.idBecaViewPanelNavigation)
                    .WithMany(p => p.BecaPanelFilters)
                    .HasForeignKey(d => d.idBecaViewPanel)
                    .HasConstraintName("FK_BecaPanelFilters_BecaViewPanels");
            });

            modelBuilder.Entity<BecaView>(entity =>
            {
                entity.HasKey(e => e.idBecaView);

                entity.HasOne(d => d.idBecaViewTypeNavigation)
                    .WithMany(p => p.BecaView)
                    .HasForeignKey(d => d.idBecaViewType)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_BecaView_BecaViewTypes");
            });

            modelBuilder.Entity<BecaViewData>(entity =>
            {
                entity.ToView("vBecaViewData");
                entity.HasKey(e => new { e.idBecaView, e.idDataDefinition });

                entity.HasOne(d => d.idBecaViewNavigation)
                    .WithMany(p => p.BecaViewData)
                    .HasForeignKey(d => d.idBecaView)
                    .HasConstraintName("FK_BecaViewData_BecaView");
            });

            modelBuilder.Entity<BecaViewFilterValues>(entity =>
            {
                entity.ToView("vBecaViewFilterValues");
                entity.HasKey(e => new { e.idBecaView, e.idFilterValue })
                    .HasName("PK_BecaViewFilterValue");

                entity.HasOne(d => d.idBecaViewNavigation)
                    .WithMany(p => p.BecaViewFilterValues)
                    .HasForeignKey(d => d.idBecaView)
                    .HasConstraintName("FK_BecaViewFilterValues_BecaView");
            });

            modelBuilder.Entity<BecaViewFilters>(entity =>
            {
                entity.ToView("vBecaViewFilters");
                entity.HasKey(e => new { e.idBecaView, e.idBecaFilter });

                entity.HasOne(d => d.idBecaViewNavigation)
                    .WithMany(p => p.BecaViewFilters)
                    .HasForeignKey(d => d.idBecaView)
                    .HasConstraintName("FK_BecaViewFilters_BecaView");
            });

            modelBuilder.Entity<BecaViewPanels>(entity =>
            {
                entity.HasKey(e => e.idBecaViewPanel);

                entity.HasOne(d => d.IdAggregationTypeNavigation)
                    .WithMany(p => p.BecaViewPanels)
                    .HasForeignKey(d => d.IdAggregationType)
                    .HasConstraintName("FK_BecaViewPanels_BecaAggregationTypes");

                entity.HasOne(d => d.idBecaViewNavigation)
                    .WithMany(p => p.BecaViewPanels)
                    .HasForeignKey(d => d.idBecaView)
                    .HasConstraintName("FK_BecaViewPanels_BecaView");

                entity.HasOne(d => d.IdFormulaNavigation)
                    .WithMany(p => p.BecaViewPanels)
                    .HasForeignKey(d => d.IdFormula)
                    .HasConstraintName("FK_BecaViewPanels_BecaFormula");
            });

            modelBuilder.Entity<BecaViewTypes>(entity =>
            {
                entity.HasKey(e => e.idBecaViewType);

            });

            modelBuilder.Entity<BecaViewFilterUI>(entity =>
            {
                entity.ToView("vBecaViewFilterUI");
                entity.HasKey(e => new { e.idBecaView, e.Name });

            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
