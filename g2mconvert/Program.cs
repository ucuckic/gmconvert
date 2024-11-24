
using System.Runtime.Intrinsics.X86;
using static g2mconvert.Program;

namespace g2mconvert
{
    internal class Program
    {
        const int _flag_ReadNormals = 2;
        public class vertex
        {
            //in the binary g3m format all vertex data is 8 floats (32 bytes) long

            public float vx = 0;
            public float vy = 0;
            public float vz = 0;

            public float nx = 0;
            public float ny = 0;
            public float nz = 0;

            public float uvx = 0;
            public float uvy = 0;
            public vertex(float _x, float _y, float _z, float ux, float uy, float gx = 0, float gy = 0, float gz = 0 )
            {
                vx = _x;
                vy = _y;
                vz = _z;

                uvx = ux;
                uvy = uy;

                nx = gx;
                ny = gy;
                nz = gz;
            }
        }

        public class tri
        {

            //in the binary g3m format all indicies are 3x int16
            public int[] indicies = new int[3];

            public tri(int x, int y, int z)
            {
                indicies[0] = x;
                indicies[1] = y;
                indicies[2] = z;
            }
        }
        static void Main(string[] args)
        {
            Console.WriteLine("c " + args[0]);
            convert_g3m(args[0]);
        }

        //g3m is a binary version of g2m
        static void convert_g3m(string in_path, string out_path = "out")
        {
            List<vertex> verts = new List<vertex>();
            List<tri> tris = new List<tri>();

            using (FileStream infile = File.OpenRead(in_path))
            {
                using (BinaryReader reader = new BinaryReader(infile))
                {
                    byte type_test = reader.ReadByte();
                    if( type_test == 0x22 )
                    {
                        convert_g2m(in_path);
                        return;
                    }

                    reader.BaseStream.Position = 0;
                    int thing = reader.ReadInt32();
                    int flags = reader.ReadInt32(); //everything to be read technically has a flag, but there shouldnt be a case where vertex+uv (8+1) arent used so the only case that matters is no normals

                    float texprop_1 = reader.ReadSingle();
                    float texprop_2 = reader.ReadSingle();

                    int texprop_3 = reader.ReadInt32();

                    //strings are chunked into chunks of 4? maybe to ensure allignment

                    UInt16 tex_string_len_1 = reader.ReadUInt16();
                    UInt16 tex_string_len_2 = reader.ReadUInt16();

                    Console.WriteLine("slen1 "+tex_string_len_1);
                    Console.WriteLine("slen2 " + tex_string_len_2);

                    //reader.BaseStream.Position = 0x18;

                    UInt16 vert_count = reader.ReadUInt16();
                    UInt16 tri_count = reader.ReadUInt16();

                    Console.WriteLine("verts: "+vert_count);

                    byte[] string_1 = reader.ReadBytes(tex_string_len_1);
                    byte[] string_2 = reader.ReadBytes(tex_string_len_2);

                    Console.WriteLine("starting garbage at " + reader.BaseStream.Position);

                    //reader.BaseStream.Position = 0x30; //some garbage table here idk what this shit is
                    //each entry is 0x20

                    reader.BaseStream.Position += ( vert_count * 0x20);

                    Console.WriteLine("starting vert table at "+ reader.BaseStream.Position);

                    for(int i = 0; i < vert_count; i++)
                    {
                        // vertex xyz > uv xy > normal xyz

                        if ( (flags & _flag_ReadNormals) != 0 )
                        {
                            vertex add_vert = new vertex(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                            verts.Add(add_vert);
                        }
                        else
                        {
                            vertex add_vert = new vertex(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                            verts.Add(add_vert);
                        }
                    }

                    Console.WriteLine("starting indicies table at " + reader.BaseStream.Position);

                    for (int i = 0; i < tri_count; i++)
                    {
                        tri add_tri = new tri((int)reader.ReadUInt16(), (int)reader.ReadUInt16(), (int)reader.ReadUInt16());
                        tris.Add(add_tri);
                    }

                    string ply_header =
                    "ply\n" +
                    "format ascii 1.0\n" +
                    "comment test\n" +
                    "element vertex " + vert_count + "\n" +
                    "property float x\n" +
                    "property float y\n" +
                    "property float z\n" +
                    "property float s\n" +
                    "property float t\n" +
                    "property float nx\n" +
                    "property float ny\n" +
                    "property float nz\n" +
                    "element face " + tri_count + "\n" +
                    "property list uchar uint vertex_indices\n" +
                    "end_header\n";

                    using (StreamWriter writer = File.CreateText(Path.GetFileNameWithoutExtension(in_path) + ".ply"))
                    {
                        writer.Write(ply_header);

                        for (int i = 0; i < vert_count; i++)
                        {
                            //writer.WriteLine(vert_lines[i]);
                            writer.Write(verts[i].vx + " " + verts[i].vy + " " + verts[i].vz + " " + verts[i].uvx + " " + verts[i].uvy + " " + verts[i].nx+ " "+ verts[i].ny+ " "+ verts[i].nz + "\n");
                        }

                        for (int i = 0; i < tri_count; i++)
                        {
                            writer.Write("3 "+tris[i].indicies[0] +" "+tris[i].indicies[1] + " "+tris[i].indicies[2]+"\n");
                        }
                    }
                }

                    
            }

        }

        static void convert_g2m(string in_path, string out_path = "out")
        {


            //changed operating methodology halfway through; requires revision

            List<String> vert_lines = new List<String>();
            List<String> tri_lines = new List<String>();

            List<vertex> verts = new List<vertex>();
            List<tri> tris = new List<tri>();

            int file_index = 0;

            string[] in_g2m = File.ReadAllLines(in_path);

            string m_type = in_g2m[file_index];
            file_index++;

            int n_texture = Int32.Parse(in_g2m[file_index]);
            file_index++;

            //string t_info = in_g2m[2];
            //skip texture info
            file_index += n_texture;

            int n_vertex = Int32.Parse(in_g2m[file_index]);
            file_index++;

            for(int i = 0; i < n_vertex; i++)
            {
                string vertex_line = in_g2m[file_index+i];
                vert_lines.Add(vertex_line);

                string[] floats = vertex_line.Split(' ');

                vertex add_vert = new vertex( float.Parse( floats[0] ), float.Parse(floats[1]), float.Parse(floats[2]), float.Parse(floats[3]), float.Parse(floats[4]) );

                verts.Add(add_vert);
            }

            file_index+= n_vertex;

            int n_tri = Int32.Parse(in_g2m[file_index]);
            file_index++;

            for (int i = 0; i < n_tri; i++)
            {
                string tri_line = in_g2m[file_index + i];
                tri_lines.Add(3+" "+tri_line);

                string[] ints = tri_line.Split(' ');

                tri add_tri = new tri(Int32.Parse(ints[0]), Int32.Parse(ints[1]), Int32.Parse(ints[2]) );

                tris.Add(add_tri);
            }

            file_index += n_tri;

            string ply_header =
                "ply\n" +
                "format ascii 1.0\n" +
                "comment test\n" +
                "element vertex "+n_vertex+"\n" +
                "property float x\n" +
                "property float y\n" +
                "property float z\n" +
                "property float s\n" +
                "property float t\n" +
                "element face "+n_tri+"\n" +
                "property list uchar uint vertex_indices\n" +
                "end_header\n";

            using (StreamWriter writer = File.CreateText(Path.GetFileNameWithoutExtension(in_path)+".ply"))
            {
                writer.Write(ply_header);

                for (int i = 0; i < n_vertex; i++)
                {
                    //writer.WriteLine(vert_lines[i]);
                    writer.Write(verts[i].vx+" "+ verts[i].vy+" "+ verts[i].vz+" "+ verts[i].uvx+" "+ verts[i].uvy+"\n"); 
                }

                for (int i = 0; i < n_tri; i++)
                {
                    writer.WriteLine(tri_lines[i]);
                }
            }
        }
    }
}