class GLBParser {
    constructor(filePath) {
      this.filePath = filePath;
    }
  
    async parse() {
      try {
        const buffer = await this.readFile(this.filePath);
        const glb = this.parseGLB(buffer);
        return {
          vertices: this.extractVertices(glb),
          triangles: this.extractTriangles(glb)
        };
      } catch (error) {
        console.error('Error parsing GLB file:', error);
        throw error;
      }
    }
  
    async readFile(filePath) {
      // Implement file reading logic here
      // This will depend on your environment (Node.js or browser)
    }
  
    parseGLB(buffer) {
      // Implement GLB parsing logic here
      // This will involve parsing the GLB header and chunks
    }
  
    extractVertices(glb) {
      // Implement vertex extraction logic here
    }
  
    extractTriangles(glb) {
      // Implement triangle extraction logic here
    }
  }
  
  // Usage example:
  // const parser = new GLBParser('path/to/your/file.glb');
  // parser.parse().then(({ vertices, triangles }) => {
  //   console.log('Vertices:', vertices);
  //   console.log('Triangles:', triangles);
  // }).catch(error => {
  //   console.error('Error:', error);
  // });
  
  function testGLBParser() {
  const path = "base\\worlds\\03_night_city\\sectors_external\\proxy\\89695709\\decoset_trash_cardboard_pile_v1.glb"
  const parser = new GLBParser(path);
  parser.parse().then(({ vertices, triangles }) => {
      console.log('Vertices:', vertices);
      console.log('Triangles:', triangles);
  }).catch(error => {
      console.error('Error:', error);
  });
  }