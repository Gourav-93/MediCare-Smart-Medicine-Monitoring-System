# Comprehensive verification test for remaining endpoints
$pLogin = Invoke-RestMethod -Uri 'http://localhost:5000/api/auth/login' -Method POST -Body '{"Email":"gourav.patient@medicare.com","Password":"Password123"}' -ContentType 'application/json' -ErrorAction SilentlyContinue
if (-not $pLogin) {
    $pLogin = Invoke-RestMethod -Uri 'http://localhost:5000/api/auth/login' -Method POST -Body '{"Email":"testpatient@medicare.com","Password":"Password123"}' -ContentType 'application/json'
}
$pHeaders = @{ Authorization = "Bearer $($pLogin.token)" }

$adminLogin = Invoke-RestMethod -Uri 'http://localhost:5000/api/auth/login' -Method POST -Body '{"Email":"admin@medicare.com","Password":"Password123"}' -ContentType 'application/json'
$adminHeaders = @{ Authorization = "Bearer $($adminLogin.token)" }

Write-Host "--- Testing Adherence Endpoints with Admin Token ---"
try {
    $adh = Invoke-RestMethod -Uri 'http://localhost:5000/api/adherence/patient/1' -Method GET -Headers $adminHeaders
    Write-Host "GET api/adherence/patient/1 Success! Total Logs:" $adh.totalLogs "Adherence:" $adh.adherenceRate
} catch {
    Write-Host "GET api/adherence/patient/1 Error:" $_.Exception.Message
}

try {
    $adhRep = Invoke-RestMethod -Uri 'http://localhost:5000/api/adherence/patient/1/report?period=Daily' -Method GET -Headers $adminHeaders
    Write-Host "GET api/adherence/patient/1/report Success! Adherence Rate:" $adhRep.adherenceRate
} catch {
    Write-Host "GET api/adherence/patient/1/report Error:" $_.Exception.Message
}

Write-Host "`n--- Testing Medicine Low-Stock & Single ID Endpoints ---"
try {
    $lowStock = Invoke-RestMethod -Uri 'http://localhost:5000/api/medicine/low-stock' -Method GET -Headers $pHeaders
    Write-Host "GET api/medicine/low-stock Success! Low stock count:" ($lowStock | Measure-Object).Count
} catch {
    Write-Host "GET api/medicine/low-stock Error:" $_.Exception.Message
}

try {
    $allMeds = Invoke-RestMethod -Uri 'http://localhost:5000/api/medicine' -Method GET -Headers $pHeaders
    $firstMedId = $allMeds[0].id
    $medById = Invoke-RestMethod -Uri "http://localhost:5000/api/medicine/$firstMedId" -Method GET -Headers $pHeaders
    Write-Host "GET api/medicine/$firstMedId Success! Name:" $medById.name

    # Test PUT api/medicine/{id}
    $updateMed = Invoke-RestMethod -Uri "http://localhost:5000/api/medicine/$firstMedId" -Method PUT -Headers $pHeaders -Body (@{
        id = $firstMedId;
        name = $medById.name;
        dosage = $medById.dosage;
        stock = $medById.stock;
        dose = $medById.dose;
        mealTimeing = $medById.mealTimeing;
        startDate = $medById.startDate;
        endDate = $medById.endDate;
        patientId = $medById.patientId;
    } | ConvertTo-Json) -ContentType 'application/json'
    Write-Host "PUT api/medicine/$firstMedId Success! Updated:" $updateMed.name
} catch {
    Write-Host "Medicine single ID / update error:" $_.Exception.Message
}

Write-Host "`n--- Testing Single Notification Endpoints ---"
$adminLogin = Invoke-RestMethod -Uri 'http://localhost:5000/api/auth/login' -Method POST -Body '{"Email":"admin@medicare.com","Password":"Password123"}' -ContentType 'application/json'
$adminHeaders = @{ Authorization = "Bearer $($adminLogin.token)" }

