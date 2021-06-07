using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using UnityEngine;

using Profiler = UnityEngine.Profiling.Profiler;

public class Fulid : MonoBehaviour
{
    public GameObject particle_instance;
    public GameObject Tank;
    GameObject[] particles;
    GameObject liquid_face;
    int particle_each_edge = 15;
    int mesh_resolution = 15;
    int n_particles;
    Vector3[] position;
    Vector3[] velocity;
    Vector3[] accelerate;
    Vector3[,] face_points;
    float[] density;
    float[] pressure;
    float Viscosity_coeffcient = 0.0001f;
    float mass;
    float particle_radius = 1.8f;
    float initial_density = 1000.0f;
    float pressure_constant = 1000.0f;
    const float PI = 3.1415926f;
    Vector3 gravity = new Vector3(0, -9.81f, 0);
    float h3;
    float damping_rate = 0.995f;
    float timestep = 0.10f;
    float nowtime = 0.0f;
    #region YCHField
    HashSet<int>[,,] search_grids;
    float grid_side_length;
    //Side of the whole grid
    Vector3 grid_origin;
    Vector3 diagonal_point;
    Vector3Int matrix_num;
    List<int>[] neighbors;
    #endregion
    Vector3 Tank_Center;


    void Start()
    {
        n_particles = particle_each_edge * particle_each_edge * particle_each_edge;
        position = new Vector3[n_particles];
        velocity = new Vector3[n_particles];
        accelerate = new Vector3[n_particles];
        //face_points = new Vector3[mesh_resolution, mesh_resolution];
        density = new float[n_particles];
        pressure = new float[n_particles];
        particles = new GameObject[n_particles];
        for (int i = 0; i < particle_each_edge; i++)
        {
            for (int j = 0; j < particle_each_edge; j++)
            {
                for (int k = 0; k < particle_each_edge; k++)
                {
                    position[i + particle_each_edge * (j + particle_each_edge * k)] = new Vector3(-5.0f + ((5.0f + 5.0f) / (particle_each_edge - 1)) * i, +((0.0f + 10.0f) / (particle_each_edge - 1)) * j, -5.0f + ((5.0f + 5.0f) / (particle_each_edge - 1)) * k);
                    position[i + particle_each_edge * (j + particle_each_edge * k)] += new Vector3(UnityEngine.Random.Range(-0.1f, 0.1f), UnityEngine.Random.Range(-0.1f, 0.1f), UnityEngine.Random.Range(-0.1f, 0.1f));
                    velocity[i + particle_each_edge * (j + particle_each_edge * k)] = new Vector3(0, 0, 0);
                    accelerate[i + particle_each_edge * (j + particle_each_edge * k)] = new Vector3(0, 0, 0);
                }
                
            }
        }
        /*for(int i = 0; i < mesh_resolution; i++)
        {
            for(int j=0;j<mesh_resolution;j++) face_points[i, j] = new Vector3(0.0f, 0.0f, 0.0f);
        }*/
        
        mass = (initial_density * 1000.0f) / n_particles;
        h3 = particle_radius * particle_radius * particle_radius;
        create_particles();
        particle_instance.active = false;
        Tank_Center = new Vector3(0.0f, 0.0f, 1.0f);
        //liquid_face = createMesh(mesh_resolution, mesh_resolution);
        #region YCHInit
        neighbors = new List<int>[n_particles];
        for (int i = 0; i < neighbors.Length; i++)
        {
            neighbors[i] = new List<int>();
        }
        grid_side_length = 2.0f;
        grid_origin = new Vector3(-10, -10, -10);
        diagonal_point = new Vector3(10, 20, 10);

        Vector3 matrix_scale = diagonal_point - grid_origin;
        matrix_num = new Vector3Int(
            Mathf.CeilToInt(matrix_scale.x / grid_side_length),
            Mathf.CeilToInt(matrix_scale.y / grid_side_length),
            Mathf.CeilToInt(matrix_scale.z / grid_side_length));
        //print($"num:{matrix_num}");
        search_grids = new HashSet<int>[matrix_num.x, matrix_num.y, matrix_num.z];
        init_search_grids();
        #endregion



    }

