using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class single_entity : MonoBehaviour
{
    
    public Material redMaterial; 

    public void changeToChaser(){
        Renderer renderer = gameObject.GetComponent<Renderer>();
        float timePassed = 0f;

        while(timePassed < 1f){
            gameObject.transform.localScale = new Vector3(0.3f,0.3f,0.3f)*(1f+timePassed);
            timePassed += Time.deltaTime;
        }

        gameObject.GetComponent<MeshRenderer>().material = redMaterial;

    } 
}


