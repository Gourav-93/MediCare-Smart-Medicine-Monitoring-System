$files = Get-ChildItem 'c:\Users\Dell\OneDrive\Desktop\MediCare\Controllers\*.cs'
$endpoints = @()

foreach ($file in $files) {
    $content = Get-Content $file.FullName
    $baseRoute = ''
    foreach ($line in $content) {
        if ($line -match '\[Route\("([^"]+)"\)\]') {
            $baseRoute = $matches[1]
            break
        }
    }
    
    for ($i = 0; $i -lt $content.Count; $i++) {
        $line = $content[$i].Trim()
        if ($line -match '^\[Http(Get|Post|Put|Delete)(\("([^"]*)"\))?\]') {
            $verb = $matches[1].ToUpper()
            $sub = $matches[3]
            $route = $baseRoute
            if ($sub) {
                if ($sub.StartsWith("/")) { 
                    $route = $sub.TrimStart("/") 
                } else { 
                    $route = "$baseRoute/$sub" 
                }
            }
            $action = ""
            for ($j = $i + 1; $j -lt [Math]::Min($i + 5, $content.Count); $j++) {
                if ($content[$j] -match 'public\s+(async\s+)?(Task<IActionResult>|IActionResult)\s+([A-Za-z0-9_]+)') {
                    $action = $matches[3]
                    break
                }
            }
            $endpoints += [PSCustomObject]@{
                Controller = $file.BaseName
                Verb = $verb
                Route = $route
                Action = $action
            }
        }
    }
}

$endpoints | Format-Table -AutoSize
Write-Host "Total Endpoints: $($endpoints.Count)"
