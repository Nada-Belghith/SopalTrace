$path = "SopalTraceApp/SopalTrace.Infrastructure/Services/ExcelImportService.VM.cs"
$content = Get-Content $path -Raw -Encoding UTF8

# On remplace les caractères corrompus s'ils sont là (Ã© etc) ou les normaux (Pièce)
# On utilise directement les séquences Unicode pour être sûr
$content = $content -replace 'Pi[èceÃ¨]+ de R[éférenceÃ©]+', 'Pi\u00e8ce de R\u00e9f\u00e9rence'
$content = $content -replace 'Fuite [ÉtalonÃ‰]+', 'Fuite \u00c9talon'

# On sauvegarde en UTF8 avec BOM (très important pour MSBuild sur Windows)
[System.IO.File]::WriteAllText($path, $content, [System.Text.Encoding]::UTF8)
Write-Output "Fixed $path"

$path2 = "SopalTraceApp/SopalTrace.Infrastructure/Services/ExcelImportService.cs"
$content2 = Get-Content $path2 -Raw -Encoding UTF8
$content2 = $content2 -replace 'Caract[éristiqueÃ©]+ contr[ôléeÃ´]+', 'Caract\u00e9ristique contr\u00f4l\u00e9e'
$content2 = $content2 -replace 'Caract[érisiquesÃ©]+', 'Caract\u00e9ristiques'
[System.IO.File]::WriteAllText($path2, $content2, [System.Text.Encoding]::UTF8)
Write-Output "Fixed $path2"
