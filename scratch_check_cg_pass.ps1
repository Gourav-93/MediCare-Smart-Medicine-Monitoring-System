$passwords = @("Password123!", "Password123", "Caregiver123!", "Caregiver123", "caregiver", "Gourav@123", "Gourav123", "123456", "Admin123!")
foreach ($p in $passwords) {
    try {
        $body = @{ email = "caregiver@gmail.com"; password = $p } | ConvertTo-Json
        $res = Invoke-RestMethod -Uri 'http://localhost:5000/api/auth/login' -Method Post -ContentType 'application/json' -Body $body
        Write-Host "FOUND PASSWORD: $p, Role=$($res.role), TokenLength=$($res.token.Length)"
        break
    } catch {}
}
