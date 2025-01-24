using Microsoft.EntityFrameworkCore;
using MultitarefaAPI.Controllers;
using MultitarefaAPI.Data;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ExecutionTimeActionFilter>();
});
builder.Services.AddScoped<ExecutionTimeActionFilter>();

builder.Services.AddApplicationInsightsTelemetry(builder.Configuration["ApplicationInsights:ConnectionString"]);
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.EnableDependencyTrackingTelemetryModule = true;
    options.EnableRequestTrackingTelemetryModule = true;
});

builder.Services.AddDbContext<CadastroContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

builder.WebHost.UseUrls("xxxxxxx");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});

var httpRequestsCounter = Metrics.CreateCounter(
    "http_requests_total",
    "Número total de requisições HTTP recebidas e seus resultados.",
    new CounterConfiguration
    {
        LabelNames = new[] { "method", "status", "result" }
    });

var app = builder.Build();

app.Use(async (context, next) =>
{
    try
    {
        await next();
        httpRequestsCounter
            .WithLabels(context.Request.Method, context.Response.StatusCode.ToString(), "success")
            .Inc();
    }
    catch (Exception)
    {
        httpRequestsCounter
            .WithLabels(context.Request.Method, "500", "error")
            .Inc();
        throw;
    }
});

app.UseApplicationInsightsRequestTelemetry();
app.UseMetricServer();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
