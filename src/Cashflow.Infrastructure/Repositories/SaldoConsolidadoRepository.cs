using Cashflow.Abstractions;
using Cashflow.Infrastructure.Data;
using Cashflow.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cashflow.Infrastructure.Repositories;

/// <summary>
/// Implementação do repositório de saldos consolidados usando Entity Framework
/// </summary>
public class SaldoConsolidadoRepository : ISaldoConsolidadoRepository
{
    private readonly CashflowDbContext _context;

    public SaldoConsolidadoRepository(CashflowDbContext context)
    {
        _context = context;
    }

    public async Task<SaldoDiario?> ObterPorDataAsync(DateTime data, CancellationToken cancellationToken = default)
    {
        var dataConsulta = data.Date;

        var entity = await _context.SaldosConsolidados
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Data == dataConsulta, cancellationToken);

        return entity?.ToDomain();
    }

    public async Task<IEnumerable<SaldoDiario>> ObterPorPeriodoAsync(DateTime dataInicio, DateTime dataFim, CancellationToken cancellationToken = default)
    {
        var inicio = dataInicio.Date;
        var fim = dataFim.Date;

        var entities = await _context.SaldosConsolidados
            .AsNoTracking()
            .Where(s => s.Data >= inicio && s.Data <= fim)
            .OrderBy(s => s.Data)
            .ToListAsync(cancellationToken);

        return entities.Select(e => e.ToDomain());
    }

    public async Task SalvarAsync(SaldoDiario saldoDiario, CancellationToken cancellationToken = default)
    {
        var entity = SaldoConsolidadoEntity.FromDomain(saldoDiario);

        var existente = await _context.SaldosConsolidados
            .FirstOrDefaultAsync(s => s.Data == entity.Data, cancellationToken);

        if (existente != null)
        {
            // Atualiza o existente
            existente.TotalCreditos = entity.TotalCreditos;
            existente.TotalDebitos = entity.TotalDebitos;
            existente.Saldo = entity.Saldo;
            existente.QuantidadeLancamentos = entity.QuantidadeLancamentos;
            existente.ProcessadoEm = DateTime.UtcNow;
        }
        else
        {
            // Adiciona novo
            _context.SaldosConsolidados.Add(entity);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<SaldoDiario> RecalcularAsync(DateTime data, CancellationToken cancellationToken = default)
    {
        var dataInicio = data.Date;
        var dataFim = dataInicio.AddDays(1);

        // Busca todos os lançamentos do dia
        var lancamentos = await _context.Lancamentos
            .AsNoTracking()
            .Where(l => l.Data >= dataInicio && l.Data < dataFim)
            .ToListAsync(cancellationToken);

        // Calcula os totais
        var totalCreditos = lancamentos
            .Where(l => l.Tipo == (short)TipoLancamento.Credito)
            .Sum(l => l.Valor);

        var totalDebitos = lancamentos
            .Where(l => l.Tipo == (short)TipoLancamento.Debito)
            .Sum(l => l.Valor);

        // Cria o saldo diário
        var saldoDiario = SaldoDiario.Vazio(data);
        
        // Usa reflection para definir os valores
        var tipoSaldo = typeof(SaldoDiario);
        tipoSaldo.GetProperty(nameof(SaldoDiario.TotalCreditos))?.SetValue(saldoDiario, totalCreditos);
        tipoSaldo.GetProperty(nameof(SaldoDiario.TotalDebitos))?.SetValue(saldoDiario, totalDebitos);
        tipoSaldo.GetProperty(nameof(SaldoDiario.QuantidadeLancamentos))?.SetValue(saldoDiario, lancamentos.Count);

        // Salva no banco
        await SalvarAsync(saldoDiario, cancellationToken);

        return saldoDiario;
    }
}

