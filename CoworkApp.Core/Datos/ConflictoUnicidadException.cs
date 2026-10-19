namespace CoworkApp.Core.Datos;

public sealed class ConflictoUnicidadException : Exception
{
    public ConflictoUnicidadException(Exception? excepcionInterna = null)
        : base("El registro ya existe.", excepcionInterna)
    {
    }
}
