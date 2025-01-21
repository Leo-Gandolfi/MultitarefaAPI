using Microsoft.EntityFrameworkCore;
using MultitarefaAPI.Models;

public class CadastroContext(DbContextOptions<CadastroContext> options) : DbContext(options)
{
    public required DbSet<Cadastro> Cadastros { get; set; }
}

