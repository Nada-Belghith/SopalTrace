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

    public virtual DbSet<GroupeInstrument> GroupeInstruments { get; set; }

    public virtual DbSet<GroupeInstrumentDetail> GroupeInstrumentDetails { get; set; }

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

    public virtual DbSet<Nqa> Nqas { get; set; }

    public virtual DbSet<Operation> Operations { get; set; }

    public virtual DbSet<Periodicite> Periodicites { get; set; }

    public virtual DbSet<PieceReference> PieceReferences { get; set; }

    public virtual DbSet<PlanAssEntete> PlanAssEntetes { get; set; }

    public virtual DbSet<PlanAssLigne> PlanAssLignes { get; set; }

    public virtual DbSet<PlanAssSection> PlanAssSections { get; set; }

    public virtual DbSet<PlanEchantillonnageEntete> PlanEchantillonnageEntetes { get; set; }

    public virtual DbSet<PlanFabEntete> PlanFabEntetes { get; set; }

    public virtual DbSet<PlanFabLigne> PlanFabLignes { get; set; }

    public virtual DbSet<PlanFabSection> PlanFabSections { get; set; }

    public virtual DbSet<PlanNcColonne> PlanNcColonnes { get; set; }

    public virtual DbSet<PlanNcEntete> PlanNcEntetes { get; set; }

    public virtual DbSet<PlanVerifMachineEcheance> PlanVerifMachineEcheances { get; set; }

    public virtual DbSet<PlanVerifMachineEntete> PlanVerifMachineEntetes { get; set; }

    public virtual DbSet<PlanVerifMachineLigne> PlanVerifMachineLignes { get; set; }

    public virtual DbSet<PlanVerifMachinePieceRef> PlanVerifMachinePieceRefs { get; set; }

    public virtual DbSet<PosteTravail> PosteTravails { get; set; }

    public virtual DbSet<RefFormulaire> RefFormulaires { get; set; }

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
            entity.HasKey(e => new { e.Codfic0, e.Zone0, e.Ident10, e.Langue0 }).HasName("PK__ATEXTRA__4F21B2DBFBC6DC3B");

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
            entity.HasKey(e => e.Usr0).HasName("PK__AUTILIS__0812AE695E03AED2");

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
            entity.HasKey(e => e.Id).HasName("PK__Defauthe__3214EC074C37B169");

            entity.ToTable("Defautheque", tb => tb.HasTrigger("trg_no_del_Defaut"));

            entity.HasIndex(e => e.Code, "UQ__Defauthe__A25C5AA73100B855").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.Code)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Description)
                .HasMaxLength(200)
                .IsUnicode(false);
        });

        modelBuilder.Entity<GroupeInstrument>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GroupeIn__3214EC07FC360550");

            entity.ToTable("GroupeInstrument", tb => tb.HasTrigger("trg_no_del_GrpInstr"));

            entity.HasIndex(e => e.CodeAlias, "UQ__GroupeIn__C98487A1ADFF7DBA").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.CodeAlias)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Libelle)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<GroupeInstrumentDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GroupeIn__3214EC07592303F5");

            entity.ToTable("GroupeInstrumentDetail");

            entity.HasIndex(e => new { e.GroupeId, e.CodeInstrument }, "UQ__GroupeIn__12EF58016E0B94DF").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CodeInstrument)
                .HasMaxLength(40)
                .IsUnicode(false);

            entity.HasOne(d => d.CodeInstrumentNavigation).WithMany(p => p.GroupeInstrumentDetails)
                .HasForeignKey(d => d.CodeInstrument)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__GroupeIns__CodeI__3A4CA8FD");

            entity.HasOne(d => d.Groupe).WithMany(p => p.GroupeInstrumentDetails)
                .HasForeignKey(d => d.GroupeId)
                .HasConstraintName("FK__GroupeIns__Group__395884C4");
        });

        modelBuilder.Entity<Instrument>(entity =>
        {
            entity.HasKey(e => e.CodeInstrument).HasName("PK__Instrume__E6E43505DF12FB87");

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
            entity.HasKey(e => e.CodeArticle).HasName("PK__ITMMASTE__32384FB04AE89A8D");

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
            entity.Property(e => e.Statut)
                .HasMaxLength(10)
                .IsUnicode(false);
        });

        modelBuilder.Entity<JournalConnexion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__JournalC__3214EC07F08B2C20");

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
            entity.HasKey(e => e.CodeMachine).HasName("PK__Machine__50D6760F52FFB57A");

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
                .HasConstraintName("FK__Machine__Operati__01142BA1");

            entity.HasOne(d => d.TypeRobinetCodeNavigation).WithMany(p => p.Machines)
                .HasForeignKey(d => d.TypeRobinetCode)
                .HasConstraintName("FK__Machine__TypeRob__00200768");
        });

        modelBuilder.Entity<MagExpeditionBl>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Mag_Expe__3214EC0780849B61");

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
            entity.HasKey(e => e.Id).HasName("PK__Mag_Expe__3214EC079387651C");

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
            entity.HasKey(e => e.Id).HasName("PK__Mag_Prep__3214EC07127BABA8");

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
            entity.HasKey(e => e.Id).HasName("PK__Mag_Prep__3214EC07D43A7E48");

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
            entity.HasKey(e => e.NumeroOf).HasName("PK__MFGHEAD__C6A65F303EA683E6");

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
            entity.HasKey(e => e.Id).HasName("PK__MFGMAT__3214EC0744C67F24");

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
            entity.HasKey(e => e.Id).HasName("PK__Modele_F__3214EC07EC3BCFC9");

            entity.ToTable("Modele_Fab_Entete", tb => tb.HasTrigger("trg_no_del_ModeleFab"));

            entity.HasIndex(e => new { e.TypeRobinetCode, e.NatureComposantCode, e.OperationCode, e.Version }, "UQ__Modele_F__A9F33C4F9EC9EC61").IsUnique();

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
                .HasConstraintName("FK__Modele_Fa__Formu__6DCC4D03");

            entity.HasOne(d => d.NatureComposantCodeNavigation).WithMany(p => p.ModeleFabEntetes)
                .HasForeignKey(d => d.NatureComposantCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Modele_Fa__Natur__6BE40491");

            entity.HasOne(d => d.OperationCodeNavigation).WithMany(p => p.ModeleFabEntetes)
                .HasForeignKey(d => d.OperationCode)
                .HasConstraintName("FK__Modele_Fa__Opera__6CD828CA");

            entity.HasOne(d => d.TypeRobinetCodeNavigation).WithMany(p => p.ModeleFabEntetes)
                .HasForeignKey(d => d.TypeRobinetCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Modele_Fa__TypeR__6AEFE058");
        });

        modelBuilder.Entity<ModeleFabLigne>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Modele_F__3214EC0720D5C833");

            entity.ToTable("Modele_Fab_Ligne");

            entity.HasIndex(e => new { e.ModeleEnteteId, e.SectionId, e.OrdreAffiche }, "UQ__Modele_F__A14F41517C67E90D").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.LibelleAffiche)
                .HasMaxLength(200)
                .IsUnicode(false);

            entity.HasOne(d => d.GroupeInstrument).WithMany(p => p.ModeleFabLignes)
                .HasForeignKey(d => d.GroupeInstrumentId)
                .HasConstraintName("FK__Modele_Fa__Group__01D345B0");

            entity.HasOne(d => d.ModeleEntete).WithMany(p => p.ModeleFabLignes)
                .HasForeignKey(d => d.ModeleEnteteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Modele_Fa__Model__7C1A6C5A");

            entity.HasOne(d => d.MoyenControle).WithMany(p => p.ModeleFabLignes)
                .HasForeignKey(d => d.MoyenControleId)
                .HasConstraintName("FK__Modele_Fa__Moyen__00DF2177");

            entity.HasOne(d => d.Periodicite).WithMany(p => p.ModeleFabLignes)
                .HasForeignKey(d => d.PeriodiciteId)
                .HasConstraintName("FK__Modele_Fa__Perio__02C769E9");

            entity.HasOne(d => d.Section).WithMany(p => p.ModeleFabLignes)
                .HasForeignKey(d => d.SectionId)
                .HasConstraintName("FK__Modele_Fa__Secti__7D0E9093");

            entity.HasOne(d => d.TypeCaracteristique).WithMany(p => p.ModeleFabLignes)
                .HasForeignKey(d => d.TypeCaracteristiqueId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Modele_Fa__TypeC__7EF6D905");

            entity.HasOne(d => d.TypeControle).WithMany(p => p.ModeleFabLignes)
                .HasForeignKey(d => d.TypeControleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Modele_Fa__TypeC__7FEAFD3E");
        });

        modelBuilder.Entity<ModeleFabSection>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Modele_F__3214EC0772C3E68E");

            entity.ToTable("Modele_Fab_Section");

            entity.HasIndex(e => new { e.ModeleEnteteId, e.OrdreAffiche }, "UQ__Modele_F__60571032C527B3A1").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.FrequenceLibelle)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.LibelleSection)
                .HasMaxLength(200)
                .IsUnicode(false);

            entity.HasOne(d => d.ModeleEntete).WithMany(p => p.ModeleFabSections)
                .HasForeignKey(d => d.ModeleEnteteId)
                .HasConstraintName("FK__Modele_Fa__Model__76619304");
        });

        modelBuilder.Entity<MoyenControle>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__MoyenCon__3214EC07BD04C8A5");

            entity.ToTable("MoyenControle", tb => tb.HasTrigger("trg_no_del_Moyen"));

            entity.HasIndex(e => e.Code, "UQ__MoyenCon__A25C5AA789C985FC").IsUnique();

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
            entity.HasKey(e => e.Code).HasName("PK__NatureCo__A25C5AA6042CFE0C");

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

        modelBuilder.Entity<Nqa>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__NQA__3214EC073CD020B3");

            entity.ToTable("NQA");

            entity.HasIndex(e => e.ValeurNqa, "UQ__NQA__1DA3E248742BA62C").IsUnique();

            entity.Property(e => e.ValeurNqa).HasColumnName("ValeurNQA");
        });

        modelBuilder.Entity<Operation>(entity =>
        {
            entity.HasKey(e => e.Code).HasName("PK__Operatio__A25C5AA6899CD0E9");

            entity.ToTable("Operation", tb => tb.HasTrigger("trg_no_del_Operation"));

            entity.Property(e => e.Code)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.Libelle)
                .HasMaxLength(80)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Periodicite>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Periodic__3214EC07E685A979");

            entity.ToTable("Periodicite", tb => tb.HasTrigger("trg_no_del_Perio"));

            entity.HasIndex(e => e.Code, "UQ__Periodic__A25C5AA7A1948E31").IsUnique();

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
            entity.HasKey(e => e.Id).HasName("PK__PieceRef__3214EC0739EF5D95");

            entity.ToTable("PieceReference", tb => tb.HasTrigger("trg_no_del_PieceRef"));

            entity.HasIndex(e => e.Code, "UQ__PieceRef__A25C5AA7B161C2FD").IsUnique();

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
                .HasConstraintName("FK__PieceRefe__Machi__40058253");
        });

        modelBuilder.Entity<PlanAssEntete>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Ass__3214EC07CD9246BD");

            entity.ToTable("Plan_Ass_Entete", tb => tb.HasTrigger("trg_no_del_PlanAss"));

            entity.HasIndex(e => new { e.OperationCode, e.TypeRobinetCode, e.CodeArticleSage, e.Version }, "UQ_PlanAss_Instance")
                .IsUnique()
                .HasFilter("([EstModele]=(0))");

            entity.HasIndex(e => new { e.OperationCode, e.TypeRobinetCode, e.Version }, "UQ_PlanAss_Modele")
                .IsUnique()
                .HasFilter("([EstModele]=(1))");

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

            entity.HasOne(d => d.Formulaire).WithMany(p => p.PlanAssEntetes)
                .HasForeignKey(d => d.FormulaireId)
                .HasConstraintName("FK__Plan_Ass___Formu__2BC97F7C");

            entity.HasOne(d => d.OperationCodeNavigation).WithMany(p => p.PlanAssEntetes)
                .HasForeignKey(d => d.OperationCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_Ass___Opera__2704CA5F");

            entity.HasOne(d => d.TypeRobinetCodeNavigation).WithMany(p => p.PlanAssEntetes)
                .HasForeignKey(d => d.TypeRobinetCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_Ass___TypeR__27F8EE98");
        });

        modelBuilder.Entity<PlanAssLigne>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Ass__3214EC0769BBFA7B");

            entity.ToTable("Plan_Ass_Ligne");

            entity.HasIndex(e => new { e.PlanEnteteId, e.SectionId, e.OrdreAffiche }, "UQ__Plan_Ass__314027698557FE13").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.LibelleAffiche)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.LimiteSpecTexte)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.MachineCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Unite)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.GroupeInstrument).WithMany(p => p.PlanAssLignes)
                .HasForeignKey(d => d.GroupeInstrumentId)
                .HasConstraintName("FK__Plan_Ass___Group__41B8C09B");

            entity.HasOne(d => d.MachineCodeNavigation).WithMany(p => p.PlanAssLignes)
                .HasForeignKey(d => d.MachineCode)
                .HasConstraintName("FK__Plan_Ass___Machi__42ACE4D4");

            entity.HasOne(d => d.MoyenControle).WithMany(p => p.PlanAssLignes)
                .HasForeignKey(d => d.MoyenControleId)
                .HasConstraintName("FK__Plan_Ass___Moyen__40C49C62");

            entity.HasOne(d => d.PlanEntete).WithMany(p => p.PlanAssLignes)
                .HasForeignKey(d => d.PlanEnteteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_Ass___PlanE__3BFFE745");

            entity.HasOne(d => d.Section).WithMany(p => p.PlanAssLignes)
                .HasForeignKey(d => d.SectionId)
                .HasConstraintName("FK__Plan_Ass___Secti__3CF40B7E");

            entity.HasOne(d => d.TypeCaracteristique).WithMany(p => p.PlanAssLignes)
                .HasForeignKey(d => d.TypeCaracteristiqueId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_Ass___TypeC__3EDC53F0");

            entity.HasOne(d => d.TypeControle).WithMany(p => p.PlanAssLignes)
                .HasForeignKey(d => d.TypeControleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_Ass___TypeC__3FD07829");
        });

        modelBuilder.Entity<PlanAssSection>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Ass__3214EC07D0AD65A7");

            entity.ToTable("Plan_Ass_Section");

            entity.HasIndex(e => new { e.PlanEnteteId, e.OrdreAffiche }, "UQ__Plan_Ass__F058760A2CC3A243").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.LibelleSection)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.NormeReference)
                .HasMaxLength(40)
                .IsUnicode(false);

            entity.HasOne(d => d.Nqa).WithMany(p => p.PlanAssSections)
                .HasForeignKey(d => d.NqaId)
                .HasConstraintName("FK__Plan_Ass___NqaId__373B3228");

            entity.HasOne(d => d.Periodicite).WithMany(p => p.PlanAssSections)
                .HasForeignKey(d => d.PeriodiciteId)
                .HasConstraintName("FK__Plan_Ass___Perio__36470DEF");

            entity.HasOne(d => d.PlanEntete).WithMany(p => p.PlanAssSections)
                .HasForeignKey(d => d.PlanEnteteId)
                .HasConstraintName("FK__Plan_Ass___PlanE__336AA144");

            entity.HasOne(d => d.TypeSection).WithMany(p => p.PlanAssSections)
                .HasForeignKey(d => d.TypeSectionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_Ass___TypeS__3552E9B6");
        });

        modelBuilder.Entity<PlanEchantillonnageEntete>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Ech__3214EC07D9573CD8");

            entity.ToTable("Plan_Echantillonnage_Entete", tb => tb.HasTrigger("trg_no_del_PlanEchan"));

            entity.HasIndex(e => new { e.CodeReference, e.CodeArticleSage, e.Version }, "UQ_PlanEchan_Article")
                .IsUnique()
                .HasFilter("([CodeArticleSage] IS NOT NULL)");

            entity.HasIndex(e => new { e.CodeReference, e.Version }, "UQ_PlanEchan_Generic")
                .IsUnique()
                .HasFilter("([CodeArticleSage] IS NULL)");

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
                .HasConstraintName("FK__Plan_Echa__Formu__5E8A0973");

            entity.HasOne(d => d.MachineCodeNavigation).WithMany(p => p.PlanEchantillonnageEntetes)
                .HasForeignKey(d => d.MachineCode)
                .HasConstraintName("FK__Plan_Echa__Machi__5D95E53A");

            entity.HasOne(d => d.Nqa).WithMany(p => p.PlanEchantillonnageEntetes)
                .HasForeignKey(d => d.NqaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_Echa__NqaId__625A9A57");
        });

        modelBuilder.Entity<PlanFabEntete>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Fab__3214EC07B4E094BE");

            entity.ToTable("Plan_Fab_Entete", tb => tb.HasTrigger("trg_no_del_PlanFab"));

            entity.HasIndex(e => new { e.CodeArticleSage, e.ModeleSourceId, e.Version }, "UQ__Plan_Fab__6520898197AFC063").IsUnique();

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
            entity.Property(e => e.Statut)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("BROUILLON");
            entity.Property(e => e.Version).HasDefaultValue(1);

            entity.HasOne(d => d.Formulaire).WithMany(p => p.PlanFabEntetes)
                .HasForeignKey(d => d.FormulaireId)
                .HasConstraintName("FK__Plan_Fab___Formu__0C50D423");

            entity.HasOne(d => d.MachineDefautCodeNavigation).WithMany(p => p.PlanFabEntetes)
                .HasForeignKey(d => d.MachineDefautCode)
                .HasConstraintName("FK__Plan_Fab___Machi__0B5CAFEA");

            entity.HasOne(d => d.ModeleSource).WithMany(p => p.PlanFabEntetes)
                .HasForeignKey(d => d.ModeleSourceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_Fab___Model__0880433F");
        });

        modelBuilder.Entity<PlanFabLigne>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Fab__3214EC07175025F1");

            entity.ToTable("Plan_Fab_Ligne");

            entity.HasIndex(e => new { e.PlanEnteteId, e.SectionId, e.OrdreAffiche }, "UQ__Plan_Fab__31402769E856D9B1").IsUnique();

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
            entity.Property(e => e.Unite)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.GroupeInstrument).WithMany(p => p.PlanFabLignes)
                .HasForeignKey(d => d.GroupeInstrumentId)
                .HasConstraintName("FK__Plan_Fab___Group__2057CCD0");

            entity.HasOne(d => d.InstrumentCodeNavigation).WithMany(p => p.PlanFabLignes)
                .HasForeignKey(d => d.InstrumentCode)
                .HasConstraintName("FK__Plan_Fab___Instr__214BF109");

            entity.HasOne(d => d.ModeleLigneSource).WithMany(p => p.PlanFabLignes)
                .HasForeignKey(d => d.ModeleLigneSourceId)
                .HasConstraintName("FK__Plan_Fab___Model__1B9317B3");

            entity.HasOne(d => d.MoyenControle).WithMany(p => p.PlanFabLignes)
                .HasForeignKey(d => d.MoyenControleId)
                .HasConstraintName("FK__Plan_Fab___Moyen__1F63A897");

            entity.HasOne(d => d.Periodicite).WithMany(p => p.PlanFabLignes)
                .HasForeignKey(d => d.PeriodiciteId)
                .HasConstraintName("FK__Plan_Fab___Perio__22401542");

            entity.HasOne(d => d.PlanEntete).WithMany(p => p.PlanFabLignes)
                .HasForeignKey(d => d.PlanEnteteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_Fab___PlanE__19AACF41");

            entity.HasOne(d => d.Section).WithMany(p => p.PlanFabLignes)
                .HasForeignKey(d => d.SectionId)
                .HasConstraintName("FK__Plan_Fab___Secti__1A9EF37A");

            entity.HasOne(d => d.TypeCaracteristique).WithMany(p => p.PlanFabLignes)
                .HasForeignKey(d => d.TypeCaracteristiqueId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_Fab___TypeC__1D7B6025");

            entity.HasOne(d => d.TypeControle).WithMany(p => p.PlanFabLignes)
                .HasForeignKey(d => d.TypeControleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_Fab___TypeC__1E6F845E");
        });

        modelBuilder.Entity<PlanFabSection>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Fab__3214EC07733AFC0C");

            entity.ToTable("Plan_Fab_Section");

            entity.HasIndex(e => new { e.PlanEnteteId, e.OrdreAffiche }, "UQ__Plan_Fab__F058760A07C64FA9").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.FrequenceLibelle)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.LibelleSection)
                .HasMaxLength(200)
                .IsUnicode(false);

            entity.HasOne(d => d.ModeleSection).WithMany(p => p.PlanFabSections)
                .HasForeignKey(d => d.ModeleSectionId)
                .HasConstraintName("FK__Plan_Fab___Model__13F1F5EB");

            entity.HasOne(d => d.PlanEntete).WithMany(p => p.PlanFabSections)
                .HasForeignKey(d => d.PlanEnteteId)
                .HasConstraintName("FK__Plan_Fab___PlanE__12FDD1B2");
        });

        modelBuilder.Entity<PlanNcColonne>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_NC___3214EC07E5E2F54C");

            entity.ToTable("Plan_NC_Colonne");

            entity.HasIndex(e => new { e.PlanNcenteteId, e.OrdreAffiche }, "UQ__Plan_NC___90C61738830F6490").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.LibelleDefaut)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.MachineCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.PlanNcenteteId).HasColumnName("PlanNCEnteteId");

            entity.HasOne(d => d.MachineCodeNavigation).WithMany(p => p.PlanNcColonnes)
                .HasForeignKey(d => d.MachineCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_NC_C__Machi__54CB950F");

            entity.HasOne(d => d.PlanNcentete).WithMany(p => p.PlanNcColonnes)
                .HasForeignKey(d => d.PlanNcenteteId)
                .HasConstraintName("FK__Plan_NC_C__PlanN__52E34C9D");
        });

        modelBuilder.Entity<PlanNcEntete>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_NC___3214EC075B7784BC");

            entity.ToTable("Plan_NC_Entete", tb => tb.HasTrigger("trg_no_del_PlanNC"));

            entity.HasIndex(e => new { e.TypeRobinetCode, e.OperationCode, e.PosteCode, e.Version }, "UQ_PlanNC_Version").IsUnique();

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
            entity.Property(e => e.OperationCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.PosteCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Statut)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("BROUILLON");
            entity.Property(e => e.TypeRobinetCode)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Version).HasDefaultValue(1);

            entity.HasOne(d => d.Formulaire).WithMany(p => p.PlanNcEntetes)
                .HasForeignKey(d => d.FormulaireId)
                .HasConstraintName("FK__Plan_NC_E__Formu__4A4E069C");

            entity.HasOne(d => d.OperationCodeNavigation).WithMany(p => p.PlanNcEntetes)
                .HasForeignKey(d => d.OperationCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_NC_E__Opera__4865BE2A");

            entity.HasOne(d => d.PosteCodeNavigation).WithMany(p => p.PlanNcEntetes)
                .HasForeignKey(d => d.PosteCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_NC_E__Poste__4959E263");

            entity.HasOne(d => d.TypeRobinetCodeNavigation).WithMany(p => p.PlanNcEntetes)
                .HasForeignKey(d => d.TypeRobinetCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_NC_E__TypeR__477199F1");
        });

        modelBuilder.Entity<PlanVerifMachineEcheance>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Ver__3214EC07573C962B");

            entity.ToTable("Plan_VerifMachine_Echeance");

            entity.HasIndex(e => new { e.PlanLigneId, e.PeriodiciteId }, "UQ_PlanVerif_Echeance").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.LibelleMoyen)
                .HasMaxLength(60)
                .IsUnicode(false);

            entity.HasOne(d => d.Periodicite).WithMany(p => p.PlanVerifMachineEcheances)
                .HasForeignKey(d => d.PeriodiciteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_Veri__Perio__6F7F8B4B");

            entity.HasOne(d => d.PlanLigne).WithMany(p => p.PlanVerifMachineEcheances)
                .HasForeignKey(d => d.PlanLigneId)
                .HasConstraintName("FK__Plan_Veri__PlanL__6E8B6712");
        });

        modelBuilder.Entity<PlanVerifMachineEntete>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Ver__3214EC07F56A5E08");

            entity.ToTable("Plan_VerifMachine_Entete", tb => tb.HasTrigger("trg_no_del_PlanVerif"));

            entity.HasIndex(e => new { e.MachineCode, e.TypeRapport, e.TypeRobinetCode, e.Version }, "UQ_PlanVerif_AvecType")
                .IsUnique()
                .HasFilter("([TypeRobinetCode] IS NOT NULL)");

            entity.HasIndex(e => new { e.MachineCode, e.TypeRapport, e.Version }, "UQ_PlanVerif_SansType")
                .IsUnique()
                .HasFilter("([TypeRobinetCode] IS NULL)");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
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
            entity.Property(e => e.TypeRapport)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.TypeRobinetCode)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Version).HasDefaultValue(1);

            entity.HasOne(d => d.Formulaire).WithMany(p => p.PlanVerifMachineEntetes)
                .HasForeignKey(d => d.FormulaireId)
                .HasConstraintName("FK__Plan_Veri__Formu__5B78929E");

            entity.HasOne(d => d.MachineCodeNavigation).WithMany(p => p.PlanVerifMachineEntetes)
                .HasForeignKey(d => d.MachineCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_Veri__Machi__589C25F3");

            entity.HasOne(d => d.TypeRobinetCodeNavigation).WithMany(p => p.PlanVerifMachineEntetes)
                .HasForeignKey(d => d.TypeRobinetCode)
                .HasConstraintName("FK__Plan_Veri__TypeR__59904A2C");
        });

        modelBuilder.Entity<PlanVerifMachineLigne>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Ver__3214EC0702E708E7");

            entity.ToTable("Plan_VerifMachine_Ligne");

            entity.HasIndex(e => new { e.PlanEnteteId, e.OrdreAffiche }, "UQ_PlanVerif_Ligne").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.LibelleMethode)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.LibelleRisque)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.TypeSaisie)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasDefaultValue("CONFORMITE");
            entity.Property(e => e.Unite)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.PlanEntete).WithMany(p => p.PlanVerifMachineLignes)
                .HasForeignKey(d => d.PlanEnteteId)
                .HasConstraintName("FK__Plan_Veri__PlanE__6501FCD8");

            entity.HasOne(d => d.RisqueDefaut).WithMany(p => p.PlanVerifMachineLignes)
                .HasForeignKey(d => d.RisqueDefautId)
                .HasConstraintName("FK__Plan_Veri__Risqu__66EA454A");
        });

        modelBuilder.Entity<PlanVerifMachinePieceRef>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Ver__3214EC075D2D5D2F");

            entity.ToTable("Plan_VerifMachine_PieceRef");

            entity.HasIndex(e => new { e.EcheanceId, e.PieceRefId, e.RoleVerif }, "UQ_PlanVerif_PieceRef").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.FamilleDesc)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.ResultatAttendu)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("C");
            entity.Property(e => e.RoleVerif)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.Echeance).WithMany(p => p.PlanVerifMachinePieceRefs)
                .HasForeignKey(d => d.EcheanceId)
                .HasConstraintName("FK__Plan_Veri__Echea__753864A1");

            entity.HasOne(d => d.PieceRef).WithMany(p => p.PlanVerifMachinePieceRefs)
                .HasForeignKey(d => d.PieceRefId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_Veri__Piece__762C88DA");
        });

        modelBuilder.Entity<PosteTravail>(entity =>
        {
            entity.HasKey(e => e.CodePoste).HasName("PK__PosteTra__4045446BDC9E6905");

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
                        .HasConstraintName("FK__PosteTrav__CodeM__07C12930"),
                    l => l.HasOne<PosteTravail>().WithMany()
                        .HasForeignKey("CodePoste")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__PosteTrav__CodeP__06CD04F7"),
                    j =>
                    {
                        j.HasKey("CodePoste", "CodeMachine").HasName("PK__PosteTra__A548230B165DA08E");
                        j.ToTable("PosteTravail_Machine");
                        j.IndexerProperty<string>("CodePoste")
                            .HasMaxLength(30)
                            .IsUnicode(false);
                        j.IndexerProperty<string>("CodeMachine")
                            .HasMaxLength(30)
                            .IsUnicode(false);
                    });
        });

        modelBuilder.Entity<RefFormulaire>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Ref_Form__3214EC076FC561EB");

            entity.ToTable("Ref_Formulaire", tb => tb.HasTrigger("trg_no_del_RefForm"));

            entity.HasIndex(e => e.CodeReference, "UQ__Ref_Form__0F671058C5412620").IsUnique();

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
                .HasConstraintName("FK__Ref_Formu__Machi__0E6E26BF");

            entity.HasOne(d => d.OperationCodeNavigation).WithMany(p => p.RefFormulaires)
                .HasForeignKey(d => d.OperationCode)
                .HasConstraintName("FK__Ref_Formu__Opera__0C85DE4D");

            entity.HasOne(d => d.PosteCodeNavigation).WithMany(p => p.RefFormulaires)
                .HasForeignKey(d => d.PosteCode)
                .HasConstraintName("FK__Ref_Formu__Poste__0D7A0286");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RefreshT__3214EC0735ECF361");

            entity.HasIndex(e => e.Token, "UQ__RefreshT__1EB4F8173F8FA05B").IsUnique();

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
            entity.HasKey(e => e.Id).HasName("PK__RisqueDe__3214EC0720F94FDF");

            entity.ToTable("RisqueDefaut", tb => tb.HasTrigger("trg_no_del_Risque"));

            entity.HasIndex(e => e.CodeDefaut, "UQ__RisqueDe__2EF87343CACEE7D9").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.CodeDefaut)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.LibelleDefaut)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.TypeControle).WithMany(p => p.RisqueDefauts)
                .HasForeignKey(d => d.TypeControleId)
                .HasConstraintName("FK__RisqueDef__TypeC__45BE5BA9");
        });

        modelBuilder.Entity<Sdelivery>(entity =>
        {
            entity.HasKey(e => e.NumeroBl).HasName("PK__SDELIVER__C664DCCD9061197A");

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
            entity.HasKey(e => e.Id).HasName("PK__TypeCara__3214EC07751F1DE3");

            entity.ToTable("TypeCaracteristique", tb => tb.HasTrigger("trg_no_del_TypeCar"));

            entity.HasIndex(e => e.Code, "UQ__TypeCara__A25C5AA7465A6D96").IsUnique();

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
            entity.HasKey(e => e.Id).HasName("PK__TypeCont__3214EC071F0CE975");

            entity.ToTable("TypeControle", tb => tb.HasTrigger("trg_no_del_TypeCtrl"));

            entity.HasIndex(e => e.Code, "UQ__TypeCont__A25C5AA770AF5E13").IsUnique();

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
            entity.HasKey(e => e.Code).HasName("PK__TypeRobi__A25C5AA63EAB502C");

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
            entity.HasKey(e => e.Id).HasName("PK__TypeSect__3214EC0751EDE518");

            entity.ToTable("TypeSection", tb => tb.HasTrigger("trg_no_del_TypeSec"));

            entity.HasIndex(e => e.Code, "UQ__TypeSect__A25C5AA75308FC40").IsUnique();

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
            entity.HasKey(e => e.Id).HasName("PK__Utilisat__3214EC07BD99013E");

            entity.ToTable("UtilisateursApp");

            entity.HasIndex(e => e.Matricule, "IX_UtilisateursApp_Matricule");

            entity.HasIndex(e => e.Matricule, "UQ__Utilisat__0FB9FB43EE9803B6").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__Utilisat__A9D10534BC28FCD4").IsUnique();

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
