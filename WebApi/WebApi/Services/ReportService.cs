using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WebApi.Context;
using WebApi.Dto.OperationDto;
using WebApi.Services.IServices;

public class ReportService : IReportService
{
    private readonly FinanceContext _context;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

    public ReportService(FinanceContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<ReportDailyDto> GetDailyReportAsync(DateTime date)
    {
        var cacheKey = $"DailyReport_{date:yyyyMMdd}";
        if (!_cache.TryGetValue(cacheKey, out ReportDailyDto report))
        {
            var operations = await _context.Operations
                .Include(op => op.Type)
                .Where(op => op.Date.Date == date.Date)
                .ToListAsync();

            var totalIncome = operations
                .Where(op => op.Type?.IsIncome == true)
                .Sum(op => op.Amount);

            var totalExpenses = operations
                .Where(op => op.Type?.IsIncome == false)
                .Sum(op => op.Amount);

            var operationDtos = operations.Select(op => new OperationDto
            {
                Id = op.Id,
                Description = op.Description,
                Amount = op.Amount,
                Date = op.Date,
                TypeId = op.TypeId,
                TypeName = op.Type?.Name ?? "Unknown",
                IsIncome = op.Type?.IsIncome ?? false
            }).ToList();

            report = new ReportDailyDto
            {
                Date = date,
                TotalIncome = totalIncome,
                TotalExpenses = totalExpenses,
                Operations = operationDtos
            };

            // Зберігаємо результат у кеші
            _cache.Set(cacheKey, report, _cacheDuration);
        }

        return report;
    }

    public async Task<ReportPeriodDto> GetPeriodReportAsync(DateTime start, DateTime end)
    {
        var cacheKey = $"PeriodReport_{start:yyyyMMdd}_{end:yyyyMMdd}";
        if (!_cache.TryGetValue(cacheKey, out ReportPeriodDto report))
        {
            var operations = await _context.Operations
                .Include(op => op.Type)
                .Where(op => op.Date >= start && op.Date <= end)
                .ToListAsync();

            var totalIncome = operations
                .Where(op => op.Type?.IsIncome == true)
                .Sum(op => op.Amount);

            var totalExpenses = operations
                .Where(op => op.Type?.IsIncome == false)
                .Sum(op => op.Amount);

            var operationDtos = operations.Select(op => new OperationDto
            {
                Id = op.Id,
                Description = op.Description,
                Amount = op.Amount,
                Date = op.Date,
                TypeId = op.TypeId,
                TypeName = op.Type?.Name ?? "Unknown",
                IsIncome = op.Type?.IsIncome ?? false
            }).ToList();

            report = new ReportPeriodDto
            {
                TotalIncome = totalIncome,
                TotalExpenses = totalExpenses,
                Operations = operationDtos
            };

            // Зберігаємо результат у кеші
            _cache.Set(cacheKey, report, _cacheDuration);
        }

        return report;
    }
}
