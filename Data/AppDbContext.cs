using Microsoft.EntityFrameworkCore;
using MultitarefaAPI.Models;

namespace MultitarefaAPI.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public required DbSet<Cadastro> Tarefas { get; set; }
    }
}
