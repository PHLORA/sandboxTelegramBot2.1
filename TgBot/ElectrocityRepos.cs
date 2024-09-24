using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types;

namespace TgBot;

public class ElectrocityRepos
{
    private readonly ApplicationDbContext _dbContext;


    public ElectrocityRepos(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Electrocity>> Get(string month)
    {
        return await _dbContext.Electrocities
            .Where(p=>p.Date.Month == Convert.ToInt32(month))
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task Add(String message)
    {
        Electrocity elect = new Electrocity();
            elect.Indicate = Convert.ToSingle(message); 
            elect.Date = DateTime.Today;

            
            elect.Difference = await GetDiffAsync(elect.Indicate);

           
            _dbContext.Add(elect);
            await _dbContext.SaveChangesAsync();
        
            Console.WriteLine("Data successfully added.");
    }

    public async Task<float> GetDiffAsync(float indicate)
    {
        var lastIndicate = await _dbContext.Electrocities
            .OrderByDescending(p => p.Date)
            .Select(p =>p.Indicate)
            .FirstOrDefaultAsync();
        
            return indicate - lastIndicate;
    }
}