try {
    $newNotif = Invoke-RestMethod -Uri 'http://localhost:5000/api/notification' -Method POST -Headers $adminHeaders -Body '{"UserId": 1, "Message": "Test notification item", "Type": "SYSTEM"}' -ContentType 'application/json'
    $notifId = $newNotif.id
    Write-Host "Created notification ID:" $notifId

    $getNotif = Invoke-RestMethod -Uri "http://localhost:5000/api/notification/$notifId" -Method GET -Headers $adminHeaders
    Write-Host "GET api/notification/$notifId Success! Message:" $getNotif.message

    $markRead = Invoke-RestMethod -Uri "http://localhost:5000/api/notification/$notifId/read" -Method PUT -Headers $adminHeaders
    Write-Host "PUT api/notification/$notifId/read Success! IsRead:" $markRead.isRead
} catch {
    Write-Host "Notification single ID error:" $_.Exception.Message
}

Write-Host "`n--- Testing Single MedicineSchedule & Log Endpoints ---"
try {
    $sched = Invoke-RestMethod -Uri 'http://localhost:5000/api/medicineschedule' -Method POST -Headers $pHeaders -Body (@{
        medicineId = $firstMedId;
        time = "10:30:00";
        frequency = "Daily"
    } | ConvertTo-Json) -ContentType 'application/json'
    $schedId = $sched.id
    Write-Host "Created schedule ID:" $schedId

    $getSched = Invoke-RestMethod -Uri "http://localhost:5000/api/medicineschedule/$schedId" -Method GET -Headers $pHeaders
    Write-Host "GET api/medicineschedule/$schedId Success! Time:" $getSched.time

    $putSched = Invoke-RestMethod -Uri "http://localhost:5000/api/medicineschedule/$schedId" -Method PUT -Headers $pHeaders -Body (@{
        id = $schedId;
        medicineId = $firstMedId;
        time = "11:00:00";
        frequency = "Daily"
    } | ConvertTo-Json) -ContentType 'application/json'
    Write-Host "PUT api/medicineschedule/$schedId Success!"

    $delSched = Invoke-RestMethod -Uri "http://localhost:5000/api/medicineschedule/$schedId" -Method DELETE -Headers $pHeaders
    Write-Host "DELETE api/medicineschedule/$schedId Success!"
} catch {
    Write-Host "MedicineSchedule error:" $_.Exception.Message
}

try {
    $log = Invoke-RestMethod -Uri 'http://localhost:5000/api/medicinelog' -Method POST -Headers $pHeaders -Body (@{
        medicineId = $firstMedId;
        status = "Pending";
        takenTime = (Get-Date).ToString("o")
    } | ConvertTo-Json) -ContentType 'application/json'
    $logId = $log.id
    Write-Host "Created log ID:" $logId

    $getLog = Invoke-RestMethod -Uri "http://localhost:5000/api/medicinelog/$logId" -Method GET -Headers $pHeaders
    Write-Host "GET api/medicinelog/$logId Success! Status:" $getLog.status
} catch {
    Write-Host "MedicineLog single error:" $_.Exception.Message
}

Write-Host "`n--- Testing GET api/Caregiverpatient/caregiver/{caregiverId} ---"
try {
    $cgLogin = Invoke-RestMethod -Uri 'http://localhost:5000/api/auth/login' -Method POST -Body '{"Email":"sarah@medicare.com","Password":"Password123!"}' -ContentType 'application/json'
    $cgHeaders = @{ Authorization = "Bearer $($cgLogin.token)" }
    $cgPatients = Invoke-RestMethod -Uri 'http://localhost:5000/api/Caregiverpatient/caregiver/1' -Method GET -Headers $cgHeaders
    Write-Host "GET api/Caregiverpatient/caregiver/1 Success! Count:" ($cgPatients | Measure-Object).Count
} catch {
    Write-Host "Caregiverpatient by caregiver error:" $_.Exception.Message
}
