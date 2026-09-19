from machine import Pin
from mfrc522 import MFRC522
from time import sleep_ms, ticks_ms
import random


D1 = {
    'a': Pin(13, Pin.OUT),
    'b': Pin(12, Pin.OUT),
    'c': Pin(18, Pin.OUT),
    'd': Pin(17, Pin.OUT),
    'e': Pin(16, Pin.OUT),
    'f': Pin(14, Pin.OUT),
    'g': Pin(15, Pin.OUT)
}

D2 = {
    'a': Pin(9, Pin.OUT),
    'b': Pin(8, Pin.OUT),
    'c': Pin(22, Pin.OUT),
    'd': Pin(21, Pin.OUT),
    'e': Pin(20, Pin.OUT),
    'f': Pin(10, Pin.OUT),
    'g': Pin(11, Pin.OUT)
}

DIGITOS = {
    0: "abcdef",
    1: "bc",
    2: "abdeg",
    3: "abcdg",
    4: "bcfg",
    5: "acdfg",
    6: "acdefg",
    7: "abc",
    8: "abcdefg",
    9: "abcdfg"
}

def apagar(display):
    for pin in display.values():
        pin.value(0)

def mostrar_digito(display, numero):
    apagar(display)

    for seg in DIGITOS[numero]:
        display[seg].value(1)

def mostrar_numero(numero):
    decena = numero // 10
    unidad = numero % 10

    mostrar_digito(D1, decena)
    mostrar_digito(D2, unidad)


boton = Pin(0, Pin.IN, Pin.PULL_UP)


rdr = MFRC522(
    sck=2,
    mosi=3,
    miso=4,
    rst=5,
    cs=1
)

ultimo_uid = None
ultimo_tiempo = 0

def uid_a_texto(uid):
    return "".join("{:02X}".format(x) for x in uid)

def leer_tarjeta():
    global ultimo_uid
    global ultimo_tiempo

    stat, tag_type = rdr.request(rdr.REQIDL)

    if stat != rdr.OK:
        return None

    stat, uid = rdr.anticoll(rdr.PICC_ANTICOLL1)

    if stat != rdr.OK:
        return None

    uid_txt = uid_a_texto(uid)

    ahora = ticks_ms()

    if uid_txt == ultimo_uid and ahora - ultimo_tiempo < 2000:
        return None

    ultimo_uid = uid_txt
    ultimo_tiempo = ahora

    return uid_txt


def tirar_dados():
    return random.randint(1, 6) + random.randint(1, 6)


print("Sistema iniciado")
mostrar_numero(0)

boton_anterior = 1

while True:

    uid = leer_tarjeta()

    if uid:
        print("RFID:", uid)

        # enviar UID al servidor

    estado = boton.value()

    if boton_anterior == 1 and estado == 0:

        resultado = tirar_dados()

        print("DADO:", resultado)

        mostrar_numero(resultado)

        # enviar resultado al servidor

    boton_anterior = estado

    sleep_ms(50)