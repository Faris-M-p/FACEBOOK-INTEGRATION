using FACEBOOK_INTEGRATION.Data;
using FACEBOOK_INTEGRATION.Interface;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IClientInterface, ClientRepository>();
builder.Services.AddScoped<IFacebookConnectionInterface, FacebookConnectionRepository>();
builder.Services.AddScoped<IFacebookPageInterface, FacebookPageRepository>();
builder.Services.AddScoped<IFacebookPostInterface, FacebookPostRepository>();

builder.Services.AddScoped<FACEBOOK_INTEGRATION.Services.FacebookTokenService>();
builder.Services.AddScoped<FACEBOOK_INTEGRATION.Services.FacebookService>();

var app = builder.Build();

app.UseExceptionHandler("/Error");
app.UseStatusCodePagesWithReExecute("/Error", "?code={0}");

if (!app.Environment.IsDevelopment())
{
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
