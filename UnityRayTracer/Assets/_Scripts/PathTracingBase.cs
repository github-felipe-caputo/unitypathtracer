using System.Collections.Generic;
using UnityEngine;
using Objects;

public class PathTracingBase : MonoBehaviour
{

    public ComputeShader rayTracingShader;
    public Texture skyboxTexture;

    private RenderTexture target;
    private Camera myCamera;
    private uint _currentSample;
    private Material _addMaterial;

    // Before displaying result on screen we will accumulate the results
    private RenderTexture _converged;

    private ComputeBuffer _sphereBuffer;
    private ComputeBuffer _quadBuffer;
    private ComputeBuffer _triangleBuffer;

    [Header("Post Processing")]
    public bool AntiAliasingShader = false;
    public Shader AntiAliasingShaderFile;

    [Header("Spheres")]
    public Vector2 SphereRadius = new Vector2(3.0f, 8.0f);
    public uint SpheresMax = 100;
    public float SpherePlacementRadius = 100.0f;

    [Header("Objects")]
    public Mesh _object;

    private void Awake()
    {
        myCamera = GetComponent<Camera>();
    }

    private void Update()
    {
        if (transform.hasChanged)
        {
            _currentSample = 0;
            transform.hasChanged = false;
        }
    }

    private void OnEnable()
    {
        Debug.Log("Script was enabled");
        _currentSample = 0;
        //SetUpScene();
        SetUpCornellBox();
    }

    private void OnDisable()
    {
        Debug.Log("Script was disabled");
        if (_sphereBuffer != null)
        {
            _sphereBuffer.Release();
        }
        if (_quadBuffer != null)
        {
            _quadBuffer.Release();
        }
        if (_triangleBuffer != null)
        {
            _triangleBuffer.Release();
        }
    }

    private void SetShaderParameters()
    {
        // Triangles objects
        //rayTracingShader.SetBuffer(0, "_Triangles", _triangleBuffer);
        //rayTracingShader.SetInt("_TrianglesCount", _triangleBuffer.count);

        // Quads objects
        rayTracingShader.SetBuffer(0, "_Quads", _quadBuffer);
        rayTracingShader.SetInt("_QuadsCount", _quadBuffer.count);

        // Spheres objects
        //rayTracingShader.SetBuffer(0, "_Spheres", _sphereBuffer);
        //rayTracingShader.SetInt("_SpheresCount", _sphereBuffer.count);

        // The seed for our random shader function
        rayTracingShader.SetFloat("_Seed", Random.value);

        // offset for direction of ray in the pixel
        if (AntiAliasingShader)
        {
            float offsetx = HaltonSequence(_currentSample, 2);
            float offsety = HaltonSequence(_currentSample, 3);
            rayTracingShader.SetVector("_PixelOffset", new Vector2(offsetx, offsety));
        } 
        else 
        {
            rayTracingShader.SetFloat("_PixelOffset", 0.5f);
        }

        // skybox texture
        rayTracingShader.SetTexture(0, "_SkyboxTexture", skyboxTexture);

        // matrix values for transformations
        rayTracingShader.SetMatrix("_CameraToWorld", myCamera.cameraToWorldMatrix);
        rayTracingShader.SetMatrix("_CameraInverseProjection", myCamera.projectionMatrix.inverse);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        SetShaderParameters();
        Render(destination);
    }

    private void Render(RenderTexture destination)
    {
        // Set up render target if necessary
        InitRenderTexture();

        // Set the target and dispatch the compute shader
        // target is the output texture that is written into by the shader
        // This means that on the comput shader Result -> target
        rayTracingShader.SetTexture(0, "Result", target);

        // we want spawn one thread per pixel of the render target
        // this spawns one thread group per 8×8 pixels
        // because default thread group size as defined in 
        // the Unity compute shader template is [numthreads(8,8,1)]
        int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
        rayTracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

        // material will be the AntiAliasingShader shader
        if (AntiAliasingShader)
        {
            if (_addMaterial == null)
            {
                _addMaterial = new Material(AntiAliasingShaderFile);
            }
            _addMaterial.SetFloat("_Sample", _currentSample);

            // Copies source texture into destination render texture with a shader
            Graphics.Blit(target, _converged, _addMaterial);
            Graphics.Blit(_converged, destination);
            _currentSample++;
        }
        else 
        {
            Graphics.Blit(target, _converged);
            Graphics.Blit(_converged, destination);
        }

    }

