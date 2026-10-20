using System.Text;
using mediCare.DTOs;
using MediCare.Data;
using MediCare.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Services;

public class AiChatService : IAiChatService
{
    private readonly AppDbContext _context;

    public AiChatService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ChatResponseDto> ProcessQueryAsync(int userId, string role, ChatRequestDto request)
    {
        var rawQuery = request.Message.Trim();
        var query = rawQuery.ToLower();
        int patientId = 0;

        // Target Patient identify karna
        if (role == "PATIENT")
        {
            var p = await _context.Patients.FirstOrDefaultAsync(x => x.UserId == userId);
            if (p != null) patientId = p.Id;
        }
        else if (role == "CAREGIVER")
        {
            if (request.TargetPatientId.HasValue && request.TargetPatientId.Value > 0)
            {
                patientId = request.TargetPatientId.Value;
            }
            else
            {
                var caregiver = await _context.Caregivers.FirstOrDefaultAsync(c => c.UserId == userId);
                if (caregiver != null)
                {
                    patientId = await _context.CaregiverPatients
                        .Where(cp => cp.CaregiverId == caregiver.Id)
                        .Select(cp => cp.PatientId)
                        .FirstOrDefaultAsync();
                }
            }
        }

        var patient = await _context.Patients
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == patientId);

        var patientName = patient?.User.Name ?? "Patient";

        var medicines = await _context.Medicines
            .Include(m => m.MedicineSchedules)
            .Where(m => m.PatientId == patientId)
            .ToListAsync();

        var today = DateTime.Today;
        var todayLogs = await _context.MedicineLogs
            .Include(l => l.Medicine)
            .Where(l => l.PatientId == patientId && l.ScheduledTime.Date == today)
            .ToListAsync();

        var allLogs = await _context.MedicineLogs
            .Where(l => l.PatientId == patientId && (l.Status == "Taken" || l.Status == "Missed"))
            .ToListAsync();

        // =========================================================================
        // 1. EMERGENCY & RED-FLAG SYMPTOMS (Virtual Doctor Triage)
        // =========================================================================
        if (query.Contains("chest pain") || query.Contains("breathing problem") || 
            query.Contains("heart attack") || query.Contains("unconscious") || 
            query.Contains("heavy bleeding") || query.Contains("severe pain"))
        {
            return new ChatResponseDto
            {
                Reply = $"⚠️ **CRITICAL MEDICAL ALERT for {patientName}:**\n\n" +
                        "Aapne jo symptoms bataye hain ye emergency category me aate hain. Kripya online advice par wait na karein:\n" +
                        "1. Turant MediCare Dashboard se **'SOS Emergency Button'** dabayein taaki aapke caregiver ko SMS/Email alert chale jaye.\n" +
                        "2. Nearest hospital ya Ambulance (**108 / 112**) ko turant call karein.\n" +
                        "3. Shaant hokar aisi position me baithein jisme saans lene me aasani ho.",
                QuickSuggestions = new List<string> { "🚨 Trigger SOS Emergency", "Show Emergency Contacts" }
            };
        }

