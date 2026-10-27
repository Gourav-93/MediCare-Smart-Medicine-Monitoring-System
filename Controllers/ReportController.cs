using System.Security.Claims;
using System.Text;
using mediCare.DTOs;
using MediCare.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Controllers;

[ApiController]
[Route("api/report")]
[Authorize(Roles = "ADMIN,PATIENT,CAREGIVER")]
public class ReportController : ControllerBase
{
    private readonly AppDbContext _context;

    public ReportController(AppDbContext context)
    {
        _context = context;
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : 0;
    }

    private string GetCurrentRole()
    {
        return User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    }

    private async Task<bool> CanAccessPatientAsync(int patientId)
    {
        var userId = GetCurrentUserId();
        var role = GetCurrentRole();

        if (role == "ADMIN")
            return await _context.Patients.AnyAsync(p => p.Id == patientId);

        if (role == "PATIENT")
            return await _context.Patients.AnyAsync(p => p.Id == patientId && p.UserId == userId);

        if (role == "CAREGIVER")
        {
            var caregiver = await _context.Caregivers.FirstOrDefaultAsync(c => c.UserId == userId);
            if (caregiver == null) return false;

            return await _context.CaregiverPatients
                .AnyAsync(cp => cp.CaregiverId == caregiver.Id && cp.PatientId == patientId);
        }

        return false;
    }

    // GET: api/report/patient/{patientId}/data
    [HttpGet("patient/{patientId}/data")]
    public async Task<IActionResult> GetReportData(int patientId, [FromQuery] int days = 30)
    {
        if (!await CanAccessPatientAsync(patientId))
            return Forbid();

        var patient = await _context.Patients
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == patientId);

        if (patient == null)
            return NotFound("Patient not found.");

        var sinceDate = DateTime.Today.AddDays(-days);

        var medicines = await _context.Medicines
            .Include(m => m.MedicineSchedules)
            .Where(m => m.PatientId == patientId && !m.IsDeleted)
            .ToListAsync();

        var logs = await _context.MedicineLogs
            .Include(l => l.Medicine)
            .Where(l => l.PatientId == patientId && l.ScheduledTime >= sinceDate)
            .OrderByDescending(l => l.ScheduledTime)
            .ToListAsync();

        var totalDoses = logs.Count(l => l.Status == "Taken" || l.Status == "Missed");
        var taken = logs.Count(l => l.Status == "Taken");
        var missed = logs.Count(l => l.Status == "Missed");
        var adherence = totalDoses == 0 ? 0 : Math.Round((double)taken / totalDoses * 100, 2);

        var report = new MedicalReportDto
        {
            PatientId = patient.Id,
            PatientName = patient.User.Name,
            PatientEmail = patient.User.Email,
            DateOfBirth = patient.DateOfBirth,
            EmergencyContact = patient.EmergencyContact,
            ReportGeneratedAt = DateTime.Now,
            OverallAdherence = adherence,
            TotalDosesRecorded = totalDoses,
            TotalTaken = taken,
            TotalMissed = missed,
            ActiveMedicines = medicines.Select(m => new MedicineItemSummaryDto
            {
                Name = m.Name,
                Dosage = m.Dosage,
                Dose = m.Dose,
                Stock = m.Stock,
                ScheduledTimes = m.MedicineSchedules.Select(s => s.Time.ToString(@"hh\:mm")).ToList()
            }).ToList(),
            RecentLogs = logs.Select(l => new LogItemSummaryDto
            {
                MedicineName = !string.IsNullOrEmpty(l.MedicineName) ? l.MedicineName : (l.Medicine != null ? l.Medicine.Name : "Medicine"),
                ScheduledTime = l.ScheduledTime,
                TakenTime = l.TakenTime,
                Status = l.Status
            }).ToList()
        };

