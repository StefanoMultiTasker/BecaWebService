using Entities.Models;
using ExtensionsLib;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Entities.Contexts
{
    public partial class DbBecaContext : DbContext
    {
        //public string domain;
        //public int idUtente;

        private readonly IConfiguration Configuration;

        public virtual DbSet<BecaUser> BecaUsers { get; set; }
        public virtual DbSet<UserMenu> RawUserMenu { get; set; }
        public virtual DbSet<BasicMenu> RawMenu { get; set; }

        public virtual DbSet<Company> Companies { get; set; }

        public virtual DbSet<BecaView> BecaView { get; set; }
        public virtual DbSet<BecaViewData> BecaViewData { get; set; }
        public virtual DbSet<BecaViewDataUser> BecaViewDataUser { get; set; }
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
        public virtual DbSet<BecaViewDetailUI> BecaViewDetailUI { get; set; }
        public virtual DbSet<BecaViewChild> BecaViewChildren { get; set; }
        public virtual DbSet<BecaViewChildData> BecaViewChildData { get; set; }

        public DbSet<BecaViewForm> BecaViewForms { get; set; }
        public DbSet<BecaForm> BecaForm { get; set; }
        public DbSet<BecaFormLevels> BecaFormLevels { get; set; }
        public DbSet<BecaFormField> BecaFormField { get; set; }
        public DbSet<BecaFormFieldLevel> BecaFormFieldLevel { get; set; }

        //public DbdatiContext(IConfiguration configuration)
        //{
        //    Configuration = configuration;
        //}

        //public FormTool _formTool;

        public DbBecaContext(DbContextOptions<DbBecaContext> options) //, FormTool formTool)
            : base(options)
        {
            //_formTool = formTool;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<Company>(entity =>
            {
                entity.ToTable("Company");
                entity.HasKey(e => e.idCompany);
                entity.OwnsMany(p => p.Connections, a =>
                {
                    a.Property<int>("idConnection")
                        .HasColumnType("int");

                    a.Property<int>("idCompany")
                        //.ValueGeneratedOnAdd()
                        .HasColumnType("int");
                    //.HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    a.Property<string>("ConnectionName")
                        .HasColumnType("string");

                    a.Property<string>("ConnectionString")
                        .HasColumnType("string");

                    a.HasKey("idCompany", "idConnection");
                    a.WithOwner()
                        .HasForeignKey("idCompany");
                    a.ToTable("vCompanyConnection");
                });
            });

            #region "Authenticate"

            modelBuilder.Entity<BecaUser>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(e => e.idUtente);
                entity.OwnsMany(p => p.RefreshTokens, a =>
                {
                    a.Property<int>("idUtente")
                        .HasColumnType("int");

                    a.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    a.Property<string>("Token")
                        .HasColumnType("nvarchar(max)");

                    a.Property<DateTime>("Expires")
                        .HasColumnType("datetime2");

                    a.Property<DateTime>("Created")
                        .HasColumnType("datetime2");

                    a.Property<string>("CreatedByIp")
                        .HasColumnType("nvarchar(max)");

                    a.Property<DateTime?>("Revoked")
                        .HasColumnType("datetime2");

                    a.Property<string>("RevokedByIp")
                        .HasColumnType("nvarchar(max)");

                    a.Property<string>("ReplacedByToken")
                        .HasColumnType("nvarchar(max)");

                    a.Property<string>("ReasonRevoked")
                        .HasColumnType("nvarchar(max)");

                    a.HasKey("idUtente", "Id");

                    //a.ToTable("RefreshToken");

                    a.WithOwner()
                        .HasForeignKey("idUtente");

                    a.ToTable("RefreshTokens");
                });
                entity.OwnsMany(c => c.Companies, a =>
                {
                    //a.Property<int>("idUtente")
                    //    .HasColumnType("int");

                    a.Property<int>("idCompany")
                        .HasColumnType("int");

                    a.Property<string>("CompanyName")
                        .HasColumnType("nvarchar(max)");

                    a.Property<int>("isDefault")
                        .HasColumnType("int");

                    a.Property<string>("Logo1url")
                        .HasColumnType("nvarchar(max)");
                    a.Property<string>("Logo2url")
                        .HasColumnType("nvarchar(max)");
                    a.Property<string>("Logo3url")
                        .HasColumnType("nvarchar(max)");
                    a.Property<string>("Logo4url")
                        .HasColumnType("nvarchar(max)");
                    a.Property<string>("Logo5url")
                        .HasColumnType("nvarchar(max)");

                    a.Property<string>("Color1")
                        .HasColumnType("nvarchar(50)");
                    a.Property<string>("Color2")
                        .HasColumnType("nvarchar(50)");
                    a.Property<string>("Color3")
                        .HasColumnType("nvarchar(50)");
                    a.Property<string>("Color4")
                        .HasColumnType("nvarchar(50)");
                    a.Property<string>("Color5")
                        .HasColumnType("nvarchar(50)");

                    a.Property<string>("MainFolder")
                        .HasColumnType("varchar(50)");

                    a.HasKey("idUtente", "idCompany");
                    a.WithOwner()
                        .HasForeignKey("idUtente");
                    a.ToTable("vUsersCompanies");

                    a.OwnsMany(p => p.Profiles, a =>
                    {
                        a.Property<int>("idUtente")
                            .HasColumnType("int");

                        a.Property<int>("idCompany")
                            .HasColumnType("int");

                        a.Property<int>("idProfile")
                            .HasColumnType("int");

                        a.Property<string>("Profile")
                            .HasColumnType("nvarchar(max)");

                        a.Property<bool>("PasswordChange")
                            .HasColumnType("bit");

                        a.Property<bool>("isDefault")
                            .HasColumnType("bit");

                        a.HasKey("idUtente", "idProfile", "idCompany");
                        a.WithOwner()
                            .HasForeignKey("idUtente", "idCompany");
                        a.ToTable("vUsers");
                    });
                });
            });

            modelBuilder.Entity<UserMenu>(entity =>
            {
                entity.ToView("vMenuUser");
                entity.HasKey(e => new { e.idUtente, e.idCompany, e.idItem });
            });

            modelBuilder.Entity<BasicMenu>(entity =>
            {
                entity.ToView("MenuItems");
                entity.HasKey(e => e.idItem);
            });

            #endregion

            #region "Views"

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
                entity.ToView("vBecaView");

                entity.HasOne(d => d.idBecaViewTypeNavigation)
                    .WithMany(p => p.BecaView)
                    .HasForeignKey(d => d.idBecaViewType)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_BecaView_BecaViewTypes");
            });

            modelBuilder.Entity<BecaViewData>(entity =>
            {
                entity.ToView("vBecaViewData");
                entity.HasKey(e => new { e.idBecaView, e.Field });

                entity.HasOne(d => d.idBecaViewNavigation)
                    .WithMany(p => p.BecaViewData)
                    .HasForeignKey(d => d.idBecaView)
                    .HasConstraintName("FK_BecaViewData_BecaView");
            });

            modelBuilder.Entity<BecaViewDataUser>(entity =>
            {
                entity.ToView("BecaViewDataUser");
                entity.HasKey(e => new { e.idBecaView, e.field, e.idCompany, e.idUtente });
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
            modelBuilder.Entity<BecaViewDetailUI>(entity =>
            {
                entity.ToView("vBecaViewDetailUI");
                entity.HasKey(e => new { e.idBecaView, e.Name });

            });
            modelBuilder.Entity<BecaViewChild>(entity =>
            {
                entity.ToView("vBecaFormChild");
                entity.HasKey(e => new { e.form, e.childForm });
                entity.HasAlternateKey(p => p.childForm);
                entity.HasMany<BecaViewChildData>(e => e.BecaFormChildData)
                    .WithOne(x => x.containerForm)
                    .HasPrincipalKey(x => x.childForm)
                    .HasForeignKey(x => x.form);
            });
            modelBuilder.Entity<BecaViewChildData>(entity =>
            {
                entity.ToView("vBecaFormChildFields");
                entity.HasKey(e => new { e.form, e.field });
            });

            #endregion

            #region "Beca"

            modelBuilder.Entity<BecaViewForm>().ToTable("BecaViewForms");
            modelBuilder.Entity<BecaViewForm>().HasKey(p => new { p.idBecaView, p.Form });

            modelBuilder.Entity<BecaForm>().ToTable("vBecaForm");
            modelBuilder.Entity<BecaForm>().HasKey(p => new { p.Form });

            modelBuilder.Entity<BecaFormLevels>().ToTable("BecaFormChild");
            modelBuilder.Entity<BecaFormLevels>().HasKey(p => new { p.Form, p.SubLevel });

            modelBuilder.Entity<BecaFormField>().ToTable("vBecaFormFields");
            modelBuilder.Entity<BecaFormField>().HasKey(p => new { p.Form, p.Name });

            modelBuilder.Entity<BecaFormFieldLevel>().ToTable("_dbaFormsObjAccess");
            modelBuilder.Entity<BecaFormFieldLevel>().HasKey(p => new { p.Form, p.objName, p.idLivello });

            #endregion

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }


}
