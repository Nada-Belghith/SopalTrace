-- =================================================================================
-- SopalTraceDB — Migration d'une base existante (sans la recréer)
--
-- Documents externes déposés par les services et retrouvés pour chaque OF (déjà présent dans 01 et 02 pour une base
-- neuve) :
--   1. Copies des tables Sage des commandes : SORDER (commandes clients, ouvertes, avec la référence de commande du
--      client), PORDER (commandes fournisseurs) ; SDELIVERY reçoit la commande Sage de chaque BL (NumeroCommande).
--      Leur alimentation suit celle des autres copies Sage.
--   2. Rôles des services qui déposent : SERVICE_ACHATS, SERVICE_COMMERCIAL, SERVICE_QUALITE, SERVICE_DI.
--   3. Types de documents externes et documents déposés (nom normalisé formé par la base, jamais modifiés ni
--      supprimés, remplacement avec motif).
--
-- Rejouable : chaque étape vérifie si elle est déjà faite. Tout ou rien (transaction).
-- Faire une sauvegarde avant. Ensuite : redémarrer l'API.
-- =================================================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
SET NOCOUNT ON;
GO
USE SopalTraceDB;
GO

BEGIN TRY
BEGIN TRANSACTION;

-- ─────────────────────────── 1. Commandes Sage ───────────────────────────
IF OBJECT_ID('dbo.SORDER') IS NULL
BEGIN
    CREATE TABLE dbo.SORDER (
        NumeroCommande          VARCHAR(30) NOT NULL CONSTRAINT PK_SORDER PRIMARY KEY,
        CodeClient              VARCHAR(30) NOT NULL,
        DateCommande            DATE        NOT NULL,
        ReferenceCommandeClient VARCHAR(50) NULL
    );
    CREATE INDEX IX_SORDER_CustomerReference ON dbo.SORDER(ReferenceCommandeClient) WHERE ReferenceCommandeClient IS NOT NULL;
END

IF OBJECT_ID('dbo.PORDER') IS NULL
    CREATE TABLE dbo.PORDER (
        NumeroCommande  VARCHAR(30) NOT NULL CONSTRAINT PK_PORDER PRIMARY KEY,
        CodeFournisseur VARCHAR(30) NOT NULL,
        DateCommande    DATE        NOT NULL
    );

IF COL_LENGTH('dbo.SDELIVERY', 'NumeroCommande') IS NULL
    ALTER TABLE dbo.SDELIVERY ADD NumeroCommande VARCHAR(30) NULL
        CONSTRAINT FK_SDELIVERY_SalesOrder REFERENCES dbo.SORDER(NumeroCommande);

-- ─────────────────────────── 2. Rôles des services ───────────────────────────
INSERT INTO dbo.AppRole (Code, Label)
SELECT r.Code, r.Label FROM (VALUES ('SERVICE_ACHATS', 'Service achats'), ('SERVICE_COMMERCIAL', 'Service commercial'),
                                    ('SERVICE_QUALITE', 'Service qualité (réception)'),
                                    ('SERVICE_DI', 'Service développement industriel')) r(Code, Label)
WHERE NOT EXISTS (SELECT 1 FROM dbo.AppRole a WHERE a.Code = r.Code);

-- ─────────────────────────── 3. Documents externes ───────────────────────────
IF OBJECT_ID('dbo.ExternalDocumentType') IS NULL
    CREATE TABLE dbo.ExternalDocumentType (
        Code                             VARCHAR(40)   NOT NULL CONSTRAINT PK_ExternalDocumentType PRIMARY KEY,
        Label                            NVARCHAR(100) NOT NULL,
        DisplayOrder                     INT           NOT NULL,
        UploadRole                       VARCHAR(50)   NOT NULL CONSTRAINT FK_ExternalDocumentType_UploadRole REFERENCES dbo.AppRole(Code),
        -- Sous-dossier du répertoire partagé où l'application recopie les fichiers déposés (si ce répertoire est configuré).
        SharedFolder                     NVARCHAR(100) NOT NULL,
        RequiresControlReportNumber      BIT           NOT NULL,
        RequiresArticleCode              BIT           NOT NULL,
        RequiresDeliveryNoteNumber       BIT           NOT NULL,
        RequiresManufacturingOrderNumber BIT           NOT NULL,
        RequiresSupplierOrderNumber      BIT           NOT NULL,
        -- Commande client Sage (commande ouverte) saisie ; sa référence client est reprise de Sage (BL).
        RequiresSalesOrderNumber         BIT           NOT NULL,
        RequiresCustomerOrderReference   BIT           NOT NULL
    );

