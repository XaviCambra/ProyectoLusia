Normas de escritura de carpetas/archivos
	 - Usar formato CamelCase para archivos y carpetas
	 - Usar el singular siempre


Carpetas:
Code: Codigo del juego
	- Archivos permitidos:
		- .cs
	- Organizado por carpetas segun la función. Ej.
		- Dialogue
		- Character Movement
		- Inputs
		- Combat
	- Las carpetas de interfaces, tests, etc. deben estar dentro de la misma. Ej.
		- Dialogue
			- Interface
			- Editor
			- Test
		- Combat
			- Interface
			- Editor
			- Test
Export: Objetos usados solo para la plataforma (creo)
	- Archivos permitidos:
		- .prefab
Narrative (No estoy seguro de que esta carpeta tenga que ser general)
Resources: Aqui van todo lo relacionado con el apartado visual y funcional.
	- Organizado por carpeta segun la funcion. Ej.
		- Animations
			- Dialogue Animations
			- Character Animations
			- Combat Animations
		- Art
			- Portraits
			- Spritesheets
			- Backgrounds
			- UI
		- Game Data: Principalmente los scriptableobjects
			- Dialogue
			- Rules
			- Combat
		- Localization
			- .csv
		- Prefabs: Excluidos los que sean para exports
		- Scenes
Settings
TMPro
			
			
 