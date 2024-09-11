import bpy

def vector_to_string(vector):
    scaled_vector = vector * 100
    return f"{scaled_vector.x} {scaled_vector.y} {scaled_vector.z}"

obj = bpy.context.active_object

if obj and obj.type == 'MESH':
    # Get the mesh data
    mesh = obj.data

    # Extract vertices and scale by 100
    vertices = [vector_to_string(obj.matrix_world @ vertex.co) for vertex in mesh.vertices]

    # Extract edges
    edges = [(edge.vertices[0], edge.vertices[1]) for edge in mesh.edges]

    # Format vertices as a C# array
    cs_vertices = "readonly string[] vertices = [\n    " + ",\n    ".join(f'\"{v}\"' for v in vertices) + "\n];"

    # Format edges as a C# readonly List of tuples
    cs_edges = "readonly List<(int, int)> edges =\n[\n    " + ",\n    ".join(f'({e[0]}, {e[1]})' for e in edges) + "\n];"

    # Output C# code
    print(cs_vertices)
    print(cs_edges)

else:
    print("No mesh object selected!")
