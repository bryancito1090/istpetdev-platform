# Dataset Hackathon - Recolección Urbana Quito

Dataset con coordenadas geográficas y restricciones operativas dentro del área urbana del Distrito Metropolitano de Quito (Pichincha, Ecuador).

## Puntos de Parada y Recolección
| Código | Nombre | Sector | Latitud | Longitud | Tipo | Prioridad |
|---|---|---|---|---|---|---|
| BOD-ECUA | Barrio La Ecuatoriana | Quito Sur | -0.3079 | -78.5682 | Recolección | Normal |
| BOD-VILL | La Villaflora - El Recreo | Quito Sur-Centro | -0.2520 | -78.5180 | Recolección | Normal |
| BOD-GUAM | Feria de Autos Guamaní | Quito Sur | -0.3556 | -78.5444 | Recolección | Media |
| BOD-CHIM | Av. Napo - Chimbacalle | Quito Centro-Sur | -0.2333 | -78.5083 | Recolección | **Alta (Urgente)** |
| BOD-CEQ | Centro de Exposiciones Quito | Quito Norte (Iñaquito) | -0.1925 | -78.4839 | Entrega Final | Sede Hackathon |

## Archivos Generados
- `bodegas.csv`: Catálogo de bodegas de origen y destino.
- `puntos.csv`: Puntos de servicio compatibles con la importación en la plataforma.
- `vehiculos.csv`: Flota asignada a cada depósito con costos y capacidades urbanas.
- `productos.csv`: Definición de integrantes como unidad transportable.
- `inventario.csv`: Stock de integrantes disponible en cada punto.
- `lotes.csv`: Trazabilidad y procedencia de cada lote.
- `tramos.csv`: Matriz de distancias y tiempos de viaje con tráfico urbano.
- `ruta_recoleccion_bodegas.csv`: Dataset consolidado de la ruta de recolección y entrega.
- `mapping.json`: Descriptor canónico para la carga de datos masiva en la plataforma.
