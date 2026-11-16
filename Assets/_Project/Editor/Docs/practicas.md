# Zona de prácticas: `PrototypeSceneBuilder.Training.cs`
`BuildTrainingArea` se llama después de la cámara y crea el objeto `Practica` en la raíz de la escena, fuera de "Mundo" porque se mueve. Todo va en la acera sur de la manzana central, en z −71.6:
- Tres **dianas** (`TrainingDummy` + `Health`) en x 5, 7.5 y 10, mirando al oeste, hacia el inicio. Tienen poste, torso, cabeza y círculo rojo; la esfera `Cabeza` cuenta como tiro en la cabeza.
- Una fila de **objetos** (`ItemPickup`) en `TrainingPickups`, de x −8.5 a −1: munición, botiquín, alitas, bebida energética, chaleco y 50 $.
