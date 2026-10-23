$login = Invoke-RestMethod -Uri 'http://localhost:5000/api/auth/login' -Method Post -ContentType 'application/json' -Body (@{ email = 'testpatient@medicare.com'; password = 'Password123' } | ConvertTo-Json)
$token = $login.token
Write-Host "JWT Token Acquired for Patient ID: $($login.userId)"

$headers = @{ 'Authorization' = "Bearer $token" }

# 1. Test GET /api/dashboard/patient
$dash = Invoke-RestMethod -Uri 'http://localhost:5000/api/dashboard/patient' -Method Get -Headers $headers
Write-Host "Patient Dashboard: Total Medicines: $($dash.totalMedicines), Adherence: $($dash.adherencePercentage)%"

# 2. Test POST /api/emergency
$sos = Invoke-RestMethod -Uri 'http://localhost:5000/api/emergency' -Method Post -ContentType 'application/json' -Headers $headers -Body (@{ message = 'Verification distress test' } | ConvertTo-Json)
Write-Host "Emergency Created: ID $($sos.emergencyId), Status: $($sos.status)"

# 3. Test GET /api/emergency
$emergencies = Invoke-RestMethod -Uri 'http://localhost:5000/api/emergency' -Method Get -Headers $headers
Write-Host "Emergencies Retrieved: $($emergencies.Count)"

# 4. Test PUT /api/emergency/{id}/resolve
$resolved = Invoke-RestMethod -Uri "http://localhost:5000/api/emergency/$($sos.emergencyId)/resolve" -Method Put -Headers $headers
Write-Host "Emergency Resolved: Status $($resolved.status)"

# 5. Test GET /api/notification
$notifs = Invoke-RestMethod -Uri 'http://localhost:5000/api/notification' -Method Get -Headers $headers
Write-Host "Notifications Count: $($notifs.Count)"

# 6. Test PUT /api/notification/read-all
$readAll = Invoke-RestMethod -Uri 'http://localhost:5000/api/notification/read-all' -Method Put -Headers $headers
Write-Host "Notification Read All Response: $readAll"

# 7. Test POST /api/medicine
$newMed = Invoke-RestMethod -Uri 'http://localhost:5000/api/medicine' -Method Post -ContentType 'application/json' -Headers $headers -Body (@{
    patientId = $dash.patient.id;
    name = "Amoxicillin 250mg";
    dosage = "1 Capsule";
    dose = 1;
    stock = 15;
    startDate = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ss");
    endDate = (Get-Date).AddMonths(1).ToString("yyyy-MM-ddTHH:mm:ss");
    mealTiming = "AfterMeal"
} | ConvertTo-Json)
Write-Host "Medicine Added: ID $($newMed.id), Name: $($newMed.name)"

# 8. Test POST /api/medicineschedule
$newSched = Invoke-RestMethod -Uri 'http://localhost:5000/api/medicineschedule' -Method Post -ContentType 'application/json' -Headers $headers -Body (@{
    medicineId = $newMed.id;
    time = "09:00:00";
    frequency = "Daily"
} | ConvertTo-Json)
Write-Host "Schedule Added: ID $($newSched.id), Time: $($newSched.time)"

# 9. Test GET /api/medicineschedule
$allScheds = Invoke-RestMethod -Uri 'http://localhost:5000/api/medicineschedule' -Method Get -Headers $headers
Write-Host "All Schedules: $($allScheds.Count)"

# 10. Test POST /api/medicinelog
$newLog = Invoke-RestMethod -Uri 'http://localhost:5000/api/medicinelog' -Method Post -ContentType 'application/json' -Headers $headers -Body (@{
    medicineId = $newMed.id;
    patientId = $dash.patient.id;
    scheduledTime = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ss");
    status = "Pending"
} | ConvertTo-Json)
Write-Host "Medicine Log Added: ID $($newLog.id), Status: $($newLog.status)"

# 11. Test PUT /api/medicinelog/{id}/snooze
$snoozed = Invoke-RestMethod -Uri "http://localhost:5000/api/medicinelog/$($newLog.id)/snooze?minutes=15" -Method Put -Headers $headers
Write-Host "Medicine Log Snoozed: $($snoozed.message), Count: $($snoozed.snoozeCount)"

# 12. Test PUT /api/medicinelog/{id}/taken
$taken = Invoke-RestMethod -Uri "http://localhost:5000/api/medicinelog/$($newLog.id)/taken" -Method Put -Headers $headers
Write-Host "Medicine Log Taken: ID $($taken.id), Status: $($taken.status), Stock Now: $($taken.medicine.stock)"

# 13. Test GET /api/medicinelog/patient/{id}/history
$history = Invoke-RestMethod -Uri "http://localhost:5000/api/medicinelog/patient/$($dash.patient.id)/history" -Method Get -Headers $headers
Write-Host "Patient History Count: $($history.Count)"

# 14. Test GET /api/adherence/patient/{id}
$adherence = Invoke-RestMethod -Uri "http://localhost:5000/api/adherence/patient/$($dash.patient.id)" -Method Get -Headers $headers
Write-Host "Adherence: $($adherence.adherencePercentage)%, Total Doses: $($adherence.totalDoses)"

# 15. Test GET /api/adherence/patient/{id}/report
$adhReport = Invoke-RestMethod -Uri "http://localhost:5000/api/adherence/patient/$($dash.patient.id)/report?period=Daily" -Method Get -Headers $headers
Write-Host "Adherence Timeline Points: $($adhReport.timeline.Count)"

# 16. Test GET /api/report/patient/{id}/data
$reportData = Invoke-RestMethod -Uri "http://localhost:5000/api/report/patient/$($dash.patient.id)/data?days=30" -Method Get -Headers $headers
Write-Host "Report Data: Total Recorded: $($reportData.totalDosesRecorded), Adherence: $($reportData.overallAdherence)%"

# 17. Clean up created schedule & medicine
$delSched = Invoke-RestMethod -Uri "http://localhost:5000/api/medicineschedule/$($newSched.id)" -Method Delete -Headers $headers
Write-Host "Schedule Deleted: $delSched"

$delMed = Invoke-RestMethod -Uri "http://localhost:5000/api/medicine/$($newMed.id)" -Method Delete -Headers $headers
Write-Host "Medicine Deleted: $delMed"
