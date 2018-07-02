using UnityEngine;
using System.Collections.Generic;

namespace Objects
{
    public static class Extensions
    {
        // From http://blog.three-eyed-games.com/
        public static List<Sphere> createRandomSpheres(Vector2 SphereRadius, uint SpheresMax, float SpherePlacementRadius)
        {
            List<Sphere> spheres = new List<Sphere>();

            // Add a number of random spheres
            for (int i = 0; i < SpheresMax; i++)
            {
                Sphere sphere = new Sphere();

                // Radius and radius
                sphere.radius = SphereRadius.x + Random.value * (SphereRadius.y - SphereRadius.x);
                Vector2 randomPos = Random.insideUnitCircle * SpherePlacementRadius;
                sphere.position = new Vector3(randomPos.x, sphere.radius, randomPos.y);

                // Reject spheres that are intersecting others
                if (checkSpheresCollide(sphere, spheres)) {
                    continue;
                }

                // Albedo and specular color
                Color color = Random.ColorHSV();
                bool metal = Random.value < 0.5f;
                sphere.albedo = metal ? Vector3.zero : new Vector3(color.r, color.g, color.b);
                sphere.specular = metal ? new Vector3(color.r, color.g, color.b) : Vector3.one * 0.04f;
                sphere.smoothness = metal ? 1.0f : Random.value;

                bool lightSource = Random.value < 0.1f;
                sphere.emission = lightSource ? Vector3.one * 5.0f : Vector3.zero;

                // Add the sphere to the list
                spheres.Add(sphere);
            }

            return spheres;
        }

        /* 
         * Checks a sphere agins a list of sphere to see if sphere collides with any 
        */
        private static bool checkSpheresCollide(Sphere sphere, List<Sphere> spheres) {
            foreach (Sphere other in spheres)
            {
                float minDist = sphere.radius + other.radius;
                if (Vector3.SqrMagnitude(sphere.position - other.position) < minDist * minDist)
                    return true;
            }
            return false;
        }

        /*
         * This function will create a quad, centered on origin,
         * with normal pointing up in y. It will use the length value to crete 
         * the plane size and the y value for where the floor is. 
         */
        public static Quad makeQuad(float length)
        {
            Quad plane = new Quad
            {
                p1 = new Vector3(-length, 0, -length),
                p2 = new Vector3(-length, 0, length),
                p3 = new Vector3(length, 0, length),
                p4 = new Vector3(length, 0, -length)
            };
            return calculatePlaneValues(plane);
        }

        /*
         * Creates a cube out of quads, centered on origin, with "side" used
         * as the size of the cubes side
         */
        public static List<Quad> makeCube(float side)
        {
            List<Quad> planes = new List<Quad>();
            float val = side / 2.0f;

            // front
            Quad front = new Quad
            {
                p1 = new Vector3(-val, val, -val),
                p2 = new Vector3(val, val, -val),
                p3 = new Vector3(val, -val, -val),
                p4 = new Vector3(-val, -val, -val)
            };
            planes.Add(calculatePlaneValues(front));

            // back
            Quad back = new Quad
            {
                p1 = new Vector3(-val, -val, val),
                p2 = new Vector3(val, -val, val),
                p3 = new Vector3(val, val, val),
                p4 = new Vector3(-val, val, val)
            };
            planes.Add(calculatePlaneValues(back));

            // right
            Quad right = new Quad
            {
                p1 = new Vector3(val, val, val),
                p2 = new Vector3(val, -val, val),
                p3 = new Vector3(val, -val, -val),
                p4 = new Vector3(val, val, -val)
            };
            planes.Add(calculatePlaneValues(right));

            // left
            Quad left = new Quad
            {
                p1 = new Vector3(-val, val, val),
                p2 = new Vector3(-val, val, -val),
                p3 = new Vector3(-val, -val, -val),
                p4 = new Vector3(-val, -val, val)
            };
            planes.Add(calculatePlaneValues(left));

            // top
            Quad top = new Quad
            {
                p1 = new Vector3(-val, val, -val),
                p2 = new Vector3(-val, val, val),
                p3 = new Vector3(val, val, val),
                p4 = new Vector3(val, val, -val)
            };
            planes.Add(calculatePlaneValues(top));

            // bottom
            Quad bottom = new Quad
            {
                p1 = new Vector3(-val, -val, -val),
                p2 = new Vector3(val, -val, -val),
                p3 = new Vector3(val, -val, val),
                p4 = new Vector3(-val, -val, val)
            };
            planes.Add(calculatePlaneValues(bottom));

            return planes;
        }

        /*
         * Given a list of quads and a transform, this function wil apply the
         * transform to all the quads in the list.
        */
        public static List<Quad> tranformPlanes(Matrix4x4 transform, List<Quad> quads) {
            List<Quad> transformedQuads = new List<Quad>();
            foreach (Quad q in quads) {
                Quad newQ = q;
                newQ.p1 = transform.MultiplyPoint(q.p1);
                newQ.p2 = transform.MultiplyPoint(q.p2);
                newQ.p3 = transform.MultiplyPoint(q.p3);
                newQ.p4 = transform.MultiplyPoint(q.p4);
                transformedQuads.Add(calculatePlaneValues(newQ));
            }
            return transformedQuads;
        }

        /*
         * A quad is essentially a plane limited on its sides, this function
         * calculates some values to create the plane function of the quad,
         * which hlps on intersection test
        */
        public static Quad calculatePlaneValues(Quad quad)
        {
            quad.normal = Vector3.Normalize(Vector3.Cross(quad.p2 - quad.p1, quad.p4 - quad.p1));

            quad.equation.x = quad.p1.y * (quad.p2.z - quad.p3.z) + quad.p2.y * (quad.p3.z - quad.p1.z) + quad.p3.y * (quad.p1.z - quad.p2.z);
            quad.equation.y = quad.p1.z * (quad.p2.x - quad.p3.x) + quad.p2.z * (quad.p3.x - quad.p1.x) + quad.p3.z * (quad.p1.x - quad.p2.x);
            quad.equation.z = quad.p1.x * (quad.p2.y - quad.p3.y) + quad.p2.x * (quad.p3.y - quad.p1.y) + quad.p3.x * (quad.p1.y - quad.p2.y);
            quad.dist = -quad.p1.x * (quad.p2.y * quad.p3.z - quad.p3.y * quad.p2.z) - quad.p2.x * (quad.p3.y * quad.p1.z - quad.p1.y * quad.p3.z) - quad.p3.x * (quad.p1.y * quad.p2.z - quad.p2.y * quad.p1.z);

            return quad;
        }

        public static List<Triangle> objectToTriangles(Mesh _object)
        {
            List<Triangle> triangles = new List<Triangle>();
            for (int i = 0; i < _object.triangles.Length; i += 3)
            {
                Triangle triangle = new Triangle
                {
                    p1 = _object.vertices[_object.triangles[i + 0]],
                    p2 = _object.vertices[_object.triangles[i + 1]],
                    p3 = _object.vertices[_object.triangles[i + 2]]
                };
                triangles.Add(triangle);
            }
            return triangles;
        }
    }
}

