# Procedural-Mesh-Terrain-Learn
This is a learning project focusing on one of the many sides of Procedural generation, Terrain-Mesh-Generation with some key features:
 - Generate the data of height map using PerlinNoise function built-in in Unity.
 - Generate the data of Mesh using the generated map data.
 - Each mesh is treated as a terrain chunk and will connect seamlessly to each other.
 - Each terrain chunk has its own levels of detail or LODs, which are procedurally changed based on the distance between the chunk and viewer or player.
 - Every meshes generated will have texture mapped by colour.
 - Endless terrain. When the position of viewer or player changes, the surrounded terrains will also be updated.
