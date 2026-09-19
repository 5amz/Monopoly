# Monopoly distribuido - estado de implementación

## Integración de RFID y dados electrónicos

Se agregó la programación del dispositivo electrónico del proyecto mediante `main.py`, utilizando `mfrc522.py` para el lector RFID.

- Se integró la lectura de tarjetas **RFID MFRC522** mediante SPI.
- El UID de cada tarjeta se obtiene y se convierte a formato hexadecimal.
- Se agregó una protección de **2 segundos** para evitar lecturas repetidas de la misma tarjeta.
- Se incorporó un **botón en el GPIO 0** para lanzar los dados.
- Cada lanzamiento genera **dos dados aleatorios entre 1 y 6** y calcula su suma.
- El resultado se muestra en **dos displays de siete segmentos**, permitiendo representar valores de 2 a 12.
- El código ya deja preparado lo que se enviará al codigo del juego, el **UID** y el **resultado de los dados al servidor**.
- El dispositivo electrónico únicamente genera y muestra estos datos; no modifica directamente el saldo ni el estado oficial de la partida. Las tarjetas "rechazadas" son parte del codigo C#.

Se utilizarán estos archivos en la Raspberry Pi Pico 2 W:

```text
main.py
mfrc522.py
```

Esta actualización corresponde únicamente a la implementación del dispositivo electrónico.

## Eliminación por insolvencia

Cuando Banco rechaza un cobro o alquiler porque el saldo oficial no alcanza, `ResultadoOperacion` marca explícitamente `FueRechazadaPorFondosInsuficientes`. `ServidorJuego` usa esa información para aplicar la eliminación oficial:

```text
Banco rechaza el pago por fondos insuficientes
        ↓
ServidorJuego marca al jugador inactivo
        ↓
CoordinadorPartidaTablero libera sus propiedades
        ↓
ColaTurnos elimina al jugador de la cola circular
        ↓
Si queda un jugador activo, EstadoPartida pasa a Finalizada
```

La eliminación se aplica a cobros por compra, pagos al Banco, pérdidas por evento y alquileres. El pago rechazado no modifica ningún saldo ni crea una transacción económica; las transacciones ya realizadas se conservan en el historial.

## Coordinación de tablero, turnos y servidor

`CoordinadorPartidaTablero` es el adaptador del módulo `Int2`. Implementa los contratos públicos `IRegistroJugadoresJuego`, `IValidadorTurnos` e `IAccionesJuego`, por lo que `ServidorJuego` puede usar tablero y cola de turnos sin manipular sus nodos.

Al crear la partida, el organizador debe conectar los módulos antes de aceptar clientes:

```csharp
var servidor = new ServidorJuego();
var tablero = new ConfiguradorTablero().CrearTablero();
var colaTurnos = new ColaTurnos();
var coordinador = new CoordinadorPartidaTablero(servidor, tablero, colaTurnos);

servidor.ConfigurarModulos(coordinador, coordinador, coordinador);
```

Después de esto, cada `CONECTAR` registrado por el servidor crea una única representación `JugadorTablero` con el mismo ID. Los comandos `COMPRAR_PROPIEDAD`, `NO_COMPRAR` y `TERMINAR_TURNO` ya validan el turno mediante la cola circular y se procesan con el coordinador.

La compra y el alquiler siguen esta secuencia:

```text
Coordinador valida casilla y turno
        ↓
ServidorJuego solicita operación a Banco
        ↓
Banco cambia el saldo y registra la transacción
        ↓
Coordinador actualiza propiedad o posición en tablero
```

`TIRAR_DADOS` permanece rechazado con un mensaje claro hasta que el módulo de dado electrónico se integre. El coordinador ya expone `MoverJugador(...)` para recibir posteriormente el resultado de los dados sin que el dado modifique dinero ni posición oficial por su cuenta.

## Integración tablero-Banco

