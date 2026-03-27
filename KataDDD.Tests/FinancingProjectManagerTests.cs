using KataDDD;
using Xunit;

namespace KataDDD.Tests
{
    public class FinancingProjectManagerTests
    {
        private FinancingProjectManager _manager;

        public FinancingProjectManagerTests()
        {
            _manager = new FinancingProjectManager();
        }

        [Fact]
        public void CreateClient_ShouldAddClientSuccessfully()
        {
            _manager.CreateClient(1, "Client A");
            var files = _manager.GetClientFiles(1);
            Assert.Empty(files);
        }

        [Fact]
        public void CreateFinancingFile_ShouldCreateFileWithMontageEnCoursStatus()
        {
            _manager.CreateClient(1, "Client A");
            int fileId = _manager.CreateFinancingFile(1, "tresorerie");

            var file = _manager.GetFile(fileId);
            Assert.NotNull(file);
            Assert.Equal("montage_en_cours", file.Status);
            Assert.Equal("long_moyen_terme", file.FileType);
        }

        [Fact]
        public void CreateFinancingFile_ShouldThrowWhenClientHasActiveFile()
        {
            _manager.CreateClient(1, "Client A");
            _manager.CreateFinancingFile(1, "tresorerie");

            void Action() => _manager.CreateFinancingFile(1, "investissement");
            var ex = Assert.Throws<Exception>((Action)Action);
            Assert.NotNull(ex);
        }

        [Fact]
        public void AddNeedToFile_ShouldAddNeedSuccessfully()
        {
            _manager.CreateClient(1, "Client A");
            int fileId = _manager.CreateFinancingFile(1, "tresorerie");

            _manager.AddNeedToFile(fileId, "tresorerie");
            var file = _manager.GetFile(fileId);

            Assert.Single(file.Needs);
            Assert.Equal("tresorerie", file.Needs[0].Type);
        }

        [Fact]
        public void AddNeedToFile_ShouldThrowWhenFileIsLocked()
        {
            _manager.CreateClient(1, "Client A");
            int fileId = _manager.CreateFinancingFile(1, "investissement");

            _manager.AddNeedToFile(fileId, "investissement");
            int simId = _manager.CreateSimulation(fileId, 1, 50000, 60, 5.5m, 900, true, true, 
                                                   new List<string> { "hypotheque" }, 
                                                   new List<string> { "frais_dossier" });
            _manager.ValidateSimulation(fileId, 1, simId);

            void Action() => _manager.AddNeedToFile(fileId, "achat_materiel");
            var ex = Assert.Throws<Exception>((Action)Action);
            Assert.NotNull(ex);
        }

        [Fact]
        public void CreateSimulation_ShouldCreateSimulationSuccessfully()
        {
            _manager.CreateClient(1, "Client A");
            int fileId = _manager.CreateFinancingFile(1, "investissement");
            _manager.AddNeedToFile(fileId, "investissement");

            int simId = _manager.CreateSimulation(fileId, 1, 100000, 84, 4.8m, 1250, true, false,
                                                   new List<string> { "hypotheque" },
                                                   new List<string> { "frais_dossier", "frais_assurance" });

            Assert.NotEqual(0, simId);
            var simulations = _manager.GetAllSimulationsForNeed(fileId, 1);
            Assert.Single(simulations);
        }

        [Fact]
        public void CreateSimulation_ShouldAllowMaxThreeSimulationsPerNeed()
        {
            _manager.CreateClient(1, "Client A");
            int fileId = _manager.CreateFinancingFile(1, "achat_materiel");
            _manager.AddNeedToFile(fileId, "achat_materiel");

            _manager.CreateSimulation(fileId, 1, 50000, 36, 5.5m, 1400, false, true, new List<string>(), new List<string>());
            _manager.CreateSimulation(fileId, 1, 50000, 48, 5.2m, 1100, false, true, new List<string>(), new List<string>());
            _manager.CreateSimulation(fileId, 1, 50000, 60, 4.9m, 900, false, true, new List<string>(), new List<string>());

            var simulations = _manager.GetAllSimulationsForNeed(fileId, 1);
            Assert.Equal(3, simulations.Count);
        }

        [Fact]
        public void CreateSimulation_ShouldThrowWhenExceedingMaxThreeSimulations()
        {
            _manager.CreateClient(1, "Client A");
            int fileId = _manager.CreateFinancingFile(1, "achat_materiel");
            _manager.AddNeedToFile(fileId, "achat_materiel");

            _manager.CreateSimulation(fileId, 1, 50000, 36, 5.5m, 1400, false, true, new List<string>(), new List<string>());
            _manager.CreateSimulation(fileId, 1, 50000, 48, 5.2m, 1100, false, true, new List<string>(), new List<string>());
            _manager.CreateSimulation(fileId, 1, 50000, 60, 4.9m, 900, false, true, new List<string>(), new List<string>());

            void Action() => _manager.CreateSimulation(fileId, 1, 50000, 72, 4.8m, 800, false, true, new List<string>(), new List<string>());
            var ex = Assert.Throws<Exception>((Action)Action);
            Assert.NotNull(ex);
        }