    // Update is called once per frame
    void create_particles()
    {
        for (int i = 0; i < n_particles; i++)
        {
            particles[i] = Instantiate(particle_instance);
            Transform particle_position = particles[i].GetComponent<Transform>();
            particle_position.position = position[i];
            particles[i].name = String.Format("Particle {0}", i);
        }
    }
    void draw_particles()
    {
        for (int i = 0; i < n_particles; i++)
        {
            Transform particle_position = particles[i].GetComponent<Transform>();
            particle_position.position = position[i];
        }
        Transform tank_position=Tank.GetComponent<Transform>();
        tank_position.position = Tank_Center;
    }
    void Update()
    {
        search();
        calculate_force();
        State_Update();
        
        draw_particles();
        //draw_edge();
        Shake_Tank();
        //UnityEngine.Debug.Log(find_point(0.0f,0.0f));
    }
    void Shake_Tank()
    {
        Tank_Center.x = (float)Math.Sin(20 * nowtime);
        Tank_Center.z = (float)Math.Cos(20 * nowtime);
    }
    void calculate_force()
    {
        for (int i = 0; i < n_particles; i++)
        {
            neighbors[i].Clear();
        }

        Parallel.For(0, n_particles, i =>
        {
            get_neighbor(i, ref neighbors[i]);
        });

        Parallel.For(0, n_particles, i =>
        {
            density[i] = 0.0f;
            for (int j = 0; j < neighbors[i].Count; j++)
            {
                float temp1 = particle_radius * particle_radius - (position[i] - position[neighbors[i][j]]).magnitude;
                density[i] += temp1 * temp1 * temp1;
            }

            float h9 = h3 * h3 * h3;
            density[i] *= (mass * 315) / (64 * PI * h9);
        });
        Parallel.For(0, n_particles, i =>
        {
            pressure[i] = pressure_constant * (density[i] - initial_density);
        });
        Parallel.For(0, n_particles, i =>
        {
            Vector3 a_pressure = new Vector3(0, 0, 0);
            Vector3 a_viscosity = new Vector3(0, 0, 0);
            float temp_coefficient = (mass * 45.0f) / (PI * h3 * h3);
            for (int j = 0; j < neighbors[i].Count; j++)
            {
                Vector3 deltaR = position[i] - position[neighbors[i][j]];
                float h_r = particle_radius - deltaR.magnitude;
                a_pressure += (((pressure[i] + pressure[neighbors[i][j]]) * h_r * h_r) / (2 * density[i] * density[neighbors[i][j]] * deltaR.magnitude)) * deltaR;
                a_viscosity -= (h_r / (density[i] * density[neighbors[i][j]])) * (velocity[i] - velocity[neighbors[i][j]]);
            }
            a_pressure *= temp_coefficient;
            a_viscosity *= temp_coefficient * Viscosity_coeffcient;
            accelerate[i] = gravity + a_pressure + a_viscosity;
        });
    }
    void State_Update()
    {
        float dt = Time.deltaTime;
        for (int i = 0; i < n_particles; i++)
        {
            velocity[i] += accelerate[i] * dt * timestep;
            boundary_force(i);
            velocity[i] *= damping_rate;
            position[i] += velocity[i] * dt * timestep;
        }
        nowtime += dt * timestep;
    }


    #region YCHFuncs
    void init_search_grids()
    {
        for (int i = 0; i < matrix_num.x; i++)
        {
            for (int j = 0; j < matrix_num.y; j++)
            {
                for (int k = 0; k < matrix_num.z; k++)
                {
                    search_grids[i, j, k] = new HashSet<int>();
                }
            }
        }
    }

    Vector3Int get_grid(Vector3 pos)
    {
        Vector3 local_position = pos - grid_origin;
        Vector3Int grid_position = new Vector3Int(
            Mathf.FloorToInt(local_position.x / grid_side_length),
            Mathf.FloorToInt(local_position.y / grid_side_length),
            Mathf.FloorToInt(local_position.z / grid_side_length));
        //print($"local_posiiton:{local_position},grid_position:{grid_position}");
        return grid_position;
    }

