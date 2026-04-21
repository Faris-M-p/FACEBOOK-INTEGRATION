var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpClient();
builder.Services.Configure<FACEBOOK_INTEGRATION.Options.FacebookOptions>(
    builder.Configuration.GetSection(FACEBOOK_INTEGRATION.Options.FacebookOptions.SectionName));

builder.Services.AddSingleton<FACEBOOK_INTEGRATION.Data.SqlConnectionFactory>();
builder.Services.AddScoped<FACEBOOK_INTEGRATION.Data.ClientRepository>();
builder.Services.AddScoped<FACEBOOK_INTEGRATION.Data.FacebookConnectionRepository>();
builder.Services.AddScoped<FACEBOOK_INTEGRATION.Data.FacebookPageRepository>();
builder.Services.AddScoped<FACEBOOK_INTEGRATION.Data.FacebookPostRepository>();

builder.Services.AddScoped<FACEBOOK_INTEGRATION.Services.FacebookTokenService>();
builder.Services.AddScoped<FACEBOOK_INTEGRATION.Services.FacebookService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
// Always show a friendly error page (avoid the big developer exception screen).
app.UseExceptionHandler("/Error");
app.UseStatusCodePagesWithReExecute("/Error", "?code={0}");

if (!app.Environment.IsDevelopment())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();

app.Run();