        // =========================================================================
        // 2. COMMON SYMPTOMS & CLINICAL ADVICE (Virtual Doctor Consultation)
        // =========================================================================
        if (query.Contains("fever") || query.Contains("bukhar") || query.Contains("headache") || 
            query.Contains("sar dard") || query.Contains("cold") || query.Contains("cough") || 
            query.Contains("khasi") || query.Contains("vomit") || query.Contains("pet dard") || 
            query.Contains("stomach pain") || query.Contains("acidity") || query.Contains("gas"))
        {
            var prescribedNames = medicines.Select(m => m.Name.ToLower()).ToList();
            var sb = new StringBuilder();

            sb.AppendLine($"👨‍⚕️ **Doctor's Clinical Assessment for {patientName}:**\n");
            
            if (query.Contains("fever") || query.Contains("bukhar"))
            {
                sb.AppendLine("• **Observation:** Bukhar aamtaur par infection ya body ke immune response ki wajah se hota hai.");
                sb.AppendLine("• **Immediate Care:** Khoob paani/fluids piyein aur proper bed rest karein. Maathe par taaze paani ki patti lagayein.");
                sb.AppendLine("• **Dosage Precaution:** Agar doctor ne pehle se Paracetamol prescribe ki hai to prescribed dose lein. Empty stomach na lein.");
            }
            else if (query.Contains("headache") || query.Contains("sar dard"))
            {
                sb.AppendLine("• **Observation:** Sar dard dehydration, neend ki kami, screen time ya stress se ho sakta hai.");
                sb.AppendLine("• **Immediate Care:** 1-2 glass paani piyein, andhere kamre me 20 minute rest karein aur screen se door rahein.");
            }
            else if (query.Contains("acidity") || query.Contains("gas") || query.Contains("pet dard"))
            {
                sb.AppendLine("• **Observation:** Acidity aksar khali pet rehne, teekha khane ya dawaon ke side effects se ho sakti hai.");
                sb.AppendLine("• **Immediate Care:** Gunguna paani piyein. Dawai hamesha doctor ke bataye anusar khane ke baad lein.");
            }

            // Cross check active medicines
            if (medicines.Any())
            {
                sb.AppendLine($"\n📋 **Active Prescriptions in Profile:** {string.Join(", ", medicines.Select(m => m.Name))}");
                sb.AppendLine("⚠️ *Dhyan rahe koi bhi nayi dawai shuru karne se pehle active medicines ke reaction ka dhyan rakhein.*");
            }

            sb.AppendLine("\n*Notice: Agar symptoms 24 ghante se zyada continue rahein ya badhein, to turant physical doctor se checkup karwayein.*");

            return new ChatResponseDto
            {
                Reply = sb.ToString(),
                QuickSuggestions = new List<string> { "Show today's medicines", "Check medicine timing", "Diet guidance" }
            };
        }

        // =========================================================================
        // 3. MEAL & TIMING ADVICE (Virtual Doctor Guidance)
        // =========================================================================
        if (query.Contains("khana") || query.Contains("meal") || query.Contains("food") || 
            query.Contains("timing") || query.Contains("dudh") || query.Contains("milk") || query.Contains("pani"))
        {
            return new ChatResponseDto
            {
                Reply = $"🥗 **Doctor's Medication & Meal Protocol for {patientName}:**\n\n" +
                        "1. **Before Meal (Khali Pet):** Thyroid, Acidity (Pantoprazole/Omeprazole) dawaiyan subah khane se 30 minute pehle taaze paani se li jaati hain.\n" +
                        "2. **After Meal (Khane ke baad):** Painkillers, Antibiotics aur Multivitamins hamesha khane ke 15-20 minute baad lein taaki acidity ya pet me jalan na ho.\n" +
                        "3. **Hydration Rule:** Dawai hamesha 1 poore glass taaze paani ke saath lein. Chai, coffee ya cold drink ke sath dawai lene se bachein.",
                QuickSuggestions = new List<string> { "Show today's medicines", "Check adherence score" }
            };
        }

        // =========================================================================
        // 4. ADHERENCE & COMPLIANCE (Clinical Record Review)
        // =========================================================================
        if (query.Contains("adherence") || query.Contains("compliance") || query.Contains("score") || query.Contains("report"))
        {
            var total = allLogs.Count;
            var taken = allLogs.Count(l => l.Status == "Taken");
            var rate = total == 0 ? 0 : Math.Round((double)taken / total * 100, 1);

            string docVerdict = rate >= 80 
                ? "🌟 **Excellent Compliance!** Aapki medication recovery rate bohot acchi chal rahi hai."
                : "⚠️ **Compliance Needs Improvement:** Dose miss karne se dawai ka therapeutic effect kam ho sakta hai. Routine maintain karein.";

            return new ChatResponseDto
            {
                Reply = $"📊 **Doctor's Compliance Audit for {patientName}:**\n\n" +
                        $"- **Overall Adherence:** {rate}%\n" +
                        $"- **Total Prescribed Intakes:** {total}\n" +
                        $"- **Doses Taken On Time:** {taken}\n\n" +
                        $"{docVerdict}",
                QuickSuggestions = new List<string> { "Show today's schedule", "Missed doses today", "Print medical report" }
            };
        }

