import bpy
import json

output_file_path = "//wsl.localhost/Ubuntu/home/dea/serverfiles/game/csgo/cfg/CS2DrawWireframe/wireframe.json"

def vector_to_string(vector):
    scaled_vector = vector * 100
    return f"{scaled_vector.x} {scaled_vector.y} {scaled_vector.z}"

def update_wireframe(obj):
    if obj and obj.type == 'MESH':
        mesh = obj.data
        vertices = [vector_to_string(obj.matrix_world @ vertex.co) for vertex in mesh.vertices]
        edges = [[edge.vertices[0], edge.vertices[1]] for edge in mesh.edges]

        wireframe_data = {
            "Vertices": vertices,
            "Edges": edges
        }

        with open(output_file_path, 'w') as outfile:
            json.dump(wireframe_data, outfile, indent=4)

def object_move_handler(scene):
    if bpy.context.active_object:
        update_wireframe(bpy.context.active_object)

bpy.app.handlers.depsgraph_update_post.append(object_move_handler)
