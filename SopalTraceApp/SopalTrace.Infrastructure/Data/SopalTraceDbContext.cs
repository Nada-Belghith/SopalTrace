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

    public virtual DbSet<ExternalDocument> ExternalDocuments { get; set; }

    public virtual DbSet<ExternalDocumentType> ExternalDocumentTypes { get; set; }

    public virtual DbSet<FamilleProduitFini> FamilleProduitFinis { get; set; }

    public virtual DbSet<FormCodeChange> FormCodeChanges { get; set; }

    public virtual DbSet<FormRole> FormRoles { get; set; }

    public virtual DbSet<GeneratedDocument> GeneratedDocuments { get; set; }

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

    public virtual DbSet<Porder> Porders { get; set; }

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

    public virtual DbSet<ReportTemplate> ReportTemplates { get; set; }

    public virtual DbSet<ReportTemplateAssignment> ReportTemplateAssignments { get; set; }

    public virtual DbSet<ReportTemplateInitialForm> ReportTemplateInitialForms { get; set; }

    public virtual DbSet<ReportTemplateRequest> ReportTemplateRequests { get; set; }

    public virtual DbSet<ReportTemplateVersion> ReportTemplateVersions { get; set; }

    public virtual DbSet<RisqueDefaut> RisqueDefauts { get; set; }

    public virtual DbSet<SamplingPlan> SamplingPlans { get; set; }

    public virtual DbSet<Sdelivery> Sdeliveries { get; set; }

    public virtual DbSet<Sorder> Sorders { get; set; }

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
            entity.HasKey(e => e.Id).HasName("PK__Alertes__3214EC0709F27C3F");

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
            entity.HasKey(e => e.Code).HasName("PK__AppRole__A25C5AA63A814868");

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
            entity.HasKey(e => e.CodeArticle).HasName("PK__Article__32384FB035ADA4AE");

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
            entity.HasKey(e => new { e.Codfic0, e.Zone0, e.Ident10, e.Langue0 }).HasName("PK__ATEXTRA__4F21B2DB2B288001");

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
            entity.HasKey(e => e.Id).HasName("PK__AuditLog__3214EC07742F3D1F");

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
            entity.HasKey(e => e.Usr0).HasName("PK__AUTILIS__0812AE69EFB0368E");

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
            entity.HasKey(e => new { e.ArticleParent, e.CodeComposant, e.CodeAlternative }).HasName("PK__BOMD_Nom__2710E8513F704FCF");

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
            entity.HasKey(e => e.Id).HasName("PK__ColumnAl__3214EC0704C70AA2");

            entity.ToTable("ColumnAllowedValue");

            entity.HasIndex(e => new { e.ColumnDefinitionId, e.Value }, "UQ__ColumnAl__FDA6B6281DBDD17A").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.Value).HasMaxLength(200);

            entity.HasOne(d => d.ColumnDefinition).WithMany(p => p.ColumnAllowedValues)
                .HasForeignKey(d => d.ColumnDefinitionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ColumnAll__Colum__3BFFE745");
        });

        modelBuilder.Entity<ColumnValueType>(entity =>
        {
            entity.HasKey(e => e.Code).HasName("PK__ColumnVa__A25C5AA6825ABC5C");

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
            entity.HasKey(e => e.Id).HasName("PK__Document__3214EC072E6FF5DE");

            entity.ToTable("Document_Entete", tb =>
                {
                    tb.HasTrigger("TR_DocumentEntete_CreationOrder");
                    tb.HasTrigger("TR_DocumentEntete_FormInProgress");
                    tb.HasTrigger("TR_DocumentEntete_SingleActive");
                });

            entity.HasIndex(e => e.CodeArticleSageVersionne, "IX_Document_Entete_Article").HasFilter("([CodeArticleSageVersionne] IS NOT NULL)");

            entity.HasIndex(e => e.MachineCode, "IX_Document_Entete_Machine").HasFilter("([MachineCode] IS NOT NULL)");

            entity.HasIndex(e => new { e.TypeDocumentCode, e.Statut }, "IX_Document_Entete_TypeStatut");

            entity.HasIndex(e => e.FormulaireId, "UX_DocumentEntete_GenericActiveForm")
                .IsUnique()
                .HasFilter("([Statut]='ACTIF' AND [Portee]='GENERIQUE' AND [TypeDocumentCode]<>'MODELE_FAB' AND [FormulaireId] IS NOT NULL)");

            entity.HasIndex(e => e.CreationOrder, "UX_Document_Entete_CreationOrder").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CodeArticleSageVersionne)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.CreationOrder).HasDefaultValueSql("(NEXT VALUE FOR [dbo].[CreationOrderSequence])");
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
                .HasConstraintName("FK__Document___CreeP__5B78929E");

            entity.HasOne(d => d.DocumentSource).WithMany(p => p.InverseDocumentSource)
                .HasForeignKey(d => d.DocumentSourceId)
                .HasConstraintName("FK__Document___Docum__55BFB948");

            entity.HasOne(d => d.FamilleProduitFiniCodeNavigation).WithMany(p => p.DocumentEntetes)
                .HasForeignKey(d => d.FamilleProduitFiniCode)
                .HasConstraintName("FK__Document___Famil__5F492382");

            entity.HasOne(d => d.Formulaire).WithOne(p => p.DocumentEntete)
                .HasForeignKey<DocumentEntete>(d => d.FormulaireId)
                .HasConstraintName("FK__Document___Formu__5A846E65");

            entity.HasOne(d => d.MachineCodeNavigation).WithMany(p => p.DocumentEntetes)
                .HasForeignKey(d => d.MachineCode)
                .HasConstraintName("FK__Document___Machi__61316BF4");

            entity.HasOne(d => d.ModifieParNavigation).WithMany(p => p.DocumentEnteteModifieParNavigations)
                .HasForeignKey(d => d.ModifiePar)
                .HasConstraintName("FK__Document___Modif__5D60DB10");

            entity.HasOne(d => d.NatureArticleCodeNavigation).WithMany(p => p.DocumentEntetes)
                .HasForeignKey(d => d.NatureArticleCode)
                .HasConstraintName("FK__Document___Natur__5E54FF49");

            entity.HasOne(d => d.OperationCodeNavigation).WithMany(p => p.DocumentEntetes)
                .HasForeignKey(d => d.OperationCode)
                .HasConstraintName("FK__Document___Opera__59904A2C");

            entity.HasOne(d => d.PosteCodeNavigation).WithMany(p => p.DocumentEntetes)
                .HasForeignKey(d => d.PosteCode)
                .HasConstraintName("FK__Document___Poste__603D47BB");

            entity.HasOne(d => d.TypeDocumentCodeNavigation).WithMany(p => p.DocumentEntetes)
                .HasForeignKey(d => d.TypeDocumentCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Document___TypeD__52E34C9D");
        });

        modelBuilder.Entity<DocumentLigne>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Document__3214EC07B5E6F6B3");

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
                .HasConstraintName("FK__Document___Carac__7908F585");

            entity.HasOne(d => d.Entete).WithMany(p => p.DocumentLignes)
                .HasForeignKey(d => d.EnteteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Document___Entet__762C88DA");

            entity.HasOne(d => d.InstrumentCodeNavigation).WithMany(p => p.DocumentLignes)
                .HasForeignKey(d => d.InstrumentCode)
                .HasConstraintName("FK__Document___Instr__7CD98669");

            entity.HasOne(d => d.MachineCodeNavigation).WithMany(p => p.DocumentLignes)
                .HasForeignKey(d => d.MachineCode)
                .HasConstraintName("FK__Document___Machi__7FB5F314");

            entity.HasOne(d => d.MoyenControle).WithMany(p => p.DocumentLignes)
                .HasForeignKey(d => d.MoyenControleId)
                .HasConstraintName("FK__Document___Moyen__7BE56230");

            entity.HasOne(d => d.Periodicite).WithMany(p => p.DocumentLignes)
                .HasForeignKey(d => d.PeriodiciteId)
                .HasConstraintName("FK__Document___Perio__7DCDAAA2");

            entity.HasOne(d => d.RisqueDefaut).WithMany(p => p.DocumentLignes)
                .HasForeignKey(d => d.RisqueDefautId)
                .HasConstraintName("FK__Document___Risqu__00AA174D");

            entity.HasOne(d => d.Section).WithMany(p => p.DocumentLignes)
                .HasForeignKey(d => d.SectionId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Document___Secti__7720AD13");

            entity.HasOne(d => d.TypeCaracteristique).WithMany(p => p.DocumentLignes)
                .HasForeignKey(d => d.TypeCaracteristiqueId)
                .HasConstraintName("FK__Document___TypeC__79FD19BE");

            entity.HasOne(d => d.TypeControle).WithMany(p => p.DocumentLignes)
                .HasForeignKey(d => d.TypeControleId)
                .HasConstraintName("FK__Document___TypeC__7AF13DF7");
        });

        modelBuilder.Entity<DocumentLigneExtraColonne>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Document__3214EC0743372D16");

            entity.ToTable("Document_Ligne_ExtraColonne", tb => tb.HasTrigger("TR_DocumentLigneExtraColonne_Validate"));

            entity.HasIndex(e => new { e.LigneId, e.CleColonne }, "UQ__Document__B2068CF3F0B35331").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CleColonne)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.ValeurColonne).HasMaxLength(500);

            entity.HasOne(d => d.Ligne).WithMany(p => p.DocumentLigneExtraColonnes)
                .HasForeignKey(d => d.LigneId)
                .HasConstraintName("FK__Document___Ligne__084B3915");
        });

        modelBuilder.Entity<DocumentLineImage>(entity =>
        {
            entity.HasKey(e => e.LineId).HasName("PK__Document__2EAE65299D67AFC4");

            entity.ToTable("DocumentLineImage");

            entity.Property(e => e.LineId).ValueGeneratedNever();
            entity.Property(e => e.ContentType)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Line).WithOne(p => p.DocumentLineImage)
                .HasForeignKey<DocumentLineImage>(d => d.LineId)
                .HasConstraintName("FK__DocumentL__LineI__0C1BC9F9");
        });

        modelBuilder.Entity<DocumentSection>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Document__3214EC075D5CFDD7");

            entity.ToTable("Document_Section");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.LibelleSection)
                .HasMaxLength(300)
                .IsUnicode(false);

            entity.HasOne(d => d.Entete).WithMany(p => p.DocumentSections)
                .HasForeignKey(d => d.EnteteId)
                .HasConstraintName("FK__Document___Entet__6E8B6712");

            entity.HasOne(d => d.Nqa).WithMany(p => p.DocumentSections)
                .HasForeignKey(d => d.NqaId)
                .HasConstraintName("FK__Document___NqaId__725BF7F6");

            entity.HasOne(d => d.Periodicite).WithMany(p => p.DocumentSections)
                .HasForeignKey(d => d.PeriodiciteId)
                .HasConstraintName("FK__Document___Perio__7167D3BD");

            entity.HasOne(d => d.TypeSection).WithMany(p => p.DocumentSections)
                .HasForeignKey(d => d.TypeSectionId)
                .HasConstraintName("FK__Document___TypeS__7073AF84");
        });

        modelBuilder.Entity<DocumentVerifMachineEcheance>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Document__3214EC07C7608789");

            entity.ToTable("Document_VerifMachine_Echeance");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");

            entity.HasOne(d => d.Ligne).WithMany(p => p.DocumentVerifMachineEcheances)
                .HasForeignKey(d => d.LigneId)
                .HasConstraintName("FK__Document___Ligne__1E3A7A34");

            entity.HasOne(d => d.PeriodiciteMachine).WithMany(p => p.DocumentVerifMachineEcheances)
                .HasForeignKey(d => d.PeriodiciteMachineId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Document___Perio__1F2E9E6D");

            entity.HasOne(d => d.RefMoyenDetection).WithMany(p => p.DocumentVerifMachineEcheances)
                .HasForeignKey(d => d.RefMoyenDetectionId)
                .HasConstraintName("FK__Document___RefMo__2022C2A6");
        });

        modelBuilder.Entity<DocumentVerifMachineFamille>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Document__3214EC077507AC95");

            entity.ToTable("Document_VerifMachine_Famille");

            entity.HasIndex(e => new { e.EnteteId, e.RefFamilleCorpsId }, "UQ__Document__90BE3146D6A2DB7F").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");

            entity.HasOne(d => d.Entete).WithMany(p => p.DocumentVerifMachineFamilles)
                .HasForeignKey(d => d.EnteteId)
                .HasConstraintName("FK__Document___Entet__1975C517");

            entity.HasOne(d => d.RefFamilleCorps).WithMany(p => p.DocumentVerifMachineFamilles)
                .HasForeignKey(d => d.RefFamilleCorpsId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Document___RefFa__1A69E950");
        });

        modelBuilder.Entity<DocumentVerifMachineMatricePiece>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Document__3214EC077855298B");

            entity.ToTable("Document_VerifMachine_MatricePiece");

            entity.HasIndex(e => new { e.EcheanceId, e.FamilleId, e.RoleVerif, e.PieceRefId }, "UQ__Document__91CCDAA8CFDBED73").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.RoleVerif)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.Echeance).WithMany(p => p.DocumentVerifMachineMatricePieces)
                .HasForeignKey(d => d.EcheanceId)
                .HasConstraintName("FK__Document___Echea__25DB9BFC");

            entity.HasOne(d => d.Famille).WithMany(p => p.DocumentVerifMachineMatricePieces)
                .HasForeignKey(d => d.FamilleId)
                .HasConstraintName("FK__Document___Famil__26CFC035");

            entity.HasOne(d => d.PieceRef).WithMany(p => p.DocumentVerifMachineMatricePieces)
                .HasForeignKey(d => d.PieceRefId)
                .HasConstraintName("FK__Document___Piece__28B808A7");

            entity.HasOne(d => d.RoleVerifNavigation).WithMany(p => p.DocumentVerifMachineMatricePieces)
                .HasForeignKey(d => d.RoleVerif)
                .HasConstraintName("FK__Document___RoleV__27C3E46E");
        });

        modelBuilder.Entity<ExecControleDocumentStatut>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Exec_Con__3214EC07EB494AD1");

            entity.ToTable("Exec_ControleDocumentStatut", tb =>
                {
                    tb.HasTrigger("TR_ExecControleDocumentStatut_CreationOrder");
                    tb.HasTrigger("TR_ExecControleDocumentStatut_Form");
                    tb.HasTrigger("TR_ExecControleDocumentStatut_ProtectClosed");
                    tb.HasTrigger("TR_ExecControleDocumentStatut_TypeDocument");
                });

            entity.HasIndex(e => new { e.ExecControleOfId, e.SessionId }, "IX_ExecControleDocumentStatut_ExecSession");

            entity.HasIndex(e => e.CreationOrder, "UX_ExecControleDocumentStatut_CreationOrder").IsUnique();

            entity.HasIndex(e => new { e.ExecControleOfId, e.PosteCode }, "UX_ExecControleDocumentStatut_SamplingPerPoste")
                .IsUnique()
                .HasFilter("([TypeDocument]='ECHANTILLONNAGE' AND [PosteCode] IS NOT NULL)");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CreationOrder).HasDefaultValueSql("(NEXT VALUE FOR [dbo].[CreationOrderSequence])");
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
                .HasConstraintName("FK__Exec_Cont__DocId__79C80F94");

            entity.HasOne(d => d.EquipeNavigation).WithMany(p => p.ExecControleDocumentStatuts)
                .HasForeignKey(d => d.Equipe)
                .HasConstraintName("FK__Exec_Cont__Equip__7E8CC4B1");

            entity.HasOne(d => d.ExecControleOf).WithMany(p => p.ExecControleDocumentStatuts)
                .HasForeignKey(d => d.ExecControleOfId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Exec_Cont__ExecC__75F77EB0");

            entity.HasOne(d => d.Form).WithMany(p => p.ExecControleDocumentStatuts)
                .HasForeignKey(d => d.FormId)
                .HasConstraintName("FK__Exec_Cont__FormI__00750D23");

            entity.HasOne(d => d.MachineCodeNavigation).WithMany(p => p.ExecControleDocumentStatuts)
                .HasForeignKey(d => d.MachineCode)
                .HasConstraintName("FK__Exec_Cont__Machi__78D3EB5B");

            entity.HasOne(d => d.MatriculeOperateurNavigation).WithMany(p => p.ExecControleDocumentStatuts)
                .HasForeignKey(d => d.MatriculeOperateur)
                .HasConstraintName("FK__Exec_Cont__Matri__7F80E8EA");

            entity.HasOne(d => d.PosteCodeNavigation).WithMany(p => p.ExecControleDocumentStatuts)
                .HasForeignKey(d => d.PosteCode)
                .HasConstraintName("FK__Exec_Cont__Poste__77DFC722");

            entity.HasOne(d => d.Session).WithMany(p => p.ExecControleDocumentStatuts)
                .HasForeignKey(d => d.SessionId)
                .HasConstraintName("FK__Exec_Cont__Sessi__7ABC33CD");

            entity.HasOne(d => d.TypeDocumentNavigation).WithMany(p => p.ExecControleDocumentStatuts)
                .HasForeignKey(d => d.TypeDocument)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Exec_Cont__TypeD__76EBA2E9");
        });

        modelBuilder.Entity<ExecControleLigneReponse>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Exec_Con__3214EC07F62D656B");

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
                .HasConstraintName("FK__Exec_Cont__Docum__30242045");

            entity.HasOne(d => d.EquipeNavigation).WithMany(p => p.ExecControleLigneReponses)
                .HasForeignKey(d => d.Equipe)
                .HasConstraintName("FK__Exec_Cont__Equip__33F4B129");

            entity.HasOne(d => d.ExecControleDocumentStatut).WithMany(p => p.ExecControleLigneReponses)
                .HasForeignKey(d => d.ExecControleDocumentStatutId)
                .HasConstraintName("FK__Exec_Cont__ExecC__2F2FFC0C");

            entity.HasOne(d => d.ExecControleOf).WithMany(p => p.ExecControleLigneReponses)
                .HasForeignKey(d => d.ExecControleOfid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Exec_Cont__ExecC__2E3BD7D3");

            entity.HasOne(d => d.MatriculeOperateurNavigation).WithMany(p => p.ExecControleLigneReponses)
                .HasForeignKey(d => d.MatriculeOperateur)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Exec_Cont__Matri__33008CF0");
        });

        modelBuilder.Entity<ExecControleOf>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Exec_Con__3214EC075E684E05");

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
                .HasConstraintName("FK__Exec_Cont__Machi__4C0144E4");

            entity.HasOne(d => d.MachineCodePrevuNavigation).WithMany(p => p.ExecControleOfMachineCodePrevuNavigations)
                .HasForeignKey(d => d.MachineCodePrevu)
                .HasConstraintName("FK__Exec_Cont__Machi__4B0D20AB");

            entity.HasOne(d => d.NumeroOfNavigation).WithMany(p => p.ExecControleOfs)
                .HasForeignKey(d => d.NumeroOf)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Exec_Cont__Numer__4924D839");

            entity.HasOne(d => d.OperationCodeNavigation).WithMany(p => p.ExecControleOfs)
                .HasForeignKey(d => d.OperationCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Exec_Cont__Opera__4A18FC72");

            entity.HasOne(d => d.PlanSource).WithMany(p => p.ExecControleOfs)
                .HasForeignKey(d => d.PlanSourceId)
                .HasConstraintName("FK__Exec_Cont__PlanS__4CF5691D");
        });

        modelBuilder.Entity<ExecControleOfPoste>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Exec_Con__3214EC07EA851025");

            entity.ToTable("Exec_ControleOf_Poste", tb => tb.HasTrigger("TR_ExecControleOfPoste_ProtectClosed"));

            entity.HasIndex(e => new { e.ExecControleOfId, e.PosteCode }, "UX_ExecControleOfPoste").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.PosteCode)
                .HasMaxLength(30)
                .IsUnicode(false);

            entity.HasOne(d => d.ExecControleOf).WithMany(p => p.ExecControleOfPostes)
                .HasForeignKey(d => d.ExecControleOfId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Exec_Cont__ExecC__58671BC9");

            entity.HasOne(d => d.PosteCodeNavigation).WithMany(p => p.ExecControleOfPostes)
                .HasForeignKey(d => d.PosteCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Exec_Cont__Poste__595B4002");
        });

        modelBuilder.Entity<ExecControleOfSession>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Exec_Con__3214EC079A388DD1");

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
                .HasConstraintName("FK__Exec_Cont__Equip__5E1FF51F");

            entity.HasOne(d => d.ExecControleOf).WithMany(p => p.ExecControleOfSessions)
                .HasForeignKey(d => d.ExecControleOfId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Exec_Cont__ExecC__5D2BD0E6");
        });

        modelBuilder.Entity<ExecControleTranche>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Exec_Con__3214EC07DDD57AB9");

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
                .HasConstraintName("FK__Exec_Cont__ExecC__0EC32C7A");

            entity.HasOne(d => d.MatriculeApprobateurNavigation).WithMany(p => p.ExecControleTranches)
                .HasForeignKey(d => d.MatriculeApprobateur)
                .HasConstraintName("FK__Exec_Cont__Matri__10AB74EC");

            entity.HasOne(d => d.Session).WithMany(p => p.ExecControleTranches)
                .HasForeignKey(d => d.SessionId)
                .HasConstraintName("FK__Exec_Cont__Sessi__119F9925");
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
            entity.HasKey(e => e.Id).HasName("PK__Exec_Pie__3214EC07F820A8A5");

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
                .HasConstraintName("FK__Exec_Piec__ExecC__08162EEB");

            entity.HasOne(d => d.MatriculeOperateurNavigation).WithMany(p => p.ExecPieceTypes)
                .HasForeignKey(d => d.MatriculeOperateur)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Exec_Piec__Matri__0AF29B96");
        });

        modelBuilder.Entity<ExecPrelevementIntermediaire>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Exec_Pre__3214EC0735ADC8C6");

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
                .HasConstraintName("FK__Exec_Prel__ExecC__22CA2527");

            entity.HasOne(d => d.PosteCodeNavigation).WithMany(p => p.ExecPrelevementIntermediaires)
                .HasForeignKey(d => d.PosteCode)
                .HasConstraintName("FK__Exec_Prel__Poste__297722B6");

            entity.HasOne(d => d.ProductionPeriod).WithMany(p => p.ExecPrelevementIntermediaires)
                .HasForeignKey(d => d.ProductionPeriodId)
                .HasConstraintName("FK__Exec_Prel__Produ__2A6B46EF");

            entity.HasOne(d => d.Section).WithMany(p => p.ExecPrelevementIntermediaires)
                .HasForeignKey(d => d.SectionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Exec_Prel__Secti__23BE4960");

            entity.HasOne(d => d.Session).WithMany(p => p.ExecPrelevementIntermediaires)
                .HasForeignKey(d => d.SessionId)
                .HasConstraintName("FK__Exec_Prel__Sessi__2882FE7D");
        });

        modelBuilder.Entity<ExecProductionPeriod>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Exec_Pro__3214EC07AAF20439");

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
                .HasConstraintName("FK__Exec_Prod__ExecC__15702A09");

            entity.HasOne(d => d.PosteCodeNavigation).WithMany(p => p.ExecProductionPeriods)
                .HasForeignKey(d => d.PosteCode)
                .HasConstraintName("FK__Exec_Prod__Poste__1758727B");

            entity.HasOne(d => d.Section).WithMany(p => p.ExecProductionPeriods)
                .HasForeignKey(d => d.SectionId)
                .HasConstraintName("FK__Exec_Prod__Secti__184C96B4");

            entity.HasOne(d => d.Session).WithMany(p => p.ExecProductionPeriods)
                .HasForeignKey(d => d.SessionId)
                .HasConstraintName("FK__Exec_Prod__Sessi__16644E42");
        });

        modelBuilder.Entity<ExecRcPosteBilan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Exec_RC___3214EC07AFEDBF3B");

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
                .HasConstraintName("FK__Exec_RC_P__Equip__6F1576F7");

            entity.HasOne(d => d.ExecControleDocumentStatut).WithMany(p => p.ExecRcPosteBilans)
                .HasForeignKey(d => d.ExecControleDocumentStatutId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Exec_RC_P__ExecC__6E2152BE");

            entity.HasOne(d => d.MatriculeOperateurNavigation).WithMany(p => p.ExecRcPosteBilans)
                .HasForeignKey(d => d.MatriculeOperateur)
                .HasConstraintName("FK__Exec_RC_P__Matri__70FDBF69");
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
                .HasConstraintName("FK__Exec_RC_P__Equip__6774552F");

            entity.HasOne(d => d.ExecControleDocumentStatut).WithMany(p => p.ExecRcPosteReponses)
                .HasForeignKey(d => d.ExecControleDocumentStatutId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ExecRcPosteReponse_Statut");

            entity.HasOne(d => d.MatriculeOperateurNavigation).WithMany(p => p.ExecRcPosteReponses)
                .HasForeignKey(d => d.MatriculeOperateur)
                .HasConstraintName("FK__Exec_RC_P__Matri__68687968");
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
                .HasConstraintName("FK__ExecRegis__Cance__7993056A");

            entity.HasOne(d => d.EquipeNavigation).WithMany(p => p.ExecRegistreTracabilites)
                .HasForeignKey(d => d.Equipe)
                .HasConstraintName("FK__ExecRegis__Equip__77AABCF8");

            entity.HasOne(d => d.ExecControleOf).WithMany(p => p.ExecRegistreTracabilites)
                .HasForeignKey(d => d.ExecControleOfId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ExecRegistreTracabilite_ExecControleOf");

            entity.HasOne(d => d.MatriculeOperateurNavigation).WithMany(p => p.ExecRegistreTracabiliteMatriculeOperateurNavigations)
                .HasForeignKey(d => d.MatriculeOperateur)
                .HasConstraintName("FK__ExecRegis__Matri__789EE131");

            entity.HasOne(d => d.Session).WithMany(p => p.ExecRegistreTracabilites)
                .HasForeignKey(d => d.SessionId)
                .HasConstraintName("FK__ExecRegis__Sessi__76B698BF");

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
                .HasConstraintName("FK__ExecRegis__CodeA__01342732");

            entity.HasOne(d => d.RegistreTracabilite).WithMany(p => p.ExecRegistreTracabiliteComposants)
                .HasForeignKey(d => d.RegistreTracabiliteId)
                .HasConstraintName("FK_ExecRegistreTracabiliteComposant_ExecRegistreTracabilite");
        });

        modelBuilder.Entity<ExecSessionPost>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Exec_Ses__3214EC078F23636B");

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
                .HasConstraintName("FK__Exec_Sess__Ended__6E565CE8");

            entity.HasOne(d => d.ExecControleOf).WithMany(p => p.ExecSessionPosts)
                .HasForeignKey(d => d.ExecControleOfId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Exec_Sess__ExecC__6A85CC04");

            entity.HasOne(d => d.PosteCodeNavigation).WithMany(p => p.ExecSessionPosts)
                .HasForeignKey(d => d.PosteCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Exec_Sess__Poste__6B79F03D");

            entity.HasOne(d => d.Session).WithMany(p => p.ExecSessionPosts)
                .HasForeignKey(d => d.SessionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Exec_Sess__Sessi__6991A7CB");

            entity.HasOne(d => d.StartedByNavigation).WithMany(p => p.ExecSessionPostStartedByNavigations)
                .HasForeignKey(d => d.StartedBy)
                .HasConstraintName("FK__Exec_Sess__Start__6D6238AF");
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
                .HasConstraintName("FK__Exec_Veri__Matri__5FD33367");
        });

        modelBuilder.Entity<ExecutionDocumentType>(entity =>
        {
            entity.HasKey(e => e.Code).HasName("PK__Executio__A25C5AA6BB60750B");

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
            entity.HasKey(e => e.Code).HasName("PK__FamilleP__A25C5AA6694C4107");

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

        modelBuilder.Entity<FormCodeChange>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__FormCode__3214EC07E86E13F5");

            entity.ToTable("FormCodeChange", tb => tb.HasTrigger("TR_FormCodeChange_Immutable"));

            entity.HasIndex(e => e.NewFormId, "UX_FormCodeChange_NewForm").IsUnique();

            entity.HasIndex(e => e.PreviousFormId, "UX_FormCodeChange_PreviousForm").IsUnique();

            entity.Property(e => e.ChangedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.ChangedBy)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.ChangedByNavigation).WithMany(p => p.FormCodeChanges)
                .HasForeignKey(d => d.ChangedBy)
                .HasConstraintName("FK__FormCodeC__Chang__2CBDA3B5");

            entity.HasOne(d => d.NewForm).WithOne(p => p.FormCodeChangeNewForm)
                .HasForeignKey<FormCodeChange>(d => d.NewFormId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__FormCodeC__NewFo__2BC97F7C");

            entity.HasOne(d => d.PreviousForm).WithOne(p => p.FormCodeChangePreviousForm)
                .HasForeignKey<FormCodeChange>(d => d.PreviousFormId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__FormCodeC__Previ__2AD55B43");
        });

        modelBuilder.Entity<FormRole>(entity =>
        {
            entity.HasKey(e => e.Code).HasName("PK__FormRole__A25C5AA620B027CF");

            entity.ToTable("FormRole");

            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.Label).HasMaxLength(100);
            entity.Property(e => e.Nature)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("DOCUMENT");
        });

        modelBuilder.Entity<GeneratedDocument>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Generate__3214EC0716074E79");

            entity.ToTable("GeneratedDocument", tb =>
                {
                    tb.HasTrigger("TR_GeneratedDocument_Immutable");
                    tb.HasTrigger("TR_GeneratedDocument_Template");
                });

            entity.HasIndex(e => new { e.DocumentEnteteId, e.GeneratedAt }, "IX_GeneratedDocument_Document").IsDescending(false, true);

            entity.HasIndex(e => new { e.ExecControleDocumentStatutId, e.GeneratedAt }, "IX_GeneratedDocument_Execution").IsDescending(false, true);

            entity.HasIndex(e => new { e.DocumentEnteteId, e.ReportTemplateVersionId, e.SourceDataSha256 }, "UX_GeneratedDocument_Document")
                .IsUnique()
                .HasFilter("([DocumentEnteteId] IS NOT NULL)");

            entity.HasIndex(e => new { e.ExecControleDocumentStatutId, e.ReportTemplateVersionId, e.SourceDataSha256 }, "UX_GeneratedDocument_Execution")
                .IsUnique()
                .HasFilter("([ExecControleDocumentStatutId] IS NOT NULL)");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.FileName).HasMaxLength(200);
            entity.Property(e => e.GeneratedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.PdfSha256)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasComputedColumnSql("(CONVERT([char](64),hashbytes('SHA2_256',[PdfContent]),(2)))", true)
                .IsFixedLength();
            entity.Property(e => e.RequestedBy)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.SourceDataSha256)
                .HasMaxLength(64)
                .IsUnicode(false)
                .IsFixedLength();

            entity.HasOne(d => d.DocumentEntete).WithMany(p => p.GeneratedDocuments)
                .HasForeignKey(d => d.DocumentEnteteId)
                .HasConstraintName("FK__Generated__Docum__35A7EF71");

            entity.HasOne(d => d.ExecControleDocumentStatut).WithMany(p => p.GeneratedDocuments)
                .HasForeignKey(d => d.ExecControleDocumentStatutId)
                .HasConstraintName("FK__Generated__ExecC__369C13AA");

            entity.HasOne(d => d.ReportTemplateVersion).WithMany(p => p.GeneratedDocuments)
                .HasForeignKey(d => d.ReportTemplateVersionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Generated__Repor__379037E3");

            entity.HasOne(d => d.RequestedByNavigation).WithMany(p => p.GeneratedDocuments)
                .HasForeignKey(d => d.RequestedBy)
                .HasConstraintName("FK__Generated__Reque__39788055");
        });

        modelBuilder.Entity<InspectionLevel>(entity =>
        {
            entity.HasKey(e => e.Code).HasName("PK__Inspecti__A25C5AA6F8EBBCB9");

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
            entity.HasKey(e => e.CodeInstrument).HasName("PK__Instrume__E6E435055B50C16D");

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
            entity.HasKey(e => e.Id).HasName("PK__JournalC__3214EC07F2CCC5E9");

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
            entity.HasKey(e => e.CodeMachine).HasName("PK__Machine__50D6760F49393288");

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
                        j.HasKey("MachineCode", "RefFamilleCorpsId").HasName("PK__Machine___74270A8AD9B3EA0D");
                        j.ToTable("Machine_FamilleCorps");
                        j.IndexerProperty<string>("MachineCode")
                            .HasMaxLength(30)
                            .IsUnicode(false);
                    });
        });

        modelBuilder.Entity<MagExpeditionBl>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Mag_Expe__3214EC07BCB84D24");

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
                .HasConstraintName("FK__Mag_Exped__Matri__3CBF0154");

            entity.HasOne(d => d.NumeroBlNavigation).WithMany(p => p.MagExpeditionBls)
                .HasForeignKey(d => d.NumeroBl)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Mag_Exped__Numer__3BCADD1B");
        });

        modelBuilder.Entity<MagExpeditionBlScanOf>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Mag_Expe__3214EC07F187E1C5");

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
                .HasConstraintName("FK__Mag_Exped__Exped__436BFEE3");

            entity.HasOne(d => d.NumeroOfscanneNavigation).WithMany(p => p.MagExpeditionBlScanOfs)
                .HasForeignKey(d => d.NumeroOfscanne)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Mag_Exped__Numer__4460231C");
        });

        modelBuilder.Entity<MagPreparationOf>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Mag_Prep__3214EC0714926F3D");

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
                .HasConstraintName("FK__Mag_Prepa__Matri__2E70E1FD");

            entity.HasOne(d => d.NumeroOfNavigation).WithMany(p => p.MagPreparationOfs)
                .HasForeignKey(d => d.NumeroOf)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Mag_Prepa__Numer__2D7CBDC4");
        });

        modelBuilder.Entity<MagPreparationOfLot>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Mag_Prep__3214EC07DC8FBAD9");

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
                .HasConstraintName("FK__Mag_Prepa__CodeA__361203C5");

            entity.HasOne(d => d.PreparationOf).WithMany(p => p.MagPreparationOfLots)
                .HasForeignKey(d => d.PreparationOfid)
                .HasConstraintName("FK__Mag_Prepa__Prepa__351DDF8C");
        });

        modelBuilder.Entity<MfgheadOrdreFabrication>(entity =>
        {
            entity.HasKey(e => e.NumeroOf).HasName("PK__MFGHEAD___C6A65F30EAF2D294");

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
            entity.HasKey(e => e.Id).HasName("PK__MFGMAT_B__3214EC07E965E647");

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
            entity.HasKey(e => e.Id).HasName("PK__MoyenCon__3214EC07C630B8A1");

            entity.ToTable("MoyenControle", tb => tb.HasTrigger("trg_no_del_MoyenControle"));

            entity.HasIndex(e => e.Code, "UQ__MoyenCon__A25C5AA7B74C25D3").IsUnique();

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
            entity.HasKey(e => e.Code).HasName("PK__NatureAr__A25C5AA62CB6D06D");

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
            entity.HasKey(e => new { e.NatureArticleCode, e.OperationCode }).HasName("PK__NatureAr__6403AE7796297E11");

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
            entity.HasKey(e => e.Id).HasName("PK__NQA__3214EC0760172167");

            entity.ToTable("NQA", tb => tb.HasTrigger("trg_no_del_NQA"));

            entity.HasIndex(e => e.ValeurNqa, "UQ__NQA__1DA3E2483B127632").IsUnique();

            entity.Property(e => e.ValeurNqa)
                .HasColumnType("decimal(6, 3)")
                .HasColumnName("ValeurNQA");
        });

        modelBuilder.Entity<Operation>(entity =>
        {
            entity.HasKey(e => e.Code).HasName("PK__Operatio__A25C5AA6AC092E77");

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
            entity.HasKey(e => e.Id).HasName("PK__Periodic__3214EC0785E13DC8");

            entity.ToTable("Periodicite", tb => tb.HasTrigger("trg_no_del_Periodicite"));

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.Actif).HasDefaultValue(true);
            entity.Property(e => e.Unite)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<PeriodiciteMachine>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Periodic__3214EC078988042B");

            entity.ToTable("PeriodiciteMachine");

            entity.HasIndex(e => e.Code, "UQ__Periodic__A25C5AA7455ABACC").IsUnique();

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
            entity.HasKey(e => e.Id).HasName("PK__PieceRef__3214EC07861A46F0");

            entity.ToTable("PieceReference", tb => tb.HasTrigger("trg_no_del_PieceReference"));

            entity.HasIndex(e => e.Code, "UQ__PieceRef__A25C5AA796E4EAB3").IsUnique();

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
            entity.HasKey(e => e.CodePoste).HasName("PK__PosteTra__4045446B3D8F0BED");

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
                        j.HasKey("CodePoste", "CodeMachine").HasName("PK__PosteTra__A548230BDCAC5EFC");
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
            entity.HasKey(e => e.Matricule).HasName("PK__Preassig__0FB9FB42B96A0382");

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
            entity.HasKey(e => e.CodeArticle).HasName("PK__ProduitF__32384FB03EDE2CB7");

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
            entity.HasKey(e => e.Id).HasName("PK__Ref_Cara__3214EC070A628CE8");

            entity.ToTable("Ref_Caracteristique", tb => tb.HasTrigger("trg_no_del_Ref_Caracteristique"));

            entity.HasIndex(e => e.LibelleNormalise, "IX_Ref_Caracteristique_Normalise");

            entity.HasIndex(e => e.LibelleNormalise, "UQ__Ref_Cara__5C5A3C6E77D4AA95").IsUnique();

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
            entity.HasKey(e => e.Id).HasName("PK__Ref_Fami__3214EC078447678A");

            entity.ToTable("Ref_FamilleCorps", tb => tb.HasTrigger("trg_no_del_Ref_FamilleCorps"));

            entity.HasIndex(e => e.Code, "UQ__Ref_Fami__A25C5AA7ED1B09B1").IsUnique();

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
            entity.HasKey(e => e.Id).HasName("PK__Ref_Form__3214EC07770AA725");

            entity.ToTable("Ref_Formulaire", tb =>
                {
                    tb.HasTrigger("TR_RefFormulaire_Identity");
                    tb.HasTrigger("TR_RefFormulaire_Scope");
                });

            entity.HasIndex(e => new { e.VersionGroupId, e.CreationOrder }, "IX_RefFormulaire_VersionGroup");

            entity.HasIndex(e => new { e.CodeReference, e.Version }, "UQ__Ref_Form__4F92504B45C77BD5").IsUnique();

            entity.HasIndex(e => new { e.Role, e.FamilleProduitFiniCode, e.PosteCode, e.TypeRobinetCode, e.MachineCode, e.NatureArticleCode }, "UX_RefFormulaire_ChampApplication")
                .IsUnique()
                .HasFilter("([Statut] IN ('BROUILLON', 'ACTIF'))");

            entity.HasIndex(e => e.CodeReference, "UX_RefFormulaire_CodeEnCours")
                .IsUnique()
                .HasFilter("([Statut] IN ('BROUILLON', 'ACTIF'))");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CodeReference)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.CreationOrder).HasDefaultValueSql("(NEXT VALUE FOR [dbo].[CreationOrderSequence])");
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
            entity.Property(e => e.TypeRobinetCode)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.VersionGroupId).HasDefaultValueSql("(newid())");

            entity.HasOne(d => d.CreeParNavigation).WithMany(p => p.RefFormulaireCreeParNavigations)
                .HasForeignKey(d => d.CreePar)
                .HasConstraintName("FK__Ref_Formu__CreeP__1E6F845E");

            entity.HasOne(d => d.FamilleProduitFiniCodeNavigation).WithMany(p => p.RefFormulaires)
                .HasForeignKey(d => d.FamilleProduitFiniCode)
                .HasConstraintName("FK__Ref_Formu__Famil__22401542");

            entity.HasOne(d => d.MachineCodeNavigation).WithMany(p => p.RefFormulaires)
                .HasForeignKey(d => d.MachineCode)
                .HasConstraintName("FK__Ref_Formu__Machi__24285DB4");

            entity.HasOne(d => d.ModifieParNavigation).WithMany(p => p.RefFormulaireModifieParNavigations)
                .HasForeignKey(d => d.ModifiePar)
                .HasConstraintName("FK__Ref_Formu__Modif__1F63A897");

            entity.HasOne(d => d.NatureArticleCodeNavigation).WithMany(p => p.RefFormulaires)
                .HasForeignKey(d => d.NatureArticleCode)
                .HasConstraintName("FK__Ref_Formu__Natur__251C81ED");

            entity.HasOne(d => d.PosteCodeNavigation).WithMany(p => p.RefFormulaires)
                .HasForeignKey(d => d.PosteCode)
                .HasConstraintName("FK__Ref_Formu__Poste__214BF109");

            entity.HasOne(d => d.RoleNavigation).WithMany(p => p.RefFormulaires)
                .HasForeignKey(d => d.Role)
                .HasConstraintName("FK__Ref_Formul__Role__2057CCD0");

            entity.HasOne(d => d.TypeRobinetCodeNavigation).WithMany(p => p.RefFormulaires)
                .HasForeignKey(d => d.TypeRobinetCode)
                .HasConstraintName("FK__Ref_Formu__TypeR__2334397B");
        });

        modelBuilder.Entity<RefFormulaireColonneDef>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Ref_Form__3214EC074C13EC9A");

            entity.ToTable("Ref_Formulaire_ColonneDef", tb =>
                {
                    tb.HasTrigger("TR_RefFormulaireColonneDef_TypeLocked");
                    tb.HasTrigger("trg_no_del_Ref_Formulaire_ColonneDef");
                });

            entity.HasIndex(e => new { e.FormulaireId, e.CleColonne }, "UQ__Ref_Form__EC7AEAEB94057197").IsUnique();

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
                .HasConstraintName("FK__Ref_Formu__Formu__336AA144");

            entity.HasOne(d => d.TypeValeurNavigation).WithMany(p => p.RefFormulaireColonneDefs)
                .HasForeignKey(d => d.TypeValeur)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Ref_Formu__TypeV__3552E9B6");
        });

        modelBuilder.Entity<RefFormulaireEquipe>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Ref_Form__3214EC07EE19209A");

            entity.ToTable("Ref_Formulaire_Equipe", tb => tb.HasTrigger("trg_no_del_Ref_Formulaire_Equipe"));

            entity.HasIndex(e => new { e.FormulaireId, e.NomEquipe }, "UQ__Ref_Form__DE54B052FE4A78C6").IsUnique();

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
                .HasConstraintName("FK__Ref_Formu__Formu__43A1090D");

            entity.HasOne(d => d.TeamCodeNavigation).WithMany(p => p.RefFormulaireEquipes)
                .HasForeignKey(d => d.TeamCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Ref_Formu__TeamC__44952D46");
        });

        modelBuilder.Entity<RefIso2859Echantillonnage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Ref_ISO2__3214EC07BA77795C");

            entity.ToTable("Ref_ISO2859_Echantillonnage");

            entity.HasIndex(e => e.CodeLettre, "UQ__Ref_ISO2__4A778AF74FA40F13").IsUnique();

            entity.Property(e => e.CodeLettre)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
        });

        modelBuilder.Entity<RefIso2859LettresCode>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Ref_ISO2__3214EC073B86D9FC");

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
                .HasConstraintName("FK__Ref_ISO28__Nivea__4B422AD5");
        });

        modelBuilder.Entity<RefMoyenDetection>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Ref_Moye__3214EC076256B758");

            entity.ToTable("Ref_MoyenDetection", tb => tb.HasTrigger("trg_no_del_Ref_MoyenDetection"));

            entity.HasIndex(e => e.Code, "UQ__Ref_Moye__A25C5AA72B21418D").IsUnique();

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
            entity.HasKey(e => e.Code).HasName("PK__Referenc__A25C5AA62570B6E6");

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
            entity.HasKey(e => e.Id).HasName("PK__RefreshT__3214EC079B4B87D0");

            entity.HasIndex(e => e.TokenHash, "UQ__RefreshT__BCB33F9260A78E71").IsUnique();

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

        modelBuilder.Entity<ReportTemplate>(entity =>
        {
            entity.HasKey(e => e.Code).HasName("PK__ReportTe__A25C5AA637553980");

            entity.ToTable("ReportTemplate");

            entity.Property(e => e.Code)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.DocumentTypeCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.ExecutionDocumentTypeCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FormRole)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Name).HasMaxLength(150);

            entity.HasOne(d => d.DocumentTypeCodeNavigation).WithMany(p => p.ReportTemplates)
                .HasForeignKey(d => d.DocumentTypeCode)
                .HasConstraintName("FK__ReportTem__Docum__18178C8A");

            entity.HasOne(d => d.ExecutionDocumentTypeCodeNavigation).WithMany(p => p.ReportTemplates)
                .HasForeignKey(d => d.ExecutionDocumentTypeCode)
                .HasConstraintName("FK__ReportTem__Execu__190BB0C3");

            entity.HasOne(d => d.FormRoleNavigation).WithMany(p => p.ReportTemplates)
                .HasForeignKey(d => d.FormRole)
                .HasConstraintName("FK__ReportTem__FormR__19FFD4FC");
        });

        modelBuilder.Entity<ReportTemplateAssignment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ReportTe__3214EC071834DB53");

            entity.ToTable("ReportTemplateAssignment", tb => tb.HasTrigger("TR_ReportTemplateAssignment_Rules"));

            entity.HasIndex(e => e.FormId, "IX_ReportTemplateAssignment_Form").HasFilter("([FormId] IS NOT NULL)");

            entity.HasIndex(e => e.ReportTemplateVersionId, "IX_ReportTemplateAssignment_Version");

            entity.HasIndex(e => e.CreationOrder, "UX_ReportTemplateAssignment_CreationOrder").IsUnique();

            entity.Property(e => e.AssignedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.AssignedBy)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Comment).HasMaxLength(500);
            entity.Property(e => e.CreationOrder).HasDefaultValueSql("(NEXT VALUE FOR [dbo].[CreationOrderSequence])");
            entity.Property(e => e.Kind)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.AssignedByNavigation).WithMany(p => p.ReportTemplateAssignments)
                .HasForeignKey(d => d.AssignedBy)
                .HasConstraintName("FK__ReportTem__Assig__2EFAF1E2");

            entity.HasOne(d => d.Form).WithMany(p => p.ReportTemplateAssignments)
                .HasForeignKey(d => d.FormId)
                .HasConstraintName("FK__ReportTem__FormI__2E06CDA9");

            entity.HasOne(d => d.ReportTemplateVersion).WithMany(p => p.ReportTemplateAssignments)
                .HasForeignKey(d => d.ReportTemplateVersionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ReportTem__Repor__2C1E8537");
        });

        modelBuilder.Entity<ReportTemplateRequest>(entity =>
        {
            entity.ToTable("ReportTemplateRequest");

            entity.HasIndex(e => e.FormId, "UX_ReportTemplateRequest_Pending").IsUnique().HasFilter("([Status]='PENDING')");

            entity.Property(e => e.Origin).HasMaxLength(20).IsUnicode(false);
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.VersionChange).HasMaxLength(20).IsUnicode(false);
            entity.Property(e => e.Status).HasMaxLength(20).IsUnicode(false).HasDefaultValue("PENDING");
            entity.Property(e => e.RequestedBy).HasMaxLength(20).IsUnicode(false);
            entity.Property(e => e.RequestedAt).HasPrecision(3).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.HandledBy).HasMaxLength(20).IsUnicode(false);
            entity.Property(e => e.HandledAt).HasPrecision(3);

            entity.HasOne(d => d.Form).WithMany().HasForeignKey(d => d.FormId);
            entity.HasOne(d => d.Assignment).WithMany().HasForeignKey(d => d.AssignmentId);
            entity.HasOne<Autili>().WithMany().HasForeignKey(d => d.RequestedBy);
            entity.HasOne<Autili>().WithMany().HasForeignKey(d => d.HandledBy);
        });

        modelBuilder.Entity<ReportTemplateInitialForm>(entity =>
        {
            entity.HasKey(e => e.FormCodeReference).HasName("PK__ReportTe__D4A22471E093F5D4");

            entity.ToTable("ReportTemplateInitialForm");

            entity.Property(e => e.FormCodeReference)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.ReportTemplateCode)
                .HasMaxLength(30)
                .IsUnicode(false);

            entity.HasOne(d => d.ReportTemplateCodeNavigation).WithMany(p => p.ReportTemplateInitialForms)
                .HasForeignKey(d => d.ReportTemplateCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ReportTem__Repor__1DD065E0");
        });

        modelBuilder.Entity<ReportTemplateVersion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ReportTe__3214EC074620A814");

            entity.ToTable("ReportTemplateVersion", tb =>
                {
                    tb.HasTrigger("TR_ReportTemplateVersion_Immutable");
                    tb.HasTrigger("TR_ReportTemplateVersion_Owner");
                });

            entity.HasIndex(e => new { e.FormGroupId, e.VersionNumber }, "UX_ReportTemplateVersion_FormNumber").IsUnique()
                .HasFilter("([FormGroupId] IS NOT NULL)");
            entity.HasIndex(e => new { e.ReportTemplateCode, e.VersionNumber }, "UX_ReportTemplateVersion_DefaultNumber").IsUnique()
                .HasFilter("([FormGroupId] IS NULL)");
            entity.Property(e => e.Name).HasMaxLength(200);

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.Comment).HasMaxLength(500);
            entity.Property(e => e.ContentSha256)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasComputedColumnSql("(CONVERT([char](64),hashbytes('SHA2_256',[Content]),(2)))", true)
                .IsFixedLength();
            entity.Property(e => e.FileName).HasMaxLength(200);
            entity.Property(e => e.PublishedAt).HasPrecision(3);
            entity.Property(e => e.PublishedBy)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.ReportTemplateCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("DRAFT");
            entity.Property(e => e.UploadedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.UploadedBy)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.PublishedByNavigation).WithMany(p => p.ReportTemplateVersionPublishedByNavigations)
                .HasForeignKey(d => d.PublishedBy)
                .HasConstraintName("FK__ReportTem__Publi__284DF453");

            entity.HasOne(d => d.ReportTemplateCodeNavigation).WithMany(p => p.ReportTemplateVersions)
                .HasForeignKey(d => d.ReportTemplateCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ReportTem__Repor__22951AFD");

            entity.HasOne(d => d.UploadedByNavigation).WithMany(p => p.ReportTemplateVersionUploadedByNavigations)
                .HasForeignKey(d => d.UploadedBy)
                .HasConstraintName("FK__ReportTem__Uploa__2665ABE1");
        });

        modelBuilder.Entity<RisqueDefaut>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RisqueDe__3214EC07C0126E6A");

            entity.ToTable("RisqueDefaut", tb => tb.HasTrigger("trg_no_del_RisqueDefaut"));

            entity.HasIndex(e => e.CodeDefaut, "UQ__RisqueDe__2EF873436CD7A56A").IsUnique();

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
            entity.HasKey(e => e.DocumentId).HasName("PK__Sampling__1ABEEF0F5849EB24");

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
                .HasConstraintName("FK__SamplingP__Accep__14B10FFA");

            entity.HasOne(d => d.Document).WithOne(p => p.SamplingPlan)
                .HasForeignKey<SamplingPlan>(d => d.DocumentId)
                .HasConstraintName("FK__SamplingP__Docum__10E07F16");

            entity.HasOne(d => d.InspectionLevelCodeNavigation).WithMany(p => p.SamplingPlans)
                .HasForeignKey(d => d.InspectionLevelCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SamplingP__Inspe__11D4A34F");
        });

        modelBuilder.Entity<Sdelivery>(entity =>
        {
            entity.HasKey(e => e.NumeroBl).HasName("PK__SDELIVER__C664DCCD6A64E160");

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
            entity.Property(e => e.NumeroCommande)
                .HasMaxLength(30)
                .IsUnicode(false);

            entity.HasOne(d => d.NumeroCommandeNavigation).WithMany(p => p.Sdeliveries)
                .HasForeignKey(d => d.NumeroCommande)
                .HasConstraintName("FK_SDELIVERY_SalesOrder");
        });

        modelBuilder.Entity<Sorder>(entity =>
        {
            entity.HasKey(e => e.NumeroCommande).HasName("PK_SORDER");

            entity.ToTable("SORDER");

            entity.HasIndex(e => e.ReferenceCommandeClient, "IX_SORDER_CustomerReference").HasFilter("([ReferenceCommandeClient] IS NOT NULL)");

            entity.Property(e => e.NumeroCommande)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.CodeClient)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.ReferenceCommandeClient)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Porder>(entity =>
        {
            entity.HasKey(e => e.NumeroCommande).HasName("PK_PORDER");

            entity.ToTable("PORDER");

            entity.Property(e => e.NumeroCommande)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.CodeFournisseur)
                .HasMaxLength(30)
                .IsUnicode(false);
        });

        modelBuilder.Entity<ExternalDocumentType>(entity =>
        {
            entity.HasKey(e => e.Code).HasName("PK_ExternalDocumentType");

            entity.ToTable("ExternalDocumentType");

            entity.Property(e => e.Code)
                .HasMaxLength(40)
                .IsUnicode(false);
            entity.Property(e => e.Label).HasMaxLength(100);
            entity.Property(e => e.SharedFolder).HasMaxLength(100);
            entity.Property(e => e.UploadRole)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.UploadRoleNavigation).WithMany()
                .HasForeignKey(d => d.UploadRole)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ExternalDocumentType_UploadRole");
        });

        modelBuilder.Entity<ExternalDocument>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ExternalDocument");

            entity.ToTable("ExternalDocument", tb => tb.HasTrigger("TR_ExternalDocument_Rules"));

            entity.HasIndex(e => e.ArticleCode, "IX_ExternalDocument_Article").HasFilter("([SupersededAt] IS NULL)");

            entity.HasIndex(e => e.ControlReportNumber, "IX_ExternalDocument_ControlReport").HasFilter("([SupersededAt] IS NULL)");

            entity.HasIndex(e => e.CustomerOrderReference, "IX_ExternalDocument_CustomerOrder").HasFilter("([SupersededAt] IS NULL)");

            entity.HasIndex(e => e.DeliveryNoteNumber, "IX_ExternalDocument_DeliveryNote").HasFilter("([SupersededAt] IS NULL)");

            entity.HasIndex(e => e.ManufacturingOrderNumber, "IX_ExternalDocument_ManufacturingOrder").HasFilter("([SupersededAt] IS NULL)");

            entity.HasIndex(e => e.SalesOrderNumber, "IX_ExternalDocument_SalesOrder").HasFilter("([SupersededAt] IS NULL)");

            entity.HasIndex(e => e.SupplierOrderNumber, "IX_ExternalDocument_SupplierOrder").HasFilter("([SupersededAt] IS NULL)");

            entity.HasIndex(e => new { e.TypeCode, e.UploadedAt }, "IX_ExternalDocument_UploadedAt").IsDescending(false, true);

            entity.HasIndex(e => new { e.TypeCode, e.DocumentKey }, "UX_ExternalDocument_Current")
                .IsUnique()
                .HasFilter("([SupersededAt] IS NULL)");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.ArticleCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.ContentSha256)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasComputedColumnSql("(CONVERT([char](64),hashbytes('SHA2_256',[Content]),(2)))", true)
                .IsFixedLength();
            entity.Property(e => e.ControlReportNumber)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.CustomerOrderReference)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.DeliveryNoteNumber)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.DocumentKey)
                .HasMaxLength(400)
                .IsUnicode(false)
                .HasComputedColumnSql("(CONVERT([varchar](400),case [TypeCode] when 'CONTROL_REPORT' then [ControlReportNumber] when 'DELIVERY_NOTE' then [DeliveryNoteNumber] else concat_ws('-',[ControlReportNumber],[ArticleCode],[DeliveryNoteNumber],[ManufacturingOrderNumber],[SupplierOrderNumber],translate([CustomerOrderReference],' /','__')) end))", true);
            entity.Property(e => e.FileName)
                .HasMaxLength(400)
                .IsUnicode(false)
                .HasComputedColumnSql("(CONVERT([varchar](400),concat_ws('-',[ControlReportNumber],[ArticleCode],[DeliveryNoteNumber],[ManufacturingOrderNumber],[SupplierOrderNumber],translate([CustomerOrderReference],' /','__'))+'.pdf'))", true);
            entity.Property(e => e.ManufacturingOrderNumber)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.OriginalFileName).HasMaxLength(260);
            entity.Property(e => e.ReplacementReason).HasMaxLength(300);
            entity.Property(e => e.SalesOrderNumber)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.SupersededAt).HasPrecision(3);
            entity.Property(e => e.SupplierOrderNumber)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.TypeCode)
                .HasMaxLength(40)
                .IsUnicode(false);
            entity.Property(e => e.UploadedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.UploadedBy)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.ArticleCodeNavigation).WithMany()
                .HasForeignKey(d => d.ArticleCode)
                .HasConstraintName("FK_ExternalDocument_Article");

            entity.HasOne(d => d.DeliveryNoteNumberNavigation).WithMany(p => p.ExternalDocuments)
                .HasForeignKey(d => d.DeliveryNoteNumber)
                .HasConstraintName("FK_ExternalDocument_DeliveryNote");

            entity.HasOne(d => d.ManufacturingOrderNumberNavigation).WithMany()
                .HasForeignKey(d => d.ManufacturingOrderNumber)
                .HasConstraintName("FK_ExternalDocument_ManufacturingOrder");

            entity.HasOne(d => d.ReplacesDocument).WithMany(p => p.InverseReplacesDocument)
                .HasForeignKey(d => d.ReplacesDocumentId)
                .HasConstraintName("FK_ExternalDocument_Replaces");

            entity.HasOne(d => d.SalesOrderNumberNavigation).WithMany(p => p.ExternalDocuments)
                .HasForeignKey(d => d.SalesOrderNumber)
                .HasConstraintName("FK_ExternalDocument_SalesOrder");

            entity.HasOne(d => d.SupplierOrderNumberNavigation).WithMany(p => p.ExternalDocuments)
                .HasForeignKey(d => d.SupplierOrderNumber)
                .HasConstraintName("FK_ExternalDocument_SupplierOrder");

            entity.HasOne(d => d.TypeCodeNavigation).WithMany(p => p.ExternalDocuments)
                .HasForeignKey(d => d.TypeCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ExternalDocument_Type");

            entity.HasOne(d => d.UploadedByNavigation).WithMany()
                .HasForeignKey(d => d.UploadedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ExternalDocument_UploadedBy");
        });

        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasKey(e => e.Code).HasName("PK__Team__A25C5AA61247B035");

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
            entity.HasKey(e => e.Id).HasName("PK__TypeCara__3214EC07F49D3EE3");

            entity.ToTable("TypeCaracteristique", tb => tb.HasTrigger("trg_no_del_TypeCaracteristique"));

            entity.HasIndex(e => e.Code, "UQ__TypeCara__A25C5AA70E4A8B6F").IsUnique();

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
            entity.HasKey(e => e.Id).HasName("PK__TypeCont__3214EC0778D18619");

            entity.ToTable("TypeControle", tb => tb.HasTrigger("trg_no_del_TypeControle"));

            entity.HasIndex(e => e.Code, "UQ__TypeCont__A25C5AA74751B706").IsUnique();

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
            entity.HasKey(e => e.Code).HasName("PK__TypeDocu__A25C5AA67E746E90");

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
            entity.HasKey(e => e.Code).HasName("PK__TypeRobi__A25C5AA638277EC8");

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
            entity.HasKey(e => e.Id).HasName("PK__TypeSect__3214EC0717667DD3");

            entity.ToTable("TypeSection", tb => tb.HasTrigger("trg_no_del_TypeSection"));

            entity.HasIndex(e => e.TypeNormalise, "UQ__TypeSect__70B9DB9E00421651").IsUnique();

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
            entity.HasKey(e => e.Id).HasName("PK__Utilisat__3214EC07ECD44E76");

            entity.ToTable("UtilisateursApp");

            entity.HasIndex(e => e.Matricule, "IX_UtilisateursApp_Matricule");

            entity.HasIndex(e => e.Matricule, "UQ__Utilisat__0FB9FB4331B49C64").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__Utilisat__A9D10534E047F408").IsUnique();

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
        modelBuilder.HasSequence("CreationOrderSequence");

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
