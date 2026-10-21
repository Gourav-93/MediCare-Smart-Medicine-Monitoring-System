using System.Text;
using System.Text.Json;
using mediCare.DTOs;
using MediCare.Data;
using MediCare.Models;
using MediCare.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace MediCare.Services;

public class AiChatService : IAiChatService
{
    private readonly AppDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _geminiApiKey;

    public AiChatService(
        AppDbContext context,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _geminiApiKey = configuration["Gemini:ApiKey"] ?? string.Empty;
    }

    public async Task<ChatResponseDto> ProcessQueryAsync(int userId, string role, ChatRequestDto request)
    {
        var rawQuery = request.Message?.Trim() ?? string.Empty;
        int patientId = 0;

        // 1. Identify Target Patient
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

        var patientName = patient?.User?.Name ?? "Patient";

        // 2. Fetch Live Medical Context from Database
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

        var totalDoses = allLogs.Count;
        var takenDoses = allLogs.Count(l => l.Status == "Taken");
        var adherence = totalDoses == 0 ? 0 : Math.Round((double)takenDoses / totalDoses * 100, 1);

        var medListStr = string.Join("; ", medicines.Select(m => $"{m.Name} ({m.Dosage}, Stock: {m.Stock})"));
        var todayLogStr = string.Join("; ", todayLogs.Select(l => $"{l.Medicine?.Name ?? "Medicine"}: {l.Status} at {l.ScheduledTime:hh:mm tt}"));

        // 3. Build Clinical System Prompt for Gemini
        var systemInstruction = $@"You are Dr. MediCare, an empathetic, certified clinical medical AI assistant for MediCare smart medicine monitoring system.
Patient Profile:
- Name: {patientName}
- Prescribed Medicines: {(string.IsNullOrEmpty(medListStr) ? "None currently prescribed" : medListStr)}
- Today's Medication Logs: {(string.IsNullOrEmpty(todayLogStr) ? "No logs recorded today" : todayLogStr)}
- Overall Medication Adherence: {adherence}%

Safety Guidelines:
1. Speak in friendly, polite Hinglish/Hindi or English based on how the user asks.
2. If patient mentions severe emergency symptoms (chest pain, breathing trouble, severe unconsciousness, blood loss, sudden collapse), strictly warn them to call emergency ambulance (108/112) immediately and trigger SOS in the app.
3. For normal symptoms (headache, fever, gas, acidity, cold), give safe first-aid care, correct meal timings (before/after food), and cross-check against their active medicines.
4. Keep the answer concise, actionable, and formatted with clean bullet points.
5. If user asks about their schedule, dose, stock, or adherence, use their profile data provided above to answer accurately.";

        // 4. Call Gemini REST API (with automatic fallback to clinical rules if API fails)
        var replyText = await CallGeminiApiAsync(systemInstruction, rawQuery);

        if (string.IsNullOrWhiteSpace(replyText))
        {
            replyText = GenerateClinicalFallback(rawQuery, patientName, medicines, todayLogs, allLogs, adherence);
        }

        return new ChatResponseDto
        {
            Reply = replyText,
            Timestamp = DateTime.Now,
            QuickSuggestions = GetQuickSuggestions(rawQuery)
        };
    }

    private async Task<string> CallGeminiApiAsync(string systemPrompt, string userPrompt)
    {
        if (string.IsNullOrWhiteSpace(_geminiApiKey))
        {
            return string.Empty;
        }

        // Try gemini-3.5-flash-lite (fast & stable), fallback to gemini-3.8-flash
        var modelsToTry = new[] { "gemini-3.5-flash-lite", "gemini-3.8-flash" };

        foreach (var model in modelsToTry)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(25);
                var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={_geminiApiKey}";

                var requestBody = new
                {
                    system_instruction = new
                    {
                        parts = new[]
                        {
                            new { text = systemPrompt }
                        }
                    },
                    contents = new[]
                    {
                        new
                        {
                            role = "user",
                            parts = new[]
                            {
                                new { text = userPrompt }
                            }
                        }
                    }
                };

                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await client.PostAsync(endpoint, jsonContent);
                if (!response.IsSuccessStatusCode)
                {
                    continue;
                }

                var responseString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseString);

