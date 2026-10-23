USE MediCareDbdotnet;
DELETE FROM Medicines WHERE Name LIKE '%Vitamin C%';
SELECT Id, Name, Dosage, Stock, Dose, MealTimeing, PatientId FROM Medicines;
