Se implementaron:

- `Jugador`: identidad, saldo y datos mínimos de integración.
- `Banco`: registro, consulta, validación y modificación centralizada de dinero.
- `ServidorJuego`: estado general de la partida y puntos de entrada para futuras solicitudes TCP.
- `RegistroJugadores`: estructura lineal propia para almacenar jugadores.
- `ResultadoOperacion`: respuesta uniforme para operaciones exitosas o rechazadas.
- `EstadoPartida`: estados `EsperandoJugadores`, `EnCurso` y `Finalizada`.

No se implementaron en este commit el tablero, las casillas, las propiedades, las cartas, los turnos, los dados, RFID, la interfaz ni los sockets TCP.

## Regla principal de autoridad

El cliente no debe modificar `Saldo`, `PosicionActual`, `EstaActivo` ni ninguna otra propiedad oficial. `Jugador.Saldo` tiene un setter privado y los métodos internos de actualización no deben invocarse desde un cliente.

Las operaciones económicas deben pasar por el servidor y el Banco:

```csharp
var resultado = servidor.ProcesarCobro("J1", 200, "Alquiler");
if (!resultado.FueExitosa)
    Console.WriteLine(resultado.Mensaje);
```

El cliente recibe la respuesta y actualiza su presentación, pero no cambia el objeto oficial por su cuenta.

## Estructura del proyecto

```text
src/Monopoly.Administracion/
├── Jugador.cs
├── Banco.cs
├── RegistroJugadores.cs
├── ResultadoOperacion.cs
├── ServidorJuego.cs
├── EstadoPartida.cs
└── Monopoly.Administracion.csproj
```

`RegistroJugadores` es `internal`: solo Banco conoce sus nodos y su recorrido. Ningún otro módulo debe depender de esa implementación. La restricción de estructuras lineales se mantiene porque no se usan `List`, `LinkedList`, `Queue` ni equivalentes.

## Contratos públicos disponibles

### `Jugador`

Propiedades de solo lectura para consumidores:

- `Id`: identificador único.
- `Nombre`: nombre visible.
- `Saldo`: dinero oficial.
- `PosicionActual`: posición que posteriormente actualizará el flujo autorizado de tablero/servidor.
- `EstaActivo`: estado del jugador.
- `CantidadPropiedades`: dato mínimo de integración; la colección real de propiedades pertenece al integrante 2.

El constructor valida ID, nombre y saldo inicial no negativo.

### `Banco`

Métodos públicos:

- `RegistrarJugador(id, nombre, saldoInicial)`: registra hasta cuatro jugadores por defecto y rechaza IDs repetidos.
- `ConsultarJugador(id)`: devuelve `Jugador?`; puede devolver `null`.
- `ConsultarSaldo(id)`: devuelve `decimal?`; puede devolver `null`.
- `Abonar(idJugador, monto, motivo)`: suma dinero después de validar monto y jugador.
- `Cobrar(idJugador, monto, motivo)`: descuenta solo si hay fondos suficientes.
- `Transferir(idOrigen, idDestino, monto, motivo)`: mueve dinero entre dos jugadores distintos y validados.

Los montos deben ser mayores que cero. Las operaciones no exitosas no deben cambiar ningún saldo.

### `ResultadoOperacion`

Contiene:

- `FueExitosa`.
- `Mensaje`.
- `SaldoAnterior`.
- `SaldoActual`.

El futuro módulo de transacciones debe registrar cada operación exitosa usando los datos de esta respuesta y el contexto de la partida. Banco seguirá siendo responsable de autorizar el cambio; no se debe duplicar esa lógica en el cliente.

### `ServidorJuego`

Propiedades:

- `Banco`: referencia al Banco oficial.
- `Estado`: estado actual de la partida.

Métodos:

- `RegistrarJugador(...)`: permite conexiones/altas solo mientras el estado sea `EsperandoJugadores`.
- `ProcesarCobro(...)` y `ProcesarAbono(...)`: entradas del servidor para dinero.
- `MarcarPartidaIniciada()`: pasa a `EnCurso`; requiere al menos un jugador.
- `MarcarPartidaFinalizada()`: pasa a `Finalizada`.

El manejador TCP futuro debe traducir mensajes del protocolo a estos métodos. No debe entregar al cliente referencias que permitan mutar el estado.

## Integración por integrante

### Integrante 2: tablero, casillas y propiedades

Debe conservar sus estructuras propias y exponer métodos públicos de alto nivel, por ejemplo `MoverJugador(...)`, `ConsultarCasilla(...)` y `ComprarPropiedad(...)`. El servidor coordina esas llamadas; no debe acceder a nodos del tablero ni a la colección interna de propiedades.

### Integrante 3: turnos, dados y movimiento

Debe exponer operaciones como validar jugador actual, lanzar dados una vez por turno y terminar turno. `ServidorJuego` será el punto que combine esa validación con la operación del tablero. La cola circular permanece dentro de su módulo.

### Integrante 4: cliente, interfaz, RFID y dado electrónico

El RFID solo identifica al jugador. El dispositivo envía el ID al servidor; nunca administra el saldo. La interfaz muestra respuestas del servidor y no aplica cobros, abonos, compras ni movimientos localmente.

## Ejecución y verificación

Desde la raíz del repositorio:

```powershell
dotnet build src\Monopoly.Administracion\Monopoly.Administracion.csproj
```

La compilación esperada es exitosa sin advertencias ni errores. Las carpetas `bin` y `obj` son generadas por .NET y no forman parte del código fuente.

## Decisiones y límites

- Se usa `decimal` para dinero.
- Se usa `Jugador?` y `decimal?` cuando una consulta puede no encontrar resultados.
- No se inventa todavía una estructura de propiedades: corresponde al módulo del tablero.
- Historial, exportación TXT y protocolo TCP completo deben integrarse en las siguientes partes, utilizando estos contratos sin acceder a implementaciones internas.
