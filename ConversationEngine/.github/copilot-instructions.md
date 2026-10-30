- @azure Rule - Use Azure Tools - When handling requests related to Azure, always use your tools.
- @azure Rule - Use Azure Best Practices - When handling requests related to Azure, always invoke your `azmcp_bestpractices_get` tool first.
- @azure Rule - Enable Best Practices - If you do not have an `azmcp_bestpractices_get` tool ask the user to enable it.

---
# Reglas para el agente de Copilot

## Reglas para documentar el codigo:
	* Para mayor difusión, redactar en inglés.
	* Añadir funciones nuevas en los archivos README dentro del proyecto.
    * Evitar (a menos que se indique):
        - Crear archivos .md adicionales.
        - Documentar cambios en changelog o bitácora.
        - Documentar correcciones de bugs.
		- Si se realizo cambios en comportamiento existente, quitar en la documentacion el comportamiento anterior y sustituirlo por el actual.
    * Usar emoticonos y símbolos si es necesario (configurar codificación en .md).
	* Al agregar comentarios evitar que tengan mas de 1 linea
	* No usar espacios en blanco entre metodos
	
---

## Como redactar los .md:
	* El objetivo de la documentacion es que el usuario o agente de IA pueda informarse de las funciones principales de cada archivo antes de empezar a trabajar
	* Debe tener un archivo read_me.md para el repositorio en general y cada proyecto (los archivos agrupados por el .csproj) tendra el suyo empezando por "read_me_" seguido del nombre del proyecto y terminando con la extension ".md", estos archivos deberan estar juntos con el archivo read_me.md
	* Estructura del archivo read_me
		- Al inicio colocar un resumen breve de lo que hace
		- Enlistar los proyectos en los que estan divididos y una explicacion de su funcionamiento
		- Hacer una guia de usuario para poder utilizar las interfaces o ventanas o librerias del proyecto
		- Enlistar para que puede utilizarse
	* Estructura de los archivos read_me por proyecto
		- Al inicio colocar un resumen breve de lo que hace 
		- Enlistar los archivos que conforman el proyecto en forma de subsecciones donde se resuma que hace y las funciones que desempeña, en caso del read_me.md general un resumen de cada proyecto
		- Cuando se haga un nuevo archivo colocar su respectiva subseccion 
		- Los cambios o adiciones nuevas que se hagan a un archivo existente agregarlos en su respectiva subseccion
		- Si se realizo cambios de logica o funcionamiento del proyecto, quitar el comportamiento anterior y sustituirlo por el actual.
		- Colocar una conclusion sobre las utilidades del proyecto

---

## Reglas para preparar el area de trabajo
	### Antes de proyectos desde cero
        - Analiza si el código puede reutilizarse.
        - Evitar que los archivos tengan mas de 800 lineas de codigo si se puede repartir en clases modulares que se encarguen de tareas especificas pero que podrían ocupar muchas lineas de codigo.
		- Si haces código que se encarga de manejar segmentos modulares (como interfaces, paneles o ventanas) manejaras una clase principal que funcione como la "base" que servira para instanciar, manejar y mantener en comunicacion a las demas clases modulares.
		- El objetivo es ahorrar tiempo, prevenir tener archivos pesados, codigo duplicado, logica redundante y volver a analizar el codigo para hacer la segmentacion en una etapa avanzada del proyecto.
    ### Ejemplo de tareas especificas
        - Metodos estaticos que reciban parametros y retorne valores sin modificar otros valores pueden entrar en un archivo separado tipo "Utilities"
        - Variables que solo se van a designar un valor una vez como variables const o propiedades para estilizar la interfaz        
        - Paneles o botones modulares que consuman demasiadas lineas de codigo, cuya interaccion con componentes fuera de su entorno sea minima y la clase "base" sea quien tenga que acceder a sus propiedades
---

