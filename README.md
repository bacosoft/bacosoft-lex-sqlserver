# bacosoft-lex-sqlserver
Se trata de unos procedimientos almacenados para utilizar en SQLServer que permiten consultar e importar datos utilizando la web API de Bacosoft LEX.

## Build del proyecto
Utilizar Visual Studio 2022 o superior. Configurar en **Release** y realizar un **Rebuild solution**. En la carpeta `bacosoft-lex-sqlserver\bin\Release` interesa el fichero `bacosoft_lex_sqlserver.dll`.

## Instrucciones de instalación

Estos procedimientos almacenados necesitan el permiso `EXTERNAL_ACCESS` de manera que es necesario habilitar la propiedad `TRUSTWORTHY` en la base de datos donde se vayan a instalar y utilizar:
```
ALTER DATABASE [NombreDeLaBaseDeDatos] SET TRUSTWORTHY ON
go
```

Luego se crea el assembly y se declaran sus procedimientos almacenados:
```
CREATE ASSEMBLY [bacosoft_lex_sqlserver]
    AUTHORIZATION [dbo]
    FROM 'c:\algunaCarpeta\bacosoft_lex_sqlserver.dll'
    WITH PERMISSION_SET = EXTERNAL_ACCESS;
GO

CREATE PROCEDURE [dbo].[BacolexQuery]
@baseUrl NVARCHAR (MAX) NULL, @tenant NVARCHAR (MAX) NULL, @userName NVARCHAR (MAX) NULL, @password NVARCHAR (MAX) NULL, @resource NVARCHAR (MAX) NULL, @parameters NVARCHAR (MAX) NULL, @error INT NULL OUTPUT, @response NVARCHAR (MAX) NULL OUTPUT
AS EXTERNAL NAME [bacosoft_lex_sqlserver].[StoredProcedures].[BacolexQuery]
GO

CREATE PROCEDURE [dbo].[BacolexImport]
@baseUrl NVARCHAR (MAX) NULL, @tenant NVARCHAR (MAX) NULL, @userName NVARCHAR (MAX) NULL, @password NVARCHAR (MAX) NULL, @idEmpresa BIGINT NULL, @idEstablecimiento BIGINT NULL, @data NVARCHAR (MAX) NULL, @validate BIT NULL, @error INT NULL OUTPUT, @response NVARCHAR (MAX) NULL OUTPUT
AS EXTERNAL NAME [bacosoft_lex_sqlserver].[StoredProcedures].[BacolexImport]
GO
```

Como alternativa, en lugar de copiar al servidor SQLServer el fichero `bacosoft_lex_sqlserver.dll` es posible hacer el `CREATE ASSEMBLY` tal como se muestra dentro del fichero `bacosoft-lex-sqlserver_Create.sql` que se genera en la carpeta `bacosoft-lex-sqlserver\bin\Release`. En ese caso el contenido del fichero queda codificado en la propia sentencia SQL. En ese caso la sentencia es así (el texto del `FROM` no aparece completo aquí):
```
CREATE ASSEMBLY [bacosoft_lex_sqlserver]
    AUTHORIZATION [dbo]
    FROM 0x4D5A90000300000004000000FFFF0000B8...
    WITH PERMISSION_SET = EXTERNAL_ACCESS;
```

## Ejemplo de uso

```
DECLARE @server varchar(60) = 'https://test.bacosoft.com';
DECLARE @tenant varchar(30) = 'demos';
DECLARE @userName varchar(30) = 'test';
DECLARE @password varchar(30) = 'laContraseña';

DECLARE @respuesta varchar(max);
DECLARE @parametros varchar(max);
DECLARE @datos varchar(max);
DECLARE @error int;

-- Consultar países definidos
SET @parametros = '<FindParameters><projection>detail</projection></FindParameters>';
EXECUTE BacolexQuery @server, @tenant, @userName, @password, '/lex/api/pais/query', @parametros, @error OUTPUT, @respuesta OUTPUT;
PRINT 'Resp:' + cast(@error as varchar(6)) + ':' + @respuesta;

-- Agregar unas provincias y poblaciones ficticias asociadas al país Francia
SET @datos = '<registros><entidad _class="Provincia" nombre="Francia" pais="[codigoIso=FR]"/><entidad _class="Poblacion" nombre="Francia" provincia="Francia"/></registros>'
EXECUTE BacolexImport @server, @tenant, @userName, @password, null, null, @datos, false, @error OUTPUT, @respuesta OUTPUT;
PRINT 'Resp:' + cast(@error as varchar(6)) + ':' + @respuesta;
```
