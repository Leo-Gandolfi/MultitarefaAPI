using System.ComponentModel.DataAnnotations;

namespace MultitarefaAPI.Models
{
    public class Cadastro
    {
        public int id { get; set; }
        public string nome { get; set; } = string.Empty;
        public string descricao { get; set; } = string.Empty;
        public string? endereco { get; set; }
        public string? telefone { get; set; }
        public string? email { get; set; }
        public DateOnly dataAbertura { get; set; }
        public decimal saldoInicial { get; set; }
        public string tipoConta { get; set; } = string.Empty;
            }
}
