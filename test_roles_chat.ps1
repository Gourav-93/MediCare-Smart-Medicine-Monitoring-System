$login = Invoke-RestMethod -Uri 'http://localhost:5000/api/auth/login' -Method POST -Body '{"Email":"testpatient@medicare.com","Password":"Password123"}' -ContentType 'application/json'
$headers = @{ Authorization = "Bearer $($login.token)" }

Write-Host "Testing Chat API..."
try {
    $chatRes = Invoke-RestMethod -Uri 'http://localhost:5000/api/chat/ask' -Method POST -Headers $headers -Body '{"Message":"What is Amoxicillin?"}' -ContentType 'application/json'
    Write-Host "Chat Success! AI Response:" $chatRes.reply
} catch {
    Write-Host "Chat Error:" $_.Exception.Message
}

# Now let's register an Admin user and test Admin endpoints
Write-Host "`nRegistering / Logging in Admin..."
# Admin Login
Write-Host "`nLogging in Admin (admin@medicare.com)..."
$adminLogin = Invoke-RestMethod -Uri 'http://localhost:5000/api/auth/login' -Method POST -Body '{"Email":"admin@medicare.com","Password":"Password123"}' -ContentType 'application/json'
$adminHeaders = @{ Authorization = "Bearer $($adminLogin.token)" }

Write-Host "Testing GET /api/dashboard/admin..."
try {
    $adminDash = Invoke-RestMethod -Uri 'http://localhost:5000/api/dashboard/admin' -Method GET -Headers $adminHeaders
    Write-Host "Admin Dash Success! Total Users:" $adminDash.totalUsers "Total Medicines:" $adminDash.totalMedicines
} catch {
    Write-Host "Admin Dash Error:" $_.Exception.Message
}

Write-Host "Testing POST /api/notification (Admin Broadcast)..."
try {
    $adminNotif = Invoke-RestMethod -Uri 'http://localhost:5000/api/notification' -Method POST -Headers $adminHeaders -Body '{"UserId": 3, "Message": "System maintenance scheduled", "Type": "SYSTEM"}' -ContentType 'application/json'
    Write-Host "Admin Notification Created ID:" $adminNotif.id
} catch {
    Write-Host "Admin Notification Error:" $_.Exception.Message
}

# Now register a Caregiver user
Write-Host "`nRegistering / Logging in Caregiver..."
try {
    $regCg = Invoke-RestMethod -Uri 'http://localhost:5000/api/auth/register' -Method POST -Body '{"Name":"Sarah Nurse","Email":"sarah@medicare.com","Password":"Password123!","Role":"CAREGIVER"}' -ContentType 'application/json'
} catch {}

$cgLogin = Invoke-RestMethod -Uri 'http://localhost:5000/api/auth/login' -Method POST -Body '{"Email":"sarah@medicare.com","Password":"Password123!"}' -ContentType 'application/json'
$cgHeaders = @{ Authorization = "Bearer $($cgLogin.token)" }

Write-Host "Testing GET /api/dashboard/caregiver..."
try {
    $cgDash = Invoke-RestMethod -Uri 'http://localhost:5000/api/dashboard/caregiver' -Method GET -Headers $cgHeaders
    Write-Host "Caregiver Dash Success! Total Patients:" $cgDash.totalPatients
} catch {
    Write-Host "Caregiver Dash Error:" $_.Exception.Message
}

Write-Host "Testing GET /api/caregiver/patients-contact-cards..."
try {
    $cgCards = Invoke-RestMethod -Uri 'http://localhost:5000/api/caregiver/patients-contact-cards' -Method GET -Headers $cgHeaders
    Write-Host "Caregiver Contact Cards Success! Count:" ($cgCards | Measure-Object).Count
} catch {
    Write-Host "Caregiver Contact Cards Error:" $_.Exception.Message
}

Write-Host "Testing POST /api/Caregiverpatient/link..."
try {
    # Link patient (Id 3 user, let's find patient ID or pass 1)
    $linkRes = Invoke-RestMethod -Uri 'http://localhost:5000/api/Caregiverpatient/link' -Method POST -Headers $cgHeaders -Body '{"CaregiverId": 1, "PatientId": 1}' -ContentType 'application/json'
    Write-Host "Caregiver-Patient Link Success:" $linkRes
} catch {
    Write-Host "Link Error:" $_.Exception.Message
}

Write-Host "Testing GET /api/Caregiverpatient/patient/1..."
try {
    $pLinks = Invoke-RestMethod -Uri 'http://localhost:5000/api/Caregiverpatient/patient/1' -Method GET -Headers $cgHeaders
    Write-Host "Caregiverpatient by Patient Success! Count:" ($pLinks | Measure-Object).Count
} catch {
    Write-Host "Caregiverpatient by Patient Error:" $_.Exception.Message
}

Write-Host "Testing DELETE /api/Caregiverpatient/unlink?caregiverId=1&patientId=1..."
try {
    $unlinkRes = Invoke-RestMethod -Uri 'http://localhost:5000/api/Caregiverpatient/unlink?caregiverId=1&patientId=1' -Method DELETE -Headers $cgHeaders
    Write-Host "Caregiver-Patient Unlink Success:" $unlinkRes
} catch {
    Write-Host "Unlink Error:" $_.Exception.Message
}
