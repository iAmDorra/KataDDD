using System;
using System.Collections.Generic;
using System.Linq;

namespace KataDDD
{
    public class FinancingFileManager
    {
        private List<FinancingProject> _files = new();
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
            if(_files.Any(f=> f.HasSameClient(clientId) && f.Status == "montage_en_cours"))
            {
                throw new Exception("Le client a déjà un dossier en montage en cours");
            }

            FinancingProject file = FinancingProject.Create(clientId, fileType, _fileIdCounter++);

            _files.Add(file);
            return file.GetId();
        }

       

        public void AddNeedToFile(int fileId, string needType)
        {
            var file = _files.FirstOrDefault(f => f.IsEqualTo(fileId));
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
            var file = _files.FirstOrDefault(f => f.IsEqualTo(fileId));
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
            var file = _files.FirstOrDefault(f => f.IsEqualTo(fileId));
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
            var file = _files.FirstOrDefault(f => f.IsEqualTo(fileId));
            if (file == null) throw new Exception("Dossier non trouvé");
            file.Submit(responsibleOfficerId);
        }

        public void ApproveFile(int fileId)
        {
            var file = _files.FirstOrDefault(f => f.IsEqualTo(fileId));
            if (file == null) throw new Exception("Dossier non trouvé");
            if (file.Status != "en_validation") throw new Exception("Seul un dossier en validation peut être approuvé");

            file.Status = "accorde";
            file.LastModifiedDate = DateTime.Now;
        }

        public void RejectFile(int fileId, string reason)
        {
            var file = _files.FirstOrDefault(f => f.IsEqualTo(fileId));
            file.Reject(reason);
        }

        public void AbandonFile(int fileId)
        {
            var file = _files.FirstOrDefault(f => f.IsEqualTo(fileId));
            if (file == null) throw new Exception("Dossier non trouvé");

            file.Status = "abandonne";
            file.LastModifiedDate = DateTime.Now;
        }

        public FinancingProject GetFile(int fileId)
        {
            return _files.FirstOrDefault(f => f.IsEqualTo(fileId));
        }

        public List<FinancingProject> GetClientFiles(int clientId)
        {
            return _files.Where(f => f.HasSameClient(clientId)).ToList();
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
            var file = _files.FirstOrDefault(f => f.IsEqualTo(fileId));
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
            var file = _files.FirstOrDefault(f => f.IsEqualTo(fileId));
            return file?.Status ?? "non_trouve";
        }

        public List<Simulation> GetAllSimulationsForNeed(int fileId, int needId)
        {
            var file = _files.FirstOrDefault(f => f.IsEqualTo(fileId));
            if (file == null) return new List<Simulation>();

            var need = file.Needs.FirstOrDefault(n => n.Id == needId);
            return need?.Simulations ?? new List<Simulation>();
        }

        public void ModifySimulation(int fileId, int needId, int simulationId, decimal amount, int duration, 
                                    decimal interestRate, decimal monthlyAmount)
        {
            var file = _files.FirstOrDefault(f => f.IsEqualTo(fileId));
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
            return _files.Count(f => f.HasSameClient( clientId) && f.Status != "abandonne" && f.Status != "accorde" && f.Status != "refuse");
        }

        public string GetFileTypeLabel(int fileId)
        {
            var file = _files.FirstOrDefault(f => f.IsEqualTo(fileId));
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
}
