namespace KataDDD
{
    public class Client
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<FinancingFile> ActiveFiles { get; set; } = new();
    }
}
