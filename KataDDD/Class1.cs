using System;
using System.Collections.Generic;
using System.Linq;

namespace KataDDD
{
    public class FinancingFileManager
    {
        private List<FinancingFile> _files = new();
        private Dictionary<int, Client> _clients = new();
        private int _fileIdCounter = 1;
        private int _needIdCounter = 1;
        private int _simulationIdCounter = 1;

        public void CreateClient(int clientId, string name)
        {
            _clients[clientId] = new Client { Id = clientId, Name = name };
        }

        public int CreateFinancingFile(int clientId, string fileType)
        {
            if (_clients.FirstOrDefault(c => c.Value.Id == clientId && c.Value.ActiveFiles.Count > 0 && c.Value.ActiveFiles.Any(f => f.Status == "montage_en_cours")).Value != null)
            {
                throw new Exception("Le client a déjà un dossier en montage en cours");
            }

            var file = new FinancingFile
            {
                Id = _fileIdCounter++,
                ClientId = clientId,
                CreatedDate = DateTime.Now,
                Status = "montage_en_cours",
                FileType = DetermineFileType(fileType),
                Needs = new List<Need>(),
                ResponsibleOfficer = null
            };

            _files.Add(file);
            _clients[clientId].ActiveFiles.Add(file);
            return file.Id;
        }

        public void AddNeedToFile(int fileId, string needType)
        {
            var file = _files.FirstOrDefault(f => f.Id == fileId);
            if (file == null) throw new Exception("Dossier non trouvé");
            if (file.Status != "montage_en_cours") throw new Exception("Le dossier ne peut pas être modifié");

            if (file.Needs.Count >= 4) throw new Exception("Maximum 4 besoins par dossier");

            var mappedFileType = DetermineFileTypeFromNeed(needType);
            if (file.FileType != mappedFileType && file.Needs.Count > 0)
                throw new Exception($"Le type de besoin '{needType}' n'est pas compatible avec ce dossier");

            var need = new Need
            {
                Id = _needIdCounter++,
                Type = needType,
                FileId = fileId,
                Simulations = new List<Simulation>(),
                SelectedSimulationId = null,
                Amount = 0,
                Duration = 0,
                InterestRate = 0
            };

            file.Needs.Add(need);
            file.FileType = mappedFileType;
        }

        public int CreateSimulation(int fileId, int needId, decimal amount, int duration, decimal interestRate, 
                                    decimal monthlyAmount, bool materialInsurance, bool personInsurance,
                                    List<string> guarantees, List<string> fees)
        {
            var file = _files.FirstOrDefault(f => f.Id == fileId);
            if (file == null) throw new Exception("Dossier non trouvé");
            if (file.Status != "montage_en_cours") throw new Exception("Le dossier ne peut pas être modifié");

            var need = file.Needs.FirstOrDefault(n => n.Id == needId);
            if (need == null) throw new Exception("Besoin non trouvé");

            if (need.Simulations.Count >= 3) throw new Exception("Maximum 3 simulations par besoin");

            var simulation = new Simulation
            {
                Id = _simulationIdCounter++,
                NeedId = needId,
                CreatedDate = DateTime.Now,
                InvestmentAmount = amount,
                Duration = duration,
                InterestRate = interestRate,
                MonthlyAmount = monthlyAmount,
                MaterialInsurance = materialInsurance,
                PersonalInsurance = personInsurance,
                Guarantees = guarantees,
                Fees = fees,
                Status = "pending"
            };

            need.Simulations.Add(simulation);
            return simulation.Id;
        }

        public void ValidateSimulation(int fileId, int needId, int simulationId)
        {
            var file = _files.FirstOrDefault(f => f.Id == fileId);
            if (file == null) throw new Exception("Dossier non trouvé");

            var need = file.Needs.FirstOrDefault(n => n.Id == needId);
            if (need == null) throw new Exception("Besoin non trouvé");

            var simulation = need.Simulations.FirstOrDefault(s => s.Id == simulationId);
            if (simulation == null) throw new Exception("Simulation non trouvée");

            // Invalider les autres simulations du même besoin
            foreach (var sim in need.Simulations.Where(s => s.Id != simulationId))
            {
                sim.Status = "rejected";
            }

            simulation.Status = "validated";
            need.SelectedSimulationId = simulationId;
            need.Amount = simulation.InvestmentAmount;
            need.Duration = simulation.Duration;
            need.InterestRate = simulation.InterestRate;

            // Verrouiller le dossier après validation d'une simulation
            file.Status = "verrouille";
            file.LastModifiedDate = DateTime.Now;
        }

        public void SubmitFileForValidation(int fileId, int responsibleOfficerId)
        {
            var file = _files.FirstOrDefault(f => f.Id == fileId);
            if (file == null) throw new Exception("Dossier non trouvé");

            if (file.Needs.Count == 0) throw new Exception("Le dossier doit contenir au moins un besoin");
            if (file.Needs.Any(n => n.SelectedSimulationId == null)) 
                throw new Exception("Tous les besoins doivent avoir une simulation validée");

            file.Status = "en_validation";
            file.ResponsibleOfficer = responsibleOfficerId;
            file.SubmittedDate = DateTime.Now;
        }