    private void InitRenderTexture()
    {
        if (target == null || target.width != Screen.width || target.height != Screen.height)
        {
            // Release texture if it's already setup
            if (target != null)
            {
                target.Release();
            }

            _currentSample = 0;

            // Get a render target for Ray Tracing
            target = new RenderTexture(Screen.width, Screen.height, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            target.enableRandomWrite = true;
            target.Create();
        }

        if (_converged == null || _converged.width != Screen.width || _converged.height != Screen.height)
        {
            // Release texture if it's already setup
            if (_converged != null)
            {
                _converged.Release();
            }

            // Get a render target for Ray Tracing
            _converged = new RenderTexture(Screen.width, Screen.height, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _converged.enableRandomWrite = true;
            _converged.Create();
        }
    }

    private void SetUpScene()
    {
        // Spheres
        List<Sphere> spheres = Objects.Extensions.createRandomSpheres(SphereRadius, SpheresMax, SpherePlacementRadius);
        _sphereBuffer = new ComputeBuffer(spheres.Count, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Sphere)));
        _sphereBuffer.SetData(spheres);
        Debug.Log("Number of spheres: " + spheres.Count);

        // Floot
        List<Quad> quads = new List<Quad>();
        Quad floor = Objects.Extensions.makeQuad(15.0f);
        floor.albedo = Vector3.one * 0.8f;
        floor.specular = Vector3.one * 0.03f;
        floor.smoothness = 0.2f;
        floor.emission = Vector3.zero;
        quads.Add(floor);

        // Assign to compute buffer
        _quadBuffer = new ComputeBuffer(quads.Count, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Quad)));
        _quadBuffer.SetData(quads);
        Debug.Log("Number of Quads: " + quads.Count);
    }

    private void SetUpCornellBox()
    {
        // Cornell box made of quds EVERYWHERE :)
        List<Quad> quads = new List<Quad>();

        Quad quad = Objects.Extensions.makeQuad(3.0f);
        quad.albedo = new Vector3(0.7295f, 0.7355f, 0.729f);
        quad.specular = Vector3.zero;
        quad.smoothness = 0.0f;
        quad.emission = Vector3.zero;

        Quad redQuad = Objects.Extensions.makeQuad(3.0f);
        redQuad.albedo = new Vector3(0.611f, 0.0555f, 0.062f);
        redQuad.specular = Vector3.zero;
        redQuad.smoothness = 0.0f;
        redQuad.emission = Vector3.zero;

        Quad greenQuad = Objects.Extensions.makeQuad(3.0f);
        greenQuad.albedo = new Vector3(0.117f, 0.4125f, 0.115f);
        greenQuad.specular = Vector3.zero;
        greenQuad.smoothness = 0.0f;
        greenQuad.emission = Vector3.zero;

        Quad lightQuad = Objects.Extensions.makeQuad(1.0f);
        lightQuad.albedo = Vector3.one;
        lightQuad.specular = Vector3.one;
        lightQuad.smoothness = 0.0f;
        lightQuad.emission = Vector3.one * 15.0f;

        List<Quad> lightCube = new List<Quad>();
        foreach (Quad q in Objects.Extensions.makeCube(2.0f)) {
            Quad newQ = q;
            newQ.albedo = Vector3.one;
            newQ.specular = Vector3.one;
            newQ.smoothness = 0.0f;
            newQ.emission = Vector3.one * 20.0f;
            lightCube.Add(newQ);
        }

        Matrix4x4 transf;

        // Floor
        quads.Add(quad);

        // Light
        transf = Matrix4x4.Translate(new Vector3(0.0f, 6.0f, 0.0f))
                          * Matrix4x4.Scale(new Vector3(1.0f, 0.05f, 1.0f));
        quads.AddRange(Objects.Extensions.tranformPlanes(transf, lightCube ));

        // Ceiling
        transf = Matrix4x4.Translate(new Vector3(0.0f, 6.0f, 0.0f))
                          * Matrix4x4.Rotate(Quaternion.AngleAxis(180, Vector3.forward));
        quads.AddRange(Objects.Extensions.tranformPlanes(transf, new List<Quad>() { quad }));

        // Right
        transf = Matrix4x4.Translate(new Vector3(3.0f, 3.0f, 0.0f))
                          * Matrix4x4.Rotate(Quaternion.AngleAxis(90, Vector3.forward));
        quads.AddRange(Objects.Extensions.tranformPlanes(transf, new List<Quad>() { greenQuad }));

        // Left
        transf = Matrix4x4.Translate(new Vector3(-3.0f, 3.0f, 0.0f))
                          * Matrix4x4.Rotate(Quaternion.AngleAxis(-90, Vector3.forward));
        quads.AddRange(Objects.Extensions.tranformPlanes(transf, new List<Quad>() { redQuad }));

        // Front
        transf = Matrix4x4.Translate(new Vector3(0.0f, 3.0f, 3.0f))
                          * Matrix4x4.Rotate(Quaternion.AngleAxis(-90, Vector3.right));
        quads.AddRange(Objects.Extensions.tranformPlanes(transf, new List<Quad>() { quad }));

        // Back
        transf = Matrix4x4.Translate(new Vector3(0.0f, 3.0f, -3.0f))
                          * Matrix4x4.Rotate(Quaternion.AngleAxis(90, Vector3.right));
        quads.AddRange(Objects.Extensions.tranformPlanes(transf, new List<Quad>() { quad }));


        // Right cube
        List<Quad> rightCube = new List<Quad>();
        foreach (Quad q in Objects.Extensions.makeCube(2.0f))
        {
            Quad newQ = q;
            newQ.albedo = new Vector3(0.6f, 0.6f, 0.6f);
            newQ.specular = Vector3.zero;
            newQ.smoothness = 0.0f;
            newQ.emission = Vector3.zero;
            rightCube.Add(newQ);
        }
        transf = Matrix4x4.Translate(new Vector3(1.0f, 1.0f, -1.0f))
                          * Matrix4x4.Rotate(Quaternion.AngleAxis(20, Vector3.up))
                          * Matrix4x4.Scale(new Vector3(1.0f, 1.0f, 1.0f));
        quads.AddRange(Objects.Extensions.tranformPlanes(transf, rightCube));

        // Left cube
        List<Quad> leftCube = new List<Quad>();
        foreach (Quad q in Objects.Extensions.makeCube(2.0f))
        {
            Quad newQ = q;
            newQ.albedo = new Vector3(0.5f, 0.5f, 0.5f);
            newQ.specular = Vector3.zero;
            newQ.smoothness = 0.0f;
            newQ.emission = Vector3.zero;
            leftCube.Add(newQ);
        }
        transf = Matrix4x4.Translate(new Vector3(-1.0f, 2.0f, 1.0f))
                          * Matrix4x4.Rotate(Quaternion.AngleAxis(-20, Vector3.up))
                          * Matrix4x4.Scale(new Vector3(1.0f, 2.0f, 1.0f));
        quads.AddRange(Objects.Extensions.tranformPlanes(transf, leftCube));

        // Assign to compute buffer
        _quadBuffer = new ComputeBuffer(quads.Count, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Quad)));
        _quadBuffer.SetData(quads);
        Debug.Log("Number of quads: " + quads.Count);
        /*
        Sphere lightSphere1 = new Sphere
        {
            position = new Vector3(2.4f, 5.4f, 1.6f),
            radius = 0.5f,
            albedo = Vector3.one,
            specular = Vector3.one,
            smoothness = 0.0f,
            emission = Vector3.one * 8.0f
        };

        Sphere lightSphere2 = new Sphere
        {
            position = new Vector3(-2.4f, 5.0f, -2.0f),
            radius = 0.7f,
            albedo = Vector3.one,
            specular = Vector3.one,
            smoothness = 0.0f,
            emission = Vector3.one * 8.0f
        };

        Sphere lightSphere3 = new Sphere
        {
            position = new Vector3(-2.4f, 0.6f, 1.0f),
            radius = 0.5f,
            albedo = Vector3.one,
            specular = Vector3.one,
            smoothness = 0.0f,
            emission = Vector3.one * 8.0f
        };

        Sphere lightSphere4 = new Sphere
        {
            position = new Vector3(2.0f, 1.2f, -1.8f),
            radius = 0.6f,
            albedo = Vector3.one,
            specular = Vector3.one,
            smoothness = 0.0f,
            emission = Vector3.one * 8.0f
        };

        List<Sphere> spheres = new List<Sphere>() { lightSphere1, lightSphere2, lightSphere3, lightSphere4 };

        _sphereBuffer = new ComputeBuffer(spheres.Count, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Sphere)));
        _sphereBuffer.SetData(spheres);
        Debug.Log("Number of spheres: " + spheres.Count);
        */
    }

    // https://en.wikipedia.org/wiki/Halton_sequence
    private float HaltonSequence(uint i, int b)
    {
        float f = 1;
        float r = 0;

        while (i > 0) {
            f = f / b;
            r = r + f * (i % b);
            i = (uint) Mathf.Ceil(i / b);
        }

        return r;
    }

}
