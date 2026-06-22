WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

IServiceCollection services = builder.Services;
services.AddControllers();
services.AddOpenApi();
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();

WebApplication app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
