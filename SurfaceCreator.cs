using UnityEngine;
using System;
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SurfaceCreator : MonoBehaviour {

	[Range(1, 1024)]
	public int resolution = 10;

	public Vector3 offset;
	public Vector3 rotation;

	[Range(0f, 1f)]
	public float strength = 1f;

	public bool damping;

	public float frequency = 1f;
	
	[Range(1, 8)]
	public int octaves = 1;
	
	[Range(1f, 4f)]
	public float lacunarity = 2f;
	
	[Range(0f, 1f)]
	public float persistence = 0.5f;
	
	[Range(1, 3)]
	public int dimensions = 3;
	
	public NoiseMethodType type;
	
	public Gradient coloring;

	public bool coloringForStrength;

	private Mesh mesh;
	private Vector3[] vertices;
	private Vector3[] normals;
	private Color[] colors;
    private float seed = 0.3f;

    private Vector3[,] vertexGrid;

	private int currentResolution;
    private int counter = 0;
	private void OnEnable ()
    {
		if (mesh == null)
        {
			mesh = new Mesh();
			mesh.name = "Surface Mesh";
			GetComponent<MeshFilter>().mesh = mesh;
		}
		Refresh();
	}

	public void Refresh ()
    {
		if (resolution != currentResolution) {CreateGrid();}
		Quaternion q = Quaternion.Euler(rotation);
		Vector3 point00 = q * new Vector3(-0.5f, -0.5f) + offset;
		Vector3 point10 = q * new Vector3( 0.5f, -0.5f) + offset;
		Vector3 point01 = q * new Vector3(-0.5f, 0.5f) + offset;
		Vector3 point11 = q * new Vector3( 0.5f, 0.5f) + offset;

		NoiseMethod method = Noise.methods[(int)type][dimensions - 1];
		float stepSize = 1f / resolution;
		float amplitude = damping ? strength / frequency : strength;
		for (int v = 0, y = 0; y <= resolution; y++)
        {
			Vector3 point0 = Vector3.Lerp(point00, point01, y * stepSize);
			Vector3 point1 = Vector3.Lerp(point10, point11, y * stepSize);
			for (int x = 0; x <= resolution; x++, v++)
            {
				Vector3 point = Vector3.Lerp(point0, point1, x * stepSize);
				float sample = Noise.Sum(method, point, frequency, octaves, lacunarity, persistence);
				sample = type == NoiseMethodType.Value ? (sample - 0.5f) : (sample * 0.5f);
				if (coloringForStrength)
                {
					colors[v] = coloring.Evaluate(sample + 0.5f);
					sample *= amplitude;
				}
				else
                {
					sample *= amplitude;
					colors[v] = coloring.Evaluate(sample + 0.5f);
				}
				//vertices[v].y = sample;
			}
		}
        // vertexGrid[resolution / 2, resolution / 2].y = 1f;
        seed = 0.05f;
        vertexGrid[0, 0].y = seed;
        vertexGrid[resolution, 0].y = seed;
        vertexGrid[0, resolution].y = seed;
        vertexGrid[resolution, resolution].y = seed;
        diamondSquare(0,0,resolution);
        for (int v = 0, z = 0; z <= resolution; z++)
        {
            for (int xg = 0; xg <= resolution; xg++, v++)
            {
                vertices[v] = vertexGrid[xg, z];
            }
        }
        mesh.vertices = vertices;
		mesh.colors = colors;
		mesh.RecalculateNormals();
	}
    private void diamondSquare(int x, int y, int size)
    {
        if (size<2) { return; }
        square(x, y, size);
        
        diamond(x+size/2, y, size);
        diamond(x, y+size/2, size);
        diamond(x + size / 2, y+size, size);
        diamond(x + size, y+size/2, size);
        seed /= Mathf.Pow(2f, 1.2f);
        size++;
        diamondSquare(x + size / 2, y + size / 2, size / 2);
       // seed /= Mathf.Pow(1.2f, 1.2f);
        diamondSquare(x, y, size / 2);
       // seed /= Mathf.Pow(1.2f, 1.2f);
        //seed /= Mathf.Pow(1.5f, 1.1f);
        diamondSquare(x+size/2, y, size / 2);
       // seed /= Mathf.Pow(1.2f, 1.2f);
        diamondSquare(x, y+size/2, size / 2);
        

    }
    private void square(int x, int y, int size)
    {
        int xpsize = x + size;
        int xmsize = x - size;
        int ypsize = y + size;
        int ymsize = y - size;
        if (y - size < 0)
        {
            ymsize = Math.Abs(size);
        }
        if (y + size > resolution)
        {
            ypsize -= resolution;
        }
        if (x + size > resolution)
        {
            xpsize -= resolution;
        }
        if (x - size < 0)
        {
            xmsize = Math.Abs(size);
        }
        float  average = vertexGrid[x, y].y + vertexGrid[xpsize, y].y + vertexGrid[x, ypsize].y + vertexGrid[xpsize,ypsize].y;
        average /= 4.0f;
        vertexGrid[x + size / 2, y + size / 2].y = (average + UnityEngine.Random.Range(0, seed));
    }

    private void diamond(int x, int y, int size)
    {
        size /= 2;
        int xpsize = x + size;
        int xmsize = x - size;
        int ypsize = y + size;
        int ymsize = y - size;
        if (y - size < 0)
        {
            ymsize = Math.Abs(size);
        }
        if (y + size > resolution)
        {
            ypsize -= resolution;
        }
        if (x + size > resolution)
        {
            xpsize -= resolution;
        }
        if (x-size<0)
        {
            xmsize = Math.Abs(size);
        } 
        //print(y+","+x+","+size);
        float average = vertexGrid[xmsize, y].y + vertexGrid[xpsize, ypsize].y + vertexGrid[xpsize, y].y + vertexGrid[x, ymsize].y;
        average /= 4.0f;
        vertexGrid[x, y].y = (average + UnityEngine.Random.Range(0, seed));

      
    }

    private int indexOf(int x, int y)
    {
        if(x < 0) { x = 0; }
        if (y < 0) { y = 0; }
        if (x + (y * resolution) > resolution * resolution) { return (resolution * resolution) - 1; }

        return x + (y*resolution);
    }
	private void CreateGrid ()
    {
		currentResolution = resolution;
		mesh.Clear();
        vertexGrid = new Vector3[(resolution + 1), (resolution + 1)];
		vertices = new Vector3[(resolution + 1) * (resolution + 1)];
		colors = new Color[vertices.Length];
		normals = new Vector3[vertices.Length];
		Vector2[] uv = new Vector2[vertices.Length];
		float stepSize = 1f / resolution;
		for (int v = 0, z = 0; z <= resolution; z++)
        {
			for (int x = 0; x <= resolution; x++, v++)
            {
				vertices[v] = new Vector3(x * stepSize - 0.5f, 0f, z * stepSize - 0.5f);
                vertexGrid[x,z] = new Vector3(x * stepSize - 0.5f, 0f, z * stepSize - 0.5f);
                colors[v] = Color.black;
				normals[v] = Vector3.up;
				uv[v] = new Vector2(x * stepSize, z * stepSize);
			}
		}
		mesh.vertices = vertices;
		mesh.colors = colors;
		mesh.normals = normals;
		mesh.uv = uv;

		int[] triangles = new int[resolution * resolution * 6];
		for (int t = 0, v = 0, y = 0; y < resolution; y++, v++)
        {
			for (int x = 0; x < resolution; x++, v++, t += 6)
            {
				triangles[t] = v;
				triangles[t + 1] = v + resolution + 1;
				triangles[t + 2] = v + 1;
				triangles[t + 3] = v + 1;
				triangles[t + 4] = v + resolution + 1;
				triangles[t + 5] = v + resolution + 2;
			}
		}
		mesh.triangles = triangles;
	}
}