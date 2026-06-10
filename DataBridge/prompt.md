Eres un arquitecto experto en Net Core. Codifica en inglés siempre.

Quiero implementar una Unidad de Trabajo que lea de diferentes fuentes. Principalmente sqlite, Excel, csv. La unidad de trabajo usará diferentes conectores para cada tecnología.  Esta unidad de trabajo debe ser genérica y reutilizable en diferentes proyectos.

Todas las entidades que maneje la Unidad de trabajo tendran una propiedad Id de tipo Guid.

El ejemplo debe:

- Leer productos de supermercado de un excel. 
- Leer Usuarios de una base de datos de SQLite.
- Leer/escribir compras de un archivo CSV.

El proyecto debe tener una inicialización de datos de prueba.

La capa de apis debe usar JSON API para exponer los datos. Debe permitir hacer CRUD.

Para la unidad de trabajo, es importante que las querys cumplan con el fichero de especificación de la unidad de trabajo "query_spec.json" del raíz. Debe implementar:
- Paginación
- Ordenación por campos
- proyección de campos a devolver

Los filtros pueden ser unarios o binarios, y deben permitir operaciones como igualdad, mayor que, menor que, distinto, contiene, pertenece a una lista, etc. Los filtros deben ser combinables con operadores lógicos AND y OR. Tambien deben permitir la anidación de filtros para crear consultas complejas. Tofas las operaciones pueden negarse para permitir la exclusión de ciertos resultados.

En el fichero de especificación "query_spec.json" el campo "from" Identifica la entidad sobre la que se va a realizar la consulta. 
el valor del campo select puede ser por ejemplo: "AddressPrincipal.street". Esto significa que "User" tiene una propiedad "AddressPrincipal" que a su vez tiene una propiedad "street". La proyección de campos a devolver debe soportar esta sintaxis para permitir la selección de campos anidados y no distinguir entre mayúsculas y minúsculas. 
La propiedad "AddressPrincipal" no tiene que estar en la misma base de datos que "User". Puede estar en una fuente de datos diferente, y la Unidad de Trabajo debe ser capaz de manejar esta situación y devolver los datos correctamente. La clabe siempre será el nombre de la entidad y el ID de tipo Guid.

La unidad de trabajo debe ser capaz de manejar transacciones distribuidas entre las diferentes fuentes de datos para garantizar la consistencia de los datos. Si una operación falla en una fuente de datos, la unidad de trabajo debe ser capaz de revertir las operaciones realizadas en las otras fuentes de datos para mantener la integridad de los datos.

La unidad de trabajo debe ser capaz de ejecutar consultas en paralelo a diferentes fuentes de datos.

Debe haber un módulo de auditoría que registre todas las operaciones realizadas sobre las entidades. La auditoría debe ser persistente y consultable. El formato sera:

- GUID ID de auditoría
- DateTime Fecha de la operación
- Enum Tipo de operación (Create, Read, Update, Delete)
- Guid Id de la entidad afectada
- String Nombre de la persona
- Leer/escribir una auditoría de todas las entidades que se manejencompras de un archivo CSV.

El proyecto debe tener tests end-to-end para validar la funcionalidad de la Unidad de Trabajo, la capa de APIs y el módulo de auditoría. Deben estar implementados con Gherkin y reqnroll.

tambien quiero un proyecto en angular que consuma las APIs expuestas por el proyecto de Net Core. El proyecto de Angular debe tener una interfaz de usuario para gestionar los productos, usuarios y compras. La interfaz debe permitir realizar operaciones CRUD y mostrar los datos de manera clara y organizada. Además, debe incluir funcionalidades de búsqueda, filtrado y paginación para facilitar la navegación por los datos.