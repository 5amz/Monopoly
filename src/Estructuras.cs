using System;
using System.Collections;
using System.Collections.Generic;

namespace Monopoly.Protocolo;
public sealed class NodoSimple<T>
{
    public T Valor { get; internal set; }
    public NodoSimple<T>? Siguiente { get; internal set; }

    // Guarda el valor del nuevo nodo.
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

    // Agrega un elemento al final de la estructura.
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

    // Busca y devuelve el dato solicitado.
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

    // Recorre la lista hasta encontrar un dato que cumpla la condición.
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

    // Elimina el primer dato que cumpla la condición.
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

    // Quita todos los datos de la estructura.
    public void Limpiar()
    {
        Comprobar();
        primero = ultimo = null;
        Cantidad = 0;
    }

    // Deja la estructura solo para lectura.
    public ListaSimple<T> Congelar()
    {
        congelada = true;
        return this;
    }

    // Comprueba que la lista permita cambios.
    private void Comprobar()
    {
        if (congelada)
        {
            throw new InvalidOperationException("La lista es de solo lectura.");
        }
    }

    // Recorre los elementos de la lista en orden.
    public IEnumerator<T> GetEnumerator()
    {
        for (var n = primero; n is not null; n = n.Siguiente)
        {
            yield return n.Valor;
        }
    }

    // Recorre los elementos de la lista en orden.
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

    // Guarda el valor del nuevo nodo.
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

    // Agrega un elemento al final de la estructura.
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

    // Busca y devuelve el dato solicitado.
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

    // Deja la estructura solo para lectura.
    public ListaCircularDoble<T> Congelar()
    {
        congelada = true;
        return this;
    }

    // Recorre los elementos de la lista en orden.
    public IEnumerator<T> GetEnumerator()
    {
        var n = cabeza;
        for (int i = 0; i < Cantidad; i++)
        {
            yield return n!.Valor;
            n = n.Siguiente;
        }
    }

    // Recorre los elementos de la lista en orden.
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

    // Crea los nodos de la cola y conecta el último con el primero.
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

    // Agrega un dato al final de la cola si queda espacio.
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

    // Saca el dato más antiguo de la cola.
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

    // Devuelve el primer dato sin sacarlo de la cola.
    public T Primero()
    {
        return Cantidad > 0 ? lectura.Valor : throw new InvalidOperationException("La cola está vacía.");
    }

    // Crea una copia de los datos actuales de la cola.
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

    // Quita todos los datos de la estructura.
    public void Limpiar()
    {
        while (Desencolar(out _))
        {
        }
    }
}
