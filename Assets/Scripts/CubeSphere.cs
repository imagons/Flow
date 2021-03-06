using UnityEngine;
using System.Collections;

// 必须要一个网格过滤器和渲染器
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CubeSphere : MonoBehaviour
{
    // 
    public float radius = 1f;
    // 圆度
    public int gridSize;
    // 用来收集顶点的网格
    private Mesh mesh;
    // 顶点集
    private Vector3[] vertices;
    // 法线
    private Vector3[] normals;
    // UV
    private Color32[] cubeUV;

    private void Awake() {
        Generate();
    }

    private void OnDrawGizmos() {
        if (vertices == null) {
            return;
        }
        for (int i = 0; i < vertices.Length; i++) {
            // Gizmos.color = Color.black;
            // Gizmos.DrawSphere(vertices[i], 0.1f);
            // Gizmos.color = Color.yellow;
            // Gizmos.DrawRay(vertices[i], normals[i]);
        }
    }

    private void Generate() {
        GetComponent<MeshFilter>().mesh = mesh = new Mesh();
        mesh.name = "Procedural CubeSphere";
        CreateVertices();
        CreateTriangles();
        CreateColliders();
    }

    // 创建顶点
    private void CreateVertices() {
        int cornerVertices = 8;
        int edgeVertices = (gridSize + gridSize + gridSize - 3) * 4;
        int faceVertices = ((gridSize - 1) * (gridSize - 1) + (gridSize - 1) * (gridSize - 1) + (gridSize - 1) * (gridSize - 1)) * 2;
        vertices = new Vector3[cornerVertices + edgeVertices + faceVertices];
        normals  = new Vector3[vertices.Length];
        cubeUV   = new Color32[vertices.Length];

        int v = 0;
        for (int y = 0; y <= gridSize; y++) {
            for (int x = 0; x <= gridSize; x++) {
                SetVertex(v++, x, y, 0);
            }

            for (int z = 1; z <= gridSize; z++) {
                SetVertex(v++, gridSize, y, z);
            }
            
            for (int x = gridSize - 1; x >= 0; x--) {
                SetVertex(v++, x, y, gridSize);
            }

            for (int z = gridSize - 1; z > 0; z--) {
                SetVertex(v++, 0, y, z);
            }
        }
        // 顶面
        for (int z = 1; z < gridSize; z++) {
            for (int x = 1; x < gridSize; x++) {
                SetVertex(v++, x, gridSize, z);
            }
        }
        // 底面
        for (int z = 1; z < gridSize; z++) {
            for (int x = 1; x < gridSize; x++) {
                SetVertex(v++, x, 0, z);
            }
        }
        mesh.vertices = vertices;
        mesh.normals  = normals;
        mesh.colors32  = cubeUV;
    }

    // 创建三角形
    private void CreateTriangles() {
        int quads = (gridSize * gridSize + gridSize * gridSize + gridSize * gridSize) * 2;
        int pointCount = 6;

        int[] trianglesZ = new int[(gridSize * gridSize) * pointCount * 2];
        int[] trianglesX = new int[(gridSize * gridSize) * pointCount * 2];
        int[] trianglesY = new int[(gridSize * gridSize) * pointCount * 2];

        // ring 一环的总数
        int ring = (gridSize + gridSize) * 2;
        // t是顶点数组的下标 v是第几个顶点
        int tZ = 0, tX = 0, tY = 0, v = 0;
        for (int y = 0; y < gridSize; y++, v++) {
            // q 这一面上的第几个正方形
            for (int q = 0; q < gridSize; q++, v++) {
                tZ = SetQuad(trianglesZ, tZ, v, v + 1, v + ring, v + ring + 1);
            }
            for (int q = 0; q < gridSize; q++, v++) {
                tX = SetQuad(trianglesX, tX, v, v + 1, v + ring, v + ring + 1);
            }
            for (int q = 0; q < gridSize; q++, v++) {
                tZ = SetQuad(trianglesZ, tZ, v, v + 1, v + ring, v + ring + 1);
            }
            for (int q = 0; q < gridSize - 1; q++, v++) {
                tX = SetQuad(trianglesX, tX, v, v + 1, v + ring, v + ring + 1);
            }
            // 每环结束的最后一个三角形会抬高一环链接到高环上去 我们应该让他和本环第一个相连
            tX = SetQuad(trianglesX, tX, v, v - ring + 1, v + ring, v + 1);
        }
        tY = CreateTopFace(trianglesY, tY, ring);
        tY = CreateBottomFace(trianglesY, tY, ring);

        mesh.subMeshCount = 3;
        mesh.SetTriangles(trianglesZ, 0);
        mesh.SetTriangles(trianglesX, 1);
        mesh.SetTriangles(trianglesY, 2);
    }
    // 创建最顶上的面
    private int CreateTopFace(int[] triangles, int t, int ring) {
        // 最上面一环的顶点
        int v = ring * gridSize;
        // 靠近x的第一行
        for (int x = 0; x < gridSize - 1; x++, v++) {
            t = SetQuad(triangles, t, v, v + 1, v + ring - 1, v + ring);
        }
        t = SetQuad(triangles, t, v, v + 1, v + ring - 1, v + 2);
        /**
            o----vTop-<---o------o
            v------o------o------^
            o------o------o------o
          vMin-->vMid-----o----vMax
           (s)-----o-->--(v)-----o
            vMin: 最外环的一个点 第一个vMin是环算法的最后一个 在算面的时候回是倒着的
            vMid: 面上的一个点 第一个vMid是面算法的第一个 在算面的时候 刚刚好适用于面算法
            vMax: 再算第一行时的最后一个+2得到的 这里+2刚好对应着vMin vMin--时 vMax++
            vTop: 等算到该面的最后一行的时候 直接vMin-2即可获得
         */
        int vMin = ring * (gridSize + 1) - 1;
        int vMid = vMin + 1;
        // 这里这个v还是靠近x的第一行的最后一个的首点
        int vMax = v + 2;
        for (int z = 1; z < gridSize - 1; z++, vMin--, vMid++, vMax++) {
            // 靠近x的第z行的第一个
            t = SetQuad(triangles, t, vMin, vMid, vMin - 1, vMid + gridSize - 1);
            // 第z行剩余的 但不含最后一个 x从1开始-1结束 两边都被绘制过了
            for (int x = 1; x < gridSize - 1; x++, vMid++) {
                t = SetQuad(triangles, t, vMid, vMid + 1, vMid + gridSize - 1, vMid + gridSize);
            }
            // 第z行最后一个四边形
            t = SetQuad(triangles, t, vMid, vMax, vMid + gridSize - 1, vMax + 1);
        }
        // 该面最后一个四边形
        int vTop = vMin - 2;
        t = SetQuad(triangles, t, vMin, vMid, vTop + 1, vTop);
        for (int x = 1; x < gridSize - 1; x++, vTop--, vMid++) {
            t = SetQuad(triangles, t, vMid, vMid + 1, vTop, vTop - 1);
        }
        t = SetQuad(triangles, t, vMid, vTop - 2, vTop, vTop - 1);
        return t;
    }

    private int CreateBottomFace(int[] triangles, int t, int ring) {
        // 最下面一环的顶点
        int v = 1;
        int vMid = vertices.Length - (gridSize - 1) * (gridSize - 1);
        t = SetQuad(triangles, t, ring - 1, vMid, 0, 1);
        // 靠近x的第一行
        for (int x = 1; x < gridSize - 1; x++, v++, vMid++) {
            t = SetQuad(triangles, t, vMid, vMid + 1, v, v + 1);
        }
        t = SetQuad(triangles, t, vMid, v + 2, v, v + 1);
        /**
           (0)----(1)-->-(v)-----o
           (e)-->-vMid----o-----vMax
          vMin-----o------o------o
            ^------o------o------o
            o-----vTop-<--o------o
            vMin: 最外环的一个点 第一个vMin是环算法的倒数第二个 在算面的时候回是倒着的
            vMid: 面上的一个点 第一个vMid是面算法的第一个 在算面的时候 刚刚好适用于面算法
            vMax: 再算v点时的最后一个+2得到的
            vTop: 等算到该面的最后一行的时候 直接vMin-2即可获得
         */
        int vMin = ring - 2;
        vMid -= gridSize - 2;
        // 这里这个v还是靠近x的第一行的最后一个的首点
        int vMax = v + 2;
        for (int z = 1; z < gridSize - 1; z++, vMin--, vMid++, vMax++) {
            // 靠近x的第z行的第一个
            t = SetQuad(triangles, t, vMin, vMid + gridSize - 1, vMin + 1, vMid);
            // 第z行剩余的 但不含最后一个 x从1开始-1结束 两边都被绘制过了
            for (int x = 1; x < gridSize - 1; x++, vMid++) {
                t = SetQuad(triangles, t, vMid + gridSize - 1, vMid + gridSize, vMid, vMid + 1);
            }
            // 第z行最后一个四边形
            t = SetQuad(triangles, t, vMid + gridSize - 1, vMax + 1, vMid, vMax);
        }
        // 该面最后一个四边形
        int vTop = vMin - 1;
        t = SetQuad(triangles, t, vTop + 1, vTop, vTop + 2, vMid);
        for (int x = 1; x < gridSize - 1; x++, vTop--, vMid++) {
            t = SetQuad(triangles, t, vTop, vTop - 1, vMid, vMid + 1);
        }
        t = SetQuad(triangles, t, vTop, vTop - 1, vMid, vTop - 2);
        return t;
    }

    // 创建碰撞器
    private void CreateColliders() {
        gameObject.AddComponent<SphereCollider>();
    }

    // 给予四个点创建两个三角形
    private static int SetQuad(int[] triangles, int i, int a, int b, int c, int d) {
        /**
            c------d
            |      |
            |      |
            a------b
            两个顺时针方向的三角形就是 a->c->b b->c->d
         */
        triangles[i] = a;
        triangles[i + 1] = triangles[i + 4] = c;
        triangles[i + 2] = triangles[i + 3] = b;
        triangles[i + 5] = d;
        return i + 6;
    }

    private void SetVertex(int i, int x, int y, int z) {
        Vector3 v = new Vector3(x, y, z) * 2f / gridSize - Vector3.one;
        float x2 = v.x * v.x;
        float y2 = v.y * v.y;
        float z2 = v.z * v.z;
        Vector3 s;
        s.x = v.x * Mathf.Sqrt(1f - y2 / 2f - z2 / 2f + y2 * z2 / 3f);
        s.y = v.y * Mathf.Sqrt(1f - x2 / 2f - z2 / 2f + x2 * z2 / 3f);
        s.z = v.z * Mathf.Sqrt(1f - x2 / 2f - y2 / 2f + x2 * y2 / 3f);

        normals [i] = s;
        vertices[i] = normals[i] * radius;
        cubeUV  [i] = new Color32((byte)x, (byte)y, (byte)z, 0);
    }
}
