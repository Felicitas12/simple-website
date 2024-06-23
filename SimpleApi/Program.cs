using System.Net;
using System.Net.NetworkInformation;
using System.Security.Cryptography.X509Certificates;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.MapGet("/api", () =>
{
    var response = new ApiResponse(
        Guid.NewGuid(),
        $"Hey, this was received from [{Dns.GetHostName()}]!"
    );
    return Results.Ok(response);
});

bool ftpLive = false;
string ftpServer = "fs-1.fallen.lan";

app.MapGet("/api/ftp", async () =>
{
    var ping = new Ping();
    var resp = await ping.SendPingAsync(ftpServer);
    if (resp.Status  == IPStatus.Success)
    {
        ftpLive = true;
        Results.Ok();
    }
    else
    {
        ftpLive = false;
        Results.NotFound();
    }
});
app.MapGet("/api/download", async () =>
{
    if (ftpLive)
    {
        var ftpRequest = (FtpWebRequest)
            WebRequest.Create($"ftp://{ftpServer}/test_file.txt");
        ftpRequest.Credentials = new NetworkCredential("vsftpd", "password");
        ftpRequest.Method = WebRequestMethods.Ftp.DownloadFile;
        ftpRequest.KeepAlive = false;

        ftpRequest.EnableSsl = true;
        var cert = X509Certificate.CreateFromCertFile("/home/fallen/cacert.pem");
        ftpRequest.ClientCertificates.Add(cert);
        
        var resp = await ftpRequest.GetResponseAsync();
        var ftpStream = resp.GetResponseStream();
        
        return ftpStream == null 
                ? Results.NotFound() 
                : Results.File(ftpStream, "application/octet-stream", "ftp_file.txt");
    }
    else
        return Results.NotFound();
});


app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
record ApiResponse(Guid Id, string? Message);