        public void ApproveFile(int fileId)
        {
            var file = _files.FirstOrDefault(f => f.Id == fileId);
            if (file == null) throw new Exception("Dossier non trouvé");
            if (file.Status != "en_validation") throw new Exception("Seul un dossier en validation peut être approuvé");

            file.Status = "accorde";
            file.LastModifiedDate = DateTime.Now;
        }

        public void RejectFile(int fileId, string reason)
        {
            var file = _files.FirstOrDefault(f => f.Id == fileId);
            if (file == null) throw new Exception("Dossier non trouvé");
            if (file.Status != "en_validation") throw new Exception("Seul un dossier en validation peut être rejeté");

            file.Status = "refuse";
            file.RejectionReason = reason;
            file.LastModifiedDate = DateTime.Now;
        }

        public void AbandonFile(int fileId)
        {
            var file = _files.FirstOrDefault(f => f.Id == fileId);
            if (file == null) throw new Exception("Dossier non trouvé");

            file.Status = "abandonne";
            file.LastModifiedDate = DateTime.Now;
        }

        public FinancingFile GetFile(int fileId)
        {
            return _files.FirstOrDefault(f => f.Id == fileId);
        }

        public List<FinancingFile> GetClientFiles(int clientId)
        {
            return _files.Where(f => f.ClientId == clientId).ToList();
        }

        private string DetermineFileType(string needType)
        {
            return needType switch
            {
                "tresorerie" => "long_moyen_terme",
                "achat_materiel" => "court_terme",
                "investissement" => "credit_express",
                "location_longue_duree" => "leasing",
                "credit_bail" => "leasing",
                "cession_bail" => "leasing",
                _ => throw new Exception($"Type de besoin invalide: {needType}")
            };
        }

        private string DetermineFileTypeFromNeed(string needType)
        {
            return needType switch
            {
                "tresorerie" => "long_moyen_terme",
                "achat_materiel" => "court_terme",
                "investissement" => "credit_express",
                "location_longue_duree" => "leasing",
                "credit_bail" => "leasing",
                "cession_bail" => "leasing",
                _ => throw new Exception($"Type de besoin invalide: {needType}")
            };
        }

        public decimal CalculateTotalMonthlyAmount(int fileId)
        {
            var file = _files.FirstOrDefault(f => f.Id == fileId);
            if (file == null) return 0;

            decimal total = 0;
            foreach (var need in file.Needs)
            {
                if (need.SelectedSimulationId.HasValue)
                {
                    var selectedSim = need.Simulations.FirstOrDefault(s => s.Id == need.SelectedSimulationId);
                    if (selectedSim != null)
                        total += selectedSim.MonthlyAmount;
                }
            }
            return total;
        }

        public string GetFileStatus(int fileId)
        {
            var file = _files.FirstOrDefault(f => f.Id == fileId);
            return file?.Status ?? "non_trouve";
        }

        public List<Simulation> GetAllSimulationsForNeed(int fileId, int needId)
        {
            var file = _files.FirstOrDefault(f => f.Id == fileId);
            if (file == null) return new List<Simulation>();

            var need = file.Needs.FirstOrDefault(n => n.Id == needId);
            return need?.Simulations ?? new List<Simulation>();
        }

        public void ModifySimulation(int fileId, int needId, int simulationId, decimal amount, int duration, 
                                    decimal interestRate, decimal monthlyAmount)
        {
            var file = _files.FirstOrDefault(f => f.Id == fileId);
            if (file == null) throw new Exception("Dossier non trouvé");
            if (file.Status != "montage_en_cours") throw new Exception("Le dossier ne peut pas être modifié");

            var need = file.Needs.FirstOrDefault(n => n.Id == needId);
            if (need == null) throw new Exception("Besoin non trouvé");

            var simulation = need.Simulations.FirstOrDefault(s => s.Id == simulationId);
            if (simulation == null) throw new Exception("Simulation non trouvée");
            if (simulation.Status != "pending") throw new Exception("Seules les simulations en attente peuvent être modifiées");

            simulation.InvestmentAmount = amount;
            simulation.Duration = duration;
            simulation.InterestRate = interestRate;
            simulation.MonthlyAmount = monthlyAmount;
        }

        public int CountActiveFilesForClient(int clientId)
        {
            return _files.Count(f => f.ClientId == clientId && f.Status != "abandonne" && f.Status != "accorde" && f.Status != "refuse");
        }

        public string GetFileTypeLabel(int fileId)
        {
            var file = _files.FirstOrDefault(f => f.Id == fileId);
            if (file == null) return "Inconnu";

            return file.FileType switch
            {
                "credit_express" => "Crédit Express",
                "credit_court_terme" => "Crédit Court Terme",
                "long_moyen_terme" => "Crédit Long et Moyen Terme",
                "leasing" => "Leasing",
                _ => "Inconnu"
            };
        }
    }

    public class FinancingFile
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public string Status { get; set; }
        public string FileType { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? SubmittedDate { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public List<Need> Needs { get; set; }
        public int? ResponsibleOfficer { get; set; }
        public string RejectionReason { get; set; }
    }

    public class Need
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public int FileId { get; set; }
        public List<Simulation> Simulations { get; set; }
        public int? SelectedSimulationId { get; set; }
        public decimal Amount { get; set; }
        public int Duration { get; set; }
        public decimal InterestRate { get; set; }
    }

    public class Simulation
    {

    }
}
