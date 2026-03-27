namespace KataDDD
{
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
}
