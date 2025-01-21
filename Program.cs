using Microsoft.EntityFrameworkCore;
using MultitarefaAPI.Controllers;
using MultitarefaAPI.Data;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Adiciona o filtro global para medir o tempo de execução de cada ação
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ExecutionTimeActionFilter>();  // Filtro para medir o tempo de execução
});
builder.Services.AddScoped<ExecutionTimeActionFilter>();

//Configura o Application Insights para monitoramento e rastreamento
// O Application Insights permite monitorar o desempenho da aplicação, detectar falhas, e coletar métricas e logs automaticamente.
builder.Services.AddApplicationInsightsTelemetry(builder.Configuration["ApplicationInsights:ConnectionString"]);

// Configuração adicional para Application Insights, habilitando o rastreamento de dependências e requisições
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.EnableDependencyTrackingTelemetryModule = true;  // Habilita rastreamento de dependências externas (ex: banco de dados, APIs)
    options.EnableRequestTrackingTelemetryModule = true;    // Habilita rastreamento de requisições HTTP
});

// Configura a conexão com o banco de dados PostgreSQL
builder.Services.AddDbContext<CadastroContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Configura a URL para o servidor
builder.WebHost.UseUrls("http://localhost:5089");

// Adiciona suporte ao Swagger para documentação da API
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configura o CORS para permitir requisições de qualquer origem
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});

// Configura o Prometheus para coletar métricas sobre as requisições HTTP
var httpRequestsCounter = Metrics.CreateCounter(
    "http_requests_total",  // Nome da métrica
    "Número total de requisições HTTP recebidas e seus resultados.",  // Descrição da métrica
    new CounterConfiguration
    {
        LabelNames = new[] { "method", "status", "result" }  // Labels para identificar o método, status e resultado das requisições
    });

var app = builder.Build();

// Middleware para monitorar as requisições HTTP e incrementar o contador do Prometheus
app.Use(async (context, next) =>
{
    try
    {
        await next();  // Executa a próxima etapa do pipeline

        // Incrementa o contador para requisições bem-sucedidas
        httpRequestsCounter
            .WithLabels(context.Request.Method, context.Response.StatusCode.ToString(), "success")
            .Inc();
    }
    catch (Exception)
    {
        // Incrementa o contador para erros
        httpRequestsCounter
            .WithLabels(context.Request.Method, "500", "error")
            .Inc();

        throw;  // Relança a exceção para que o ASP.NET Core possa lidar com ela
    }
});

//Expondo as métricas para que o Prometheus as colete
app.UseApplicationInsightsRequestTelemetry();  // Usado para integração com Application Insights
app.UseMetricServer();  // Expondo métricas do Prometheus na rota /metrics

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();  // Habilita o Swagger em ambientes de desenvolvimento
    app.UseSwaggerUI();  // UI para visualizar a documentação da API
}

// Middleware para redirecionar requisições HTTP para HTTPS
app.UseHttpsRedirection();

// Middleware de autorização (caso seja necessário no futuro)
app.UseAuthorization();

// Mapeia os controllers para as rotas
app.MapControllers();

// Inicia a aplicação
app.Run();