INSERT INTO dbo.ExternalDocumentType (Code, Label, DisplayOrder, UploadRole, SharedFolder,
    RequiresControlReportNumber, RequiresArticleCode, RequiresDeliveryNoteNumber, RequiresManufacturingOrderNumber,
    RequiresSupplierOrderNumber, RequiresSalesOrderNumber, RequiresCustomerOrderReference)
SELECT t.* FROM (VALUES
    ('CONTROL_REPORT', N'Rapport de contrôle', 1, 'SERVICE_QUALITE', N'Rapports_Controle', 1, 0, 0, 0, 1, 0, 0),
    ('CONFORMITY_CERTIFICATE', N'Certificat de conformité', 2, 'SERVICE_QUALITE', N'Certificats_Conformite', 0, 1, 0, 0, 1, 0, 0),
    ('SUPPLIER_ORDER', N'Bon de commande fournisseur', 3, 'SERVICE_ACHATS', N'BC_Frs', 0, 0, 0, 0, 1, 0, 0),
    ('DELIVERY_NOTE', N'Bon de livraison', 4, 'SERVICE_COMMERCIAL', N'BL', 0, 0, 1, 0, 0, 1, 1),
    ('CUSTOMER_ORDER', N'Bon de commande client', 5, 'SERVICE_COMMERCIAL', N'BC_Client', 0, 0, 0, 0, 0, 0, 1),
    ('FINISHED_PRODUCT_CONTROL_REPORT', N'Rapport de contrôle rapide du produit fini', 6, 'SERVICE_QUALITE', N'Rapports_Produit_Fini', 0, 0, 0, 1, 0, 0, 0),
    ('FINISHED_PRODUCT_DEFINITION_PLAN', N'Plan de définition du produit fini', 7, 'SERVICE_DI', N'Plans_Definition_Produit_Fini', 0, 1, 0, 0, 0, 0, 0)
) t(Code, Label, DisplayOrder, UploadRole, SharedFolder, A, B, C, D, E, F, G)
WHERE NOT EXISTS (SELECT 1 FROM dbo.ExternalDocumentType x WHERE x.Code = t.Code);

