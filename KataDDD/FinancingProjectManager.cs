using System;
using System.Collections.Generic;
using System.Linq;

namespace KataDDD
{
    public class FinancingProjectManager
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
            file.AddNeed(fileId, needType, _needIdCounter++);
        }

       
        public int CreateSimulation(int fileId, int needId, decimal amount, int duration, decimal interestRate, 
                                    decimal monthlyAmount, bool materialInsurance, bool personInsurance,
                                    List<string> guarantees, List<string> fees)
        {
            var file = _files.FirstOrDefault(f => f.IsEqualTo(fileId));
            if (file == null) throw new Exception("Dossier non trouvé");
            return file.CreateSimulation(needId, amount, duration, interestRate, monthlyAmount, materialInsurance, personInsurance, guarantees, fees, _simulationIdCounter++);
        }

        public void ValidateSimulation(int fileId, int needId, int simulationId)
        {
            var file = _files.FirstOrDefault(f => f.IsEqualTo(fileId));
            if (file == null) throw new Exception("Dossier non trouvé");

            file.ValidateSimulation(needId, simulationId);
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
            file.Approve();
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

            file.Abandonner();
        }

        public FinancingProject GetFile(int fileId)
        {
            return _files.FirstOrDefault(f => f.IsEqualTo(fileId));
        }

        public List<FinancingProject> GetClientFiles(int clientId)
        {
            return _files.Where(f => f.HasSameClient(clientId)).ToList();
        }


        public decimal CalculateTotalMonthlyAmount(int fileId)
        {
            var file = _files.FirstOrDefault(f => f.IsEqualTo(fileId));
            if (file == null) return 0;
            return file.CalculateTotalMonthlyAmount();
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
            return file.GetAllNeedSimulation(needId);
        }

        public void ModifySimulation(int fileId, int needId, int simulationId, decimal amount, int duration, 
                                    decimal interestRate, decimal monthlyAmount)
        {
            var file = _files.FirstOrDefault(f => f.IsEqualTo(fileId));
            if (file == null) throw new Exception("Dossier non trouvé");
            file.ModifySimulation(needId, simulationId, amount, duration, interestRate, monthlyAmount);
        }

        public int CountActiveFilesForClient(int clientId)
        {
            return _files.Count(f => f.HasSameClient( clientId) && f.Status != "abandonne" && f.Status != "accorde" && f.Status != "refuse");
        }

        public string GetFileTypeLabel(int fileId)
        {
            var file = _files.FirstOrDefault(f => f.IsEqualTo(fileId));
            if (file == null) return "Inconnu";
            return file.GetFileTypeLabel();
        }

    }
}