        [Fact]
        public void ValidateSimulation_ShouldLockFileAndMarkOthersAsRejected()
        {
            _manager.CreateClient(1, "Client A");
            int fileId = _manager.CreateFinancingFile(1, "location_longue_duree");
            _manager.AddNeedToFile(fileId, "location_longue_duree");

            int sim1 = _manager.CreateSimulation(fileId, 1, 75000, 36, 6.0m, 2100, true, true, new List<string> { "garantie_bailloriste" }, new List<string>());
            int sim2 = _manager.CreateSimulation(fileId, 1, 75000, 48, 5.7m, 1600, true, true, new List<string> { "garantie_bailloriste" }, new List<string>());

            _manager.ValidateSimulation(fileId, 1, sim1);

            var file = _manager.GetFile(fileId);
            Assert.Equal("verrouille", file.Status);

            var simulations = _manager.GetAllSimulationsForNeed(fileId, 1);
            Assert.Equal("validated", simulations.First(s => s.Id == sim1).Status);
            Assert.Equal("rejected", simulations.First(s => s.Id == sim2).Status);
        }

        [Fact]
        public void CalculateTotalMonthlyAmount_ShouldReturnCorrectTotal()
        {
            _manager.CreateClient(1, "Client A");
            int fileId = _manager.CreateFinancingFile(1, "credit_bail");
            _manager.AddNeedToFile(fileId, "credit_bail");

            int simId = _manager.CreateSimulation(fileId, 1, 50000, 60, 5.5m, 950, true, false,
                                                   new List<string> { "garantie_bailloriste" }, new List<string>());
            _manager.ValidateSimulation(fileId, 1, simId);

            decimal total = _manager.CalculateTotalMonthlyAmount(fileId);
            Assert.Equal(950, total);
        }

        [Fact]
        public void SubmitFileForValidation_ShouldChangeStatusToEnValidation()
        {
            _manager.CreateClient(1, "Client A");
            int fileId = _manager.CreateFinancingFile(1, "investissement");
            _manager.AddNeedToFile(fileId, "investissement");

            int simId = _manager.CreateSimulation(fileId, 1, 100000, 84, 4.5m, 1250, true, true,
                                                   new List<string> { "hypotheque" }, new List<string> { "frais_dossier" });
            _manager.ValidateSimulation(fileId, 1, simId);

            _manager.SubmitFileForValidation(fileId, 42);
            var file = _manager.GetFile(fileId);

            Assert.Equal("en_validation", file.Status);
            Assert.Equal(42, file.ResponsibleOfficer);
        }

        [Fact]
        public void ApproveFile_ShouldChangeStatusToAccorde()
        {
            _manager.CreateClient(1, "Client A");
            int fileId = _manager.CreateFinancingFile(1, "investissement");
            _manager.AddNeedToFile(fileId, "investissement");

            int simId = _manager.CreateSimulation(fileId, 1, 50000, 36, 5.0m, 1450, false, false,
                                                   new List<string>(), new List<string> { "frais_dossier" });
            _manager.ValidateSimulation(fileId, 1, simId);
            _manager.SubmitFileForValidation(fileId, 10);

            _manager.ApproveFile(fileId);
            var file = _manager.GetFile(fileId);

            Assert.Equal("accorde", file.Status);
        }

        [Fact]
        public void RejectFile_ShouldChangeStatusToRefuseWithReason()
        {
            const int ClientId = 1;
            _manager.CreateClient(ClientId, "Client A");
            const string FileType = "tresorerie";
            int fileId = _manager.CreateFinancingFile(1, FileType);
            _manager.AddNeedToFile(fileId, "tresorerie");

            int simId = _manager.CreateSimulation(fileId, 1, 150000, 120, 6.5m, 1500, false, true,
                                                   new List<string> { "hypotheque", "pledge_mobilier" },
                                                   new List<string> { "frais_dossier", "frais_expertise" });
            _manager.ValidateSimulation(fileId, 1, simId);
            _manager.SubmitFileForValidation(fileId, 25);

            _manager.RejectFile(fileId, "Apport personnel insuffisant");
            var file = _manager.GetFile(fileId);

            const string ExpectedRejectionReason = "Apport personnel insuffisant";
           Assert.True(file.IsRejectedWith(ExpectedRejectionReason));
        }

        [Fact]
        public void AbandonFile_ShouldChangeStatusToAbandonne()
        {
            _manager.CreateClient(1, "Client A");
            int fileId = _manager.CreateFinancingFile(1, "cession_bail");
            _manager.AddNeedToFile(fileId, "cession_bail");

            _manager.AbandonFile(fileId);
            var file = _manager.GetFile(fileId);

            Assert.Equal("abandonne", file.Status);
        }

        [Fact]
        public void GetFileTypeLabel_ShouldReturnCorrectLabel()
        {
            _manager.CreateClient(1, "Client A");

            int file1 = _manager.CreateFinancingFile(1, "investissement");
            Assert.Equal("Crédit Express", _manager.GetFileTypeLabel(file1));
        }

        [Fact]
        public void AddNeedToFile_ShouldAllowMaxFourNeeds()
        {
            _manager.CreateClient(1, "Client A");
            int fileId = _manager.CreateFinancingFile(1, "credit_bail");

            _manager.AddNeedToFile(fileId, "credit_bail");
            _manager.AddNeedToFile(fileId, "cession_bail");
            _manager.AddNeedToFile(fileId, "location_longue_duree");
            _manager.AddNeedToFile(fileId, "credit_bail");

            var file = _manager.GetFile(fileId);
            Assert.Equal(4, file.Needs.Count);
        }
    }
}