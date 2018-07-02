using UnityEngine;

namespace Objects {
    public struct Sphere
    {
        public Vector3 position;
        public float radius;

        public Vector3 albedo;
        public Vector3 specular;

        public float smoothness;
        public Vector3 emission;
    };

    public struct Quad
    {
        public Vector3 p1;
        public Vector3 p2;
        public Vector3 p3;
        public Vector3 p4;
        public Vector3 normal;

        public Vector3 equation;
        public float dist;

        public Vector3 albedo;
        public Vector3 specular;

        public float smoothness;
        public Vector3 emission;
    };

    public struct Triangle
    {
        public Vector3 p1;
        public Vector3 p2;
        public Vector3 p3;

        public Vector3 albedo;
        public Vector3 specular;

        public float smoothness;
        public Vector3 emission;
    };
}