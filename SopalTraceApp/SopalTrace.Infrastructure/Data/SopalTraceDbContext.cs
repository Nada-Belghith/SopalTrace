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

    public virtual DbSet<DocumentEntete> DocumentEntetes { get; set; }

    public virtual DbSet<DocumentLigne> DocumentLignes { get; set; }

    public virtual DbSet<DocumentLigneExtraColonne> DocumentLigneExtraColonnes { get; set; }

    public virtual DbSet<DocumentSection> DocumentSections { get; set; }

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

    public virtual DbSet<ModeleFabricationLigneExtraColonne> ModeleFabricationLigneExtraColonnes { get; set; }

    public virtual DbSet<ModeleFabricationSection> ModeleFabricationSections { get; set; }

    public virtual DbSet<MoyenControle> MoyenControles { get; set; }

    public virtual DbSet<NatureArticle> NatureArticles { get; set; }

    public virtual DbSet<NatureArticleOperation> NatureArticleOperations { get; set; }

    public virtual DbSet<Nqa> Nqas { get; set; }

    public virtual DbSet<Operation> Operations { get; set; }

    public virtual DbSet<OutilControle> OutilControles { get; set; }

    public virtual DbSet<Periodicite> Periodicites { get; set; }

    public virtual DbSet<PieceReference> PieceReferences { get; set; }

    public virtual DbSet<PlanEchantillonnageEntete> PlanEchantillonnageEntetes { get; set; }

    public virtual DbSet<PlanEchantillonnageRegle> PlanEchantillonnageRegles { get; set; }

    public virtual DbSet<PlanFabricationEntete> PlanFabricationEntetes { get; set; }

    public virtual DbSet<PlanFabricationLigne> PlanFabricationLignes { get; set; }

    public virtual DbSet<PlanFabricationLigneExtraColonne> PlanFabricationLigneExtraColonnes { get; set; }

    public virtual DbSet<PlanFabricationSection> PlanFabricationSections { get; set; }

    public virtual DbSet<PlanVerifMachineEcheance> PlanVerifMachineEcheances { get; set; }

    public virtual DbSet<PlanVerifMachineEntete> PlanVerifMachineEntetes { get; set; }

    public virtual DbSet<PlanVerifMachineFamille> PlanVerifMachineFamilles { get; set; }

    public virtual DbSet<PlanVerifMachineLigne> PlanVerifMachineLignes { get; set; }

    public virtual DbSet<PlanVerifMachineLigneExtraColonne> PlanVerifMachineLigneExtraColonnes { get; set; }

    public virtual DbSet<PlanVerifMachineMatricePiece> PlanVerifMachineMatricePieces { get; set; }

    public virtual DbSet<PosteTravail> PosteTravails { get; set; }

    public virtual DbSet<ProduitFini> ProduitFinis { get; set; }

    public virtual DbSet<RefCaracteristique> RefCaracteristiques { get; set; }

    public virtual DbSet<RefFamilleCorp> RefFamilleCorps { get; set; }

    public virtual DbSet<RefFormulaire> RefFormulaires { get; set; }

    public virtual DbSet<RefFormulaireColonneDef> RefFormulaireColonneDefs { get; set; }

    public virtual DbSet<RefFormulaireEquipe> RefFormulaireEquipes { get; set; }

    public virtual DbSet<RefMoyenDetection> RefMoyenDetections { get; set; }

    public virtual DbSet<RefRegleEchantillonnage> RefRegleEchantillonnages { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<RisqueDefaut> RisqueDefauts { get; set; }

    public virtual DbSet<Sdelivery> Sdeliveries { get; set; }

    public virtual DbSet<TypeCaracteristique> TypeCaracteristiques { get; set; }

    public virtual DbSet<TypeControle> TypeControles { get; set; }

    public virtual DbSet<TypeDocument> TypeDocuments { get; set; }

    public virtual DbSet<TypeRobinet> TypeRobinets { get; set; }

    public virtual DbSet<TypeSection> TypeSections { get; set; }

    public virtual DbSet<UtilisateursApp> UtilisateursApps { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Article>(entity =>
        {
            entity.HasKey(e => e.CodeArticle).HasName("PK__Article__32384FB039BBC637");

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
                .HasConstraintName("FK__Article__NatureA__2645B050");
        });

        modelBuilder.Entity<Atextra>(entity =>
        {
            entity.HasKey(e => new { e.Codfic0, e.Zone0, e.Ident10, e.Langue0 }).HasName("PK__ATEXTRA__4F21B2DBE1BFD227");

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
            entity.HasKey(e => e.Usr0).HasName("PK__AUTILIS__0812AE699A93E4D1");

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
            entity.HasKey(e => new { e.ArticleParent, e.CodeComposant, e.CodeAlternative }).HasName("PK__BOMD_Nom__2710E851E8FF7002");

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
                .HasConstraintName("FK__BOMD_Nome__Artic__31B762FC");

            entity.HasOne(d => d.CodeComposantNavigation).WithMany(p => p.BomdNomenclatureCodeComposantNavigations)
                .HasForeignKey(d => d.CodeComposant)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__BOMD_Nome__CodeC__32AB8735");
        });

        modelBuilder.Entity<Defautheque>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Defauthe__3214EC07DB46C8F6");

            entity.ToTable("Defautheque", tb => tb.HasTrigger("trg_no_del_Defautheque"));

            entity.HasIndex(e => e.Code, "UQ__Defauthe__A25C5AA71579F75E").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.Code)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Description)
                .HasMaxLength(200)
                .IsUnicode(false);
        });

        modelBuilder.Entity<DocumentEntete>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Document__3214EC0774108556");

            entity.ToTable("Document_Entete");

            entity.HasIndex(e => e.Statut, "IX_Document_Entete_Statut");

            entity.HasIndex(e => e.TypeDocumentCode, "IX_Document_Entete_Type");

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
            entity.Property(e => e.Libre1).HasMaxLength(255);
            entity.Property(e => e.Libre2).HasMaxLength(255);
            entity.Property(e => e.Libre3).HasMaxLength(255);
            entity.Property(e => e.ModifieLe).HasColumnType("datetime");
            entity.Property(e => e.ModifiePar)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.NatureArticleCode)
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
            entity.Property(e => e.TypeDocumentCode)
                .HasMaxLength(30)
                .IsUnicode(false);

            entity.HasOne(d => d.FamilleProduitFiniCodeNavigation).WithMany(p => p.DocumentEntetes)
                .HasForeignKey(d => d.FamilleProduitFiniCode)
                .HasConstraintName("FK__Document___Famil__4C364F0E");

            entity.HasOne(d => d.Formulaire).WithMany(p => p.DocumentEntetes)
                .HasForeignKey(d => d.FormulaireId)
                .HasConstraintName("FK__Document___Formu__4959E263");

            entity.HasOne(d => d.NatureArticleCodeNavigation).WithMany(p => p.DocumentEntetes)
                .HasForeignKey(d => d.NatureArticleCode)
                .HasConstraintName("FK__Document___Natur__4B422AD5");

            entity.HasOne(d => d.OperationCodeNavigation).WithMany(p => p.DocumentEntetes)
                .HasForeignKey(d => d.OperationCode)
                .HasConstraintName("FK__Document___Opera__4865BE2A");

            entity.HasOne(d => d.PosteCodeNavigation).WithMany(p => p.DocumentEntetes)
                .HasForeignKey(d => d.PosteCode)
                .HasConstraintName("FK__Document___Poste__4D2A7347");

            entity.HasOne(d => d.TypeDocumentCodeNavigation).WithMany(p => p.DocumentEntetes)
                .HasForeignKey(d => d.TypeDocumentCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Document___TypeD__44952D46");
        });

        modelBuilder.Entity<DocumentLigne>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Document__3214EC075D606376");

            entity.ToTable("Document_Ligne");

            entity.HasIndex(e => e.CaracteristiqueId, "IX_Document_Ligne_Caracteristique");

            entity.HasIndex(e => e.EnteteId, "IX_Document_Ligne_Entete");

            entity.HasIndex(e => e.SectionId, "IX_Document_Ligne_Section");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.ImageBase64).IsUnicode(false);
            entity.Property(e => e.InstrumentCode)
                .HasMaxLength(40)
                .IsUnicode(false);
            entity.Property(e => e.LibelleAffiche)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.Libre1).HasMaxLength(255);
            entity.Property(e => e.Libre2).HasMaxLength(255);
            entity.Property(e => e.Libre3).HasMaxLength(255);
            entity.Property(e => e.Libre4).HasMaxLength(255);
            entity.Property(e => e.Libre5).HasMaxLength(255);
            entity.Property(e => e.LimiteSpecTexte)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.MachineCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.MachineCodeCtrlPoste)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.MoyenTexteLibre)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.RefPlanProduit)
                .HasMaxLength(60)
                .IsUnicode(false);

            entity.HasOne(d => d.Caracteristique).WithMany(p => p.DocumentLignes)
                .HasForeignKey(d => d.CaracteristiqueId)
                .HasConstraintName("FK__Document___Carac__5C6CB6D7");

            entity.HasOne(d => d.Defautheque).WithMany(p => p.DocumentLignes)
                .HasForeignKey(d => d.DefauthequeId)
                .HasConstraintName("FK__Document___Defau__6501FCD8");

            entity.HasOne(d => d.Entete).WithMany(p => p.DocumentLignes)
                .HasForeignKey(d => d.EnteteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Document___Entet__59904A2C");

            entity.HasOne(d => d.InstrumentCodeNavigation).WithMany(p => p.DocumentLignes)
                .HasForeignKey(d => d.InstrumentCode)
                .HasConstraintName("FK__Document___Instr__603D47BB");

            entity.HasOne(d => d.MachineCodeNavigation).WithMany(p => p.DocumentLigneMachineCodeNavigations)
                .HasForeignKey(d => d.MachineCode)
                .HasConstraintName("FK__Document___Machi__6319B466");

            entity.HasOne(d => d.MachineCodeCtrlPosteNavigation).WithMany(p => p.DocumentLigneMachineCodeCtrlPosteNavigations)
                .HasForeignKey(d => d.MachineCodeCtrlPoste)
                .HasConstraintName("FK__Document___Machi__65F62111");

            entity.HasOne(d => d.MoyenControle).WithMany(p => p.DocumentLignes)
                .HasForeignKey(d => d.MoyenControleId)
                .HasConstraintName("FK__Document___Moyen__5F492382");

            entity.HasOne(d => d.Periodicite).WithMany(p => p.DocumentLignes)
                .HasForeignKey(d => d.PeriodiciteId)
                .HasConstraintName("FK__Document___Perio__61316BF4");

            entity.HasOne(d => d.RisqueDefaut).WithMany(p => p.DocumentLignes)
                .HasForeignKey(d => d.RisqueDefautId)
                .HasConstraintName("FK__Document___Risqu__66EA454A");

            entity.HasOne(d => d.Section).WithMany(p => p.DocumentLignes)
                .HasForeignKey(d => d.SectionId)
                .HasConstraintName("FK__Document___Secti__5A846E65");

            entity.HasOne(d => d.TypeCaracteristique).WithMany(p => p.DocumentLignes)
                .HasForeignKey(d => d.TypeCaracteristiqueId)
                .HasConstraintName("FK__Document___TypeC__5D60DB10");

            entity.HasOne(d => d.TypeControle).WithMany(p => p.DocumentLignes)
                .HasForeignKey(d => d.TypeControleId)
                .HasConstraintName("FK__Document___TypeC__5E54FF49");
        });

        modelBuilder.Entity<DocumentLigneExtraColonne>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Document__3214EC078B7CC919");

            entity.ToTable("Document_Ligne_ExtraColonne");

            entity.HasIndex(e => new { e.LigneId, e.CleColonne }, "UQ__Document__B2068CF3F0BA52F8").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CleColonne)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.ValeurColonne).HasMaxLength(500);

            entity.HasOne(d => d.Ligne).WithMany(p => p.DocumentLigneExtraColonnes)
                .HasForeignKey(d => d.LigneId)
                .HasConstraintName("FK__Document___Ligne__6CA31EA0");
        });

        modelBuilder.Entity<DocumentSection>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Document__3214EC07D6E2B7DC");

            entity.ToTable("Document_Section");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.LibelleSection)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.NormeReference)
                .HasMaxLength(40)
                .IsUnicode(false);

            entity.HasOne(d => d.Entete).WithMany(p => p.DocumentSections)
                .HasForeignKey(d => d.EnteteId)
                .HasConstraintName("FK__Document___Entet__50FB042B");

            entity.HasOne(d => d.Nqa).WithMany(p => p.DocumentSections)
                .HasForeignKey(d => d.NqaId)
                .HasConstraintName("FK__Document___NqaId__55BFB948");

            entity.HasOne(d => d.Periodicite).WithMany(p => p.DocumentSections)
                .HasForeignKey(d => d.PeriodiciteId)
                .HasConstraintName("FK__Document___Perio__53D770D6");

            entity.HasOne(d => d.RegleEchantillonnage).WithMany(p => p.DocumentSections)
                .HasForeignKey(d => d.RegleEchantillonnageId)
                .HasConstraintName("FK__Document___Regle__54CB950F");

            entity.HasOne(d => d.TypeSection).WithMany(p => p.DocumentSections)
                .HasForeignKey(d => d.TypeSectionId)
                .HasConstraintName("FK__Document___TypeS__52E34C9D");
        });

        modelBuilder.Entity<ExecControleOf>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Exec_Con__3214EC0717D3EE66");

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
                .HasConstraintName("FK__Exec_Cont__Machi__3BCADD1B");

            entity.HasOne(d => d.MachineCodePrevuNavigation).WithMany(p => p.ExecControleOfMachineCodePrevuNavigations)
                .HasForeignKey(d => d.MachineCodePrevu)
                .HasConstraintName("FK__Exec_Cont__Machi__39E294A9");

            entity.HasOne(d => d.NumeroOfNavigation).WithMany(p => p.ExecControleOfs)
                .HasForeignKey(d => d.NumeroOf)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Exec_Cont__Numer__37FA4C37");

            entity.HasOne(d => d.OperationCodeNavigation).WithMany(p => p.ExecControleOfs)
                .HasForeignKey(d => d.OperationCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Exec_Cont__Opera__38EE7070");

            entity.HasOne(d => d.PosteCodeNavigation).WithMany(p => p.ExecControleOfPosteCodeNavigations)
                .HasForeignKey(d => d.PosteCode)
                .HasConstraintName("FK__Exec_Cont__Poste__3CBF0154");

            entity.HasOne(d => d.PosteCodePrevuNavigation).WithMany(p => p.ExecControleOfPosteCodePrevuNavigations)
                .HasForeignKey(d => d.PosteCodePrevu)
                .HasConstraintName("FK__Exec_Cont__Poste__3AD6B8E2");
        });

        modelBuilder.Entity<ExecControleTranche>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Exec_Con__3214EC07D635DB50");

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
                .HasConstraintName("FK__Exec_Cont__ExecC__4B0D20AB");
        });

        modelBuilder.Entity<ExecPieceType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Exec_Pie__3214EC07A251FFCA");

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
                .HasConstraintName("FK__Exec_Piec__ExecC__45544755");
        });

        modelBuilder.Entity<ExecPrelevement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Exec_Pre__3214EC07873D7BDF");

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
                .HasConstraintName("FK__Exec_Prel__ExecC__4FD1D5C8");
        });

        modelBuilder.Entity<ExecPrelevementLigne>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Exec_Pre__3214EC0706FA88D1");

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
                .HasConstraintName("FK__Exec_Prel__Prele__54968AE5");
        });

        modelBuilder.Entity<FamilleProduitFini>(entity =>
        {
            entity.HasKey(e => e.Code).HasName("PK__FamilleP__A25C5AA6F63701A7");

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
            entity.HasKey(e => e.CodeInstrument).HasName("PK__Instrume__E6E43505429ACE6A");

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
            entity.HasKey(e => e.Id).HasName("PK__JournalC__3214EC07C3EF0F20");

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
            entity.HasKey(e => e.CodeMachine).HasName("PK__Machine__50D6760F5EB5F658");

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
                .HasConstraintName("FK__Machine__Operati__46B27FE2");

            entity.HasMany(d => d.RefFamilleCorps).WithMany(p => p.MachineCodes)
                .UsingEntity<Dictionary<string, object>>(
                    "MachineFamilleCorp",
                    r => r.HasOne<RefFamilleCorp>().WithMany()
                        .HasForeignKey("RefFamilleCorpsId")
                        .HasConstraintName("FK__Machine_F__RefFa__4E53A1AA"),
                    l => l.HasOne<Machine>().WithMany()
                        .HasForeignKey("MachineCode")
                        .HasConstraintName("FK__Machine_F__Machi__4D5F7D71"),
                    j =>
                    {
                        j.HasKey("MachineCode", "RefFamilleCorpsId").HasName("PK__Machine___74270A8A33A6F39F");
                        j.ToTable("Machine_FamilleCorps");
                        j.IndexerProperty<string>("MachineCode")
                            .HasMaxLength(30)
                            .IsUnicode(false);
                    });
        });

        modelBuilder.Entity<MagExpeditionBl>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Mag_Expe__3214EC07631377C2");

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
                .HasConstraintName("FK__Mag_Exped__Numer__2B947552");
        });

        modelBuilder.Entity<MagExpeditionBlScanOf>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Mag_Expe__3214EC07FCBC783D");

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
                .HasConstraintName("FK__Mag_Exped__Exped__324172E1");

            entity.HasOne(d => d.NumeroOfscanneNavigation).WithMany(p => p.MagExpeditionBlScanOfs)
                .HasForeignKey(d => d.NumeroOfscanne)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Mag_Exped__Numer__3335971A");
        });

        modelBuilder.Entity<MagPreparationOf>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Mag_Prep__3214EC070A34EAEF");

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
                .HasConstraintName("FK__Mag_Prepa__Numer__1975C517");
        });

        modelBuilder.Entity<MagPreparationOfLot>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Mag_Prep__3214EC070EA20B33");

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
                .HasConstraintName("FK__Mag_Prepa__CodeA__2116E6DF");

            entity.HasOne(d => d.PreparationOf).WithMany(p => p.MagPreparationOfLots)
                .HasForeignKey(d => d.PreparationOfid)
                .HasConstraintName("FK__Mag_Prepa__Prepa__2022C2A6");
        });

        modelBuilder.Entity<MagQuickControlRapport>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Mag_Quic__3214EC07D190BEC1");

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
                .HasConstraintName("FK__Mag_Quick__CodeA__26CFC035");

            entity.HasOne(d => d.NumeroOfNavigation).WithMany(p => p.MagQuickControlRapports)
                .HasForeignKey(d => d.NumeroOf)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Mag_Quick__Numer__25DB9BFC");
        });

        modelBuilder.Entity<MfgheadOrdreFabrication>(entity =>
        {
            entity.HasKey(e => e.NumeroOf).HasName("PK__MFGHEAD___C6A65F30BA379730");

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
                .HasConstraintName("FK__MFGHEAD_O__CodeA__3A4CA8FD");
        });

        modelBuilder.Entity<MfgmatBesoinOf>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__MFGMAT_B__3214EC0744368FE8");

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
                .HasConstraintName("FK__MFGMAT_Be__CodeA__40F9A68C");

            entity.HasOne(d => d.NumeroOfNavigation).WithMany(p => p.MfgmatBesoinOfs)
                .HasForeignKey(d => d.NumeroOf)
                .HasConstraintName("FK__MFGMAT_Be__Numer__40058253");
        });

        modelBuilder.Entity<ModeleFabricationEntete>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Modele_F__3214EC07C2689747");

            entity.ToTable("Modele_Fabrication_Entete");

            entity.HasIndex(e => new { e.Code, e.Libelle, e.Version }, "UQ__Modele_F__EE6B9887E66424FC").IsUnique();

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
                .HasConstraintName("FK__Modele_Fa__Famil__0697FACD");

            entity.HasOne(d => d.Formulaire).WithMany(p => p.ModeleFabricationEntetes)
                .HasForeignKey(d => d.FormulaireId)
                .HasConstraintName("FK__Modele_Fa__Formu__078C1F06");

            entity.HasOne(d => d.NatureArticleCodeNavigation).WithMany(p => p.ModeleFabricationEntetes)
                .HasForeignKey(d => d.NatureArticleCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Modele_Fa__Natur__04AFB25B");

            entity.HasOne(d => d.OperationCodeNavigation).WithMany(p => p.ModeleFabricationEntetes)
                .HasForeignKey(d => d.OperationCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Modele_Fa__Opera__05A3D694");
        });

        modelBuilder.Entity<ModeleFabricationLigne>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Modele_F__3214EC07F757F1AE");

            entity.ToTable("Modele_Fabrication_Ligne");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.ImageBase64).IsUnicode(false);
            entity.Property(e => e.InstrumentCode)
                .HasMaxLength(40)
                .IsUnicode(false);
            entity.Property(e => e.LibelleAffiche)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Libre1).HasMaxLength(255);
            entity.Property(e => e.Libre2).HasMaxLength(255);
            entity.Property(e => e.Libre3).HasMaxLength(255);
            entity.Property(e => e.Libre4).HasMaxLength(255);
            entity.Property(e => e.Libre5).HasMaxLength(255);
            entity.Property(e => e.LimiteSpecTexte)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.MoyenTexteLibre)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.Caracteristique).WithMany(p => p.ModeleFabricationLignes)
                .HasForeignKey(d => d.CaracteristiqueId)
                .HasConstraintName("FK__Modele_Fa__Carac__18B6AB08");

            entity.HasOne(d => d.InstrumentCodeNavigation).WithMany(p => p.ModeleFabricationLignes)
                .HasForeignKey(d => d.InstrumentCode)
                .HasConstraintName("FK__Modele_Fa__Instr__1B9317B3");

            entity.HasOne(d => d.MoyenControle).WithMany(p => p.ModeleFabricationLignes)
                .HasForeignKey(d => d.MoyenControleId)
                .HasConstraintName("FK__Modele_Fa__Moyen__1E6F845E");

            entity.HasOne(d => d.Periodicite).WithMany(p => p.ModeleFabricationLignes)
                .HasForeignKey(d => d.PeriodiciteId)
                .HasConstraintName("FK__Modele_Fa__Perio__1C873BEC");

            entity.HasOne(d => d.Section).WithMany(p => p.ModeleFabricationLignes)
                .HasForeignKey(d => d.SectionId)
                .HasConstraintName("FK__Modele_Fa__Secti__16CE6296");

            entity.HasOne(d => d.TypeCaracteristique).WithMany(p => p.ModeleFabricationLignes)
                .HasForeignKey(d => d.TypeCaracteristiqueId)
                .HasConstraintName("FK__Modele_Fa__TypeC__19AACF41");

            entity.HasOne(d => d.TypeControle).WithMany(p => p.ModeleFabricationLignes)
                .HasForeignKey(d => d.TypeControleId)
                .HasConstraintName("FK__Modele_Fa__TypeC__1A9EF37A");
        });

        modelBuilder.Entity<ModeleFabricationLigneExtraColonne>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Modele_F__ExtraCol__3214EC07");

            entity.ToTable("Modele_Fabrication_Ligne_ExtraColonne");

            entity.HasIndex(e => new { e.LigneId, e.CleColonne }, "UQ__Modele_F__ExtraCol_LigneCol").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CleColonne)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.ValeurColonne).HasMaxLength(500);

            entity.HasOne(d => d.Ligne).WithMany(p => p.ModeleFabricationLigneExtraColonnes)
                .HasForeignKey(d => d.LigneId)
                .HasConstraintName("FK__Modele_Fa__Ligne__ExtraCol");
        });

        modelBuilder.Entity<ModeleFabricationSection>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Modele_F__3214EC077251BEFA");

            entity.ToTable("Modele_Fabrication_Section");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.LibelleSection)
                .HasMaxLength(200)
                .IsUnicode(false);

            entity.HasOne(d => d.ModeleEntete).WithMany(p => p.ModeleFabricationSections)
                .HasForeignKey(d => d.ModeleEnteteId)
                .HasConstraintName("FK__Modele_Fa__Model__0F2D40CE");

            entity.HasOne(d => d.Periodicite).WithMany(p => p.ModeleFabricationSections)
                .HasForeignKey(d => d.PeriodiciteId)
                .HasConstraintName("FK__Modele_Fa__Perio__1209AD79");

            entity.HasOne(d => d.RegleEchantillonnage).WithMany(p => p.ModeleFabricationSections)
                .HasForeignKey(d => d.RegleEchantillonnageId)
                .HasConstraintName("FK__Modele_Fa__Regle__12FDD1B2");

            entity.HasOne(d => d.TypeSection).WithMany(p => p.ModeleFabricationSections)
                .HasForeignKey(d => d.TypeSectionId)
                .HasConstraintName("FK__Modele_Fa__TypeS__11158940");
        });

        modelBuilder.Entity<MoyenControle>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__MoyenCon__3214EC07B4DF833F");

            entity.ToTable("MoyenControle", tb => tb.HasTrigger("trg_no_del_MoyenControle"));

            entity.HasIndex(e => e.Code, "UQ__MoyenCon__A25C5AA7ED9B1410").IsUnique();

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
            entity.HasKey(e => e.Code).HasName("PK__NatureAr__A25C5AA665C04541");

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
            entity.HasKey(e => new { e.NatureArticleCode, e.OperationCode }).HasName("PK__NatureAr__6403AE773910B143");

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
            entity.HasKey(e => e.Id).HasName("PK__NQA__3214EC078E76F668");

            entity.ToTable("NQA", tb => tb.HasTrigger("trg_no_del_NQA"));

            entity.HasIndex(e => e.ValeurNqa, "UQ__NQA__1DA3E248B3EB625B").IsUnique();

            entity.Property(e => e.ValeurNqa).HasColumnName("ValeurNQA");
        });

        modelBuilder.Entity<Operation>(entity =>
        {
            entity.HasKey(e => e.Code).HasName("PK__Operatio__A25C5AA6CF85569D");

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
            entity.HasKey(e => e.Id).HasName("PK__OutilCon__3214EC07767E06B5");

            entity.ToTable("OutilControle");

            entity.HasIndex(e => e.Code, "UQ__OutilCon__A25C5AA75A2529F6").IsUnique();

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
                .HasConstraintName("FK__OutilCont__Moyen__6CD828CA");

            entity.HasOne(d => d.PeriodiciteDefaut).WithMany(p => p.OutilControles)
                .HasForeignKey(d => d.PeriodiciteDefautId)
                .HasConstraintName("FK__OutilCont__Perio__6DCC4D03");

            entity.HasOne(d => d.TypeCaracteristique).WithMany(p => p.OutilControles)
                .HasForeignKey(d => d.TypeCaracteristiqueId)
                .HasConstraintName("FK__OutilCont__TypeC__6BE40491");

            entity.HasOne(d => d.TypeControle).WithMany(p => p.OutilControles)
                .HasForeignKey(d => d.TypeControleId)
                .HasConstraintName("FK__OutilCont__TypeC__6AEFE058");
        });

        modelBuilder.Entity<Periodicite>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Periodic__3214EC0758F3B9F1");

            entity.ToTable("Periodicite", tb => tb.HasTrigger("trg_no_del_Periodicite"));

            entity.HasIndex(e => e.Code, "UQ__Periodic__A25C5AA764160E23").IsUnique();

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
            entity.HasKey(e => e.Id).HasName("PK__PieceRef__3214EC07E74A18C0");

            entity.ToTable("PieceReference", tb => tb.HasTrigger("trg_no_del_PieceReference"));

            entity.HasIndex(e => e.Code, "UQ__PieceRef__A25C5AA73405BF18").IsUnique();

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

        modelBuilder.Entity<PlanEchantillonnageEntete>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Ech__3214EC0729F67274");

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
                .HasDefaultValue("BROUILLON");
            entity.Property(e => e.TypePlan)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.Formulaire).WithMany(p => p.PlanEchantillonnageEntetes)
                .HasForeignKey(d => d.FormulaireId)
                .HasConstraintName("FK__Plan_Echa__Formu__76619304");

            entity.HasOne(d => d.Nqa).WithMany(p => p.PlanEchantillonnageEntetes)
                .HasForeignKey(d => d.NqaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_Echa__NqaId__756D6ECB");
        });

        modelBuilder.Entity<PlanEchantillonnageRegle>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Ech__3214EC074735FFE9");

            entity.ToTable("Plan_Echantillonnage_Regle");

            entity.HasIndex(e => new { e.FicheEnteteId, e.LettreCode }, "UQ__Plan_Ech__D6AC40B662F224BD").IsUnique();

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
                .HasConstraintName("FK__Plan_Echa__Fiche__7EF6D905");
        });

        modelBuilder.Entity<PlanFabricationEntete>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Fab__3214EC07F3D3D719");

            entity.ToTable("Plan_Fabrication_Entete");

            entity.HasIndex(e => e.CodeArticleSageVersionne, "IX_PlanFab_CodeArticle");

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
                .HasDefaultValue("BROUILLON");

            entity.HasOne(d => d.Formulaire).WithMany(p => p.PlanFabricationEntetes)
                .HasForeignKey(d => d.FormulaireId)
                .HasConstraintName("FK__Plan_Fabr__Formu__28ED12D1");

            entity.HasOne(d => d.MachineDefautCodeNavigation).WithMany(p => p.PlanFabricationEntetes)
                .HasForeignKey(d => d.MachineDefautCode)
                .HasConstraintName("FK__Plan_Fabr__Machi__27F8EE98");

            entity.HasOne(d => d.ModeleSource).WithMany(p => p.PlanFabricationEntetes)
                .HasForeignKey(d => d.ModeleSourceId)
                .HasConstraintName("FK__Plan_Fabr__Model__2334397B");

            entity.HasOne(d => d.OperationCodeNavigation).WithMany(p => p.PlanFabricationEntetes)
                .HasForeignKey(d => d.OperationCode)
                .HasConstraintName("FK__Plan_Fabr__Opera__251C81ED");
        });

        modelBuilder.Entity<PlanFabricationLigne>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Fab__3214EC071DDBA714");

            entity.ToTable("Plan_Fabrication_Ligne");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.ImageBase64).IsUnicode(false);
            entity.Property(e => e.InstrumentCode)
                .HasMaxLength(40)
                .IsUnicode(false);
            entity.Property(e => e.LibelleAffiche)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Libre1).HasMaxLength(255);
            entity.Property(e => e.Libre2).HasMaxLength(255);
            entity.Property(e => e.Libre3).HasMaxLength(255);
            entity.Property(e => e.Libre4).HasMaxLength(255);
            entity.Property(e => e.Libre5).HasMaxLength(255);
            entity.Property(e => e.LimiteSpecTexte)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.MoyenTexteLibre)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.Caracteristique).WithMany(p => p.PlanFabricationLignes)
                .HasForeignKey(d => d.CaracteristiqueId)
                .HasConstraintName("FK__Plan_Fabr__Carac__3A179ED3");

            entity.HasOne(d => d.InstrumentCodeNavigation).WithMany(p => p.PlanFabricationLignes)
                .HasForeignKey(d => d.InstrumentCode)
                .HasConstraintName("FK__Plan_Fabr__Instr__3CF40B7E");

            entity.HasOne(d => d.ModeleLigneSource).WithMany(p => p.PlanFabricationLignes)
                .HasForeignKey(d => d.ModeleLigneSourceId)
                .HasConstraintName("FK__Plan_Fabr__Model__382F5661");

            entity.HasOne(d => d.MoyenControle).WithMany(p => p.PlanFabricationLignes)
                .HasForeignKey(d => d.MoyenControleId)
                .HasConstraintName("FK__Plan_Fabr__Moyen__3FD07829");

            entity.HasOne(d => d.Periodicite).WithMany(p => p.PlanFabricationLignes)
                .HasForeignKey(d => d.PeriodiciteId)
                .HasConstraintName("FK__Plan_Fabr__Perio__3DE82FB7");

            entity.HasOne(d => d.PlanEntete).WithMany(p => p.PlanFabricationLignes)
                .HasForeignKey(d => d.PlanEnteteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_Fabr__PlanE__36470DEF");

            entity.HasOne(d => d.Section).WithMany(p => p.PlanFabricationLignes)
                .HasForeignKey(d => d.SectionId)
                .HasConstraintName("FK__Plan_Fabr__Secti__373B3228");

            entity.HasOne(d => d.TypeCaracteristique).WithMany(p => p.PlanFabricationLignes)
                .HasForeignKey(d => d.TypeCaracteristiqueId)
                .HasConstraintName("FK__Plan_Fabr__TypeC__3B0BC30C");

            entity.HasOne(d => d.TypeControle).WithMany(p => p.PlanFabricationLignes)
                .HasForeignKey(d => d.TypeControleId)
                .HasConstraintName("FK__Plan_Fabr__TypeC__3BFFE745");
        });

        modelBuilder.Entity<PlanFabricationLigneExtraColonne>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Fab__ExtraCol__3214EC07");

            entity.ToTable("Plan_Fabrication_Ligne_ExtraColonne");

            entity.HasIndex(e => new { e.LigneId, e.CleColonne }, "UQ__Plan_Fab__ExtraCol_LigneCol").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CleColonne)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.ValeurColonne).HasMaxLength(500);

            entity.HasOne(d => d.Ligne).WithMany(p => p.PlanFabricationLigneExtraColonnes)
                .HasForeignKey(d => d.LigneId)
                .HasConstraintName("FK__Plan_Fab__Ligne__ExtraCol");
        });

        modelBuilder.Entity<PlanFabricationSection>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Fab__3214EC07264A86C7");

            entity.ToTable("Plan_Fabrication_Section");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.LibelleSection)
                .HasMaxLength(300)
                .IsUnicode(false);

            entity.HasOne(d => d.ModeleSection).WithMany(p => p.PlanFabricationSections)
                .HasForeignKey(d => d.ModeleSectionId)
                .HasConstraintName("FK__Plan_Fabr__Model__2EA5EC27");

            entity.HasOne(d => d.Periodicite).WithMany(p => p.PlanFabricationSections)
                .HasForeignKey(d => d.PeriodiciteId)
                .HasConstraintName("FK__Plan_Fabr__Perio__318258D2");

            entity.HasOne(d => d.PlanEntete).WithMany(p => p.PlanFabricationSections)
                .HasForeignKey(d => d.PlanEnteteId)
                .HasConstraintName("FK__Plan_Fabr__PlanE__2DB1C7EE");

            entity.HasOne(d => d.RegleEchantillonnage).WithMany(p => p.PlanFabricationSections)
                .HasForeignKey(d => d.RegleEchantillonnageId)
                .HasConstraintName("FK__Plan_Fabr__Regle__32767D0B");

            entity.HasOne(d => d.TypeSection).WithMany(p => p.PlanFabricationSections)
                .HasForeignKey(d => d.TypeSectionId)
                .HasConstraintName("FK__Plan_Fabr__TypeS__308E3499");
        });

        modelBuilder.Entity<PlanVerifMachineEcheance>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Ver__3214EC073124947D");

            entity.ToTable("Plan_VerifMachine_Echeance");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");

            entity.HasOne(d => d.Periodicite).WithMany(p => p.PlanVerifMachineEcheances)
                .HasForeignKey(d => d.PeriodiciteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_Veri__Perio__0C1BC9F9");

            entity.HasOne(d => d.PlanLigne).WithMany(p => p.PlanVerifMachineEcheances)
                .HasForeignKey(d => d.PlanLigneId)
                .HasConstraintName("FK__Plan_Veri__PlanL__0B27A5C0");

            entity.HasOne(d => d.RefMoyenDetection).WithMany(p => p.PlanVerifMachineEcheances)
                .HasForeignKey(d => d.RefMoyenDetectionId)
                .HasConstraintName("FK__Plan_Veri__RefMo__0D0FEE32");
        });

        modelBuilder.Entity<PlanVerifMachineEntete>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Ver__3214EC07EC95D89A");

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
                .HasDefaultValue("BROUILLON");
            entity.Property(e => e.Version).HasDefaultValue(0);

            entity.HasOne(d => d.Formulaire).WithMany(p => p.PlanVerifMachineEntetes)
                .HasForeignKey(d => d.FormulaireId)
                .HasConstraintName("FK__Plan_Veri__Formu__762C88DA");

            entity.HasOne(d => d.MachineCodeNavigation).WithMany(p => p.PlanVerifMachineEntetes)
                .HasForeignKey(d => d.MachineCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_Veri__Machi__7167D3BD");
        });

        modelBuilder.Entity<PlanVerifMachineFamille>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Ver__3214EC07E337156A");

            entity.ToTable("Plan_VerifMachine_Famille");

            entity.HasIndex(e => new { e.PlanEnteteId, e.RefFamilleCorpsId }, "UQ__Plan_Ver__7457AEA4F19DEF73").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");

            entity.HasOne(d => d.PlanEntete).WithMany(p => p.PlanVerifMachineFamilles)
                .HasForeignKey(d => d.PlanEnteteId)
                .HasConstraintName("FK__Plan_Veri__PlanE__7AF13DF7");

            entity.HasOne(d => d.RefFamilleCorps).WithMany(p => p.PlanVerifMachineFamilles)
                .HasForeignKey(d => d.RefFamilleCorpsId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Plan_Veri__RefFa__7BE56230");
        });

        modelBuilder.Entity<PlanVerifMachineLigne>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Ver__3214EC071DCDF646");

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
                .HasConstraintName("FK__Plan_Veri__PlanE__7FB5F314");
        });

        modelBuilder.Entity<PlanVerifMachineLigneExtraColonne>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Ver__3214EC0780CAB349");

            entity.ToTable("Plan_VerifMachine_Ligne_ExtraColonne");

            entity.HasIndex(e => new { e.LigneId, e.CleColonne }, "UQ__Plan_Ver__B2068CF310D1833E").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CleColonne)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.ValeurColonne).HasMaxLength(500);

            entity.HasOne(d => d.Ligne).WithMany(p => p.PlanVerifMachineLigneExtraColonnes)
                .HasForeignKey(d => d.LigneId)
                .HasConstraintName("FK__Plan_Veri__Ligne__0662F0A3");
        });

        modelBuilder.Entity<PlanVerifMachineMatricePiece>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Plan_Ver__3214EC07364EF14E");

            entity.ToTable("Plan_VerifMachine_MatricePiece");

            entity.HasIndex(e => new { e.EcheanceId, e.FamilleId, e.RoleVerif, e.PieceRefId }, "UQ__Plan_Ver__91CCDAA89DA31744").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.RoleVerif)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.Echeance).WithMany(p => p.PlanVerifMachineMatricePieces)
                .HasForeignKey(d => d.EcheanceId)
                .HasConstraintName("FK__Plan_Veri__Echea__12C8C788");

            entity.HasOne(d => d.Famille).WithMany(p => p.PlanVerifMachineMatricePieces)
                .HasForeignKey(d => d.FamilleId)
                .HasConstraintName("FK__Plan_Veri__Famil__13BCEBC1");

            entity.HasOne(d => d.PieceRef).WithMany(p => p.PlanVerifMachineMatricePieces)
                .HasForeignKey(d => d.PieceRefId)
                .HasConstraintName("FK__Plan_Veri__Piece__15A53433");
        });

        modelBuilder.Entity<PosteTravail>(entity =>
        {
            entity.HasKey(e => e.CodePoste).HasName("PK__PosteTra__4045446B3A665B72");

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
                        .HasConstraintName("FK__PosteTrav__CodeM__5224328E"),
                    l => l.HasOne<PosteTravail>().WithMany()
                        .HasForeignKey("CodePoste")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__PosteTrav__CodeP__51300E55"),
                    j =>
                    {
                        j.HasKey("CodePoste", "CodeMachine").HasName("PK__PosteTra__A548230BE4B912C8");
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
            entity.HasKey(e => e.CodeArticle).HasName("PK__ProduitF__32384FB0960F61E2");

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
                .HasConstraintName("FK__ProduitFi__CodeA__2CF2ADDF");

            entity.HasOne(d => d.FamilleProduitFiniCodeNavigation).WithMany(p => p.ProduitFinis)
                .HasForeignKey(d => d.FamilleProduitFiniCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ProduitFi__Famil__2DE6D218");

            entity.HasOne(d => d.TypeRobinetCodeNavigation).WithMany(p => p.ProduitFinis)
                .HasForeignKey(d => d.TypeRobinetCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ProduitFi__TypeR__2EDAF651");
        });

        modelBuilder.Entity<RefCaracteristique>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Ref_Cara__3214EC07900DE767");

            entity.ToTable("Ref_Caracteristique", tb => tb.HasTrigger("trg_no_del_Ref_Caracteristique"));

            entity.HasIndex(e => e.LibelleNormalise, "IX_Ref_Caracteristique_Normalise");

            entity.HasIndex(e => e.LibelleNormalise, "UQ__Ref_Cara__5C5A3C6ECF0052BB").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.Libelle)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.LibelleNormalise)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.LimiteSpecTexteDefaut)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.TypeCaracteristique).WithMany(p => p.RefCaracteristiques)
                .HasForeignKey(d => d.TypeCaracteristiqueId)
                .HasConstraintName("FK__Ref_Carac__TypeC__0E6E26BF");
        });

        modelBuilder.Entity<RefFamilleCorp>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Ref_Fami__3214EC07ED8BBA7F");

            entity.ToTable("Ref_FamilleCorps", tb => tb.HasTrigger("trg_no_del_Ref_FamilleCorps"));

            entity.HasIndex(e => e.Code, "UQ__Ref_Fami__A25C5AA7F42847B9").IsUnique();

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
            entity.HasKey(e => e.Id).HasName("PK__Ref_Form__3214EC07CD178B7D");

            entity.ToTable("Ref_Formulaire");

            entity.HasIndex(e => new { e.CodeReference, e.Version }, "UQ__Ref_Form__4F92504BDB0911DF").IsUnique();

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
                .HasDefaultValue("ACTIF");
        });

        modelBuilder.Entity<RefFormulaireColonneDef>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Ref_Form__3214EC078268C75F");

            entity.ToTable("Ref_Formulaire_ColonneDef", tb => tb.HasTrigger("trg_no_del_Ref_Formulaire_ColonneDef"));

            entity.HasIndex(e => new { e.CodeReference, e.CleColonne }, "UQ__Ref_Form__862800991333CA82").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.CleColonne)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.CodeReference).HasMaxLength(250);
            entity.Property(e => e.InsertAfter)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.LabelAffiche)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.TargetTable)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TypeValeur)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("TEXT");
        });

        modelBuilder.Entity<RefFormulaireEquipe>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Ref_Form__3214EC074779A656");

            entity.ToTable("Ref_Formulaire_Equipe", tb => tb.HasTrigger("trg_no_del_Ref_Formulaire_Equipe"));

            entity.HasIndex(e => new { e.CodeReference, e.NomEquipe }, "UQ__Ref_Form__B4065A2096D1EA75").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.CodeReference).HasMaxLength(250);
            entity.Property(e => e.NomEquipe)
                .HasMaxLength(60)
                .IsUnicode(false);
        });

        modelBuilder.Entity<RefMoyenDetection>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Ref_Moye__3214EC07E78C16B5");

            entity.ToTable("Ref_MoyenDetection", tb => tb.HasTrigger("trg_no_del_Ref_MoyenDetection"));

            entity.HasIndex(e => e.Code, "UQ__Ref_Moye__A25C5AA7133D9B91").IsUnique();

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
            entity.HasKey(e => e.Id).HasName("PK__Ref_Regl__3214EC0765D3637E");

            entity.ToTable("Ref_RegleEchantillonnage", tb => tb.HasTrigger("trg_no_del_Ref_RegleEchantillonnage"));

            entity.HasIndex(e => e.Code, "UQ__Ref_Regl__A25C5AA7A9E082E5").IsUnique();

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
            entity.HasKey(e => e.Id).HasName("PK__RefreshT__3214EC078D05C8FF");

            entity.HasIndex(e => e.Token, "UQ__RefreshT__1EB4F8177D49C854").IsUnique();

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
                .HasConstraintName("FK__RefreshTo__Utili__1DB06A4F");
        });

        modelBuilder.Entity<RisqueDefaut>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RisqueDe__3214EC07252C52F2");

            entity.ToTable("RisqueDefaut", tb => tb.HasTrigger("trg_no_del_RisqueDefaut"));

            entity.HasIndex(e => e.CodeDefaut, "UQ__RisqueDe__2EF8734307187AAF").IsUnique();

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
            entity.HasKey(e => e.NumeroBl).HasName("PK__SDELIVER__C664DCCD2CE03603");

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
            entity.HasKey(e => e.Id).HasName("PK__TypeCara__3214EC07681D48D1");

            entity.ToTable("TypeCaracteristique", tb => tb.HasTrigger("trg_no_del_TypeCaracteristique"));

            entity.HasIndex(e => e.Code, "UQ__TypeCara__A25C5AA753C80DA0").IsUnique();

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
            entity.HasKey(e => e.Id).HasName("PK__TypeCont__3214EC07D2AB11EA");

            entity.ToTable("TypeControle", tb => tb.HasTrigger("trg_no_del_TypeControle"));

            entity.HasIndex(e => e.Code, "UQ__TypeCont__A25C5AA7CB9C5D23").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.Code)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Libelle)
                .HasMaxLength(80)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TypeDocument>(entity =>
        {
            entity.HasKey(e => e.Code).HasName("PK__TypeDocu__A25C5AA6F146D1E0");

            entity.ToTable("TypeDocument", tb => tb.HasTrigger("trg_no_del_TypeDocument"));

            entity.Property(e => e.Code)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.Libelle)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TypeRobinet>(entity =>
        {
            entity.HasKey(e => e.Code).HasName("PK__TypeRobi__A25C5AA6F81EF124");

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
            entity.HasKey(e => e.Id).HasName("PK__TypeSect__3214EC07337642B6");

            entity.ToTable("TypeSection", tb => tb.HasTrigger("trg_no_del_TypeSection"));

            entity.HasIndex(e => e.Code, "UQ__TypeSect__A25C5AA75C88EB06").IsUnique();

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
            entity.HasKey(e => e.Id).HasName("PK__Utilisat__3214EC07BFB34545");

            entity.ToTable("UtilisateursApp");

            entity.HasIndex(e => e.Matricule, "IX_UtilisateursApp_Matricule");

            entity.HasIndex(e => e.Matricule, "UQ__Utilisat__0FB9FB43A8646B72").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__Utilisat__A9D1053409FABF27").IsUnique();

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
