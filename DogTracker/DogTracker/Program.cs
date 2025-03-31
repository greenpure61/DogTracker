using DogTracker.Web.Data;
using DogTracker.Web.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR(); // Add SignalR service

builder.Services.AddScoped<DogRepository>();
builder.Services.AddScoped<EatingHabitRepository>();
builder.Services.AddScoped<ToiletHabitRepository>();
builder.Services.AddScoped<WeightMeasurementRepository>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error"); // Route to error action in production
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage(); // Show detailed errors only in development
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<DogUpdateHub>("/dogUpdateHub"); // <-- MAP HUB

app.Run();
