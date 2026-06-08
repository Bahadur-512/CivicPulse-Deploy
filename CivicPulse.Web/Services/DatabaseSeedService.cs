using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using CivicPulse.Core.Entities;
using CivicPulse.Core.Enums;
using CivicPulse.Core.Helpers;
using CivicPulse.Infrastructure.Data;

namespace CivicPulse.Web.Services;

public class DatabaseSeedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DatabaseSeedService> _logger;
    private readonly IConfiguration _configuration;

    public DatabaseSeedService(IServiceScopeFactory scopeFactory, ILogger<DatabaseSeedService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.EnsureCreatedAsync(cancellationToken);

        if (!db.Categories.Any()) await SeedCategoriesAsync(db);
        if (!db.Users.Any()) await SeedUsersAsync(db);
        if (!db.SlaConfigs.Any()) await SeedSlaConfigsAsync(db);
        if (!db.Complaints.Any()) await SeedSampleComplaintsAsync(db);
        if (!db.Feedbacks.Any()) await SeedSampleFeedbackAsync(db);

        _logger.LogInformation("Database seeded successfully.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task SeedCategoriesAsync(AppDbContext db)
    {
        var categories = new List<Category>
        {
            new() { Name = "Road Damage", Description = "Damaged roads, potholes, broken pavement", IconName = "bi-road", ColorHex = "#FF6B35", DefaultSlaDays = 7, DefaultPriority = PriorityLevel.High, Keywords = "road,pothole,crack,damaged,broken road,pavement" },
            new() { Name = "Water Supply", Description = "Water supply issues, pipe leaks, shortage", IconName = "bi-droplet", ColorHex = "#2196F3", DefaultSlaDays = 2, DefaultPriority = PriorityLevel.Critical, Keywords = "water,pipe,leak,supply,shortage,pressure" },
            new() { Name = "Electricity Fault", Description = "Power outages, electrical faults, transformer issues", IconName = "bi-lightning", ColorHex = "#FFC107", DefaultSlaDays = 1, DefaultPriority = PriorityLevel.Critical, Keywords = "electricity,light,power,outage,wire,transformer,fault" },
            new() { Name = "Waste & Sanitation", Description = "Garbage collection, waste dumping, sanitation", IconName = "bi-trash", ColorHex = "#4CAF50", DefaultSlaDays = 3, DefaultPriority = PriorityLevel.Medium, Keywords = "garbage,waste,trash,dump,sanitation,smell,dirty" },
            new() { Name = "Street Lighting", Description = "Street light malfunctions, dark areas", IconName = "bi-lamp", ColorHex = "#9C27B0", DefaultSlaDays = 5, DefaultPriority = PriorityLevel.Medium, Keywords = "light,street light,lamp,dark,lighting" },
            new() { Name = "Sewerage", Description = "Sewer blockages, drain overflow, sewage issues", IconName = "bi-water", ColorHex = "#795548", DefaultSlaDays = 2, DefaultPriority = PriorityLevel.High, Keywords = "sewer,drain,overflow,sewage,blocked" },
            new() { Name = "Parks & Green Spaces", Description = "Park maintenance, gardens, playgrounds", IconName = "bi-tree", ColorHex = "#4CAF50", DefaultSlaDays = 10, DefaultPriority = PriorityLevel.Low, Keywords = "park,tree,garden,grass,bench,playground" },
            new() { Name = "Building Hazard", Description = "Unsafe buildings, structural hazards, collapse risk", IconName = "bi-building", ColorHex = "#F44336", DefaultSlaDays = 1, DefaultPriority = PriorityLevel.Critical, Keywords = "building,collapse,crack,unsafe,hazard,falling" },
            new() { Name = "Noise Pollution", Description = "Excessive noise, loudspeakers, disturbances", IconName = "bi-volume-up", ColorHex = "#607D8B", DefaultSlaDays = 5, DefaultPriority = PriorityLevel.Low, Keywords = "noise,loud,sound,disturb,horn,speaker" },
            new() { Name = "Encroachment", Description = "Illegal construction, footpath blocking, encroachment", IconName = "bi-x-circle", ColorHex = "#E91E63", DefaultSlaDays = 7, DefaultPriority = PriorityLevel.Medium, Keywords = "encroach,block,footpath,road block,illegal" }
        };

        db.Categories.AddRange(categories);
        await db.SaveChangesAsync();
    }

    private async Task SeedUsersAsync(AppDbContext db)
    {
        var adminUser = new ApplicationUser
        {
            FullName = "System Admin",
            Email = _configuration["AppSettings:DefaultAdminEmail"] ?? "admin@civicpulse.pk",
            PasswordHash = PasswordHelper.HashPassword(_configuration["AppSettings:DefaultAdminPassword"] ?? "Admin@123!"),
            Role = UserRole.Admin,
            IsActive = true,
            IsEmailVerified = true
        };

        var demoCitizen = new ApplicationUser
        {
            FullName = "Demo Citizen",
            Email = "citizen@demo.pk",
            PasswordHash = PasswordHelper.HashPassword("Demo@123!"),
            Role = UserRole.Citizen,
            IsActive = true,
            IsEmailVerified = true,
            PhoneNumber = "0300-1234567"
        };

        db.Users.AddRange(adminUser, demoCitizen);
        await db.SaveChangesAsync();
    }

    private async Task SeedSlaConfigsAsync(AppDbContext db)
    {
        var categories = await db.Categories.ToListAsync();
        var priorities = Enum.GetValues<PriorityLevel>();

        foreach (var category in categories)
        {
            foreach (var priority in priorities)
            {
                var slaDays = priority switch
                {
                    PriorityLevel.Critical => Math.Max(1, category.DefaultSlaDays / 2),
                    PriorityLevel.High => category.DefaultSlaDays,
                    PriorityLevel.Medium => (int)(category.DefaultSlaDays * 1.5),
                    PriorityLevel.Low => category.DefaultSlaDays * 2,
                    _ => category.DefaultSlaDays
                };

                db.SlaConfigs.Add(new SlaConfig
                {
                    CategoryId = category.Id,
                    Priority = priority,
                    ResolutionDays = slaDays,
                    WarningThresholdPercent = 80,
                    IsActive = true
                });
            }
        }

        await db.SaveChangesAsync();
    }

    private async Task SeedSampleComplaintsAsync(AppDbContext db)
    {
        var citizen = await db.Users.FirstAsync(u => u.Email == "citizen@demo.pk");
        var admin = await db.Users.FirstAsync(u => u.Email == "admin@civicpulse.pk");
        var categories = await db.Categories.ToListAsync();

        var islamabadLocations = new[]
        {
            (33.6844, 73.0479, "Faisal Avenue, Islamabad"),
            (33.6938, 73.0653, "Jinnah Avenue, Islamabad"),
            (33.7204, 73.0761, "I-8 Sector, Islamabad"),
            (33.6699, 73.0180, "G-11 Sector, Islamabad"),
            (33.6485, 73.0784, "Blue Area, Islamabad"),
            (33.7035, 73.0658, "I-10 Sector, Islamabad"),
            (33.6829, 73.0234, "G-9 Sector, Islamabad"),
            (33.7100, 73.0500, "H-8 Sector, Islamabad"),
            (33.6600, 73.0900, "F-10 Sector, Islamabad"),
            (33.6740, 73.0050, "G-13 Sector, Islamabad"),
            (33.6930, 73.0800, "I-9 Sector, Islamabad"),
            (33.7200, 73.0500, "I-11 Sector, Islamabad"),
            (33.6770, 73.0350, "F-7 Sector, Islamabad"),
            (33.6900, 73.0900, "F-11 Sector, Islamabad"),
            (33.6950, 73.0100, "G-12 Sector, Islamabad"),
            (33.6680, 73.0550, "F-6 Sector, Islamabad"),
            (33.7150, 73.0700, "I-8/3, Islamabad"),
            (33.6750, 73.0420, "G-7 Sector, Islamabad"),
            (33.6840, 73.0700, "F-8 Sector, Islamabad"),
            (33.7000, 73.0400, "G-10 Sector, Islamabad")
        };

        var statuses = new[] { ComplaintStatus.Pending, ComplaintStatus.UnderReview, ComplaintStatus.InProgress, ComplaintStatus.Resolved, ComplaintStatus.Closed };
        var rng = new Random(42);
        var sampleComplaints = new List<Complaint>();
        var statusHistories = new List<StatusHistory>();

        for (int i = 0; i < 20; i++)
        {
            var cat = categories[i % categories.Count];
            var loc = islamabadLocations[i % islamabadLocations.Length];
            var status = statuses[i % statuses.Length];
            var daysAgo = rng.Next(1, 60);
            var createdAt = DateTime.UtcNow.AddDays(-daysAgo);

            var complaint = new Complaint
            {
                ComplaintNumber = $"CP-{DateTime.UtcNow.Year}-{1001 + i:D5}",
                Title = SampleTitles[i % SampleTitles.Length],
                Description = SampleDescriptions[i % SampleDescriptions.Length],
                Status = status,
                Priority = cat.DefaultPriority,
                Latitude = loc.Item1,
                Longitude = loc.Item2,
                LocationDescription = loc.Item3,
                CitizenId = citizen.Id,
                CategoryId = cat.Id,
                SlaDeadline = createdAt.AddDays(cat.DefaultSlaDays),
                CreatedAt = createdAt,
                UpdatedAt = createdAt,
                AssignedAdminId = status > ComplaintStatus.Pending ? admin.Id : null,
                ResolvedAt = status >= ComplaintStatus.Resolved ? createdAt.AddDays(rng.Next(1, cat.DefaultSlaDays)) : null
            };

            if (complaint.ResolvedAt.HasValue && complaint.ResolvedAt > complaint.SlaDeadline)
                complaint.IsSlaBreached = true;

            sampleComplaints.Add(complaint);
            db.Complaints.Add(complaint);
            await db.SaveChangesAsync();

            statusHistories.Add(new StatusHistory
            {
                ComplaintId = complaint.Id,
                FromStatus = null,
                ToStatus = ComplaintStatus.Pending,
                Note = "Complaint submitted",
                ChangedById = citizen.Id,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            });

            if (status > ComplaintStatus.Pending)
            {
                statusHistories.Add(new StatusHistory
                {
                    ComplaintId = complaint.Id,
                    FromStatus = ComplaintStatus.Pending,
                    ToStatus = ComplaintStatus.UnderReview,
                    Note = "Complaint under review",
                    ChangedById = admin.Id,
                    CreatedAt = createdAt.AddHours(2),
                    UpdatedAt = createdAt.AddHours(2)
                });
            }

            if (status >= ComplaintStatus.InProgress)
            {
                statusHistories.Add(new StatusHistory
                {
                    ComplaintId = complaint.Id,
                    FromStatus = ComplaintStatus.UnderReview,
                    ToStatus = ComplaintStatus.InProgress,
                    Note = "Work in progress",
                    ChangedById = admin.Id,
                    CreatedAt = createdAt.AddDays(1),
                    UpdatedAt = createdAt.AddDays(1)
                });
            }

            if (status >= ComplaintStatus.Resolved)
            {
                statusHistories.Add(new StatusHistory
                {
                    ComplaintId = complaint.Id,
                    FromStatus = ComplaintStatus.InProgress,
                    ToStatus = ComplaintStatus.Resolved,
                    Note = "Issue resolved",
                    ChangedById = admin.Id,
                    CreatedAt = complaint.ResolvedAt ?? createdAt.AddDays(2),
                    UpdatedAt = complaint.ResolvedAt ?? createdAt.AddDays(2)
                });
            }
        }

        db.StatusHistories.AddRange(statusHistories);
        await db.SaveChangesAsync();
    }

    private async Task SeedSampleFeedbackAsync(AppDbContext db)
    {
        var citizen = await db.Users.FirstAsync(u => u.Email == "citizen@demo.pk");
        var resolvedComplaints = await db.Complaints
            .Where(c => c.Status >= ComplaintStatus.Resolved)
            .ToListAsync();

        var rng = new Random(99);
        var feedbackEntries = new List<Feedback>();
        var sampleComments = new[]
        {
            "Very satisfied with the quick response!",
            "Good work but could be faster.",
            "The issue was resolved satisfactorily.",
            "Excellent service, thank you!",
            "Decent response time, problem fixed.",
            "Happy with the outcome overall.",
            "Could improve communication, but work done well.",
            "Great job! Keep it up.",
            "Average experience, expected faster service.",
            "Very professional team, appreciated."
        };

        foreach (var complaint in resolvedComplaints)
        {
            var rating = rng.Next(2, 6);
            feedbackEntries.Add(new Feedback
            {
                ComplaintId = complaint.Id,
                CitizenId = citizen.Id,
                Rating = rating,
                Comment = sampleComments[rng.Next(sampleComments.Length)],
                WasResolutionSatisfactory = rating >= 3,
                WasResponseTimely = rating >= 3,
                CreatedAt = complaint.ResolvedAt ?? complaint.UpdatedAt
            });
        }

        db.Feedbacks.AddRange(feedbackEntries);
        await db.SaveChangesAsync();
    }

    private static readonly string[] SampleTitles =
    {
        "Deep pothole on main road causing accidents",
        "Water supply interrupted for 3 days",
        "Street light not working for a week",
        "Garbage not collected from sector dump",
        "Sewer line overflow in street",
        "Park swing broken and unsafe",
        "Electricity transformer sparking dangerously",
        "Loud construction noise after midnight",
        "Illegal footpath encroachment by shop",
        "Cracked building wall appears unsafe",
        "Broken water pipe flooding the street",
        "Fallen tree branch blocking footpath",
        "No street light in dark alley",
        "Open manhole cover very dangerous",
        "School zone missing speed bumps",
        "Drainage blocked causing flood after rain",
        "Park bench broken and needs repair",
        "Stray dogs near school area",
        "Public toilet not maintained",
        "Playground equipment rusted and broken"
    };

    private static readonly string[] SampleDescriptions =
    {
        "There is a large pothole approximately 3 feet wide and 1 foot deep on the main road near the signal. Multiple vehicles have suffered tire damage. This needs urgent repair.",
        "The water supply in our area has been completely cut off for the past 3 days. Residents are facing severe hardship. Please restore water supply immediately.",
        "The street light outside house number 12 has been malfunctioning for a week. The area remains completely dark at night, posing safety risks for residents.",
        "Garbage has not been collected from the sector dump point for over two weeks. The waste is piling up and creating a health hazard with foul smell.",
        "The sewer line on our street is overflowing and wastewater is flowing into houses. This is a serious health concern and needs immediate attention.",
        "The children's swing set in the neighborhood park is broken with sharp edges exposed. A child could get seriously injured. Please repair urgently.",
        "The electricity transformer near the market is sparking loudly and making humming sounds. Residents are worried it might explode. Emergency repair needed.",
        "A construction site on our street is operating heavy machinery after midnight daily. The noise is unbearable and residents cannot sleep.",
        "A shop owner has extended his stall onto the footpath, blocking pedestrian access completely. Elderly and disabled persons cannot pass through.",
        "A building in our neighborhood has developed large cracks in its main wall. This appears structurally unsafe and could collapse during rain.",
        "A water pipe has burst on the main road and water is gushing out, flooding the entire street. Wastage of water and slippery road surface.",
        "A large tree branch fell during the storm and is now blocking the footpath near the school. Children are having to walk on the road.",
        "Several street lights in the alley behind the market have been non-functional for months. The alley is very dark and unsafe at night.",
        "The manhole cover on the corner of our street is missing. It's a serious safety hazard especially for children and elderly walking at night.",
        "There are no speed bumps near the school zone on the main road. Vehicles speed past when children are crossing. Very dangerous situation.",
        "The drainage system in our area is completely blocked. During the recent rain, water accumulated knee-deep on the streets for hours.",
        "The wooden bench in the park is completely broken with nails sticking out. Seniors who sit there are at risk of injury.",
        "A pack of stray dogs has become aggressive near the school entrance. Parents are worried about children's safety during pick-up and drop-off.",
        "The public toilet at the sector park has not been cleaned in weeks. It is unusable and smells terrible. Needs regular maintenance.",
        "The playground slide and see-saw are rusted with sharp edges. Children have gotten minor injuries. Equipment needs replacement or repair."
    };
}
