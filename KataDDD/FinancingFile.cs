namespace KataDDD
{
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
}
