namespace KataDDD
{
    public class FinancingFile
    {
        private int Id;
        public int ClientId { get; set; }
        public string Status { get; set; }
        public string FileType { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? SubmittedDate { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public List<Need> Needs { get; set; }
        public int? ResponsibleOfficer { get; set; }
        public string RejectionReason { get; set; }

        public bool IsEqualTo(int fileId)
        {
            return this.Id == fileId;
        }

        public int GetId() { return this.Id; }

        public static FinancingFile Create(int clientId, string fileType, int idcounter)
        {
            return new FinancingFile
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
    }
}