IF OBJECT_ID('dbo.ExternalDocument') IS NULL
BEGIN
    CREATE TABLE dbo.ExternalDocument (
        Id                       UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ExternalDocument PRIMARY KEY
                                 CONSTRAINT DF_ExternalDocument_Id DEFAULT NEWSEQUENTIALID(),
        TypeCode                 VARCHAR(40)    NOT NULL CONSTRAINT FK_ExternalDocument_Type REFERENCES dbo.ExternalDocumentType(Code),
        ControlReportNumber      VARCHAR(100)   NULL,
        ArticleCode              VARCHAR(30)    NULL CONSTRAINT FK_ExternalDocument_Article REFERENCES dbo.Article(CodeArticle),
        DeliveryNoteNumber       VARCHAR(30)    NULL CONSTRAINT FK_ExternalDocument_DeliveryNote REFERENCES dbo.SDELIVERY(NumeroBL),
        ManufacturingOrderNumber VARCHAR(30)    NULL CONSTRAINT FK_ExternalDocument_ManufacturingOrder REFERENCES dbo.MFGHEAD_OrdreFabrication(NumeroOF),
        SupplierOrderNumber      VARCHAR(30)    NULL CONSTRAINT FK_ExternalDocument_SupplierOrder REFERENCES dbo.PORDER(NumeroCommande),
        -- Commande client Sage (commande ouverte) livrée par le BL ; elle ne fait pas partie du nom.
        SalesOrderNumber         VARCHAR(30)    NULL CONSTRAINT FK_ExternalDocument_SalesOrder REFERENCES dbo.SORDER(NumeroCommande),
        -- Référence de commande du client (SORDER.ReferenceCommandeClient, vérifiée par TR_ExternalDocument_Rules).
        CustomerOrderReference   VARCHAR(50)    NULL,
        FileName                 AS CAST(CONCAT_WS('-', ControlReportNumber, ArticleCode, DeliveryNoteNumber, ManufacturingOrderNumber,
                                                   SupplierOrderNumber, TRANSLATE(CustomerOrderReference, ' /', '__')) + '.pdf' AS VARCHAR(400)) PERSISTED,
        -- Ce qui identifie le document dans son type : le numéro du rapport, le numéro du BL, sinon son nom.
        DocumentKey              AS CAST(CASE TypeCode
                                             WHEN 'CONTROL_REPORT' THEN ControlReportNumber
                                             WHEN 'DELIVERY_NOTE' THEN DeliveryNoteNumber
                                             ELSE CONCAT_WS('-', ControlReportNumber, ArticleCode, DeliveryNoteNumber, ManufacturingOrderNumber,
                                                            SupplierOrderNumber, TRANSLATE(CustomerOrderReference, ' /', '__'))
                                         END AS VARCHAR(400)) PERSISTED,
        OriginalFileName         NVARCHAR(260)  NOT NULL,
        Content                  VARBINARY(MAX) NOT NULL,
        ContentSha256            AS CONVERT(CHAR(64), HASHBYTES('SHA2_256', Content), 2) PERSISTED,
        UploadedBy               VARCHAR(20)    NOT NULL CONSTRAINT FK_ExternalDocument_UploadedBy REFERENCES dbo.AUTILIS(USR_0),
        UploadedAt               DATETIME2(3)   NOT NULL CONSTRAINT DF_ExternalDocument_UploadedAt DEFAULT SYSDATETIME(),
        -- Remplacement : le nouveau document nomme celui qu'il remplace et pourquoi ; l'ancien reçoit la date de son remplacement.
        ReplacesDocumentId       UNIQUEIDENTIFIER NULL CONSTRAINT FK_ExternalDocument_Replaces REFERENCES dbo.ExternalDocument(Id),
        ReplacementReason        NVARCHAR(300)  NULL,
        SupersededAt             DATETIME2(3)   NULL,
        -- Numéros en majuscules, chiffres, « . », « _ » et « - » : ils forment le nom du fichier. La référence client, saisie
        -- librement dans Sage, ne contient aucun caractère interdit dans un nom de fichier et n'a pas d'espace aux extrémités.
        CONSTRAINT CK_ExternalDocument_Numbers CHECK (
            (ControlReportNumber IS NULL OR (LEN(ControlReportNumber) > 0 AND ControlReportNumber NOT LIKE '%[^A-Z0-9._-]%'))
            AND (ArticleCode IS NULL OR (LEN(ArticleCode) > 0 AND ArticleCode NOT LIKE '%[^A-Z0-9._-]%'))
            AND (DeliveryNoteNumber IS NULL OR (LEN(DeliveryNoteNumber) > 0 AND DeliveryNoteNumber NOT LIKE '%[^A-Z0-9._-]%'))
            AND (ManufacturingOrderNumber IS NULL OR (LEN(ManufacturingOrderNumber) > 0 AND ManufacturingOrderNumber NOT LIKE '%[^A-Z0-9._-]%'))
            AND (SupplierOrderNumber IS NULL OR (LEN(SupplierOrderNumber) > 0 AND SupplierOrderNumber NOT LIKE '%[^A-Z0-9._-]%'))
            AND (SalesOrderNumber IS NULL OR (LEN(SalesOrderNumber) > 0 AND SalesOrderNumber NOT LIKE '%[^A-Z0-9._-]%'))
            AND (CustomerOrderReference IS NULL OR (LEN(CustomerOrderReference) > 0 AND CustomerOrderReference = LTRIM(RTRIM(CustomerOrderReference))
                                                     AND CustomerOrderReference NOT LIKE '%[\:*?"<>|]%'))),
        CONSTRAINT CK_ExternalDocument_Replacement CHECK (
            (ReplacesDocumentId IS NULL AND ReplacementReason IS NULL)
            OR (ReplacesDocumentId IS NOT NULL AND LEN(LTRIM(RTRIM(ReplacementReason))) > 0))
    );
    CREATE UNIQUE INDEX UX_ExternalDocument_Current ON dbo.ExternalDocument(TypeCode, DocumentKey) WHERE SupersededAt IS NULL;
    CREATE INDEX IX_ExternalDocument_ControlReport ON dbo.ExternalDocument(ControlReportNumber) WHERE SupersededAt IS NULL;
    CREATE INDEX IX_ExternalDocument_Article ON dbo.ExternalDocument(ArticleCode) WHERE SupersededAt IS NULL;
    CREATE INDEX IX_ExternalDocument_SupplierOrder ON dbo.ExternalDocument(SupplierOrderNumber) WHERE SupersededAt IS NULL;
    CREATE INDEX IX_ExternalDocument_DeliveryNote ON dbo.ExternalDocument(DeliveryNoteNumber) WHERE SupersededAt IS NULL;
    CREATE INDEX IX_ExternalDocument_CustomerOrder ON dbo.ExternalDocument(CustomerOrderReference) WHERE SupersededAt IS NULL;
    CREATE INDEX IX_ExternalDocument_SalesOrder ON dbo.ExternalDocument(SalesOrderNumber) WHERE SupersededAt IS NULL;
    CREATE INDEX IX_ExternalDocument_ManufacturingOrder ON dbo.ExternalDocument(ManufacturingOrderNumber) WHERE SupersededAt IS NULL;
    CREATE INDEX IX_ExternalDocument_UploadedAt ON dbo.ExternalDocument(TypeCode, UploadedAt DESC);