    Vector3Int get_grid(int target)
    {
        Vector3 target_position = position[target];
        Vector3 local_position = target_position - grid_origin;
        Vector3Int grid_position = new Vector3Int(
            Mathf.FloorToInt(local_position.x / grid_side_length),
            Mathf.FloorToInt(local_position.y / grid_side_length),
            Mathf.FloorToInt(local_position.z / grid_side_length));
        //print($"local_posiiton:{local_position},grid_position:{grid_position}");
        return grid_position;
    }

    void search()
    {
        foreach (HashSet<int> set in search_grids)
        {
            set.Clear();
        }
        for (int target = 0; target < n_particles; target++)
        {
            Vector3Int g_pos = get_grid(target);
            search_grids[g_pos.x, g_pos.y, g_pos.z].Add(target);
        }
    }
    bool grid_bound(Vector3Int grid_pos)
    {
        return
            0 <= grid_pos.x && grid_pos.x < matrix_num.x &&
            0 <= grid_pos.y && grid_pos.y < matrix_num.y &&
            0 <= grid_pos.z && grid_pos.z < matrix_num.z;
    }

    void get_neighbor(Vector3 pos,ref List<int> neighbor_index)
    {
        Vector3Int grid_pos = get_grid(pos);
        //iterate all the adjacent grid
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                for (int k = -1; k <= 1; k++)
                {

                    Vector3Int adjacent_grid = new Vector3Int(
                        i + grid_pos.x,
                        j + grid_pos.y,
                        k + grid_pos.z);

                    if (grid_bound(adjacent_grid))
                    {

                        foreach (int index in search_grids[
                            adjacent_grid.x,
                            adjacent_grid.y,
                            adjacent_grid.z])
                        {
                            if (Vector3.Distance(position[index], pos) <= particle_radius)
                            {
                                neighbor_index.Add(index);
                            }
                        }
                    }

                }
            }
        }
    }

    void get_neighbor(int target, ref List<int> neighbor_index)
    {
        Vector3Int t_g_pos = get_grid(target);
        //iterate all the adjacent grid
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                for (int k = -1; k <= 1; k++)
                {

                    Vector3Int adjacent_grid = new Vector3Int(
                        i + t_g_pos.x,
                        j + t_g_pos.y,
                        k + t_g_pos.z);

                    if (grid_bound(adjacent_grid))
                    {

                        foreach (int index in search_grids[
                            adjacent_grid.x,
                            adjacent_grid.y,
                            adjacent_grid.z])
                        {
                            if (Vector3.Distance(position[index], position[target]) <= particle_radius)
                            {
                                neighbor_index.Add(index);
                            }
                        }
                    }

                }
            }
        }
        neighbor_index.Remove(target);

        //Profiler.BeginSample("ToArray");
        //neighbor_index = _neighbor_index.ToList();
        //Profiler.EndSample();

    }
    #endregion

    void boundary_force(int i)
    {
        float boundx = 1.0f;
        float boundy = 1.0f;
        float boundz = 1.0f;
        if (position[i].y < 0.0f)
        {
            if (velocity[i].y < 0.0f) boundy = -1.0f;
            position[i].y = Tank_Center.y;
        }
        if (position[i].x < -5.0f + Tank_Center.x)
        {
            if (velocity[i].x < 0.0f) boundx = -1.0f;
            position[i].x = -5.0f + Tank_Center.x;
        }
        if (position[i].x > 5.0f + Tank_Center.x)
        {
            if (velocity[i].x > 0.0f) boundx = -1.0f;
            position[i].x = 5.0f + Tank_Center.x;
        }
        if (position[i].z < -5.0f + Tank_Center.z)
        {
            if (velocity[i].z < 0.0f) boundz = -1.0f;
            position[i].z = -5.0f + Tank_Center.z;
        }
        if (position[i].z > 5.0f + Tank_Center.z)
        {
            if (velocity[i].z > 0.0f) boundz = -1.0f;
            position[i].z = 5.0f + Tank_Center.z;
        }
        velocity[i] = new Vector3(velocity[i].x * boundx, velocity[i].y * boundy, velocity[i].z * boundz);
    }
    float get_density(Vector3 p)
    {
        List<int> temp_neighbor = new List<int>();
        get_neighbor(p, ref temp_neighbor);
        float temp_den = 0.0f;
        for (int j = 0; j < temp_neighbor.Count; j++)
        {
            float temp1 = particle_radius * particle_radius - (p - position[temp_neighbor[j]]).magnitude;
            temp_den += temp1 * temp1 * temp1;
        }

        float h9 = h3 * h3 * h3;
        temp_den *= (mass * 315) / (64 * PI * h9);
        return temp_den;
    }
    private GameObject createMesh(int m,int n)
    {
        float mstep = 2.0f / (float)(m - 1);
        float nstep = 2.0f / (float)(n - 1);
        GameObject cloth = new GameObject();
        MeshFilter meshf = cloth.AddComponent<MeshFilter>();
        MeshRenderer meshr = cloth.AddComponent<MeshRenderer>();
        meshr.material = Resources.Load<Material>("Test Material");
        cloth.name = "The Cloth";
        Mesh meshs = new Mesh();
        List<Vector2> uvList = new List<Vector2>();
        List<int> triangularList = new List<int>();
        for (int i = 0; i < m; i++)
        {
            for (int j = 0; j < n; j++)
            {
                uvList.Add(new Vector2((mstep * (float)i) / 2.0f, (nstep * (float)j) / 2.0f));
            }
        }
        for (int i = 1; i < m; i++)
        {
            for (int j = 1; j < n; j++)
            {
                triangularList.Add((j - 1) + (i - 1) * m);
                triangularList.Add((j) + (i - 1) * m);
                triangularList.Add((j) + (i) * m);

                triangularList.Add((j - 1) + (i - 1) * m);
                triangularList.Add((j) + (i) * m);
                triangularList.Add((j - 1) + (i) * m);

                triangularList.Add((j - 1) + (i - 1) * m);
                triangularList.Add((j) + (i) * m);
                triangularList.Add((j) + (i - 1) * m);

                triangularList.Add((j - 1) + (i - 1) * m);
                triangularList.Add((j - 1) + (i) * m);
                triangularList.Add((j) + (i) * m);
            }
        }
        meshs.vertices = new Vector3[m * n];
        List<Vector3> verticesList = new List<Vector3>();
        for(int i = 0; i < m; i++)
        {
            for(int j=0;j<n;j++) verticesList.Add(face_points[i,j]);
        }
        meshs.vertices = verticesList.ToArray();
        meshs.uv = uvList.ToArray();
        meshs.triangles = triangularList.ToArray();
        meshf.mesh = meshs;
        return cloth;
    }
    void draw_edge()
    {
        for(int i = 0; i < mesh_resolution; i++)
        {
            for(int j = 0; j < mesh_resolution; j++)
            {
                float x = -5.0f + ((5.0f + 5.0f) / (mesh_resolution - 1)) * i+Tank_Center.x;
                float z = -5.0f + ((5.0f + 5.0f) / (mesh_resolution - 1)) * j+Tank_Center.z;
                face_points[i, j] = new Vector3(x, find_point(x, z), z);
            }
        }
        MeshFilter meshf = liquid_face.GetComponent<MeshFilter>();
        List<Vector3> verticesList = new List<Vector3>();
        for (int i = 0; i < mesh_resolution; i++)
        {
            for (int j = 0; j < mesh_resolution; j++)
            {
                verticesList.Add(face_points[i, j]);
            }
        }
        meshf.mesh.vertices = verticesList.ToArray();
    }
    float find_point(float x, float z)
    {
        float ymin = 0.0f;
        float ymax = 15.0f;
        float y = (ymax + ymin) / 2;
        int count = 0;
        float target = initial_density * 0.7f;
        while (Math.Abs(get_density(new Vector3(x, y, z)) - target) > 5.0f && count < 10)
        {
            if (get_density(new Vector3(x, y, z)) > target) ymin = y;
            else ymax = y;
            y = (ymax + ymin) / 2;
            count++;
        }
        return y;
    }
}