                if (doc.RootElement.TryGetProperty("candidates", out var candidates) &&
                    candidates.GetArrayLength() > 0)
                {
                    var firstCandidate = candidates[0];
                    if (firstCandidate.TryGetProperty("content", out var content) &&
                        content.TryGetProperty("parts", out var parts) &&
                        parts.GetArrayLength() > 0 &&
                        parts[0].TryGetProperty("text", out var textEl))
                    {
                        var text = textEl.GetString();
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            return text;
                        }
                    }
                }
            }
            catch
            {
                // Continue to next model or fallback
            }
        }

        return string.Empty;
    }

    private string GenerateClinicalFallback(
        string rawQuery,
        string patientName,
        List<Medicine> medicines,
        List<MedicineLog> todayLogs,
        List<MedicineLog> allLogs,
        double adherence)
    {
        var q = rawQuery.ToLower();

        // 1. Emergency
        if (q.Contains("emergency") || q.Contains("chest pain") || q.Contains("heart attack") || q.Contains("breath") || q.Contains("saans"))
        {
            return "🚨 **EMERGENCY MEDICAL WARNING:**\n\n" +
                   "Aapke dwara bataye gaye lakshan gambhir ho sakte hain. Kripya bina samay gawaye:\n" +
                   "1. Turant **Ambulance (108 / 112)** ko call karein.\n" +
                   "2. MediCare dashboard par **SOS Emergency Alert** button dabayein taaki aapke caregivers ko alert pahunch jaye.\n" +
                   "3. Shanti se let jayein aur akele na rahein.";
        }

        // 2. Schedule / Today
        if (q.Contains("today") || q.Contains("schedule") || q.Contains("aaj") || q.Contains("routine") || q.Contains("dawa"))
        {
            var sb = new StringBuilder();
            sb.AppendLine($"🩺 **{patientName} ka Aaj Ka Medication Schedule:**\n");

            if (!medicines.Any())
            {
                sb.AppendLine("Aapke profile me abhi koi active dawa add nahi hai.");
            }
            else
            {
                foreach (var med in medicines)
                {
                    var times = string.Join(", ", med.MedicineSchedules.Select(s => s.Time.ToString(@"hh\:mm")));
                    var log = todayLogs.FirstOrDefault(l => l.MedicineId == med.Id);
                    var status = log != null ? log.Status : "Pending";
                    var icon = status == "Taken" ? "✅" : (status == "Missed" ? "❌" : "⏳");
                    sb.AppendLine($"• {icon} **{med.Name}** ({med.Dosage}) | Samay: [{times}] | Status: *{status}*");
                }
            }
            sb.AppendLine("\n💡 *Doctor's Tip: Kisi bhi dose ko skip na karein. Sahi samay par dawa lein.*");
            return sb.ToString();
        }

        // 3. Adherence / Compliance
        if (q.Contains("adherence") || q.Contains("score") || q.Contains("compliance") || q.Contains("report"))
        {
            var total = allLogs.Count;
            var taken = allLogs.Count(l => l.Status == "Taken");
            var verdict = adherence >= 80
                ? "🌟 **Shaandar Compliance!** Aap regular dawaiyan le rahe hain."
                : "⚠️ **Dhyan Dein:** Doses miss hone se health recovery slow ho sakti hai. Routine follow karein.";

            return $"📊 **Doctor's Adherence Audit for {patientName}:**\n\n" +
                   $"- **Overall Adherence Rate:** {adherence}%\n" +
                   $"- **Total Scheduled Doses:** {total}\n" +
                   $"- **Taken On Time:** {taken}\n\n" +
                   $"{verdict}";
        }

        // 4. Stock / Refill
        if (q.Contains("stock") || q.Contains("refill") || q.Contains("khatam") || q.Contains("kam"))
        {
            var lowStock = medicines.Where(m => m.Stock <= 5).ToList();
            if (!lowStock.Any())
            {
                return $"💊 **Pharmacy Inventory Status:**\n\n**{patientName}** ki sabhi dawaiyon ka sufficient stock uplabdh hai (> 5 doses). Refill ki abhi aavashyakta nahi hai.";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"⚠️ **Doctor's Refill Warning for {patientName}:**\n");
            sb.AppendLine("Dawa ruk na jaye isliye in dawaiyon ko turant refill karwayein:");
            foreach (var m in lowStock)
            {
                sb.AppendLine($"• **{m.Name}**: Sirf **{m.Stock}** doses bachi hain.");
            }
            sb.AppendLine("\nKripya local chemist ya caregiver se nayi strip arrange karein.");
            return sb.ToString();
        }

        // 5. Meal & Timings
        if (q.Contains("khana") || q.Contains("meal") || q.Contains("food") || q.Contains("timing") || q.Contains("pehle") || q.Contains("baad"))
        {
            return $"🥗 **Dawai Aur Khane Ka Sahi Niyam:**\n\n" +
                   "1. **Khane Se Pehle (Khali Pet):** Acidity (Pantoprazole, Omeprazole) ya Thyroid ki dawai subah khane se 30 minute pehle taaze paani ke sath lein.\n" +
                   "2. **Khane Ke Baad:** Painkillers, Antibiotics aur Multivitamins hamesha khana khane ke 15 minute baad lein taaki pet me jalan na ho.\n" +
                   "3. **Paani:** Dawai hamesha 1 poora glass saaf paani ke sath lein. Chai ya cold drink ke sath na lein.";
        }

        // Default Virtual Doctor
        return $"Namaste! Main **Dr. MediCare**, aapka 24/7 Virtual AI Health Assistant hoon.\n\n" +
               $"Main **{patientName}** ke bare me aapki madad kar sakta hoon:\n" +
               "• **Aaj ka schedule:** 'Aaj ki dawaiyan dikhao'\n" +
               "• **Health score:** 'Mera adherence score kaisa hai?'\n" +
               "• **Stock check:** 'Konsi medicine khatam hone wali hai?'\n" +
               "• **Symptoms guidance:** Sar dard, bukhar, pet dard ke safe gharelu upaye\n\n" +
               "Aap abhi kaisa mehsoos kar rahe hain?";
    }

    private List<string> GetQuickSuggestions(string query)
    {
        return new List<string>
        {
            "Aaj ki schedule dikhao",
            "Mera adherence score kaisa hai?",
            "Refill ki jarurat hai kya?",
            "Khane ke baad konsi dawa leni hai?"
        };
    }
}