END

EXEC(N'CREATE OR ALTER TRIGGER TR_ExternalDocument_Rules ON dbo.ExternalDocument
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM inserted) AND EXISTS (SELECT 1 FROM deleted)
        THROW 50002, N''Un document externe ne peut pas être supprimé : déposez un nouveau fichier qui le remplace.'', 1;

    IF EXISTS (SELECT 1 FROM deleted)
    BEGIN
        IF UPDATE(TypeCode) OR UPDATE(ControlReportNumber) OR UPDATE(ArticleCode) OR UPDATE(DeliveryNoteNumber)
           OR UPDATE(ManufacturingOrderNumber) OR UPDATE(SupplierOrderNumber) OR UPDATE(SalesOrderNumber) OR UPDATE(CustomerOrderReference)
           OR UPDATE(OriginalFileName) OR UPDATE(Content) OR UPDATE(UploadedBy) OR UPDATE(UploadedAt)
           OR UPDATE(ReplacesDocumentId) OR UPDATE(ReplacementReason)
           OR EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.Id = i.Id WHERE d.SupersededAt IS NOT NULL OR i.SupersededAt IS NULL)
            THROW 50002, N''Un document externe déposé ne change plus : seul son remplacement peut être enregistré, une fois.'', 1;
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.ExternalDocumentType t ON t.Code = i.TypeCode
               WHERE (t.RequiresControlReportNumber = 1 AND i.ControlReportNumber IS NULL) OR (t.RequiresControlReportNumber = 0 AND i.ControlReportNumber IS NOT NULL)
                  OR (t.RequiresArticleCode = 1 AND i.ArticleCode IS NULL) OR (t.RequiresArticleCode = 0 AND i.ArticleCode IS NOT NULL)
                  OR (t.RequiresDeliveryNoteNumber = 1 AND i.DeliveryNoteNumber IS NULL) OR (t.RequiresDeliveryNoteNumber = 0 AND i.DeliveryNoteNumber IS NOT NULL)
                  OR (t.RequiresManufacturingOrderNumber = 1 AND i.ManufacturingOrderNumber IS NULL) OR (t.RequiresManufacturingOrderNumber = 0 AND i.ManufacturingOrderNumber IS NOT NULL)
                  OR (t.RequiresSupplierOrderNumber = 1 AND i.SupplierOrderNumber IS NULL) OR (t.RequiresSupplierOrderNumber = 0 AND i.SupplierOrderNumber IS NOT NULL)
                  OR (t.RequiresSalesOrderNumber = 1 AND i.SalesOrderNumber IS NULL) OR (t.RequiresSalesOrderNumber = 0 AND i.SalesOrderNumber IS NOT NULL)
                  OR (t.RequiresCustomerOrderReference = 1 AND i.CustomerOrderReference IS NULL) OR (t.RequiresCustomerOrderReference = 0 AND i.CustomerOrderReference IS NOT NULL))
        THROW 50003, N''Un document externe porte exactement les numéros exigés par son type.'', 1;

    IF EXISTS (SELECT 1 FROM inserted i WHERE i.CustomerOrderReference IS NOT NULL
                 AND NOT EXISTS (SELECT 1 FROM dbo.SORDER o WHERE o.ReferenceCommandeClient = i.CustomerOrderReference))
        THROW 50003, N''La référence de commande client est inconnue de Sage.'', 1;

    IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.SDELIVERY b ON b.NumeroBL = i.DeliveryNoteNumber
               WHERE b.NumeroCommande IS NOT NULL AND b.NumeroCommande <> i.SalesOrderNumber)
        THROW 50003, N''La commande d''''un BL est celle que Sage donne à ce BL.'', 1;

    IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.SORDER o ON o.NumeroCommande = i.SalesOrderNumber
               WHERE o.ReferenceCommandeClient IS NULL OR o.ReferenceCommandeClient <> i.CustomerOrderReference)
        THROW 50003, N''La référence client d''''un BL est celle de sa commande dans Sage.'', 1;

    IF EXISTS (SELECT 1 FROM inserted i LEFT JOIN dbo.ExternalDocument r ON r.Id = i.ReplacesDocumentId
               WHERE i.ReplacesDocumentId IS NOT NULL
                 AND (r.Id IS NULL OR r.TypeCode <> i.TypeCode OR r.DocumentKey <> i.DocumentKey OR r.SupersededAt IS NULL))
        THROW 50003, N''Un document remplace un document de même type et de même numéro, marqué remplacé.'', 1;
END;
');

COMMIT TRANSACTION;
PRINT 'Migration 2026-10-18 (documents externes : commandes Sage, rôles des services, documents déposés) appliquée.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH
GO
