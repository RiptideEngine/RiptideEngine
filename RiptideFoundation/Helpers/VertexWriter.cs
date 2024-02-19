using RiptideFoundation.Rendering;

namespace RiptideFoundation.Helpers;

public delegate void VertexWriter<in TFrom>(MeshBuilder builder, TFrom from) where TFrom : unmanaged;