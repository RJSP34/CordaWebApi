using System.Reflection;
using System.Security.Cryptography.X509Certificates;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.WithOrigins("http://localhost:4200")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

builder.Host.ConfigureLogging(logging =>
{
    logging.AddConsole();
});

var app = builder.Build();

app.UseCors("AllowAll");

app.UseSwagger();
app.UseSwaggerUI();

string appDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

string certificatePath = Path.Combine(appDirectory, "Certificate", "certificate.crt");

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
