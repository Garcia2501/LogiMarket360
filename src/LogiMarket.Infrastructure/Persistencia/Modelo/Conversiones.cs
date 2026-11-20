using LogiMarket.Domain.Catalogo;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LogiMarket.Infrastructure.Persistencia.Modelo;

internal sealed class SkuConverter() : ValueConverter<Sku, string>(sku => sku.Valor, valor => new Sku(valor));

internal sealed class FechaUtcConverter() : ValueConverter<DateTime, DateTime>(
    fecha => fecha,
    fecha => DateTime.SpecifyKind(fecha, DateTimeKind.Utc));
