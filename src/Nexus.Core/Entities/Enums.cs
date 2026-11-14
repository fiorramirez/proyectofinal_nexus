namespace Nexus.Core.Entities;

public enum Rol { Administrador, Coordinador, Operador }

/// <summary>Estados permitidos de un incidente (RF008).</summary>
public enum EstadoIncidente { Nuevo, Asignado, EnProceso, Resuelto, Cerrado }

public enum NivelPrioridad { Baja, Media, Alta, Critica }
