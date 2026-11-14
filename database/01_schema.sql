/* NEXUS - Esquema relacional propuesto (SQL Server). Corresponde al diccionario de datos del informe.
   Nota: en desarrollo la API crea este esquema con EF Core (EnsureCreated); este script sirve para
   revisarlo en SQL Server Management Studio o desplegarlo manualmente. */
IF DB_ID(N'NexusDb') IS NULL CREATE DATABASE NexusDb;
GO
USE NexusDb;
GO

CREATE TABLE USUARIO (
    id_usuario      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_USUARIO PRIMARY KEY,
    nombre          NVARCHAR(120) NOT NULL,
    correo          NVARCHAR(180) NOT NULL CONSTRAINT UQ_USUARIO_correo UNIQUE,
    hash_contrasena NVARCHAR(255) NOT NULL,
    rol             VARCHAR(20)   NOT NULL CONSTRAINT CK_USUARIO_rol CHECK (rol IN ('Administrador','Coordinador','Operador')),
    activo          BIT           NOT NULL CONSTRAINT DF_USUARIO_activo DEFAULT 1,
    fecha_creacion  DATETIME2     NOT NULL CONSTRAINT DF_USUARIO_fecha DEFAULT SYSUTCDATETIME()
);

CREATE TABLE CATEGORIA (
    id_categoria INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CATEGORIA PRIMARY KEY,
    nombre       NVARCHAR(80)  NOT NULL CONSTRAINT UQ_CATEGORIA_nombre UNIQUE,
    descripcion  NVARCHAR(300) NULL,
    activa       BIT NOT NULL CONSTRAINT DF_CATEGORIA_activa DEFAULT 1
);

CREATE TABLE INCIDENTE (
    id_incidente        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_INCIDENTE PRIMARY KEY,
    titulo              NVARCHAR(150) NOT NULL,
    descripcion         NVARCHAR(MAX) NOT NULL,
    id_categoria        INT NOT NULL CONSTRAINT FK_INCIDENTE_CATEGORIA REFERENCES CATEGORIA(id_categoria),
    id_reportante       INT NOT NULL CONSTRAINT FK_INCIDENTE_USUARIO   REFERENCES USUARIO(id_usuario),
    area_afectada       NVARCHAR(120) NOT NULL,
    impacto             TINYINT NOT NULL CONSTRAINT CK_INCIDENTE_impacto  CHECK (impacto  BETWEEN 1 AND 5),
    urgencia            TINYINT NOT NULL CONSTRAINT CK_INCIDENTE_urgencia CHECK (urgencia BETWEEN 1 AND 5),
    prioridad_puntaje   DECIMAL(6,2) NULL,
    estado              VARCHAR(20) NOT NULL CONSTRAINT CK_INCIDENTE_estado
                        CHECK (estado IN ('Nuevo','Asignado','EnProceso','Resuelto','Cerrado')),
    fecha_creacion      DATETIME2 NOT NULL CONSTRAINT DF_INCIDENTE_creacion DEFAULT SYSUTCDATETIME(),
    fecha_actualizacion DATETIME2 NOT NULL CONSTRAINT DF_INCIDENTE_actualizacion DEFAULT SYSUTCDATETIME(),
    fecha_resolucion    DATETIME2 NULL
);

CREATE TABLE RECURSO (
    id_recurso       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RECURSO PRIMARY KEY,
    nombre           NVARCHAR(120) NOT NULL,
    capacidades      NVARCHAR(500) NOT NULL,
    disponible       BIT NOT NULL CONSTRAINT DF_RECURSO_disponible DEFAULT 1,
    capacidad_maxima INT NOT NULL CONSTRAINT CK_RECURSO_capacidad CHECK (capacidad_maxima > 0),
    activo           BIT NOT NULL CONSTRAINT DF_RECURSO_activo DEFAULT 1
);

CREATE TABLE ASIGNACION (
    id_asignacion    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ASIGNACION PRIMARY KEY,
    id_incidente     INT NOT NULL CONSTRAINT FK_ASIGNACION_INCIDENTE REFERENCES INCIDENTE(id_incidente),
    id_recurso       INT NOT NULL CONSTRAINT FK_ASIGNACION_RECURSO   REFERENCES RECURSO(id_recurso),
    id_asignador     INT NOT NULL CONSTRAINT FK_ASIGNACION_USUARIO   REFERENCES USUARIO(id_usuario),
    fecha_asignacion DATETIME2 NOT NULL CONSTRAINT DF_ASIGNACION_fecha DEFAULT SYSUTCDATETIME(),
    fecha_fin        DATETIME2 NULL,
    observacion      NVARCHAR(500) NULL
);

CREATE TABLE REGLA_PRIORIDAD (
    id_regla         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_REGLA PRIMARY KEY,
    nombre           NVARCHAR(100) NOT NULL,
    peso_impacto     DECIMAL(5,2) NOT NULL,
    peso_urgencia    DECIMAL(5,2) NOT NULL,
    peso_antiguedad  DECIMAL(5,2) NOT NULL,
    peso_recurrencia DECIMAL(5,2) NOT NULL,
    activa           BIT NOT NULL CONSTRAINT DF_REGLA_activa DEFAULT 0,
    fecha_creacion   DATETIME2 NOT NULL CONSTRAINT DF_REGLA_fecha DEFAULT SYSUTCDATETIME()
);

CREATE TABLE ESCENARIO (
    id_escenario    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ESCENARIO PRIMARY KEY,
    id_creador      INT NOT NULL CONSTRAINT FK_ESCENARIO_USUARIO REFERENCES USUARIO(id_usuario),
    nombre          NVARCHAR(120) NOT NULL,
    supuestos_json  NVARCHAR(MAX) NOT NULL,
    resultados_json NVARCHAR(MAX) NULL,
    fecha_creacion  DATETIME2 NOT NULL CONSTRAINT DF_ESCENARIO_fecha DEFAULT SYSUTCDATETIME(),
    fecha_ejecucion DATETIME2 NULL
);

CREATE TABLE HISTORIAL_INCIDENTE (
    id_historial    BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HISTORIAL PRIMARY KEY,
    id_incidente    INT NOT NULL CONSTRAINT FK_HISTORIAL_INCIDENTE REFERENCES INCIDENTE(id_incidente),
    id_usuario      INT NOT NULL CONSTRAINT FK_HISTORIAL_USUARIO   REFERENCES USUARIO(id_usuario),
    estado_anterior VARCHAR(20) NULL,
    estado_nuevo    VARCHAR(20) NULL,
    accion          NVARCHAR(120) NOT NULL,
    observacion     NVARCHAR(600) NULL,
    fecha_evento    DATETIME2 NOT NULL CONSTRAINT DF_HISTORIAL_fecha DEFAULT SYSUTCDATETIME()
);
GO

CREATE INDEX IX_INCIDENTE_estado     ON INCIDENTE(estado);
CREATE INDEX IX_ASIGNACION_activas   ON ASIGNACION(id_recurso) WHERE fecha_fin IS NULL;
CREATE INDEX IX_HISTORIAL_incidente  ON HISTORIAL_INCIDENTE(id_incidente, fecha_evento);
GO
