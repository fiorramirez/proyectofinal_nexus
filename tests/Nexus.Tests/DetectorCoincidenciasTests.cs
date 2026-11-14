using Nexus.Core.Entities;
using Nexus.Core.Services;

namespace Nexus.Tests;

/// <summary>CP004 / RF004: se sugieren coincidencias sin fusionar registros.</summary>
public class DetectorCoincidenciasTests
{
    private readonly DetectorCoincidencias _svc = new();
    private static readonly DateTime T = new(2026, 11, 13, 8, 0, 0, DateTimeKind.Utc);

    private static Incidente Inc(int id, int cat, string area, string titulo, DateTime fecha) => new()
    {
        IdIncidente = id, IdCategoria = cat, AreaAfectada = area, Titulo = titulo,
        Descripcion = titulo, FechaCreacion = fecha,
    };

    [Fact]
    public void ReportesSimilares_SeMuestranConSusCriterios()
    {
        var a = Inc(1, 1, "Biblioteca", "WiFi caído en biblioteca", T);
        var b = Inc(2, 1, "Biblioteca", "Sin WiFi biblioteca segundo piso", T.AddHours(2));
        var r = _svc.Detectar(b, new[] { a, b }, TimeSpan.FromDays(3));
        Assert.Single(r);
        Assert.Equal(1, r[0].IdIncidente);
        Assert.Contains("Misma categoría", r[0].Criterios);
    }

    [Fact]
    public void ReportesDistintos_NoSeRelacionan()
    {
        var a = Inc(1, 1, "Biblioteca", "WiFi caído", T);
        var b = Inc(2, 3, "Registro", "Impresora sin tinta", T.AddDays(10));
        Assert.Empty(_svc.Detectar(b, new[] { a, b }, TimeSpan.FromDays(3)));
    }

    [Fact]
    public void IncidentesCerrados_NoSeConsideran()
    {
        var a = Inc(1, 1, "Biblioteca", "WiFi caído en biblioteca", T);
        a.Estado = EstadoIncidente.Cerrado;
        var b = Inc(2, 1, "Biblioteca", "WiFi caído en biblioteca", T);
        Assert.Empty(_svc.Detectar(b, new[] { a }, TimeSpan.FromDays(3)));
    }
}