        return Ok(report);
    }

    // GET: api/report/patient/{patientId}/print
    // Returns browser printable HTML formatted document (Print to PDF)
    [HttpGet("patient/{patientId}/print")]
    public async Task<IActionResult> GetPrintableReportHtml(int patientId, [FromQuery] int days = 30)
    {
        if (!await CanAccessPatientAsync(patientId))
            return Forbid();

        var patient = await _context.Patients
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == patientId);

        if (patient == null)
            return NotFound("Patient not found.");

        var sinceDate = DateTime.Today.AddDays(-days);

        var medicines = await _context.Medicines
            .Include(m => m.MedicineSchedules)
            .Where(m => m.PatientId == patientId && !m.IsDeleted)
            .ToListAsync();

        var logs = await _context.MedicineLogs
            .Include(l => l.Medicine)
            .Where(l => l.PatientId == patientId && l.ScheduledTime >= sinceDate)
            .OrderByDescending(l => l.ScheduledTime)
            .ToListAsync();

        var totalDoses = logs.Count(l => l.Status == "Taken" || l.Status == "Missed");
        var taken = logs.Count(l => l.Status == "Taken");
        var missed = logs.Count(l => l.Status == "Missed");
        var adherence = totalDoses == 0 ? 0 : Math.Round((double)taken / totalDoses * 100, 2);

        var sb = new StringBuilder();
        sb.Append($@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'/>
    <title>MediCare Compliance Report - {patient.User.Name}</title>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; color: #1e293b; padding: 24px; background: #fff; line-height: 1.5; }}
        .header {{ border-bottom: 2px solid #0284c7; padding-bottom: 12px; margin-bottom: 20px; display: flex; justify-content: space-between; align-items: flex-end; }}
        .title {{ font-size: 24px; font-weight: 700; color: #0f172a; margin: 0; }}
        .meta {{ font-size: 13px; color: #64748b; margin-top: 4px; }}
        .badge {{ background: #e0f2fe; color: #0369a1; padding: 6px 12px; border-radius: 6px; font-weight: 600; font-size: 14px; display: inline-block; }}
        .stats-grid {{ display: grid; grid-template-columns: repeat(4, 1fr); gap: 12px; margin: 20px 0; }}
        .stat-card {{ border: 1px solid #e2e8f0; border-radius: 8px; padding: 12px; background: #f8fafc; text-align: center; }}
        .stat-val {{ font-size: 22px; font-weight: 700; color: #0284c7; }}
        .stat-lbl {{ font-size: 12px; color: #64748b; text-transform: uppercase; margin-top: 4px; }}
        table {{ width: 100%; border-collapse: collapse; margin-top: 14px; font-size: 13px; }}
        th, td {{ border: 1px solid #cbd5e1; padding: 8px 12px; text-align: left; }}
        th {{ background: #f1f5f9; font-weight: 600; }}
        .status-taken {{ color: #16a34a; font-weight: 600; }}
        .status-missed {{ color: #dc2626; font-weight: 600; }}
        .status-pending {{ color: #d97706; font-weight: 600; }}
        .print-btn {{ background: #0284c7; color: #fff; border: none; padding: 8px 16px; border-radius: 6px; cursor: pointer; font-size: 14px; margin-bottom: 16px; }}
        @media print {{
            .no-print {{ display: none; }}
            body {{ padding: 0; }}
        }}
    </style>
</head>
<body>
    <div class='no-print' style='text-align: right;'>
        <button class='print-btn' onclick='window.print()'>Print / Download PDF</button>
    </div>
    <div class='header'>
        <div>
            <h1 class='title'>MediCare Smart Health Report</h1>
            <div class='meta'>Patient: <strong>{patient.User.Name}</strong> ({patient.User.Email}) | Emergency Contact: {patient.EmergencyContact}</div>
        </div>
        <div style='text-align: right;'>
            <div class='badge'>Period: Last {days} Days</div>
            <div class='meta'>Generated: {DateTime.Now:dd MMM yyyy, hh:mm tt}</div>
        </div>
    </div>

    <div class='stats-grid'>
        <div class='stat-card'><div class='stat-val'>{adherence}%</div><div class='stat-lbl'>Adherence Rate</div></div>
        <div class='stat-card'><div class='stat-val'>{totalDoses}</div><div class='stat-lbl'>Total Doses Due</div></div>
        <div class='stat-card'><div class='stat-val' style='color:#16a34a;'>{taken}</div><div class='stat-lbl'>Doses Taken</div></div>
        <div class='stat-card'><div class='stat-val' style='color:#dc2626;'>{missed}</div><div class='stat-lbl'>Doses Missed</div></div>
    </div>

    <h3 style='margin-top: 24px; color: #0f172a;'>Active Prescribed Medications</h3>
    <table>
        <thead>
            <tr><th>Medicine</th><th>Dosage</th><th>Dose Qty</th><th>Remaining Stock</th><th>Daily Scheduled Times</th></tr>
        </thead>
        <tbody>");

        foreach (var m in medicines)
        {
            var times = string.Join(", ", m.MedicineSchedules.Select(s => s.Time.ToString(@"hh\:mm")));
            sb.Append($@"<tr>
                <td><strong>{m.Name}</strong></td>
                <td>{m.Dosage}</td>
                <td>{m.Dose}</td>
                <td>{m.Stock}</td>
                <td>{(string.IsNullOrEmpty(times) ? "None" : times)}</td>
            </tr>");
        }

        sb.Append(@"</tbody></table>

    <h3 style='margin-top: 28px; color: #0f172a;'>Recent Medication Intake Logs</h3>
    <table>
        <thead>
            <tr><th>Scheduled Time</th><th>Medicine</th><th>Recorded Status</th><th>Actual Taken Time</th></tr>
        </thead>
        <tbody>");

        foreach (var l in logs.Take(40))
        {
            var cls = l.Status == "Taken" ? "status-taken" : (l.Status == "Missed" ? "status-missed" : "status-pending");
            var takenStr = l.TakenTime.HasValue ? l.TakenTime.Value.ToString("dd MMM yyyy, hh:mm tt") : "-";
            var medName = !string.IsNullOrEmpty(l.MedicineName) ? l.MedicineName : (l.Medicine != null ? l.Medicine.Name : "Medicine");
            sb.Append($@"<tr>
                <td>{l.ScheduledTime:dd MMM yyyy, hh:mm tt}</td>
                <td><strong>{medName}</strong></td>
                <td class='{cls}'>{l.Status}</td>
                <td>{takenStr}</td>
            </tr>");
        }

        sb.Append(@"</tbody></table>
    <div style='margin-top: 30px; font-size: 11px; color: #94a3b8; text-align: center; border-top: 1px solid #e2e8f0; padding-top: 10px;'>
        This document is an automated electronic adherence dossier generated by MediCare Smart Health System.
    </div>
</body>
</html>");

        return Content(sb.ToString(), "text/html", Encoding.UTF8);
    }
}