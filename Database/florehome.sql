-- FloreHome · script de base de datos (MySQL 8+)
-- Ejecútalo completo en MySQL Workbench antes de abrir la aplicación.

CREATE DATABASE IF NOT EXISTS florehome
    CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE florehome;

CREATE TABLE IF NOT EXISTS Usuario (
    IdUsuario      INT AUTO_INCREMENT PRIMARY KEY,
    Nombre         VARCHAR(50)  NOT NULL UNIQUE,
    NombreCompleto VARCHAR(100) NOT NULL,
    PasswordHash   VARCHAR(256) NOT NULL,
    PasswordSalt   VARCHAR(128) NOT NULL,
    Rol            ENUM('Administrador','Empleado') NOT NULL DEFAULT 'Empleado',
    FechaRegistro  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS Producto (
    Idproducto  INT AUTO_INCREMENT PRIMARY KEY,
    Codigo      VARCHAR(20)   NULL,          -- lo genera la app con las letras del nombre (TUL-0001)
    Descripcion VARCHAR(150)  NOT NULL,
    Categoria   VARCHAR(50)   NOT NULL DEFAULT 'Flores',
    Cantidad    INT           NOT NULL DEFAULT 0,
    Precio      DECIMAL(10,2) NOT NULL,
    Imagen      MEDIUMBLOB    NULL,          -- imagen 256x256 (PNG pixel art o JPEG si es foto normal)
    CONSTRAINT ck_producto_cantidad CHECK (Cantidad >= 0),
    CONSTRAINT ck_producto_precio   CHECK (Precio > 0),
    UNIQUE KEY ux_producto_codigo (Codigo),
    INDEX ix_producto_descripcion (Descripcion)
);

CREATE TABLE IF NOT EXISTS Cliente (
    Idcliente INT AUTO_INCREMENT PRIMARY KEY,
    Nombre    VARCHAR(100) NOT NULL,
    Telefono  VARCHAR(9)   NOT NULL,         -- formato 1234-5678
    Email     VARCHAR(100) NULL,
    Direccion VARCHAR(200) NULL
);

CREATE TABLE IF NOT EXISTS Factura (
    Idfactura     INT AUTO_INCREMENT PRIMARY KEY,
    IdCliente     INT           NULL,
    ClienteNombre VARCHAR(100)  NOT NULL,    -- nombre al momento de la venta
    IdUsuario     INT           NULL,        -- quién hizo la venta
    Fecha         DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    TipoDocumento VARCHAR(20)   NOT NULL DEFAULT 'Consumidor final',   -- o 'Crédito fiscal'
    MetodoPago    VARCHAR(20)   NOT NULL DEFAULT 'Efectivo',
    Subtotal      DECIMAL(10,2) NOT NULL,
    Descuento     DECIMAL(10,2) NOT NULL DEFAULT 0,
    IVA           DECIMAL(10,2) NOT NULL,
    Total         DECIMAL(10,2) NOT NULL,
    Estado        VARCHAR(10)   NOT NULL DEFAULT 'Emitida',   -- Emitida / Anulada (las facturas no se borran)
    FechaAnulacion  DATETIME     NULL,
    MotivoAnulacion VARCHAR(200) NULL,
    AnuladaPor      INT          NULL,
    CONSTRAINT fk_factura_cliente FOREIGN KEY (IdCliente) REFERENCES Cliente(Idcliente) ON DELETE SET NULL,
    CONSTRAINT fk_factura_usuario FOREIGN KEY (IdUsuario) REFERENCES Usuario(IdUsuario) ON DELETE SET NULL,
    INDEX ix_factura_fecha (Fecha)
);

CREATE TABLE IF NOT EXISTS Detallefactura (
    Iddetalle  INT AUTO_INCREMENT PRIMARY KEY,
    idfactura  INT           NOT NULL,
    idproducto INT           NOT NULL,
    cantidad   INT           NOT NULL,
    Precio     DECIMAL(10,2) NOT NULL,
    Total      DECIMAL(10,2) NOT NULL,
    CONSTRAINT fk_detalle_factura  FOREIGN KEY (idfactura)  REFERENCES Factura(Idfactura) ON DELETE CASCADE,
    CONSTRAINT fk_detalle_producto FOREIGN KEY (idproducto) REFERENCES Producto(Idproducto)
);
