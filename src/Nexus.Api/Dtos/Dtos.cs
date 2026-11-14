using System.ComponentModel.DataAnnotations;
using Nexus.Core.Services;

namespace Nexus.Api.Dtos;

public class LoginRequest
{
    [Required] public string Correo { get; set; } = string.Empty;
    [Required] public string Contrasena { get; set; } = string.Empty;
}

public record LoginResponse(string Token, string Nombre, string Rol);

public class CrearUsuarioRequest
{
    [Required, MaxLength(120)] public string Nombre { get; set; } = string.Empty;
    [Required, EmailAddress, MaxLength(180)] public string Correo { get; set; } = string.Empty;
    [Required, MinLength(8)] public string Contrasena { get; set; } = string.Empty;
    [Required] public string Rol { get; set; } = string.Empty;
}

public record UsuarioDto(int Id, string Nombre, string Correo, string Rol, bool Activo);

public class CrearIncidenteRequest
{
    [Required, MaxLength(150)] public string Titulo { get; set; } = string.Empty;
    [Required] public string Descripcion { get; set; } = string.Empty;
    [Range(1, int.MaxValue)] public int IdCategoria { get; set; }
    [Required, MaxLength(120)] public string AreaAfectada { get; set; } = string.Empty;
    [Range(1, 5)] public byte Impacto { get; set; }
    [Range(1, 5)] public byte Urgencia { get; set; }
}

public class CambiarEstadoRequest
{
    [Required] public string Estado { get; set; } = string.Empty;
    [MaxLength(600)] public string? Observacion { get; set; }
}

public class AsignarRequest
{
    [Range(1, int.MaxValue)] public int IdRecurso { get; set; }
    [MaxLength(500)] public string? Observacion { get; set; }
}

public class CrearRecursoRequest
{
    [Required, MaxLength(120)] public string Nombre { get; set; } = string.Empty;
    [Required, MaxLength(500)] public string Capacidades { get; set; } = string.Empty;
    [Range(1, 100)] public int CapacidadMaxima { get; set; }
    public bool Disponible { get; set; } = true;
}

public record PrioridadDto(decimal Puntaje, string Nivel, IReadOnlyList<FactorPrioridad> Factores);

public record IncidenteDto(
    int Id, string Titulo, string Categoria, string AreaAfectada, byte Impacto, byte Urgencia,
    string Estado, DateTime FechaCreacion, PrioridadDto Prioridad);

public record CreadoResponse(IncidenteDto Incidente, IReadOnlyList<Coincidencia> Coincidencias);

public record RecursoDto(int Id, string Nombre, string Capacidades, bool Disponible, int CapacidadMaxima, int CargaActual);

public record RecomendacionDto(int IdRecurso, string Nombre, double Puntaje, int CargaActual, IReadOnlyList<string> Criterios);

public record RecomendacionesResponse(IReadOnlyList<RecomendacionDto> Recomendaciones, string? Mensaje);

public class SimulacionRequest
{
    [Required, MaxLength(120)] public string Nombre { get; set; } = "Escenario";
    /// <summary>idRecurso → disponible (true/false). Solo los recursos incluidos se modifican.</summary>
    public Dictionary<int, bool>? DisponibilidadRecursos { get; set; }
    [Range(0, 1000)] public int VolumenAdicionalPorcentaje { get; set; }
    [Range(0.1, 100)] public double HorasPorIncidente { get; set; } = 2.0;
}

public class ComparacionRequest
{
    [Required, MinLength(2)] public List<SimulacionRequest> Escenarios { get; set; } = new();
}

public record SimulacionResponse(string Nombre, ResultadoSimulacion Resultado, string Aviso);

public record PanelDto(int Total, Dictionary<string, int> PorEstado, Dictionary<string, int> PorNivel, IReadOnlyList<RecursoDto> CargaPorRecurso);

public record Resultado<T>(T? Valor, string? Error = null, int Status = 200)
{
    public bool Ok => Error is null;
    public static Resultado<T> Fallo(string error, int status = 400) => new(default, error, status);
}
