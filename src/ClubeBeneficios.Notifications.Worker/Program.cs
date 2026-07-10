using ClubeBeneficios.Notifications.Worker.Configuration;
using ClubeBeneficios.Notifications.Worker.Infrastructure.Database;
using ClubeBeneficios.Notifications.Worker.Infrastructure.Email;
using ClubeBeneficios.Notifications.Worker.Infrastructure.Repositories;
using ClubeBeneficios.Notifications.Worker.Services;
using ClubeBeneficios.Notifications.Worker.Workers;
using Dapper;

DefaultTypeMap.MatchNamesWithUnderscores = true;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<NotificationWorkerOptions>(
    builder.Configuration.GetSection("NotificationWorker"));

builder.Services.Configure<SmtpOptions>(
    builder.Configuration.GetSection("Smtp"));

builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
builder.Services.AddScoped<INotificationOutboxRepository, NotificationOutboxRepository>();

builder.Services.AddSingleton<ITemplateRenderer, SimpleTemplateRenderer>();
builder.Services.AddScoped<INotificationProcessingService, NotificationProcessingService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();

builder.Services.AddHostedService<NotificationBackgroundWorker>();

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "Clube Beneficios Notifications Worker";
});

var host = builder.Build();

await host.RunAsync();
