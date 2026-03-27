namespace KataDDD
{
    public class Simulation
    {
        public int Id { get; set; }
        public int NeedId { get; set; }
        public DateTime CreatedDate { get; set; }
        public decimal InvestmentAmount { get; set; }
        public int Duration { get; set; }
        public decimal InterestRate { get; set; }
        public decimal MonthlyAmount { get; set; }
        public bool MaterialInsurance { get; set; }
        public bool PersonalInsurance { get; set; }
        public List<string> Guarantees { get; set; }
        public List<string> Fees { get; set; }
        public string Status { get; set; }
    }
}
