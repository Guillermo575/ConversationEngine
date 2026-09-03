
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
	* Antes de proyectos desde cero, analizar si el código puede reutilizarse.
		- Es preferible tener archivos separados que se encarguen de tareas especificas que podrían ocupar muchas lineas de codigo.
		- Si haces código que se encarga de manejar segmentos modulares (como interfaces, paneles o ventanas) manejaras una clase principal que funcione como la "base" que servira para instanciar, manejar y mantener en comunicacion a las demas clases modulares.
		- El objetivo es ahorrar tiempo, prevenir tener archivos pesados, codigo duplicado, logica redundante y volver a analizar el codigo para hacer la segmentacion en una etapa avanzada del proyecto.

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
    ### Para componentes GUI:
        - Verificar si tienen funciones nativas para tooltips.
        - Agregar tooltip en cada componente editado o creado.
        - No agregar tooltips en otros componentes a menos que se indique.
        - No implementar funciones externas para tooltips en código sin tocar (salvo indicación).
    ### Estas reglas aplican solo al código que estás creando o modificando en ese momento, no al código existente no tocado.
        - Excepciones:
            - Cuando se indique lo contrario.
            - Cuando modifiques un método existente.

---