El módulo `Int2` ya no mantiene un saldo local. `JugadorTablero` conserva solo el identificador, posición, estado activo y efectos espaciales; el saldo oficial existe únicamente en `Jugador` y lo modifica `Banco` mediante `ServidorJuego`.

El flujo obligatorio para una compra es:

```text
Tablero.PuedeComprarPropiedad
        ↓
ServidorJuego.ProcesarCompraPropiedad
        ↓
Banco valida fondos, cobra y registra transacción
        ↓
Tablero.AsignarPropiedad
```

Para un alquiler:

```text
Tablero.PuedePagarAlquiler
        ↓
ServidorJuego.ProcesarPagoAlquiler
        ↓
Banco transfiere y registra transacción
```

`Tablero.MoverJugador(...)` devuelve `ResultadoMovimientoTablero`, que indica cuántas veces se pasó por inicio. El coordinador de juego debe solicitar cada premio mediante `ServidorJuego.ProcesarPremioInicio(...)`. De forma similar, `EjecutarCartaEvento(...)` devuelve `ResultadoEventoTablero`; el coordinador solicita a `ServidorJuego` la ganancia o pérdida correspondiente.

El tablero nunca debe llamar setters de saldo ni actualizar dinero por su cuenta. La demostración en `ProgramTest.cs` verifica compra, alquiler y transacciones con esta separación.

## Sincronización de clientes TCP

`ServidorTcp` ahora conserva las conexiones identificadas usando `RegistroSesionesTcp`, una estructura lineal propia. Cuando un jugador se conecta o una acción de juego se completa correctamente, el servidor envía a todas las sesiones activas una notificación adicional:

```text
EVENTO|ESTADO_ACTUALIZADO|Estado=EnCurso#Jugadores=J1;Ana;1500.00;0;True#J2;Luis;1300.00;4;True
```

Este evento no es un comando que el cliente deba enviar. Es una actualización espontánea del servidor: el cliente la interpreta y refresca su pantalla. La respuesta normal a quien solicitó la acción se conserva, por ejemplo:

```text
OK|TIRAR_DADOS|Dados lanzados.|5;3
EVENTO|ESTADO_ACTUALIZADO|...
```

Si el mismo jugador se conecta de nuevo, el servidor reemplaza y cierra su sesión TCP anterior. Esto evita que dos conexiones controlen simultáneamente a la misma identidad. Una desconexión o error de red elimina la sesión sin modificar el jugador, su saldo ni el estado de la partida.

## Historial y transacciones oficiales

El módulo administrativo ahora registra cada operación económica exitosa desde Banco. Una transacción incluye identificador, fecha y hora, número de turno, tipo, jugador origen, jugador destino, monto y descripción.

Los tipos mínimos disponibles son:

- `CompraPropiedad`
- `PagoAlquiler`
- `PagoAlBanco`
- `PagoEntreJugadores`
- `GananciaPorEvento`
- `PerdidaPorEvento`
- `PremioPorPasarInicio`

`HistorialTransacciones` utiliza una lista doblemente enlazada propia. Permite recorrer desde la operación más antigua a la más reciente, en sentido inverso, buscar por jugador o tipo y generar un reporte de texto. Sus nodos no son públicos.

Ejemplo de operación desde la lógica oficial:

```csharp
ResultadoOperacion resultado = servidor.ProcesarCobro(
    "J1",
    200m,
    "Compra de Avenida Central",
    TipoTransaccion.CompraPropiedad,
    numeroTurno: 3);
```

El servidor consulta únicamente las transacciones del jugador conectado al recibir `CONSULTAR_TRANSACCIONES`. Para generar el entregable TXT desde una acción administrativa del servidor:

```csharp
servidor.ExportarTransacciones("transacciones-partida.txt");
```

Un pago insuficiente, un tipo de transacción incompatible o un jugador inexistente no modifican saldo ni agregan una transacción.

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
- TCP y el protocolo de mensajes se implementan en `ServidorTcp`, sin incluir la lógica de tablero ni turnos.

