using Cashflow.Abstractions;
using Cashflow.Infrastructure.Data;
using Cashflow.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cashflow.Infrastructure.Repositories;

/// <summary>
/// Implementação do repositório de lançamentos usando Entity Framework
/// </summary>
public class LancamentoRepository : ILancamentoRepository
{
    private readonly CashflowDbContext _context;

    public LancamentoRepository(CashflowDbContext context)
    {
        _context = context;
    }

    public async Task<Lancamento?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Lancamentos
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

        return entity?.ToDomain();
    }

    public async Task<IEnumerable<Lancamento>> ObterPorDataAsync(DateTime data, CancellationToken cancellationToken = default)
    {
        var dataInicio = data.Date;
        var dataFim = dataInicio.AddDays(1);

        var entities = await _context.Lancamentos
            .AsNoTracking()
            .Where(l => l.Data >= dataInicio && l.Data < dataFim)
            .OrderBy(l => l.Data)
            .ToListAsync(cancellationToken);

        return entities.Select(e => e.ToDomain());
    }

    public async Task<IEnumerable<Lancamento>> ObterPorPeriodoAsync(DateTime dataInicio, DateTime dataFim, CancellationToken cancellationToken = default)
    {
        var inicio = dataInicio.Date;
        var fim = dataFim.Date.AddDays(1);

        var entities = await _context.Lancamentos
            .AsNoTracking()
            .Where(l => l.Data >= inicio && l.Data < fim)
            .OrderBy(l => l.Data)
            .ToListAsync(cancellationToken);

        return entities.Select(e => e.ToDomain());
    }

    public async Task<Lancamento> AdicionarAsync(Lancamento lancamento, CancellationToken cancellationToken = default)
    {
        var entity = LancamentoEntity.FromDomain(lancamento);
        
        _context.Lancamentos.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return lancamento;
    }

    public async Task<IEnumerable<Lancamento>> ObterTodosAsync(int pagina, int tamanhoPagina, CancellationToken cancellationToken = default)
    {
        var entities = await _context.Lancamentos
            .AsNoTracking()
            .OrderByDescending(l => l.Data)
            .ThenByDescending(l => l.CreatedAt)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync(cancellationToken);

        return entities.Select(e => e.ToDomain());
    }

    public async Task<int> ContarAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Lancamentos.CountAsync(cancellationToken);
    }
}


