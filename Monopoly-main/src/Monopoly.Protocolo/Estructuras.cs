using System;
using System.Collections;
using System.Collections.Generic;

namespace Monopoly.Protocolo;
public sealed class NodoSimple<T>
{
    public T Valor { get; internal set; }
    public NodoSimple<T>? Siguiente { get; internal set; }

    
// Crea el objeto.
    internal NodoSimple(T valor)
    {
        Valor = valor;
    }
}

public sealed class ListaSimple<T> : IEnumerable<T>
{
    private NodoSimple<T>? primero;
    private NodoSimple<T>? ultimo;
    private bool congelada;
    public int Cantidad { get; private set; }
    public bool EsSoloLectura => congelada;

    
// Ejecuta Agregar.
    public void Agregar(T valor)
    {
        Comprobar();
        var nodo = new NodoSimple<T>(valor);
        if (ultimo is null)
        {
            primero = nodo;
        }
        else
        {
            ultimo.Siguiente = nodo;
        }

        ultimo = nodo;
        Cantidad++;
    }

    
// Ejecuta Obtener.
    public T Obtener(int indice)
    {
        if (indice < 0 || indice >= Cantidad)
        {
            throw new ArgumentOutOfRangeException(nameof(indice));
        }

        var n = primero!;
        for (int i = 0; i < indice; i++)
        {
            n = n.Siguiente!;
        }

        return n.Valor;
    }

    
// Ejecuta Buscar.
    public bool Buscar(Predicate<T> predicado, out T valor)
    {
        for (var n = primero; n is not null; n = n.Siguiente)
        {
            if (predicado(n.Valor))
            {
                valor = n.Valor;
                return true;
            }
        }

        valor = default !;
        return false;
    }

    
// Ejecuta Quitar.
    public bool Quitar(Predicate<T> predicado)
    {
        Comprobar();
        NodoSimple<T>? previo = null;
        for (var n = primero; n is not null; n = n.Siguiente)
        {
            if (predicado(n.Valor))
            {
                if (previo is null)
                {
                    primero = n.Siguiente;
                }
                else
                {
                    previo.Siguiente = n.Siguiente;
                }

                if (n == ultimo)
                {
                    ultimo = previo;
                }

                Cantidad--;
                return true;
            }

            previo = n;
        }

        return false;
    }

    
// Ejecuta Limpiar.
    public void Limpiar()
    {
        Comprobar();
        primero = ultimo = null;
        Cantidad = 0;
    }

    
// Ejecuta Congelar.
    public ListaSimple<T> Congelar()
    {
        congelada = true;
        return this;
    }

    
// Ejecuta Comprobar.
    private void Comprobar()
    {
        if (congelada)
        {
            throw new InvalidOperationException("La lista es de solo lectura.");
        }
    }

    
// Ejecuta GetEnumerator.
    public IEnumerator<T> GetEnumerator()
    {
        for (var n = primero; n is not null; n = n.Siguiente)
        {
            yield return n.Valor;
        }
    }

    
// Ejecuta GetEnumerator.
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public sealed class NodoDoble<T>
{
    public T Valor { get; internal set; }
    public NodoDoble<T>? Anterior { get; internal set; }
    public NodoDoble<T>? Siguiente { get; internal set; }

    
// Crea el objeto.
    internal NodoDoble(T valor)
    {
        Valor = valor;
    }
}

public sealed class ListaCircularDoble<T> : IEnumerable<T>
{
    private NodoDoble<T>? cabeza;
    private bool congelada;
    public int Cantidad { get; private set; }

    
// Ejecuta Agregar.
    public void Agregar(T valor)
    {
        if (congelada)
        {
            throw new InvalidOperationException("El tablero es de solo lectura.");
        }

        var n = new NodoDoble<T>(valor);
        if (cabeza is null)
        {
            cabeza = n;
            n.Anterior = n.Siguiente = n;
        }
        else
        {
            n.Anterior = cabeza.Anterior;
            n.Siguiente = cabeza;
            cabeza.Anterior!.Siguiente = n;
            cabeza.Anterior = n;
        }

        Cantidad++;
    }

    
// Ejecuta Obtener.
    public T Obtener(int posicion)
    {
        if (posicion < 0 || posicion >= Cantidad)
        {
            throw new ArgumentOutOfRangeException(nameof(posicion));
        }

        var n = cabeza!;
        for (int i = 0; i < posicion; i++)
        {
            n = n.Siguiente!;
        }

        return n.Valor;
    }

    
// Ejecuta Congelar.
    public ListaCircularDoble<T> Congelar()
    {
        congelada = true;
        return this;
    }

    
// Ejecuta GetEnumerator.
    public IEnumerator<T> GetEnumerator()
    {
        var n = cabeza;
        for (int i = 0; i < Cantidad; i++)
        {
            yield return n!.Valor;
            n = n.Siguiente;
        }
    }

    
// Ejecuta GetEnumerator.
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public sealed class ColaCircular<T>
{
    private readonly int capacidad;
    private NodoSimple<T> lectura;
    private NodoSimple<T> escritura;
    public int Cantidad { get; private set; }

    
// Crea el objeto.
    public ColaCircular(int capacidad)
    {
        if (capacidad < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(capacidad));
        }

        this.capacidad = capacidad;
        lectura = escritura = new NodoSimple<T>(default !);
        var n = lectura;
        for (int i = 1; i < capacidad; i++)
        {
            n.Siguiente = new NodoSimple<T>(default !);
            n = n.Siguiente;
        }

        n.Siguiente = lectura;
    }

    
// Ejecuta Encolar.
    public bool Encolar(T valor)
    {
        if (Cantidad == capacidad)
        {
            return false;
        }

        escritura.Valor = valor;
        escritura = escritura.Siguiente!;
        Cantidad++;
        return true;
    }

    
// Ejecuta Desencolar.
    public bool Desencolar(out T valor)
    {
        if (Cantidad == 0)
        {
            valor = default !;
            return false;
        }

        valor = lectura.Valor;
        lectura.Valor = default !;
        lectura = lectura.Siguiente!;
        Cantidad--;
        return true;
    }

    
// Ejecuta Primero.
    public T Primero()
    {
        return Cantidad > 0 ? lectura.Valor : throw new InvalidOperationException("La cola está vacía.");
    }

    
// Ejecuta Instantanea.
    public ListaSimple<T> Instantanea()
    {
        var copia = new ListaSimple<T>();
        var n = lectura;
        for (int i = 0; i < Cantidad; i++)
        {
            copia.Agregar(n.Valor);
            n = n.Siguiente!;
        }

        return copia.Congelar();
    }

    
// Ejecuta Limpiar.
    public void Limpiar()
    {
        while (Desencolar(out _))
        {
        }
    }
}
