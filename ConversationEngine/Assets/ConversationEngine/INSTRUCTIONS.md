>	Reglas para documentar el codigo:
	*	Para que este codigo tenga mayor difusion redactalo en ingles
	*	El codigo que añada funciones nuevas al programa agregalas en los archivos read_me que estan dentro del proyecto
	*	A menos que yo te lo indique evitaras:
		-	Crear archivos .md adicionales
		-	Documentar cambios en archivos tipo changelog o bitacora de cambios (esos que indica los cambios de la nueva version)
		-	Documentar las correcciones de bugs
		-	Los cambios que se hagan de algo que ya existe y ahora funciona diferente evitaras documentarlo como novedades de una nueva version a otra
			-	En su lugar editaras la documentacion para que se ajuste al nuevo contexto
	*	Usa emoticonos y simbolos si lo necesitas (configura la codificacion de los .md si es necesario)
>	Reglas para redactar y ordenar el codigo:
	*	Todas las variables, metodos, comentarios, etc tiene que estar en Ingles
		-	Solo usar caracteres alfanumericos al nombrarlos (nada de caracteres especiales)
		-	No usar caracteres que no se usen en ingles ejemplo: ñ, Ç y vocales con acento
		-	No usar este caracter (´) de ninguna forma
		-	No uses emoticonos ni simbolos en los comentarios
	*	Cuando uses #region colocaras una descripcion maximo de 4 palabras que resuma lo que hay dentro
	*	No uses lineas vacias para separar, me es mas legible leer una linea debajo de la otra y las etiquetas de #region/#endregion para separar el codigo extenso:
		-	En Notepad++ uso la expresion regular "^\s*\r?\n" para identificar y quitar las lineas vacias y dejar lo demas como esta (para que me entiendas a que me refiero con lineas vacias)
		-	A pesar de lo anterior mencionado con #region/#endregion aplicaras las siguientes reglas:
			-	cuando uses #region coloca una linea vacia (en caso de no haber un cochete "{" arriba de #region)
			-	cuando uses #endregion coloca una linea vacia (en caso de no haber un cochete "}" abajo de #endregion)
	*	Si la clase tiene metodos con disintas funciones ordenalos por categoria (la funcion que cumplen) y envuelvelos con #region/#endregion
	*	Si tienes bastantes variables y piensas separarlos con comentarios usa #region/#endregion en su lugar
	*	Dentro de un metodo:
		-	Si vas a colocar comentarios de mas de una linea envuelve el codigo del que se esta hablando (y esos mismos comentarios) en un #region/#endregion
		-	si una parte del codigo esta envuelto en un #region/#endregion no puedes colocar otro #region/#endregion
		-	Si usas return o continue para salir abruptamente de un metodo o loop por medio de una condicional el return tiene que estar dentro de la misma linea que la condicional ejemplo:
			-	if (x < 0) return;
		-	Si en la condicional no vas a usar corchetes "{ }" el codigo que se ejecuta al cumplirse la condicional estara en la misma linea ejemplo:
			-	if (x < 0) y = true;
	*	Con lo anterior descrito solo aplicaras estas reglas con el codigo que este creando o editando, no con el codigo que ya existe y que no estas tocando
		-	La excepciones a lo anterior y puedas aplicar las reglas anteriores seian:
			-	A menos que yo lo indique
			-	Estas modificando sobre un metodo que ya existia previamente