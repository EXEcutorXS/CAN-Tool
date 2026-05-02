# Generates sheets_export/*.csv from omnidata.json
# Run: powershell -ExecutionPolicy Bypass -File generate_csv.ps1

$jsonPath = "$PSScriptRoot\..\CAN Tool\Resources\omnidata.json"
$outDir   = "$PSScriptRoot\sheets_export"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$data = Get-Content $jsonPath -Raw -Encoding UTF8 | ConvertFrom-Json

function OrE($v)   { if ($null -eq $v) { return '' }; return "$v" }
function BoolC($v) { if ($v) { return 'TRUE' }; return '' }
function MStr($v) {
    if ($null -eq $v) { return '' }
    if ($v -is [string]) { return $v }
    $p = @()
    $v.PSObject.Properties | ForEach-Object { $p += "$($_.Name)=$($_.Value)" }
    return ($p -join '|')
}
function EscC($v) {
    $s = "$v"
    if ($s -match '[,"\r\n]') { return '"' + $s.Replace('"','""') + '"' }
    return $s
}
function SaveCsv($rows, $file) {
    $lines = $rows | ForEach-Object { ($_ | ForEach-Object { EscC $_ }) -join ',' }
    [System.IO.File]::WriteAllLines("$outDir\$file", $lines, [System.Text.UTF8Encoding]::new($false))
    Write-Host "$file : $($rows.Count - 1) rows"
}

# pgns
$pg = [System.Collections.ArrayList]@()
[void]$pg.Add(@('id','name','multiPack','manual','notes'))
foreach ($p in $data.pgns) {
    [void]$pg.Add(@($p.id, (OrE $p.name), (BoolC $p.multiPack), '', ''))
}
SaveCsv $pg 'pgns.csv'

# parameters — column order must stay in sync with codegen.py
$hdr = @('pgn','packNumber','name','startByte','startBit','bitLength','signed',
         'a','b','unitType',
         'meanings',    # col 11: preset name OR "0=x|1=y" inline dict
         'getMeaning',  # col 12: custom decoder key (device_name, error_code…)
         'decoder','answerOnly','var',
         'varName','cVar','cType','nodata','condition','postHook')
$pm = [System.Collections.ArrayList]@()
[void]$pm.Add($hdr)
foreach ($p in $data.parameters) {
    $sb = if ($null -ne $p.startBit) { $p.startBit } else { 0 }
    [void]$pm.Add(@(
        $p.pgn, (OrE $p.packNumber), (OrE $p.name),
        (OrE $p.startByte), $sb, (OrE $p.bitLength),
        (BoolC $p.signed), (OrE $p.a), (OrE $p.b), (OrE $p.unitType),
        (MStr $p.meanings), (OrE $p.getMeaning), (OrE $p.decoder),
        (BoolC $p.answerOnly), (OrE $p.var),
        '','','','','',''
    ))
}
SaveCsv $pm 'parameters.csv'

# meaning presets
$pr = [System.Collections.ArrayList]@()
[void]$pr.Add(@('presetName','value','label'))
$data.meaningPresets.PSObject.Properties | ForEach-Object {
    $n = $_.Name
    $_.Value.PSObject.Properties | ForEach-Object { [void]$pr.Add(@($n, $_.Name, $_.Value)) }
}
SaveCsv $pr 'meaning_presets.csv'

Write-Host "Done: $outDir"
