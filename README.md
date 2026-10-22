# Spider Solitaire (.NET 10) 🕷️🃏

Un clon completo y moderno del clásico juego Spider Solitaire, desarrollado en C# sobre **.NET 10** utilizando **Windows Forms**. 

Este proyecto fue construido con un enfoque estricto en **Arquitectura Limpia**, principios **S.O.L.I.D.** y **Patrones de Diseño**, separando completamente la lógica de negocio de la interfaz gráfica.

## 🚀 Características Principales

* **Reglas Clásicas:** Soporta los 3 modos de dificultad tradicionales:
  * Fácil (1 Palo - Picas)
  * Medio (2 Palos - Picas y Corazones)
  * Difícil (4 Palos)
* **Motor de Animaciones:** Las cartas se reparten y vuelan hacia las fundaciones de manera fluida y suave utilizando un motor visual basado en interpolación (Ease-Out).
* **Sistema de Deshacer (Undo) Robusto:** Presiona `Ctrl + Z` para revertir cualquier movimiento. Funciona de manera transaccional, revirtiendo incluso movimientos complejos como la finalización de un palo entero.
* **Interfaz Responsiva:** El tablero de juego (cartas, mazo y espacios) se escala dinámicamente según el tamaño de la ventana.
* **Temas Visuales:** Soporte nativo para modo Claro (Light) y Oscuro (Dark).
* **Generación por Semillas (Seeds):** Posibilidad de reiniciar la partida actual utilizando exactamente el mismo mazo mezclado.

## 🏗️ Arquitectura y Patrones de Diseño

La solución está dividida estrictamente en dos capas para asegurar un bajo acoplamiento:

### 1. `CardGames.Core` (Dominio y Lógica)
Una librería de clases pura que no conoce nada sobre UI, donde residen las reglas de negocio:
* **Inmutabilidad:** Las cartas son manejadas utilizando `records` de C#.
* **Command Pattern & MacroCommand:** Cada movimiento del jugador se encapsula en comandos (`SpiderMoveCommand`, `SpiderDealCommand`). Los movimientos complejos se agrupan en un `SpiderMacroCommand` que permite deshacer acciones en bloque sin romper el historial.
* **Snapshot Pattern:** Utilizado en la recolección de palos completos para guardar el estado exacto de las 13 cartas en memoria y garantizar un Undo perfecto.
* **Result Pattern:** El motor no lanza excepciones para el flujo de control, devuelve objetos `Result` (Success/Invalid) para validar reglas de negocio (como origen y destino en el Drag & Drop).

### 2. `SpiderWinForms` (Interfaz de Usuario)
Una capa de presentación ligera que reacciona a los comandos del core:
* Renderizado dinámico utilizando `PictureBox`.
* Gestión de Drag & Drop con validación de áreas (Hitboxes tolerantes).
* `DoubleBuffering` activado para evitar el parpadeo de WinForms.

## 🛠️ Tecnologías

* **Lenguaje:** C# 12
* **Framework:** .NET 8.0
* **UI:** Windows Forms (WinForms)

## 🎮 Cómo jugar (Instalación)

1. Clona este repositorio:
   ```bash
   git clone https://github.com/TU_USUARIO/SpiderSolitaire-CleanArchitecture.git
   ```
2. Abre la solución (`.sln`) en **Visual Studio 2022**.
3. Asegúrate de tener instalado el SDK de .NET 8.
4. Compila y ejecuta el proyecto `SpiderWinForms`.

## 📸 Capturas de Pantalla

*(Reemplaza estas URLs con imágenes reales de tu juego)*

| Tema Claro - Nivel Intermedio) | Tema Oscuro - Nivel Difícil |
| :---: | :---: |
| <img width="947" height="527" alt="image" src="https://github.com/user-attachments/assets/cf08fbcf-34b3-4163-bbd6-d74375357d14" />
| <img width="1171" height="562" alt="image" src="https://github.com/user-attachments/assets/4fabdb33-ee6c-480f-97f2-01ed922efe92" />
 

## 🤝 Contribuciones
Las sugerencias y *Pull Requests* son bienvenidos. Si deseas contribuir, por favor abre un *Issue* primero para discutir los cambios que te gustaría realizar.
Cabe mencionar que en este proyecto participó principalmente Gemini Pro, aunque Claude también ayudó un poco.
## 📝 Licencia
Este proyecto está bajo la Licencia MIT.
