using DotNetEnv;
using MediatR;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

// Env wins over appsettings.json. Consumed by AddDbContext in a later phase.
_ = Environment.GetEnvironmentVariable("DB_CONNECTION")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "ShopApi", Version = "v1" });
});

// TODO(phase-03): point MediatR registration to the Application assembly marker
// instead of Program once the first handler is added.
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// TODO(phase-03+): AddValidatorsFromAssembly, AddAutoMapper,
// AddDbContext<AppDbContext> (using connectionString), AddScoped<IUnitOfWork, ...>.

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// TODO(auth-phase): add app.UseAuthentication() before authorization once JWT is introduced.
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
