import bpy
import json

output_file_path = "//wsl.localhost/Ubuntu/home/dea/serverfiles/game/csgo/cfg/CS2DrawWireframe/wireframe.json"

last_update_frame = 0

def vector_to_string(vector):
    scaled_vector = vector * 50
    return f"{scaled_vector.x} {scaled_vector.y} {scaled_vector.z}"

def update_combined_wireframe(selected_objects):
    combined_vertices = []
    combined_edges = []
    vertex_offset = 0

    for obj in selected_objects:
        if obj.type == 'MESH':
            depsgraph = bpy.context.evaluated_depsgraph_get()
            evaluated_obj = obj.evaluated_get(depsgraph)
            mesh = evaluated_obj.to_mesh()

            for vertex in mesh.vertices:
                combined_vertices.append(vector_to_string(obj.matrix_world @ vertex.co))

            for edge in mesh.edges:
                combined_edges.append([edge.vertices[0] + vertex_offset, edge.vertices[1] + vertex_offset])

            vertex_offset += len(mesh.vertices)

            evaluated_obj.to_mesh_clear()

    wireframe_data = {
        "Vertices": combined_vertices,
        "Edges": combined_edges
    }

    with open(output_file_path, 'w') as outfile:
        json.dump(wireframe_data, outfile, indent=4)

def frame_change_handler(scene):
    global last_update_frame

    current_frame = scene.frame_current
    fps = scene.render.fps

    frames_per_update = fps / 16.0

    if current_frame < last_update_frame:
        last_update_frame = 0

    if current_frame - last_update_frame >= frames_per_update:
        selected_objects = bpy.context.selected_objects
        if selected_objects:
            update_combined_wireframe(selected_objects)
        last_update_frame = current_frame

bpy.app.handlers.frame_change_pre.append(frame_change_handler)