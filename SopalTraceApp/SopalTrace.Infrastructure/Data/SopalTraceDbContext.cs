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

    public virtual DbSet<PlanEchantillonnageEntete> PlanEchantillonnageEntetes { get; set; }

    public virtual DbSet<PlanEchantillonnageRegle> PlanEchantillonnageRegles { get; set; }

    public virtual DbSet<PlanFabricationEntete> PlanFabricationEntetes { get; set; }

    public virtual DbSet<PlanFabricationLigne> PlanFabricationLignes { get; set; }

    public virtual DbSet<PlanFabricationSection> PlanFabricationSections { get; set; }

    public virtual DbSet<PlanNonConformiteEntete> PlanNonConformiteEntetes { get; set; }

    public virtual DbSet<PlanNonConformiteLigne> PlanNonConformiteLignes { get; set; }

    public virtual DbSet<PlanProduitFiniEntete> PlanProduitFiniEntetes { get; set; }

    public virtual DbSet<PlanProduitFiniLigne> PlanProduitFiniLignes { get; set; }

    public virtual DbSet<PlanProduitFiniSection> PlanProduitFiniSections { get; set; }

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
            entity.HasKey(e => e.CodeArticle).HasName("PK__Article__32384FB0D74B97DC");

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
                .HasDefaultValue("ACTIF");
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
            entity.HasKey(e => new { e.Codfic0, e.Zone0, e.Ident10, e.Langue0 }).HasName("PK__ATEXTRA__4F21B2DBFD680F72");

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
            entity.HasKey(e => e.Usr0).HasName("PK__AUTILIS__0812AE696CA3CACE");

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
            entity.HasKey(e => new { e.ArticleParent, e.CodeComposant, e.CodeAlternative }).HasName("PK__BOMD_Nom__2710E851CBE28CDC");

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
            entity.HasKey(e => e.Id).HasName("PK__Defauthe__3214EC07FD85FE11");

            entity.ToTable("Defautheque", tb => tb.HasTrigger("trg_no_del_Defautheque"));

            entity.HasIndex(e => e.Code, "UQ__Defauthe__A25C5AA719D943A7").IsUnique();

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
            entity.HasKey(e => e.Id).HasName("PK__Exec_Con__3214EC0794325BF3");

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
                .HasConstraintName("FK__Exec_Cont__Machi__4183B671");

            entity.HasOne(d => d.MachineCodePrevuNavigation).WithMany(p => p.ExecControleOfMachineCodePrevuNavigations)
                .HasForeignKey(d => d.MachineCodePrevu)
                .HasConstraintName("FK__Exec_Cont__Machi__3F9B6DFF");

            entity.HasOne(d => d.NumeroOfNavigation).WithMany(p => p.ExecControleOfs)
                .HasForeignKey(d => d.NumeroOf)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Exec_Cont__Numer__3DB3258D");

            entity.HasOne(d => d.OperationCodeNavigation).WithMany(p => p.ExecControleOfs)
                .HasForeignKey(d => d.OperationCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Exec_Cont__Opera__3EA749C6");

            entity.HasOne(d => d.PosteCodeNavigation).WithMany(p => p.ExecControleOfPosteCodeNavigations)
                .HasForeignKey(d => d.PosteCode)
                .HasConstraintName("FK__Exec_Cont__Poste__4277DAAA");

            entity.HasOne(d => d.PosteCodePrevuNavigation).WithMany(p => p.ExecControleOfPosteCodePrevuNavigations)
                .HasForeignKey(d => d.PosteCodePrevu)
                .HasConstraintName("FK__Exec_Cont__Poste__408F9238");
        });

        modelBuilder.Entity<ExecControleTranche>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Exec_Con__3214EC07BE49847D");

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
                .HasConstraintName("FK__Exec_Cont__ExecC__50C5FA01");
        });

        modelBuilder.Entity<ExecPieceType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Exec_Pie__3214EC07D57B90E2");

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
                .HasConstraintName("FK__Exec_Piec__ExecC__4B0D20AB");
        });

        modelBuilder.Entity<ExecPrelevement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Exec_Pre__3214EC078E6DDE6B");

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
                .HasConstraintName("FK__Exec_Prel__ExecC__558AAF1E");
        });

        modelBuilder.Entity<ExecPrelevementLigne>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Exec_Pre__3214EC07F24444CA");

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
                .HasConstraintName("FK__Exec_Prel__Prele__5A4F643B");
        });

        modelBuilder.Entity<FamilleProduitFini>(entity =>
        {
            entity.HasKey(e => e.Code).HasName("PK__FamilleP__A25C5AA65EFF473A");

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
            entity.HasKey(e => e.CodeInstrument).HasName("PK__Instrume__E6E43505F4BB8197");

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

        modelBuilder.Entity<JournalConnexion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__JournalC__3214EC07F681A23C");

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
            entity.HasKey(e => e.CodeMachine).HasName("PK__Machine__50D6760FF080B099");

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
                        j.HasKey("MachineCode", "RefFamilleCorpsId").HasName("PK__Machine___74270A8AA22DEF08");
                        j.ToTable("Machine_FamilleCorps");
                        j.IndexerProperty<string>("MachineCode")
                            .HasMaxLength(30)
                            .IsUnicode(false);
                    });
        });

        modelBuilder.Entity<MagExpeditionBl>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Mag_Expe__3214EC0706D3B411");

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
                .HasConstraintName("FK__Mag_Exped__Numer__314D4EA8");
        });

        modelBuilder.Entity<MagExpeditionBlScanOf>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Mag_Expe__3214EC07CF93BD06");

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
                .HasConstraintName("FK__Mag_Exped__Exped__37FA4C37");

            entity.HasOne(d => d.NumeroOfscanneNavigation).WithMany(p => p.MagExpeditionBlScanOfs)
                .HasForeignKey(d => d.NumeroOfscanne)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Mag_Exped__Numer__38EE7070");
        });

        modelBuilder.Entity<MagPreparationOf>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Mag_Prep__3214EC07A66E557A");

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
                .HasConstraintName("FK__Mag_Prepa__Numer__1F2E9E6D");
        });

        modelBuilder.Entity<MagPreparationOfLot>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Mag_Prep__3214EC07CF68B37D");

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
                .HasConstraintName("FK__Mag_Prepa__CodeA__26CFC035");

            entity.HasOne(d => d.PreparationOf).WithMany(p => p.MagPreparationOfLots)
                .HasForeignKey(d => d.PreparationOfid)
                .HasConstraintName("FK__Mag_Prepa__Prepa__25DB9BFC");
        });

        modelBuilder.Entity<MagQuickControlRapport>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Mag_Quic__3214EC077C07A324");

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
                .HasConstraintName("FK__Mag_Quick__CodeA__2C88998B");

            entity.HasOne(d => d.NumeroOfNavigation).WithMany(p => p.MagQuickControlRapports)
                .HasForeignKey(d => d.NumeroOf)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Mag_Quick__Numer__2B947552");
        });

        modelBuilder.Entity<MfgheadOrdreFabrication>(entity =>
        {
            entity.HasKey(e => e.NumeroOf).HasName("PK__MFGHEAD___C6A65F306DDA6D24");

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
            entity.HasKey(e => e.Id).HasName("PK__MFGMAT_B__3214EC07DFB9B60B");

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
            entity.HasKey(e => e.Id).HasName("PK__Modele_F__3214EC07CB16BB8A");

            entity.ToTable("Modele_Fabrication_Entete");

            entity.HasIndex(e => new { e.Code, e.Libelle, e.Version }, "UQ__Modele_F__EE6B98877C5EA8D7").IsUnique();

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
                .HasDefaultValue("BROUILLON");

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
            entity.HasKey(e => e.Id).HasName("PK__Modele_F__3214EC0725D5A1CE");

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
            entity.HasKey(e => e.Id).HasName("PK__Modele_F__3214EC0735DF82DB");

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
            entity.HasKey(e => e.Id).HasName("PK__MoyenCon__3214EC075E337C7D");

            entity.ToTable("MoyenControle", tb => tb.HasTrigger("trg_no_del_MoyenControle"));

            entity.HasIndex(e => e.Code, "UQ__MoyenCon__A25C5AA7BA6D098E").IsUnique();

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
            entity.HasKey(e => e.Code).HasName("PK__NatureAr__A25C5AA6F013914B");

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
            entity.HasKey(e => new { e.NatureArticleCode, e.OperationCode }).HasName("PK__NatureAr__6403AE7791A40BBB");

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
            entity.HasKey(e => e.Id).HasName("PK__NQA__3214EC074D673846");

            entity.ToTable("NQA", tb => tb.HasTrigger("trg_no_del_NQA"));

            entity.HasIndex(e => e.ValeurNqa, "UQ__NQA__1DA3E248A57E60F6").IsUnique();

            entity.Property(e => e.ValeurNqa).HasColumnName("ValeurNQA");
        });

        modelBuilder.Entity<Operation>(entity =>
        {
            entity.HasKey(e => e.Code).HasName("PK__Operatio__A25C5AA6901AEAAF");

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
            entity.HasKey(e => e.Id).HasName("PK__OutilCon__3214EC07610C5131");

            entity.ToTable("OutilControle");

            entity.HasIndex(e => e.Code, "UQ__OutilCon__A25C5AA7B92EB2E2").IsUnique();

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
            entity.HasKey(e => e.Id).HasName("PK__Periodic__3214EC077A82532B");

            entity.ToTable("Periodicite", tb => tb.HasTrigger("trg_no_del_Periodicite"));

            entity.HasIndex(e => e.Code, "UQ__Periodic__A25C5AA74D914937").IsUnique();

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
            entity.HasKey(e => e.Id).HasName("PK__PieceRef__3214EC07AB0C9BE9");

            entity.ToTable("PieceReference", tb => tb.HasTrigger("trg_no_del_PieceReference"));

            entity.HasIndex(e => e.Code, "UQ__PieceRef__A25C5AA73AB98E4A").IsUnique();

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
            entity.HasKey(e => e.Id).HasName("PK__Plan_Ass__3214EC07B13D9689");

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
                .HasDefaultValue("BROUILLON");

            entity.HasOne(d => d.FamilleProduitFiniCodeNavigation).WithMany(p => p.PlanAssemblageEntetes)
                .HasForeignKey(d => d.FamilleProduitFiniCode)
                .HasConstraintName("FK__Plan_Asse__Famil__2DB1C7EE");

            entity.HasOne(d => d.Formulaire).WithMany(p => p.PlanAssemblageEntetes)
                .HasForeignKey(d => d.FormulaireId)
                .HasConstraintName("FK__Plan_Asse__Formu__336AA144");

            entity.HasOne(d => d.NatureArticleCodeNavigation).WithMany(p => p.PlanAssemblageEntetes)
                .HasForeignKey(d => d.NatureArticleCode)
                .HasConstraintName("FK__Plan_Asse__Natur__2EA5EC27");

            entity.HasOne(d => d.OperationCodeNavigation).WithMany(p => p.PlanAssemblageEntetes)
                .HasForeignKey(d => d.OperationCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_Asse__Opera__2CBDA3B5");

            entity.HasOne(d => d.PosteCodeNavigation).WithMany(p => p.PlanAssemblageEntetes)
                .HasForeignKey(d => d.PosteCode)
                .HasConstraintName("FK__Plan_Asse__Poste__2F9A1060");
        });

        modelBuilder.Entity<PlanAssemblageLigne>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Ass__3214EC0770D77153");

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
                .HasConstraintName("FK__Plan_Asse__Defau__4959E263");

            entity.HasOne(d => d.InstrumentCodeNavigation).WithMany(p => p.PlanAssemblageLignes)
                .HasForeignKey(d => d.InstrumentCode)
                .HasConstraintName("FK__Plan_Asse__Instr__467D75B8");

            entity.HasOne(d => d.MachineCodeNavigation).WithMany(p => p.PlanAssemblageLignes)
                .HasForeignKey(d => d.MachineCode)
                .HasConstraintName("FK__Plan_Asse__Machi__4589517F");

            entity.HasOne(d => d.MoyenControle).WithMany(p => p.PlanAssemblageLignes)
                .HasForeignKey(d => d.MoyenControleId)
                .HasConstraintName("FK__Plan_Asse__Moyen__4B422AD5");

            entity.HasOne(d => d.Periodicite).WithMany(p => p.PlanAssemblageLignes)
                .HasForeignKey(d => d.PeriodiciteId)
                .HasConstraintName("FK__Plan_Asse__Perio__477199F1");

            entity.HasOne(d => d.PlanEntete).WithMany(p => p.PlanAssemblageLignes)
                .HasForeignKey(d => d.PlanEnteteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_Asse__PlanE__40C49C62");

            entity.HasOne(d => d.Section).WithMany(p => p.PlanAssemblageLignes)
                .HasForeignKey(d => d.SectionId)
                .HasConstraintName("FK__Plan_Asse__Secti__41B8C09B");

            entity.HasOne(d => d.TypeCaracteristique).WithMany(p => p.PlanAssemblageLignes)
                .HasForeignKey(d => d.TypeCaracteristiqueId)
                .HasConstraintName("FK__Plan_Asse__TypeC__43A1090D");

            entity.HasOne(d => d.TypeControle).WithMany(p => p.PlanAssemblageLignes)
                .HasForeignKey(d => d.TypeControleId)
                .HasConstraintName("FK__Plan_Asse__TypeC__44952D46");
        });

        modelBuilder.Entity<PlanAssemblageSection>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Ass__3214EC073D16F4EE");

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
                .HasConstraintName("FK__Plan_Asse__NqaId__3BFFE745");

            entity.HasOne(d => d.Periodicite).WithMany(p => p.PlanAssemblageSections)
                .HasForeignKey(d => d.PeriodiciteId)
                .HasConstraintName("FK__Plan_Asse__Perio__3B0BC30C");

            entity.HasOne(d => d.PlanEntete).WithMany(p => p.PlanAssemblageSections)
                .HasForeignKey(d => d.PlanEnteteId)
                .HasConstraintName("FK__Plan_Asse__PlanE__382F5661");

            entity.HasOne(d => d.RegleEchantillonnage).WithMany(p => p.PlanAssemblageSections)
                .HasForeignKey(d => d.RegleEchantillonnageId)
                .HasConstraintName("FK__Plan_Asse__Regle__3CF40B7E");

            entity.HasOne(d => d.TypeSection).WithMany(p => p.PlanAssemblageSections)
                .HasForeignKey(d => d.TypeSectionId)
                .HasConstraintName("FK__Plan_Asse__TypeS__3A179ED3");
        });

        modelBuilder.Entity<PlanEchantillonnageEntete>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Ech__3214EC07990C8970");

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
                .HasDefaultValue("ACTIF");
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
            entity.HasKey(e => e.Id).HasName("PK__Plan_Ech__3214EC07EF4BF97F");

            entity.ToTable("Plan_Echantillonnage_Regle");

            entity.HasIndex(e => new { e.FicheEnteteId, e.LettreCode }, "UQ__Plan_Ech__D6AC40B66079A6D1").IsUnique();

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
            entity.HasKey(e => e.Id).HasName("PK__Plan_Fab__3214EC0763E3BC03");

            entity.ToTable("Plan_Fabrication_Entete");

            entity.HasIndex(e => e.CodeArticleSage, "IX_PlanFab_CodeArticle");

            entity.HasIndex(e => e.Statut, "IX_PlanFab_Statut");

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
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Statut)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("BROUILLON");

            entity.HasOne(d => d.CodeArticleSageNavigation).WithMany(p => p.PlanFabricationEntetes)
                .HasForeignKey(d => d.CodeArticleSage)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_Fabr__CodeA__0C50D423");

            entity.HasOne(d => d.Formulaire).WithMany(p => p.PlanFabricationEntetes)
                .HasForeignKey(d => d.FormulaireId)
                .HasConstraintName("FK__Plan_Fabr__Formu__1209AD79");

            entity.HasOne(d => d.MachineDefautCodeNavigation).WithMany(p => p.PlanFabricationEntetes)
                .HasForeignKey(d => d.MachineDefautCode)
                .HasConstraintName("FK__Plan_Fabr__Machi__11158940");

            entity.HasOne(d => d.ModeleSource).WithMany(p => p.PlanFabricationEntetes)
                .HasForeignKey(d => d.ModeleSourceId)
                .HasConstraintName("FK__Plan_Fabr__Model__0B5CAFEA");

            entity.HasOne(d => d.OperationCodeNavigation).WithMany(p => p.PlanFabricationEntetes)
                .HasForeignKey(d => d.OperationCode)
                .HasConstraintName("FK__Plan_Fabr__Opera__0E391C95");
        });

        modelBuilder.Entity<PlanFabricationLigne>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Fab__3214EC072709E411");

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
                .HasConstraintName("FK__Plan_Fabr__Instr__251C81ED");

            entity.HasOne(d => d.ModeleLigneSource).WithMany(p => p.PlanFabricationLignes)
                .HasForeignKey(d => d.ModeleLigneSourceId)
                .HasConstraintName("FK__Plan_Fabr__Model__214BF109");

            entity.HasOne(d => d.MoyenControle).WithMany(p => p.PlanFabricationLignes)
                .HasForeignKey(d => d.MoyenControleId)
                .HasConstraintName("FK__Plan_Fabr__Moyen__27F8EE98");

            entity.HasOne(d => d.Periodicite).WithMany(p => p.PlanFabricationLignes)
                .HasForeignKey(d => d.PeriodiciteId)
                .HasConstraintName("FK__Plan_Fabr__Perio__2610A626");

            entity.HasOne(d => d.PlanEntete).WithMany(p => p.PlanFabricationLignes)
                .HasForeignKey(d => d.PlanEnteteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_Fabr__PlanE__1F63A897");

            entity.HasOne(d => d.Section).WithMany(p => p.PlanFabricationLignes)
                .HasForeignKey(d => d.SectionId)
                .HasConstraintName("FK__Plan_Fabr__Secti__2057CCD0");

            entity.HasOne(d => d.TypeCaracteristique).WithMany(p => p.PlanFabricationLignes)
                .HasForeignKey(d => d.TypeCaracteristiqueId)
                .HasConstraintName("FK__Plan_Fabr__TypeC__2334397B");

            entity.HasOne(d => d.TypeControle).WithMany(p => p.PlanFabricationLignes)
                .HasForeignKey(d => d.TypeControleId)
                .HasConstraintName("FK__Plan_Fabr__TypeC__24285DB4");
        });

        modelBuilder.Entity<PlanFabricationSection>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Fab__3214EC0722178ACC");

            entity.ToTable("Plan_Fabrication_Section");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.LibelleSection)
                .HasMaxLength(300)
                .IsUnicode(false);

            entity.HasOne(d => d.ModeleSection).WithMany(p => p.PlanFabricationSections)
                .HasForeignKey(d => d.ModeleSectionId)
                .HasConstraintName("FK__Plan_Fabr__Model__17C286CF");

            entity.HasOne(d => d.Periodicite).WithMany(p => p.PlanFabricationSections)
                .HasForeignKey(d => d.PeriodiciteId)
                .HasConstraintName("FK__Plan_Fabr__Perio__1A9EF37A");

            entity.HasOne(d => d.PlanEntete).WithMany(p => p.PlanFabricationSections)
                .HasForeignKey(d => d.PlanEnteteId)
                .HasConstraintName("FK__Plan_Fabr__PlanE__16CE6296");

            entity.HasOne(d => d.RegleEchantillonnage).WithMany(p => p.PlanFabricationSections)
                .HasForeignKey(d => d.RegleEchantillonnageId)
                .HasConstraintName("FK__Plan_Fabr__Regle__1B9317B3");

            entity.HasOne(d => d.TypeSection).WithMany(p => p.PlanFabricationSections)
                .HasForeignKey(d => d.TypeSectionId)
                .HasConstraintName("FK__Plan_Fabr__TypeS__19AACF41");
        });

        modelBuilder.Entity<PlanNonConformiteEntete>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Non__3214EC074C16FBDA");

            entity.ToTable("Plan_NonConformite_Entete");

            entity.HasIndex(e => new { e.PosteCode, e.Version }, "UQ__Plan_Non__03CB8A1429510CEA").IsUnique();

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

            entity.HasOne(d => d.PosteCodeNavigation).WithMany(p => p.PlanNonConformiteEntetes)
                .HasForeignKey(d => d.PosteCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_NonC__Poste__6D9742D9");
        });

        modelBuilder.Entity<PlanNonConformiteLigne>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Non__3214EC0721E0217D");

            entity.ToTable("Plan_NonConformite_Ligne");

            entity.HasIndex(e => new { e.PlanNcenteteId, e.OrdreAffiche }, "UQ__Plan_Non__90C61738B86063D8").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.MachineCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.PlanNcenteteId).HasColumnName("PlanNCEnteteId");

            entity.HasOne(d => d.MachineCodeNavigation).WithMany(p => p.PlanNonConformiteLignes)
                .HasForeignKey(d => d.MachineCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_NonC__Machi__7720AD13");

            entity.HasOne(d => d.PlanNcentete).WithMany(p => p.PlanNonConformiteLignes)
                .HasForeignKey(d => d.PlanNcenteteId)
                .HasConstraintName("FK__Plan_NonC__PlanN__762C88DA");

            entity.HasOne(d => d.RisqueDefaut).WithMany(p => p.PlanNonConformiteLignes)
                .HasForeignKey(d => d.RisqueDefautId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_NonC__Risqu__7814D14C");
        });

        modelBuilder.Entity<PlanProduitFiniEntete>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Pro__3214EC07500C2B52");

            entity.ToTable("Plan_ProduitFini_Entete");

            entity.HasIndex(e => new { e.FamilleProduitFiniCode, e.Version }, "UQ__Plan_Pro__98E0A08269952EA9").IsUnique();

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
                .HasDefaultValue("BROUILLON");

            entity.HasOne(d => d.FamilleProduitFiniCodeNavigation).WithMany(p => p.PlanProduitFiniEntetes)
                .HasForeignKey(d => d.FamilleProduitFiniCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_Prod__Famil__50FB042B");

            entity.HasOne(d => d.Formulaire).WithMany(p => p.PlanProduitFiniEntetes)
                .HasForeignKey(d => d.FormulaireId)
                .HasConstraintName("FK__Plan_Prod__Formu__54CB950F");
        });

        modelBuilder.Entity<PlanProduitFiniLigne>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Pro__3214EC0702AA48F9");

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
                .HasConstraintName("FK__Plan_Prod__Defau__66EA454A");

            entity.HasOne(d => d.InstrumentCodeNavigation).WithMany(p => p.PlanProduitFiniLignes)
                .HasForeignKey(d => d.InstrumentCode)
                .HasConstraintName("FK__Plan_Prod__Instr__65F62111");

            entity.HasOne(d => d.MoyenControle).WithMany(p => p.PlanProduitFiniLignes)
                .HasForeignKey(d => d.MoyenControleId)
                .HasConstraintName("FK__Plan_Prod__Moyen__67DE6983");

            entity.HasOne(d => d.PlanEntete).WithMany(p => p.PlanProduitFiniLignes)
                .HasForeignKey(d => d.PlanEnteteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_Prod__PlanE__61316BF4");

            entity.HasOne(d => d.Section).WithMany(p => p.PlanProduitFiniLignes)
                .HasForeignKey(d => d.SectionId)
                .HasConstraintName("FK__Plan_Prod__Secti__6225902D");

            entity.HasOne(d => d.TypeCaracteristique).WithMany(p => p.PlanProduitFiniLignes)
                .HasForeignKey(d => d.TypeCaracteristiqueId)
                .HasConstraintName("FK__Plan_Prod__TypeC__640DD89F");

            entity.HasOne(d => d.TypeControle).WithMany(p => p.PlanProduitFiniLignes)
                .HasForeignKey(d => d.TypeControleId)
                .HasConstraintName("FK__Plan_Prod__TypeC__6501FCD8");
        });

        modelBuilder.Entity<PlanProduitFiniSection>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Pro__3214EC0748EA727E");

            entity.ToTable("Plan_ProduitFini_Section");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.LibelleSection)
                .HasMaxLength(300)
                .IsUnicode(false);

            entity.HasOne(d => d.Periodicite).WithMany(p => p.PlanProduitFiniSections)
                .HasForeignKey(d => d.PeriodiciteId)
                .HasConstraintName("FK__Plan_Prod__Perio__5C6CB6D7");

            entity.HasOne(d => d.PlanEntete).WithMany(p => p.PlanProduitFiniSections)
                .HasForeignKey(d => d.PlanEnteteId)
                .HasConstraintName("FK__Plan_Prod__PlanE__59904A2C");

            entity.HasOne(d => d.RegleEchantillonnage).WithMany(p => p.PlanProduitFiniSections)
                .HasForeignKey(d => d.RegleEchantillonnageId)
                .HasConstraintName("FK__Plan_Prod__Regle__5D60DB10");

            entity.HasOne(d => d.TypeSection).WithMany(p => p.PlanProduitFiniSections)
                .HasForeignKey(d => d.TypeSectionId)
                .HasConstraintName("FK__Plan_Prod__TypeS__5B78929E");
        });

        modelBuilder.Entity<PlanVerifMachineEcheance>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Ver__3214EC073CB5FF05");

            entity.ToTable("Plan_VerifMachine_Echeance");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");

            entity.HasOne(d => d.Periodicite).WithMany(p => p.PlanVerifMachineEcheances)
                .HasForeignKey(d => d.PeriodiciteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_Veri__Perio__11D4A34F");

            entity.HasOne(d => d.PlanLigne).WithMany(p => p.PlanVerifMachineEcheances)
                .HasForeignKey(d => d.PlanLigneId)
                .HasConstraintName("FK__Plan_Veri__PlanL__10E07F16");

            entity.HasOne(d => d.RefMoyenDetection).WithMany(p => p.PlanVerifMachineEcheances)
                .HasForeignKey(d => d.RefMoyenDetectionId)
                .HasConstraintName("FK__Plan_Veri__RefMo__12C8C788");
        });

        modelBuilder.Entity<PlanVerifMachineEntete>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Ver__3214EC0773FB778C");

            entity.ToTable("Plan_VerifMachine_Entete");

            entity.HasIndex(e => new { e.MachineCode, e.Version }, "UQ__Plan_Ver__9B71F5AB5E806B88").IsUnique();

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
            entity.Property(e => e.Version).HasDefaultValue(0);

            entity.HasOne(d => d.Formulaire).WithMany(p => p.PlanVerifMachineEntetes)
                .HasForeignKey(d => d.FormulaireId)
                .HasConstraintName("FK__Plan_Veri__Formu__019E3B86");

            entity.HasOne(d => d.MachineCodeNavigation).WithMany(p => p.PlanVerifMachineEntetes)
                .HasForeignKey(d => d.MachineCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_Veri__Machi__7CD98669");
        });

        modelBuilder.Entity<PlanVerifMachineFamille>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Ver__3214EC0795326BFE");

            entity.ToTable("Plan_VerifMachine_Famille");

            entity.HasIndex(e => new { e.PlanEnteteId, e.RefFamilleCorpsId }, "UQ__Plan_Ver__7457AEA4BC1B65DA").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");

            entity.HasOne(d => d.PlanEntete).WithMany(p => p.PlanVerifMachineFamilles)
                .HasForeignKey(d => d.PlanEnteteId)
                .HasConstraintName("FK__Plan_Veri__PlanE__0662F0A3");

            entity.HasOne(d => d.RefFamilleCorps).WithMany(p => p.PlanVerifMachineFamilles)
                .HasForeignKey(d => d.RefFamilleCorpsId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_Veri__RefFa__075714DC");
        });

        modelBuilder.Entity<PlanVerifMachineLigne>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Ver__3214EC070C12D02A");

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
                .HasConstraintName("FK__Plan_Veri__PlanE__0B27A5C0");
        });

        modelBuilder.Entity<PlanVerifMachineMatricePiece>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Ver__3214EC07F1D37255");

            entity.ToTable("Plan_VerifMachine_MatricePiece");

            entity.HasIndex(e => new { e.EcheanceId, e.FamilleId, e.RoleVerif }, "UQ__Plan_Ver__779871C4CD4F1467").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.RoleVerif)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.Echeance).WithMany(p => p.PlanVerifMachineMatricePieces)
                .HasForeignKey(d => d.EcheanceId)
                .HasConstraintName("FK__Plan_Veri__Echea__1881A0DE");

            entity.HasOne(d => d.Famille).WithMany(p => p.PlanVerifMachineMatricePieces)
                .HasForeignKey(d => d.FamilleId)
                .HasConstraintName("FK__Plan_Veri__Famil__1975C517");

            entity.HasOne(d => d.PieceRef).WithMany(p => p.PlanVerifMachineMatricePieces)
                .HasForeignKey(d => d.PieceRefId)
                .HasConstraintName("FK__Plan_Veri__Piece__1B5E0D89");
        });

        modelBuilder.Entity<PosteTravail>(entity =>
        {
            entity.HasKey(e => e.CodePoste).HasName("PK__PosteTra__4045446B18A90EB4");

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
                        j.HasKey("CodePoste", "CodeMachine").HasName("PK__PosteTra__A548230B06667CEB");
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
            entity.HasKey(e => e.CodeArticle).HasName("PK__ProduitF__32384FB03DF653BF");

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
            entity.HasKey(e => e.Id).HasName("PK__Ref_Fami__3214EC078BA9C65E");

            entity.ToTable("Ref_FamilleCorps", tb => tb.HasTrigger("trg_no_del_Ref_FamilleCorps"));

            entity.HasIndex(e => e.Code, "UQ__Ref_Fami__A25C5AA78B05C751").IsUnique();

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
            entity.HasKey(e => e.Id).HasName("PK__Ref_Form__3214EC071C342F61");

            entity.ToTable("Ref_Formulaire");

            entity.HasIndex(e => new { e.CodeReference, e.Version }, "UQ__Ref_Form__4F92504B2A5E6711").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CodeReference)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.CreeLe)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Designation)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Statut)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("ACTIF");
        });

        modelBuilder.Entity<RefMoyenDetection>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Ref_Moye__3214EC078EBFF1C1");

            entity.ToTable("Ref_MoyenDetection", tb => tb.HasTrigger("trg_no_del_Ref_MoyenDetection"));

            entity.HasIndex(e => e.Code, "UQ__Ref_Moye__A25C5AA72749D1CC").IsUnique();

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
            entity.HasKey(e => e.Id).HasName("PK__Ref_Regl__3214EC0712A1DBDB");

            entity.ToTable("Ref_RegleEchantillonnage", tb => tb.HasTrigger("trg_no_del_Ref_RegleEchantillonnage"));

            entity.HasIndex(e => e.Code, "UQ__Ref_Regl__A25C5AA75A0D5FEF").IsUnique();

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
            entity.HasKey(e => e.Id).HasName("PK__RefreshT__3214EC0761220DB5");

            entity.HasIndex(e => e.Token, "UQ__RefreshT__1EB4F81735E4DD49").IsUnique();

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
            entity.HasKey(e => e.Id).HasName("PK__RisqueDe__3214EC07FEF91456");

            entity.ToTable("RisqueDefaut", tb => tb.HasTrigger("trg_no_del_RisqueDefaut"));

            entity.HasIndex(e => e.CodeDefaut, "UQ__RisqueDe__2EF87343F5C2E973").IsUnique();

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
            entity.HasKey(e => e.NumeroBl).HasName("PK__SDELIVER__C664DCCD917AD56F");

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
            entity.HasKey(e => e.Id).HasName("PK__TypeCara__3214EC07CAB88FC6");

            entity.ToTable("TypeCaracteristique", tb => tb.HasTrigger("trg_no_del_TypeCaracteristique"));

            entity.HasIndex(e => e.Code, "UQ__TypeCara__A25C5AA7D49526E3").IsUnique();

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
            entity.HasKey(e => e.Id).HasName("PK__TypeCont__3214EC07DEAA9218");

            entity.ToTable("TypeControle", tb => tb.HasTrigger("trg_no_del_TypeControle"));

            entity.HasIndex(e => e.Code, "UQ__TypeCont__A25C5AA78FDE064E").IsUnique();

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
            entity.HasKey(e => e.Code).HasName("PK__TypeRobi__A25C5AA64D6D0900");

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
            entity.HasKey(e => e.Id).HasName("PK__TypeSect__3214EC0719879A93");

            entity.ToTable("TypeSection", tb => tb.HasTrigger("trg_no_del_TypeSection"));

            entity.HasIndex(e => e.Code, "UQ__TypeSect__A25C5AA7E9F8E875").IsUnique();

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
            entity.HasKey(e => e.Id).HasName("PK__Utilisat__3214EC07EF88B330");

            entity.ToTable("UtilisateursApp");

            entity.HasIndex(e => e.Matricule, "IX_UtilisateursApp_Matricule");

            entity.HasIndex(e => e.Matricule, "UQ__Utilisat__0FB9FB4399CF16D8").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__Utilisat__A9D10534A46C0883").IsUnique();

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
