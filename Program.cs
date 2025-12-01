using MailUptime.Data;
using MailUptime.Models;
using MailUptime.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MailboxSettings>(builder.Configuration.GetSection("MailboxSettings"));

builder.Services.AddDbContext<MailUptimeContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=MailUptime.db"));

builder.Services.AddSingleton<IMailUptimeService, MailUptimeService>();
builder.Services.AddHostedService<MailUptimeBackgroundService>();

builder.Services.AddControllers();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "MailUptime API";
        document.Info.Version = "v1";
        document.Info.Description = "API for monitoring mailbox status and checking for received emails. Supports IMAP and POP3 protocols.";
        return Task.CompletedTask;
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MailUptimeContext>();
    context.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(x=>
    {
        x.Theme = ScalarTheme.BluePlanet;
    });
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.Run();
