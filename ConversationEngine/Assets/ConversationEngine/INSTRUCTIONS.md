
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
