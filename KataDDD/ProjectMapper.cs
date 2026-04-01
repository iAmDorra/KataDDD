namespace KataDDD
{
    public class ProjectMapper : IVisitor
    {
        private ProjectDTO ProjectDto;

        public ProjectDTO ToDTO(FinancingProject projet)
        {
            projet.Accept(this);
            return this.ProjectDto;
        }

        public void Visit(int id, int clientId)
        {
            this.ProjectDto = new ProjectDTO
            {
                Id = id,
                ClientId = clientId
            };
        }
    }
}