## Servidor TCP y protocolo de mensajes

El módulo administrativo ahora incluye `ServidorTcp`. TCP es una conexión de red confiable: un cliente abre una conexión con el servidor, envía una línea de texto y espera una respuesta. En este proyecto el cliente solicita; `ServidorJuego` valida con el estado oficial; y el cliente solo muestra el resultado.

### Inicio del servidor

```csharp
var juego = new ServidorJuego();
using var servidorTcp = new ServidorTcp(juego, 5000);
await servidorTcp.IniciarAsync();
```

El servidor escucha en el puerto `5000` por defecto y acepta más de un cliente. Cada mensaje se procesa de uno en uno para que el saldo y el estado oficial no se modifiquen al mismo tiempo desde dos conexiones. `DetenerAsync()` finaliza la escucha de forma controlada.

### Formato de solicitudes

Cada solicitud ocupa una única línea y usa `|` como separador. El saldo se escribe con punto decimal, por ejemplo `1500.00`.

```text
CONECTAR|id|nombre|saldoInicial
TIRAR_DADOS
COMPRAR_PROPIEDAD
NO_COMPRAR
TERMINAR_TURNO
CONSULTAR_ESTADO
CONSULTAR_TRANSACCIONES
```

Primero debe enviarse `CONECTAR`. En una conexión ya identificada no se puede cambiar de jugador. `CONECTAR` registra al jugador mientras la partida está esperando jugadores o permite reconectar a un jugador existente si el ID y nombre coinciden.

### Formato de respuestas

```text
OK|COMANDO|mensaje|datosOpcionales
ERROR|CODIGO_ERROR|mensaje
```

Ejemplos:

```text
OK|CONECTAR|Jugador registrado.|J1
OK|CONSULTAR_ESTADO|Estado consultado.|J1;Ana;1500.00;0;True;EsperandoJugadores
ERROR|NO_IDENTIFICADO|Debe enviar CONECTAR antes de solicitar acciones.
ERROR|FORMATO_INVALIDO|Use CONECTAR|id|nombre|saldoInicial.
```

### Validaciones realizadas por el servidor

- Rechaza líneas vacías, comandos no reconocidos, parámetros sobrantes y mensajes de más de 512 caracteres.
- Exige identificación antes de cualquier acción.
- Confirma que el jugador exista y esté activo.
- Solo permite acciones de juego cuando la partida está en curso.
- Delega la validación del turno a `IValidadorTurnos`.
- Delega tirar dados, compra, rechazo de compra y terminar turno a `IAccionesJuego`.
- Delega la consulta de historial a `IConsultaTransacciones`.

Los últimos tres contratos son interfaces públicas para que los módulos responsables se conecten sin que el servidor manipule sus nodos o estructuras internas. Mientras no estén integrados, el servidor responde `ERROR|MODULO_NO_INTEGRADO|...`; no inventa tablero, cola de turnos, propiedades ni transacciones.

### Archivos agregados

- `ComandoProtocolo.cs`: enumera los comandos admitidos.
- `SolicitudProtocolo.cs` y `RespuestaProtocolo.cs`: representan mensajes antes y después de procesarlos.
- `AnalizadorProtocolo.cs`: valida la sintaxis de cada línea.
- `ServidorTcp.cs`: escucha conexiones, lee solicitudes y escribe respuestas.
- `ContratosModulosJuego.cs`: define los puntos públicos de integración con turnos, tablero/dados y transacciones.
- Se usa `Jugador?` y `decimal?` cuando una consulta puede no encontrar resultados.
- No se inventa todavía una estructura de propiedades: corresponde al módulo del tablero.
- Historial, exportación TXT y protocolo TCP completo deben integrarse en las siguientes partes, utilizando estos contratos sin acceder a implementaciones internas.
