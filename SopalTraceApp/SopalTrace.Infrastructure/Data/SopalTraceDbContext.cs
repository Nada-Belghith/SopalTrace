using SopalTrace.Domain.Constants;
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

    public virtual DbSet<Article> Articles { get; set; }

    public virtual DbSet<Atextra> Atextras { get; set; }

    public virtual DbSet<Autili> Autilis { get; set; }

    public virtual DbSet<BomdNomenclature> BomdNomenclatures { get; set; }

    public virtual DbSet<Defautheque> Defautheques { get; set; }

    public virtual DbSet<ExecControleOf> ExecControleOfs { get; set; }

    public virtual DbSet<ExecControleTranche> ExecControleTranches { get; set; }

    public virtual DbSet<ExecPieceType> ExecPieceTypes { get; set; }

    public virtual DbSet<ExecPrelevement> ExecPrelevements { get; set; }

    public virtual DbSet<ExecPrelevementLigne> ExecPrelevementLignes { get; set; }

    public virtual DbSet<FamilleProduitFini> FamilleProduitFinis { get; set; }

    public virtual DbSet<Instrument> Instruments { get; set; }

    public virtual DbSet<JournalConnexion> JournalConnexions { get; set; }

    public virtual DbSet<Machine> Machines { get; set; }

    public virtual DbSet<MagExpeditionBl> MagExpeditionBls { get; set; }

    public virtual DbSet<MagExpeditionBlScanOf> MagExpeditionBlScanOfs { get; set; }

    public virtual DbSet<MagPreparationOf> MagPreparationOfs { get; set; }

    public virtual DbSet<MagPreparationOfLot> MagPreparationOfLots { get; set; }

    public virtual DbSet<MagQuickControlRapport> MagQuickControlRapports { get; set; }

    public virtual DbSet<MfgheadOrdreFabrication> MfgheadOrdreFabrications { get; set; }

    public virtual DbSet<MfgmatBesoinOf> MfgmatBesoinOfs { get; set; }

    public virtual DbSet<ModeleFabricationEntete> ModeleFabricationEntetes { get; set; }

    public virtual DbSet<ModeleFabricationLigne> ModeleFabricationLignes { get; set; }

    public virtual DbSet<ModeleFabricationSection> ModeleFabricationSections { get; set; }

    public virtual DbSet<MoyenControle> MoyenControles { get; set; }

    public virtual DbSet<NatureArticle> NatureArticles { get; set; }

    public virtual DbSet<NatureArticleOperation> NatureArticleOperations { get; set; }

    public virtual DbSet<Nqa> Nqas { get; set; }

    public virtual DbSet<Operation> Operations { get; set; }

    public virtual DbSet<OutilControle> OutilControles { get; set; }

    public virtual DbSet<Periodicite> Periodicites { get; set; }

    public virtual DbSet<PieceReference> PieceReferences { get; set; }

    public virtual DbSet<PlanAssemblageEntete> PlanAssemblageEntetes { get; set; }

    public virtual DbSet<PlanAssemblageLigne> PlanAssemblageLignes { get; set; }

    public virtual DbSet<PlanAssemblageSection> PlanAssemblageSections { get; set; }

    public virtual DbSet<PlanControlePosteEntete> PlanControlePosteEntetes { get; set; }

    public virtual DbSet<PlanControlePosteLigne> PlanControlePosteLignes { get; set; }

    public virtual DbSet<PlanEchantillonnageEntete> PlanEchantillonnageEntetes { get; set; }

    public virtual DbSet<PlanEchantillonnageRegle> PlanEchantillonnageRegles { get; set; }

    public virtual DbSet<PlanFabricationEntete> PlanFabricationEntetes { get; set; }

    public virtual DbSet<PlanFabricationLigne> PlanFabricationLignes { get; set; }

    public virtual DbSet<PlanFabricationSection> PlanFabricationSections { get; set; }

    public virtual DbSet<PlanProduitFiniEntete> PlanProduitFiniEntetes { get; set; }

    public virtual DbSet<PlanProduitFiniLigne> PlanProduitFiniLignes { get; set; }

    public virtual DbSet<PlanProduitFiniSection> PlanProduitFiniSections { get; set; }

    public virtual DbSet<PlanResultatControleCfEntete> PlanResultatControleCfEntetes { get; set; }

    public virtual DbSet<PlanResultatControleCfLigne> PlanResultatControleCfLignes { get; set; }

    public virtual DbSet<PlanResultatControleCfSection> PlanResultatControleCfSections { get; set; }

    public virtual DbSet<PlanVerifMachineEcheance> PlanVerifMachineEcheances { get; set; }

    public virtual DbSet<PlanVerifMachineEntete> PlanVerifMachineEntetes { get; set; }

    public virtual DbSet<PlanVerifMachineFamille> PlanVerifMachineFamilles { get; set; }

    public virtual DbSet<PlanVerifMachineLigne> PlanVerifMachineLignes { get; set; }

    public virtual DbSet<PlanVerifMachineMatricePiece> PlanVerifMachineMatricePieces { get; set; }

    public virtual DbSet<PosteTravail> PosteTravails { get; set; }

    public virtual DbSet<ProduitFini> ProduitFinis { get; set; }

    public virtual DbSet<RefFamilleCorp> RefFamilleCorps { get; set; }

    public virtual DbSet<RefFormulaire> RefFormulaires { get; set; }

    public virtual DbSet<RefMoyenDetection> RefMoyenDetections { get; set; }

    public virtual DbSet<RefRegleEchantillonnage> RefRegleEchantillonnages { get; set; }

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
        modelBuilder.Entity<Article>(entity =>
        {
            entity.HasKey(e => e.CodeArticle).HasName("PK__Article__32384FB07B676A9E");

            entity.ToTable("Article");

            entity.HasIndex(e => e.NatureArticleCode, "IX_Article_NatureArticle");

            entity.HasIndex(e => e.TypeArticle, "IX_Article_TypeArticle");

            entity.Property(e => e.CodeArticle)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.DateCreation)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DateModification).HasColumnType("datetime");
            entity.Property(e => e.Designation)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Designation2)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.NatureArticleCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Statut)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue(StatutsPlan.Actif);
            entity.Property(e => e.TypeArticle)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.NatureArticleCodeNavigation).WithMany(p => p.Articles)
                .HasForeignKey(d => d.NatureArticleCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Article__NatureA__1DB06A4F");
        });

        modelBuilder.Entity<Atextra>(entity =>
        {
            entity.HasKey(e => new { e.Codfic0, e.Zone0, e.Ident10, e.Langue0 }).HasName("PK__ATEXTRA__4F21B2DB9772468B");

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
            entity.HasKey(e => e.Usr0).HasName("PK__AUTILIS__0812AE69A50C7A5B");

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

        modelBuilder.Entity<BomdNomenclature>(entity =>
        {
            entity.HasKey(e => new { e.ArticleParent, e.CodeComposant, e.CodeAlternative }).HasName("PK__BOMD_Nom__2710E8513599720E");

            entity.ToTable("BOMD_Nomenclature");

            entity.HasIndex(e => e.CodeComposant, "IX_BOMD_Composant");

            entity.HasIndex(e => e.ArticleParent, "IX_BOMD_Parent");

            entity.Property(e => e.ArticleParent)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.CodeComposant)
                .HasMaxLength(30)
                .IsUnicode(false);

            entity.HasOne(d => d.ArticleParentNavigation).WithMany(p => p.BomdNomenclatureArticleParentNavigations)
                .HasForeignKey(d => d.ArticleParent)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__BOMD_Nome__Artic__29221CFB");

            entity.HasOne(d => d.CodeComposantNavigation).WithMany(p => p.BomdNomenclatureCodeComposantNavigations)
                .HasForeignKey(d => d.CodeComposant)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__BOMD_Nome__CodeC__2A164134");
        });

        modelBuilder.Entity<Defautheque>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Defauthe__3214EC0741A384BC");

            entity.ToTable("Defautheque", tb => tb.HasTrigger("trg_no_del_Defautheque"));

            entity.HasIndex(e => e.Code, "UQ__Defauthe__A25C5AA79DFD8F78").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.Code)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Description)
                .HasMaxLength(200)
                .IsUnicode(false);
        });

        modelBuilder.Entity<ExecControleOf>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Exec_Con__3214EC07AA16A71E");

            entity.ToTable("Exec_ControleOF");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.DateDebut)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DateFin).HasColumnType("datetime");
            entity.Property(e => e.MachineCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.MachineCodePrevu)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.NumEquipe).HasDefaultValue(1);
            entity.Property(e => e.NumeroOf)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("NumeroOF");
            entity.Property(e => e.OperationCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.PosteCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.PosteCodePrevu)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Statut)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("EN_COURS");
            entity.Property(e => e.TypePlan)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.MachineCodeNavigation).WithMany(p => p.ExecControleOfMachineCodeNavigations)
                .HasForeignKey(d => d.MachineCode)
                .HasConstraintName("FK__Exec_Cont__Machi__3F9B6DFF");

            entity.HasOne(d => d.MachineCodePrevuNavigation).WithMany(p => p.ExecControleOfMachineCodePrevuNavigations)
                .HasForeignKey(d => d.MachineCodePrevu)
                .HasConstraintName("FK__Exec_Cont__Machi__3DB3258D");

            entity.HasOne(d => d.NumeroOfNavigation).WithMany(p => p.ExecControleOfs)
                .HasForeignKey(d => d.NumeroOf)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Exec_Cont__Numer__3BCADD1B");

            entity.HasOne(d => d.OperationCodeNavigation).WithMany(p => p.ExecControleOfs)
                .HasForeignKey(d => d.OperationCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Exec_Cont__Opera__3CBF0154");

            entity.HasOne(d => d.PosteCodeNavigation).WithMany(p => p.ExecControleOfPosteCodeNavigations)
                .HasForeignKey(d => d.PosteCode)
                .HasConstraintName("FK__Exec_Cont__Poste__408F9238");

            entity.HasOne(d => d.PosteCodePrevuNavigation).WithMany(p => p.ExecControleOfPosteCodePrevuNavigations)
                .HasForeignKey(d => d.PosteCodePrevu)
                .HasConstraintName("FK__Exec_Cont__Poste__3EA749C6");
        });

        modelBuilder.Entity<ExecControleTranche>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Exec_Con__3214EC070CA47665");

            entity.ToTable("Exec_ControleTranche");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.ActionsCorrection)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.DetailsNc)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("DetailsNC");
            entity.Property(e => e.ExecControleOfid).HasColumnName("ExecControleOFId");
            entity.Property(e => e.HeureDebut).HasColumnType("datetime");
            entity.Property(e => e.HeureFin).HasColumnType("datetime");
            entity.Property(e => e.MatriculeApprobateur)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.ResultatFinal)
                .HasMaxLength(2)
                .IsUnicode(false);
            entity.Property(e => e.TrancheHoraire)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.ExecControleOf).WithMany(p => p.ExecControleTranches)
                .HasForeignKey(d => d.ExecControleOfid)
                .HasConstraintName("FK__Exec_Cont__ExecC__4EDDB18F");
        });

        modelBuilder.Entity<ExecPieceType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Exec_Pie__3214EC07DF5BC73F");

            entity.ToTable("Exec_PieceType");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.ExecControleOfid).HasColumnName("ExecControleOFId");
            entity.Property(e => e.HeureValidation)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.MatriculeOperateur)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Remarque)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Resultat)
                .HasMaxLength(2)
                .IsUnicode(false);

            entity.HasOne(d => d.ExecControleOf).WithMany(p => p.ExecPieceTypes)
                .HasForeignKey(d => d.ExecControleOfid)
                .HasConstraintName("FK__Exec_Piec__ExecC__4924D839");
        });

        modelBuilder.Entity<ExecPrelevement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Exec_Pre__3214EC07B2179567");

            entity.ToTable("Exec_Prelevement");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.HeurePrevue).HasColumnType("datetime");
            entity.Property(e => e.HeureSaisie).HasColumnType("datetime");
            entity.Property(e => e.MatriculeOperateur)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.ResultatGlobal)
                .HasMaxLength(2)
                .IsUnicode(false);

            entity.HasOne(d => d.ExecControleTranche).WithMany(p => p.ExecPrelevements)
                .HasForeignKey(d => d.ExecControleTrancheId)
                .HasConstraintName("FK__Exec_Prel__ExecC__53A266AC");
        });

        modelBuilder.Entity<ExecPrelevementLigne>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Exec_Pre__3214EC07FEAC9AE3");

            entity.ToTable("Exec_Prelevement_Ligne");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Remarque)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Resultat)
                .HasMaxLength(2)
                .IsUnicode(false);

            entity.HasOne(d => d.Prelevement).WithMany(p => p.ExecPrelevementLignes)
                .HasForeignKey(d => d.PrelevementId)
                .HasConstraintName("FK__Exec_Prel__Prele__58671BC9");
        });

        modelBuilder.Entity<FamilleProduitFini>(entity =>
        {
            entity.HasKey(e => e.Code).HasName("PK__FamilleP__A25C5AA68162DFF7");

            entity.ToTable("FamilleProduitFini", tb => tb.HasTrigger("trg_no_del_FamilleProduitFini"));

            entity.Property(e => e.Code)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.Designation)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.TypeRobinetCode)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.TypeRobinetCodeNavigation).WithMany(p => p.FamilleProduitFinis)
                .HasForeignKey(d => d.TypeRobinetCode)
                .HasConstraintName("FK__FamillePr__TypeR__3A81B327");
        });

        modelBuilder.Entity<Instrument>(entity =>
        {
            entity.HasKey(e => e.CodeInstrument).HasName("PK__Instrume__E6E43505B204E719");

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
                .HasDefaultValue(StatutsPlan.Actif);
            entity.Property(e => e.Unite)
                .HasMaxLength(10)
                .IsUnicode(false);
        });

        modelBuilder.Entity<JournalConnexion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__JournalC__3214EC07777FB5C2");

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
            entity.HasKey(e => e.CodeMachine).HasName("PK__Machine__50D6760FE12E5B73");

            entity.ToTable("Machine");

            entity.HasIndex(e => e.OperationCode, "IX_Machine_Operation");

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

            entity.HasOne(d => d.OperationCodeNavigation).WithMany(p => p.Machines)
                .HasForeignKey(d => d.OperationCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Machine__Operati__3E1D39E1");

            entity.HasMany(d => d.RefFamilleCorps).WithMany(p => p.MachineCodes)
                .UsingEntity<Dictionary<string, object>>(
                    "MachineFamilleCorp",
                    r => r.HasOne<RefFamilleCorp>().WithMany()
                        .HasForeignKey("RefFamilleCorpsId")
                        .HasConstraintName("FK__Machine_F__RefFa__45BE5BA9"),
                    l => l.HasOne<Machine>().WithMany()
                        .HasForeignKey("MachineCode")
                        .HasConstraintName("FK__Machine_F__Machi__44CA3770"),
                    j =>
                    {
                        j.HasKey("MachineCode", "RefFamilleCorpsId").HasName("PK__Machine___74270A8A22BB041B");
                        j.ToTable("Machine_FamilleCorps");
                        j.IndexerProperty<string>("MachineCode")
                            .HasMaxLength(30)
                            .IsUnicode(false);
                    });
        });

        modelBuilder.Entity<MagExpeditionBl>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Mag_Expe__3214EC0738000C45");

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

            entity.HasOne(d => d.NumeroBlNavigation).WithMany(p => p.MagExpeditionBls)
                .HasForeignKey(d => d.NumeroBl)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Mag_Exped__Numer__2F650636");
        });

        modelBuilder.Entity<MagExpeditionBlScanOf>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Mag_Expe__3214EC07C18CA570");

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
                .HasConstraintName("FK__Mag_Exped__Exped__361203C5");

            entity.HasOne(d => d.NumeroOfscanneNavigation).WithMany(p => p.MagExpeditionBlScanOfs)
                .HasForeignKey(d => d.NumeroOfscanne)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Mag_Exped__Numer__370627FE");
        });

        modelBuilder.Entity<MagPreparationOf>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Mag_Prep__3214EC0730FB5A9C");

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

            entity.HasOne(d => d.NumeroOfNavigation).WithMany(p => p.MagPreparationOfs)
                .HasForeignKey(d => d.NumeroOf)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Mag_Prepa__Numer__1D4655FB");
        });

        modelBuilder.Entity<MagPreparationOfLot>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Mag_Prep__3214EC074226FAFB");

            entity.ToTable("Mag_PreparationOF_Lot");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CodeArticle)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.DateScan)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.NumeroLotScanne)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PreparationOfid).HasColumnName("PreparationOFId");

            entity.HasOne(d => d.CodeArticleNavigation).WithMany(p => p.MagPreparationOfLots)
                .HasForeignKey(d => d.CodeArticle)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Mag_Prepa__CodeA__24E777C3");

            entity.HasOne(d => d.PreparationOf).WithMany(p => p.MagPreparationOfLots)
                .HasForeignKey(d => d.PreparationOfid)
                .HasConstraintName("FK__Mag_Prepa__Prepa__23F3538A");
        });

        modelBuilder.Entity<MagQuickControlRapport>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Mag_Quic__3214EC07831D104F");

            entity.ToTable("Mag_QuickControl_Rapport");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CodeArticle)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.DateScan)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.NumeroOf)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("NumeroOF");
            entity.Property(e => e.NumeroRapportQc)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("NumeroRapportQC");

            entity.HasOne(d => d.CodeArticleNavigation).WithMany(p => p.MagQuickControlRapports)
                .HasForeignKey(d => d.CodeArticle)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Mag_Quick__CodeA__2AA05119");

            entity.HasOne(d => d.NumeroOfNavigation).WithMany(p => p.MagQuickControlRapports)
                .HasForeignKey(d => d.NumeroOf)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Mag_Quick__Numer__29AC2CE0");
        });

        modelBuilder.Entity<MfgheadOrdreFabrication>(entity =>
        {
            entity.HasKey(e => e.NumeroOf).HasName("PK__MFGHEAD___C6A65F300CD1F8C4");

            entity.ToTable("MFGHEAD_OrdreFabrication");

            entity.HasIndex(e => e.CodeArticle, "IX_MFGHEAD_CodeArticle");

            entity.Property(e => e.NumeroOf)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("NumeroOF");
            entity.Property(e => e.CodeArticle)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.DateDebut).HasColumnType("datetime");
            entity.Property(e => e.DateFin).HasColumnType("datetime");
            entity.Property(e => e.StatutOf)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("StatutOF");

            entity.HasOne(d => d.CodeArticleNavigation).WithMany(p => p.MfgheadOrdreFabrications)
                .HasForeignKey(d => d.CodeArticle)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__MFGHEAD_O__CodeA__31B762FC");
        });

        modelBuilder.Entity<MfgmatBesoinOf>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__MFGMAT_B__3214EC0795475DBA");

            entity.ToTable("MFGMAT_BesoinOF");

            entity.Property(e => e.CodeArticle)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.NumeroOf)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("NumeroOF");

            entity.HasOne(d => d.CodeArticleNavigation).WithMany(p => p.MfgmatBesoinOfs)
                .HasForeignKey(d => d.CodeArticle)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__MFGMAT_Be__CodeA__3864608B");

            entity.HasOne(d => d.NumeroOfNavigation).WithMany(p => p.MfgmatBesoinOfs)
                .HasForeignKey(d => d.NumeroOf)
                .HasConstraintName("FK__MFGMAT_Be__Numer__37703C52");
        });

        modelBuilder.Entity<ModeleFabricationEntete>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Modele_F__3214EC07E0708556");

            entity.ToTable("Modele_Fabrication_Entete");

            entity.HasIndex(e => new { e.Code, e.Libelle, e.Version }, "UQ__Modele_F__EE6B98875B0FA584").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Code)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.CreeLe)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.CreePar)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.FamilleProduitFiniCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Libelle)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.NatureArticleCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.OperationCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Statut)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue(StatutsPlan.Brouillon);

            entity.HasOne(d => d.FamilleProduitFiniCodeNavigation).WithMany(p => p.ModeleFabricationEntetes)
                .HasForeignKey(d => d.FamilleProduitFiniCode)
                .HasConstraintName("FK__Modele_Fa__Famil__6FB49575");

            entity.HasOne(d => d.Formulaire).WithMany(p => p.ModeleFabricationEntetes)
                .HasForeignKey(d => d.FormulaireId)
                .HasConstraintName("FK__Modele_Fa__Formu__70A8B9AE");

            entity.HasOne(d => d.NatureArticleCodeNavigation).WithMany(p => p.ModeleFabricationEntetes)
                .HasForeignKey(d => d.NatureArticleCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Modele_Fa__Natur__6DCC4D03");

            entity.HasOne(d => d.OperationCodeNavigation).WithMany(p => p.ModeleFabricationEntetes)
                .HasForeignKey(d => d.OperationCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Modele_Fa__Opera__6EC0713C");
        });

        modelBuilder.Entity<ModeleFabricationLigne>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Modele_F__3214EC070430B8AD");

            entity.ToTable("Modele_Fabrication_Ligne");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.ImageBase64).IsUnicode(false);
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

            entity.HasOne(d => d.InstrumentCodeNavigation).WithMany(p => p.ModeleFabricationLignes)
                .HasForeignKey(d => d.InstrumentCode)
                .HasConstraintName("FK__Modele_Fa__Instr__03BB8E22");

            entity.HasOne(d => d.MoyenControle).WithMany(p => p.ModeleFabricationLignes)
                .HasForeignKey(d => d.MoyenControleId)
                .HasConstraintName("FK__Modele_Fa__Moyen__0697FACD");

            entity.HasOne(d => d.Periodicite).WithMany(p => p.ModeleFabricationLignes)
                .HasForeignKey(d => d.PeriodiciteId)
                .HasConstraintName("FK__Modele_Fa__Perio__04AFB25B");

            entity.HasOne(d => d.Section).WithMany(p => p.ModeleFabricationLignes)
                .HasForeignKey(d => d.SectionId)
                .HasConstraintName("FK__Modele_Fa__Secti__7FEAFD3E");

            entity.HasOne(d => d.TypeCaracteristique).WithMany(p => p.ModeleFabricationLignes)
                .HasForeignKey(d => d.TypeCaracteristiqueId)
                .HasConstraintName("FK__Modele_Fa__TypeC__01D345B0");

            entity.HasOne(d => d.TypeControle).WithMany(p => p.ModeleFabricationLignes)
                .HasForeignKey(d => d.TypeControleId)
                .HasConstraintName("FK__Modele_Fa__TypeC__02C769E9");
        });

        modelBuilder.Entity<ModeleFabricationSection>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Modele_F__3214EC073F5A557F");

            entity.ToTable("Modele_Fabrication_Section");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.LibelleSection)
                .HasMaxLength(200)
                .IsUnicode(false);

            entity.HasOne(d => d.ModeleEntete).WithMany(p => p.ModeleFabricationSections)
                .HasForeignKey(d => d.ModeleEnteteId)
                .HasConstraintName("FK__Modele_Fa__Model__7849DB76");

            entity.HasOne(d => d.Periodicite).WithMany(p => p.ModeleFabricationSections)
                .HasForeignKey(d => d.PeriodiciteId)
                .HasConstraintName("FK__Modele_Fa__Perio__7B264821");

            entity.HasOne(d => d.RegleEchantillonnage).WithMany(p => p.ModeleFabricationSections)
                .HasForeignKey(d => d.RegleEchantillonnageId)
                .HasConstraintName("FK__Modele_Fa__Regle__7C1A6C5A");

            entity.HasOne(d => d.TypeSection).WithMany(p => p.ModeleFabricationSections)
                .HasForeignKey(d => d.TypeSectionId)
                .HasConstraintName("FK__Modele_Fa__TypeS__7A3223E8");
        });

        modelBuilder.Entity<MoyenControle>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__MoyenCon__3214EC07D34A6B6A");

            entity.ToTable("MoyenControle", tb => tb.HasTrigger("trg_no_del_MoyenControle"));

            entity.HasIndex(e => e.Code, "UQ__MoyenCon__A25C5AA7020EB0CF").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.Code)
                .HasMaxLength(40)
                .IsUnicode(false);
            entity.Property(e => e.Libelle)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<NatureArticle>(entity =>
        {
            entity.HasKey(e => e.Code).HasName("PK__NatureAr__A25C5AA62A8A4B75");

            entity.ToTable("NatureArticle", tb => tb.HasTrigger("trg_no_del_NatureArticle"));

            entity.Property(e => e.Code)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.Libelle)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Origine)
                .HasMaxLength(10)
                .IsUnicode(false);
        });

        modelBuilder.Entity<NatureArticleOperation>(entity =>
        {
            entity.HasKey(e => new { e.NatureArticleCode, e.OperationCode }).HasName("PK__NatureAr__6403AE77E47AA9BD");

            entity.ToTable("NatureArticle_Operation");

            entity.Property(e => e.NatureArticleCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.OperationCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.EstObligatoire).HasDefaultValue(true);
            entity.Property(e => e.OrdreGamme).HasDefaultValue(1);

            entity.HasOne(d => d.NatureArticleCodeNavigation).WithMany(p => p.NatureArticleOperations)
                .HasForeignKey(d => d.NatureArticleCode)
                .HasConstraintName("FK__NatureArt__Natur__4AB81AF0");

            entity.HasOne(d => d.OperationCodeNavigation).WithMany(p => p.NatureArticleOperations)
                .HasForeignKey(d => d.OperationCode)
                .HasConstraintName("FK__NatureArt__Opera__4BAC3F29");
        });

        modelBuilder.Entity<Nqa>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__NQA__3214EC075DAF0E3A");

            entity.ToTable("NQA", tb => tb.HasTrigger("trg_no_del_NQA"));

            entity.HasIndex(e => e.ValeurNqa, "UQ__NQA__1DA3E248E137B48F").IsUnique();

            entity.Property(e => e.ValeurNqa).HasColumnName("ValeurNQA");
        });

        modelBuilder.Entity<Operation>(entity =>
        {
            entity.HasKey(e => e.Code).HasName("PK__Operatio__A25C5AA69D4960A1");

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
            entity.HasKey(e => e.Id).HasName("PK__OutilCon__3214EC07D81AED1B");

            entity.ToTable("OutilControle");

            entity.HasIndex(e => e.Code, "UQ__OutilCon__A25C5AA793453D27").IsUnique();

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
                .HasConstraintName("FK__OutilCont__Moyen__56E8E7AB");

            entity.HasOne(d => d.PeriodiciteDefaut).WithMany(p => p.OutilControles)
                .HasForeignKey(d => d.PeriodiciteDefautId)
                .HasConstraintName("FK__OutilCont__Perio__57DD0BE4");

            entity.HasOne(d => d.TypeCaracteristique).WithMany(p => p.OutilControles)
                .HasForeignKey(d => d.TypeCaracteristiqueId)
                .HasConstraintName("FK__OutilCont__TypeC__55F4C372");

            entity.HasOne(d => d.TypeControle).WithMany(p => p.OutilControles)
                .HasForeignKey(d => d.TypeControleId)
                .HasConstraintName("FK__OutilCont__TypeC__55009F39");
        });

        modelBuilder.Entity<Periodicite>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Periodic__3214EC0737D5FD4B");

            entity.ToTable("Periodicite", tb => tb.HasTrigger("trg_no_del_Periodicite"));

            entity.HasIndex(e => e.Code, "UQ__Periodic__A25C5AA7672C314D").IsUnique();

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
            entity.HasKey(e => e.Id).HasName("PK__PieceRef__3214EC07A6567964");

            entity.ToTable("PieceReference", tb => tb.HasTrigger("trg_no_del_PieceReference"));

            entity.HasIndex(e => e.Code, "UQ__PieceRef__A25C5AA764DB6DE6").IsUnique();

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
            entity.Property(e => e.TypePiece)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.FamilleCorps).WithMany(p => p.PieceReferences)
                .HasForeignKey(d => d.FamilleCorpsId)
                .HasConstraintName("FK__PieceRefe__Famil__08B54D69");
        });

        modelBuilder.Entity<PlanAssemblageEntete>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Ass__3214EC07941E4BA1");

            entity.ToTable("Plan_Assemblage_Entete");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreeLe)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.CreePar)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Designation)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.FamilleProduitFiniCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.ModifieLe).HasColumnType("datetime");
            entity.Property(e => e.ModifiePar)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.NatureArticleCode)
                .HasMaxLength(20)
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
                .HasDefaultValue(StatutsPlan.Brouillon);

            entity.HasOne(d => d.FamilleProduitFiniCodeNavigation).WithMany(p => p.PlanAssemblageEntetes)
                .HasForeignKey(d => d.FamilleProduitFiniCode)
                .HasConstraintName("FK__Plan_Asse__Famil__2CBDA3B5");

            entity.HasOne(d => d.Formulaire).WithMany(p => p.PlanAssemblageEntetes)
                .HasForeignKey(d => d.FormulaireId)
                .HasConstraintName("FK__Plan_Asse__Formu__32767D0B");

            entity.HasOne(d => d.NatureArticleCodeNavigation).WithMany(p => p.PlanAssemblageEntetes)
                .HasForeignKey(d => d.NatureArticleCode)
                .HasConstraintName("FK__Plan_Asse__Natur__2DB1C7EE");

            entity.HasOne(d => d.OperationCodeNavigation).WithMany(p => p.PlanAssemblageEntetes)
                .HasForeignKey(d => d.OperationCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_Asse__Opera__2BC97F7C");

            entity.HasOne(d => d.PosteCodeNavigation).WithMany(p => p.PlanAssemblageEntetes)
                .HasForeignKey(d => d.PosteCode)
                .HasConstraintName("FK__Plan_Asse__Poste__2EA5EC27");
        });

        modelBuilder.Entity<PlanAssemblageLigne>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Ass__3214EC07839CC3E0");

            entity.ToTable("Plan_Assemblage_Ligne");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.ImageBase64).IsUnicode(false);
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

            entity.HasOne(d => d.Defautheque).WithMany(p => p.PlanAssemblageLignes)
                .HasForeignKey(d => d.DefauthequeId)
                .HasConstraintName("FK__Plan_Asse__Defau__4865BE2A");

            entity.HasOne(d => d.InstrumentCodeNavigation).WithMany(p => p.PlanAssemblageLignes)
                .HasForeignKey(d => d.InstrumentCode)
                .HasConstraintName("FK__Plan_Asse__Instr__4589517F");

            entity.HasOne(d => d.MachineCodeNavigation).WithMany(p => p.PlanAssemblageLignes)
                .HasForeignKey(d => d.MachineCode)
                .HasConstraintName("FK__Plan_Asse__Machi__44952D46");

            entity.HasOne(d => d.MoyenControle).WithMany(p => p.PlanAssemblageLignes)
                .HasForeignKey(d => d.MoyenControleId)
                .HasConstraintName("FK__Plan_Asse__Moyen__4A4E069C");

            entity.HasOne(d => d.Periodicite).WithMany(p => p.PlanAssemblageLignes)
                .HasForeignKey(d => d.PeriodiciteId)
                .HasConstraintName("FK__Plan_Asse__Perio__467D75B8");

            entity.HasOne(d => d.PlanEntete).WithMany(p => p.PlanAssemblageLignes)
                .HasForeignKey(d => d.PlanEnteteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_Asse__PlanE__3FD07829");

            entity.HasOne(d => d.Section).WithMany(p => p.PlanAssemblageLignes)
                .HasForeignKey(d => d.SectionId)
                .HasConstraintName("FK__Plan_Asse__Secti__40C49C62");

            entity.HasOne(d => d.TypeCaracteristique).WithMany(p => p.PlanAssemblageLignes)
                .HasForeignKey(d => d.TypeCaracteristiqueId)
                .HasConstraintName("FK__Plan_Asse__TypeC__42ACE4D4");

            entity.HasOne(d => d.TypeControle).WithMany(p => p.PlanAssemblageLignes)
                .HasForeignKey(d => d.TypeControleId)
                .HasConstraintName("FK__Plan_Asse__TypeC__43A1090D");
        });

        modelBuilder.Entity<PlanAssemblageSection>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Ass__3214EC07E5750609");

            entity.ToTable("Plan_Assemblage_Section");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.LibelleSection)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.NormeReference)
                .HasMaxLength(40)
                .IsUnicode(false);

            entity.HasOne(d => d.Nqa).WithMany(p => p.PlanAssemblageSections)
                .HasForeignKey(d => d.NqaId)
                .HasConstraintName("FK__Plan_Asse__NqaId__3B0BC30C");

            entity.HasOne(d => d.Periodicite).WithMany(p => p.PlanAssemblageSections)
                .HasForeignKey(d => d.PeriodiciteId)
                .HasConstraintName("FK__Plan_Asse__Perio__3A179ED3");

            entity.HasOne(d => d.PlanEntete).WithMany(p => p.PlanAssemblageSections)
                .HasForeignKey(d => d.PlanEnteteId)
                .HasConstraintName("FK__Plan_Asse__PlanE__373B3228");

            entity.HasOne(d => d.RegleEchantillonnage).WithMany(p => p.PlanAssemblageSections)
                .HasForeignKey(d => d.RegleEchantillonnageId)
                .HasConstraintName("FK__Plan_Asse__Regle__3BFFE745");

            entity.HasOne(d => d.TypeSection).WithMany(p => p.PlanAssemblageSections)
                .HasForeignKey(d => d.TypeSectionId)
                .HasConstraintName("FK__Plan_Asse__TypeS__39237A9A");
        });

        modelBuilder.Entity<PlanControlePosteEntete>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Con__3214EC07EEEC1554");

            entity.ToTable("Plan_ControlePoste_Entete");

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
                .HasDefaultValue(StatutsPlan.Brouillon);

            entity.HasOne(d => d.Formulaire).WithMany(p => p.PlanControlePosteEntetes)
                .HasForeignKey(d => d.FormulaireId)
                .HasConstraintName("FK__Plan_Cont__Formu__7073AF84");

            entity.HasOne(d => d.PosteCodeNavigation).WithMany(p => p.PlanControlePosteEntetes)
                .HasForeignKey(d => d.PosteCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_Cont__Poste__6BAEFA67");
        });

        modelBuilder.Entity<PlanControlePosteLigne>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Con__3214EC076841FF47");

            entity.ToTable("Plan_ControlePoste_Ligne");

            entity.HasIndex(e => new { e.ControlePosteEnteteId, e.OrdreAffiche }, "UQ__Plan_Con__F457F59A75B0FD75").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.MachineCode)
                .HasMaxLength(30)
                .IsUnicode(false);

            entity.HasOne(d => d.ControlePosteEntete).WithMany(p => p.PlanControlePosteLignes)
                .HasForeignKey(d => d.ControlePosteEnteteId)
                .HasConstraintName("FK__Plan_Cont__Contr__753864A1");

            entity.HasOne(d => d.MachineCodeNavigation).WithMany(p => p.PlanControlePosteLignes)
                .HasForeignKey(d => d.MachineCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_Cont__Machi__762C88DA");

            entity.HasOne(d => d.RisqueDefaut).WithMany(p => p.PlanControlePosteLignes)
                .HasForeignKey(d => d.RisqueDefautId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_Cont__Risqu__7720AD13");
        });

        modelBuilder.Entity<PlanEchantillonnageEntete>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Ech__3214EC071B73CFCF");

            entity.ToTable("Plan_Echantillonnage_Entete");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreeLe)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.CreePar)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.ModeControle)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.ModifieLe).HasColumnType("datetime");
            entity.Property(e => e.ModifiePar)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.NiveauControle)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.Statut)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue(StatutsPlan.Actif);
            entity.Property(e => e.TypePlan)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.Nqa).WithMany(p => p.PlanEchantillonnageEntetes)
                .HasForeignKey(d => d.NqaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_Echa__NqaId__5F7E2DAC");
        });

        modelBuilder.Entity<PlanEchantillonnageRegle>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Ech__3214EC076BB84052");

            entity.ToTable("Plan_Echantillonnage_Regle");

            entity.HasIndex(e => new { e.FicheEnteteId, e.LettreCode }, "UQ__Plan_Ech__D6AC40B66B1CD50F").IsUnique();

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
                .HasConstraintName("FK__Plan_Echa__Fiche__681373AD");
        });

        modelBuilder.Entity<PlanFabricationEntete>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Fab__3214EC071E52EAC3");

            entity.ToTable("Plan_Fabrication_Entete");

            entity.HasIndex(e => e.CodeArticleSageVersionne, "IX_PlanFab_CodeArticleVersionne");

            entity.HasIndex(e => e.Statut, "IX_PlanFab_Statut");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CodeArticleSageVersionne)
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
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Statut)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue(StatutsPlan.Brouillon);

            entity.HasOne(d => d.Formulaire).WithMany(p => p.PlanFabricationEntetes)
                .HasForeignKey(d => d.FormulaireId)
                .HasConstraintName("FK__Plan_Fabr__Formu__11158940");

            entity.HasOne(d => d.MachineDefautCodeNavigation).WithMany(p => p.PlanFabricationEntetes)
                .HasForeignKey(d => d.MachineDefautCode)
                .HasConstraintName("FK__Plan_Fabr__Machi__10216507");

            entity.HasOne(d => d.ModeleSource).WithMany(p => p.PlanFabricationEntetes)
                .HasForeignKey(d => d.ModeleSourceId)
                .HasConstraintName("FK__Plan_Fabr__Model__0B5CAFEA");

            entity.HasOne(d => d.OperationCodeNavigation).WithMany(p => p.PlanFabricationEntetes)
                .HasForeignKey(d => d.OperationCode)
                .HasConstraintName("FK__Plan_Fabr__Opera__0D44F85C");
        });

        modelBuilder.Entity<PlanFabricationLigne>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Fab__3214EC072CD0C6EB");

            entity.ToTable("Plan_Fabrication_Ligne");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.ImageBase64).IsUnicode(false);
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

            entity.HasOne(d => d.InstrumentCodeNavigation).WithMany(p => p.PlanFabricationLignes)
                .HasForeignKey(d => d.InstrumentCode)
                .HasConstraintName("FK__Plan_Fabr__Instr__24285DB4");

            entity.HasOne(d => d.ModeleLigneSource).WithMany(p => p.PlanFabricationLignes)
                .HasForeignKey(d => d.ModeleLigneSourceId)
                .HasConstraintName("FK__Plan_Fabr__Model__2057CCD0");

            entity.HasOne(d => d.MoyenControle).WithMany(p => p.PlanFabricationLignes)
                .HasForeignKey(d => d.MoyenControleId)
                .HasConstraintName("FK__Plan_Fabr__Moyen__2704CA5F");

            entity.HasOne(d => d.Periodicite).WithMany(p => p.PlanFabricationLignes)
                .HasForeignKey(d => d.PeriodiciteId)
                .HasConstraintName("FK__Plan_Fabr__Perio__251C81ED");

            entity.HasOne(d => d.PlanEntete).WithMany(p => p.PlanFabricationLignes)
                .HasForeignKey(d => d.PlanEnteteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_Fabr__PlanE__1E6F845E");

            entity.HasOne(d => d.Section).WithMany(p => p.PlanFabricationLignes)
                .HasForeignKey(d => d.SectionId)
                .HasConstraintName("FK__Plan_Fabr__Secti__1F63A897");

            entity.HasOne(d => d.TypeCaracteristique).WithMany(p => p.PlanFabricationLignes)
                .HasForeignKey(d => d.TypeCaracteristiqueId)
                .HasConstraintName("FK__Plan_Fabr__TypeC__22401542");

            entity.HasOne(d => d.TypeControle).WithMany(p => p.PlanFabricationLignes)
                .HasForeignKey(d => d.TypeControleId)
                .HasConstraintName("FK__Plan_Fabr__TypeC__2334397B");
        });

        modelBuilder.Entity<PlanFabricationSection>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Fab__3214EC07762AE721");

            entity.ToTable("Plan_Fabrication_Section");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.LibelleSection)
                .HasMaxLength(300)
                .IsUnicode(false);

            entity.HasOne(d => d.ModeleSection).WithMany(p => p.PlanFabricationSections)
                .HasForeignKey(d => d.ModeleSectionId)
                .HasConstraintName("FK__Plan_Fabr__Model__16CE6296");

            entity.HasOne(d => d.Periodicite).WithMany(p => p.PlanFabricationSections)
                .HasForeignKey(d => d.PeriodiciteId)
                .HasConstraintName("FK__Plan_Fabr__Perio__19AACF41");

            entity.HasOne(d => d.PlanEntete).WithMany(p => p.PlanFabricationSections)
                .HasForeignKey(d => d.PlanEnteteId)
                .HasConstraintName("FK__Plan_Fabr__PlanE__15DA3E5D");

            entity.HasOne(d => d.RegleEchantillonnage).WithMany(p => p.PlanFabricationSections)
                .HasForeignKey(d => d.RegleEchantillonnageId)
                .HasConstraintName("FK__Plan_Fabr__Regle__1A9EF37A");

            entity.HasOne(d => d.TypeSection).WithMany(p => p.PlanFabricationSections)
                .HasForeignKey(d => d.TypeSectionId)
                .HasConstraintName("FK__Plan_Fabr__TypeS__18B6AB08");
        });

        modelBuilder.Entity<PlanProduitFiniEntete>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Pro__3214EC079568C05D");

            entity.ToTable("Plan_ProduitFini_Entete");

            entity.HasIndex(e => new { e.FamilleProduitFiniCode, e.Version }, "UQ__Plan_Pro__98E0A082E143955E").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreeLe)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.CreePar)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.FamilleProduitFiniCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.ModifieLe).HasColumnType("datetime");
            entity.Property(e => e.ModifiePar)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Statut)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue(StatutsPlan.Brouillon);

            entity.HasOne(d => d.FamilleProduitFiniCodeNavigation).WithMany(p => p.PlanProduitFiniEntetes)
                .HasForeignKey(d => d.FamilleProduitFiniCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_Prod__Famil__5006DFF2");

            entity.HasOne(d => d.Formulaire).WithMany(p => p.PlanProduitFiniEntetes)
                .HasForeignKey(d => d.FormulaireId)
                .HasConstraintName("FK__Plan_Prod__Formu__53D770D6");
        });

        modelBuilder.Entity<PlanProduitFiniLigne>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Pro__3214EC0795229130");

            entity.ToTable("Plan_ProduitFini_Ligne");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.ImageBase64).IsUnicode(false);
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

            entity.HasOne(d => d.Defautheque).WithMany(p => p.PlanProduitFiniLignes)
                .HasForeignKey(d => d.DefauthequeId)
                .HasConstraintName("FK__Plan_Prod__Defau__65F62111");

            entity.HasOne(d => d.InstrumentCodeNavigation).WithMany(p => p.PlanProduitFiniLignes)
                .HasForeignKey(d => d.InstrumentCode)
                .HasConstraintName("FK__Plan_Prod__Instr__6501FCD8");

            entity.HasOne(d => d.MoyenControle).WithMany(p => p.PlanProduitFiniLignes)
                .HasForeignKey(d => d.MoyenControleId)
                .HasConstraintName("FK__Plan_Prod__Moyen__66EA454A");

            entity.HasOne(d => d.PlanEntete).WithMany(p => p.PlanProduitFiniLignes)
                .HasForeignKey(d => d.PlanEnteteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_Prod__PlanE__603D47BB");

            entity.HasOne(d => d.Section).WithMany(p => p.PlanProduitFiniLignes)
                .HasForeignKey(d => d.SectionId)
                .HasConstraintName("FK__Plan_Prod__Secti__61316BF4");

            entity.HasOne(d => d.TypeCaracteristique).WithMany(p => p.PlanProduitFiniLignes)
                .HasForeignKey(d => d.TypeCaracteristiqueId)
                .HasConstraintName("FK__Plan_Prod__TypeC__6319B466");

            entity.HasOne(d => d.TypeControle).WithMany(p => p.PlanProduitFiniLignes)
                .HasForeignKey(d => d.TypeControleId)
                .HasConstraintName("FK__Plan_Prod__TypeC__640DD89F");
        });

        modelBuilder.Entity<PlanProduitFiniSection>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Pro__3214EC076DA13DCE");

            entity.ToTable("Plan_ProduitFini_Section");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.LibelleSection)
                .HasMaxLength(300)
                .IsUnicode(false);

            entity.HasOne(d => d.Periodicite).WithMany(p => p.PlanProduitFiniSections)
                .HasForeignKey(d => d.PeriodiciteId)
                .HasConstraintName("FK__Plan_Prod__Perio__5B78929E");

            entity.HasOne(d => d.PlanEntete).WithMany(p => p.PlanProduitFiniSections)
                .HasForeignKey(d => d.PlanEnteteId)
                .HasConstraintName("FK__Plan_Prod__PlanE__589C25F3");

            entity.HasOne(d => d.RegleEchantillonnage).WithMany(p => p.PlanProduitFiniSections)
                .HasForeignKey(d => d.RegleEchantillonnageId)
                .HasConstraintName("FK__Plan_Prod__Regle__5C6CB6D7");

            entity.HasOne(d => d.TypeSection).WithMany(p => p.PlanProduitFiniSections)
                .HasForeignKey(d => d.TypeSectionId)
                .HasConstraintName("FK__Plan_Prod__TypeS__5A846E65");
        });

        modelBuilder.Entity<PlanResultatControleCfEntete>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Res__3214EC073ABBE5AF");

            entity.ToTable("Plan_ResultatControleCF_Entete");

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
                .HasDefaultValue(StatutsPlan.Brouillon);

            entity.HasOne(d => d.Formulaire).WithMany(p => p.PlanResultatControleCfEntetes)
                .HasForeignKey(d => d.FormulaireId)
                .HasConstraintName("FK__Plan_Resu__Formu__703EA55A");

            entity.HasOne(d => d.PosteCodeNavigation).WithMany(p => p.PlanResultatControleCfEntetes)
                .HasForeignKey(d => d.PosteCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_Resu__Poste__6F4A8121");
        });

        modelBuilder.Entity<PlanResultatControleCfLigne>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Res__3214EC0741C9DAC6");

            entity.ToTable("Plan_ResultatControleCF_Ligne");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Caracteristique)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.InstrumentCode)
                .HasMaxLength(40)
                .IsUnicode(false);

            entity.HasOne(d => d.InstrumentCodeNavigation).WithMany(p => p.PlanResultatControleCfLignes)
                .HasForeignKey(d => d.InstrumentCode)
                .HasConstraintName("FK__Plan_Resu__Instr__7F80E8EA");

            entity.HasOne(d => d.MoyenControle).WithMany(p => p.PlanResultatControleCfLignes)
                .HasForeignKey(d => d.MoyenControleId)
                .HasConstraintName("FK__Plan_Resu__Moyen__7E8CC4B1");

            entity.HasOne(d => d.Section).WithMany(p => p.PlanResultatControleCfLignes)
                .HasForeignKey(d => d.SectionId)
                .HasConstraintName("FK__Plan_Resu__Secti__7CA47C3F");

            entity.HasOne(d => d.TypeControle).WithMany(p => p.PlanResultatControleCfLignes)
                .HasForeignKey(d => d.TypeControleId)
                .HasConstraintName("FK__Plan_Resu__TypeC__7D98A078");
        });

        modelBuilder.Entity<PlanResultatControleCfSection>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Res__3214EC07E1A491F3");

            entity.ToTable("Plan_ResultatControleCF_Section");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.LibelleAffiche)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.PlanRccfenteteId).HasColumnName("PlanRCCFEnteteId");
            entity.Property(e => e.SectionType)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.PlanRccfentete).WithMany(p => p.PlanResultatControleCfSections)
                .HasForeignKey(d => d.PlanRccfenteteId)
                .HasConstraintName("FK__Plan_Resu__PlanR__77DFC722");
        });

        modelBuilder.Entity<PlanVerifMachineEcheance>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Ver__3214EC07D7721379");

            entity.ToTable("Plan_VerifMachine_Echeance");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");

            entity.HasOne(d => d.Periodicite).WithMany(p => p.PlanVerifMachineEcheances)
                .HasForeignKey(d => d.PeriodiciteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_Veri__Perio__0FEC5ADD");

            entity.HasOne(d => d.PlanLigne).WithMany(p => p.PlanVerifMachineEcheances)
                .HasForeignKey(d => d.PlanLigneId)
                .HasConstraintName("FK__Plan_Veri__PlanL__0EF836A4");

            entity.HasOne(d => d.RefMoyenDetection).WithMany(p => p.PlanVerifMachineEcheances)
                .HasForeignKey(d => d.RefMoyenDetectionId)
                .HasConstraintName("FK__Plan_Veri__RefMo__10E07F16");
        });

        modelBuilder.Entity<PlanVerifMachineEntete>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Ver__3214EC0775DDAE6C");

            entity.ToTable("Plan_VerifMachine_Entete");

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
                .HasDefaultValue(StatutsPlan.Brouillon);
            entity.Property(e => e.Version).HasDefaultValue(0);

            entity.HasOne(d => d.Formulaire).WithMany(p => p.PlanVerifMachineEntetes)
                .HasForeignKey(d => d.FormulaireId)
                .HasConstraintName("FK__Plan_Veri__Formu__7FB5F314");

            entity.HasOne(d => d.MachineCodeNavigation).WithMany(p => p.PlanVerifMachineEntetes)
                .HasForeignKey(d => d.MachineCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_Veri__Machi__7AF13DF7");
        });

        modelBuilder.Entity<PlanVerifMachineFamille>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Ver__3214EC073EBACDF2");

            entity.ToTable("Plan_VerifMachine_Famille");

            entity.HasIndex(e => new { e.PlanEnteteId, e.RefFamilleCorpsId }, "UQ__Plan_Ver__7457AEA4EF11BB11").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");

            entity.HasOne(d => d.PlanEntete).WithMany(p => p.PlanVerifMachineFamilles)
                .HasForeignKey(d => d.PlanEnteteId)
                .HasConstraintName("FK__Plan_Veri__PlanE__047AA831");

            entity.HasOne(d => d.RefFamilleCorps).WithMany(p => p.PlanVerifMachineFamilles)
                .HasForeignKey(d => d.RefFamilleCorpsId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_Veri__RefFa__056ECC6A");
        });

        modelBuilder.Entity<PlanVerifMachineLigne>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Ver__3214EC07EE93AC16");

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
                .HasConstraintName("FK__Plan_Veri__PlanE__093F5D4E");
        });

        modelBuilder.Entity<PlanVerifMachineMatricePiece>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Ver__3214EC07BC1B76F7");

            entity.ToTable("Plan_VerifMachine_MatricePiece");

            entity.HasIndex(e => new { e.EcheanceId, e.FamilleId, e.RoleVerif }, "UQ__Plan_Ver__779871C4741F6FCD").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.RoleVerif)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.Echeance).WithMany(p => p.PlanVerifMachineMatricePieces)
                .HasForeignKey(d => d.EcheanceId)
                .HasConstraintName("FK__Plan_Veri__Echea__1699586C");

            entity.HasOne(d => d.Famille).WithMany(p => p.PlanVerifMachineMatricePieces)
                .HasForeignKey(d => d.FamilleId)
                .HasConstraintName("FK__Plan_Veri__Famil__178D7CA5");

            entity.HasOne(d => d.PieceRef).WithMany(p => p.PlanVerifMachineMatricePieces)
                .HasForeignKey(d => d.PieceRefId)
                .HasConstraintName("FK__Plan_Veri__Piece__1975C517");
        });

        modelBuilder.Entity<PosteTravail>(entity =>
        {
            entity.HasKey(e => e.CodePoste).HasName("PK__PosteTra__4045446B37E86594");

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
                        .HasConstraintName("FK__PosteTrav__CodeM__498EEC8D"),
                    l => l.HasOne<PosteTravail>().WithMany()
                        .HasForeignKey("CodePoste")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__PosteTrav__CodeP__489AC854"),
                    j =>
                    {
                        j.HasKey("CodePoste", "CodeMachine").HasName("PK__PosteTra__A548230BD80BC1AF");
                        j.ToTable("PosteTravail_Machine");
                        j.IndexerProperty<string>("CodePoste")
                            .HasMaxLength(30)
                            .IsUnicode(false);
                        j.IndexerProperty<string>("CodeMachine")
                            .HasMaxLength(30)
                            .IsUnicode(false);
                    });
        });

        modelBuilder.Entity<ProduitFini>(entity =>
        {
            entity.HasKey(e => e.CodeArticle).HasName("PK__ProduitF__32384FB04AC6695E");

            entity.ToTable("ProduitFini");

            entity.HasIndex(e => e.FamilleProduitFiniCode, "IX_ProduitFini_FamilleCode");

            entity.Property(e => e.CodeArticle)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.FamilleProduitFiniCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.TypeRobinetCode)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.CodeArticleNavigation).WithOne(p => p.ProduitFini)
                .HasForeignKey<ProduitFini>(d => d.CodeArticle)
                .HasConstraintName("FK__ProduitFi__CodeA__245D67DE");

            entity.HasOne(d => d.FamilleProduitFiniCodeNavigation).WithMany(p => p.ProduitFinis)
                .HasForeignKey(d => d.FamilleProduitFiniCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ProduitFi__Famil__25518C17");

            entity.HasOne(d => d.TypeRobinetCodeNavigation).WithMany(p => p.ProduitFinis)
                .HasForeignKey(d => d.TypeRobinetCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ProduitFi__TypeR__2645B050");
        });

        modelBuilder.Entity<RefFamilleCorp>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Ref_Fami__3214EC07CD060C7C");

            entity.ToTable("Ref_FamilleCorps", tb => tb.HasTrigger("trg_no_del_Ref_FamilleCorps"));

            entity.HasIndex(e => e.Code, "UQ__Ref_Fami__A25C5AA71545E9EE").IsUnique();

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
            entity.HasKey(e => e.Id).HasName("PK__Ref_Form__3214EC07A5FC7646");

            entity.ToTable("Ref_Formulaire");

            entity.HasIndex(e => new { e.CodeReference, e.Version }, "UQ__Ref_Form__4F92504BDAD05011").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CodeReference)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.CreeLe)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.CreePar)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Designation)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.ModifieLe).HasColumnType("datetime");
            entity.Property(e => e.ModifiePar)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Statut)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue(StatutsPlan.Actif);
        });

        modelBuilder.Entity<RefMoyenDetection>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Ref_Moye__3214EC079EFACF60");

            entity.ToTable("Ref_MoyenDetection", tb => tb.HasTrigger("trg_no_del_Ref_MoyenDetection"));

            entity.HasIndex(e => e.Code, "UQ__Ref_Moye__A25C5AA78AA99D52").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Designation)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<RefRegleEchantillonnage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Ref_Regl__3214EC07D7889BC3");

            entity.ToTable("Ref_RegleEchantillonnage", tb => tb.HasTrigger("trg_no_del_Ref_RegleEchantillonnage"));

            entity.HasIndex(e => e.Code, "UQ__Ref_Regl__A25C5AA7EBFF77A2").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.Code)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Libelle)
                .HasMaxLength(250)
                .IsUnicode(false);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RefreshT__3214EC07E888431B");

            entity.HasIndex(e => e.Token, "UQ__RefreshT__1EB4F817E3E3EC70").IsUnique();

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
                .HasConstraintName("FK__RefreshTo__Utili__151B244E");
        });

        modelBuilder.Entity<RisqueDefaut>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RisqueDe__3214EC07132E40A4");

            entity.ToTable("RisqueDefaut", tb => tb.HasTrigger("trg_no_del_RisqueDefaut"));

            entity.HasIndex(e => e.CodeDefaut, "UQ__RisqueDe__2EF8734337EDBAFE").IsUnique();

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
            entity.HasKey(e => e.NumeroBl).HasName("PK__SDELIVER__C664DCCDE8D9D7A4");

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
            entity.HasKey(e => e.Id).HasName("PK__TypeCara__3214EC0722407D07");

            entity.ToTable("TypeCaracteristique", tb => tb.HasTrigger("trg_no_del_TypeCaracteristique"));

            entity.HasIndex(e => e.Code, "UQ__TypeCara__A25C5AA7A13F9AE6").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.Code)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Libelle)
                .HasMaxLength(80)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TypeControle>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TypeCont__3214EC078C5797E8");

            entity.ToTable("TypeControle", tb => tb.HasTrigger("trg_no_del_TypeControle"));

            entity.HasIndex(e => e.Code, "UQ__TypeCont__A25C5AA7D7865E9C").IsUnique();

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
            entity.HasKey(e => e.Code).HasName("PK__TypeRobi__A25C5AA66E5CACFF");

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
            entity.HasKey(e => e.Id).HasName("PK__TypeSect__3214EC07CC8638B5");

            entity.ToTable("TypeSection", tb => tb.HasTrigger("trg_no_del_TypeSection"));

            entity.HasIndex(e => e.Code, "UQ__TypeSect__A25C5AA723C0FEA4").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.Code)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.Libelle)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<UtilisateursApp>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Utilisat__3214EC07FC561A5C");

            entity.ToTable("UtilisateursApp");

            entity.HasIndex(e => e.Matricule, "IX_UtilisateursApp_Matricule");

            entity.HasIndex(e => e.Matricule, "UQ__Utilisat__0FB9FB4362965C4A").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__Utilisat__A9D10534717BBC3D").IsUnique();

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
