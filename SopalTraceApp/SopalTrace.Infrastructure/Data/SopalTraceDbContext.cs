using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Infrastructure.Data;

public partial class SopalTraceDbContext : DbContext
{
    public SopalTraceDbContext(DbContextOptions<SopalTraceDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Atextra> Atextras { get; set; }

    public virtual DbSet<Autili> Autilis { get; set; }

    public virtual DbSet<Defautheque> Defautheques { get; set; }

    public virtual DbSet<Instrument> Instruments { get; set; }

    public virtual DbSet<Itmmaster> Itmmasters { get; set; }

    public virtual DbSet<JournalConnexion> JournalConnexions { get; set; }

    public virtual DbSet<Machine> Machines { get; set; }

    public virtual DbSet<MagExpeditionBl> MagExpeditionBls { get; set; }

    public virtual DbSet<MagExpeditionBlScanOf> MagExpeditionBlScanOfs { get; set; }

    public virtual DbSet<MagPreparationOf> MagPreparationOfs { get; set; }

    public virtual DbSet<MagPreparationOfLot> MagPreparationOfLots { get; set; }

    public virtual DbSet<Mfghead> Mfgheads { get; set; }

    public virtual DbSet<Mfgmat> Mfgmats { get; set; }

    public virtual DbSet<ModeleFabEntete> ModeleFabEntetes { get; set; }

    public virtual DbSet<ModeleFabLigne> ModeleFabLignes { get; set; }

    public virtual DbSet<ModeleFabSection> ModeleFabSections { get; set; }

    public virtual DbSet<MoyenControle> MoyenControles { get; set; }

    public virtual DbSet<NatureComposant> NatureComposants { get; set; }

    public virtual DbSet<NatureComposantOperation> NatureComposantOperations { get; set; }

    public virtual DbSet<Nqa> Nqas { get; set; }

    public virtual DbSet<Operation> Operations { get; set; }

    public virtual DbSet<OutilControle> OutilControles { get; set; }

    public virtual DbSet<Periodicite> Periodicites { get; set; }

    public virtual DbSet<PieceReference> PieceReferences { get; set; }

    public virtual DbSet<PlanAssEntete> PlanAssEntetes { get; set; }

    public virtual DbSet<PlanAssLigne> PlanAssLignes { get; set; }

    public virtual DbSet<PlanAssSection> PlanAssSections { get; set; }

    public virtual DbSet<PlanEchantillonnageEntete> PlanEchantillonnageEntetes { get; set; }

    public virtual DbSet<PlanEchantillonnageRegle> PlanEchantillonnageRegles { get; set; }

    public virtual DbSet<PlanFabEntete> PlanFabEntetes { get; set; }

    public virtual DbSet<PlanFabLigne> PlanFabLignes { get; set; }

    public virtual DbSet<PlanFabSection> PlanFabSections { get; set; }

    public virtual DbSet<PlanNcEntete> PlanNcEntetes { get; set; }

    public virtual DbSet<PlanNcLigne> PlanNcLignes { get; set; }

    public virtual DbSet<PlanPfEntete> PlanPfEntetes { get; set; }

    public virtual DbSet<PlanPfLigne> PlanPfLignes { get; set; }

    public virtual DbSet<PlanPfSection> PlanPfSections { get; set; }

    public virtual DbSet<PlanVerifMachineEcheance> PlanVerifMachineEcheances { get; set; }

    public virtual DbSet<PlanVerifMachineEntete> PlanVerifMachineEntetes { get; set; }

    public virtual DbSet<PlanVerifMachineFamille> PlanVerifMachineFamilles { get; set; }

    public virtual DbSet<PlanVerifMachineLigne> PlanVerifMachineLignes { get; set; }

    public virtual DbSet<PlanVerifMachineMatricePiece> PlanVerifMachineMatricePieces { get; set; }

    public virtual DbSet<PosteTravail> PosteTravails { get; set; }

    public virtual DbSet<RefFamilleCorp> RefFamilleCorps { get; set; }

    public virtual DbSet<RefFormulaire> RefFormulaires { get; set; }

    public virtual DbSet<RefMoyenDetection> RefMoyenDetections { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<RisqueDefaut> RisqueDefauts { get; set; }

    public virtual DbSet<Sdelivery> Sdeliveries { get; set; }

    public virtual DbSet<TypeCaracteristique> TypeCaracteristiques { get; set; }

    public virtual DbSet<TypeControle> TypeControles { get; set; }

    public virtual DbSet<TypeRobinet> TypeRobinets { get; set; }

    public virtual DbSet<TypeSection> TypeSections { get; set; }

    public virtual DbSet<UtilisateursApp> UtilisateursApps { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Atextra>(entity =>
        {
            entity.HasKey(e => new { e.Codfic0, e.Zone0, e.Ident10, e.Langue0 }).HasName("PK__ATEXTRA__4F21B2DBBF34255C");

            entity.ToTable("ATEXTRA");

            entity.Property(e => e.Codfic0)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("CODFIC_0");
            entity.Property(e => e.Zone0)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ZONE_0");
            entity.Property(e => e.Ident10)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("IDENT1_0");
            entity.Property(e => e.Langue0)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasColumnName("LANGUE_0");
            entity.Property(e => e.Texte0)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("TEXTE_0");
        });

        modelBuilder.Entity<Autili>(entity =>
        {
            entity.HasKey(e => e.Usr0).HasName("PK__AUTILIS__0812AE69F03DE051");

            entity.ToTable("AUTILIS");

            entity.Property(e => e.Usr0)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("USR_0");
            entity.Property(e => e.Addeml0)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("ADDEML_0");
            entity.Property(e => e.Codmet0)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("CODMET_0");
            entity.Property(e => e.Enaflg0)
                .HasDefaultValue(1)
                .HasColumnName("ENAFLG_0");
            entity.Property(e => e.Intusr0)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("INTUSR_0");
        });

        modelBuilder.Entity<Defautheque>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Defauthe__3214EC0719C2F966");

            entity.ToTable("Defautheque", tb => tb.HasTrigger("trg_no_del_Defaut"));

            entity.HasIndex(e => e.Code, "UQ__Defauthe__A25C5AA71EA3BF5A").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.Code)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Description)
                .HasMaxLength(200)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Instrument>(entity =>
        {
            entity.HasKey(e => e.CodeInstrument).HasName("PK__Instrume__E6E43505A086097C");

            entity.ToTable("Instrument", tb => tb.HasTrigger("trg_no_del_Instrument"));

            entity.Property(e => e.CodeInstrument)
                .HasMaxLength(40)
                .IsUnicode(false);
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.Categorie)
                .HasMaxLength(40)
                .IsUnicode(false);
            entity.Property(e => e.Designation)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Statut)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("ACTIF");
            entity.Property(e => e.Unite)
                .HasMaxLength(10)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Itmmaster>(entity =>
        {
            entity.HasKey(e => e.CodeArticle).HasName("PK__ITMMASTE__32384FB07FBC73FF");

            entity.ToTable("ITMMASTER");

            entity.Property(e => e.CodeArticle)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Designation)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Designation2)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.FamilleProduit)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.NatureComposantCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Statut)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.TypeRobinetCode)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.NatureComposantCodeNavigation).WithMany(p => p.Itmmasters)
                .HasForeignKey(d => d.NatureComposantCode)
                .HasConstraintName("FK__ITMMASTER__Natur__59C55456");

            entity.HasOne(d => d.TypeRobinetCodeNavigation).WithMany(p => p.Itmmasters)
                .HasForeignKey(d => d.TypeRobinetCode)
                .HasConstraintName("FK__ITMMASTER__TypeR__58D1301D");
        });

        modelBuilder.Entity<JournalConnexion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__JournalC__3214EC077F8C2158");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Action)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.DateAction)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Details)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Matricule)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Machine>(entity =>
        {
            entity.HasKey(e => e.CodeMachine).HasName("PK__Machine__50D6760F71B9CC0D");

            entity.ToTable("Machine", tb => tb.HasTrigger("trg_no_del_Machine"));

            entity.Property(e => e.CodeMachine)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.Libelle)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.OperationCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.RoleMachine)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.TypeAffectation)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasDefaultValue("INDEPENDANTE");
            entity.Property(e => e.TypeRobinetCode)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.OperationCodeNavigation).WithMany(p => p.Machines)
                .HasForeignKey(d => d.OperationCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Machine__Operati__07C12930");

            entity.HasOne(d => d.TypeRobinetCodeNavigation).WithMany(p => p.Machines)
                .HasForeignKey(d => d.TypeRobinetCode)
                .HasConstraintName("FK__Machine__TypeRob__06CD04F7");
        });

        modelBuilder.Entity<MagExpeditionBl>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Mag_Expe__3214EC07F1569BEA");

            entity.ToTable("Mag_ExpeditionBL");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.DateDebut)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DateFin).HasColumnType("datetime");
            entity.Property(e => e.MatriculeMagasinier)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.NumeroBl)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("NumeroBL");
            entity.Property(e => e.Statut)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("EN_COURS");

            entity.HasOne(d => d.MatriculeMagasinierNavigation).WithMany(p => p.MagExpeditionBls)
                .HasPrincipalKey(p => p.Matricule)
                .HasForeignKey(d => d.MatriculeMagasinier)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Mag_Exped__Matri__656C112C");
        });

        modelBuilder.Entity<MagExpeditionBlScanOf>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Mag_Expe__3214EC07846F9067");

            entity.ToTable("Mag_ExpeditionBL_ScanOF");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.DateScan)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ExpeditionBlid).HasColumnName("ExpeditionBLId");
            entity.Property(e => e.NumeroOfscanne)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("NumeroOFScanne");

            entity.HasOne(d => d.ExpeditionBl).WithMany(p => p.MagExpeditionBlScanOfs)
                .HasForeignKey(d => d.ExpeditionBlid)
                .HasConstraintName("FK__Mag_Exped__Exped__6C190EBB");
        });

        modelBuilder.Entity<MagPreparationOf>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Mag_Prep__3214EC0735C25FA2");

            entity.ToTable("Mag_PreparationOF");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.DateDebut)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DateFin).HasColumnType("datetime");
            entity.Property(e => e.MatriculeMagasinier)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.NumeroOf)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("NumeroOF");
            entity.Property(e => e.Statut)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("EN_COURS");

            entity.HasOne(d => d.MatriculeMagasinierNavigation).WithMany(p => p.MagPreparationOfs)
                .HasPrincipalKey(p => p.Matricule)
                .HasForeignKey(d => d.MatriculeMagasinier)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Mag_Prepa__Matri__59FA5E80");
        });

        modelBuilder.Entity<MagPreparationOfLot>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Mag_Prep__3214EC079765187F");

            entity.ToTable("Mag_PreparationOF_Lot");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CodeComposant)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.DateScan)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.NumeroLotScanne)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PreparationOfid).HasColumnName("PreparationOFId");

            entity.HasOne(d => d.PreparationOf).WithMany(p => p.MagPreparationOfLots)
                .HasForeignKey(d => d.PreparationOfid)
                .HasConstraintName("FK__Mag_Prepa__Prepa__60A75C0F");
        });

        modelBuilder.Entity<Mfghead>(entity =>
        {
            entity.HasKey(e => e.NumeroOf).HasName("PK__MFGHEAD__C6A65F3059BBBBFB");

            entity.ToTable("MFGHEAD");

            entity.Property(e => e.NumeroOf)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("NumeroOF");
            entity.Property(e => e.CodeArticle)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.StatutOf)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("StatutOF");

            entity.HasOne(d => d.CodeArticleNavigation).WithMany(p => p.Mfgheads)
                .HasForeignKey(d => d.CodeArticle)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__MFGHEAD__CodeArt__4F7CD00D");
        });

        modelBuilder.Entity<Mfgmat>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__MFGMAT__3214EC07E7406E6A");

            entity.ToTable("MFGMAT");

            entity.Property(e => e.CodeArticle)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.NumeroOf)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("NumeroOF");

            entity.HasOne(d => d.CodeArticleNavigation).WithMany(p => p.Mfgmats)
                .HasForeignKey(d => d.CodeArticle)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__MFGMAT__CodeArti__534D60F1");

            entity.HasOne(d => d.NumeroOfNavigation).WithMany(p => p.Mfgmats)
                .HasForeignKey(d => d.NumeroOf)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__MFGMAT__NumeroOF__52593CB8");
        });

        modelBuilder.Entity<ModeleFabEntete>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Modele_F__3214EC07ECF3B8C3");

            entity.ToTable("Modele_Fab_Entete", tb => tb.HasTrigger("trg_no_del_ModeleFab"));

            entity.HasIndex(e => new { e.TypeRobinetCode, e.NatureComposantCode, e.OperationCode, e.Version }, "UQ_ModeleFab_Version")
                .IsUnique()
                .HasFilter("([Statut] IN ('BROUILLON', 'ACTIF', 'ARCHIVE'))");

            entity.HasIndex(e => new { e.TypeRobinetCode, e.NatureComposantCode, e.OperationCode }, "UX_ModeleFab_Actif")
                .IsUnique()
                .HasFilter("([Statut]='ACTIF')");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.ArchiveLe).HasColumnType("datetime");
            entity.Property(e => e.ArchivePar)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Code)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.CreeLe)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.CreePar)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Libelle)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.NatureComposantCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.OperationCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Statut)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("BROUILLON");
            entity.Property(e => e.TypeRobinetCode)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Version).HasDefaultValue(1);

            entity.HasOne(d => d.Formulaire).WithMany(p => p.ModeleFabEntetes)
                .HasForeignKey(d => d.FormulaireId)
                .HasConstraintName("FK__Modele_Fa__Formu__04AFB25B");

            entity.HasOne(d => d.NatureComposantCodeNavigation).WithMany(p => p.ModeleFabEntetes)
                .HasForeignKey(d => d.NatureComposantCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Modele_Fa__Natur__02C769E9");

            entity.HasOne(d => d.OperationCodeNavigation).WithMany(p => p.ModeleFabEntetes)
                .HasForeignKey(d => d.OperationCode)
                .HasConstraintName("FK__Modele_Fa__Opera__03BB8E22");

            entity.HasOne(d => d.TypeRobinetCodeNavigation).WithMany(p => p.ModeleFabEntetes)
                .HasForeignKey(d => d.TypeRobinetCode)
                .HasConstraintName("FK__Modele_Fa__TypeR__01D345B0");
        });

        modelBuilder.Entity<ModeleFabLigne>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Modele_F__3214EC071ABF0671");

            entity.ToTable("Modele_Fab_Ligne");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.InstrumentCode)
                .HasMaxLength(40)
                .IsUnicode(false);
            entity.Property(e => e.LibelleAffiche)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.LimiteSpecTexte)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.MoyenTexteLibre)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Unite)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.InstrumentCodeNavigation).WithMany(p => p.ModeleFabLignes)
                .HasForeignKey(d => d.InstrumentCode)
                .HasConstraintName("FK__Modele_Fa__Instr__17C286CF");

            entity.HasOne(d => d.ModeleEntete).WithMany(p => p.ModeleFabLignes)
                .HasForeignKey(d => d.ModeleEnteteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Modele_Fa__Model__1209AD79");

            entity.HasOne(d => d.MoyenControle).WithMany(p => p.ModeleFabLignes)
                .HasForeignKey(d => d.MoyenControleId)
                .HasConstraintName("FK__Modele_Fa__Moyen__16CE6296");

            entity.HasOne(d => d.Periodicite).WithMany(p => p.ModeleFabLignes)
                .HasForeignKey(d => d.PeriodiciteId)
                .HasConstraintName("FK__Modele_Fa__Perio__18B6AB08");

            entity.HasOne(d => d.Section).WithMany(p => p.ModeleFabLignes)
                .HasForeignKey(d => d.SectionId)
                .HasConstraintName("FK__Modele_Fa__Secti__12FDD1B2");

            entity.HasOne(d => d.TypeCaracteristique).WithMany(p => p.ModeleFabLignes)
                .HasForeignKey(d => d.TypeCaracteristiqueId)
                .HasConstraintName("FK__Modele_Fa__TypeC__14E61A24");

            entity.HasOne(d => d.TypeControle).WithMany(p => p.ModeleFabLignes)
                .HasForeignKey(d => d.TypeControleId)
                .HasConstraintName("FK__Modele_Fa__TypeC__15DA3E5D");
        });

        modelBuilder.Entity<ModeleFabSection>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Modele_F__3214EC07825DEC05");

            entity.ToTable("Modele_Fab_Section");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.FrequenceLibelle)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.LibelleSection)
                .HasMaxLength(200)
                .IsUnicode(false);

            entity.HasOne(d => d.ModeleEntete).WithMany(p => p.ModeleFabSections)
                .HasForeignKey(d => d.ModeleEnteteId)
                .HasConstraintName("FK__Modele_Fa__Model__0D44F85C");
        });

        modelBuilder.Entity<MoyenControle>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__MoyenCon__3214EC07D6A95467");

            entity.ToTable("MoyenControle", tb => tb.HasTrigger("trg_no_del_Moyen"));

            entity.HasIndex(e => e.Code, "UQ__MoyenCon__A25C5AA78E01F7DF").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.Code)
                .HasMaxLength(40)
                .IsUnicode(false);
            entity.Property(e => e.Libelle)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<NatureComposant>(entity =>
        {
            entity.HasKey(e => e.Code).HasName("PK__NatureCo__A25C5AA62FF1DC3C");

            entity.ToTable("NatureComposant", tb => tb.HasTrigger("trg_no_del_NatureComposant"));

            entity.Property(e => e.Code)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.Libelle)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.TypeLotAttendu)
                .HasMaxLength(30)
                .IsUnicode(false);
        });

        modelBuilder.Entity<NatureComposantOperation>(entity =>
        {
            entity.HasKey(e => new { e.NatureComposantCode, e.OperationCode }).HasName("PK__NatureCo__3BFCC376C44B7C47");

            entity.ToTable("NatureComposant_Operation");

            entity.Property(e => e.NatureComposantCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.OperationCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.EstObligatoire).HasDefaultValue(true);
            entity.Property(e => e.OrdreGamme).HasDefaultValue(1);

            entity.HasOne(d => d.NatureComposantCodeNavigation).WithMany(p => p.NatureComposantOperations)
                .HasForeignKey(d => d.NatureComposantCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__NatureCom__Natur__7A672E12");

            entity.HasOne(d => d.OperationCodeNavigation).WithMany(p => p.NatureComposantOperations)
                .HasForeignKey(d => d.OperationCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__NatureCom__Opera__7B5B524B");
        });

        modelBuilder.Entity<Nqa>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__NQA__3214EC07FBA5D046");

            entity.ToTable("NQA");

            entity.HasIndex(e => e.ValeurNqa, "UQ__NQA__1DA3E248777CE356").IsUnique();

            entity.Property(e => e.ValeurNqa).HasColumnName("ValeurNQA");
        });

        modelBuilder.Entity<Operation>(entity =>
        {
            entity.HasKey(e => e.Code).HasName("PK__Operatio__A25C5AA6335C84D1");

            entity.ToTable("Operation", tb => tb.HasTrigger("trg_no_del_Operation"));

            entity.Property(e => e.Code)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.Libelle)
                .HasMaxLength(80)
                .IsUnicode(false);
        });

        modelBuilder.Entity<OutilControle>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__OutilCon__3214EC070EA85492");

            entity.ToTable("OutilControle");

            entity.HasIndex(e => e.Code, "UQ__OutilCon__A25C5AA784D52AA5").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.Code)
                .HasMaxLength(40)
                .IsUnicode(false);
            entity.Property(e => e.Libelle)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.LimiteSpecTexteDefaut)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.MoyenControle).WithMany(p => p.OutilControles)
                .HasForeignKey(d => d.MoyenControleId)
                .HasConstraintName("FK__OutilCont__Moyen__55F4C372");

            entity.HasOne(d => d.PeriodiciteDefaut).WithMany(p => p.OutilControles)
                .HasForeignKey(d => d.PeriodiciteDefautId)
                .HasConstraintName("FK__OutilCont__Perio__56E8E7AB");

            entity.HasOne(d => d.TypeCaracteristique).WithMany(p => p.OutilControles)
                .HasForeignKey(d => d.TypeCaracteristiqueId)
                .HasConstraintName("FK__OutilCont__TypeC__55009F39");

            entity.HasOne(d => d.TypeControle).WithMany(p => p.OutilControles)
                .HasForeignKey(d => d.TypeControleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__OutilCont__TypeC__540C7B00");
        });

        modelBuilder.Entity<Periodicite>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Periodic__3214EC074129319E");

            entity.ToTable("Periodicite", tb => tb.HasTrigger("trg_no_del_Perio"));

            entity.HasIndex(e => e.Code, "UQ__Periodic__A25C5AA72DF2B109").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.Code)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.FrequenceUnite)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Libelle)
                .HasMaxLength(200)
                .IsUnicode(false);
        });

        modelBuilder.Entity<PieceReference>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__PieceRef__3214EC073AD13C4D");

            entity.ToTable("PieceReference", tb => tb.HasTrigger("trg_no_del_PieceRef"));

            entity.HasIndex(e => e.Code, "UQ__PieceRef__A25C5AA76E1F5EC7").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.Code)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Designation)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.FamilleDesc)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.MachineCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.TypePiece)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.MachineCodeNavigation).WithMany(p => p.PieceReferences)
                .HasForeignKey(d => d.MachineCode)
                .HasConstraintName("FK__PieceRefe__Machi__3D2915A8");
        });

        modelBuilder.Entity<PlanAssEntete>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Ass__3214EC07881B4E73");

            entity.ToTable("Plan_Ass_Entete", tb => tb.HasTrigger("trg_no_del_PlanAss"));

            entity.HasIndex(e => new { e.OperationCode, e.TypeRobinetCode, e.CodeArticleSage, e.Version }, "UQ_PlanAss_Instance_Version")
                .IsUnique()
                .HasFilter("([EstModele]=(0) AND ([Statut] IN ('BROUILLON', 'ACTIF', 'ARCHIVE')))");

            entity.HasIndex(e => new { e.OperationCode, e.TypeRobinetCode, e.Version }, "UQ_PlanAss_Modele_Version")
                .IsUnique()
                .HasFilter("([EstModele]=(1) AND ([Statut] IN ('BROUILLON', 'ACTIF', 'ARCHIVE')))");

            entity.HasIndex(e => new { e.OperationCode, e.TypeRobinetCode, e.CodeArticleSage }, "UX_PlanAss_Exception_Actif")
                .IsUnique()
                .HasFilter("([EstModele]=(0) AND [Statut]='ACTIF')");

            entity.HasIndex(e => new { e.OperationCode, e.TypeRobinetCode }, "UX_PlanAss_Maitre_Actif")
                .IsUnique()
                .HasFilter("([EstModele]=(1) AND [Statut]='ACTIF')");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CodeArticleSage)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.CreeLe)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.CreePar)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Designation)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.EstModele).HasDefaultValue(true);
            entity.Property(e => e.ModifieLe).HasColumnType("datetime");
            entity.Property(e => e.ModifiePar)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.NbPiecesReglage).HasDefaultValue(5);
            entity.Property(e => e.Nom)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.OperationCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Statut)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("BROUILLON");
            entity.Property(e => e.TypeRobinetCode)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Version).HasDefaultValue(1);

            entity.HasOne(d => d.FicheEchantillonnage).WithMany(p => p.PlanAssEntetes)
                .HasForeignKey(d => d.FicheEchantillonnageId)
                .HasConstraintName("fk_planass_echan");

            entity.HasOne(d => d.Formulaire).WithMany(p => p.PlanAssEntetes)
                .HasForeignKey(d => d.FormulaireId)
                .HasConstraintName("FK__Plan_Ass___Formu__40C49C62");

            entity.HasOne(d => d.OperationCodeNavigation).WithMany(p => p.PlanAssEntetes)
                .HasForeignKey(d => d.OperationCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_Ass___Opera__3A179ED3");

            entity.HasOne(d => d.TypeRobinetCodeNavigation).WithMany(p => p.PlanAssEntetes)
                .HasForeignKey(d => d.TypeRobinetCode)
                .HasConstraintName("FK__Plan_Ass___TypeR__3B0BC30C");
        });

        modelBuilder.Entity<PlanAssLigne>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Ass__3214EC0728B6787D");

            entity.ToTable("Plan_Ass_Ligne");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.InstrumentCode)
                .HasMaxLength(40)
                .IsUnicode(false);
            entity.Property(e => e.LibelleAffiche)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.LimiteSpecTexte)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.MachineCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.MoyenTexteLibre)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.RefPlanProduit)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.Unite)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.Defautheque).WithMany(p => p.PlanAssLignes)
                .HasForeignKey(d => d.DefauthequeId)
                .HasConstraintName("FK__Plan_Ass___Defau__589C25F3");

            entity.HasOne(d => d.InstrumentCodeNavigation).WithMany(p => p.PlanAssLignes)
                .HasForeignKey(d => d.InstrumentCode)
                .HasConstraintName("FK__Plan_Ass___Instr__55BFB948");

            entity.HasOne(d => d.MachineCodeNavigation).WithMany(p => p.PlanAssLignes)
                .HasForeignKey(d => d.MachineCode)
                .HasConstraintName("FK__Plan_Ass___Machi__54CB950F");

            entity.HasOne(d => d.MoyenControle).WithMany(p => p.PlanAssLignes)
                .HasForeignKey(d => d.MoyenControleId)
                .HasConstraintName("FK__Plan_Ass___Moyen__53D770D6");

            entity.HasOne(d => d.Periodicite).WithMany(p => p.PlanAssLignes)
                .HasForeignKey(d => d.PeriodiciteId)
                .HasConstraintName("FK__Plan_Ass___Perio__56B3DD81");

            entity.HasOne(d => d.PlanEntete).WithMany(p => p.PlanAssLignes)
                .HasForeignKey(d => d.PlanEnteteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_Ass___PlanE__4F12BBB9");

            entity.HasOne(d => d.Section).WithMany(p => p.PlanAssLignes)
                .HasForeignKey(d => d.SectionId)
                .HasConstraintName("FK__Plan_Ass___Secti__5006DFF2");

            entity.HasOne(d => d.TypeCaracteristique).WithMany(p => p.PlanAssLignes)
                .HasForeignKey(d => d.TypeCaracteristiqueId)
                .HasConstraintName("FK__Plan_Ass___TypeC__51EF2864");

            entity.HasOne(d => d.TypeControle).WithMany(p => p.PlanAssLignes)
                .HasForeignKey(d => d.TypeControleId)
                .HasConstraintName("FK__Plan_Ass___TypeC__52E34C9D");
        });

        modelBuilder.Entity<PlanAssSection>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Ass__3214EC070AC0112A");

            entity.ToTable("Plan_Ass_Section");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.LibelleSection)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.NormeReference)
                .HasMaxLength(40)
                .IsUnicode(false);

            entity.HasOne(d => d.Nqa).WithMany(p => p.PlanAssSections)
                .HasForeignKey(d => d.NqaId)
                .HasConstraintName("FK__Plan_Ass___NqaId__4B422AD5");

            entity.HasOne(d => d.Periodicite).WithMany(p => p.PlanAssSections)
                .HasForeignKey(d => d.PeriodiciteId)
                .HasConstraintName("FK__Plan_Ass___Perio__4A4E069C");

            entity.HasOne(d => d.PlanEntete).WithMany(p => p.PlanAssSections)
                .HasForeignKey(d => d.PlanEnteteId)
                .HasConstraintName("FK__Plan_Ass___PlanE__477199F1");

            entity.HasOne(d => d.TypeSection).WithMany(p => p.PlanAssSections)
                .HasForeignKey(d => d.TypeSectionId)
                .HasConstraintName("FK__Plan_Ass___TypeS__4959E263");
        });

        modelBuilder.Entity<PlanEchantillonnageEntete>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Ech__3214EC07B670E189");

            entity.ToTable("Plan_Echantillonnage_Entete", tb => tb.HasTrigger("trg_no_del_PlanEchan"));

            entity.HasIndex(e => new { e.CodeReference, e.CodeArticleSage, e.Version }, "UQ_PlanEchan_Article_Version")
                .IsUnique()
                .HasFilter("([CodeArticleSage] IS NOT NULL AND ([Statut] IN ('BROUILLON', 'ACTIF', 'ARCHIVE')))");

            entity.HasIndex(e => new { e.CodeReference, e.Version }, "UQ_PlanEchan_Generic_Version")
                .IsUnique()
                .HasFilter("([CodeArticleSage] IS NULL AND ([Statut] IN ('BROUILLON', 'ACTIF', 'ARCHIVE')))");

            entity.HasIndex(e => new { e.CodeReference, e.CodeArticleSage }, "UX_PlanEchan_Actif_Article")
                .IsUnique()
                .HasFilter("([CodeArticleSage] IS NOT NULL AND [Statut]='ACTIF')");

            entity.HasIndex(e => e.CodeReference, "UX_PlanEchan_Actif_Generic")
                .IsUnique()
                .HasFilter("([CodeArticleSage] IS NULL AND [Statut]='ACTIF')");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CodeArticleSage)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.CodeReference)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.CreeLe)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.CreePar)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.MachineCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.ModeControle)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.NiveauControle)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.Statut)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("ACTIF");
            entity.Property(e => e.TypePlan)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Version).HasDefaultValue(1);

            entity.HasOne(d => d.Formulaire).WithMany(p => p.PlanEchantillonnageEntetes)
                .HasForeignKey(d => d.FormulaireId)
                .HasConstraintName("FK__Plan_Echa__Formu__6FB49575");

            entity.HasOne(d => d.MachineCodeNavigation).WithMany(p => p.PlanEchantillonnageEntetes)
                .HasForeignKey(d => d.MachineCode)
                .HasConstraintName("FK__Plan_Echa__Machi__6EC0713C");

            entity.HasOne(d => d.Nqa).WithMany(p => p.PlanEchantillonnageEntetes)
                .HasForeignKey(d => d.NqaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_Echa__NqaId__73852659");
        });

        modelBuilder.Entity<PlanEchantillonnageRegle>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Ech__3214EC07768E000E");

            entity.ToTable("Plan_Echantillonnage_Regle");

            entity.HasIndex(e => new { e.FicheEnteteId, e.LettreCode }, "UQ__Plan_Ech__D6AC40B61BE43298").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CritereAcceptationAc).HasColumnName("CritereAcceptation_Ac");
            entity.Property(e => e.CritereRejetRe).HasColumnName("CritereRejet_Re");
            entity.Property(e => e.EffectifEchantillonA).HasColumnName("EffectifEchantillon_A");
            entity.Property(e => e.EffectifParPosteAb).HasColumnName("EffectifParPoste_AB");
            entity.Property(e => e.LettreCode)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.NbPostesB)
                .HasDefaultValue(1)
                .HasColumnName("NbPostes_B");

            entity.HasOne(d => d.FicheEntete).WithMany(p => p.PlanEchantillonnageRegles)
                .HasForeignKey(d => d.FicheEnteteId)
                .HasConstraintName("FK__Plan_Echa__Fiche__7D0E9093");
        });

        modelBuilder.Entity<PlanFabEntete>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Fab__3214EC071BC7202B");

            entity.ToTable("Plan_Fab_Entete", tb => tb.HasTrigger("trg_no_del_PlanFab"));

            entity.HasIndex(e => new { e.CodeArticleSage, e.OperationCode, e.Version }, "UQ_PlanFab_Version")
                .IsUnique()
                .HasFilter("([Statut] IN ('BROUILLON', 'ACTIF', 'ARCHIVE'))");

            entity.HasIndex(e => new { e.CodeArticleSage, e.OperationCode }, "UX_PlanFab_Actif")
                .IsUnique()
                .HasFilter("([Statut]='ACTIF')");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CodeArticleSage)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.CreeLe)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.CreePar)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Designation)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.MachineDefautCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.ModifieLe).HasColumnType("datetime");
            entity.Property(e => e.ModifiePar)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Nom)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.OperationCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Statut)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("BROUILLON");
            entity.Property(e => e.Version).HasDefaultValue(1);

            entity.HasOne(d => d.Formulaire).WithMany(p => p.PlanFabEntetes)
                .HasForeignKey(d => d.FormulaireId)
                .HasConstraintName("FK__Plan_Fab___Formu__22401542");

            entity.HasOne(d => d.MachineDefautCodeNavigation).WithMany(p => p.PlanFabEntetes)
                .HasForeignKey(d => d.MachineDefautCode)
                .HasConstraintName("FK__Plan_Fab___Machi__214BF109");

            entity.HasOne(d => d.ModeleSource).WithMany(p => p.PlanFabEntetes)
                .HasForeignKey(d => d.ModeleSourceId)
                .HasConstraintName("FK__Plan_Fab___Model__1D7B6025");
        });

        modelBuilder.Entity<PlanFabLigne>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Fab__3214EC073D48F445");

            entity.ToTable("Plan_Fab_Ligne");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.InstrumentCode)
                .HasMaxLength(40)
                .IsUnicode(false);
            entity.Property(e => e.LibelleAffiche)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.LimiteSpecTexte)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.MoyenTexteLibre)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Unite)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.InstrumentCodeNavigation).WithMany(p => p.PlanFabLignes)
                .HasForeignKey(d => d.InstrumentCode)
                .HasConstraintName("FK__Plan_Fab___Instr__345EC57D");

            entity.HasOne(d => d.ModeleLigneSource).WithMany(p => p.PlanFabLignes)
                .HasForeignKey(d => d.ModeleLigneSourceId)
                .HasConstraintName("FK__Plan_Fab___Model__2F9A1060");

            entity.HasOne(d => d.MoyenControle).WithMany(p => p.PlanFabLignes)
                .HasForeignKey(d => d.MoyenControleId)
                .HasConstraintName("FK__Plan_Fab___Moyen__336AA144");

            entity.HasOne(d => d.Periodicite).WithMany(p => p.PlanFabLignes)
                .HasForeignKey(d => d.PeriodiciteId)
                .HasConstraintName("FK__Plan_Fab___Perio__3552E9B6");

            entity.HasOne(d => d.PlanEntete).WithMany(p => p.PlanFabLignes)
                .HasForeignKey(d => d.PlanEnteteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_Fab___PlanE__2DB1C7EE");

            entity.HasOne(d => d.Section).WithMany(p => p.PlanFabLignes)
                .HasForeignKey(d => d.SectionId)
                .HasConstraintName("FK__Plan_Fab___Secti__2EA5EC27");

            entity.HasOne(d => d.TypeCaracteristique).WithMany(p => p.PlanFabLignes)
                .HasForeignKey(d => d.TypeCaracteristiqueId)
                .HasConstraintName("FK__Plan_Fab___TypeC__318258D2");

            entity.HasOne(d => d.TypeControle).WithMany(p => p.PlanFabLignes)
                .HasForeignKey(d => d.TypeControleId)
                .HasConstraintName("FK__Plan_Fab___TypeC__32767D0B");
        });

        modelBuilder.Entity<PlanFabSection>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Fab__3214EC07F0797D9F");

            entity.ToTable("Plan_Fab_Section");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.FrequenceLibelle)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.LibelleSection)
                .HasMaxLength(200)
                .IsUnicode(false);

            entity.HasOne(d => d.ModeleSection).WithMany(p => p.PlanFabSections)
                .HasForeignKey(d => d.ModeleSectionId)
                .HasConstraintName("FK__Plan_Fab___Model__28ED12D1");

            entity.HasOne(d => d.PlanEntete).WithMany(p => p.PlanFabSections)
                .HasForeignKey(d => d.PlanEnteteId)
                .HasConstraintName("FK__Plan_Fab___PlanE__27F8EE98");
        });

        modelBuilder.Entity<PlanNcEntete>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_NC___3214EC072487253D");

            entity.ToTable("Plan_NC_Entete");

            entity.HasIndex(e => new { e.PosteCode, e.Version }, "UQ_PlanNC_Version")
                .IsUnique()
                .HasFilter("([Statut] IN ('BROUILLON', 'ACTIF', 'ARCHIVE'))");

            entity.HasIndex(e => e.PosteCode, "UX_PlanNC_Actif")
                .IsUnique()
                .HasFilter("([Statut]='ACTIF')");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreeLe)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.CreePar)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.ModifieLe).HasColumnType("datetime");
            entity.Property(e => e.ModifiePar)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Nom)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.PosteCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Statut)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("BROUILLON");
            entity.Property(e => e.Version).HasDefaultValue(1);

            entity.HasOne(d => d.PosteCodeNavigation).WithOne(p => p.PlanNcEntete)
                .HasForeignKey<PlanNcEntete>(d => d.PosteCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_NC_E__Poste__79FD19BE");
        });

        modelBuilder.Entity<PlanNcLigne>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_NC___3214EC077E0E0F47");

            entity.ToTable("Plan_NC_Ligne");

            entity.HasIndex(e => new { e.PlanNcenteteId, e.OrdreAffiche }, "UQ__Plan_NC___90C617389F0603CD").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.MachineCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.PlanNcenteteId).HasColumnName("PlanNCEnteteId");

            entity.HasOne(d => d.MachineCodeNavigation).WithMany(p => p.PlanNcLignes)
                .HasForeignKey(d => d.MachineCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_NC_L__Machi__038683F8");

            entity.HasOne(d => d.PlanNcentete).WithMany(p => p.PlanNcLignes)
                .HasForeignKey(d => d.PlanNcenteteId)
                .HasConstraintName("FK__Plan_NC_L__PlanN__02925FBF");

            entity.HasOne(d => d.RisqueDefaut).WithMany(p => p.PlanNcLignes)
                .HasForeignKey(d => d.RisqueDefautId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_NC_L__Risqu__047AA831");
        });

        modelBuilder.Entity<PlanPfEntete>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_PF___3214EC07E7D53C39");

            entity.ToTable("Plan_PF_Entete", tb => tb.HasTrigger("trg_no_del_PlanPF"));

            entity.HasIndex(e => new { e.TypeRobinetCode, e.CodeArticleSage, e.Version }, "UQ_PlanPF_Version")
                .IsUnique()
                .HasFilter("([Statut] IN ('BROUILLON', 'ACTIF', 'ARCHIVE'))");

            entity.HasIndex(e => new { e.TypeRobinetCode, e.CodeArticleSage }, "UX_PlanPF_Actif")
                .IsUnique()
                .HasFilter("([Statut]='ACTIF')");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CodeArticleSage)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.CreeLe)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.CreePar)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Designation)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.ModifieLe).HasColumnType("datetime");
            entity.Property(e => e.ModifiePar)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Statut)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("BROUILLON");
            entity.Property(e => e.TypeRobinetCode)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Version).HasDefaultValue(1);

            entity.HasOne(d => d.TypeRobinetCodeNavigation).WithMany(p => p.PlanPfEntetes)
                .HasForeignKey(d => d.TypeRobinetCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_PF_E__TypeR__5D60DB10");
        });

        modelBuilder.Entity<PlanPfLigne>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_PF___3214EC07CB90FDF7");

            entity.ToTable("Plan_PF_Ligne");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.InstrumentCode)
                .HasMaxLength(40)
                .IsUnicode(false);
            entity.Property(e => e.LibelleAffiche)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.LimiteSpecTexte)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.MoyenTexteLibre)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Unite)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.Defautheque).WithMany(p => p.PlanPfLignes)
                .HasForeignKey(d => d.DefauthequeId)
                .HasConstraintName("FK__Plan_PF_L__Defau__753864A1");

            entity.HasOne(d => d.InstrumentCodeNavigation).WithMany(p => p.PlanPfLignes)
                .HasForeignKey(d => d.InstrumentCode)
                .HasConstraintName("FK__Plan_PF_L__Instr__74444068");

            entity.HasOne(d => d.MoyenControle).WithMany(p => p.PlanPfLignes)
                .HasForeignKey(d => d.MoyenControleId)
                .HasConstraintName("FK__Plan_PF_L__Moyen__73501C2F");

            entity.HasOne(d => d.PlanEntete).WithMany(p => p.PlanPfLignes)
                .HasForeignKey(d => d.PlanEnteteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_PF_L__PlanE__6E8B6712");

            entity.HasOne(d => d.Section).WithMany(p => p.PlanPfLignes)
                .HasForeignKey(d => d.SectionId)
                .HasConstraintName("FK__Plan_PF_L__Secti__6F7F8B4B");

            entity.HasOne(d => d.TypeCaracteristique).WithMany(p => p.PlanPfLignes)
                .HasForeignKey(d => d.TypeCaracteristiqueId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_PF_L__TypeC__7167D3BD");

            entity.HasOne(d => d.TypeControle).WithMany(p => p.PlanPfLignes)
                .HasForeignKey(d => d.TypeControleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_PF_L__TypeC__725BF7F6");
        });

        modelBuilder.Entity<PlanPfSection>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_PF___3214EC0762A962D8");

            entity.ToTable("Plan_PF_Section");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.LibelleSection)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.NormeReference)
                .HasMaxLength(40)
                .IsUnicode(false);

            entity.HasOne(d => d.Nqa).WithMany(p => p.PlanPfSections)
                .HasForeignKey(d => d.NqaId)
                .HasConstraintName("FK__Plan_PF_S__NqaId__6ABAD62E");

            entity.HasOne(d => d.PlanEntete).WithMany(p => p.PlanPfSections)
                .HasForeignKey(d => d.PlanEnteteId)
                .HasConstraintName("FK__Plan_PF_S__PlanE__67DE6983");

            entity.HasOne(d => d.TypeSection).WithMany(p => p.PlanPfSections)
                .HasForeignKey(d => d.TypeSectionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_PF_S__TypeS__69C6B1F5");
        });

        modelBuilder.Entity<PlanVerifMachineEcheance>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Ver__3214EC076E589D1C");

            entity.ToTable("Plan_VerifMachine_Echeance");

            entity.HasIndex(e => new { e.PlanLigneId, e.PeriodiciteId, e.RefMoyenDetectionId }, "UQ__Plan_Ver__0187788745F174F5").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");

            entity.HasOne(d => d.Periodicite).WithMany(p => p.PlanVerifMachineEcheances)
                .HasForeignKey(d => d.PeriodiciteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_Veri__Perio__2116E6DF");

            entity.HasOne(d => d.PlanLigne).WithMany(p => p.PlanVerifMachineEcheances)
                .HasForeignKey(d => d.PlanLigneId)
                .HasConstraintName("FK__Plan_Veri__PlanL__2022C2A6");

            entity.HasOne(d => d.RefMoyenDetection).WithMany(p => p.PlanVerifMachineEcheances)
                .HasForeignKey(d => d.RefMoyenDetectionId)
                .HasConstraintName("FK__Plan_Veri__RefMo__220B0B18");
        });

        modelBuilder.Entity<PlanVerifMachineEntete>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Ver__3214EC071FEBF82E");

            entity.ToTable("Plan_VerifMachine_Entete");

            entity.HasIndex(e => new { e.MachineCode, e.Version }, "UQ_PlanVerif_Version")
                .IsUnique()
                .HasFilter("([Statut] IN ('BROUILLON', 'ACTIF', 'ARCHIVE'))");

            entity.HasIndex(e => e.MachineCode, "UX_PlanVerif_Actif")
                .IsUnique()
                .HasFilter("([Statut]='ACTIF')");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.AfficheConformite).HasDefaultValue(true);
            entity.Property(e => e.AfficheFamilles).HasDefaultValue(false);
            entity.Property(e => e.AfficheFuiteEtalon).HasDefaultValue(false);
            entity.Property(e => e.AfficheMoyenDetectionRisques).HasDefaultValue(true);
            entity.Property(e => e.CreeLe)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.CreePar)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.MachineCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.ModifieLe).HasColumnType("datetime");
            entity.Property(e => e.ModifiePar)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Nom)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Statut)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("BROUILLON");
            entity.Property(e => e.Version).HasDefaultValue(1);

            entity.HasOne(d => d.MachineCodeNavigation).WithOne(p => p.PlanVerifMachineEntete)
                .HasForeignKey<PlanVerifMachineEntete>(d => d.MachineCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_Veri__Machi__084B3915");
        });

        modelBuilder.Entity<PlanVerifMachineFamille>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Ver__3214EC078519CA20");

            entity.ToTable("Plan_VerifMachine_Famille");

            entity.HasIndex(e => new { e.PlanEnteteId, e.RefFamilleCorpsId }, "UQ__Plan_Ver__7457AEA4F052F45E").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");

            entity.HasOne(d => d.PlanEntete).WithMany(p => p.PlanVerifMachineFamilles)
                .HasForeignKey(d => d.PlanEnteteId)
                .HasConstraintName("FK__Plan_Veri__PlanE__14B10FFA");

            entity.HasOne(d => d.RefFamilleCorps).WithMany(p => p.PlanVerifMachineFamilles)
                .HasForeignKey(d => d.RefFamilleCorpsId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_Veri__RefFa__15A53433");
        });

        modelBuilder.Entity<PlanVerifMachineLigne>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Ver__3214EC073FE1D895");

            entity.ToTable("Plan_VerifMachine_Ligne");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.LibelleMethode)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.LibelleRisque)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.TypeLigne)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("RISQUE");

            entity.HasOne(d => d.PlanEntete).WithMany(p => p.PlanVerifMachineLignes)
                .HasForeignKey(d => d.PlanEnteteId)
                .HasConstraintName("FK__Plan_Veri__PlanE__1975C517");
        });

        modelBuilder.Entity<PlanVerifMachineMatricePiece>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Ver__3214EC071A3667C8");

            entity.ToTable("Plan_VerifMachine_MatricePiece");

            entity.HasIndex(e => new { e.EcheanceId, e.FamilleId, e.RoleVerif }, "UQ__Plan_Ver__779871C4FAEC9DBE").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.RoleVerif)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.Echeance).WithMany(p => p.PlanVerifMachineMatricePieces)
                .HasForeignKey(d => d.EcheanceId)
                .HasConstraintName("FK__Plan_Veri__Echea__27C3E46E");

            entity.HasOne(d => d.Famille).WithMany(p => p.PlanVerifMachineMatricePieces)
                .HasForeignKey(d => d.FamilleId)
                .HasConstraintName("FK__Plan_Veri__Famil__28B808A7");

            entity.HasOne(d => d.PieceRef).WithMany(p => p.PlanVerifMachineMatricePieces)
                .HasForeignKey(d => d.PieceRefId)
                .HasConstraintName("FK__Plan_Veri__Piece__2AA05119");
        });

        modelBuilder.Entity<PosteTravail>(entity =>
        {
            entity.HasKey(e => e.CodePoste).HasName("PK__PosteTra__4045446B26AEBA29");

            entity.ToTable("PosteTravail", tb => tb.HasTrigger("trg_no_del_PosteTravail"));

            entity.Property(e => e.CodePoste)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.Libelle)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasMany(d => d.CodeMachines).WithMany(p => p.CodePostes)
                .UsingEntity<Dictionary<string, object>>(
                    "PosteTravailMachine",
                    r => r.HasOne<Machine>().WithMany()
                        .HasForeignKey("CodeMachine")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__PosteTrav__CodeM__0F624AF8"),
                    l => l.HasOne<PosteTravail>().WithMany()
                        .HasForeignKey("CodePoste")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__PosteTrav__CodeP__0E6E26BF"),
                    j =>
                    {
                        j.HasKey("CodePoste", "CodeMachine").HasName("PK__PosteTra__A548230B2F58912E");
                        j.ToTable("PosteTravail_Machine");
                        j.IndexerProperty<string>("CodePoste")
                            .HasMaxLength(30)
                            .IsUnicode(false);
                        j.IndexerProperty<string>("CodeMachine")
                            .HasMaxLength(30)
                            .IsUnicode(false);
                    });
        });

        modelBuilder.Entity<RefFamilleCorp>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Ref_Fami__3214EC07BFD73D0A");

            entity.ToTable("Ref_FamilleCorps", tb => tb.HasTrigger("trg_no_del_RefFam"));

            entity.HasIndex(e => e.Code, "UQ__Ref_Fami__A25C5AA7CEB0892D").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Designation)
                .HasMaxLength(150)
                .IsUnicode(false);
        });

        modelBuilder.Entity<RefFormulaire>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Ref_Form__3214EC07DD976F46");

            entity.ToTable("Ref_Formulaire", tb => tb.HasTrigger("trg_no_del_RefForm"));

            entity.HasIndex(e => e.CodeReference, "UQ__Ref_Form__0F6710587EF06DB1").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.CodeReference)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.CreeLe)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Designation)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.MachineCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.OperationCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.PosteCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Version).HasDefaultValue(1);

            entity.HasOne(d => d.MachineCodeNavigation).WithMany(p => p.RefFormulaires)
                .HasForeignKey(d => d.MachineCode)
                .HasConstraintName("FK__Ref_Formu__Machi__160F4887");

            entity.HasOne(d => d.OperationCodeNavigation).WithMany(p => p.RefFormulaires)
                .HasForeignKey(d => d.OperationCode)
                .HasConstraintName("FK__Ref_Formu__Opera__14270015");

            entity.HasOne(d => d.PosteCodeNavigation).WithMany(p => p.RefFormulaires)
                .HasForeignKey(d => d.PosteCode)
                .HasConstraintName("FK__Ref_Formu__Poste__151B244E");
        });

        modelBuilder.Entity<RefMoyenDetection>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Ref_Moye__3214EC07ED1A7EA7");

            entity.ToTable("Ref_MoyenDetection", tb => tb.HasTrigger("trg_no_del_RefMoy"));

            entity.HasIndex(e => e.Code, "UQ__Ref_Moye__A25C5AA7EBE16C30").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Designation)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RefreshT__3214EC07A22D7411");

            entity.HasIndex(e => e.Token, "UQ__RefreshT__1EB4F817D338F195").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.DateCreation)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DateExpiration).HasColumnType("datetime");
            entity.Property(e => e.EstRevoque).HasDefaultValue(false);
            entity.Property(e => e.JwtId)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Token)
                .HasMaxLength(255)
                .IsUnicode(false);

            entity.HasOne(d => d.Utilisateur).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.UtilisateurId)
                .HasConstraintName("FK__RefreshTo__Utili__403A8C7D");
        });

        modelBuilder.Entity<RisqueDefaut>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RisqueDe__3214EC078B7288D4");

            entity.ToTable("RisqueDefaut", tb => tb.HasTrigger("trg_no_del_Risque"));

            entity.HasIndex(e => e.CodeDefaut, "UQ__RisqueDe__2EF873432CBE7AEA").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.CodeDefaut)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.LibelleDefaut)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Sdelivery>(entity =>
        {
            entity.HasKey(e => e.NumeroBl).HasName("PK__SDELIVER__C664DCCD99C3BC4C");

            entity.ToTable("SDELIVERY");

            entity.Property(e => e.NumeroBl)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("NumeroBL");
            entity.Property(e => e.CodeClient)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.StatutBl)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("StatutBL");
        });

        modelBuilder.Entity<TypeCaracteristique>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TypeCara__3214EC07C0C00FE8");

            entity.ToTable("TypeCaracteristique", tb => tb.HasTrigger("trg_no_del_TypeCar"));

            entity.HasIndex(e => e.Code, "UQ__TypeCara__A25C5AA7334200A8").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.Code)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.EstNumerique).HasDefaultValue(true);
            entity.Property(e => e.Libelle)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.UniteDefaut)
                .HasMaxLength(10)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TypeControle>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TypeCont__3214EC07CC7DE7D1");

            entity.ToTable("TypeControle", tb => tb.HasTrigger("trg_no_del_TypeCtrl"));

            entity.HasIndex(e => e.Code, "UQ__TypeCont__A25C5AA74347EE8A").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.Code)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Libelle)
                .HasMaxLength(80)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TypeRobinet>(entity =>
        {
            entity.HasKey(e => e.Code).HasName("PK__TypeRobi__A25C5AA6DBC2A5FB");

            entity.ToTable("TypeRobinet", tb => tb.HasTrigger("trg_no_del_TypeRobinet"));

            entity.Property(e => e.Code)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.Libelle)
                .HasMaxLength(60)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TypeSection>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TypeSect__3214EC079B650906");

            entity.ToTable("TypeSection", tb => tb.HasTrigger("trg_no_del_TypeSec"));

            entity.HasIndex(e => e.Code, "UQ__TypeSect__A25C5AA7364EE675").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.Code)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Libelle)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<UtilisateursApp>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Utilisat__3214EC07CB7C724E");

            entity.ToTable("UtilisateursApp");

            entity.HasIndex(e => e.Matricule, "IX_UtilisateursApp_Matricule");

            entity.HasIndex(e => e.Matricule, "UQ__Utilisat__0FB9FB43967469A0").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__Utilisat__A9D105346C7E4184").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CodeRecuperation)
                .HasMaxLength(6)
                .IsUnicode(false);
            entity.Property(e => e.DateCreation)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DateDerniereConnexion).HasColumnType("datetime");
            entity.Property(e => e.DateExpirationCode).HasColumnType("datetime");
            entity.Property(e => e.Email)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.EstActif).HasDefaultValue(true);
            entity.Property(e => e.IntituleMetier)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Matricule)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.MotDePasseHash)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.NomComplet)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.RoleApp)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
