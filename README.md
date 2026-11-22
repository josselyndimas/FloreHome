# 🌸 FloreHome

**CRUD de administración de una floristería** hecho con C# (Windows Forms, .NET 9) y MySQL.

Gestiona productos, clientes, usuarios y facturas. Lo especial: al subir la foto de una flor, el sistema la convierte automáticamente en un dibujo **pixel art** de tamaño fijo, que luego se ve como un menú de tarjetas al facturar.

## Funciones

- **Inicio de sesión con roles** (Administrador / Empleado). No hay registro público: el administrador crea los usuarios, y quien no es administrador ni ve el menú *Usuarios*.
- **Productos** con código automático (TUL-0001, según el nombre), categorías y foto. La foto se puede dejar normal o convertir a **pixel art** (con opción de quitar el fondo); siempre queda en un cuadro de 256×256 px.
- **Clientes** con validaciones: teléfono de El Salvador (8 números, el guion se coloca solo: `1234-5678`), nombre sin números y correo opcional válido. Los campos con error muestran un aviso rojo.
- **Facturación** tipo menú (consumidor final o crédito fiscal): tarjetas de productos, buscador de clientes, carrito con cantidad editable, método de pago, control de existencias y descuento automático del inventario. Descuento de 10 % para clientes frecuentes (cada 6 compras de más de $25).
- **Historial de facturas** con filtro por fechas, detalle y comprobante en PDF. Las facturas **no se borran, se anulan** (queda el registro, el motivo y el stock regresa). Cada empleado ve solo las que él hizo.
- **Exportar a Excel y a PDF** cualquier lista, con la paleta de la app.
- Contraseñas con sal aleatoria y PBKDF2.

## Cómo ejecutarlo

1. Instala **MySQL 8** y el **SDK de .NET 9**.
2. En MySQL Workbench ejecuta `Database/florehome.sql`.
3. Abre `conexion.txt` y pon tu usuario y contraseña de MySQL (este archivo no se sube a GitHub).
4. Abre `FloreHome.sln` en Visual Studio y presiona **F5**.
5. La primera vez te pedirá crear el usuario **administrador**.
6. Si ya tenías una base de datos de una versión anterior, no necesitas hacer nada: la app agrega sola las columnas nuevas al abrir.

> El proyecto no genera un `FloreHome.exe` (`UseAppHost=false`): se ejecuta con `dotnet`, así Windows no bloquea un ejecutable sin firma.

## Roles

| | Administrador | Empleado |
|---|:---:|:---:|
| Nueva factura / ver facturas | ✔ | ✔ |
| Clientes: crear y editar | ✔ | ✔ |
| Clientes: eliminar | ✔ | |
| Productos: ver y exportar | ✔ | ✔ |
| Productos: crear, editar, eliminar | ✔ | |
| Ver facturas | todas | solo las suyas |
| Anular facturas | ✔ | |
| Usuarios | ✔ | |

## Estructura

```
Core/       conexión, acceso a datos, seguridad, validaciones, reglas de IVA y descuento
Imaging/    conversión de foto a pixel art
Services/   exportar a Excel (ClosedXML) y PDF (generador propio, sin dependencias)
Ui/         paleta "Mint chocolate", estilos y controles comunes
Forms/      pantallas y ventanas de captura
Database/   script SQL
```

## Paleta

Mint chocolate: `#684F48` · `#D6E9ED` · `#CCD6D6` · `#F9E7C7`

## Descripción sugerida para GitHub

> CRUD de administración de una floristería en C# y MySQL: usuarios con roles, productos con imágenes convertidas a pixel art, clientes, facturación y exportación a Excel/PDF.

## IVA y tipos de factura

Los precios de los productos ya incluyen el IVA (13 %): si dice $40, el cliente paga $40.

- **Consumidor final:** la factura muestra los productos y el total, sin mencionar el IVA.
- **Crédito fiscal:** muestra los productos sin IVA, el monto del IVA y el total (la suma de los dos). Requiere un cliente registrado.

Las reglas están en `Core/Fiscal.cs`.