## Reglas para redactar y ordenar el codigo:
    ### Variables, métodos, comentarios
		- En ingles.
        - Solo caracteres alfanuméricos en nombres.
        - Sin caracteres especiales, incluyendo ñ, Ç, acentos.
        - No usar el carácter (´).
        - Sin emoticonos ni símbolos en comentarios.
    * Usar #region con una descripción de máximo 4 palabras.
    * No usar líneas vacías para separar pero con la siguientes excepciones:
        - Cuando uses #region, coloca una línea vacía antes si no hay un "{" arriba.
        - Cuando uses #endregion, coloca línea vacía después si no hay un "}" abajo.
	* Si la clase tiene metodos con disintas funciones ordenalos por categoria (la funcion que cumplen) y envuelvelos con #region/#endregion.
	* Si tienes bastantes variables y piensas separarlos con comentarios usa #region/#endregion en su lugar.
    ### Dentro de métodos:
        - Encerrar bloques de comentarios largos en #region/#endregion.
        - No anidar #region/#endregion dentro de otros.
        - Uso de return o continue en la misma línea que la condición:
            - Ejemplo: if (x < 0) return;
        - Sin llaves en condicionales si solo hay una línea:
            - Ejemplo: if (x < 0) y = true;
    ### Uso del if y switch
        - Cuando uses condicionales que involucren strings, enums u otro tipo de valores fijos que hagan uso de condicionales "==" o "!=" usa "switch", "case" y "default" en lugar de "if" ya que facilitara la adicion de codigo en futuros cambios
    ### Para componentes GUI:
        - Verificar si tienen funciones nativas para tooltips.
        - Agregar tooltip en cada componente editado o creado.
        - No agregar tooltips en otros componentes a menos que se indique.
        - No implementar funciones externas para tooltips en código sin tocar (salvo indicación).
	###Uso de Singletons
	Al momento de crear clases modulares (auxiliares) verifica si es optimo hacer uso de singletons. Ejemplo de estructura singleton (colocala al inicio de la clase):

	#region Singleton
	private static ConversationNodeStyle SingletonObject;
	private ConversationNodeStyle() { }
	private ConversationNodeStyle CreateSingleton()
	{
		if (SingletonObject == null)
		{
			SingletonObject = this;
			//Aqui ejecuta los metodos para inicializar variables si tiene
		}
		return SingletonObject;
	}
	public static ConversationNodeStyle GetSingleton()
	{
		#Si no existe se creara automaticamente
		if (SingletonObject == null)
		{
			SingletonObject = new ConversationNodeStyle().CreateSingleton();
		}
		return SingletonObject;
	}
	#endregion

    ### Estas reglas aplican solo al código que estás creando o modificando en ese momento, no al código existente no tocado.
        - Excepciones:
            - Cuando se indique lo contrario.
            - Cuando modifiques un método existente.

---

## Experiencia de usuario (UX) en campos de interfaz
	* En proyectos con interaccion constante del usuario, define primero el ROL de cada campo (identificador, referencia, dato editable, calculado) y elige el control segun como el usuario interactuara con el.
	* Identificadores (ej. IDs numericos): mostrarlos como solo lectura, no como captura manual.
	* Campos que referencian a un identificador: usar dropdown/selector con las opciones validas, no texto libre.
	* Campos calculados o derivados: solo lectura, con tooltip que explique su origen.
	* Rangos numéricos acotados: slider o campo con límites min/max.
	* Colores, archivos o rutas: selector dedicado (color picker, explorador), no texto.
	* Vector2 o Vector3: los campos de coordenadas estar siempre en una misma linea
    * Acciones destructivas (borrar, reiniciar): pedir confirmación.
    * Validar en el momento de la captura y mostrar el error junto al campo.	
	* Propon al usuario otros casos de UX que identifiques para agregarlos a estas instrucciones.
    * Valores de un conjunto fijo (enums, estados, tipos): dropdown, no texto libre.
    * Campos que dependen de otros: deshabilitarlos o filtrar sus opciones hasta que el campo del que dependen tenga valor.

---

# Instrucciones operativas para Copilot (ConversationEngine)

## 1) Alcance estricto (NO salir de aqui)
Trabaja SOLO dentro de estas carpetas de `ConversationEngine/Assets/ConversationEngine/`:
- `ConversationBox/`
- `ConversationScheme/`
- `Editor/`
- `EditorActor/`
- `System/`
- `Icons/` (solo imagenes para iconos)

Cualquier carpeta/archivo fuera de esta lista queda fuera de alcance, salvo solicitud explicita del usuario.

---

