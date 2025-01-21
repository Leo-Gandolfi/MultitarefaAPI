using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.ApplicationInsights;  // Biblioteca para Azure Application Insights
using MultitarefaAPI.Models;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MultitarefaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ServiceFilter(typeof(ExecutionTimeActionFilter))] // O filtro de execução de tempo é registrado no Program.cs
    public class CadastroController : ControllerBase
    {
        private readonly CadastroContext _context;
        private readonly TelemetryClient _telemetryClient;  // Usado para o Azure Application Insights

        public CadastroController(CadastroContext context, TelemetryClient telemetryClient)
        {
            _context = context;
            _telemetryClient = telemetryClient;  // Inicialização do cliente para enviar métricas ao Azure
        }

        // GET: api/Cadastro
        [HttpGet]
        public async Task<ActionResult> GetCadastros([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                pageSize = Math.Min(pageSize, 100); // Limita o máximo de itens por página a 100
                var cadastros = await _context.Cadastros
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(c => new CadastroDTO
                    {
                        Id = c.id,
                        Nome = c.nome,
                        Descricao = c.descricao,
                        Endereco = c.endereco,
                        Telefone = c.telefone,
                        Email = c.email,
                        DataAbertura = c.dataAbertura,
                        SaldoInicial = c.saldoInicial,
                        TipoConta = c.tipoConta
                    })
                    .ToListAsync();

                return Ok(new { success = true, data = cadastros });
            }
            catch (Exception ex)
            {
                // Envio da exceção para o Azure Application Insights
                _telemetryClient.TrackException(ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new { success = false, message = "Erro ao buscar cadastros." });
            }
        }

        // GET: api/Cadastro/5
        [HttpGet("{id}")]
        public async Task<ActionResult> GetCadastro(int id)
        {
            try
            {
                var cadastro = await _context.Cadastros.FindAsync(id);

                if (cadastro == null)
                {
                    return NotFound(new { success = false, message = $"Cadastro com ID {id} não encontrado." });
                }

                return Ok(new { success = true, data = cadastro });
            }
            catch (Exception ex)
            {
                // Envio da exceção para o Azure Application Insights
                _telemetryClient.TrackException(ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new { success = false, message = "Erro ao buscar cadastro." });
            }
        }

        // POST: api/Cadastro
        [HttpPost]
        public async Task<ActionResult> PostCadastro(Cadastro cadastro)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Dados inválidos.", errors = ModelState });
                }

                cadastro.dataAbertura = DateOnly.FromDateTime(DateTime.Now); // Define a data de criação
                cadastro.saldoInicial = Math.Max(cadastro.saldoInicial, 0); // Valida saldo inicial

                _context.Cadastros.Add(cadastro);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetCadastro), new { id = cadastro.id }, new { success = true, data = cadastro });
            }
            catch (Exception ex)
            {
                // Envio da exceção para o Azure Application Insights
                _telemetryClient.TrackException(ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new { success = false, message = "Erro ao salvar cadastro." });
            }
        }

        // PUT: api/Cadastro/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCadastro(int id, Cadastro cadastro)
        {
            try
            {
                var existingCadastro = await _context.Cadastros.FindAsync(id);
                if (existingCadastro == null)
                {
                    return NotFound(new { success = false, message = $"Cadastro com ID {id} não encontrado." });
                }

                existingCadastro.nome = cadastro.nome;
                existingCadastro.descricao = cadastro.descricao;
                existingCadastro.endereco = cadastro.endereco;
                existingCadastro.telefone = cadastro.telefone;
                existingCadastro.email = cadastro.email;
                existingCadastro.tipoConta = cadastro.tipoConta;

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Cadastro atualizado com sucesso." });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // Envio da exceção para o Azure Application Insights
                _telemetryClient.TrackException(ex);
                return Conflict(new { success = false, message = "Houve um conflito de concorrência." });
            }
            catch (Exception ex)
            {
                // Envio da exceção para o Azure Application Insights
                _telemetryClient.TrackException(ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new { success = false, message = "Erro ao atualizar cadastro." });
            }
        }

        // DELETE: api/Cadastro/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCadastro(int id)
        {
            try
            {
                var cadastro = await _context.Cadastros.FindAsync(id);
                if (cadastro == null)
                {
                    return NotFound(new { success = false, message = $"Cadastro com ID {id} não encontrado." });
                }

                _context.Cadastros.Remove(cadastro);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Cadastro deletado com sucesso." });
            }
            catch (Exception ex)
            {
                // Envio da exceção para o Azure Application Insights
                _telemetryClient.TrackException(ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new { success = false, message = "Erro ao deletar cadastro." });
            }
        }
    }

    public class CadastroDTO
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string Descricao { get; set; }
        public string? Endereco { get; set; }
        public string? Telefone { get; set; }
        public string? Email { get; set; }
        public DateOnly DataAbertura { get; set; }
        public decimal SaldoInicial { get; set; }
        public string TipoConta { get; set; }
    }

    public class ExecutionTimeActionFilter : IActionFilter
    {
        private readonly TelemetryClient _telemetryClient;  // Usado para o Azure Application Insights

        public ExecutionTimeActionFilter(TelemetryClient telemetryClient)
        {
            _telemetryClient = telemetryClient;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            context.HttpContext.Items["ActionStartTime"] = Stopwatch.StartNew();
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            var stopwatch = (Stopwatch)context.HttpContext.Items["ActionStartTime"];
            stopwatch.Stop();
            var elapsedTime = stopwatch.ElapsedMilliseconds;

            // Envia o tempo de execução para o Azure Application Insights
            _telemetryClient.TrackMetric("ActionExecutionTime", elapsedTime);

            if (context.Exception != null)
            {
                // Envio da exceção para o Azure Application Insights
                _telemetryClient.TrackException(context.Exception);
                context.HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            }
        }
    }
}
