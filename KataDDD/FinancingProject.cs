namespace KataDDD
{
    public class FinancingProject
    {
        private int Id;
        private int ClientId;
        public string Status { get; set; }
        public string FileType { get; set; }
        private DateTime CreatedDate;
        private DateTime? SubmittedDate;
        public DateTime? LastModifiedDate { get; set; }
        public List<Need> Needs { get; set; }
        private int? ResponsibleOfficer;
        private string RejectionReason;

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