        // =========================================================================
        // 5. TODAY'S SCHEDULE (Prescription Tracker)
        // =========================================================================
        if (query.Contains("today") || query.Contains("schedule") || query.Contains("aaj") || query.Contains("routine") || query.Contains("dawa"))
        {
            var sb = new StringBuilder();
            sb.AppendLine($"🩺 **Today's Clinical Medication Chart ({today:dd MMM yyyy}) for {patientName}:**\n");

            if (!medicines.Any())
            {
                sb.AppendLine("Currently no active prescribed medications are enrolled.");
            }
            else
            {
                foreach (var med in medicines)
                {
                    var times = string.Join(", ", med.MedicineSchedules.Select(s => s.Time.ToString(@"hh\:mm")));
                    var log = todayLogs.FirstOrDefault(l => l.MedicineId == med.Id);
                    var status = log != null ? log.Status : "Pending";
                    var icon = status == "Taken" ? "✅" : (status == "Missed" ? "❌" : "⏳");
                    sb.AppendLine($"• {icon} **{med.Name}** | Dosage: {med.Dosage} | Time: [{times}] | Status: *{status}*");
                }
            }

            sb.AppendLine("\n💡 *Doctor's Tip: Kisi bhi dose ko skip na karein. Agar late ho jayein to turant dose lein par double dose na karein.*");

            return new ChatResponseDto
            {
                Reply = sb.ToString(),
                QuickSuggestions = new List<string> { "How to take after meal?", "Low stock alert", "Check adherence" }
            };
        }

        // =========================================================================
        // 6. INVENTORY / REFILL CHECK
        // =========================================================================
        if (query.Contains("stock") || query.Contains("refill") || query.Contains("khatam") || query.Contains("kam"))
        {
            var lowStock = medicines.Where(m => m.Stock <= 5).ToList();
            if (!lowStock.Any())
            {
                return new ChatResponseDto
                {
                    Reply = $"💊 **Pharmacy Inventory Status:**\n\nAll medicines for **{patientName}** have sufficient supply (> 5 doses remaining). Treatment continuity safe hai.",
                    QuickSuggestions = new List<string> { "Show today's medicines", "Adherence score" }
                };
            }

            var sb = new StringBuilder();
            sb.AppendLine($"⚠️ **Doctor's Refill Warning for {patientName}:**\n");
            sb.AppendLine("Treatment break na ho isliye ye medicines turant refill karwayein:");
            foreach (var m in lowStock)
            {
                sb.AppendLine($"• **{m.Name}**: Sirf **{m.Stock}** doses bachi hain.");
            }
            sb.AppendLine("\nKripya apne local chemist ya caregiver se nayi strip arrange karein.");

            return new ChatResponseDto
            {
                Reply = sb.ToString(),
                QuickSuggestions = new List<string> { "Print refill sheet", "Check today's medicines" }
            };
        }

        // =========================================================================
        // 7. DEFAULT VIRTUAL DOCTOR GREETING & GUIDELINES
        // =========================================================================
        return new ChatResponseDto
        {
            Reply = $"Namaste! Main **Dr. MediCare**, aapka 24/7 Virtual Health Companion hoon.\n\n" +
                    $"Aap mujhse **{patientName}** ke baare me pooch sakte hain:\n" +
                    "• **Symptoms Guidance:** Bukhar, sar dard, acidity ya kisi takleef ke gharelu aur safe upaye\n" +
                    "• **Schedule & Doses:** Aaj ki dawaiyon ka time aur routine\n" +
                    "• **Medicine Guidelines:** Khane se pehle ya baad me lene ke niyam\n" +
                    "• **Adherence & Health Score:** Compliance audit aur reports\n" +
                    "• **Refill Status:** Low stock dawaiyon ki warning\n\n" +
                    "Aap abhi kaisa mehsoos kar rahe hain?",
            QuickSuggestions = new List<string>
            {
                "Aaj ki dawaiyan dikhao",
                "Mera adherence score kaisa hai?",
                "Sar dard ho raha hai kya karu?",
                "Kaunsi medicine khatam hone wali hai?"
            }
        };
    }
}