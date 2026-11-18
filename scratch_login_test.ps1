try {
    $body = @{
        email = "livetest_patient_20261028161704@medicare.com"
        password = "Password123!"
    } | ConvertTo-Json
    $res = Invoke-RestMethod -Uri 'http://localhost:5000/api/auth/login' -Method Post -ContentType 'application/json' -Body $body
    Write-Host "LOGIN_SUCCESS: Email=$($res.email), Role=$($res.role), TokenLength=$($res.token.Length)"
} catch {
    Write-Host "LOGIN_ERROR: $($_.Exception.Message)"
}
