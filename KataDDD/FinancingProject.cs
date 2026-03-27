namespace KataDDD
{
    public class FinancingProject
    {
        private int Id;
        private int ClientId;
        public required string Status { get; set; }
        private string FileType;
        private DateTime CreatedDate;
        private DateTime? SubmittedDate;
        private DateTime? LastModifiedDate;
        private List<Need> Needs;
        private int? ResponsibleOfficer;
        private string? RejectionReason;

        public bool IsEqualTo(int fileId)
        {
            return this.Id == fileId;
        }

        public int GetId() { return this.Id; }

        public static FinancingProject Create(int clientId, string fileType, int idcounter)
        {

            return new FinancingProject
            {
                Id = idcounter,
                ClientId = clientId,
                CreatedDate = DateTime.Now,
                Status = "montage_en_cours",
                FileType = DetermineFileType(fileType),
                Needs = new List<Need>(),
                ResponsibleOfficer = null
            };
        }

        public void ModifySimulation(int needId, int simulationId, decimal amount, int duration, decimal interestRate, decimal monthlyAmount)
        {
            if (this.Status != "montage_en_cours") throw new Exception("Le dossier ne peut pas être modifié");

            var need = this.Needs.FirstOrDefault(n => n.Id == needId);
            if (need == null) throw new Exception("Besoin non trouvé");

            var simulation = need.Simulations.FirstOrDefault(s => s.Id == simulationId);
            if (simulation == null) throw new Exception("Simulation non trouvée");
            if (simulation.Status != "pending") throw new Exception("Seules les simulations en attente peuvent être modifiées");

            simulation.InvestmentAmount = amount;
            simulation.Duration = duration;
            simulation.InterestRate = interestRate;
            simulation.MonthlyAmount = monthlyAmount;
        }

        public List<Simulation> GetAllNeedSimulation(int needId)
        {
            var need = this.Needs.FirstOrDefault(n => n.Id == needId);
            return need?.Simulations ?? [];
        }

        public decimal CalculateTotalMonthlyAmount()
        {
            decimal total = 0;
            foreach (var need in this.Needs)
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

        public int CreateSimulation(int needId, decimal amount, int duration, decimal interestRate, decimal monthlyAmount, bool materialInsurance, bool personInsurance, List<string> guarantees, List<string> fees, int simulationId)
        {
            if (this.Status != "montage_en_cours") throw new Exception("Le dossier ne peut pas être modifié");

            var need = this.Needs.FirstOrDefault(n => n.Id == needId);
            if (need == null) throw new Exception("Besoin non trouvé");

            if (need.Simulations.Count >= 3) throw new Exception("Maximum 3 simulations par besoin");

            var simulation = new Simulation
            {
                Id = simulationId,
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

        public string GetFileTypeLabel()
        {
            return this.FileType switch
            {
                "credit_express" => "Crédit Express",
                "credit_court_terme" => "Crédit Court Terme",
                "long_moyen_terme" => "Crédit Long et Moyen Terme",
                "leasing" => "Leasing",
                _ => "Inconnu"
            };
        }

        public void AddNeed(int fileId, string needType, int needId)
        {
            if (this.Status != "montage_en_cours") throw new Exception("Le dossier ne peut pas être modifié");

            if (this.Needs.Count >= 4) throw new Exception("Maximum 4 besoins par dossier");

            var mappedFileType = DetermineFileTypeFromNeed(needType);
            if (this.FileType != mappedFileType && this.Needs.Count > 0)
                throw new Exception($"Le type de besoin '{needType}' n'est pas compatible avec ce dossier");

            var need = new Need
            {
                Id = needId,
                Type = needType,
                FileId = fileId,
                Simulations = new List<Simulation>(),
                SelectedSimulationId = null,
                Amount = 0,
                Duration = 0,
                InterestRate = 0
            };

            this.Needs.Add(need);
            this.FileType = mappedFileType;
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

        public void Abandonner()
        {
            this.Status = "abandonne";
            this.LastModifiedDate = DateTime.Now;
        }

        public void Approve()
        {
            if (this.Status != "locked") throw new Exception("Seul un dossier en validation peut être approuvé");

            this.Status = "accorde";
            this.LastModifiedDate = DateTime.Now;
        }

        public void ValidateSimulation(int needId, int simulationId)
        {
            var need = this.Needs.FirstOrDefault(n => n.Id == needId);
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
            this.Status = "verrouille";
            this.LastModifiedDate = DateTime.Now;
        }


        public bool HasSameClient(int clientId)
        {
            return this.ClientId == clientId;
        }


        private static string DetermineFileType(string needType)
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

        public bool IsRejectedWith(string expectedRejectionReason)
        {
            return IsRejected() && this.RejectionReason == expectedRejectionReason;
        }

        private bool IsRejected()
        {
            return this.Status == "refuse";
        }

        public void Reject(string reason)
        {
            if (this.Status != "locked") throw new Exception("Seul un dossier en validation peut être rejeté");

            this.Status = "refuse";
            this.RejectionReason = reason;
            this.LastModifiedDate = DateTime.Now;
        }

        public void Submit(int responsibleOfficerId)
        {
            if (this.Needs.Count == 0) throw new Exception("Le dossier doit contenir au moins un besoin");
            if (this.Needs.Any(n => n.SelectedSimulationId == null))
                throw new Exception("Tous les besoins doivent avoir une simulation validée");

            this.Status = "locked";
            this.ResponsibleOfficer = responsibleOfficerId;
            this.SubmittedDate = DateTime.Now;
        }

        public bool IsLockedBy(int responsibleOfficer)
        {
            return this.Status == "locked" &&
                this.ResponsibleOfficer == responsibleOfficer;
        }
    }
}