## 2) Rol de cada modulo
- `ConversationScheme/`  
  Modelo de datos canonico de conversaciones y utilidades de nodos.  
  Cualquier cambio aqui impacta Editor y Runtime.

- `Editor/`  
  Herramientas de autoria visual (window, graph, inspector, properties, helpers, json).

- `EditorActor/`  
  Herramientas de autoria visual para archivos `.actor` (window, panel, graph, importador y helpers propios).

- `ConversationBox/`  
  Runtime/UI de dialogos en juego (actualmente en evolucion).

- `System/`  
  Estado global y variables de juego usadas por el runtime.

- `Icons/`  
  Solo recursos visuales de iconos. Sin logica.

---

## 3) Regla anti-duplicado (obligatoria)
Antes de crear codigo nuevo, SIEMPRE busca y reutiliza primero en:
- `ConversationScheme/ConversationNodeUtility.cs`
- `Editor/ConversationEditorCore.cs`
- `Editor/Helper/ConversationEditorHelpers.cs`
- `Editor/Helper/ConversationFunctionLibrary.cs`
- Clases existentes de `Editor/Graph`, `Editor/Panel`, `ConversationBox`, `System`

Si existe logica similar:
- Extiendela o reutilizala.
- NO la dupliques en una clase/metodo nuevo.

---

## 4) Reglas de ubicacion de codigo
- Contratos de datos, validaciones, integridad de grafo -> `ConversationScheme/`
- UI del editor y flujos de autoria -> `Editor/`
- UI del editor de actores y flujos de autoria `.actor` -> `EditorActor/`
- Ejecucion runtime de conversacion/UI -> `ConversationBox/`
- Estado/variables globales de juego -> `System/`
- Imagenes de iconos -> `Icons/`

---

## 5) Reglas de cambios en Editor (obligatorias)
Para cualquier mutacion de datos en el editor:
1. Registrar undo ANTES del cambio.
2. Aplicar la mutacion.
3. Marcar dirty DESPUES del cambio.
4. Respetar modo read-only: si esta activo, bloquear escrituras.

Mantener coherencia entre:
- `ConversationEditorWindow`
- `ConversationGraphView`
- `EditorInspector`
- `EditorProperties`

---

## 6) Reglas de impacto por cambios de esquema
Si modificas `ConversationScheme/`, debes revisar y ajustar (si aplica):
- `Editor/` (inspector, graph, serializacion/deserializacion, dropdowns)
- `ConversationBox/` (consumo runtime de datos)
- `System/` (si depende de condiciones/variables del esquema)

No dar por terminado un cambio de esquema sin validar impacto en esos modulos.

---

## 7) Estado de madurez (guia de criterio)
- `ConversationScheme`: estable (base del contrato)
- `Editor`: estable en uso, en evolucion
- `ConversationBox`: en construccion
- `System`: en construccion
- `Icons`: estable (visual)

Notas:
- `ConversationBox` puede contener flujos incompletos.
- `SimpleJsonSerializer` puede estar en evolucion; validar persistencia de campos complejos antes de asumir completitud.

---

## 8) Regla de documento vivo (obligatoria)
Estas instrucciones deben mantenerse actualizadas al estado vigente del proyecto:
- Eliminar informacion obsoleta.
- No registrar historial de cambios aqui.
- Si se agrega un nuevo proyecto/carpeta mediante un archivo `.asmdef`, actualizar estas instrucciones para:
  1) incluir el nuevo proyecto/carpeta en el alcance,
  2) describir su rol,
  3) definir sus reglas de ubicacion/uso e impacto.

---

## 9) Regla de estilo (aplica solo a codigo tocado)
Aplicar reglas de estilo y documentacion solo al codigo creado/modificado en la tarea actual.  
No refactorizar codigo no tocado solo por estilo, salvo solicitud explicita.

---

## 10) Checklist minimo antes de finalizar
- [ ] Trabaje solo en carpetas permitidas.
- [ ] Reutilice logica existente antes de crear nueva.
- [ ] Evite duplicacion de funciones/utilidades.
- [ ] Si cambie `.actor`, revise impacto en `EditorActor/` y su importador/helpers.
- [ ] Si cambie esquema, revise impacto en Editor/Runtime/System.
- [ ] Si el cambio fue significativo, actualice estas instrucciones (documento vivo).

---
