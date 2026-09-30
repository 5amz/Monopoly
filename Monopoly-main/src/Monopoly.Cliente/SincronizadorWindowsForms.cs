using System;
using System.Windows.Forms;
using Monopoly.Nucleo;

namespace Monopoly.Cliente;
public sealed class SincronizadorWindowsForms : ISincronizadorUI
{
    private readonly Control destino;
    
// Crea el objeto.
    public SincronizadorWindowsForms(Control destino)
    {
        this.destino = destino;
    }

    
// Ejecuta Ejecutar.
    public void Ejecutar(Action accion)
    {
        if (destino.IsDisposed || destino.Disposing || !destino.IsHandleCreated)
        {
            return;
        }

        try
        {
            destino.BeginInvoke(new Action(() =>
            {
                if (!destino.IsDisposed && !destino.Disposing)
                {
                    try
                    {
                        accion();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Trace.TraceError("Fallo al actualizar la pantalla: " + ex);
                    }
                }
            }));
        }
        catch (InvalidOperationException)
        {
        }
    }
}
