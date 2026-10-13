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

    public virtual DbSet<Alerte> Alertes { get; set; }

    public virtual DbSet<AppRole> AppRoles { get; set; }

    public virtual DbSet<Article> Articles { get; set; }

    public virtual DbSet<Atextra> Atextras { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Autili> Autilis { get; set; }

    public virtual DbSet<BomdNomenclature> BomdNomenclatures { get; set; }

    public virtual DbSet<ColumnAllowedValue> ColumnAllowedValues { get; set; }

    public virtual DbSet<ColumnValueType> ColumnValueTypes { get; set; }

    public virtual DbSet<DocumentEntete> DocumentEntetes { get; set; }

    public virtual DbSet<DocumentLigne> DocumentLignes { get; set; }

    public virtual DbSet<DocumentLigneExtraColonne> DocumentLigneExtraColonnes { get; set; }

    public virtual DbSet<DocumentLineImage> DocumentLineImages { get; set; }

    public virtual DbSet<DocumentSection> DocumentSections { get; set; }

    public virtual DbSet<DocumentVerifMachineEcheance> DocumentVerifMachineEcheances { get; set; }

    public virtual DbSet<DocumentVerifMachineFamille> DocumentVerifMachineFamilles { get; set; }

    public virtual DbSet<DocumentVerifMachineMatricePiece> DocumentVerifMachineMatricePieces { get; set; }

    public virtual DbSet<ExecControleDocumentStatut> ExecControleDocumentStatuts { get; set; }

    public virtual DbSet<ExecControleLigneReponse> ExecControleLigneReponses { get; set; }

    public virtual DbSet<ExecControleOf> ExecControleOfs { get; set; }

    public virtual DbSet<ExecControleOfPoste> ExecControleOfPostes { get; set; }

    public virtual DbSet<ExecControleOfSession> ExecControleOfSessions { get; set; }

    public virtual DbSet<ExecControleTranche> ExecControleTranches { get; set; }

    public virtual DbSet<ExecEchantillonnage> ExecEchantillonnages { get; set; }

    public virtual DbSet<ExecPieceType> ExecPieceTypes { get; set; }

    public virtual DbSet<ExecPrelevementIntermediaire> ExecPrelevementIntermediaires { get; set; }

    public virtual DbSet<ExecProductionPeriod> ExecProductionPeriods { get; set; }

    public virtual DbSet<ExecRcPosteBilan> ExecRcPosteBilans { get; set; }

    public virtual DbSet<ExecRcPosteReponse> ExecRcPosteReponses { get; set; }

    public virtual DbSet<ExecRegistreTracabilite> ExecRegistreTracabilites { get; set; }

    public virtual DbSet<ExecRegistreTracabiliteComposant> ExecRegistreTracabiliteComposants { get; set; }

    public virtual DbSet<ExecSessionPost> ExecSessionPosts { get; set; }

    public virtual DbSet<ExecVerifMachineReponse> ExecVerifMachineReponses { get; set; }

    public virtual DbSet<ExecutionDocumentType> ExecutionDocumentTypes { get; set; }

    public virtual DbSet<FamilleProduitFini> FamilleProduitFinis { get; set; }

    public virtual DbSet<InspectionLevel> InspectionLevels { get; set; }

    public virtual DbSet<Instrument> Instruments { get; set; }

    public virtual DbSet<JournalConnexion> JournalConnexions { get; set; }

    public virtual DbSet<Machine> Machines { get; set; }

    public virtual DbSet<MagExpeditionBl> MagExpeditionBls { get; set; }

    public virtual DbSet<MagExpeditionBlScanOf> MagExpeditionBlScanOfs { get; set; }

    public virtual DbSet<MagPreparationOf> MagPreparationOfs { get; set; }

    public virtual DbSet<MagPreparationOfLot> MagPreparationOfLots { get; set; }

    public virtual DbSet<MfgheadOrdreFabrication> MfgheadOrdreFabrications { get; set; }

    public virtual DbSet<MfgmatBesoinOf> MfgmatBesoinOfs { get; set; }

    public virtual DbSet<MoyenControle> MoyenControles { get; set; }

    public virtual DbSet<NatureArticle> NatureArticles { get; set; }

    public virtual DbSet<NatureArticleOperation> NatureArticleOperations { get; set; }

    public virtual DbSet<Nqa> Nqas { get; set; }

    public virtual DbSet<Operation> Operations { get; set; }

    public virtual DbSet<Periodicite> Periodicites { get; set; }

    public virtual DbSet<PeriodiciteMachine> PeriodiciteMachines { get; set; }

    public virtual DbSet<PieceReference> PieceReferences { get; set; }

    public virtual DbSet<PosteTravail> PosteTravails { get; set; }

    public virtual DbSet<PreassignedRole> PreassignedRoles { get; set; }

    public virtual DbSet<ProduitFini> ProduitFinis { get; set; }

    public virtual DbSet<RefCaracteristique> RefCaracteristiques { get; set; }

    public virtual DbSet<RefFamilleCorp> RefFamilleCorps { get; set; }

    public virtual DbSet<RefFormulaire> RefFormulaires { get; set; }

    public virtual DbSet<RefFormulaireColonneDef> RefFormulaireColonneDefs { get; set; }

    public virtual DbSet<RefFormulaireEquipe> RefFormulaireEquipes { get; set; }

    public virtual DbSet<RefIso2859Echantillonnage> RefIso2859Echantillonnages { get; set; }

    public virtual DbSet<RefIso2859LettresCode> RefIso2859LettresCodes { get; set; }

    public virtual DbSet<RefMoyenDetection> RefMoyenDetections { get; set; }

    public virtual DbSet<ReferencePieceRole> ReferencePieceRoles { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<RisqueDefaut> RisqueDefauts { get; set; }

    public virtual DbSet<SamplingPlan> SamplingPlans { get; set; }

    public virtual DbSet<Sdelivery> Sdeliveries { get; set; }

    public virtual DbSet<Team> Teams { get; set; }

    public virtual DbSet<TypeCaracteristique> TypeCaracteristiques { get; set; }

    public virtual DbSet<TypeControle> TypeControles { get; set; }

    public virtual DbSet<TypeDocument> TypeDocuments { get; set; }

    public virtual DbSet<TypeRobinet> TypeRobinets { get; set; }

    public virtual DbSet<TypeSection> TypeSections { get; set; }

    public virtual DbSet<UtilisateursApp> UtilisateursApps { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Alerte>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Alertes__3214EC072410665C");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CleEntite)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.DateAlerte)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.DateResolution).HasPrecision(3);
            entity.Property(e => e.Destinataires)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.TypeAlerte)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<AppRole>(entity =>
        {
            entity.HasKey(e => e.Code).HasName("PK__AppRole__A25C5AA66D85197E");

            entity.ToTable("AppRole", tb => tb.HasTrigger("trg_no_del_AppRole"));

            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.Label)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Article>(entity =>
        {
            entity.HasKey(e => e.CodeArticle).HasName("PK__Article__32384FB045E84AEF");

            entity.ToTable("Article");

            entity.HasIndex(e => e.NatureArticleCode, "IX_Article_NatureArticle");

            entity.HasIndex(e => e.TypeArticle, "IX_Article_TypeArticle");

            entity.HasIndex(e => new { e.CodeArticle, e.Origin }, "UQ_Article_Origin").IsUnique();

            entity.HasIndex(e => new { e.CodeArticle, e.TypeArticle }, "UQ_Article_Type").IsUnique();

            entity.Property(e => e.CodeArticle)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.DateCreation)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.DateModification).HasPrecision(3);
            entity.Property(e => e.Designation)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.NatureArticleCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Origin)
                .HasMaxLength(12)
                .IsUnicode(false)
                .HasComputedColumnSql("(CONVERT([varchar](12),case when [TypeArticle]='COMPOSANT' then 'PURCHASED' else 'MANUFACTURED' end))", true);
            entity.Property(e => e.TypeArticle)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.NatureArticleCodeNavigation).WithMany(p => p.Articles)
                .HasForeignKey(d => d.NatureArticleCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Article__NatureA__5D95E53A");
        });

        modelBuilder.Entity<Atextra>(entity =>
        {
            entity.HasKey(e => new { e.Codfic0, e.Zone0, e.Ident10, e.Langue0 }).HasName("PK__ATEXTRA__4F21B2DBCE2A6E61");

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

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__AuditLog__3214EC07AC6EEEE6");

            entity.ToTable("AuditLog", tb => tb.HasTrigger("TR_AuditLog_AppendOnly"));

            entity.HasIndex(e => new { e.TableName, e.EntityId, e.OccurredAt }, "IX_AuditLog_Entity");

            entity.Property(e => e.Action)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.EntityId)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.OccurredAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.TableName)
                .HasMaxLength(128)
                .IsUnicode(false);
            entity.Property(e => e.UserMatricule)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.UserMatriculeNavigation).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.UserMatricule)
                .HasConstraintName("FK__AuditLog__UserMa__56E8E7AB");
        });

        modelBuilder.Entity<Autili>(entity =>
        {
            entity.HasKey(e => e.Usr0).HasName("PK__AUTILIS__0812AE69C945F859");

            entity.ToTable("AUTILIS");

            entity.Property(e => e.Usr0)
                .HasMaxLength(20)
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
            entity.HasKey(e => new { e.ArticleParent, e.CodeComposant, e.CodeAlternative }).HasName("PK__BOMD_Nom__2710E851832F7337");

            entity.ToTable("BOMD_Nomenclature");

            entity.HasIndex(e => e.CodeComposant, "IX_BOMD_Composant");

            entity.HasIndex(e => e.ArticleParent, "IX_BOMD_Parent");

            entity.Property(e => e.ArticleParent)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.CodeComposant)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.ParentOrigin)
                .HasMaxLength(12)
                .IsUnicode(false)
                .HasComputedColumnSql("(CONVERT([varchar](12),'MANUFACTURED'))", true);
            entity.Property(e => e.QuantiteRequise).HasColumnType("decimal(18, 3)");

            entity.HasOne(d => d.CodeComposantNavigation).WithMany(p => p.BomdNomenclatureCodeComposantNavigations)
                .HasForeignKey(d => d.CodeComposant)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__BOMD_Nome__CodeC__681373AD");

            entity.HasOne(d => d.Article).WithMany(p => p.BomdNomenclatureArticles)
                .HasPrincipalKey(p => new { p.CodeArticle, p.Origin })
                .HasForeignKey(d => new { d.ArticleParent, d.ParentOrigin })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BillOfMaterials_ManufacturedParent");
        });

        modelBuilder.Entity<ColumnAllowedValue>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ColumnAl__3214EC07C0A6267F");

            entity.ToTable("ColumnAllowedValue");

            entity.HasIndex(e => new { e.ColumnDefinitionId, e.Value }, "UQ__ColumnAl__FDA6B628196C178B").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.Value).HasMaxLength(200);

            entity.HasOne(d => d.ColumnDefinition).WithMany(p => p.ColumnAllowedValues)
                .HasForeignKey(d => d.ColumnDefinitionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ColumnAll__Colum__2704CA5F");
        });

        modelBuilder.Entity<ColumnValueType>(entity =>
        {
            entity.HasKey(e => e.Code).HasName("PK__ColumnVa__A25C5AA6C830FBCA");

            entity.ToTable("ColumnValueType", tb => tb.HasTrigger("trg_no_del_ColumnValueType"));

            entity.Property(e => e.Code)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.Label)
                .HasMaxLength(60)
                .IsUnicode(false);
        });

        modelBuilder.Entity<DocumentEntete>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Document__3214EC072261E7EE");

            entity.ToTable("Document_Entete", tb => tb.HasTrigger("TR_DocumentEntete_SingleActive"));

            entity.HasIndex(e => e.CodeArticleSageVersionne, "IX_Document_Entete_Article").HasFilter("([CodeArticleSageVersionne] IS NOT NULL)");

            entity.HasIndex(e => e.MachineCode, "IX_Document_Entete_Machine").HasFilter("([MachineCode] IS NOT NULL)");

            entity.HasIndex(e => new { e.TypeDocumentCode, e.Statut }, "IX_Document_Entete_TypeStatut");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CodeArticleSageVersionne)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.CreeLe)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.CreePar)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Designation)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.FamilleProduitFiniCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.MachineCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.ModifieLe).HasPrecision(3);
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
            entity.Property(e => e.Portee)
                .HasMaxLength(12)
                .IsUnicode(false)
                .HasDefaultValue("GENERIQUE");
            entity.Property(e => e.PosteCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.Statut)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("BROUILLON");
            entity.Property(e => e.TypeDocumentCode)
                .HasMaxLength(30)
                .IsUnicode(false);

            entity.HasOne(d => d.CreeParNavigation).WithMany(p => p.DocumentEnteteCreeParNavigations)
                .HasForeignKey(d => d.CreePar)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Document___CreeP__44952D46");

            entity.HasOne(d => d.DocumentSource).WithMany(p => p.InverseDocumentSource)
                .HasForeignKey(d => d.DocumentSourceId)
                .HasConstraintName("FK__Document___Docum__3EDC53F0");

            entity.HasOne(d => d.FamilleProduitFiniCodeNavigation).WithMany(p => p.DocumentEntetes)
                .HasForeignKey(d => d.FamilleProduitFiniCode)
                .HasConstraintName("FK__Document___Famil__4865BE2A");

            entity.HasOne(d => d.Formulaire).WithMany(p => p.DocumentEntetes)
                .HasForeignKey(d => d.FormulaireId)
                .HasConstraintName("FK__Document___Formu__43A1090D");

            entity.HasOne(d => d.MachineCodeNavigation).WithMany(p => p.DocumentEntetes)
                .HasForeignKey(d => d.MachineCode)
                .HasConstraintName("FK__Document___Machi__4A4E069C");

            entity.HasOne(d => d.ModifieParNavigation).WithMany(p => p.DocumentEnteteModifieParNavigations)
                .HasForeignKey(d => d.ModifiePar)
                .HasConstraintName("FK__Document___Modif__467D75B8");

            entity.HasOne(d => d.NatureArticleCodeNavigation).WithMany(p => p.DocumentEntetes)
                .HasForeignKey(d => d.NatureArticleCode)
                .HasConstraintName("FK__Document___Natur__477199F1");

            entity.HasOne(d => d.OperationCodeNavigation).WithMany(p => p.DocumentEntetes)
                .HasForeignKey(d => d.OperationCode)
                .HasConstraintName("FK__Document___Opera__42ACE4D4");

            entity.HasOne(d => d.PosteCodeNavigation).WithMany(p => p.DocumentEntetes)
                .HasForeignKey(d => d.PosteCode)
                .HasConstraintName("FK__Document___Poste__4959E263");

            entity.HasOne(d => d.TypeDocumentCodeNavigation).WithMany(p => p.DocumentEntetes)
                .HasForeignKey(d => d.TypeDocumentCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Document___TypeD__3BFFE745");
        });

        modelBuilder.Entity<DocumentLigne>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Document__3214EC07F63BE426");

            entity.ToTable("Document_Ligne");

            entity.HasIndex(e => e.CaracteristiqueId, "IX_Document_Ligne_Caracteristique");

            entity.HasIndex(e => e.EnteteId, "IX_Document_Ligne_Entete");

            entity.HasIndex(e => e.SectionId, "IX_Document_Ligne_Section");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
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
            entity.Property(e => e.TypeLigne)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Caracteristique).WithMany(p => p.DocumentLignes)
                .HasForeignKey(d => d.CaracteristiqueId)
                .HasConstraintName("FK__Document___Carac__5B78929E");

            entity.HasOne(d => d.Entete).WithMany(p => p.DocumentLignes)
                .HasForeignKey(d => d.EnteteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Document___Entet__589C25F3");

            entity.HasOne(d => d.InstrumentCodeNavigation).WithMany(p => p.DocumentLignes)
                .HasForeignKey(d => d.InstrumentCode)
                .HasConstraintName("FK__Document___Instr__5F492382");

            entity.HasOne(d => d.MachineCodeNavigation).WithMany(p => p.DocumentLignes)
                .HasForeignKey(d => d.MachineCode)
                .HasConstraintName("FK__Document___Machi__6225902D");

            entity.HasOne(d => d.MoyenControle).WithMany(p => p.DocumentLignes)
                .HasForeignKey(d => d.MoyenControleId)
                .HasConstraintName("FK__Document___Moyen__5E54FF49");

            entity.HasOne(d => d.Periodicite).WithMany(p => p.DocumentLignes)
                .HasForeignKey(d => d.PeriodiciteId)
                .HasConstraintName("FK__Document___Perio__603D47BB");

            entity.HasOne(d => d.RisqueDefaut).WithMany(p => p.DocumentLignes)
                .HasForeignKey(d => d.RisqueDefautId)
                .HasConstraintName("FK__Document___Risqu__6319B466");

            entity.HasOne(d => d.Section).WithMany(p => p.DocumentLignes)
                .HasForeignKey(d => d.SectionId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Document___Secti__59904A2C");

            entity.HasOne(d => d.TypeCaracteristique).WithMany(p => p.DocumentLignes)
                .HasForeignKey(d => d.TypeCaracteristiqueId)
                .HasConstraintName("FK__Document___TypeC__5C6CB6D7");

            entity.HasOne(d => d.TypeControle).WithMany(p => p.DocumentLignes)
                .HasForeignKey(d => d.TypeControleId)
                .HasConstraintName("FK__Document___TypeC__5D60DB10");
        });

        modelBuilder.Entity<DocumentLigneExtraColonne>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Document__3214EC07BF204965");

            entity.ToTable("Document_Ligne_ExtraColonne", tb => tb.HasTrigger("TR_DocumentLigneExtraColonne_Validate"));

            entity.HasIndex(e => new { e.LigneId, e.CleColonne }, "UQ__Document__B2068CF3F4E5C96F").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CleColonne)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.ValeurColonne).HasMaxLength(500);

            entity.HasOne(d => d.Ligne).WithMany(p => p.DocumentLigneExtraColonnes)
                .HasForeignKey(d => d.LigneId)
                .HasConstraintName("FK__Document___Ligne__6ABAD62E");
        });

        modelBuilder.Entity<DocumentLineImage>(entity =>
        {
            entity.HasKey(e => e.LineId).HasName("PK__Document__2EAE6529949B7FCF");

            entity.ToTable("DocumentLineImage");

            entity.Property(e => e.LineId).ValueGeneratedNever();
            entity.Property(e => e.ContentType)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Line).WithOne(p => p.DocumentLineImage)
                .HasForeignKey<DocumentLineImage>(d => d.LineId)
                .HasConstraintName("FK__DocumentL__LineI__6E8B6712");
        });

        modelBuilder.Entity<DocumentSection>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Document__3214EC0754181EBF");

            entity.ToTable("Document_Section");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.LibelleSection)
                .HasMaxLength(300)
                .IsUnicode(false);

            entity.HasOne(d => d.Entete).WithMany(p => p.DocumentSections)
                .HasForeignKey(d => d.EnteteId)
                .HasConstraintName("FK__Document___Entet__50FB042B");

            entity.HasOne(d => d.Nqa).WithMany(p => p.DocumentSections)
                .HasForeignKey(d => d.NqaId)
                .HasConstraintName("FK__Document___NqaId__54CB950F");

            entity.HasOne(d => d.Periodicite).WithMany(p => p.DocumentSections)
                .HasForeignKey(d => d.PeriodiciteId)
                .HasConstraintName("FK__Document___Perio__53D770D6");

            entity.HasOne(d => d.TypeSection).WithMany(p => p.DocumentSections)
                .HasForeignKey(d => d.TypeSectionId)
                .HasConstraintName("FK__Document___TypeS__52E34C9D");
        });

        modelBuilder.Entity<DocumentVerifMachineEcheance>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Document__3214EC07121A5275");

            entity.ToTable("Document_VerifMachine_Echeance");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");

            entity.HasOne(d => d.Ligne).WithMany(p => p.DocumentVerifMachineEcheances)
                .HasForeignKey(d => d.LigneId)
                .HasConstraintName("FK__Document___Ligne__00AA174D");

            entity.HasOne(d => d.PeriodiciteMachine).WithMany(p => p.DocumentVerifMachineEcheances)
                .HasForeignKey(d => d.PeriodiciteMachineId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Document___Perio__019E3B86");

            entity.HasOne(d => d.RefMoyenDetection).WithMany(p => p.DocumentVerifMachineEcheances)
                .HasForeignKey(d => d.RefMoyenDetectionId)
                .HasConstraintName("FK__Document___RefMo__02925FBF");
        });

        modelBuilder.Entity<DocumentVerifMachineFamille>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Document__3214EC07417A3F59");

            entity.ToTable("Document_VerifMachine_Famille");

            entity.HasIndex(e => new { e.EnteteId, e.RefFamilleCorpsId }, "UQ__Document__90BE3146190EE0FB").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");

            entity.HasOne(d => d.Entete).WithMany(p => p.DocumentVerifMachineFamilles)
                .HasForeignKey(d => d.EnteteId)
                .HasConstraintName("FK__Document___Entet__7BE56230");

            entity.HasOne(d => d.RefFamilleCorps).WithMany(p => p.DocumentVerifMachineFamilles)
                .HasForeignKey(d => d.RefFamilleCorpsId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Document___RefFa__7CD98669");
        });

        modelBuilder.Entity<DocumentVerifMachineMatricePiece>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Document__3214EC077FA734A4");

            entity.ToTable("Document_VerifMachine_MatricePiece");

            entity.HasIndex(e => new { e.EcheanceId, e.FamilleId, e.RoleVerif, e.PieceRefId }, "UQ__Document__91CCDAA8FF27CA34").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.RoleVerif)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.Echeance).WithMany(p => p.DocumentVerifMachineMatricePieces)
                .HasForeignKey(d => d.EcheanceId)
                .HasConstraintName("FK__Document___Echea__084B3915");

            entity.HasOne(d => d.Famille).WithMany(p => p.DocumentVerifMachineMatricePieces)
                .HasForeignKey(d => d.FamilleId)
                .HasConstraintName("FK__Document___Famil__093F5D4E");

            entity.HasOne(d => d.PieceRef).WithMany(p => p.DocumentVerifMachineMatricePieces)
                .HasForeignKey(d => d.PieceRefId)
                .HasConstraintName("FK__Document___Piece__0B27A5C0");

            entity.HasOne(d => d.RoleVerifNavigation).WithMany(p => p.DocumentVerifMachineMatricePieces)
                .HasForeignKey(d => d.RoleVerif)
                .HasConstraintName("FK__Document___RoleV__0A338187");
        });

        modelBuilder.Entity<ExecControleDocumentStatut>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Exec_Con__3214EC073DB8BFB8");

            entity.ToTable("Exec_ControleDocumentStatut", tb =>
                {
                    tb.HasTrigger("TR_ExecControleDocumentStatut_ProtectClosed");
                    tb.HasTrigger("TR_ExecControleDocumentStatut_TypeDocument");
                });

            entity.HasIndex(e => new { e.ExecControleOfId, e.SessionId }, "IX_ExecControleDocumentStatut_ExecSession");

            entity.HasIndex(e => new { e.ExecControleOfId, e.PosteCode }, "UX_ExecControleDocumentStatut_SamplingPerPoste")
                .IsUnique()
                .HasFilter("([TypeDocument]='ECHANTILLONNAGE' AND [PosteCode] IS NOT NULL)");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.DateExecution).HasPrecision(3);
            entity.Property(e => e.DateTermine).HasPrecision(3);
            entity.Property(e => e.Equipe)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.MachineCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.MatriculeOperateur)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.PosteCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.TypeDocument)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.Doc).WithMany(p => p.ExecControleDocumentStatuts)
                .HasForeignKey(d => d.DocId)
                .HasConstraintName("FK__Exec_Cont__DocId__5B438874");

            entity.HasOne(d => d.EquipeNavigation).WithMany(p => p.ExecControleDocumentStatuts)
                .HasForeignKey(d => d.Equipe)
                .HasConstraintName("FK__Exec_Cont__Equip__60083D91");

            entity.HasOne(d => d.ExecControleOf).WithMany(p => p.ExecControleDocumentStatuts)
                .HasForeignKey(d => d.ExecControleOfId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Exec_Cont__ExecC__5772F790");

            entity.HasOne(d => d.MachineCodeNavigation).WithMany(p => p.ExecControleDocumentStatuts)
                .HasForeignKey(d => d.MachineCode)
                .HasConstraintName("FK__Exec_Cont__Machi__5A4F643B");

            entity.HasOne(d => d.MatriculeOperateurNavigation).WithMany(p => p.ExecControleDocumentStatuts)
                .HasForeignKey(d => d.MatriculeOperateur)
                .HasConstraintName("FK__Exec_Cont__Matri__60FC61CA");

            entity.HasOne(d => d.PosteCodeNavigation).WithMany(p => p.ExecControleDocumentStatuts)
                .HasForeignKey(d => d.PosteCode)
                .HasConstraintName("FK__Exec_Cont__Poste__595B4002");

            entity.HasOne(d => d.Session).WithMany(p => p.ExecControleDocumentStatuts)
                .HasForeignKey(d => d.SessionId)
                .HasConstraintName("FK__Exec_Cont__Sessi__5C37ACAD");

            entity.HasOne(d => d.TypeDocumentNavigation).WithMany(p => p.ExecControleDocumentStatuts)
                .HasForeignKey(d => d.TypeDocument)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Exec_Cont__TypeD__58671BC9");
        });

        modelBuilder.Entity<ExecControleLigneReponse>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Exec_Con__3214EC07ACDE2A3E");

            entity.ToTable("Exec_ControleLigne_Reponse", tb => tb.HasTrigger("TR_ExecControleLigneReponse_ProtectClosed"));

            entity.HasIndex(e => new { e.ExecControleOfid, e.DocumentLigneId, e.DateSaisie }, "IX_ControleLigne_Ligne");

            entity.HasIndex(e => new { e.ExecControleOfid, e.Contexte, e.NumeroReglage }, "IX_ControleLigne_Reglage");

            entity.HasIndex(e => new { e.ExecControleDocumentStatutId, e.DocumentLigneId, e.TrancheHoraire }, "UX_ControleLigne_Comptage")
                .IsUnique()
                .HasFilter("([ResultatType]='COMPTAGE')");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.ActionsCorrection)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Contexte)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.DateSaisie)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.DetailsNc)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("DetailsNC");
            entity.Property(e => e.Equipe)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.ExecControleOfid).HasColumnName("ExecControleOFId");
            entity.Property(e => e.MatriculeOperateur)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.NumeroReglage)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.ResultatType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.TrancheHoraire)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.ValeurMesuree).HasColumnType("decimal(10, 3)");

            entity.HasOne(d => d.DocumentLigne).WithMany(p => p.ExecControleLigneReponses)
                .HasForeignKey(d => d.DocumentLigneId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Exec_Cont__Docum__0DCF0841");

            entity.HasOne(d => d.EquipeNavigation).WithMany(p => p.ExecControleLigneReponses)
                .HasForeignKey(d => d.Equipe)
                .HasConstraintName("FK__Exec_Cont__Equip__119F9925");

            entity.HasOne(d => d.ExecControleDocumentStatut).WithMany(p => p.ExecControleLigneReponses)
                .HasForeignKey(d => d.ExecControleDocumentStatutId)
                .HasConstraintName("FK__Exec_Cont__ExecC__0CDAE408");

            entity.HasOne(d => d.ExecControleOf).WithMany(p => p.ExecControleLigneReponses)
                .HasForeignKey(d => d.ExecControleOfid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Exec_Cont__ExecC__0BE6BFCF");

            entity.HasOne(d => d.MatriculeOperateurNavigation).WithMany(p => p.ExecControleLigneReponses)
                .HasForeignKey(d => d.MatriculeOperateur)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Exec_Cont__Matri__10AB74EC");
        });

        modelBuilder.Entity<ExecControleOf>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Exec_Con__3214EC0739189A03");

            entity.ToTable("Exec_ControleOF", tb =>
                {
                    tb.HasTrigger("TR_ExecControleOF_ClosedImmutable");
                    tb.HasTrigger("TR_ExecControleOF_NoDelete");
                    tb.HasTrigger("TR_ExecControleOF_SingleActiveAssembly");
                });

            entity.HasIndex(e => new { e.NumeroOf, e.OperationCode }, "IX_ExecControleOF_NumeroOF");

            entity.HasIndex(e => new { e.TypeOf, e.Statut }, "IX_ExecControleOF_TypeStatut");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.DateDebut)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.DateFin).HasPrecision(3);
            entity.Property(e => e.DiametreNominal).HasColumnType("decimal(10, 3)");
            entity.Property(e => e.LongueurNominale).HasColumnType("decimal(10, 3)");
            entity.Property(e => e.MachineCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.MachineCodePrevu)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.NumeroOf)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("NumeroOF");
            entity.Property(e => e.OperationCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.Statut)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("EN_COURS");
            entity.Property(e => e.SuspendedAt).HasPrecision(3);
            entity.Property(e => e.TypeOf)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.MachineCodeNavigation).WithMany(p => p.ExecControleOfMachineCodeNavigations)
                .HasForeignKey(d => d.MachineCode)
                .HasConstraintName("FK__Exec_Cont__Machi__2D7CBDC4");

            entity.HasOne(d => d.MachineCodePrevuNavigation).WithMany(p => p.ExecControleOfMachineCodePrevuNavigations)
                .HasForeignKey(d => d.MachineCodePrevu)
                .HasConstraintName("FK__Exec_Cont__Machi__2C88998B");

            entity.HasOne(d => d.NumeroOfNavigation).WithMany(p => p.ExecControleOfs)
                .HasForeignKey(d => d.NumeroOf)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Exec_Cont__Numer__2AA05119");

            entity.HasOne(d => d.OperationCodeNavigation).WithMany(p => p.ExecControleOfs)
                .HasForeignKey(d => d.OperationCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Exec_Cont__Opera__2B947552");

            entity.HasOne(d => d.PlanSource).WithMany(p => p.ExecControleOfs)
                .HasForeignKey(d => d.PlanSourceId)
                .HasConstraintName("FK__Exec_Cont__PlanS__2E70E1FD");
        });

        modelBuilder.Entity<ExecControleOfPoste>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Exec_Con__3214EC0767C3899A");

            entity.ToTable("Exec_ControleOf_Poste", tb => tb.HasTrigger("TR_ExecControleOfPoste_ProtectClosed"));

            entity.HasIndex(e => new { e.ExecControleOfId, e.PosteCode }, "UX_ExecControleOfPoste").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.PosteCode)
                .HasMaxLength(30)
                .IsUnicode(false);

            entity.HasOne(d => d.ExecControleOf).WithMany(p => p.ExecControleOfPostes)
                .HasForeignKey(d => d.ExecControleOfId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Exec_Cont__ExecC__39E294A9");

            entity.HasOne(d => d.PosteCodeNavigation).WithMany(p => p.ExecControleOfPostes)
                .HasForeignKey(d => d.PosteCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Exec_Cont__Poste__3AD6B8E2");
        });

        modelBuilder.Entity<ExecControleOfSession>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Exec_Con__3214EC075486DC76");

            entity.ToTable("Exec_ControleOf_Session", tb =>
                {
                    tb.HasTrigger("TR_ExecControleOfSession_Consistency");
                    tb.HasTrigger("TR_ExecControleOfSession_ProtectClosed");
                });

            entity.HasIndex(e => new { e.ExecControleOfId, e.Statut }, "IX_ExecControleOfSession_Exec");

            entity.HasIndex(e => new { e.ExecControleOfId, e.Equipe, e.DateFin }, "UX_ExecControleOfSession_OneOpenSessionPerTeam")
                .IsUnique()
                .HasFilter("([Statut] IN ('EN_COURS', 'EN_PAUSE'))");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.ClosingRequest)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.DateDebut)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.DateFin).HasPrecision(3);
            entity.Property(e => e.Equipe)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.Statut)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.SuspendedAt).HasPrecision(3);

            entity.HasOne(d => d.EquipeNavigation).WithMany(p => p.ExecControleOfSessions)
                .HasForeignKey(d => d.Equipe)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Exec_Cont__Equip__3F9B6DFF");

            entity.HasOne(d => d.ExecControleOf).WithMany(p => p.ExecControleOfSessions)
                .HasForeignKey(d => d.ExecControleOfId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Exec_Cont__ExecC__3EA749C6");
        });

        modelBuilder.Entity<ExecControleTranche>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Exec_Con__3214EC073D3C7FD7");

            entity.ToTable("Exec_ControleTranche", tb => tb.HasTrigger("TR_ExecControleTranche_ProtectClosed"));

            entity.HasIndex(e => new { e.ExecControleOfid, e.TrancheHoraire }, "IX_ExecControleTranche_Exec");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.ActionsCorrection)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.DetailsNc)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("DetailsNC");
            entity.Property(e => e.ExecControleOfid).HasColumnName("ExecControleOFId");
            entity.Property(e => e.HeureDebut).HasPrecision(3);
            entity.Property(e => e.HeureFin).HasPrecision(3);
            entity.Property(e => e.MatriculeApprobateur)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Remarques).IsUnicode(false);
            entity.Property(e => e.ResultatFinal)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.TrancheHoraire)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.ExecControleOf).WithMany(p => p.ExecControleTranches)
                .HasForeignKey(d => d.ExecControleOfid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Exec_Cont__ExecC__6C6E1476");

            entity.HasOne(d => d.MatriculeApprobateurNavigation).WithMany(p => p.ExecControleTranches)
                .HasForeignKey(d => d.MatriculeApprobateur)
                .HasConstraintName("FK__Exec_Cont__Matri__6E565CE8");

            entity.HasOne(d => d.Session).WithMany(p => p.ExecControleTranches)
                .HasForeignKey(d => d.SessionId)
                .HasConstraintName("FK__Exec_Cont__Sessi__6F4A8121");
        });

        modelBuilder.Entity<ExecEchantillonnage>(entity =>
        {
            entity.ToTable("Exec_Echantillonnage", tb => tb.HasTrigger("TR_ExecEchantillonnage_ProtectClosed"));

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.LettreCode)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.ExecControleDocumentStatut).WithMany(p => p.ExecEchantillonnages)
                .HasForeignKey(d => d.ExecControleDocumentStatutId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Exec_Echantillonnage_ExecControleDocumentStatut");

            entity.HasMany(d => d.CodeInstruments).WithMany(p => p.ExecEchantillonnages)
                .UsingEntity<Dictionary<string, object>>(
                    "ExecEchantillonnageInstrument",
                    r => r.HasOne<Instrument>().WithMany()
                        .HasForeignKey("CodeInstrument")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_ExecEchantillonnageInstrument_Instrument"),
                    l => l.HasOne<ExecEchantillonnage>().WithMany()
                        .HasForeignKey("ExecEchantillonnageId")
                        .HasConstraintName("FK_ExecEchantillonnageInstrument_ExecEchantillonnage"),
                    j =>
                    {
                        j.HasKey("ExecEchantillonnageId", "CodeInstrument");
                        j.ToTable("Exec_Echantillonnage_Instrument");
                        j.IndexerProperty<string>("CodeInstrument")
                            .HasMaxLength(40)
                            .IsUnicode(false);
                    });
        });

        modelBuilder.Entity<ExecPieceType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Exec_Pie__3214EC07947AE89E");

            entity.ToTable("Exec_PieceType", tb => tb.HasTrigger("TR_ExecPieceType_ProtectClosed"));

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.ExecControleOfid).HasColumnName("ExecControleOFId");
            entity.Property(e => e.HeureValidation)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())");
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
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Exec_Piec__ExecC__65C116E7");

            entity.HasOne(d => d.MatriculeOperateurNavigation).WithMany(p => p.ExecPieceTypes)
                .HasForeignKey(d => d.MatriculeOperateur)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Exec_Piec__Matri__689D8392");
        });

        modelBuilder.Entity<ExecPrelevementIntermediaire>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Exec_Pre__3214EC07C3195495");

            entity.ToTable("Exec_Prelevement_Intermediaire", tb => tb.HasTrigger("TR_ExecPrelevementIntermediaire_ProtectClosed"));

            entity.HasIndex(e => new { e.ExecControleOfid, e.SectionId, e.SessionId, e.PosteCode, e.TrancheHoraire, e.NumeroOccurrence }, "UQ_ExecPrelevementIntermediaire_Occurrence").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CreeLe)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.ExecControleOfid).HasColumnName("ExecControleOFId");
            entity.Property(e => e.HeureNotifPrevue).HasPrecision(3);
            entity.Property(e => e.HeureReponse).HasPrecision(3);
            entity.Property(e => e.PosteCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Resultat)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.TrancheHoraire)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.ExecControleOf).WithMany(p => p.ExecPrelevementIntermediaires)
                .HasForeignKey(d => d.ExecControleOfid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Exec_Prel__ExecC__00750D23");

            entity.HasOne(d => d.PosteCodeNavigation).WithMany(p => p.ExecPrelevementIntermediaires)
                .HasForeignKey(d => d.PosteCode)
                .HasConstraintName("FK__Exec_Prel__Poste__07220AB2");

            entity.HasOne(d => d.ProductionPeriod).WithMany(p => p.ExecPrelevementIntermediaires)
                .HasForeignKey(d => d.ProductionPeriodId)
                .HasConstraintName("FK__Exec_Prel__Produ__08162EEB");

            entity.HasOne(d => d.Section).WithMany(p => p.ExecPrelevementIntermediaires)
                .HasForeignKey(d => d.SectionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Exec_Prel__Secti__0169315C");

            entity.HasOne(d => d.Session).WithMany(p => p.ExecPrelevementIntermediaires)
                .HasForeignKey(d => d.SessionId)
                .HasConstraintName("FK__Exec_Prel__Sessi__062DE679");
        });

        modelBuilder.Entity<ExecProductionPeriod>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Exec_Pro__3214EC0705834F40");

            entity.ToTable("Exec_ProductionPeriod");

            entity.HasIndex(e => new { e.ExecControleOfId, e.EndedAt }, "IX_ExecProductionPeriod_Exec");

            entity.HasIndex(e => new { e.SessionId, e.PosteCode }, "UX_ExecProductionPeriod_OneOpenPerPoste")
                .IsUnique()
                .HasFilter("([EndedAt] IS NULL AND [SessionId] IS NOT NULL)");

            entity.HasIndex(e => new { e.ExecControleOfId, e.SectionId }, "UX_ExecProductionPeriod_OneOpenPerSection")
                .IsUnique()
                .HasFilter("([EndedAt] IS NULL AND [SectionId] IS NOT NULL)");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.EndReason)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.EndedAt).HasPrecision(3);
            entity.Property(e => e.PosteCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.StartedAt).HasPrecision(3);

            entity.HasOne(d => d.ExecControleOf).WithMany(p => p.ExecProductionPeriods)
                .HasForeignKey(d => d.ExecControleOfId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Exec_Prod__ExecC__731B1205");

            entity.HasOne(d => d.PosteCodeNavigation).WithMany(p => p.ExecProductionPeriods)
                .HasForeignKey(d => d.PosteCode)
                .HasConstraintName("FK__Exec_Prod__Poste__75035A77");

            entity.HasOne(d => d.Section).WithMany(p => p.ExecProductionPeriods)
                .HasForeignKey(d => d.SectionId)
                .HasConstraintName("FK__Exec_Prod__Secti__75F77EB0");

            entity.HasOne(d => d.Session).WithMany(p => p.ExecProductionPeriods)
                .HasForeignKey(d => d.SessionId)
                .HasConstraintName("FK__Exec_Prod__Sessi__740F363E");
        });

        modelBuilder.Entity<ExecRcPosteBilan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Exec_RC___3214EC07BFA68C72");

            entity.ToTable("Exec_RC_Poste_Bilan", tb => tb.HasTrigger("TR_ExecRCPosteBilan_ProtectClosed"));

            entity.HasIndex(e => new { e.ExecControleDocumentStatutId, e.Equipe }, "UQ_ExecRcPosteBilan_Equipe").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.DateSaisie).HasPrecision(3);
            entity.Property(e => e.Equipe)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.MatriculeOperateur)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.EquipeNavigation).WithMany(p => p.ExecRcPosteBilans)
                .HasForeignKey(d => d.Equipe)
                .HasConstraintName("FK__Exec_RC_P__Equip__4CC05EF3");

            entity.HasOne(d => d.ExecControleDocumentStatut).WithMany(p => p.ExecRcPosteBilans)
                .HasForeignKey(d => d.ExecControleDocumentStatutId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Exec_RC_P__ExecC__4BCC3ABA");

            entity.HasOne(d => d.MatriculeOperateurNavigation).WithMany(p => p.ExecRcPosteBilans)
                .HasForeignKey(d => d.MatriculeOperateur)
                .HasConstraintName("FK__Exec_RC_P__Matri__4EA8A765");
        });

        modelBuilder.Entity<ExecRcPosteReponse>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ExecRcPosteReponse");

            entity.ToTable("Exec_RC_Poste_Reponse", tb => tb.HasTrigger("TR_ExecRCPosteReponse_ProtectClosed"));

            entity.HasIndex(e => new { e.ExecControleDocumentStatutId, e.TrancheHoraire }, "UQ_ExecRcPosteReponse_Tranche").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.DateSaisie).HasPrecision(3);
            entity.Property(e => e.Equipe)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.MatriculeOperateur)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.TrancheHoraire)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.EquipeNavigation).WithMany(p => p.ExecRcPosteReponses)
                .HasForeignKey(d => d.Equipe)
                .HasConstraintName("FK__Exec_RC_P__Equip__451F3D2B");

            entity.HasOne(d => d.ExecControleDocumentStatut).WithMany(p => p.ExecRcPosteReponses)
                .HasForeignKey(d => d.ExecControleDocumentStatutId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ExecRcPosteReponse_Statut");

            entity.HasOne(d => d.MatriculeOperateurNavigation).WithMany(p => p.ExecRcPosteReponses)
                .HasForeignKey(d => d.MatriculeOperateur)
                .HasConstraintName("FK__Exec_RC_P__Matri__46136164");
        });

        modelBuilder.Entity<ExecRegistreTracabilite>(entity =>
        {
            entity.ToTable("ExecRegistreTracabilite", tb =>
                {
                    tb.HasTrigger("TR_ExecRegistreTracabilite_Consistency");
                    tb.HasTrigger("TR_ExecRegistreTracabilite_ProtectClosed");
                });

            entity.HasIndex(e => new { e.ExecControleOfId, e.PosteCode, e.DateHeure }, "IX_ExecRegistreTracabilite_Poste");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CancellationReason)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.CancelledAt).HasPrecision(3);
            entity.Property(e => e.CancelledBy)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Corps)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.DateHeure)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Equipe)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.MatriculeOperateur)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.PosteCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Version).HasDefaultValue(1);
            entity.Property(e => e.Volant)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.CancelledByNavigation).WithMany(p => p.ExecRegistreTracabiliteCancelledByNavigations)
                .HasForeignKey(d => d.CancelledBy)
                .HasConstraintName("FK__ExecRegis__Cance__573DED66");

            entity.HasOne(d => d.EquipeNavigation).WithMany(p => p.ExecRegistreTracabilites)
                .HasForeignKey(d => d.Equipe)
                .HasConstraintName("FK__ExecRegis__Equip__5555A4F4");

            entity.HasOne(d => d.ExecControleOf).WithMany(p => p.ExecRegistreTracabilites)
                .HasForeignKey(d => d.ExecControleOfId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ExecRegistreTracabilite_ExecControleOf");

            entity.HasOne(d => d.MatriculeOperateurNavigation).WithMany(p => p.ExecRegistreTracabiliteMatriculeOperateurNavigations)
                .HasForeignKey(d => d.MatriculeOperateur)
                .HasConstraintName("FK__ExecRegis__Matri__5649C92D");

            entity.HasOne(d => d.Session).WithMany(p => p.ExecRegistreTracabilites)
                .HasForeignKey(d => d.SessionId)
                .HasConstraintName("FK__ExecRegis__Sessi__546180BB");

            entity.HasOne(d => d.ExecControleOfPoste).WithMany(p => p.ExecRegistreTracabilites)
                .HasPrincipalKey(p => new { p.ExecControleOfId, p.PosteCode })
                .HasForeignKey(d => new { d.ExecControleOfId, d.PosteCode })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ExecRegistreTracabilite_OfPoste");
        });

        modelBuilder.Entity<ExecRegistreTracabiliteComposant>(entity =>
        {
            entity.ToTable("ExecRegistreTracabiliteComposant");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CodeArticle)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.DesignationComposant)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.LotSelectionne)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.CodeArticleNavigation).WithMany(p => p.ExecRegistreTracabiliteComposants)
                .HasForeignKey(d => d.CodeArticle)
                .HasConstraintName("FK__ExecRegis__CodeA__5EDF0F2E");

            entity.HasOne(d => d.RegistreTracabilite).WithMany(p => p.ExecRegistreTracabiliteComposants)
                .HasForeignKey(d => d.RegistreTracabiliteId)
                .HasConstraintName("FK_ExecRegistreTracabiliteComposant_ExecRegistreTracabilite");
        });

        modelBuilder.Entity<ExecSessionPost>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Exec_Ses__3214EC07897FC1AD");

            entity.ToTable("Exec_SessionPost", tb => tb.HasTrigger("TR_ExecSessionPost_Consistency"));

            entity.HasIndex(e => new { e.SessionId, e.PosteCode }, "UQ_ExecSessionPost").IsUnique();

            entity.HasIndex(e => new { e.ExecControleOfId, e.PosteCode }, "UX_ExecSessionPost_OneTeamPerPoste")
                .IsUnique()
                .HasFilter("([EndedAt] IS NULL)");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.ClosingRequestedAt).HasPrecision(3);
            entity.Property(e => e.EndedAt).HasPrecision(3);
            entity.Property(e => e.EndedBy)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.PosteCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.StartedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.StartedBy)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.EndedByNavigation).WithMany(p => p.ExecSessionPostEndedByNavigations)
                .HasForeignKey(d => d.EndedBy)
                .HasConstraintName("FK__Exec_Sess__Ended__4FD1D5C8");

            entity.HasOne(d => d.ExecControleOf).WithMany(p => p.ExecSessionPosts)
                .HasForeignKey(d => d.ExecControleOfId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Exec_Sess__ExecC__4C0144E4");

            entity.HasOne(d => d.PosteCodeNavigation).WithMany(p => p.ExecSessionPosts)
                .HasForeignKey(d => d.PosteCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Exec_Sess__Poste__4CF5691D");

            entity.HasOne(d => d.Session).WithMany(p => p.ExecSessionPosts)
                .HasForeignKey(d => d.SessionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Exec_Sess__Sessi__4B0D20AB");

            entity.HasOne(d => d.StartedByNavigation).WithMany(p => p.ExecSessionPostStartedByNavigations)
                .HasForeignKey(d => d.StartedBy)
                .HasConstraintName("FK__Exec_Sess__Start__4EDDB18F");
        });

        modelBuilder.Entity<ExecVerifMachineReponse>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ExecVerifMachineReponse");

            entity.ToTable("Exec_VerifMachine_Reponse", tb => tb.HasTrigger("TR_ExecVerifMachineReponse_ProtectClosed"));

            entity.HasIndex(e => e.ExecControleDocumentStatutId, "IX_ExecVerifMachineReponse_Statut");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.DateExecution).HasPrecision(3);
            entity.Property(e => e.FuiteAffichee).HasColumnType("decimal(10, 3)");
            entity.Property(e => e.MatriculeOperateur)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Observation)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.PressionEntree).HasColumnType("decimal(10, 3)");

            entity.HasOne(d => d.DocumentVerifMachineEcheance).WithMany(p => p.ExecVerifMachineReponses)
                .HasForeignKey(d => d.DocumentVerifMachineEcheanceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ExecVerifMachineReponse_Echeance");

            entity.HasOne(d => d.ExecControleDocumentStatut).WithMany(p => p.ExecVerifMachineReponses)
                .HasForeignKey(d => d.ExecControleDocumentStatutId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ExecVerifMachineReponse_Statut");

            entity.HasOne(d => d.MatriculeOperateurNavigation).WithMany(p => p.ExecVerifMachineReponses)
                .HasForeignKey(d => d.MatriculeOperateur)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Exec_Veri__Matri__3D7E1B63");
        });

        modelBuilder.Entity<ExecutionDocumentType>(entity =>
        {
            entity.HasKey(e => e.Code).HasName("PK__Executio__A25C5AA66EDA16B6");

            entity.ToTable("ExecutionDocumentType", tb => tb.HasTrigger("trg_no_del_ExecutionDocumentType"));

            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.DocumentTypeCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Label)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.DocumentTypeCodeNavigation).WithMany(p => p.ExecutionDocumentTypes)
                .HasForeignKey(d => d.DocumentTypeCode)
                .HasConstraintName("FK_ExecutionDocumentType_TypeDocument");
        });

        modelBuilder.Entity<FamilleProduitFini>(entity =>
        {
            entity.HasKey(e => e.Code).HasName("PK__FamilleP__A25C5AA6722C70CF");

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
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__FamillePr__TypeR__5CD6CB2B");
        });

        modelBuilder.Entity<InspectionLevel>(entity =>
        {
            entity.HasKey(e => e.Code).HasName("PK__Inspecti__A25C5AA641BEB2E0");

            entity.ToTable("InspectionLevel", tb => tb.HasTrigger("trg_no_del_InspectionLevel"));

            entity.Property(e => e.Code)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Label)
                .HasMaxLength(60)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Instrument>(entity =>
        {
            entity.HasKey(e => e.CodeInstrument).HasName("PK__Instrume__E6E43505F8023D46");

            entity.ToTable("Instrument", tb => tb.HasTrigger("trg_no_del_Instrument"));

            entity.Property(e => e.CodeInstrument)
                .HasMaxLength(40)
                .IsUnicode(false);
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.Designation)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<JournalConnexion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__JournalC__3214EC07E566D9C9");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.Action)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.DateAction)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Details)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Matricule)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Machine>(entity =>
        {
            entity.HasKey(e => e.CodeMachine).HasName("PK__Machine__50D6760F196BFC74");

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
            entity.Property(e => e.VerifModele)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.OperationCodeNavigation).WithMany(p => p.Machines)
                .HasForeignKey(d => d.OperationCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Machine__Operati__7C1A6C5A");

            entity.HasMany(d => d.RefFamilleCorps).WithMany(p => p.MachineCodes)
                .UsingEntity<Dictionary<string, object>>(
                    "MachineFamilleCorp",
                    r => r.HasOne<RefFamilleCorp>().WithMany()
                        .HasForeignKey("RefFamilleCorpsId")
                        .HasConstraintName("FK__Machine_F__RefFa__09746778"),
                    l => l.HasOne<Machine>().WithMany()
                        .HasForeignKey("MachineCode")
                        .HasConstraintName("FK__Machine_F__Machi__0880433F"),
                    j =>
                    {
                        j.HasKey("MachineCode", "RefFamilleCorpsId").HasName("PK__Machine___74270A8A0E2EF429");
                        j.ToTable("Machine_FamilleCorps");
                        j.IndexerProperty<string>("MachineCode")
                            .HasMaxLength(30)
                            .IsUnicode(false);
                    });
        });

        modelBuilder.Entity<MagExpeditionBl>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Mag_Expe__3214EC07EA95A36A");

            entity.ToTable("Mag_ExpeditionBL", tb => tb.HasTrigger("TR_MagExpeditionBL_Single"));

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.DateDebut)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.DateFin).HasPrecision(3);
            entity.Property(e => e.MatriculeMagasinier)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.NumeroBl)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("NumeroBL");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.Statut)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("EN_COURS");

            entity.HasOne(d => d.MatriculeMagasinierNavigation).WithMany(p => p.MagExpeditionBls)
                .HasForeignKey(d => d.MatriculeMagasinier)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Mag_Exped__Matri__1E3A7A34");

            entity.HasOne(d => d.NumeroBlNavigation).WithMany(p => p.MagExpeditionBls)
                .HasForeignKey(d => d.NumeroBl)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Mag_Exped__Numer__1D4655FB");
        });

        modelBuilder.Entity<MagExpeditionBlScanOf>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Mag_Expe__3214EC0728FE0DAC");

            entity.ToTable("Mag_ExpeditionBL_ScanOF");

            entity.HasIndex(e => new { e.ExpeditionBlid, e.NumeroOfscanne }, "UX_MagExpeditionBLScanOF_OF").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.DateScan)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.ExpeditionBlid).HasColumnName("ExpeditionBLId");
            entity.Property(e => e.NumeroOfscanne)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("NumeroOFScanne");

            entity.HasOne(d => d.ExpeditionBl).WithMany(p => p.MagExpeditionBlScanOfs)
                .HasForeignKey(d => d.ExpeditionBlid)
                .HasConstraintName("FK__Mag_Exped__Exped__24E777C3");

            entity.HasOne(d => d.NumeroOfscanneNavigation).WithMany(p => p.MagExpeditionBlScanOfs)
                .HasForeignKey(d => d.NumeroOfscanne)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Mag_Exped__Numer__25DB9BFC");
        });

        modelBuilder.Entity<MagPreparationOf>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Mag_Prep__3214EC070DAB4883");

            entity.ToTable("Mag_PreparationOF", tb => tb.HasTrigger("TR_MagPreparationOF_SingleActive"));

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.DateDebut)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.DateFin).HasPrecision(3);
            entity.Property(e => e.IncompleteReason).HasMaxLength(300);
            entity.Property(e => e.MatriculeMagasinier)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.NumeroOf)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("NumeroOF");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.Statut)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("EN_COURS");

            entity.HasOne(d => d.MatriculeMagasinierNavigation).WithMany(p => p.MagPreparationOfs)
                .HasForeignKey(d => d.MatriculeMagasinier)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Mag_Prepa__Matri__0FEC5ADD");

            entity.HasOne(d => d.NumeroOfNavigation).WithMany(p => p.MagPreparationOfs)
                .HasForeignKey(d => d.NumeroOf)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Mag_Prepa__Numer__0EF836A4");
        });

        modelBuilder.Entity<MagPreparationOfLot>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Mag_Prep__3214EC07D89C8196");

            entity.ToTable("Mag_PreparationOF_Lot");

            entity.HasIndex(e => new { e.PreparationOfid, e.CodeArticle, e.NumeroLotScanne }, "UX_MagPreparationOFLot_Lot").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CodeArticle)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.DateScan)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.NumeroLotScanne)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PreparationOfid).HasColumnName("PreparationOFId");
            entity.Property(e => e.QualityControlReport)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Quantite).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.ScannedContent).HasMaxLength(300);

            entity.HasOne(d => d.CodeArticleNavigation).WithMany(p => p.MagPreparationOfLots)
                .HasForeignKey(d => d.CodeArticle)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Mag_Prepa__CodeA__178D7CA5");

            entity.HasOne(d => d.PreparationOf).WithMany(p => p.MagPreparationOfLots)
                .HasForeignKey(d => d.PreparationOfid)
                .HasConstraintName("FK__Mag_Prepa__Prepa__1699586C");
        });

        modelBuilder.Entity<MfgheadOrdreFabrication>(entity =>
        {
            entity.HasKey(e => e.NumeroOf).HasName("PK__MFGHEAD___C6A65F30AA5E2D2C");

            entity.ToTable("MFGHEAD_OrdreFabrication");

            entity.HasIndex(e => e.CodeArticle, "IX_MFGHEAD_CodeArticle");

            entity.Property(e => e.NumeroOf)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("NumeroOF");
            entity.Property(e => e.CodeArticle)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.DateDebut).HasPrecision(3);
            entity.Property(e => e.DateFin).HasPrecision(3);
            entity.Property(e => e.QuantiteLancee).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.QuantitePrevue).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.QuantiteReelle).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.StatutOf)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("StatutOF");

            entity.HasOne(d => d.CodeArticleNavigation).WithMany(p => p.MfgheadOrdreFabrications)
                .HasForeignKey(d => d.CodeArticle)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__MFGHEAD_O__CodeA__6FB49575");
        });

        modelBuilder.Entity<MfgmatBesoinOf>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__MFGMAT_B__3214EC071EC2C4FE");

            entity.ToTable("MFGMAT_BesoinOF");

            entity.Property(e => e.CodeArticle)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.NumeroOf)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("NumeroOF");
            entity.Property(e => e.QuantiteRequise).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.QuantiteSortie).HasColumnType("decimal(18, 3)");

            entity.HasOne(d => d.CodeArticleNavigation).WithMany(p => p.MfgmatBesoinOfs)
                .HasForeignKey(d => d.CodeArticle)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__MFGMAT_Be__CodeA__76619304");

            entity.HasOne(d => d.NumeroOfNavigation).WithMany(p => p.MfgmatBesoinOfs)
                .HasForeignKey(d => d.NumeroOf)
                .HasConstraintName("FK__MFGMAT_Be__Numer__756D6ECB");
        });

        modelBuilder.Entity<MoyenControle>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__MoyenCon__3214EC07464778CD");

            entity.ToTable("MoyenControle", tb => tb.HasTrigger("trg_no_del_MoyenControle"));

            entity.HasIndex(e => e.Code, "UQ__MoyenCon__A25C5AA74C980969").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
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
            entity.HasKey(e => e.Code).HasName("PK__NatureAr__A25C5AA654CA77E1");

            entity.ToTable("NatureArticle", tb => tb.HasTrigger("trg_no_del_NatureArticle"));

            entity.Property(e => e.Code)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.Libelle)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<NatureArticleOperation>(entity =>
        {
            entity.HasKey(e => new { e.NatureArticleCode, e.OperationCode }).HasName("PK__NatureAr__6403AE777AB5B99C");

            entity.ToTable("NatureArticle_Operation");

            entity.Property(e => e.NatureArticleCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.OperationCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.OrdreGamme).HasDefaultValue(1);

            entity.HasOne(d => d.NatureArticleCodeNavigation).WithMany(p => p.NatureArticleOperations)
                .HasForeignKey(d => d.NatureArticleCode)
                .HasConstraintName("FK__NatureArt__Natur__6EF57B66");

            entity.HasOne(d => d.OperationCodeNavigation).WithMany(p => p.NatureArticleOperations)
                .HasForeignKey(d => d.OperationCode)
                .HasConstraintName("FK__NatureArt__Opera__6FE99F9F");
        });

        modelBuilder.Entity<Nqa>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__NQA__3214EC07E1E1D28B");

            entity.ToTable("NQA", tb => tb.HasTrigger("trg_no_del_NQA"));

            entity.HasIndex(e => e.ValeurNqa, "UQ__NQA__1DA3E24802DAF8CE").IsUnique();

            entity.Property(e => e.ValeurNqa)
                .HasColumnType("decimal(6, 3)")
                .HasColumnName("ValeurNQA");
        });

        modelBuilder.Entity<Operation>(entity =>
        {
            entity.HasKey(e => e.Code).HasName("PK__Operatio__A25C5AA6C65BC2B5");

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
            entity.HasKey(e => e.Id).HasName("PK__Periodic__3214EC0738F2E20C");

            entity.ToTable("Periodicite", tb => tb.HasTrigger("trg_no_del_Periodicite"));

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.Unite)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<PeriodiciteMachine>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Periodic__3214EC07BF71A583");

            entity.ToTable("PeriodiciteMachine");

            entity.HasIndex(e => e.Code, "UQ__Periodic__A25C5AA7A6DE0006").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.Code)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.ControlMoment)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Libelle)
                .HasMaxLength(200)
                .IsUnicode(false);
        });

        modelBuilder.Entity<PieceReference>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__PieceRef__3214EC07D11F5427");

            entity.ToTable("PieceReference", tb => tb.HasTrigger("trg_no_del_PieceReference"));

            entity.HasIndex(e => e.Code, "UQ__PieceRef__A25C5AA7D35E5F7B").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.Code)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Designation)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.TypePiece)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.FamilleCorps).WithMany(p => p.PieceReferences)
                .HasForeignKey(d => d.FamilleCorpsId)
                .HasConstraintName("FK__PieceRefe__Famil__2739D489");

            entity.HasOne(d => d.TypePieceNavigation).WithMany(p => p.PieceReferences)
                .HasForeignKey(d => d.TypePiece)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PieceRefe__TypeP__2645B050");
        });

        modelBuilder.Entity<PosteTravail>(entity =>
        {
            entity.HasKey(e => e.CodePoste).HasName("PK__PosteTra__4045446BE1904DD4");

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
                        .HasConstraintName("FK__PosteTrav__CodeM__0D44F85C"),
                    l => l.HasOne<PosteTravail>().WithMany()
                        .HasForeignKey("CodePoste")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__PosteTrav__CodeP__0C50D423"),
                    j =>
                    {
                        j.HasKey("CodePoste", "CodeMachine").HasName("PK__PosteTra__A548230B1F505DE7");
                        j.ToTable("PosteTravail_Machine");
                        j.IndexerProperty<string>("CodePoste")
                            .HasMaxLength(30)
                            .IsUnicode(false);
                        j.IndexerProperty<string>("CodeMachine")
                            .HasMaxLength(30)
                            .IsUnicode(false);
                    });
        });

        modelBuilder.Entity<PreassignedRole>(entity =>
        {
            entity.HasKey(e => e.Matricule).HasName("PK__Preassig__0FB9FB424AC7921E");

            entity.ToTable("PreassignedRole");

            entity.Property(e => e.Matricule)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.AssignedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.AssignedBy)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.RoleCode)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.MatriculeNavigation).WithOne(p => p.PreassignedRole)
                .HasForeignKey<PreassignedRole>(d => d.Matricule)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Preassign__Matri__45BE5BA9");

            entity.HasOne(d => d.RoleCodeNavigation).WithMany(p => p.PreassignedRoles)
                .HasForeignKey(d => d.RoleCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Preassign__RoleC__46B27FE2");
        });

        modelBuilder.Entity<ProduitFini>(entity =>
        {
            entity.HasKey(e => e.CodeArticle).HasName("PK__ProduitF__32384FB099FAC2FD");

            entity.ToTable("ProduitFini");

            entity.HasIndex(e => e.FamilleProduitFiniCode, "IX_ProduitFini_FamilleCode");

            entity.Property(e => e.CodeArticle)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.ArticleType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasComputedColumnSql("(CONVERT([varchar](20),'PF'))", true);
            entity.Property(e => e.FamilleProduitFiniCode)
                .HasMaxLength(30)
                .IsUnicode(false);

            entity.HasOne(d => d.FamilleProduitFiniCodeNavigation).WithMany(p => p.ProduitFinis)
                .HasForeignKey(d => d.FamilleProduitFiniCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ProduitFi__Famil__6442E2C9");

            entity.HasOne(d => d.Article).WithMany(p => p.ProduitFinis)
                .HasPrincipalKey(p => new { p.CodeArticle, p.TypeArticle })
                .HasForeignKey(d => new { d.CodeArticle, d.ArticleType })
                .HasConstraintName("FK_ProduitFini_FinishedProductArticle");
        });

        modelBuilder.Entity<RefCaracteristique>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Ref_Cara__3214EC0745816BC3");

            entity.ToTable("Ref_Caracteristique", tb => tb.HasTrigger("trg_no_del_Ref_Caracteristique"));

            entity.HasIndex(e => e.LibelleNormalise, "IX_Ref_Caracteristique_Normalise");

            entity.HasIndex(e => e.LibelleNormalise, "UQ__Ref_Cara__5C5A3C6E22533379").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.Libelle)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.LibelleNormalise)
                .HasMaxLength(250)
                .IsUnicode(false);

            entity.HasOne(d => d.TypeCaracteristique).WithMany(p => p.RefCaracteristiques)
                .HasForeignKey(d => d.TypeCaracteristiqueId)
                .HasConstraintName("FK__Ref_Carac__TypeC__2CF2ADDF");
        });

        modelBuilder.Entity<RefFamilleCorp>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Ref_Fami__3214EC073AC7D029");

            entity.ToTable("Ref_FamilleCorps", tb => tb.HasTrigger("trg_no_del_Ref_FamilleCorps"));

            entity.HasIndex(e => e.Code, "UQ__Ref_Fami__A25C5AA73E104906").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
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
            entity.HasKey(e => e.Id).HasName("PK__Ref_Form__3214EC072BCC5379");

            entity.ToTable("Ref_Formulaire");

            entity.HasIndex(e => new { e.CodeReference, e.Version }, "UQ__Ref_Form__4F92504B2CE8049F").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CodeReference)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.CreeLe)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.CreePar)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Designation)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.FamilleProduitFiniCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.ModifieLe).HasPrecision(3);
            entity.Property(e => e.ModifiePar)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.PosteCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Statut)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("ACTIF");

            entity.HasOne(d => d.CreeParNavigation).WithMany(p => p.RefFormulaireCreeParNavigations)
                .HasForeignKey(d => d.CreePar)
                .HasConstraintName("FK__Ref_Formu__CreeP__15DA3E5D");

            entity.HasOne(d => d.FamilleProduitFiniCodeNavigation).WithMany(p => p.RefFormulaires)
                .HasForeignKey(d => d.FamilleProduitFiniCode)
                .HasConstraintName("FK__Ref_Formu__Famil__19AACF41");

            entity.HasOne(d => d.ModifieParNavigation).WithMany(p => p.RefFormulaireModifieParNavigations)
                .HasForeignKey(d => d.ModifiePar)
                .HasConstraintName("FK__Ref_Formu__Modif__16CE6296");

            entity.HasOne(d => d.PosteCodeNavigation).WithMany(p => p.RefFormulaires)
                .HasForeignKey(d => d.PosteCode)
                .HasConstraintName("FK__Ref_Formu__Poste__18B6AB08");
        });

        modelBuilder.Entity<RefFormulaireColonneDef>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Ref_Form__3214EC07EBC94CD5");

            entity.ToTable("Ref_Formulaire_ColonneDef", tb =>
                {
                    tb.HasTrigger("TR_RefFormulaireColonneDef_TypeLocked");
                    tb.HasTrigger("trg_no_del_Ref_Formulaire_ColonneDef");
                });

            entity.HasIndex(e => new { e.FormulaireId, e.CleColonne }, "UQ__Ref_Form__EC7AEAEB9E3484AE").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.CleColonne)
                .HasMaxLength(60)
                .IsUnicode(false);
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
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("TEXT");

            entity.HasOne(d => d.Formulaire).WithMany(p => p.RefFormulaireColonneDefs)
                .HasForeignKey(d => d.FormulaireId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Ref_Formu__Formu__1E6F845E");

            entity.HasOne(d => d.TypeValeurNavigation).WithMany(p => p.RefFormulaireColonneDefs)
                .HasForeignKey(d => d.TypeValeur)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Ref_Formu__TypeV__2057CCD0");
        });

        modelBuilder.Entity<RefFormulaireEquipe>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Ref_Form__3214EC07FA39B752");

            entity.ToTable("Ref_Formulaire_Equipe", tb => tb.HasTrigger("trg_no_del_Ref_Formulaire_Equipe"));

            entity.HasIndex(e => new { e.FormulaireId, e.NomEquipe }, "UQ__Ref_Form__DE54B0529EA6FA75").IsUnique();

            entity.HasIndex(e => new { e.FormulaireId, e.TeamCode }, "UX_RefFormulaireEquipe_Team")
                .IsUnique()
                .HasFilter("([Actif]=(1))");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.NomEquipe)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.TeamCode)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.Formulaire).WithMany(p => p.RefFormulaireEquipes)
                .HasForeignKey(d => d.FormulaireId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Ref_Formu__Formu__2EA5EC27");

            entity.HasOne(d => d.TeamCodeNavigation).WithMany(p => p.RefFormulaireEquipes)
                .HasForeignKey(d => d.TeamCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Ref_Formu__TeamC__2F9A1060");
        });

        modelBuilder.Entity<RefIso2859Echantillonnage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Ref_ISO2__3214EC0799671B53");

            entity.ToTable("Ref_ISO2859_Echantillonnage");

            entity.HasIndex(e => e.CodeLettre, "UQ__Ref_ISO2__4A778AF74804008D").IsUnique();

            entity.Property(e => e.CodeLettre)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
        });

        modelBuilder.Entity<RefIso2859LettresCode>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Ref_ISO2__3214EC0791D9DF4B");

            entity.ToTable("Ref_ISO2859_LettresCode");

            entity.Property(e => e.CodeLettre)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.NiveauControle)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.NiveauControleNavigation).WithMany(p => p.RefIso2859LettresCodes)
                .HasForeignKey(d => d.NiveauControle)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Ref_ISO28__Nivea__3552E9B6");
        });

        modelBuilder.Entity<RefMoyenDetection>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Ref_Moye__3214EC07C5E13600");

            entity.ToTable("Ref_MoyenDetection", tb => tb.HasTrigger("trg_no_del_Ref_MoyenDetection"));

            entity.HasIndex(e => e.Code, "UQ__Ref_Moye__A25C5AA7FCA5BD8E").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Designation)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<ReferencePieceRole>(entity =>
        {
            entity.HasKey(e => e.Code).HasName("PK__Referenc__A25C5AA64BD3DFEA");

            entity.ToTable("ReferencePieceRole", tb => tb.HasTrigger("trg_no_del_ReferencePieceRole"));

            entity.Property(e => e.Code)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Label)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RefreshT__3214EC076B9010F2");

            entity.HasIndex(e => e.TokenHash, "UQ__RefreshT__BCB33F925EE7010F").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.DateCreation)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.DateExpiration).HasPrecision(3);
            entity.Property(e => e.EstRevoque).HasDefaultValue(false);
            entity.Property(e => e.JwtId)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.TokenHash)
                .HasMaxLength(64)
                .IsUnicode(false)
                .IsFixedLength();

            entity.HasOne(d => d.Utilisateur).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.UtilisateurId)
                .HasConstraintName("FK__RefreshTo__Utili__4C6B5938");
        });

        modelBuilder.Entity<RisqueDefaut>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RisqueDe__3214EC072AF08A44");

            entity.ToTable("RisqueDefaut", tb => tb.HasTrigger("trg_no_del_RisqueDefaut"));

            entity.HasIndex(e => e.CodeDefaut, "UQ__RisqueDe__2EF8734389ABD8AF").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.CodeDefaut)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.LibelleDefaut)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<SamplingPlan>(entity =>
        {
            entity.HasKey(e => e.DocumentId).HasName("PK__Sampling__1ABEEF0F2450ACE2");

            entity.ToTable("SamplingPlan");

            entity.Property(e => e.DocumentId).ValueGeneratedNever();
            entity.Property(e => e.InspectionLevelCode)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.InspectionSeverity)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.SamplingType)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.AcceptableQualityLevel).WithMany(p => p.SamplingPlans)
                .HasForeignKey(d => d.AcceptableQualityLevelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SamplingP__Accep__7720AD13");

            entity.HasOne(d => d.Document).WithOne(p => p.SamplingPlan)
                .HasForeignKey<SamplingPlan>(d => d.DocumentId)
                .HasConstraintName("FK__SamplingP__Docum__73501C2F");

            entity.HasOne(d => d.InspectionLevelCodeNavigation).WithMany(p => p.SamplingPlans)
                .HasForeignKey(d => d.InspectionLevelCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SamplingP__Inspe__74444068");
        });

        modelBuilder.Entity<Sdelivery>(entity =>
        {
            entity.HasKey(e => e.NumeroBl).HasName("PK__SDELIVER__C664DCCD5D898C72");

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

        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasKey(e => e.Code).HasName("PK__Team__A25C5AA6F7EBAE39");

            entity.ToTable("Team", tb => tb.HasTrigger("trg_no_del_Team"));

            entity.Property(e => e.Code)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.Label)
                .HasMaxLength(60)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TypeCaracteristique>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TypeCara__3214EC076C80036C");

            entity.ToTable("TypeCaracteristique", tb => tb.HasTrigger("trg_no_del_TypeCaracteristique"));

            entity.HasIndex(e => e.Code, "UQ__TypeCara__A25C5AA762ED08FD").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
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
            entity.HasKey(e => e.Id).HasName("PK__TypeCont__3214EC0736BB446B");

            entity.ToTable("TypeControle", tb => tb.HasTrigger("trg_no_del_TypeControle"));

            entity.HasIndex(e => e.Code, "UQ__TypeCont__A25C5AA78C7823F1").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
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
            entity.HasKey(e => e.Code).HasName("PK__TypeDocu__A25C5AA63A906813");

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
            entity.HasKey(e => e.Code).HasName("PK__TypeRobi__A25C5AA6EE45E2F3");

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
            entity.HasKey(e => e.Id).HasName("PK__TypeSect__3214EC0787E6C8E0");

            entity.ToTable("TypeSection", tb => tb.HasTrigger("trg_no_del_TypeSection"));

            entity.HasIndex(e => e.TypeNormalise, "UQ__TypeSect__70B9DB9E2BFC9044").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.Libelle)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.TypeNormalise)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<UtilisateursApp>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Utilisat__3214EC07C701F1BC");

            entity.ToTable("UtilisateursApp");

            entity.HasIndex(e => e.Matricule, "IX_UtilisateursApp_Matricule");

            entity.HasIndex(e => e.Matricule, "UQ__Utilisat__0FB9FB43F2E5A84D").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__Utilisat__A9D10534C1C7BAB0").IsUnique();

            entity.HasIndex(e => e.ActivationTokenHash, "UX_UtilisateursApp_ActivationTokenHash")
                .IsUnique()
                .HasFilter("([ActivationTokenHash] IS NOT NULL)");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.AccountStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("PENDING_APPROVAL");
            entity.Property(e => e.ActivationTokenExpiresAt).HasPrecision(3);
            entity.Property(e => e.ActivationTokenHash)
                .HasMaxLength(64)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.CodeRecuperationHash)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.DateCreation)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.DateDerniereConnexion).HasPrecision(3);
            entity.Property(e => e.DateExpirationCode).HasPrecision(3);
            entity.Property(e => e.Email)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.IntituleMetier)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.LockoutEndsAt).HasPrecision(3);
            entity.Property(e => e.Matricule)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.MotDePasseHash)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.NomComplet)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ReviewComment)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.ReviewedAt).HasPrecision(3);
            entity.Property(e => e.ReviewedBy)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.RoleApp)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.SecurityStamp).HasDefaultValueSql("(newid())");

            entity.HasOne(d => d.MatriculeNavigation).WithOne(p => p.UtilisateursApp)
                .HasForeignKey<UtilisateursApp>(d => d.Matricule)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Utilisate__Matri__3A4CA8FD");

            entity.HasOne(d => d.RoleAppNavigation).WithMany(p => p.UtilisateursApps)
                .HasForeignKey(d => d.RoleApp)
                .HasConstraintName("FK__Utilisate__RoleA__3B40CD36");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
