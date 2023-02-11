using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class rod_handler : MonoBehaviour
{
    public main main; 
    public Mesh rodMesh;
    void Start()
    {
        rotationTime = Time.time;
        verticyTime = Time.time;
    }
    float rotationTime = 0, verticyTime = 0;

    void Update()
    {
        if(rotationTime < Time.time){
            gameObject.transform.Rotate(0,0.5f,0, Space.Self);
            rotationTime=Time.time+0.005f;
            if(verticyTime < Time.time){
                main.changeDynamicRodPoints(calculateDynamicMeshVerticies(rodMesh.vertices, gameObject.transform));
                verticyTime = verticyTime=Time.time+0.5f;
            }
        }
    }

    List<Vector3> calculateDynamicMeshVerticies(Vector3[] vertices, Transform transform){
        List<Vector3> meshVerticies = new List<Vector3>();
        Vector3 transformedVerticy;

        //For each verticy inside the mesh.
        for(int i = 0; i < vertices.Length; i++){
            //Stretch them to fit actual size.
            vertices[i].x *= transform.localScale.x;
            vertices[i].z *= transform.localScale.z;
            vertices[i].y *= transform.localScale.y;
            
            //Rotate them to fit maps rotation.
            transformedVerticy = transform.rotation * vertices[i];
            transformedVerticy += transform.localPosition;

            meshVerticies.Add(transformedVerticy);
        }
        return meshVerticies;
    }
}
