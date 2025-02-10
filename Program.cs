using API.Data;
using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

builder.Services.AddDbContext<ApplicationDbContext>(options=> 
options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseAuthorization();
app.UseStaticFiles();
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/login.html"));

app.Run();
