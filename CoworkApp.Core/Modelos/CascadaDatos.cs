namespace CoworkApp.Core.Modelos;

public sealed record EdificioCascada(int? Id, string Nombre, string Direccion);

public sealed record PisoCascada(int? Id, string Nombre);

public sealed record OficinaCascada(string Nombre, int Capacidad, decimal Tarifa, TimeOnly HoraApertura, TimeOnly HoraCierre);
