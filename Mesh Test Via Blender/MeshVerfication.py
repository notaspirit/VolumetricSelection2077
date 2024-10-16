
import bpy
import json

# Function to triangulate a polygon face
def triangulate_face(indices):
    if len(indices) == 3:
        return [indices]  # Already a triangle
    triangles = []
    for i in range(1, len(indices) - 1):
        triangles.append([indices[0], indices[i], indices[i + 1]])
    return triangles


# Specify the path to your JSON file
file_path = "E:\\Games\\WolvenKit Projects\\ExampleProject\\ExampleProject\\source\\resources\\CollisionDetectionCache.json"


# Load the JSON file
with open(file_path, 'r') as f:
    data = json.load(f)

# Extract vertices and indices from the first mesh object in the JSON
mesh_data = data[0]['mesh']
vertices = mesh_data['vertices']
indices = mesh_data['indices']

# Convert vertices into a list of tuples (x, y, z)
vertex_list = [(v['x'], v['y'], v['z']) for v in vertices]

# Prepare a list to hold triangulated faces
triangulated_faces = []

# Triangulate the indices if necessary
for i in range(0, len(indices), 3):
    # Get the current face (group of 3 indices)
    face = indices[i:i+3]
    triangulated_faces.extend(triangulate_face(face))

# Create a new mesh and object
mesh = bpy.data.meshes.new("imported_mesh")
obj = bpy.data.objects.new("Imported_Object", mesh)

# Link object to the scene
bpy.context.collection.objects.link(obj)

# Create mesh from given vertices and triangulated indices
mesh.from_pydata(vertex_list, [], triangulated_faces)

# Update the mesh with new data
mesh.update()

# Set the object as active in the scene and select it
bpy.context.view_layer.objects.active = obj
obj.select_set(True)
